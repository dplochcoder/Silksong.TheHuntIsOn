namespace Silksong.TheHuntIsOn.Util;

internal interface IDelta<B, D> where B : IDeltaBase<B, D> where D : IDelta<B, D>
{
    bool IsEmpty { get; }
}
