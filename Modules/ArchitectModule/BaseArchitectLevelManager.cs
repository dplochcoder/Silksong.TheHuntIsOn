using Silksong.TheHuntIsOn.Util;
using System.IO;
using System.Reflection;

namespace Silksong.TheHuntIsOn.Modules.ArchitectModule;

internal abstract class BaseArchitectLevelManager(string diskFolderName)
{
    protected const string ARCHITECT_SUFFIX = ".architect.json";
    protected const string EMBEDDED_PREFIX = "Silksong.TheHuntIsOn.Resources.Data.Architect.";

    protected readonly string diskFolder = Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"Architect-{diskFolderName}");

    protected static ArchitectLevelsMetadata HashEmbedded()
    {
        ArchitectLevelsMetadata metadata = [];
        foreach (var name in Assembly.GetExecutingAssembly().GetManifestResourceNames())
        {
            if (!name.ConsumePrefix(EMBEDDED_PREFIX, out var suffix)) continue;
            if (!suffix.ConsumeSuffix(ARCHITECT_SUFFIX, out var path)) continue;
            if (!path.Split2('.', out var groupSpan, out var sceneSpan)) continue;

            string group = new(groupSpan);
            if (!metadata.TryGetValue(group, out var groupMetadata))
            {
                groupMetadata = [];
                metadata.Add(group, groupMetadata);
            }

            string scene = new(sceneSpan);
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            groupMetadata.Add(scene, SHA1Hash.Compute(stream));
        }
        return metadata;
    }

    protected string DiskGroupPath(string groupId) => Path.Join(diskFolder, groupId);

    protected string DiskLevelPath(string groupId, string sceneName) => Path.Join(diskFolder, groupId, $"{sceneName}{ARCHITECT_SUFFIX}");

    protected static Stream EmbeddedLevelStream(string groupId, string sceneName) => Assembly.GetExecutingAssembly().GetManifestResourceStream($"{EMBEDDED_PREFIX}.{groupId}.{sceneName}{ARCHITECT_SUFFIX}");
}
