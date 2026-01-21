using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Util;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Menu;

internal abstract class ModuleSubMenu<T> : IModuleSubMenu where T : Cloneable, new()
{
    public abstract IEnumerable<MenuElement> Elements();

    internal abstract void Apply(T data);

    public void ApplyRaw(object? data) => Apply(data is T typed ? typed : new());

    internal abstract T Export();

    public Cloneable ExportRaw() => Export();
}
