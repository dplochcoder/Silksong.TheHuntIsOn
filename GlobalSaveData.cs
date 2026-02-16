using Newtonsoft.Json;
using Silksong.TheHuntIsOn.Modules.Lib;

namespace Silksong.TheHuntIsOn;

public record GlobalSaveData
{
    [JsonProperty] internal bool Enabled = false;
    [JsonProperty] internal RoleId Role = RoleId.Hunter;
    [JsonProperty] internal ModuleDataset ModuleDataset = [];
    [JsonProperty] internal CosmeticConfig Cosmetics = new();
}
