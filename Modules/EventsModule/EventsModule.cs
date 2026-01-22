using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Silksong.TheHuntIsOn.Modules.EventsModule;

internal class EventsModule : Module<EventsModule, EmptySettings, EmptySubMenu, EmptySettings, LocalEventsLog>
{
    protected override EventsModule Self() => this;

    public override string Name => "Events";

    public override ModuleActivationType ModuleActivationType => ModuleActivationType.OnOffOnly;

    // FIXME: Handle item grants.
    public override void OnEnabled() => Events.OnHeroUpdate += RecordSpeedrunnerEvents;

    public override void OnDisabled() => Events.OnHeroUpdate -= RecordSpeedrunnerEvents;

    private readonly RateLimiter rateLimiter = new(1f);

    private bool AnyUnpublishedEvents() => LocalConfig.RecordedBoolEvents.Count > LocalConfig.RecordedBoolEvents.Count || LocalConfig.RecordedCountEvents.Any(e => !LocalConfig.PublishedCountEvents.TryGetValue(e.Key, out var published) || published < e.Value);

    private List<SpeedrunnerCountEvent> UnpublishedCountEvents()
    {
        List<SpeedrunnerCountEvent> events = [];
        foreach (var e in LocalConfig.RecordedCountEvents)
        {
            if (!LocalConfig.PublishedCountEvents.TryGetValue(e.Key, out var published) || published < e.Value)
            {
                events.Add(new()
                {
                    Type = e.Key,
                    Value = e.Value,
                });
            }
        }
        return events;
    }

    private void RecordSpeedrunnerEvents()
    {
        if (TheHuntIsOnPlugin.GetRole() != RoleId.Speedrunner) return;

        bool recordedAny = false;

        var pd = PlayerData.instance;

        recordedAny |= RecordBool(SpeedrunnerBoolEvent.SwiftStep, nameof(pd.hasDash));
        recordedAny |= RecordBool(SpeedrunnerBoolEvent.DriftersCloak, nameof(pd.hasBrolly));
        recordedAny |= RecordBool(SpeedrunnerBoolEvent.ClingGrip, nameof(pd.hasWalljump));
        recordedAny |= RecordBool(SpeedrunnerBoolEvent.Needolin, nameof(pd.hasNeedolin));
        recordedAny |= RecordBool(SpeedrunnerBoolEvent.Clawline, nameof(pd.hasHarpoonDash));
        recordedAny |= RecordBool(SpeedrunnerBoolEvent.FaydownCloak, nameof(pd.hasDoubleJump));
        recordedAny |= RecordBool(SpeedrunnerBoolEvent.SilkSoar, nameof(pd.hasSuperJump));
        recordedAny |= RecordBool(SpeedrunnerBoolEvent.ArchitectsMelody, nameof(pd.HasMelodyArchitect));
        recordedAny |= RecordBool(SpeedrunnerBoolEvent.ConductorsMelody, nameof(pd.HasMelodyConductor));
        recordedAny |= RecordBool(SpeedrunnerBoolEvent.VaultkeepersMelody, nameof(pd.HasMelodyLibrarian));
        recordedAny |= RecordBool(SpeedrunnerBoolEvent.HuntersHeart, nameof(pd.CollectedHeartHunter));
        recordedAny |= RecordBool(SpeedrunnerBoolEvent.PollenHeart, nameof(pd.CollectedHeartFlower));
        recordedAny |= RecordBool(SpeedrunnerBoolEvent.EncrustedHeart, nameof(pd.CollectedHeartCoral));
        recordedAny |= RecordBool(SpeedrunnerBoolEvent.ConjoinedHeart, nameof(pd.CollectedHeartClover));
        recordedAny |= RecordBool(SpeedrunnerBoolEvent.Everbloom, () => pd.HasWhiteFlower);

        recordedAny |= RecordCount(SpeedrunnerCountType.Masks, nameof(pd.maxHealth));
        recordedAny |= RecordCount(SpeedrunnerCountType.SilkSpools, nameof(pd.silkMax));
        recordedAny |= RecordCount(SpeedrunnerCountType.SilkHearts, nameof(pd.silkRegenMax));
        recordedAny |= RecordCount(SpeedrunnerCountType.CraftingKits, nameof(pd.ToolKitUpgrades));
        recordedAny |= RecordCount(SpeedrunnerCountType.ToolPouches, nameof(pd.ToolPouchUpgrades));
        recordedAny |= RecordCount(SpeedrunnerCountType.NeedleUpgrades, nameof(pd.nailUpgrades));
        recordedAny |= RecordCount(SpeedrunnerCountType.Melodies, [SpeedrunnerBoolEvent.ArchitectsMelody, SpeedrunnerBoolEvent.ConductorsMelody, SpeedrunnerBoolEvent.VaultkeepersMelody]);
        recordedAny |= RecordCount(SpeedrunnerCountType.Hearts, [SpeedrunnerBoolEvent.HuntersHeart, SpeedrunnerBoolEvent.EncrustedHeart, SpeedrunnerBoolEvent.EncrustedHeart, SpeedrunnerBoolEvent.ConjoinedHeart]);

        rateLimiter.Update();
        bool shouldPublish = recordedAny || (rateLimiter.Check() && AnyUnpublishedEvents());
        if (shouldPublish)
        {
            rateLimiter.Reset();
            HuntClientAddon.Instance?.Send(new RecordSpeedrunnerEvents()
            {
                BoolEvents = [.. LocalConfig.RecordedBoolEvents.Where(e => !LocalConfig.PublishedBoolEvents.Contains(e))],
                CountEvents = UnpublishedCountEvents(),
            });
        }
    }

    private bool RecordBool(SpeedrunnerBoolEvent boolEvent, Func<bool> test)
    {
        if (LocalConfig.RecordedBoolEvents.Contains(boolEvent) || !test()) return false;

        LocalConfig.RecordedBoolEvents.Add(boolEvent);
        return true;
    }

    private bool RecordBool(SpeedrunnerBoolEvent boolEvent, string pdName) => RecordBool(boolEvent, () => PlayerData.instance.GetBool(pdName));

    private bool RecordCount(SpeedrunnerCountType type, int newValue)
    {
        if (LocalConfig.RecordedCountEvents.TryGetValue(type, out var oldValue))
        {
            if (oldValue < newValue)
            {
                LocalConfig.RecordedCountEvents[type] = newValue;
                return true;
            }
        }
        else if (newValue > 0)
        {
            LocalConfig.RecordedCountEvents[type] = newValue;
            return true;
        }

        return false;
    }

    private bool RecordCount(SpeedrunnerCountType type, string pdName) => RecordCount(type, PlayerData.instance.GetInt(pdName));

    private bool RecordCount(SpeedrunnerCountType type, IEnumerable<SpeedrunnerBoolEvent> boolEvents) => RecordCount(type, boolEvents.Where(LocalConfig.RecordedBoolEvents.Contains).Count());
}
