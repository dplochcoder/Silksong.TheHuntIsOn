using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Util;

namespace Silksong.TheHuntIsOn.Modules.Lib;

internal abstract class GlobalSettingsModule<ModuleT, GlobalT, SubMenuT> : Module<ModuleT, GlobalT, SubMenuT, Empty> where ModuleT : GlobalSettingsModule<ModuleT, GlobalT, SubMenuT> where GlobalT : ModuleSettings<GlobalT>, new() where SubMenuT : ModuleSubMenu<GlobalT>, new() { }
