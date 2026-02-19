using GlobalEnums;
using MonoDetour;
using MonoDetour.HookGen;
using PrepatcherPlugin;
using Silksong.FsmUtil;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Silksong.TheHuntIsOn.Modules;

internal enum NotificationSetting
{
    Silent,
    Notify
}

internal enum TravelNotificationSetting
{
    Silent,
    NotifyLocal,
    NotifyDestinationLocal,
    NotifyGlobal,
    NotifyGlobalDestinationLocal,
    NotifyDestinationGlobal,
}

internal class IntelligenceSettings : ModuleSettings<IntelligenceSettings>
{
    public override ModuleSettingsType DynamicType => ModuleSettingsType.Intelligence;

    public NotificationSetting BossKills = NotificationSetting.Silent;
    public NotificationSetting ShopPurchases = NotificationSetting.Silent;
    public NotificationSetting TollPurchases = NotificationSetting.Silent;
    public NotificationSetting SimpleKeyUsages = NotificationSetting.Silent;
    public NotificationSetting LeverHits = NotificationSetting.Silent;
    public NotificationSetting BellShrines = NotificationSetting.Silent;
    public NotificationSetting FleaRescues = NotificationSetting.Silent;
    public NotificationSetting CaravanRides = NotificationSetting.Silent;
    public TravelNotificationSetting BellwayRides = TravelNotificationSetting.Silent;
    public TravelNotificationSetting VentricaRides = TravelNotificationSetting.Silent;

    public override void ReadDynamicData(IPacket packet)
    {
        BossKills = packet.ReadEnum<NotificationSetting>();
        ShopPurchases = packet.ReadEnum<NotificationSetting>();
        TollPurchases = packet.ReadEnum<NotificationSetting>();
        SimpleKeyUsages = packet.ReadEnum<NotificationSetting>();
        LeverHits = packet.ReadEnum<NotificationSetting>();
        BellShrines = packet.ReadEnum<NotificationSetting>();
        FleaRescues = packet.ReadEnum<NotificationSetting>();
        CaravanRides = packet.ReadEnum<NotificationSetting>();
        BellwayRides = packet.ReadEnum<TravelNotificationSetting>();
        VentricaRides = packet.ReadEnum<TravelNotificationSetting>();
    }

    public override void WriteDynamicData(IPacket packet)
    {
        BossKills.WriteData(packet);
        ShopPurchases.WriteData(packet);
        TollPurchases.WriteData(packet);
        SimpleKeyUsages.WriteData(packet);
        LeverHits.WriteData(packet);
        BellShrines.WriteData(packet);
        FleaRescues.WriteData(packet);
        CaravanRides.WriteData(packet);
        BellwayRides.WriteData(packet);
        VentricaRides.WriteData(packet);
    }

    protected override bool Equivalent(IntelligenceSettings other) => BossKills == other.BossKills
        && ShopPurchases == other.ShopPurchases
        && TollPurchases == other.TollPurchases
        && SimpleKeyUsages == other.SimpleKeyUsages
        && LeverHits == other.LeverHits
        && BellShrines == other.BellShrines
        && FleaRescues == other.FleaRescues
        && CaravanRides == other.CaravanRides
        && BellwayRides == other.BellwayRides
        && VentricaRides == other.VentricaRides;
}

internal class IntelligenceMessage : IIdentifiedPacket<ServerPacketId>
{
    public ServerPacketId Identifier => ServerPacketId.IntelligenceMessage;

    public bool Single => false;

    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => false;

    public string Message = "";

    public void ReadData(IPacket packet) => Message = packet.ReadString();

    public void WriteData(IPacket packet) => Message.WriteData(packet);
}

