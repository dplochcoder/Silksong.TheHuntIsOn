using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Util;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Menu;

internal abstract class ModuleSubMenu<T> : IModuleSubMenu where T : NetworkedCloneable<T>, new()
{
    public abstract IEnumerable<MenuElement> Elements();

    internal abstract void Apply(T data);

    public void ApplyRaw(INetworkedCloneable? data) => Apply(data is T typed ? typed : new());

    internal abstract T Export();

    public INetworkedCloneable ExportRaw() => Export();
}
