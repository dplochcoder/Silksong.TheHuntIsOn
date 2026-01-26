using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.Util;

namespace Silksong.TheHuntIsOn.Modules.EventsModule;

internal class EventsModuleServerAddon
{
    private readonly HuntServerAddon serverAddon;
    private ParsedEventsData parsedEventsData = ParsedEventsData.Load();

    private readonly DeltaBaseWrapper<SpeedrunnerEvents, SpeedrunnerEventsDelta> speedrunnerEvents = new();
    private readonly HunterItemGrants hunterItemGrants = new();

    internal EventsModuleServerAddon(HuntServerAddon serverAddon)
    {
        this.serverAddon = serverAddon;

        serverAddon.OnUpdatePlayer += player =>
        {
            serverAddon.SendToPlayer(player, speedrunnerEvents.Value);
            serverAddon.SendToPlayer(player, hunterItemGrants);
        };
        serverAddon.OnGameReset += OnGameReset;
    }

    private void OnGameReset()
    {
        speedrunnerEvents.Reset();
        hunterItemGrants.Clear();
        serverAddon.Broadcast(speedrunnerEvents.Value);
        serverAddon.Broadcast(hunterItemGrants);
    }

    internal void Refresh()
    {
        parsedEventsData = ParsedEventsData.Load();
        OnGameReset();
    }

#pragma warning disable IDE0060 // Remove unused parameter
    internal void OnSpeedrunnerEventsDelta(ushort id, SpeedrunnerEventsDelta eventsDelta)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        if (!speedrunnerEvents.Update(eventsDelta, out var realEventsDelta)) return;

        HunterItemGrantsDelta grantsDelta = new();
        void AddRewards(string id, EventRewards rewards)
        {
            grantsDelta.Grants.Add(id, rewards.Items);
            if (rewards.Message != "") serverAddon.BroadcastMessage(rewards.Message);
        }

        foreach (var boolEvent in realEventsDelta.BoolEvents)
        {
            if (!parsedEventsData.BoolRewards.TryGetValue(boolEvent, out var rewards)) continue;
            AddRewards($"{boolEvent}", rewards);
        }
        foreach (var countEvent in realEventsDelta.CountEvents)
        {
            for (int i = countEvent.PrevCount + 1; i <= countEvent.NewCount; i++)
            {
                if (!parsedEventsData.CountRewards.TryGetValue(new(countEvent.Type, i), out var rewards)) continue;
                AddRewards($"{countEvent.Type}{i}", rewards);
            }
        }

        serverAddon.Broadcast(realEventsDelta);
        if (!grantsDelta.IsEmpty)
        {
            hunterItemGrants.Update(grantsDelta);
            serverAddon.Broadcast(grantsDelta);
        }
    }
}
