namespace Silksong.TheHuntIsOn.Util;

internal class DeltaBaseWrapper<B, D> where B : IDeltaBase<B, D>, new() where D : IDelta<B, D>, new()
{
    private B prevData = new();
    private B currentData = new();

    internal bool Update(D delta, out D realDelta)
    {
        if (!currentData.Update(delta))
        {
            realDelta = new();
            return false;
        }

        realDelta = currentData.DeltaFrom(prevData);
        prevData.Update(realDelta);
        return true;
    }

    internal void Reset()
    {
        prevData = new();
        currentData = new();
    }

    internal B Value => currentData;
}
