using Silksong.TheHuntIsOn.Modules.ArchitectModule;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using Silksong.TheHuntIsOn.Util;
using SSMP.Networking.Packet;
using System;

namespace Silksong.TheHuntIsOn.Modules.Lib;

internal enum ModuleSettingsType
{
    Empty,
    Architect,
    Bind,
    Death,
    Healing,
    SpawnPoint,
    Stats,
}

internal abstract class ModuleSettings : Cloneable<ModuleSettings>, IDynamicValue<ModuleSettingsType, ModuleSettings, ModuleSettingsFactory>
{
    public abstract ModuleSettingsType DynamicType { get; }

    public abstract void ReadDynamicData(IPacket packet);

    public abstract void WriteDynamicData(IPacket packet);

    public abstract bool Equivalent(ModuleSettings other);
}

internal abstract class ModuleSettings<T> : ModuleSettings, ICloneable<T> where T : ModuleSettings<T>
{
    T ICloneable<T>.Clone() => (T)((ModuleSettings)this).Clone();

    public override bool Equivalent(ModuleSettings other) => other is T typed && Equivalent(typed);

    protected abstract bool Equivalent(T other);
}

internal class ModuleSettingsFactory : IDynamicValueFactory<ModuleSettingsType, ModuleSettings, ModuleSettingsFactory>
{
    public ModuleSettings Create(ModuleSettingsType type) => type switch
    {
        ModuleSettingsType.Empty => new EmptySettings(),
        ModuleSettingsType.Architect => new ArchitectSettings(),
        ModuleSettingsType.Bind => new BindSettings(),
        ModuleSettingsType.Death => new DeathSettings(),
        ModuleSettingsType.Healing => new HealingSettings(),
        ModuleSettingsType.SpawnPoint => new SpawnPointSettings(),
        ModuleSettingsType.Stats => new StatsSettings(),
        _ => throw new ArgumentException($"{nameof(type)}: {type}"),
    };
}
