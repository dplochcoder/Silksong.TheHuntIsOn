using Silksong.TheHuntIsOn.Menu;
using Silksong.TheHuntIsOn.Util;

namespace Silksong.TheHuntIsOn.Modules.Lib;

internal abstract class GlobalSettingsModule<ModuleT, GlobalT, SubMenuT> : Module<ModuleT, GlobalT, SubMenuT, EmptySettings> where ModuleT : GlobalSettingsModule<ModuleT, GlobalT, SubMenuT> where GlobalT : NetworkedCloneable<GlobalT>, new() where SubMenuT : ModuleSubMenu<GlobalT>, new() { }