[MonoDetourTargets(typeof(GameManager))]
[MonoDetourTargets(typeof(HealthManager))]
[MonoDetourTargets(typeof(Lever))]
[MonoDetourTargets(typeof(Lever_tk2d))]
[MonoDetourTargets(typeof(ShopItem))]
internal class IntelligenceModule : GlobalSettingsModule<IntelligenceModule, IntelligenceSettings, IntelligenceSubMenu>
{
    private const string BOSS_KILL = "A scream of agony echoes in the distance! A mighty foe has been slain.";
    private const string SHOP_PURCHASE = "The hubble of barter almost drowns out the clacking of rosaries. A shrewd deal hath been bargained.";
    private const string TOLL_PURCHASE = "Rosaries clink and clang noisily down a long metal chute. A machine has collected its toll.";
    private const string SIMPLE_KEY_USAGES = "A simple door creaks open, its key expired.";
    private static readonly IReadOnlyList<string> LEVER_HITS = [
        "A lever recoils, compelled to turn the gears once again.",
        "An overexcited bug shouts 'Kronk!' in the distance.",
        "The gears turn, the door moves, the lever cries.",
        "Clickity clackety, a lever is whacked-ity.",
        "Another lever is soundly defeated by the puzzle master.",
        "The lever bounces back, but its defiance is temporary.",
    ];
    private const string BELL_SHRINES = "A mighty bell dings and dongs in the distance, echoing across all of Pharloom.";
    private const string FLEA_RESCUES = "Barely audible over the flutter of wings, you hear a soft, joyful 'Awoo!'.";
    private const string CARAVAN_RIDES = "The great caravan settles down once again in new lands, its red passenger flying off.";

    private static string BellwayDestinationString(FastTravelLocations destination) => destination switch
    {
        FastTravelLocations.None => "nowhere land",
        FastTravelLocations.Bonetown => "Bone Bottom",
        FastTravelLocations.Docks => "the Deep Docks",
        FastTravelLocations.BoneforestEast => "the Far Fields",
        FastTravelLocations.Greymoor => "Greymoor",
        FastTravelLocations.Belltown => "Bellhart",
        FastTravelLocations.CoralTower => "the Blasted Steps",
        FastTravelLocations.City => "the Grand Bellway",
        FastTravelLocations.Peak => "the Slab",
        FastTravelLocations.Shellwood => "Shellwood",
        FastTravelLocations.Bone => "the Marrow",
        FastTravelLocations.Shadow => "Bilewater",
        FastTravelLocations.Aqueduct => "the Putrefied Ducts",
        _ => "the unknown",
    };

    private static string BellwayRideString(bool includeDestination, FastTravelLocations destination)
    {
        string clause = includeDestination ? $", tossed about the ground of {BellwayDestinationString(destination)}" : "";
        return $"The growl of the beast surges through a thousand little bells{clause}.";
    }

    private static string VentricaDestinationString(TubeTravelLocations destination) => destination switch
    {
        TubeTravelLocations.None => "nowhere land",
        TubeTravelLocations.Hub => "Terminus",
        TubeTravelLocations.Song => "the Choral Chambers",
        TubeTravelLocations.Under => "the Underworks",
        TubeTravelLocations.CityBellway => "the Grand Bellway",
        TubeTravelLocations.Hang => "the High Halls",
        TubeTravelLocations.Enclave => "Songclave",
        TubeTravelLocations.Arborium => "the Memorium",
        _ => "the unknown"
    };

    private static string VentricaRideString(bool includeDestination, TubeTravelLocations destination)
    {
        string clause = includeDestination ? $". A heavy thump is heard in {VentricaDestinationString(destination)}" : "";
        return $"Vacuum-sealed tubes fly angrily across the citadel{clause}.";
    }

    protected override IntelligenceModule Self() => this;

    public override string Name => "Intelligence";

    public override ModuleActivationType ModuleActivationType => ModuleActivationType.OnOffOnly;

    public IntelligenceModule()
    {
        Events.OnHeroUpdate += WatchPlayerData;
        Events.OnLeaveScene += WatchCaravanRide;
        Events.AddFsmEdit("FSM", ModifyTollFsm);
        Events.AddFsmEdit("rosary_string_machine", "Behaviour (special)", ModifyStringMachineFsm);
        Events.AddFsmEdit("Bone Beast NPC", "Interaction", ModifyBellBeast);
        Events.AddFsmEdit("City Travel Tube", "Tube Travel", ModifyVentrica);
    }

    private readonly EventSuppressor sendMessages = new();

    private readonly Updater<int> simpleKeys = new(0);

