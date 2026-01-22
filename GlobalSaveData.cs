using Silksong.TheHuntIsOn.Modules;

namespace Silksong.TheHuntIsOn;

internal record GlobalSaveData
{
    internal bool Enabled = false;
    internal RoleId Role = RoleId.Hunter;
    internal ModuleDataset ModuleDataset = new();
}
