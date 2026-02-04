using Silksong.TheHuntIsOn.SsmpAddon;
using System;
using System.Threading;

namespace Silksong.TheHuntIsOn.Modules.ArchitectModule;

internal class ArchitectModuleServerAddon
{
    private readonly HuntServerAddon serverAddon;

    private readonly ServerArchitectLevelManager levelManager = new();
    private readonly Util.ActionQueue actionQueue = new();

    private void Enqueue(Action action) => actionQueue.Enqueue(action);

    public ArchitectModuleServerAddon(HuntServerAddon serverAddon)
    {
        this.serverAddon = serverAddon;

        Thread t = new(actionQueue.Run);
        t.Start();

        serverAddon.OnUpdatePlayer += player => Enqueue(() => serverAddon.SendToPlayer(player, levelManager.GetLevelsMetadata()));
    }

    internal void OnRequestArchitectLevelData(ushort id, RequestArchitectLevelData request) => Enqueue(() =>
    {
        if (!levelManager.TryGetLevelData(request.ArchitectGroupId, request.SceneName, out var data, out var hash)) return;

        ArchitectLevelData response = new()
        {
            ArchitectGroupId = request.ArchitectGroupId,
            SceneName = request.SceneName,
            LevelData = data,
            LevelDataHash = hash,
        };
        serverAddon.SendToPlayer(id, response);
    });

    internal void Refresh() => Enqueue(() =>
    {
        levelManager.UpdateDiskMetadata();
        serverAddon.Broadcast(levelManager.GetLevelsMetadata());
    });
}
