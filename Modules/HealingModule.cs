using System.ComponentModel;
using HutongGames.PlayMaker.Actions;
using MonoDetour;
using MonoDetour.DetourTypes;
using MonoDetour.HookGen;
using PrepatcherPlugin;
using Silksong.FsmUtil;
using Silksong.ModMenu.Generator;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Modules;

public enum MaskHealType
{
    FullHeal,
    HealOneMask,
    NoHeal,
}

[GenerateMenu]
public class HealingSettings : ModuleSettings<HealingSettings>
{
    public override ModuleSettingsType DynamicType() => ModuleSettingsType.Healing;

    [Description("Heal when sitting at a bench.")]
    public bool BenchHeal = true;

    [Description("Heal when completing a new mask.")]
    public MaskHealType MaskHeal = MaskHealType.FullHeal;

    [Description("Heal when obtaining a new ability or silk heart.")]
    public bool AbilityHeal = true;

    [Description("Heal when bathing at a spa.")]
    public bool SpaHeal = true;

    public override void ReadDynamicData(IPacket packet)
    {
        BenchHeal.ReadData(packet);
        MaskHeal = packet.ReadEnum<MaskHealType>();
        AbilityHeal.ReadData(packet);
        SpaHeal.ReadData(packet);
    }

    public override void WriteDynamicData(IPacket packet)
    {
        BenchHeal.WriteData(packet);
        MaskHeal.WriteData(packet);
        AbilityHeal.WriteData(packet);
        SpaHeal.WriteData(packet);
    }

    protected override bool Equivalent(HealingSettings other) =>
        BenchHeal == other.BenchHeal
        && MaskHeal == other.MaskHeal
        && AbilityHeal == other.AbilityHeal
        && SpaHeal == other.SpaHeal;
}

[MonoDetourTargets(typeof(CallMethodProper))]
[MonoDetourTargets(typeof(PlayerData), GenerateControlFlowVariants = true)]
internal class HealingModule
    : GlobalSettingsModule<HealingModule, HealingSettings, HealingSettingsMenu>
{
    protected override HealingModule Self() => this;

    public override string Name => "Healing";

    public override ModuleActivationType ModuleActivationType =>
        ModuleActivationType.AnyConfiguration;

    private static void BenchControlInterceptMaxHealth(PlayMakerFSM fsm) =>
        fsm.ReplaceActions(
            a => a.IsCallMethodProper<HeroController>(nameof(HeroController.MaxHealth)),
            MaybeBenchHeal
        );

    private static void MaybeBenchHeal()
    {
        if (
            HeroController.instance != null
            && (!GetEnabledConfig(out var config) || config.BenchHeal)
        )
            HeroController.instance.MaxHealth();
    }

    private static void MaskShardInterceptHealing(PlayMakerFSM fsm) =>
        fsm.GetState("Full Health?")!
            .InsertMethod(
                0,
                _ =>
                {
                    if (
                        GetEnabledConfig(out var config)
                        && config.MaskHeal != MaskHealType.FullHeal
                    )
                        fsm.SendEvent("FINISHED");
                }
            );

    private static ReturnFlow OverrideAddToMaxHealth(PlayerData self, ref int count)
    {
        if (!GetEnabledConfig(out var config) || config.MaskHeal == MaskHealType.FullHeal)
            return ReturnFlow.None;

        PlayerDataAccess.maxHealth += count;
        PlayerDataAccess.maxHealthBase += count;

        if (config.MaskHeal == MaskHealType.HealOneMask)
        {
            PlayerDataAccess.prevHealth = PlayerDataAccess.health;
            PlayerDataAccess.health++;
        }

        return ReturnFlow.SkipOriginal;
    }

    private static void ShrineInterceptHealing(PlayMakerFSM fsm) =>
        fsm.GetState("Heal")!
            .InsertAction(
                IfEnabled(config =>
                {
                    if (!config.AbilityHeal)
                        fsm.SendEvent("FINISHED");
                }),
                0
            );

    private static void CrestInterceptHealing(PlayMakerFSM fsm) =>
        fsm.GetState("Set Return")!
            .ReplaceActions(
                a => a.IsCallMethodProper<HeroController>(nameof(HeroController.RefillAll)),
                MaybeCrestHeal
            );

    private static void MaybeCrestHeal()
    {
        if (
            HeroController.instance != null
            && (!GetEnabledConfig(out var config) || config.AbilityHeal)
        )
            HeroController.instance.RefillAll();
    }

    private static void SpaInterceptHealing(PlayMakerFSM fsm) =>
        fsm.GetState("Healing")!
            .InsertAction(
                0,
                IfEnabled(config =>
                {
                    if (!config.SpaHeal)
                        fsm.SendEvent("LEAVE");
                })
            );

    static HealingModule()
    {
        Events.AddFsmEdit("Bench Control", BenchControlInterceptMaxHealth);
        Events.AddFsmEdit("Heart Container UI", MaskShardInterceptHealing);
        Events.AddFsmEdit("Shrine Weaver Ability", "Inspection", ShrineInterceptHealing);
        Events.AddFsmEdit("Crest Get Shrine", "Control", CrestInterceptHealing);
        Events.AddFsmEdit("Spa Region", SpaInterceptHealing);
    }

    [MonoDetourHookInitialize]
    private static void Hook() =>
        Md.PlayerData.AddToMaxHealth.ControlFlowPrefix(OverrideAddToMaxHealth);
}
