using System;
using System.Collections.Generic;
using GlobalEnums;
using HutongGames.PlayMaker.Actions;
using Md.HutongGames.PlayMaker.Fsm;
using MonoDetour;
using MonoDetour.DetourTypes;
using MonoDetour.HookGen;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Modules;

internal enum ClampDamageType
{
    Off,
    Single,
    Double,
}

internal class DamageSettings : ModuleSettings<DamageSettings>
{
    public float InvincibilityTime = 1.0f;
    public ClampDamageType ClampDamage = ClampDamageType.Off;

    public override ModuleSettingsType DynamicType => ModuleSettingsType.Damage;

    public override void ReadDynamicData(IPacket packet)
    {
        InvincibilityTime.ReadData(packet);
        ClampDamage = packet.ReadEnum<ClampDamageType>();
    }

    public override void WriteDynamicData(IPacket packet)
    {
        InvincibilityTime.WriteData(packet);
        ClampDamage.WriteData(packet);
    }

    protected override bool Equivalent(DamageSettings other) =>
        InvincibilityTime == other.InvincibilityTime
        && ClampDamage == other.ClampDamage;
}

[MonoDetourTargets(typeof(PlayerData), GenerateControlFlowVariants = true)]
internal class DamageModule
    : GlobalSettingsModule<
            DamageModule, 
            DamageSettings, 
            DamageSubMenu
    >
{
    private double lastHit;
    private int comboHits;
    private static bool overrideNextInvulnerability;

    protected override DamageModule Self() => this;

    public override string Name => "Damage";

    public override ModuleActivationType ModuleActivationType =>
        ModuleActivationType.AnyConfiguration;

    private static ReturnFlow PrefixTakeHealth(
        PlayerData self,
        ref int amount,
        ref bool hasBlueHealth,
        ref bool allowFracturedMaskBreak
    )
    {
        if (!GetEnabledConfig(out var config) || config.ClampDamage == ClampDamageType.Off)
            return ReturnFlow.None;

        double now = UnityEngine.Time.fixedTimeAsDouble;
        int maxHits = config.ClampDamage == ClampDamageType.Double ? 2 : 1;

        double gap = now - Instance.lastHit;
        bool isCombo = gap <= 0.9;
        Instance.lastHit = now;

        UnityEngine.Debug.Log($"[DamageModule] TakeHealth amount={amount} gap={gap:F4}s isCombo={isCombo} comboHits={Instance.comboHits}");

        if (isCombo && Instance.comboHits >= maxHits)
            return ReturnFlow.SkipOriginal;

        Instance.comboHits = isCombo ? Instance.comboHits + 1 : 1;
        if (amount > maxHits)
            amount = maxHits;

        overrideNextInvulnerability = true;
        return ReturnFlow.None;
    }

    private const float HazardInvulnerabilityTime = 2.3f;

    private static void PostfixStartInvulnerable(
        HeroController self,
        ref float duration
    )
    {
        if (!GetEnabledConfig(out var config))
            return;

        if (!overrideNextInvulnerability)
            return;

        overrideNextInvulnerability = false;

        bool isHazard = duration >= HazardInvulnerabilityTime;
        if (isHazard && config.InvincibilityTime <= HazardInvulnerabilityTime)
            return;

        HeroController.instance.invulnerableDuration = config.InvincibilityTime;
    }

    [MonoDetourHookInitialize]
    private static void Hook()
    {
        Md.PlayerData.TakeHealth.ControlFlowPrefix(PrefixTakeHealth);
        Md.HeroController.StartInvulnerable.Postfix(PostfixStartInvulnerable);
    }
}

internal class DamageSubMenu : ModuleSubMenu<DamageSettings>
{
    private readonly TextInput<float> InvincibilityTime = new(
        "Invincibility Time",
        TextModels.ForFloats(1.0f, 5f),
        "Seconds of invincibility after taking damage (default 1.0)"
    );
    private readonly ChoiceElement<ClampDamageType> ClampDamage = new(
        "Clamp Damage",
        ChoiceModels.ForEnum<ClampDamageType>(),
        "Cap the maximum damage taken per hit"
    );

    public override IEnumerable<MenuElement> Elements() =>
        [InvincibilityTime, ClampDamage];

    internal override void Apply(DamageSettings data)
    {
        InvincibilityTime.Value = data.InvincibilityTime;
        ClampDamage.Value = data.ClampDamage;
    }

    internal override DamageSettings Export() =>
        new()
        {
            InvincibilityTime = InvincibilityTime.Value,
            ClampDamage = ClampDamage.Value,
        };
}
