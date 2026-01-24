using System;

namespace Silksong.TheHuntIsOn.Util;

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
