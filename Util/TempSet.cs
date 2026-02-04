using Silksong.PurenailUtil.Collections;
using System;

namespace Silksong.TheHuntIsOn.Util;

internal class TempSet<T>
{
    private readonly HashMultiset<T> set = [];

    public bool Contains(T item) => set.Contains(item);

    public IDisposable TempAdd(T item) => new Lease(this, item);

    private class Lease : IDisposable
    {
        private readonly TempSet<T> parent;
        private readonly T item;

        internal Lease(TempSet<T> parent, T item) {
            this.parent = parent;
            this.item = item;

            parent.set.Add(item);
        }

        public void Dispose() => parent.set.Remove(item);
    }
}
