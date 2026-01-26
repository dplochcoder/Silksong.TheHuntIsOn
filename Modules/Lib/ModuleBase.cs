using Silksong.ModMenu.Elements;
using Silksong.TheHuntIsOn.Menu;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Modules.Lib;

internal abstract class ModuleBase
{
    public abstract string Name { get; }

    public abstract ModuleActivationType ModuleActivationType { get; }

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

    public static IEnumerable<ModuleBase> GetAllModules() => [
        new ArchitectModule.ArchitectModule(),
        new BindModule(),
        new DeathModule(),
        new EventsModule.EventsModule(),
        new HealingModule(),
        new PauseTimerModule.PauseTimerModule(),
        new SpawnPointModule(),
        new StatsModule(),
    ];
}
