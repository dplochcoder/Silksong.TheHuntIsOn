using Silksong.PurenailUtil.Collections;
using Silksong.TheHuntIsOn.Util;
using System;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules.EventsModule;

internal class HunterItemGranter
{
    internal HunterItemGranter()
    {
        intModifiers = new()
        {
            [nameof(PlayerData.maxHealth)] = IntModifier(HunterItemGrantType.Mask),
            [nameof(PlayerData.maxHealthBase)] = IntModifier(HunterItemGrantType.Mask),
            [nameof(PlayerData.silkMax)] = IntModifier(HunterItemGrantType.SilkSpool),
            [nameof(PlayerData.nailUpgrades)] = IntModifier(HunterItemGrantType.NeedleUpgrade),
        };
    }

    // Base data.
    private readonly HunterItemGrants hunterItemGrants = new();

    // Derived.
    private readonly HashSet<string> allGrantIds = [];
    private readonly HashMultiset<HunterItemGrantType> allGrants = [];

    internal int MaxHealthAdds() => allGrants.Count(HunterItemGrantType.Mask);
    internal int MaxSilkAdds() => allGrants.Count(HunterItemGrantType.SilkSpool);

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

    private readonly Dictionary<string, Func<int>> intModifiers;

    private Func<int> IntModifier(HunterItemGrantType type) => () => TheHuntIsOnPlugin.GetRole() == Lib.RoleId.Hunter ? allGrants.Count(type) : 0;
}