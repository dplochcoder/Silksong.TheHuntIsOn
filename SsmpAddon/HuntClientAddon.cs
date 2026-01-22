using Silksong.TheHuntIsOn.Modules.EventsModule;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.Modules.PauseTimerModule;
using Silksong.TheHuntIsOn.SsmpAddon.Packets;
using SSMP.Api.Client;
using SSMP.Api.Client.Networking;
using SSMP.Networking.Packet;
using System;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal class HuntClientAddon : ClientAddon
{
    internal static HuntClientAddon? Instance { get; private set; }

    internal static bool IsConnected => Instance?.api?.NetClient.IsConnected ?? false;

    public override bool NeedsNetwork => true;

    public override uint ApiVersion => 1u;

    protected override string Name => AddonIdentifiers.NAME;

    protected override string Version => AddonIdentifiers.VERSION;

    private IClientApi? api;
    private IClientAddonNetworkSender<ServerPacketId>? sender;
    private IClientAddonNetworkReceiver<ClientPacketId>? receiver;

    public override void Initialize(IClientApi clientApi)
    {
        api = clientApi;
        sender = api.NetClient.GetNetworkSender<ServerPacketId>(this);
        receiver = api.NetClient.GetNetworkReceiver<ClientPacketId>(this, InstantiatePacket);

        HandleClientPacket<ModuleDataset>(packet => OnModuleDatasetUpdate?.Invoke(packet));
        HandleClientPacket<ServerPauseState>(packet => OnServerPauseStateUpdate?.Invoke(packet));
        HandleClientPacket<GrantHunterItems>(packet => OnGrantHunterItems?.Invoke(packet));

        Instance = this;
    }

    private readonly Dictionary<ClientPacketId, Func<IPacketData>> packetGenerators = [];

    private IPacketData InstantiatePacket(ClientPacketId packetId) => packetGenerators.TryGetValue(packetId, out var gen) ? gen() : throw new ArgumentException($"Unknown id: {packetId}");

    private void HandleClientPacket<T>(Action<T> handler) where T : IIdentifiedPacket<ClientPacketId>, new()
    {
        var id = new T().Identifier;
        packetGenerators.Add(id, () => new T());
        receiver!.RegisterPacketHandler<T>(id, data => handler(data));
    }

    internal void Send<T>(T packet) where T : IIdentifiedPacket<ServerPacketId>, new()
    {
        if (packet.Single) sender?.SendSingleData(packet.Identifier, packet);
        else sender?.SendCollectionData(packet.Identifier, packet);
    }

    // Add-on classes use events for communication to avoid direct communication with plugin classes.
    // This is necessary for the addons to work on dedicated servers, which do not have Silksong assemblies available.
    internal static event Action<ModuleDataset>? OnModuleDatasetUpdate;
    internal static event Action<ServerPauseState>? OnServerPauseStateUpdate;
    internal static event Action<GrantHunterItems>? OnGrantHunterItems;
}
