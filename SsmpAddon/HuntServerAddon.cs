using Silksong.TheHuntIsOn.Modules.ArchitectModule;
using Silksong.TheHuntIsOn.Modules.EventsModule;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.Modules.PauseTimerModule;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Api.Server;
using SSMP.Api.Server.Networking;
using System;

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
        receiver = api.NetServer.GetNetworkReceiver<ServerPacketId>(this, packetGenerators.Instantiate);
        moduleDataset = SeedModuleDataset?.Invoke();

        api.ServerManager.PlayerConnectEvent += player => OnUpdatePlayer?.Invoke(player);
        OnUpdatePlayer += SendModuleDataset;

        api.CommandManager.RegisterCommand(huntCommand);
        api.CommandManager.RegisterCommand(pauseTimerCommand);

        HandleServerPacket<ModuleDataset>(OnModuleDataset);
        HandleServerPacket<ReportDesync>(OnReportDesync);
        HandleServerPacket<RequestArchitectLevelData>(architectModuleServerAddon.OnRequestArchitectLevelData);
        HandleServerPacket<SeedModuleDataset>(OnSeedModuleDataset);
        HandleServerPacket<SpeedrunnerEventsDelta>(eventsModuleServerAddon.OnSpeedrunnerEventsDelta);
    }

    private readonly PacketGenerators<ServerPacketId> packetGenerators = new();

    private void HandleServerPacket<T>(Action<ushort, T> handler) where T : IIdentifiedPacket<ServerPacketId>, new()
    {
        packetGenerators.Register<T>();
        receiver!.RegisterPacketHandler<T>(new T().Identifier, (id, data) => handler(id, data));
    }

    private bool IsPlayerAuthorized(ushort id) => api?.ServerManager.GetPlayer(id)?.IsAuthorized ?? false;

    private string PlayerName(ushort id) => api?.ServerManager.GetPlayer(id)?.Username ?? "<unknown>";

    internal void SendToPlayer<T>(IServerPlayer player, T data) where T : IIdentifiedPacket<ClientPacketId>, new() => SendToPlayer(player.Id, data);

    internal void SendToPlayer<T>(ushort id, T data) where T : IIdentifiedPacket<ClientPacketId>, new()
    {
        if (data.Single) sender?.SendSingleData(data.Identifier, data, id);
        else sender?.SendCollectionData(data.Identifier, data, id);
    }

    private void SendModuleDataset(IServerPlayer player)
    {
        if (moduleDataset != null) SendToPlayer(player, moduleDataset);
    }

    internal void SendMessage(ushort id, string message) => api?.ServerManager.SendMessage(id, message);

    internal void BroadcastMessage(string message) => api?.ServerManager.BroadcastMessage(message);

    internal void Broadcast<T>(T packet) where T : IIdentifiedPacket<ClientPacketId>, new()
    {
        if (packet.Single) sender?.BroadcastSingleData(packet.Identifier, packet);
        else sender?.BroadcastCollectionData(packet.Identifier, packet);
    }

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
        if (moduleDataset != null) return;

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
        if (player == null) return;

        OnUpdatePlayer?.Invoke(player);
    }
}
