using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using MonoDetour.DetourTypes;
using MonoDetour.HookGen;
using Silksong.FsmUtil;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules;

internal enum MaskHealType
{
    FullHeal,
    HealOneMask,
    NoHeal,
}

internal class HealingSettings : NetworkedCloneable<HealingSettings>
{
    public bool BenchHeal = true;
    public MaskHealType MaskHeal = MaskHealType.FullHeal;
    public bool AbilityHeal = true;
    public bool SpaHeal = true;

    public override void ReadData(IPacket packet)
    {
        BenchHeal.ReadData(packet);
        MaskHeal = packet.ReadEnum<MaskHealType>();
        AbilityHeal.ReadData(packet);
        SpaHeal.ReadData(packet);
    }

    public override void WriteData(IPacket packet)
    {
        BenchHeal.WriteData(packet);
        MaskHeal.WriteData(packet);
        AbilityHeal.WriteData(packet);
        SpaHeal.WriteData(packet);
    }
}

[MonoDetourTargets(typeof(CallMethodProper))]
[MonoDetourTargets(typeof(PlayerData), GenerateControlFlowVariants = true)]
internal class HealingModule : GlobalSettingsModule<HealingModule, HealingSettings, HealingSubMenu>
{
    protected override HealingModule Self() => this;

    public override string Name => "Healing";

    public override ModuleActivationType ModuleActivationType => ModuleActivationType.AnyConfiguration;

    private static void BenchControlInterceptMaxHealth(PlayMakerFSM fsm) => fsm.ReplaceActions(a => a.IsCallMethodProper<HeroController>(nameof(HeroController.MaxHealth)), MaybeBenchHeal);

    private static void MaybeBenchHeal()
    {
        if (HeroController.instance != null && (!GetEnabledConfig(out var config) || config.BenchHeal))
            HeroController.instance.MaxHealth();
    }

    private static void MaskShardInterceptHealing(PlayMakerFSM fsm) => fsm.GetState("Full Health?")!.InsertMethod(0, _ =>
    {
        if (GetEnabledConfig(out var config) && config.MaskHeal != MaskHealType.FullHeal)
            fsm.SendEvent("FINISHED");
    });

    private static ReturnFlow OverrideAddToMaxHealth(PlayerData self, ref int count)
    {
        if (!GetEnabledConfig(out var config) || config.MaskHeal == MaskHealType.FullHeal) return ReturnFlow.None;

        self.SetInt(nameof(self.maxHealth), self.GetInt(nameof(self.maxHealth)) + count);
        self.SetInt(nameof(self.maxHealthBase), self.GetInt(nameof(self.maxHealthBase)) + count);

        if (config.MaskHeal == MaskHealType.HealOneMask)
        {
            self.SetInt(nameof(self.prevHealth), self.GetInt(nameof(self.health)));
            self.IncrementInt(nameof(self.health));
        }

        return ReturnFlow.SkipOriginal;
    }

    private static void ShrineInterceptHealing(PlayMakerFSM fsm) => fsm.GetState("Heal")!.InsertAction(IfEnabled(config =>
    {
        if (!config.AbilityHeal) fsm.SendEvent("FINISHED");
    }), 0);

    private static void CrestInterceptHealing(PlayMakerFSM fsm) => fsm.GetState("Set Return")!.ReplaceActions(a => a.IsCallMethodProper<HeroController>(nameof(HeroController.RefillAll)), MaybeCrestHeal);

    private static void MaybeCrestHeal()
    {
        if (HeroController.instance != null && (!GetEnabledConfig(out var config) || config.AbilityHeal)) HeroController.instance.RefillAll();
    }

    private static void SpaInterceptHealing(PlayMakerFSM fsm) => fsm.GetState("Healing")!.InsertAction(0, IfEnabled(config =>
    {
        if (!config.SpaHeal) fsm.SendEvent("LEAVE");
    }));

    static HealingModule()
    {
        Events.AddFsmEdit("Bench Control", BenchControlInterceptMaxHealth);
        Events.AddFsmEdit("Heart Container UI", MaskShardInterceptHealing);
        Md.PlayerData.AddToMaxHealth.ControlFlowPrefix(OverrideAddToMaxHealth);
        Events.AddFsmEdit("Shrine Weaver Ability", "Inspection", ShrineInterceptHealing);
        Events.AddFsmEdit("Crest Get Shrine", "Control", CrestInterceptHealing);
        Events.AddFsmEdit("Spa Region", SpaInterceptHealing);
    }
}

internal class HealingSubMenu : ModuleSubMenu<HealingSettings>
{
    private readonly ChoiceElement<bool> BenchHeal = new("Bench Heal", ChoiceModels.ForBool("No", "Yes"), "Heal when sitting at a bench.");
    private readonly ChoiceElement<MaskHealType> MaskHeal = new("Mask Heal", ChoiceModels.ForEnum<MaskHealType>(), "Heal when completing a new mask.");
    private readonly ChoiceElement<bool> AbilityHeal = new("Ability Heal", ChoiceModels.ForBool("No", "Yes"), "Heal when obtaining a new ability or silk heart.");
    private readonly ChoiceElement<bool> SpaHeal = new("Spa Heal", ChoiceModels.ForBool("No", "Yes"), "Heal when bathing at a spa.");

    public override IEnumerable<MenuElement> Elements() => [BenchHeal, MaskHeal, AbilityHeal, SpaHeal];

    internal override void Apply(HealingSettings data)
    {
        BenchHeal.Value = data.BenchHeal;
        MaskHeal.Value = data.MaskHeal;
        AbilityHeal.Value = data.AbilityHeal;
        SpaHeal.Value = data.SpaHeal;
    }

    internal override HealingSettings Export() => new()
    {
        BenchHeal = BenchHeal.Value,
        MaskHeal = MaskHeal.Value,
        AbilityHeal = AbilityHeal.Value,
        SpaHeal = SpaHeal.Value,
    };
}
