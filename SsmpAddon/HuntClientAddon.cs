using Silksong.TheHuntIsOn.Modules.ArchitectModule;
using Silksong.TheHuntIsOn.Modules.EventsModule;
using Silksong.TheHuntIsOn.Modules.Lib;
using Silksong.TheHuntIsOn.Modules.PauseTimerModule;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Api.Client;
using SSMP.Api.Client.Networking;
using System;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal class HuntClientAddon : TogglableClientAddon
{
    internal static HuntClientAddon? Instance { get; private set; }

    internal static bool IsConnected => Instance != null && !Instance.Disabled && (Instance.api?.NetClient.IsConnected ?? false);

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
        receiver = api.NetClient.GetNetworkReceiver<ClientPacketId>(this, packetGenerators.Instantiate);

        HandleClientPacket<ArchitectLevelData>();
        HandleClientPacket<ArchitectLevelsMetadata>();
        HandleClientPacket<HunterItemGrants>();
        HandleClientPacket<HunterItemGrantsDelta>();
        HandleClientPacket<ModuleDataset>();
        HandleClientPacket<ServerPauseState>();
        HandleClientPacket<SpeedrunnerEvents>();
        HandleClientPacket<SpeedrunnerEventsDelta>();

        Instance = this;
    }

    protected override void OnEnable() { }

    protected override void OnDisable() { }

    private readonly PacketGenerators<ClientPacketId> packetGenerators = new();

    // Add-on classes use events for communication to avoid direct communication with plugin classes.
    // This is necessary for the addons to work on dedicated servers, which do not have Silksong assemblies available.
    internal class On<T> where T : IIdentifiedPacket<ClientPacketId>
    {
        public static event Action<T>? Received;

        internal static void Invoke(T packet) => Received?.Invoke(packet);
    }

    private void HandleClientPacket<T>() where T : IIdentifiedPacket<ClientPacketId>, new()
    {
        packetGenerators.Register<T>();
        receiver!.RegisterPacketHandler<T>(new T().Identifier, On<T>.Invoke);
    }

    internal void SendMessage(string message) => api?.UiManager.ChatBox.AddMessage(message);

    internal void Send<T>(T packet) where T : IIdentifiedPacket<ServerPacketId>, new()
    {
        if (packet.Single) sender?.SendSingleData(packet.Identifier, packet);
        else sender?.SendCollectionData(packet.Identifier, packet);
    }
}
