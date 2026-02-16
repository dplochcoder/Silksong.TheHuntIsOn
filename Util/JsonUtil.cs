using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.IO;

namespace Silksong.TheHuntIsOn.Util;

internal class JsonUtil
{
    internal static JsonSerializer Serializer()
    {
        JsonSerializer serializer = new()
        {
            DefaultValueHandling = DefaultValueHandling.Include,
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto,
        };
        serializer.Converters.Add(new StringEnumConverter());
        return serializer;
    }

    internal static T DeserializeFromFile<T>(string path)
    {
        using var fileReader = File.OpenText(path);
        using JsonTextReader jsonReader = new(fileReader);
        return Serializer().Deserialize<T>(jsonReader) ?? throw new ArgumentNullException($"{nameof(path)}: {path}");
    }

    internal static T DeserializeFromDataResource<T>(string path)
    {
        using var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream($"Silksong.TheHuntIsOn.Resources.Data.{path}");
        using StreamReader streamReader = new(stream);
        using JsonTextReader jsonReader = new(streamReader);
        return Serializer().Deserialize<T>(jsonReader) ?? throw new ArgumentNullException($"{nameof(path)}: {path}");
    }
}
