using Silksong.TheHuntIsOn.Modules.EventsModule;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.SsmpAddon;

internal record EventRewards
{
    public List<HunterItemGrantType> Items = [];
    public string Message = "";
}

internal record EventsData
{
    public Dictionary<string, EventRewards> HunterRewards = [];
}
