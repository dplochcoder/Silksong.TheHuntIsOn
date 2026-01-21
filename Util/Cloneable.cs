namespace Silksong.TheHuntIsOn.Util;

internal abstract class Cloneable
{
   internal virtual Cloneable Clone() => (Cloneable)MemberwiseClone();
}

internal abstract class Cloneable<T> : Cloneable where T : Cloneable<T>
{
    internal virtual T CloneTyped() => (T)Clone();
}
