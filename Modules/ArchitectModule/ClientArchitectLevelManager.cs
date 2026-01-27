using Architect.Api;
using Architect.Placements;
using Architect.Storage;
using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Silksong.TheHuntIsOn.Modules.ArchitectModule;

internal class ClientArchitectLevelManager : BaseArchitectLevelManager
{
    private const string TOMBSTONE_SUFFIX = ".tombstone";

    private class DiskArchitectLevelMetadata
    {
        public readonly ArchitectLevelMetadata Metadata = [];
        public readonly HashSet<string> Tombstones = [];

        public bool Contains(string sceneName) => !Tombstones.Contains(sceneName) && Metadata.ContainsKey(sceneName);

        public void OverlayOnto(ArchitectLevelMetadata other)
        {
            foreach (var e in Metadata) other[e.Key] = e.Value;
            foreach (var tombstone in Tombstones) other.Remove(tombstone);
        }
    }

    private class DiskArchitectLevelsMetadata
    {
        public readonly Dictionary<string, DiskArchitectLevelMetadata> Metadata = [];
        public readonly HashSet<string> Tombstones = [];

        public bool Contains(string groupId, string sceneName) => !Tombstones.Contains(groupId) && Metadata.TryGetValue(groupId, out var groupMetadata) && groupMetadata.Contains(sceneName);

        public bool ContainsTombstone(string groupId, string sceneName) => !Tombstones.Contains(groupId) && Metadata.TryGetValue(groupId, out var groupMetadata) && groupMetadata.Tombstones.Contains(sceneName);

        public void OverlayOnto(ArchitectLevelsMetadata other)
        {
            foreach (var e in Metadata)
            {
                if (other.TryGetValue(e.Key, out var existing)) e.Value.OverlayOnto(existing);
                else other[e.Key] = e.Value.Metadata;
            }
            foreach (var tombstone in Tombstones) other.Remove(tombstone);
        }
    }

    private readonly ArchitectLevelsMetadata embeddedMetadata;
    private readonly DiskArchitectLevelsMetadata diskMetadata;

    private DiskArchitectLevelsMetadata HashDisk()
    {
        DiskArchitectLevelsMetadata metadata = new();
        if (!Directory.Exists(diskFolder)) return metadata;

        foreach (var groupDir in Directory.EnumerateDirectories(diskFolder))
        {
            DiskArchitectLevelMetadata groupMetadata = new();
            metadata.Metadata.Add(Path.GetFileName(groupDir), groupMetadata);
            foreach (var scenePath in Directory.EnumerateFiles(groupDir))
            {
                var sceneFileName = Path.GetFileName(scenePath);
                if (sceneFileName.ConsumeSuffix(TOMBSTONE_SUFFIX, out var sceneSpan))
                {
                    groupMetadata.Tombstones.Add(new(sceneSpan));
                    continue;
                }
                if (!sceneFileName.ConsumeSuffix(ARCHITECT_SUFFIX, out sceneSpan)) continue;

                string scene = new(sceneSpan);
                using var stream = File.OpenRead(sceneFileName);
                groupMetadata.Metadata.Add(scene, SHA1Hash.Compute(stream));
            }
        }
        foreach (var groupFile in Directory.EnumerateFiles(diskFolder))
        {
            if (!Path.GetFileName(groupFile).ConsumeSuffix(TOMBSTONE_SUFFIX, out var groupSpan)) continue;
            metadata.Tombstones.Add(new(groupSpan));
        }
        return metadata;
    }

    private readonly Func<IEnumerable<string>> getEnabledGroups;
    private readonly LRUCache<(string, string), LevelData> levelDataCache;

    public ClientArchitectLevelManager(Func<IEnumerable<string>> getEnabledGroups) : base("Client")
    {
        embeddedMetadata = HashEmbedded();
        diskMetadata = HashDisk();

        Reduce();

        HuntClientAddon.On<ArchitectLevelsMetadata>.Received += OnArchitectLevelsMetadata;
        HuntClientAddon.On<ArchitectLevelData>.Received += OnArchitectLevelData;

        this.getEnabledGroups = getEnabledGroups;
        levelDataCache = new(10, LoadLevelData);
        MapLoader.AddMapLoader(InjectLevelData);
    }

