using Silksong.TheHuntIsOn.Util;
using System.IO;
using System.Text;

namespace Silksong.TheHuntIsOn.Modules.ArchitectModule;

internal class ServerArchitectLevelManager : BaseArchitectLevelManager
{
    private readonly ArchitectLevelsMetadata embeddedMetadata;
    private ArchitectLevelsMetadata diskMetadata;
    private ArchitectLevelsMetadata overlayMetadata;
    private readonly LRUCache<(string, string), (string, SHA1Hash)> levelDataCache;

    private ArchitectLevelsMetadata HashDisk()
    {
        ArchitectLevelsMetadata metadata = new();
        foreach (var groupDir in Directory.EnumerateDirectories(diskFolder))
        {
            ArchitectLevelMetadata groupMetadata = [];
            metadata.Add(Path.GetFileName(groupDir), groupMetadata);
            foreach (var scenePath in Directory.EnumerateFiles(groupDir))
            {
                var sceneFileName = Path.GetFileName(scenePath);
                if (!sceneFileName.ConsumeSuffix(ARCHITECT_SUFFIX, out var sceneSpan)) continue;

                string scene = new(sceneSpan);
                using var stream = File.OpenRead(sceneFileName);
                groupMetadata.Add(scene, SHA1Hash.Compute(stream));
            }
        }
        return metadata;
    }

    public ServerArchitectLevelManager() : base("Server")
    {
        embeddedMetadata = HashEmbedded();
        levelDataCache = new(100, LoadLevelData);
    }

    private bool LoadLevelData((string, string) key, out (string, SHA1Hash) value)
    {
        var (groupId, sceneName) = key;
        
        if (diskMetadata.TryGetValue(groupId, out var groupMetadata))
        {
            if (groupMetadata.TryGetValue(sceneName, out var hash))
            {
                value = (File.ReadAllText(DiskLevelPath(groupId, sceneName)), hash);
                return true;
            }
        }
        else if (embeddedMetadata.TryGet(groupId, sceneName, out var hash))
        {
            using var stream = EmbeddedLevelStream(groupId, sceneName);
            using StreamReader reader = new(stream, Encoding.UTF8);
            value = (reader.ReadToEnd(), hash);
            return true;
        }

        value = ("", new());
        return false;
    }

    internal void UpdateDiskMetadata()
    {
        diskMetadata = HashDisk();
        overlayMetadata = embeddedMetadata.Clone();
        foreach (var e in diskMetadata) overlayMetadata[e.Key] = e.Value;
    }

    internal ArchitectLevelsMetadata GetLevelsMetadata() => overlayMetadata;

    internal bool TryGetLevelData(string groupId, string sceneName, out string levelData, out SHA1Hash hash)
    {
        if (levelDataCache.TryGetValue((groupId, sceneName), out var pair))
        {
            levelData = pair.Item1;
            hash = pair.Item2;
            return true;
        }

        levelData = "";
        hash = new();
        return false;
    }
}
