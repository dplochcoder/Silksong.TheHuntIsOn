using Silksong.TheHuntIsOn.SsmpAddon;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Silksong.TheHuntIsOn.Util;

// Synchronized action runner.
internal class ActionQueue
{
    private readonly Queue<Action> actionQueue = [];

    internal void Enqueue(Action action)
    {
        lock (actionQueue)
        {
            actionQueue.Enqueue(action);
            Monitor.Pulse(actionQueue);
        }
    }

    private Action Dequeue()
    {
        lock (actionQueue)
        {
            while (actionQueue.Count == 0) Monitor.Wait(actionQueue);
            return actionQueue.Dequeue();
        }
    }

    internal void Run()
    {
        while (true)
        {
            Action action = Dequeue();

            try { action(); }
            catch (Exception ex)
            {
                HuntLogger.LogError($"{ex}");
            }
        }
    }
}