    private bool LoadLevelData((string, string) key, out LevelData levelData)
    {
        var groupId = key.Item1;
        var sceneName = key.Item2;

        if (diskMetadata.Tombstones.Contains(groupId) || diskMetadata.ContainsTombstone(groupId, sceneName))
        {
            levelData = new([], [], []);
            return false;
        }

        if (diskMetadata.Contains(groupId, sceneName))
        {
            var data = File.ReadAllText(DiskLevelPath(groupId, sceneName));
            levelData = StorageManager.DeserializeLevel(data);
            return true;
        }

        if (embeddedMetadata.Contains(groupId, sceneName))
        {
            using var stream = EmbeddedLevelStream(groupId, sceneName);
            using StreamReader reader = new(stream, System.Text.Encoding.UTF8);
            levelData = StorageManager.DeserializeLevel(reader.ReadToEnd());
            return true;
        }

        levelData = new([], [], []);
        return false;
    }

    protected LevelData InjectLevelData(string sceneName)
    {
        LevelData levelData = new([], [], []);
        foreach (var group in getEnabledGroups())
        {
            if (!levelDataCache.TryGetValue((group, sceneName), out var data)) continue;
            levelData.Merge(data);
        }
        return levelData;
    }

    private string GroupTombstonePath(string groupId) => Path.Join(diskFolder, $"{groupId}{TOMBSTONE_SUFFIX}");

    private string LevelTombstonePath(string groupId, string sceneName) => Path.Join(diskFolder, groupId, $"{sceneName}{TOMBSTONE_SUFFIX}");

    private void FixTombstones()
    {
        List<string> tombstones = [.. diskMetadata.Tombstones];
        foreach (var groupId in tombstones)
        {
            if (diskMetadata.Metadata.ContainsKey(groupId))
            {
                Directory.Delete(DiskGroupPath(groupId), true);
                diskMetadata.Metadata.Remove(groupId);
            }
            if (!embeddedMetadata.ContainsKey(groupId))
            {
                File.Delete(GroupTombstonePath(groupId));
                diskMetadata.Tombstones.Remove(groupId);
            }
        }

        foreach (var e in diskMetadata.Metadata)
        {
            var groupId = e.Key;
            tombstones = [.. e.Value.Tombstones];
            foreach (var sceneName in e.Value.Tombstones)
            {
                if (e.Value.Metadata.ContainsKey(sceneName))
                {
                    File.Delete(DiskLevelPath(groupId, sceneName));
                    e.Value.Metadata.Remove(sceneName);
                }
                if (!embeddedMetadata.Contains(groupId, sceneName))
                {
                    File.Delete(LevelTombstonePath(groupId, sceneName));
                    e.Value.Tombstones.Remove(sceneName);
                }
            }
        }
    }

    private void RemoveDupes()
    {
        List<string> groupsToRemove = [];
        foreach (var e1 in diskMetadata.Metadata)
        {
            var groupId = e1.Key;
            List<string> scenesToRemove = [];
            foreach (var e2 in e1.Value.Metadata)
            {
                var sceneName = e2.Key;
                if (embeddedMetadata.TryGet(groupId, sceneName, out var hash) && e2.Value == hash)
                    scenesToRemove.Add(sceneName);
            }
            foreach (var sceneName in scenesToRemove)
            {
                File.Delete(DiskLevelPath(groupId, sceneName));
                e1.Value.Metadata.Remove(sceneName);
            }

            if (e1.Value.Tombstones.Count == 0 && e1.Value.Metadata.Count == 0) groupsToRemove.Add(groupId);
        }
        foreach (var groupId in groupsToRemove)
        {
            Directory.Delete(DiskGroupPath(groupId), true);
            diskMetadata.Metadata.Remove(groupId);
        }
    }

    private void Reduce()
    {
        FixTombstones();
        RemoveDupes();
    }

    public ArchitectLevelsMetadata ComputeMetadata()
    {
        ArchitectLevelsMetadata metadata = embeddedMetadata.Clone();
        diskMetadata.OverlayOnto(metadata);
        return metadata;
    }