    private readonly Updater<int> defeatedBosses = new(0);
    private static int CalculateDefeatedBosses() =>
          (PlayerDataAccess.ant02GuardDefeated ? 1 : 0)
        + (PlayerDataAccess.cog7_automaton_defeated ? 1 : 0)
        + (PlayerDataAccess.defeatedAntQueen ? 1 : 0)
        + (PlayerDataAccess.defeatedAntTrapper ? 1 : 0)
        + (PlayerDataAccess.defeatedBellBeast ? 1 : 0)
        + (PlayerDataAccess.defeatedBoneFlyerGiant ? 1 : 0)
        + (PlayerDataAccess.DefeatedBonetownBoss ? 1 : 0)
        + (PlayerDataAccess.defeatedBroodMother ? 1 : 0)
        + (PlayerDataAccess.defeatedCloverDancers ? 1 : 0)
        + (PlayerDataAccess.defeatedCogworkDancers ? 1 : 0)
        + (PlayerDataAccess.defeatedCoralDrillers ? 1 : 0)
        + (PlayerDataAccess.defeatedCoralDrillerSolo ? 1 : 0)
        + (PlayerDataAccess.defeatedCoralKing ? 1 : 0)
        + (PlayerDataAccess.defeatedCrowCourt ? 1 : 0)
        + (PlayerDataAccess.defeatedDockForemen ? 1 : 0)
        + (PlayerDataAccess.defeatedFirstWeaver ? 1 : 0)
        + (PlayerDataAccess.defeatedFlowerQueen ? 1 : 0)
        + (PlayerDataAccess.defeatedGreyWarrior ? 1 : 0)
        + (PlayerDataAccess.defeatedLace1 ? 1 : 0)
        + (PlayerDataAccess.defeatedLaceTower ? 1 : 0)
        + (PlayerDataAccess.defeatedLastJudge ? 1 : 0)
        + (PlayerDataAccess.defeatedMossEvolver ? 1 : 0)
        + (PlayerDataAccess.defeatedMossMother ? 1 : 0)
        + (PlayerDataAccess.defeatedPhantom ? 1 : 0)
        + (PlayerDataAccess.defeatedRoachkeeperChef ? 1 : 0)
        + (PlayerDataAccess.defeatedSeth ? 1 : 0)
        + (PlayerDataAccess.defeatedSongChevalierBoss ? 1 : 0)
        + (PlayerDataAccess.defeatedSplinterQueen ? 1 : 0)
        + (PlayerDataAccess.defeatedTrobbio ? 1 : 0)
        + (PlayerDataAccess.defeatedTormentedTrobbio ? 1 : 0)
        + (PlayerDataAccess.defeatedWhiteCloverstag ? 1 : 0)
        + (PlayerDataAccess.defeatedVampireGnatBoss ? 1 : 0)
        + (PlayerDataAccess.defeatedWispPyreEffigy ? 1 : 0)
        + (PlayerDataAccess.defeatedZapCoreEnemy ? 1 : 0)
        + (PlayerDataAccess.garmondBlackThreadDefeated ? 1 : 0)
        + (PlayerDataAccess.skullKingDefeated ? 1 : 0)
        + (PlayerDataAccess.spinnerDefeated ? 1 : 0)
        + (PlayerDataAccess.wardBossDefeated ? 1 : 0);

    private readonly Updater<int> bellshrines = new(0);
    private static int CalculateBellshrines() => (PlayerDataAccess.bellShrineBellhart ? 1 : 0) + (PlayerDataAccess.bellShrineBoneForest ? 1 : 0) + (PlayerDataAccess.bellShrineEnclave ? 1 : 0) + (PlayerDataAccess.bellShrineGreymoor ? 1 : 0) + (PlayerDataAccess.bellShrineShellwood ? 1 : 0) + (PlayerDataAccess.bellShrineWilds ? 1 : 0);

    private readonly Updater<int> savedFleas = new(0);
    private static int CalculateSavedFleas(PlayerData pd) => pd.SavedFleasCount + (PlayerDataAccess.CaravanLechSaved ? 1 : 0) + (PlayerDataAccess.MetTroupeHunterWild ? 1 : 0) + (PlayerDataAccess.tamedGiantFlea ? 1 : 0);

