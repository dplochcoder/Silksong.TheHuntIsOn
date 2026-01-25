using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Modules.Lib;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Menu;

internal abstract class ModuleSubMenu<T> : IModuleSubMenu where T : ModuleSettings<T>, new()
{
    public abstract IEnumerable<MenuElement> Elements();

    internal abstract void Apply(T data);

    public void ApplyRaw(ModuleSettings? data) => Apply(data is T typed ? typed : new());

    internal abstract T Export();

    public ModuleSettings ExportRaw() => Export();
}
