using Silksong.TheHuntIsOn.Modules.Lib;

namespace Silksong.TheHuntIsOn;

public record GlobalSaveData
{
    internal bool Enabled = false;
    internal RoleId Role = RoleId.Hunter;
    internal ModuleDataset ModuleDataset = [];
    internal CosmeticConfig Cosmetics = new();
}
