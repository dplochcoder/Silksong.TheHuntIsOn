namespace Silksong.TheHuntIsOn.Util;

internal interface IDeltaBase<B, D> where B : IDeltaBase<B, D> where D : IDelta<B, D>
{
    bool Update(D delta);

    D DeltaFrom(B deltaBase);
}
