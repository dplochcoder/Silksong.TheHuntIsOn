using Silksong.TheHuntIsOn.Modules.EventsModule;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.Modules.PauseTimerModule;
using Silksong.TheHuntIsOn.SsmpAddon.Packets;
using SSMP.Api.Server;
using SSMP.Api.Server.Networking;
using SSMP.Networking.Packet;
using System;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal class HuntServerAddon : ServerAddon
{
    public override bool NeedsNetwork => true;

    public override uint ApiVersion => 1u;

    protected override string Name => AddonIdentifiers.NAME;

    protected override string Version => AddonIdentifiers.VERSION;

    private IServerApi? api;
    private IServerAddonNetworkSender<ClientPacketId>? sender;
    private IServerAddonNetworkReceiver<ServerPacketId>? receiver;

    private ModuleDataset? moduleDataset;

    internal event Action<IServerPlayer> OnPlayerConnected
    {
        add => api?.ServerManager.PlayerConnectEvent += value;
        remove => api?.ServerManager.PlayerConnectEvent -= value;
    }

    internal void SendToPlayer<T>(IServerPlayer player, T data) where T : IIdentifiedPacket<ClientPacketId> => sender?.SendSingleData(data.Identifier, data, player.Id);

    private void SendModuleDataset(IServerPlayer player)
    {
        if (moduleDataset != null) SendToPlayer(player, moduleDataset);
    }

    public override void Initialize(IServerApi serverApi)
    {
        api = serverApi;
        sender = api.NetServer.GetNetworkSender<ClientPacketId>(this);
        receiver = api.NetServer.GetNetworkReceiver<ServerPacketId>(this, InstantiatePacket);

        OnPlayerConnected += SendModuleDataset;
        api.CommandManager.RegisterCommand(new HuntCommand());
        api.CommandManager.RegisterCommand(new PauseTimerCommand(this));
        EventsModuleAddon eventsAddon = new(this);

        HandleServerPacket<ModuleDataset>(HandleModuleDataset);
        HandleServerPacket<RecordSpeedrunnerEvents>(eventsAddon.HandleRecordSpeedrunnerEvents);
        HandleServerPacket<RequestGrantHunterItems>(eventsAddon.HandleRequestGrantHunterItems);
    }

    private readonly Dictionary<ServerPacketId, Func<IPacketData>> packetGenerators = [];

    private IPacketData InstantiatePacket(ServerPacketId packetId) => packetGenerators.TryGetValue(packetId, out var gen) ? gen() : throw new ArgumentException($"Unknown id: {packetId}");

    private void HandleServerPacket<T>(Action<ushort, T> handler) where T : IIdentifiedPacket<ServerPacketId>, new()
    {
        ServerPacketId id = new T().Identifier;
        packetGenerators.Add(id, () => new T());
        receiver!.RegisterPacketHandler<T>(id, (id, data) => handler(id, data));
    }

    private bool IsPlayerAuthorized(ushort id) => api?.ServerManager.GetPlayer(id)?.IsAuthorized ?? false;

    private string PlayerName(ushort id) => api?.ServerManager.GetPlayer(id)?.Username ?? "<unknown>";

    internal void BroadcastMessage(string message) => api?.ServerManager.BroadcastMessage(message);

    internal void Broadcast<T>(T packet) where T : IIdentifiedPacket<ClientPacketId>, new()
    {
        if (packet.Single) sender?.BroadcastSingleData(packet.Identifier, packet);
        else sender?.BroadcastCollectionData(packet.Identifier, packet);
    }

    private void HandleModuleDataset(ushort id, ModuleDataset moduleDataset)
    {
        if (!IsPlayerAuthorized(id))
        {
            api?.ServerManager.SendMessage(id, "Only authorized users can change module settings.");
            return;
        }

        this.moduleDataset = new(moduleDataset);
        Broadcast(moduleDataset);
        api?.ServerManager.BroadcastMessage($"{PlayerName(id)} updated module settings.");
    }
}
