using Silksong.PurenailUtil.Collections;
using Silksong.TheHuntIsOn.Util;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Silksong.TheHuntIsOn.Modules.EventsModule;

internal class HunterItemGranter
{
    private static readonly IReadOnlyList<(HunterItemGrantType, int)> MASK_SHARD_REWARDS = [
        (HunterItemGrantType.MaskShard, 1),
        (HunterItemGrantType.Mask, 4),
    ];

    private static readonly IReadOnlyList<(HunterItemGrantType, int)> SILK_SPOOL_REWARDS = [
        (HunterItemGrantType.SpoolFragment, 1),
        (HunterItemGrantType.SilkSpool, 2),
    ];

    private readonly Dictionary<string, Func<int>> intModifiers;
    private readonly Func<int> maxHealthModifier;
    private readonly Func<int> maxSilkModifier;

    internal HunterItemGranter()
    {
        maxHealthModifier = QuotientModifier(MASK_SHARD_REWARDS, nameof(PlayerData.heartPieces), 4);
        maxSilkModifier = QuotientModifier(SILK_SPOOL_REWARDS, nameof(PlayerData.silkSpoolParts), 2);
        intModifiers = new()
        {
            [nameof(PlayerData.heartPieces)] = ModulusModifier(MASK_SHARD_REWARDS, nameof(PlayerData.heartPieces), 4),
            [nameof(PlayerData.maxHealth)] = maxHealthModifier,
            [nameof(PlayerData.maxHealthBase)] = maxHealthModifier,
            [nameof(PlayerData.silkSpoolParts)] = ModulusModifier(SILK_SPOOL_REWARDS, nameof(PlayerData.silkSpoolParts), 2),
            [nameof(PlayerData.silkMax)] = maxSilkModifier,
            [nameof(PlayerData.nailUpgrades)] = IntModifier(HunterItemGrantType.NeedleUpgrade),
        };
    }

    // Base data.
    private readonly HunterItemGrants hunterItemGrants = new();

    // Derived.
    private readonly HashSet<string> allGrantIds = [];
    private readonly HashMultiset<HunterItemGrantType> allGrants = [];

    internal int MaxHealthAdds() => maxHealthModifier();
    internal int MaxSilkAdds() => maxSilkModifier();

    internal bool Update(HunterItemGrantsDelta delta, out bool desynced)
    {
        if (!hunterItemGrants.Update(delta, out desynced)) return false;

        foreach (var e in delta.Grants)
        {
            if (!allGrantIds.Add(e.Key)) continue;
            allGrants.AddRange(e.Value);
        }

        return true;
    }

    internal void Reset(HunterItemGrants newGrants)
    {
        hunterItemGrants.Clear();
        allGrantIds.Clear();
        allGrants.Clear();

        foreach (var e in newGrants.Grants)
        {
            allGrantIds.Add(e.Key);
            allGrants.AddRange(e.Value);
        }
    }

    internal void OnEnabled()
    {
        PrepatcherPlugin.PlayerDataVariableEvents<bool>.OnGetVariable += OverrideGetPDBool;
        foreach (var e in intModifiers) Events.AddPdIntModifier(e.Key, e.Value);
    }

    internal void OnDisabled()
    {
        PrepatcherPlugin.PlayerDataVariableEvents<bool>.OnGetVariable -= OverrideGetPDBool;
        foreach (var e in intModifiers) Events.RemovePdIntModifier(e.Key, e.Value);
    }

    private static readonly Dictionary<string, HunterItemGrantType> boolGrants = new()
    {
        [nameof(PlayerData.hasDash)] = HunterItemGrantType.SwiftStep,
        [nameof(PlayerData.hasBrolly)] = HunterItemGrantType.DriftersCloak,
        [nameof(PlayerData.hasWalljump)] = HunterItemGrantType.ClingGrip,
        [nameof(PlayerData.hasNeedolin)] = HunterItemGrantType.Needolin,
        [nameof(PlayerData.hasHarpoonDash)] = HunterItemGrantType.Clawline,
        [nameof(PlayerData.hasDoubleJump)] = HunterItemGrantType.FaydownCloak,
        [nameof(PlayerData.hasSuperJump)] = HunterItemGrantType.SilkSoar,
        [nameof(PlayerData.hasNeedleThrow)] = HunterItemGrantType.Silkspear,
        [nameof(PlayerData.hasSilkCharge)] = HunterItemGrantType.Sharpdart,
        [nameof(PlayerData.hasThreadSphere)] = HunterItemGrantType.ThreadStorm,
        [nameof(PlayerData.hasSilkBomb)] = HunterItemGrantType.RuneRage,
        [nameof(PlayerData.hasParry)] = HunterItemGrantType.CrossStitch,
        [nameof(PlayerData.hasSilkBossNeedle)] = HunterItemGrantType.PaleNails,
    };

    private bool OverrideGetPDBool(PlayerData instance, string name, bool current)
    {
        if (current || TheHuntIsOnPlugin.GetRole() != Lib.RoleId.Hunter) return current;
        return boolGrants.TryGetValue(name, out var type) && allGrants.Contains(type);
    }

    private Func<int> IntModifier(HunterItemGrantType type) => () => TheHuntIsOnPlugin.GetRole() == Lib.RoleId.Hunter ? allGrants.Count(type) : 0;

    private static readonly Dictionary<string, FieldInfo> origFields = [];
    private static int GetOrigPDInt(string name)
    {
        if (!origFields.TryGetValue(name, out var field))
        {
            field = typeof(PlayerData).GetField(name, BindingFlags.Public | BindingFlags.Instance) ?? typeof(PlayerData).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            origFields.Add(name, field);
        }

        return (int)field.GetValue(PlayerData.instance);
    }

    private bool GetNewShards(IEnumerable<(HunterItemGrantType, int)> shardRewards, string pdShardName, int modulus, out int origShards, out int newShards)
    {
        if (TheHuntIsOnPlugin.GetRole() != Lib.RoleId.Hunter)
        {
            origShards = 0;
            newShards = 0;
            return false;
        }

        origShards = GetOrigPDInt(pdShardName);
        newShards = origShards;
        foreach (var (type, count) in shardRewards) newShards += count * allGrants.Count(type);
        return true;
    }

    private Func<int> QuotientModifier(IEnumerable<(HunterItemGrantType, int)> shardRewards, string pdShardName, int modulus) => () => GetNewShards(shardRewards, pdShardName, modulus, out var origShards, out var newShards) ? newShards / modulus : 0;

    private Func<int> ModulusModifier(IEnumerable<(HunterItemGrantType, int)> shardRewards, string pdShardName, int modulus) => () => GetNewShards(shardRewards, pdShardName, modulus, out var origShards, out var newShards) ? (newShards % modulus) - origShards : 0;
}