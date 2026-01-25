using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Silksong.TheHuntIsOn.Modules.ArchitectModule;

internal class ArchitectSettings : ModuleSettings<ArchitectSettings>
{
    public HashSet<string> EnabledGroups = [];

    public override ModuleSettingsType DynamicType => ModuleSettingsType.Architect;

    public override void ReadDynamicData(IPacket packet) => EnabledGroups.ReadData(packet, packet => packet.ReadString());

    public override void WriteDynamicData(IPacket packet) => EnabledGroups.WriteData(packet, (packet, value) => value.WriteData(packet));
}

internal class ArchitectModule : GlobalSettingsModule<ArchitectModule, ArchitectSettings, ArchitectSubMenu>
{
    private readonly ClientArchitectLevelManager levelManager;

    public ArchitectModule() => levelManager = new(() => GetEnabledConfig(out var config) ? config.EnabledGroups : []);

    protected override ArchitectModule Self() => this;

    public override string Name => "Deployers";

    public override ModuleActivationType ModuleActivationType => ModuleActivationType.AnyConfiguration;

    internal static IEnumerable<string> GetAllGroups() => Instance?.levelManager.GetAllGroups() ?? [];
}

internal class ArchitectGroupSelectorModel : IChoiceModel<string>
{
    private static List<string> AllGroups()
    {
        List<string> all = [.. ArchitectModule.GetAllGroups()];
        if (all.Count == 0) all.Add("None");
        return all;
    }

    public string Value
    {
        get => field;
        set
        {
            if (field == value) return;

            field = value;
            OnValueChanged?.Invoke(value);
            OnRawValueChanged?.Invoke(value);
        }
    } = AllGroups()[0];

    public event Action<string>? OnValueChanged;
    public event Action<object>? OnRawValueChanged;

    public string DisplayString() => Value;

    public string GetValue() => Value;

    private bool HandleOne(List<string> all, out bool changed)
    {
        if (all.Count == 1)
        {
            changed = Value != all[0];
            Value = all[0];
            return true;
        }

        if (Value == "None")
        {
            Value = all[0];
            changed = true;
            return true;
        }

        changed = false;
        return false;
    }

    public bool MoveLeft()
    {
        List<string> all = AllGroups();
        if (HandleOne(all, out bool changed)) return changed;

        for (int i = 0; i < all.Count; i++)
        {
            if (all[i] == Value || all[i].CompareTo(Value) > 0)
            {
                Value = all[(i - 1 + all.Count) % all.Count];
                return true;
            }
        }

        Value = all.Last();
        return true;
    }

    internal void PickClosest()
    {
        List<string> all = AllGroups();
        if (!all.Contains(Value)) MoveRight(all);
    }

    internal bool MoveRight(List<string> all)
    {
        if (HandleOne(all, out bool changed)) return changed;

        for (int i = 0; i < all.Count; i++)
        {
            if (all[i] == Value || all[i].CompareTo(Value) < 0)
            {
                Value = all[(i + 1) % all.Count];
                return true;
            }
        }

        Value = all.Last();
        return true;
    }

    public bool MoveRight() => MoveRight(AllGroups());

    public bool SetValue(string value)
    {
        Value = value;
        return true;
    }
}

internal class ArchitectSubMenu : ModuleSubMenu<ArchitectSettings>
{
    private readonly ArchitectGroupSelectorModel model;
    private readonly ChoiceElement<string> GroupSelector;
    private readonly ChoiceElement<bool> Enabled;

    private readonly HashSet<string> enabledGroups = [];
    private readonly EventSuppressor updateEnabled = new();

    public ArchitectSubMenu()
    {
        model = new();

        GroupSelector = new("Level Group", model, "Level group to enable/disable.");
        GroupSelector.OnValueChanged += groupId =>
        {
            using (updateEnabled.Suppress())
            {
                Enabled?.Value = enabledGroups.Contains(groupId);
            }
        };

        Enabled = new("Enabled", ChoiceModels.ForBool("No", "Yes"));
        Enabled.OnValueChanged += enabled =>
        {
            if (updateEnabled.Suppressed) return;

            if (enabled) enabledGroups.Add(GroupSelector.Value);
            else enabledGroups.Remove(GroupSelector.Value);
        };
    }

    public override IEnumerable<MenuElement> Elements() => [GroupSelector, Enabled];

    internal override void Apply(ArchitectSettings data)
    {
        enabledGroups.Clear();
        foreach (var group in data.EnabledGroups) enabledGroups.Add(group);
        model.PickClosest();
    }

    internal override ArchitectSettings Export() => new() { EnabledGroups = [.. enabledGroups] };
}
