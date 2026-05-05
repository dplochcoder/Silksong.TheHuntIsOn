using System;
using System.Collections.Generic;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Generator;
using Silksong.TheHuntIsOn.Modules.Lib;

namespace Silksong.TheHuntIsOn.Menu;

public class EmptySettingsMenu : ICustomMenu<EmptySettings>
{
#pragma warning disable 0067
    public event Action<CustomMenuValueChangedEvent>? OnValueChanged;
#pragma warning restore 0067

    public void ApplyFrom(EmptySettings data) { }

    public void ExportTo(EmptySettings data) { }

    public IEnumerable<MenuElement> Elements() => [];
}