    private DiskArchitectLevelMetadata EnsureDiskGroup(string groupId)
    {
        if (diskMetadata.Tombstones.Contains(groupId))
        {
            File.Delete(GroupTombstonePath(groupId));
            diskMetadata.Tombstones.Remove(groupId);
        }

        if (!diskMetadata.Metadata.TryGetValue(groupId, out var groupMetadata))
        {
            if (!Directory.Exists(diskFolder)) Directory.CreateDirectory(diskFolder);

            Directory.CreateDirectory(DiskGroupPath(groupId));
            groupMetadata = new();
            diskMetadata.Metadata.Add(groupId, groupMetadata);
        }
        return groupMetadata;
    }

    private void DeleteGroup(string groupId)
    {
        // Delete the group if it exists.
        if (diskMetadata.Metadata.ContainsKey(groupId))
        {
            Directory.Delete(DiskGroupPath(groupId), true);
            diskMetadata.Metadata.Remove(groupId);
        }

        // Install a tombstone if needed.
        if (embeddedMetadata.ContainsKey(groupId) && diskMetadata.Tombstones.Add(groupId))
            File.Create(GroupTombstonePath(groupId));

        levelDataCache.Clear();
    }

    public void OnArchitectLevelData(ArchitectLevelData update)
    {
        var metadata = EnsureDiskGroup(update.ArchitectGroupId);

        var levelPath = DiskLevelPath(update.ArchitectGroupId, update.SceneName);
        if (File.Exists(levelPath)) File.Delete(levelPath);
        File.WriteAllText(levelPath, update.LevelData);
        metadata.Metadata[update.SceneName] = update.LevelDataHash;

        levelDataCache.Evict((update.ArchitectGroupId, update.SceneName));
    }

    private void UpdateLevel(string groupId, string sceneName, SHA1Hash levelHash)
    {
        if (embeddedMetadata.TryGet(groupId, sceneName, out var hash) && hash.Equals(levelHash))
            RemoveDiskLevel(groupId, sceneName);
        else
            HuntClientAddon.Instance?.Send(new RequestArchitectLevelData()
            {
                ArchitectGroupId = groupId,
                SceneName = sceneName,
            });
    }

    private void MakeTombstone(string groupId, string sceneName)
    {
        // Delete the level if it exists.
        if (diskMetadata.Contains(groupId, sceneName))
        {
            File.Delete(DiskLevelPath(groupId, sceneName));
            diskMetadata.Metadata[groupId].Metadata.Remove(sceneName);
        }
        // Install a tombstone if needed.
        if (embeddedMetadata.Contains(groupId, sceneName) && !diskMetadata.ContainsTombstone(groupId, sceneName))
        {
            EnsureDiskGroup(groupId).Tombstones.Add(sceneName);
            File.Create(LevelTombstonePath(groupId, sceneName));
        }

        levelDataCache.Evict((groupId, sceneName));
    }

    private void RemoveDiskLevel(string groupId, string sceneName)
    {
        if (!diskMetadata.Contains(groupId, sceneName)) return;

        File.Delete(DiskLevelPath(groupId, sceneName));
        diskMetadata.Metadata[groupId].Metadata.Remove(sceneName);
        levelDataCache.Evict((groupId, sceneName));
    }

    private void DeleteLevel(string groupId, string sceneName)
    {
        if (embeddedMetadata.Contains(groupId, sceneName))
            MakeTombstone(groupId, sceneName);
        else
            RemoveDiskLevel(groupId, sceneName);
    }

    public void OnArchitectLevelsMetadata(ArchitectLevelsMetadata metadata) => ComputeMetadata().Diff(
        metadata,
        deleteGroup: DeleteGroup,
        updateLevel: UpdateLevel,
        deleteLevel: DeleteLevel);

    public IEnumerable<string> GetAllGroups()
    {
        HashSet<string> set = [.. embeddedMetadata.Keys.Concat(diskMetadata.Metadata.Keys).Distinct()];
        foreach (var groupId in diskMetadata.Tombstones) set.Remove(groupId);
        return [.. set.OrderBy(n => n)];
    }
}