    private void WatchPlayerData()
    {
        var pd = PlayerData.instance;

        if (simpleKeys.Update(PlayerData.instance.Collectables.GetData("Simple Key").Amount, out var prev, out var next) && prev > next) SendMessage(SIMPLE_KEY_USAGES);
        if (defeatedBosses.Update(CalculateDefeatedBosses())) SendMessage(BOSS_KILL);
        if (bellshrines.Update(CalculateBellshrines())) SendMessage(BELL_SHRINES);
        if (savedFleas.Update(CalculateSavedFleas(pd))) SendMessage(FLEA_RESCUES);
    }

    private void WatchCaravanRide(Scene scene)
    {
        if (scene.name != "Room_Caravan_Interior_Travel" || !Enabled) return;
        SendMessage(CARAVAN_RIDES);
    }

    private void ModifyTollFsm(PlayMakerFSM fsm)
    {
        if (!fsm.HasStates(["Get Text", "Confirm", "Cancel", "Start Sequence", "Wait For Currency Counter", "Taking Currency", "Wait Frame", "Before Sequence Pause", "Keep Reach", "End"])) return;

        fsm.GetState("End")!.InsertMethod(() =>
        {
            if (GetEnabledConfig(out var config) && config.TollPurchases == NotificationSetting.Notify) SendMessage(TOLL_PURCHASE);
        }, 0);
    }

    private void ModifyStringMachineFsm(PlayMakerFSM fsm) => fsm.GetState("Give Object")!.AddMethod(() =>
    {
        if (GetEnabledConfig(out var config) && config.TollPurchases == NotificationSetting.Notify) SendMessage(TOLL_PURCHASE);
    });

    private void SendTravelMessage<T>(PlayMakerFSM fsm, TravelNotificationSetting setting, Func<bool, T, string> messageFn) where T : Enum
    {
        var target = (T)fsm.FsmVariables.GetFsmEnum("Target Location").Value;
        bool hunterPresent = HuntClientAddon.OpponentsInRoom();
        switch (setting)
        {
            case TravelNotificationSetting.Silent:
                break;
            case TravelNotificationSetting.NotifyLocal:
                if (hunterPresent) SendMessage(messageFn(false, target));
                break;
            case TravelNotificationSetting.NotifyDestinationLocal:
                if (hunterPresent) SendMessage(messageFn(true, target));
                break;
            case TravelNotificationSetting.NotifyGlobal:
                SendMessage(messageFn(false, target));
                break;
            case TravelNotificationSetting.NotifyGlobalDestinationLocal:
                SendMessage(messageFn(hunterPresent, target));
                break;
            case TravelNotificationSetting.NotifyDestinationGlobal:
                SendMessage(messageFn(true, target));
                break;
        }
    }

    private void ModifyBellBeast(PlayMakerFSM fsm) => fsm.GetState("Fade")!.AddMethod(() =>
    {
        if (!GetEnabledConfig(out var config)) return;
        SendTravelMessage<FastTravelLocations>(fsm, config.BellwayRides, BellwayRideString);
    });

    private void ModifyVentrica(PlayMakerFSM fsm) => fsm.GetState("Preload Scene")!.AddMethod(() =>
    {
        if (!GetEnabledConfig(out var config)) return;
        SendTravelMessage<TubeTravelLocations>(fsm, config.VentricaRides, VentricaRideString);
    });

    private void SendMessage(string msg)
    {
        if (sendMessages.Suppressed) return;
        if (TheHuntIsOnPlugin.GetRole() != RoleId.Speedrunner) return;
        if (GameManager.instance.profileID == 0) return;

        HuntClientAddon.Instance?.Send(new IntelligenceMessage() { Message = msg });
    }

    private static void PostfixLoadGameData(GameManager self, ref SaveGameData saveGameData, ref int saveSlot)
    {
        if (Instance == null) return;
        using (Instance.sendMessages.Suppress()) Instance.WatchPlayerData();
    }

    private static bool IgnoreLever(Lever lever) => lever.gameObject.name == "Bell Shrine Lever";  // Covered by bell shrines.
    private static bool IgnoreLeverTk2d(Lever_tk2d lever) => lever.gameObject.name == "Bell Shrine Lever";  // Covered by bell shrines.

    private void SendLeverMessage() => SendMessage(LEVER_HITS[UnityEngine.Random.Range(0, LEVER_HITS.Count)]);

