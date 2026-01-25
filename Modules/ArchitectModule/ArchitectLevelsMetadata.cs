using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules.ArchitectModule;

// Map of group names to local metadata.
internal class ArchitectLevelsMetadata : Dictionary<string, ArchitectLevelMetadata>, INetworkedCloneable<ArchitectLevelsMetadata>, IIdentifiedPacket<ClientPacketId>
{
    public ArchitectLevelsMetadata() { }
    public ArchitectLevelsMetadata(IReadOnlyDictionary<string, ArchitectLevelMetadata> dict) : base(dict) { }

    ClientPacketId IIdentifiedPacket<ClientPacketId>.Identifier => ClientPacketId.ArchitectLevelsMetadata;

    public bool Single => true;

    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => true;

    public bool Contains(string groupId, string sceneName) => TryGetValue(groupId, out var metadata) && metadata.ContainsKey(sceneName);

    public bool TryGet(string groupId, string sceneName, out SHA1Hash hash)
    {
        if (TryGetValue(groupId, out var metadata)) return metadata.TryGetValue(sceneName, out hash);

        hash = new();
        return false;
    }

    public void ReadData(IPacket packet) => this.ReadData(packet, packet => packet.ReadString());

    public void WriteData(IPacket packet) => this.WriteData(packet, (packet, value) => value.WriteData(packet));

    public ArchitectLevelsMetadata Clone() => new(this.CloneDictDeep());

    public Util.ICloneable CloneRaw() => Clone();

    INetworkedCloneable INetworkedCloneable.CloneRaw() => Clone();

    public void Diff(ArchitectLevelsMetadata target,
        Action<string> deleteGroup,
        Action<string, string, SHA1Hash> updateLevel,
        Action<string, string> deleteLevel) => CollectionUtil.Compare(this, target, (groupId, myGroup, targetGroup) =>
        {
            if (targetGroup != null) (myGroup ?? []).Diff(targetGroup, (sceneName, hash) => updateLevel(groupId, sceneName, hash), sceneName => deleteLevel(groupId, sceneName));
            else if (myGroup != null) deleteGroup(groupId);
        });
}
