using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Silksong.TheHuntIsOn.Modules.EventsModule;

internal record EventRewards
{
    public EnumList<HunterItemGrantType> Items = [];
    public string Message = "";
}

// JSON format
internal class EventsData : Dictionary<string, EventRewards>
{
    public static EventsData Load()
    {
        var localPath = Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "events.json");
        return File.Exists(localPath) ? JsonUtil.DeserializeFromFile<EventsData>(localPath) : JsonUtil.DeserializeFromDataResource<EventsData>("default-events.json");
    }
}

internal class ParsedEventsData
{
    public readonly Dictionary<SpeedrunnerBoolEvent, EventRewards> BoolRewards = [];
    public readonly Dictionary<SpeedrunnerCountEvent, EventRewards> CountRewards = [];

    public void Parse(EventsData eventsData)
    {
        foreach (var e in eventsData)
        {
            if (Enum.TryParse(e.Key, out SpeedrunnerBoolEvent boolEvent)) BoolRewards.Add(boolEvent, e.Value);
            else if (SpeedrunnerCountEvent.TryParse(e.Key, out var countEvent)) CountRewards.Add(countEvent, e.Value);
            else throw new ArgumentException($"Unrecognized event key: '{e.Key}'");
        }
    }

    public static ParsedEventsData Load()
    {
        ParsedEventsData parsed = new();
        parsed.Parse(EventsData.Load());
        return parsed;
    }
}