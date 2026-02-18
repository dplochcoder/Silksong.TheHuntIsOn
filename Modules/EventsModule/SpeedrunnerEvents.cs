using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System.Collections.Generic;
using System.Linq;

namespace Silksong.TheHuntIsOn.Modules.EventsModule;

// New speedrunner events, reported to client or server.
internal class SpeedrunnerEvents : IDeltaBase<SpeedrunnerEvents, SpeedrunnerEventsDelta>, IIdentifiedPacket<ClientPacketId>
{
    public ClientPacketId Identifier => ClientPacketId.SpeedrunnerEvents;

    public bool Single => false;

    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => true;

    // Singular acquisitions.
    public EnumSet<SpeedrunnerBoolEvent> BoolEvents = [];
    // Repeatable acquisitions.
    public EnumMultiset<SpeedrunnerCountEventType> CountEvents = [];

    public void Clear()
    {
        BoolEvents.Clear();
        CountEvents.Clear();
    }

    public void ReadData(IPacket packet)
    {
        BoolEvents.ReadData(packet);
        CountEvents.ReadData(packet);
    }

    public void WriteData(IPacket packet)
    {
        BoolEvents.WriteData(packet);
        CountEvents.WriteData(packet);
    }

    public bool Update(SpeedrunnerEventsDelta delta)
    {
        bool changed = false;
        foreach (var boolEvent in delta.BoolEvents) changed |= BoolEvents.Add(boolEvent);
        foreach (var countDelta in delta.CountEvents) changed |= CountEvents.Set(countDelta.Type, System.Math.Max(CountEvents.Count(countDelta.Type), countDelta.NewCount));
        return changed;
    }

    public SpeedrunnerEventsDelta DeltaFrom(SpeedrunnerEvents deltaBase)
    {
        SpeedrunnerEventsDelta delta = new();
        foreach (var boolEvent in BoolEvents)
        {
            if (!deltaBase.BoolEvents.Contains(boolEvent))
                delta.BoolEvents.Add(boolEvent);
        }
        foreach (var (type, newCount) in CountEvents.Counts)
        {
            var prevCount = deltaBase.CountEvents.Count(type);
            if (prevCount >= newCount) continue;

            delta.CountEvents.Add(new()
            {
                Type = type,
                PrevCount = prevCount,
                NewCount = newCount,
            });
        }
        return delta;
    }

    private static readonly Dictionary<SpeedrunnerBoolEvent, string> pdBoolChecks = new()
    {
        [SpeedrunnerBoolEvent.SwiftStep] = nameof(PlayerData.hasDash),
        [SpeedrunnerBoolEvent.DriftersCloak] =  nameof(PlayerData.hasBrolly),
        [SpeedrunnerBoolEvent.ClingGrip] = nameof(PlayerData.hasWalljump),
        [SpeedrunnerBoolEvent.Needolin] = nameof(PlayerData.hasNeedolin),
        [SpeedrunnerBoolEvent.Clawline] = nameof(PlayerData.hasHarpoonDash),
        [SpeedrunnerBoolEvent.FaydownCloak] = nameof(PlayerData.hasDoubleJump),
        [SpeedrunnerBoolEvent.SilkSoar] = nameof(PlayerData.hasSuperJump),
        [SpeedrunnerBoolEvent.Silkspear] = nameof(PlayerData.hasNeedleThrow),
        [SpeedrunnerBoolEvent.Sharpdart] = nameof(PlayerData.hasSilkCharge),
        [SpeedrunnerBoolEvent.ThreadStorm] = nameof(PlayerData.hasThreadSphere),
        [SpeedrunnerBoolEvent.RuneRage] = nameof(PlayerData.hasSilkBomb),
        [SpeedrunnerBoolEvent.CrossStitch] = nameof(PlayerData.hasParry),
        [SpeedrunnerBoolEvent.PaleNails] = nameof(PlayerData.hasSilkBossNeedle),
        [SpeedrunnerBoolEvent.ArchitectsMelody] = nameof(PlayerData.HasMelodyArchitect),
        [SpeedrunnerBoolEvent.ConductorsMelody] = nameof(PlayerData.HasMelodyConductor),
        [SpeedrunnerBoolEvent.VaultkeepersMelody] = nameof(PlayerData.HasMelodyLibrarian),
        [SpeedrunnerBoolEvent.HuntersHeart] = nameof(PlayerData.CollectedHeartHunter),
        [SpeedrunnerBoolEvent.PollenHeart] = nameof(PlayerData.CollectedHeartFlower),
        [SpeedrunnerBoolEvent.EncrustedHeart] = nameof(PlayerData.CollectedHeartCoral),
        [SpeedrunnerBoolEvent.ConjoinedHeart] = nameof(PlayerData.CollectedHeartClover),
    };

    private static readonly Dictionary<SpeedrunnerCountEventType, string> pdIntChecks = new()
    {
        [SpeedrunnerCountEventType.Masks] = nameof(PlayerData.maxHealth),
        [SpeedrunnerCountEventType.SilkSpools] = nameof(PlayerData.silkMax),
        [SpeedrunnerCountEventType.SilkHearts] = nameof(PlayerData.silkRegenMax),
        [SpeedrunnerCountEventType.CraftingKits] = nameof(PlayerData.ToolKitUpgrades),
        [SpeedrunnerCountEventType.ToolPouches] = nameof(PlayerData.ToolPouchUpgrades),
        [SpeedrunnerCountEventType.NeedleUpgrades] = nameof(PlayerData.nailUpgrades),
    };

    public static SpeedrunnerEvents Calculate(PlayerData playerData)
    {
        SpeedrunnerEvents events = new();

        foreach (var e in pdBoolChecks)
        {
            if (playerData.GetBool(e.Value)) events.BoolEvents.Add(e.Key);
        }
        if (playerData.HasWhiteFlower) events.BoolEvents.Add(SpeedrunnerBoolEvent.Everbloom);

        foreach (var e in pdIntChecks)
        {
            var value = playerData.GetInt(e.Value);
            if (value > 0) events.CountEvents.Add(e.Key, playerData.GetInt(e.Value));
        }

        events.DeriveInt(SpeedrunnerCountEventType.SilkSkills, [SpeedrunnerBoolEvent.Silkspear, SpeedrunnerBoolEvent.Sharpdart, SpeedrunnerBoolEvent.ThreadStorm, SpeedrunnerBoolEvent.RuneRage, SpeedrunnerBoolEvent.CrossStitch, SpeedrunnerBoolEvent.PaleNails]);
        events.DeriveInt(SpeedrunnerCountEventType.Melodies, [SpeedrunnerBoolEvent.ArchitectsMelody, SpeedrunnerBoolEvent.ConductorsMelody, SpeedrunnerBoolEvent.VaultkeepersMelody]);
        events.DeriveInt(SpeedrunnerCountEventType.Hearts, [SpeedrunnerBoolEvent.HuntersHeart, SpeedrunnerBoolEvent.EncrustedHeart, SpeedrunnerBoolEvent.EncrustedHeart, SpeedrunnerBoolEvent.ConjoinedHeart]);

        return events;
    }

    private void DeriveInt(SpeedrunnerCountEventType countType, IEnumerable<SpeedrunnerBoolEvent> boolEvents)
    {
        int count = boolEvents.Where(BoolEvents.Contains).Count();
        CountEvents.Add(countType, count);
    }
}
