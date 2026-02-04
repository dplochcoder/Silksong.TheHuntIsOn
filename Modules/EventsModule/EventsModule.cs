using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.Util;

namespace Silksong.TheHuntIsOn.Modules.EventsModule;

internal class EventsModule : Module<EventsModule, EmptySettings, EmptySubMenu, Empty>
{
    private readonly HunterItemGranter hunterItemGranter = new();
    private SpeedrunnerEvents speedrunnerEvents = new();

    public EventsModule()
    {
        HuntClientAddon.On<HunterItemGrants>.Received += hunterItemGranter.Reset;
        HuntClientAddon.On<HunterItemGrantsDelta>.Received += delta =>
        {
            hunterItemGranter.Update(delta, out var desynced);
            reportDesync |= desynced;
        };
        HuntClientAddon.On<SpeedrunnerEvents>.Received += speedrunnerEvents => this.speedrunnerEvents = speedrunnerEvents;
        HuntClientAddon.On<SpeedrunnerEventsDelta>.Received += speedrunnerEventsDelta => speedrunnerEvents.Update(speedrunnerEventsDelta);
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

    private int prevHunterMasks;
    private int prevHunterSilk;

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

        bool isHunter = TheHuntIsOnPlugin.GetRole() == RoleId.Hunter;
        int hunterMasks = isHunter ? hunterItemGranter.MaxHealthAdds() : 0;
        int hunterSilk = isHunter ? hunterItemGranter.MaxSilkAdds() : 0;
        if (prevHunterMasks != hunterMasks || prevHunterSilk != hunterSilk)
        {
            prevHunterMasks = hunterMasks;
            prevHunterSilk = hunterSilk;
            UIEvents.UpdateHealthAndSilk();
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
