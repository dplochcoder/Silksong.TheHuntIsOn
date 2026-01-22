using Silksong.TheHuntIsOn.Modules.Lib;

namespace Silksong.TheHuntIsOn;

internal record GlobalSaveData
{
    internal bool Enabled = false;
    internal RoleId Role = RoleId.Hunter;
    internal ModuleDataset ModuleDataset = new();
}
