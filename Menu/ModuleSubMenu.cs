using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Util;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Menu;

internal abstract class ModuleSubMenu<T> : IModuleSubMenu where T : NetworkedCloneable, new()
{
    public abstract IEnumerable<MenuElement> Elements();

    internal abstract void Apply(T data);

    public void ApplyRaw(NetworkedCloneable? data) => Apply(data is T typed ? typed : new());

    internal abstract T Export();

    public NetworkedCloneable ExportRaw() => Export();
}
