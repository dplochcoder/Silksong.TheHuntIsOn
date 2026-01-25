using System;
using System.Collections.Generic;

namespace Silksong.TheHuntIsOn.Util;

// TODO: Use system.
internal interface ICloneable
{
    ICloneable CloneRaw();
}

internal interface ICloneable<T> : ICloneable where T : ICloneable<T>
{
    T Clone();
}

internal abstract class Cloneable<T> : ICloneable<T> where T : Cloneable<T>
{
    public ICloneable CloneRaw() => Clone();

    public virtual T Clone() => (T)MemberwiseClone();

    public T With(Action<T> edit)
    {
        T clone = Clone();
        edit(clone);
        return clone;
    }
}

internal static class ICloneableExtensions
{
    internal static Dictionary<K, V> CloneDictDeep<K, V>(this IReadOnlyDictionary<K, V> self) where V : ICloneable<V>
    {
        Dictionary<K, V> dict = [];
        foreach (var e in self) dict.Add(e.Key, e.Value.Clone());
        return dict;
    }

    internal static Dictionary<K, V> CloneDictShallow<K, V>(this IReadOnlyDictionary<K, V> self)
    {
        Dictionary<K, V> dict = [];
        foreach (var e in self) dict.Add(e.Key, e.Value);
        return dict;
    }
}