    private static void PostfixLeverAwake(Lever self) => self.OnActivated.AddListener(() =>
    {
        if (GetEnabledConfig(out var config) && config.LeverHits == NotificationSetting.Notify && !IgnoreLever(self)) Instance?.SendLeverMessage();
    });

    private static void PostfixLeverTk2dAwake(Lever_tk2d self) => self.CustomGateOpen.AddListener(() =>
    {
        if (GetEnabledConfig(out var config) && config.LeverHits == NotificationSetting.Notify && !IgnoreLeverTk2d(self)) Instance?.SendLeverMessage();
    });

    private static void PostfixShopItemPurchased(ShopItem self, ref Action onComplete, ref int subItemIndex)
    {
        if (GetEnabledConfig(out var config) && config.ShopPurchases == NotificationSetting.Notify) Instance?.SendMessage(SHOP_PURCHASE);
    }

    [MonoDetourHookInitialize]
    private static void Hook()
    {
        Md.GameManager.SetLoadedGameData_SaveGameData_System_Int32.Postfix(PostfixLoadGameData);
        Md.Lever.Awake.Postfix(PostfixLeverAwake);
        Md.Lever_tk2d.Awake.Postfix(PostfixLeverTk2dAwake);
        Md.ShopItem.SetPurchased.Postfix(PostfixShopItemPurchased);
    }
}

internal class IntelligenceSubMenu : ModuleSubMenu<IntelligenceSettings>
{
    public ChoiceElement<NotificationSetting> BossKills = new("Boss Kills", ChoiceModels.ForEnum<NotificationSetting>());
    public ChoiceElement<NotificationSetting> ShopPurchases = new("Shop Purchases", ChoiceModels.ForEnum<NotificationSetting>());
    public ChoiceElement<NotificationSetting> TollPurchases = new("Toll Purchases", ChoiceModels.ForEnum<NotificationSetting>());
    public ChoiceElement<NotificationSetting> SimpleKeyUsages = new("Simple Key Usages", ChoiceModels.ForEnum<NotificationSetting>());
    public ChoiceElement<NotificationSetting> LeverHits = new("Lever Hits", ChoiceModels.ForEnum<NotificationSetting>());
    public ChoiceElement<NotificationSetting> BellShrines = new("Bell Shrines", ChoiceModels.ForEnum<NotificationSetting>());
    public ChoiceElement<NotificationSetting> FleaRescues = new("Flea Rescues", ChoiceModels.ForEnum<NotificationSetting>());
    public ChoiceElement<NotificationSetting> CaravanRides = new("Caravan Rides", ChoiceModels.ForEnum<NotificationSetting>());
    public ChoiceElement<TravelNotificationSetting> BellwayRides = new("Bellway Rides", ChoiceModels.ForEnum<TravelNotificationSetting>());
    public ChoiceElement<TravelNotificationSetting> VentricaRides = new("Ventrica Rides", ChoiceModels.ForEnum<TravelNotificationSetting>());

    public override IEnumerable<MenuElement> Elements() => [BossKills, ShopPurchases, TollPurchases, SimpleKeyUsages, LeverHits, BellShrines, FleaRescues, CaravanRides, BellwayRides, VentricaRides];

    internal override void Apply(IntelligenceSettings data)
    {
        BossKills.Value = data.BossKills;
        ShopPurchases.Value = data.ShopPurchases;
        TollPurchases.Value = data.TollPurchases;
        SimpleKeyUsages.Value = data.SimpleKeyUsages;
        LeverHits.Value = data.LeverHits;
        BellShrines.Value = data.BellShrines;
        FleaRescues.Value = data.FleaRescues;
        CaravanRides.Value = data.CaravanRides;
        BellwayRides.Value = data.BellwayRides;
        VentricaRides.Value = data.VentricaRides;
    }

    internal override IntelligenceSettings Export() => new()
    {
        BossKills = BossKills.Value,
        ShopPurchases = ShopPurchases.Value,
        TollPurchases = TollPurchases.Value,
        SimpleKeyUsages = SimpleKeyUsages.Value,
        LeverHits = LeverHits.Value,
        BellShrines = BellShrines.Value,
        FleaRescues = FleaRescues.Value,
        CaravanRides = CaravanRides.Value,
        BellwayRides = BellwayRides.Value,
        VentricaRides = VentricaRides.Value,
    };
}