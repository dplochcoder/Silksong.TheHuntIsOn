using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Menu;
using System.Collections.Generic;
using System.Reflection;

namespace Silksong.TheHuntIsOn.Modules.Lib;

internal abstract class ModuleBase
{
    public abstract string Name { get; }

    public bool Enabled
    {
        get => field;
        set
        {
            if (field == value) return;
            field = value;

            if (value) OnEnabled();
            else OnDisabled();
        }
    }

    public abstract IModuleSubMenu CreateGlobalDataSubMenu();

    public virtual IEnumerable<MenuElement> CreateCosmeticsMenuElements() => [];

    public virtual void OnEnabled() { }

    public virtual void OnDisabled() { }

    public virtual void OnGlobalConfigUpdated() { }

    public virtual void OnLocalConfigUpdated() { }

    public static IEnumerable<ModuleBase> GetAllModulesInAssembly()
    {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (type.IsAbstract || !type.IsSubclassOf(typeof(ModuleBase))) continue;
            yield return (ModuleBase)type.GetConstructor([]).Invoke([]);
        }
    }
}
