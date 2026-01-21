using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;
using Silksong.ModMenu.Screens;
using Silksong.TheHuntIsOn.Modules;
using Silksong.TheHuntIsOn.Util;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Menu;

internal class ModuleMultiMenu
{
    public readonly MenuElement RootElement;

    private readonly ChoiceElement<ModuleActivation> ModuleActivation;

    private readonly IModuleSubMenu mainMenu;
    private readonly List<MenuElement> mainElements = [];
    private readonly IModuleSubMenu? speedrunnersSubMenu;
    private readonly TextButton? speedrunnersSubMenuButton;
    private readonly IModuleSubMenu? huntersSubMenu;
    private readonly TextButton? huntersSubMenuButton;

    private NetworkedCloneable? speedrunnersMenuData;
    private NetworkedCloneable? huntersMenuData;
    private NetworkedCloneable? everyoneMenuData;

    internal ModuleMultiMenu(ModuleBase module)
    {
        mainMenu = module.CreateSubMenu();
        UpdateData(mainMenu);
        mainElements.AddRange(mainMenu.Elements());
        if (mainElements.Count == 0)
        {
            ModuleActivation = new($"{module.Name} Module", ChoiceModels.ForEnum<ModuleActivation>(), "Which teams this module should be enabled for.");
            RootElement = ModuleActivation;
        }
        else
        {
            SimpleMenuScreen subScreen = new($"{module.Name} Module");
            ModuleActivation = new("Activation", ChoiceModels.ForEnum<ModuleActivation>(), "Which teams this module should be enabled for.");
            subScreen.Add(ModuleActivation);
            subScreen.AddRange(mainElements);

            speedrunnersSubMenu = module.CreateSubMenu();
            UpdateData(speedrunnersSubMenu);
            speedrunnersSubMenuButton = CreateRoleSpecificMenuButton(module.Name, "Speedrunner", speedrunnersSubMenu);
            subScreen.Add(speedrunnersSubMenuButton);

            huntersSubMenu = module.CreateSubMenu();
            UpdateData(huntersSubMenu);
            huntersSubMenuButton = CreateRoleSpecificMenuButton(module.Name, "Hunter", huntersSubMenu);
            subScreen.Add(huntersSubMenuButton);

            TextButton mainText = new($"{module.Name} Module");
            mainText.OnSubmit += () => MenuScreenNavigation.Show(subScreen);
            RootElement = mainText;
        }

        ModuleActivation.OnValueChanged += UpdateModuleActivation;
    }

    private void UpdateData(IModuleSubMenu menu)
    {
        foreach (var element in menu.Elements())
        {
            if (element is not BaseSelectableValueElement selectable) continue;
            selectable.RawModel.OnRawValueChanged += _ => UpdateData();
        }
    }

    private readonly EventSuppressor updateData = new();

    private void UpdateData()
    {
        if (updateData.Suppressed) return;

        switch (ModuleActivation.Value)
        {
            case Modules.ModuleActivation.SpeedrunnerOnly:
                speedrunnersMenuData = mainMenu.ExportRaw();
                break;
            case Modules.ModuleActivation.HuntersOnly:
                huntersMenuData = mainMenu.ExportRaw();
                break;
            case Modules.ModuleActivation.EveryoneSame:
                everyoneMenuData = mainMenu.ExportRaw();
                break;
            case Modules.ModuleActivation.EveryoneDifferent:
                speedrunnersMenuData = speedrunnersSubMenu?.ExportRaw();
                huntersMenuData = huntersSubMenu?.ExportRaw();
                break;
        }
    }

    private void UpdateModuleActivation(ModuleActivation value)
    {
        bool showMain = value != Modules.ModuleActivation.Inactive && value != Modules.ModuleActivation.EveryoneDifferent;
        foreach (var element in mainElements) element.VisibleSelf = showMain;

        bool showSubMenus = value == Modules.ModuleActivation.EveryoneDifferent;
        speedrunnersSubMenuButton?.VisibleSelf = showSubMenus;
        huntersSubMenuButton?.VisibleSelf = showSubMenus;

        using (updateData.Suppress())
        {
            speedrunnersSubMenu?.ApplyRaw(speedrunnersMenuData);
            huntersSubMenu?.ApplyRaw(huntersMenuData);
            switch (value)
            {
                case Modules.ModuleActivation.SpeedrunnerOnly:
                    mainMenu.ApplyRaw(speedrunnersMenuData);
                    break;
                case Modules.ModuleActivation.HuntersOnly:
                    mainMenu.ApplyRaw(huntersMenuData);
                    break;
                default:
                    mainMenu.ApplyRaw(everyoneMenuData);
                    break;
            }
        }
    }

    internal void Apply(ModuleData data)
    {
        everyoneMenuData = data.EveryoneSettings;
        speedrunnersMenuData = data.SpeedrunnerSettings;
        huntersMenuData = data.HunterSettings;
        ModuleActivation.Value = data.ModuleActivation;
        UpdateModuleActivation(ModuleActivation.Value);
    }

    internal ModuleData Export() => new(ModuleActivation.Value, speedrunnersMenuData, huntersMenuData, everyoneMenuData);

    private static TextButton CreateRoleSpecificMenuButton(string moduleName, string teamName, IModuleSubMenu subMenu)
    {
        TextButton button = new($"{teamName} Settings");
        SimpleMenuScreen screen = new($"{moduleName} {teamName} Settings");
        screen.AddRange(subMenu.Elements());
        button.OnSubmit += () => MenuScreenNavigation.Show(screen);

        return button;
    }
}
