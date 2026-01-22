using Silksong.TheHuntIsOn.Modules;
using SSMP.Api.Client;
using SSMP.Api.Client.Networking;
using SSMP.Networking.Packet;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal class HuntClientAddon : ClientAddon
{
    internal static HuntClientAddon? Instance { get; private set; }

    internal static bool IsConnected => Instance?.api?.NetClient.IsConnected ?? false;

    public override bool NeedsNetwork => true;

    public override uint ApiVersion => 1u;

    protected override string Name => "TheHuntIsOn";

    protected override string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();

    private IClientApi? api;
    private IClientAddonNetworkSender<ServerPacketId>? sender;
    private IClientAddonNetworkReceiver<ClientPacketId>? receiver;

    public override void Initialize(IClientApi clientApi)
    {
        api = clientApi;
        sender = api.NetClient.GetNetworkSender<ServerPacketId>(this);
        receiver = api.NetClient.GetNetworkReceiver<ClientPacketId>(this, InstantiatePacket);

        HandleClientPacket<ModuleDataset>(ClientPacketId.ModuleDataset, HandleModuleDataset);

        Instance = this;
    }

    private readonly Dictionary<ClientPacketId, Func<IPacketData>> packetGenerators = [];

    private IPacketData InstantiatePacket(ClientPacketId packetId) => packetGenerators.TryGetValue(packetId, out var gen) ? gen() : throw new ArgumentException($"Unknown id: {packetId}");

    private void HandleClientPacket<T>(ClientPacketId packetId, Action<T> handler) where T : IPacketData, new()
    {
        packetGenerators.Add(packetId, () => new T());
        receiver!.RegisterPacketHandler<T>(packetId, data => handler(data));
    }

    // Add-on classes use events for communication to avoid direct communication with plugin classes.
    // This is necessary for the addons to work on dedicated servers, which do not have Silksong assemblies available.
    internal static event Action<ModuleDataset>? OnModuleDatasetUpdate;

    private void HandleModuleDataset(ModuleDataset moduleDataset) => OnModuleDatasetUpdate?.Invoke(moduleDataset);

    internal void SendModuleDataset(ModuleDataset moduleDataset) => sender?.SendSingleData(ServerPacketId.ModuleDataset, moduleDataset);
}
