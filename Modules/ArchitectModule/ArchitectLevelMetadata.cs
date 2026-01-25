using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules.ArchitectModule;

// Map of scene names to LevelData hashes.
internal class ArchitectLevelMetadata : Dictionary<string, SHA1Hash>, INetworkedCloneable<ArchitectLevelMetadata>
{
    public ArchitectLevelMetadata() { }
    public ArchitectLevelMetadata(IReadOnlyDictionary<string, SHA1Hash> dict) : base(dict) { }

    public void ReadData(IPacket packet) => this.ReadData(packet, packet => packet.ReadString());

    public void WriteData(IPacket packet) => this.WriteData(packet, (packet, value) => value.WriteData(packet));

    public ArchitectLevelMetadata Clone() => new(this.CloneDictDeep());

    public INetworkedCloneable CloneRaw() => Clone();

    Util.ICloneable Util.ICloneable.CloneRaw() => Clone();

    public void Diff(ArchitectLevelMetadata target,
        Action<string, SHA1Hash> updateLevel,
        Action<string> deleteLevel) => CollectionUtil.Compare(this, target, (sceneName, myHash, targetHash) =>
        {
            if (targetHash != null)
            {
                if (myHash == null || !myHash.Equals(targetHash)) updateLevel(sceneName, targetHash);
            }
            else if (myHash != null) deleteLevel(sceneName);
        });
}
