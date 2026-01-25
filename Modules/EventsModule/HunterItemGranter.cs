using Silksong.PurenailUtil.Collections;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules.EventsModule;

internal class HunterItemGranter
{
    // Base data.
    private readonly HunterItemGrants hunterItemGrants = new();

    // Derived.
    private readonly HashSet<string> allGrantIds = [];
    private readonly HashMultiset<HunterItemGrantType> allGrants = [];

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
        this.hunterItemGrants.Clear();
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
        PrepatcherPlugin.PlayerDataVariableEvents<int>.OnGetVariable += OverridePDInt;
        PrepatcherPlugin.PlayerDataVariableEvents<int>.OnSetVariable += OverridePDInt;
    }

    internal void OnDisabled()
    {
        PrepatcherPlugin.PlayerDataVariableEvents<bool>.OnGetVariable -= OverrideGetPDBool;
        PrepatcherPlugin.PlayerDataVariableEvents<int>.OnGetVariable -= OverridePDInt;
        PrepatcherPlugin.PlayerDataVariableEvents<int>.OnSetVariable -= OverridePDInt;
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
    };

    private bool OverrideGetPDBool(PlayerData instance, string name, bool orig)
    {
        if (orig) return true;
        if (TheHuntIsOnPlugin.GetRole() != Lib.RoleId.Hunter) return orig;

        return boolGrants.TryGetValue(name, out var type) && allGrants.Contains(type);
    }

    private static readonly Dictionary<string, HunterItemGrantType> intGrants = new()
    {
        [nameof(PlayerData.maxHealth)] = HunterItemGrantType.Mask,
        [nameof(PlayerData.silkMax)] = HunterItemGrantType.SilkSpool,
        [nameof(PlayerData.nailUpgrades)] = HunterItemGrantType.NeedleUpgrade,
    };

    private int OverridePDInt(PlayerData instance, string name, int orig) => orig + (TheHuntIsOnPlugin.GetRole() == Lib.RoleId.Hunter && intGrants.TryGetValue(name, out var type) ? allGrants.Count(type) : 0);
}