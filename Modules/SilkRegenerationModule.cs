using MonoDetour;
using MonoDetour.Cil;
using MonoDetour.DetourTypes;
using MonoDetour.HookGen;
using MonoMod.Cil;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules;

internal class SilkRegenerationSettings : ModuleSettings<SilkRegenerationSettings>
{
    public int Silkhearts = 0;
    public float FirstSilkRegenTime = 1.45f;
    public float SilkRegenTime = 3.9f;
    public bool CanFarmSilk = true;

    public override ModuleSettingsType DynamicType => throw new System.NotImplementedException();

    public override void ReadDynamicData(IPacket packet)
    {
        Silkhearts.ReadData(packet);
        FirstSilkRegenTime.ReadData(packet);
        SilkRegenTime.ReadData(packet);
        CanFarmSilk.ReadData(packet);
    }

    public override void WriteDynamicData(IPacket packet)
    {
        Silkhearts.WriteData(packet);
        FirstSilkRegenTime.WriteData(packet);
        SilkRegenTime.WriteData(packet);
        CanFarmSilk.WriteData(packet);
    }

    protected override bool Equivalent(SilkRegenerationSettings other) => Silkhearts == other.Silkhearts
        && FirstSilkRegenTime == other.FirstSilkRegenTime
        && SilkRegenTime == other.SilkRegenTime
        && CanFarmSilk == other.CanFarmSilk;
}

[MonoDetourTargets(typeof(HeroController), GenerateControlFlowVariants = true)]
internal class SilkRegenerationModule : GlobalSettingsModule<SilkRegenerationModule, SilkRegenerationSettings, SilkRegenerationSubMenu>
{
    protected override SilkRegenerationModule Self() => this;

    public override string Name => "Silk Regeneration";

    public override ModuleActivationType ModuleActivationType => ModuleActivationType.AnyConfiguration;

    private readonly Func<int> extraSilkhearts = () => GetEnabledConfig(out var config) ? config.Silkhearts : 0;

    public override void OnEnabled() => Events.AddPdIntModifier(nameof(PlayerData.silkRegenMax), extraSilkhearts);

    public override void OnDisabled() => Events.RemovePdIntModifier(nameof(PlayerData.silkRegenMax), extraSilkhearts);

    protected override void OnGlobalConfigChanged(SilkRegenerationSettings before, SilkRegenerationSettings after)
    {
        if (before.Silkhearts != after.Silkhearts && HeroController.instance != null) HeroController.instance.ResetSilkRegen();
    }

    private static readonly EventSuppressor blockSilkGain = new();

    private static ReturnFlow PrefixAddSilk(HeroController self, ref int amount, ref bool heroEffect, ref SilkSpool.SilkAddSource source, ref bool forceCanBindEffect)
    {
        bool canFarmSilk = !GetEnabledConfig(out var config) || config.CanFarmSilk;
        return (canFarmSilk || blockSilkGain.Suppressed) ? ReturnFlow.None : ReturnFlow.SkipOriginal;
    }

    private static void HookDoSilkRegen(ILManipulationInfo info)
    {
        ILCursor cursor = new(info.Context);
        cursor.Goto(0);

        if (cursor.TryGotoNext(i => i.MatchCall<HeroController>(nameof(HeroController.AddSilk))))
        {
            static void ReallyAddSilk(HeroController self, int amount, bool heroEffect)
            {
                using (blockSilkGain.Suppress())
                {
                    self.AddSilk(amount, heroEffect);
                }
            }

            cursor.Remove();
            cursor.EmitDelegate(ReallyAddSilk);
        }
    }

    private static void PrefixResetSilkRegen(HeroController self)
    {
        bool hasConfig = GetEnabledConfig(out var config);
        self.FIRST_SILK_REGEN_DELAY = 0.65f * (hasConfig ? config.FirstSilkRegenTime / 1.45f : 1f);
        self.SILK_REGEN_DELAY = 1.9f * (hasConfig ? config.SilkRegenTime / 3.9f : 1f);
    }

    private static void PrefixStartSilkRegen(HeroController self)
    {
        bool hasConfig = GetEnabledConfig(out var config);
        self.FIRST_SILK_REGEN_DURATION = 0.8f * (hasConfig ? config.FirstSilkRegenTime / 1.45f : 1f);
        self.SILK_REGEN_DURATION = 2f * (hasConfig ? config.SilkRegenTime / 3.9f : 1f);
    }

    [MonoDetourHookInitialize]
    private static void Hook()
    {
        Md.HeroController.AddSilk_System_Int32_System_Boolean_SilkSpool_SilkAddSource_System_Boolean.ControlFlowPrefix(PrefixAddSilk);
        Md.HeroController.DoSilkRegen.ILHook(HookDoSilkRegen);
        Md.HeroController.ResetSilkRegen.Prefix(PrefixResetSilkRegen);
        Md.HeroController.StartSilkRegen.Prefix(PrefixStartSilkRegen);
    }
}

internal class SilkRegenerationSubMenu : ModuleSubMenu<SilkRegenerationSettings>
{
    private readonly SliderElement<int> Silkhearts = new("Silkhearts", SliderModels.ForInts(0, 18));
    private readonly TextInput<float> FirstSilkRegenTime = new("First Silk Regen Time", TextModels.ForFloats(0.1f, 60f), "Time to regenerate the first silk spool (default 1.45)");
    private readonly TextInput<float> SilkRegenTime = new("Silk Regen Time", TextModels.ForFloats(0.1f, 60f), "Time to regenerate all spools but the first (default 3.9)");
    private readonly ChoiceElement<bool> CanFarmSilk = new("Can Farm Silk", ChoiceModels.ForBool("No", "Yes"), "Whether silk can be obtained outside of regen");

    public override IEnumerable<MenuElement> Elements() => [Silkhearts, FirstSilkRegenTime, SilkRegenTime, CanFarmSilk];

    internal override void Apply(SilkRegenerationSettings data)
    {
        Silkhearts.Value = data.Silkhearts;
        FirstSilkRegenTime.Value = data.FirstSilkRegenTime;
        SilkRegenTime.Value = data.SilkRegenTime;
        CanFarmSilk.Value = data.CanFarmSilk;
    }

    internal override SilkRegenerationSettings Export() => new()
    {
        Silkhearts = Silkhearts.Value,
        FirstSilkRegenTime = FirstSilkRegenTime.Value,
        SilkRegenTime = SilkRegenTime.Value,
        CanFarmSilk = CanFarmSilk.Value,
    };
}
