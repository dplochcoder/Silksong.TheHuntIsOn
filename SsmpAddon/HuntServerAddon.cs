using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Silksong.TheHuntIsOn.Modules;
using Silksong.TheHuntIsOn.Modules.ArchitectModule;
using Silksong.TheHuntIsOn.Modules.EventsModule;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.Modules.PauseTimerModule;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Api.Server;
using SSMP.Api.Server.Networking;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal class HuntServerAddon : ServerAddon
{
    public override bool NeedsNetwork => true;

    public override uint ApiVersion => AddonIdentifiers.API_VERSION;

    protected override string Name => AddonIdentifiers.NAME;

    protected override string Version => AddonIdentifiers.VERSION;

    private IServerApi? api;
    private IServerAddonNetworkSender<ClientPacketId>? sender;
    private IServerAddonNetworkReceiver<ServerPacketId>? receiver;

    private readonly ArchitectModuleServerAddon architectModuleServerAddon;
    private readonly EventsModuleServerAddon eventsModuleServerAddon;
    private readonly HuntCommand huntCommand;
    private readonly PauseTimerCommand pauseTimerCommand;

    private ModuleDataset? moduleDataset;

    private bool isPreparing;
    private readonly HashSet<ushort> readyPlayers = [];
    private int speedrunnerCountdown;
    private List<int> hunterCountdowns = [];
    private readonly List<Timer> activeTimers = [];

    // Allow mod to provide the starting module dataset.
    internal Func<ModuleDataset?>? SeedModuleDataset;

    internal event Action<IServerPlayer>? OnUpdatePlayer;

    internal void UpdateArchitectLevels() => architectModuleServerAddon.Refresh();

    internal void UpdateEvents() => eventsModuleServerAddon.Refresh();

    public HuntServerAddon()
    {
        huntCommand = new(this);

        architectModuleServerAddon = new(this);
        eventsModuleServerAddon = new(this, huntCommand);
        pauseTimerCommand = new(this, huntCommand);
    }

    public override void Initialize(IServerApi serverApi)
    {
        api = serverApi;
        sender = api.NetServer.GetNetworkSender<ClientPacketId>(this);
        receiver = api.NetServer.GetNetworkReceiver<ServerPacketId>(
            this,
            packetGenerators.Instantiate
        );
        moduleDataset = SeedModuleDataset?.Invoke();

        api.ServerManager.PlayerConnectEvent += player => OnUpdatePlayer?.Invoke(player);
        OnUpdatePlayer += SendModuleDataset;

        api.CommandManager.RegisterCommand(huntCommand);
        api.CommandManager.RegisterCommand(pauseTimerCommand);

        HandleServerPacket<IntelligenceMessage>(OnIntelligenceMessage);
        HandleServerPacket<ModuleDataset>(OnModuleDataset);
        HandleServerPacket<ReportDesync>(OnReportDesync);
        HandleServerPacket<RequestArchitectLevelData>(
            architectModuleServerAddon.OnRequestArchitectLevelData
        );
        HandleServerPacket<ReadyUp>(OnReadyUp);
        HandleServerPacket<SeedModuleDataset>(OnSeedModuleDataset);
        HandleServerPacket<SpeedrunnerEventsDelta>(
            eventsModuleServerAddon.OnSpeedrunnerEventsDelta
        );
    }

    private readonly PacketGenerators<ServerPacketId> packetGenerators = new();

    private void HandleServerPacket<T>(Action<ushort, T> handler)
        where T : IIdentifiedPacket<ServerPacketId>, new()
    {
        packetGenerators.Register<T>();
        receiver!.RegisterPacketHandler<T>(new T().Identifier, (id, data) => handler(id, data));
    }

    private bool IsPlayerAuthorized(ushort id) =>
        api?.ServerManager.GetPlayer(id)?.IsAuthorized ?? false;

    private string PlayerName(ushort id) =>
        api?.ServerManager.GetPlayer(id)?.Username ?? "<unknown>";

    internal void SendToPlayer<T>(IServerPlayer player, T data)
        where T : IIdentifiedPacket<ClientPacketId>, new() => SendToPlayer(player.Id, data);

    internal void SendToPlayer<T>(ushort id, T data)
        where T : IIdentifiedPacket<ClientPacketId>, new()
    {
        if (data.Single)
            sender?.SendSingleData(data.Identifier, data, id);
        else
            sender?.SendCollectionData(data.Identifier, data, id);
    }

    private void SendModuleDataset(IServerPlayer player)
    {
        if (moduleDataset != null)
            SendToPlayer(player, moduleDataset);
    }

    internal void SendMessage(ushort id, string message) =>
        api?.ServerManager.SendMessage(id, message);

    internal void BroadcastMessage(string message) => api?.ServerManager.BroadcastMessage(message);

    internal void Broadcast<T>(T packet)
        where T : IIdentifiedPacket<ClientPacketId>, new()
    {
        if (packet.Single)
            sender?.BroadcastSingleData(packet.Identifier, packet);
        else
            sender?.BroadcastCollectionData(packet.Identifier, packet);
    }

    internal bool IsPreparing => isPreparing;

    internal int TotalPlayerCount => api?.ServerManager.Players.Count ?? 0;

    internal int ReadyPlayerCount => readyPlayers.Count;

    internal bool AllPlayersReady =>
        TotalPlayerCount > 0 && readyPlayers.Count >= TotalPlayerCount;

    internal void StartPreparing()
    {
        isPreparing = true;
        readyPlayers.Clear();
        BroadcastRoundPrepareState();
    }

    internal void CancelPreparing()
    {
        isPreparing = false;
        readyPlayers.Clear();
        BroadcastRoundPrepareState();
    }

    internal void FinishRound()
    {
        isPreparing = false;
        readyPlayers.Clear();
    }

    internal int SpeedrunnerCountdown => speedrunnerCountdown;

    internal IReadOnlyList<int> HunterCountdowns => hunterCountdowns;

    internal void SetSpeedrunnerCountdown(int seconds) => speedrunnerCountdown = seconds;

    internal void SetHunterCountdowns(List<int> seconds) => hunterCountdowns = seconds;

    internal void ExecuteRoundStart()
    {
        ClearActiveTimers();

        var now = DateTime.UtcNow;

        // Reset pause timer state without broadcasting (OnGameReset already cleared it).
        pauseTimerCommand.ClearCountdownsSilent();

        // Pause the server indefinitely; unpause when the speedrunner countdown expires.
        if (speedrunnerCountdown > 0)
        {
            var unpauseAt = now.AddSeconds(speedrunnerCountdown);
            pauseTimerCommand.SetPauseState(true, long.MaxValue);

            // Speedrunner countdown display.
            pauseTimerCommand.AddCountdownSilent(
                now,
                new Countdown
                {
                    FinishTimeTicks = unpauseAt.Ticks,
                    Message = "Speedrunner GO",
                }
            );

            // Schedule unpause and speedrunner go message.
            ScheduleTimer(speedrunnerCountdown, () =>
            {
                pauseTimerCommand.UpdatePauseState(false, 0);
                BroadcastMessage("&lSpeedrunner GO!&r");
            });
        }

        // Hunter countdown displays and scheduled messages.
        for (int i = 0; i < hunterCountdowns.Count; i++)
        {
            int seconds = hunterCountdowns[i];
            int hunterNum = i + 1;
            string label = hunterCountdowns.Count == 1
                ? "Hunters GO"
                : $"Hunter {hunterNum} GO";

            pauseTimerCommand.AddCountdownSilent(
                now,
                new Countdown
                {
                    FinishTimeTicks = now.AddSeconds(seconds).Ticks,
                    Message = label,
                }
            );

            ScheduleTimer(seconds, () =>
                BroadcastMessage($"&l{label}!&r"));
        }

        // Single broadcast with the complete state.
        pauseTimerCommand.BroadcastState();
    }

    private void ScheduleTimer(int seconds, Action callback)
    {
        Timer? timer = null;
        timer = new Timer(
            _ =>
            {
                callback();
                timer?.Dispose();
            },
            null,
            seconds * 1000,
            Timeout.Infinite
        );
        activeTimers.Add(timer);
    }

    private void ClearActiveTimers()
    {
        foreach (var timer in activeTimers)
            timer.Dispose();
        activeTimers.Clear();
    }

    private void BroadcastRoundPrepareState()
    {
        var names = readyPlayers
            .Select(id => PlayerName(id))
            .ToList();
        Broadcast(new RoundPrepareState { IsPreparing = isPreparing, ReadyPlayerNames = names });
    }

    private void OnReadyUp(ushort id, ReadyUp _)
    {
        if (!isPreparing)
        {
            SendMessage(id, "No round is being prepared.");
            return;
        }

        if (!readyPlayers.Add(id))
        {
            SendMessage(id, "You are already ready.");
            return;
        }

        BroadcastMessage($"&l{PlayerName(id)}&r is ready! ({readyPlayers.Count}/{TotalPlayerCount})");
        BroadcastRoundPrepareState();
    }

    private void OnIntelligenceMessage(ushort id, IntelligenceMessage intelligenceMessage) =>
        BroadcastMessage(intelligenceMessage.Message);

    private void OnModuleDataset(ushort id, ModuleDataset moduleDataset)
    {
        if (!IsPlayerAuthorized(id))
        {
            SendMessage(id, "Only authorized users can change module settings.");
            return;
        }

        this.moduleDataset = moduleDataset;
        Broadcast(moduleDataset);
        BroadcastMessage($"{PlayerName(id)} updated module settings.");
    }

    private void OnSeedModuleDataset(ushort id, SeedModuleDataset seedModuleDataset)
    {
        if (moduleDataset != null)
            return;

        if (!IsPlayerAuthorized(id))
        {
            SendMessage(id, "Only authorized users can seed module settings.");
            return;
        }

        moduleDataset = seedModuleDataset.ModuleDataset;
        Broadcast(moduleDataset);
        BroadcastMessage($"{PlayerName(id)} seeded module settings.");
    }

    private void OnReportDesync(ushort id, ReportDesync reportDesync)
    {
        var player = api?.ServerManager.GetPlayer(id);
        if (player == null)
            return;

        OnUpdatePlayer?.Invoke(player);
    }
}
