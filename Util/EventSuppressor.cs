using System;

namespace Silksong.TheHuntIsOn.Util;

internal class EventSuppressor
{
    private class Scoped : IDisposable
    {
        private readonly EventSuppressor parent;
        private readonly bool before;

        internal Scoped(EventSuppressor parent)
        {
            this.parent = parent;
            before = parent.Suppressed;

            parent.Suppressed = true;
        }

        public void Dispose() => parent.Suppressed = before;
    }

    public bool Suppressed { get; private set; } = false;

    public IDisposable Suppress() => new Scoped(this);
}
