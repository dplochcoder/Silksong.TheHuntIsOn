using System.ComponentModel;
using HutongGames.PlayMaker.Actions;
using Silksong.FsmUtil;
using Silksong.FsmUtil.Actions;
using Silksong.ModMenu.Generator;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;

namespace Silksong.TheHuntIsOn.Modules;

[GenerateMenu]
public class AutoTriggerSettings : ModuleSettings<AutoTriggerSettings>
{
    public override ModuleSettingsType DynamicType() => ModuleSettingsType.AutoTrigger;

    [Description("Auto-trigger the Bell Beast fight.")]
    public bool BellBeast;

    [Description("Auto-trigger the Last Judge fight.")]
    public bool LastJudge;

    [Description("Auto-trigger the Grand Mother Silk fight.")]
    public bool GrandMotherSilk;

    public override void ReadDynamicData(IPacket packet)
    {
        BellBeast.ReadData(packet);
        LastJudge.ReadData(packet);
        GrandMotherSilk.ReadData(packet);
    }

    public override void WriteDynamicData(IPacket packet)
    {
        BellBeast.WriteData(packet);
        LastJudge.WriteData(packet);
        GrandMotherSilk.WriteData(packet);
    }

    protected override bool Equivalent(AutoTriggerSettings other) =>
        BellBeast == other.BellBeast
        && LastJudge == other.LastJudge
        && GrandMotherSilk == other.GrandMotherSilk;
}

internal class AutoTriggerModule
    : GlobalSettingsModule<AutoTriggerModule, AutoTriggerSettings, AutoTriggerSettingsMenu>
{
    public override string Name => "Auto Trigger";

    public override ModuleActivationType ModuleActivationType =>
        ModuleActivationType.AnyConfiguration;

    protected override AutoTriggerModule Self() => this;

    // TODO: Switch to AddMethod when it's fixed (https://github.com/silksong-modding/Silksong.FsmUtil/pull/16)
    private static void AutoTriggerBellBeast(PlayMakerFSM fsm) =>
        fsm.GetState("Idle")!
            .AddAction(
                new LambdaAction()
                {
                    Method = () =>
                    {
                        var x = HeroController.instance.transform.position.x;
                        if (
                            GetEnabledConfig(out var config)
                            && config.BellBeast
                            && x >= 70.5f
                            && x <= 101f
                        )
                        {
                            fsm.SendEvent("ENTER");
                            fsm.SendEvent("DESTROY");
                        }
                    },
                    EveryFrame = true,
                }
            );

    private static void AutoTriggerLastJudge(PlayMakerFSM fsm)
    {
        var waitState = fsm.AddState("Wait for Refight");
        waitState.AddTransition("START REFIGHT", "Start Refight");
        waitState.AddAction(
            new LambdaAction()
            {
                Method = () =>
                {
                    if (HeroController.instance.transform.position.x >= 9f)
                        fsm.SendEvent("START REFIGHT");
                },
                EveryFrame = true,
            }
        );

        var initState = fsm.GetState("Init")!;
        initState.AddTransition("AUTO TRIGGER", "Wait for Refight");

        int idx = initState.Actions.ArrayIndexOf(a => a is PlayerDataBoolTest);
        initState.InsertMethod(
            _ =>
            {
                if (GetEnabledConfig(out var config) && config.LastJudge)
                    fsm.SendEvent("AUTO TRIGGER");
            },
            idx
        );
    }

    private static void AutoTriggerGrandMotherSilk(PlayMakerFSM fsm)
    {
        fsm.GetState("Idle")!
            .AddAction(
                new LambdaAction()
                {
                    Method = () =>
                    {
                        var pos = HeroController.instance.transform.position;
                        if (
                            GetEnabledConfig(out var config)
                            && config.GrandMotherSilk
                            && pos.x >= 22f
                            && pos.y >= 133f
                        )
                            fsm.SendEvent("CHALLENGE START");
                    },
                    EveryFrame = true,
                }
            );
        fsm.GetState("Challenge Cam")!
            .AddAction(
                new LambdaAction()
                {
                    Method = () =>
                    {
                        if (GetEnabledConfig(out var config) && config.GrandMotherSilk)
                            fsm.SendEvent("CHALLENGE");
                    },
                    EveryFrame = true,
                }
            );
    }

    internal AutoTriggerModule()
    {
        Events.AddFsmEdit(
            "Bone_05_boss",
            "Thick Silk Vines Beast",
            "Control",
            AutoTriggerBellBeast
        );
        Events.AddFsmEdit(
            "Bone_05_boss",
            "Return Battle",
            "Start Return Battle",
            AutoTriggerBellBeast
        );
        Events.AddFsmEdit("Coral_Judge_Arena", "Boss Scene", "Control", AutoTriggerLastJudge);
        Events.AddFsmEdit(
            "Cradle_03",
            "Intro Sequence",
            "First Challenge",
            AutoTriggerGrandMotherSilk
        );
    }
}
