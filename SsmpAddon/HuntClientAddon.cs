using Silksong.TheHuntIsOn.Modules.EventsModule;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.Modules.PauseTimerModule;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Api.Client;
using SSMP.Api.Client.Networking;
using SSMP.Networking.Packet;
using System;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal class HuntClientAddon : TogglableClientAddon
{
    internal static HuntClientAddon? Instance { get; private set; }

    internal static bool IsConnected => Instance?.api?.NetClient.IsConnected ?? false;

    public override bool NeedsNetwork => true;

    public override uint ApiVersion => AddonIdentifiers.API_VERSION;

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

        HandleClientPacket<HunterItemGrants>(packet => OnHunterItemGrants?.Invoke(packet));
        HandleClientPacket<HunterItemGrantsDelta>(packet => OnHunterItemGrantsDelta?.Invoke(packet));
        HandleClientPacket<ModuleDataset>(packet => OnModuleDataset?.Invoke(packet));
        HandleClientPacket<ServerPauseState>(packet => OnServerPauseState?.Invoke(packet));
        HandleClientPacket<SpeedrunnerEvents>(packet => OnSpeedrunnerEvents?.Invoke(packet));
        HandleClientPacket<SpeedrunnerEventsDelta>(packet => OnSpeedrunnerEventsDelta?.Invoke(packet));

        Instance = this;
    }

    protected override void OnEnable() { }

    protected override void OnDisable() { }

    private readonly Dictionary<ClientPacketId, Func<IPacketData>> packetGenerators = [];

    private IPacketData InstantiatePacket(ClientPacketId packetId) => packetGenerators.TryGetValue(packetId, out var gen) ? gen() : throw new ArgumentException($"Unknown id: {packetId}");

    private void HandleClientPacket<T>(Action<T> handler) where T : IIdentifiedPacket<ClientPacketId>, new()
    {
        var id = new T().Identifier;
        packetGenerators.Add(id, () => new T());
        receiver!.RegisterPacketHandler<T>(id, data => handler(data));
    }

    internal void SendMessage(string message) => api?.UiManager.ChatBox.AddMessage(message);

    internal void Send<T>(T packet) where T : IIdentifiedPacket<ServerPacketId>, new()
    {
        if (packet.Single) sender?.SendSingleData(packet.Identifier, packet);
        else sender?.SendCollectionData(packet.Identifier, packet);
    }

    // Add-on classes use events for communication to avoid direct communication with plugin classes.
    // This is necessary for the addons to work on dedicated servers, which do not have Silksong assemblies available.
    internal static event Action<HunterItemGrants>? OnHunterItemGrants;
    internal static event Action<HunterItemGrantsDelta>? OnHunterItemGrantsDelta;
    internal static event Action<ModuleDataset>? OnModuleDataset;
    internal static event Action<ServerPauseState>? OnServerPauseState;
    internal static event Action<SpeedrunnerEvents>? OnSpeedrunnerEvents;
    internal static event Action<SpeedrunnerEventsDelta>? OnSpeedrunnerEventsDelta;
}
