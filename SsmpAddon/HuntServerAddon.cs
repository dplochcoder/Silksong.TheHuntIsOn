using Silksong.TheHuntIsOn.Modules;
using SSMP.Api.Server;
using SSMP.Api.Server.Networking;
using SSMP.Networking.Packet;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal class HuntServerAddon : ServerAddon
{
    public override bool NeedsNetwork => true;

    public override uint ApiVersion => 1u;

    protected override string Name => "TheHuntIsOn";

    protected override string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();

    private IServerApi? api;
    private IServerAddonNetworkSender<ClientPacketId>? sender;
    private IServerAddonNetworkReceiver<ServerPacketId>? receiver;

    public override void Initialize(IServerApi serverApi)
    {
        api = serverApi;
        sender = api.NetServer.GetNetworkSender<ClientPacketId>(this);
        receiver = api.NetServer.GetNetworkReceiver<ServerPacketId>(this, InstantiatePacket);

        HandleServerPacket<ModuleDataset>(ServerPacketId.ModuleDataset, HandleModuleDataset);
    }

    private readonly Dictionary<ServerPacketId, Func<IPacketData>> packetGenerators = [];

    private IPacketData InstantiatePacket(ServerPacketId packetId) => packetGenerators.TryGetValue(packetId, out var gen) ? gen() : throw new ArgumentException($"Unknown id: {packetId}");

    private void HandleServerPacket<T>(ServerPacketId packetId, Action<ushort, T> handler) where T : IPacketData, new()
    {
        packetGenerators.Add(packetId, () => new T());
        receiver!.RegisterPacketHandler<T>(packetId, (id, data) => handler(id, data));
    }

    private bool IsPlayerAuthorized(ushort id) => api?.ServerManager.GetPlayer(id)?.IsAuthorized ?? false;

    private string PlayerName(ushort id) => api?.ServerManager.GetPlayer(id)?.Username ?? "<unknown>";

    private void HandleModuleDataset(ushort id, ModuleDataset moduleDataset)
    {
        if (!IsPlayerAuthorized(id))
        {
            api?.ServerManager.SendMessage(id, "Only authorized users can change module settings.");
            return;
        }

        sender?.BroadcastSingleData(ClientPacketId.ModuleDataset, moduleDataset);
        api?.ServerManager.BroadcastMessage($"{PlayerName(id)} updated module settings.");
    }
}
