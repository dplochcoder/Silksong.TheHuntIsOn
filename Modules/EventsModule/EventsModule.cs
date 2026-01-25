using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.Util;

namespace Silksong.TheHuntIsOn.Modules.EventsModule;

internal class EventsModule : Module<EventsModule, EmptySettings, EmptySubMenu, Empty>
{
    private readonly HunterItemGranter hunterItemGranter = new();
    private readonly SpeedrunnerEvents speedrunnerEvents = new();

    public EventsModule()
    {
        HuntClientAddon.OnHunterItemGrants += hunterItemGranter.Reset;
        HuntClientAddon.OnHunterItemGrantsDelta += delta =>
        {
            hunterItemGranter.Update(delta, out var desynced);
            reportDesync |= desynced;
        };
        HuntClientAddon.OnSpeedrunnerEvents += speedrunnerEvents => this.speedrunnerEvents.Clear();
        HuntClientAddon.OnSpeedrunnerEventsDelta += speedrunnerEventsDelta => speedrunnerEvents.Update(speedrunnerEventsDelta);
    }

    protected override EventsModule Self() => this;

    public override string Name => "Events";

    public override ModuleActivationType ModuleActivationType => ModuleActivationType.OnOffOnly;

    public override void OnEnabled()
    {
        hunterItemGranter.OnEnabled();
        Events.OnHeroUpdate += Update;
    }

    public override void OnDisabled()
    {
        hunterItemGranter.OnDisabled();
        Events.OnHeroUpdate -= Update;
    }

    private bool reportDesync = false;
    private readonly RateLimiter desyncRateLimiter = new(1f);
    private readonly RateLimiter publishRateLimiter = new(1f);

    private void Update()
    {
        desyncRateLimiter.Update();
        publishRateLimiter.Update();

        if (reportDesync && desyncRateLimiter.Check())
        {
            TheHuntIsOnPlugin.LogError("Desync! Requesting all server information again.");
            HuntClientAddon.Instance?.Send(new ReportDesync());
            desyncRateLimiter.Reset();
        }

        if (TheHuntIsOnPlugin.GetRole() == RoleId.Speedrunner && publishRateLimiter.Check())
        {
            var events = SpeedrunnerEvents.Calculate(PlayerData.instance);
            var delta = events.DeltaFrom(speedrunnerEvents);
            if (!delta.IsEmpty)
            {
                HuntClientAddon.Instance?.Send(delta);
                publishRateLimiter.Reset();
            }
        }
    }
}
