using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.SsmpAddon.Packets;
using Silksong.TheHuntIsOn.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Silksong.TheHuntIsOn.Modules.PauseTimerModule;

internal class Countdown : NetworkedCloneable<Countdown>
{
    // Scheduled time for the countdown to expire.
    public long FinishTimeTicks;
    // If set, always show this amount as the remaining time.
    public Optional<long> FrozenRemainder = new();
    // If set, override FrozenRemainder and start counting down again after this time.
    public Optional<long> UnfreezeTimeTicks = new();
    // Message to accompany the countdown.
    public string Message = "<untitled>";

    public bool IsFrozen(DateTime now) => FrozenRemainder.HasValue && (!UnfreezeTimeTicks.Get(out var ticks) || ticks > now.Ticks);

    public bool IsCompleted(DateTime now) => !IsFrozen(now) && now.Ticks >= FinishTimeTicks;

    public bool GetDisplayTime(out float seconds)
    {
        var now = DateTime.UtcNow;
        if (IsFrozen(now) || !IsCompleted(now))
        {
            TimeSpan span = new(IsFrozen(now) ? FrozenRemainder.Value : (FinishTimeTicks - now.Ticks));
            seconds = (float)span.TotalSeconds;
            return true;
        }

        seconds = 0;
        return false;
    }

    public Countdown Pause(DateTime now)
    {
        if (IsCompleted(now)) return this;
        if (IsFrozen(now)) return With(c => c.UnfreezeTimeTicks.Clear());

        return With(c =>
        {
            c.FrozenRemainder = FinishTimeTicks - now.Ticks;
            c.UnfreezeTimeTicks.Clear();
        });
    }

    public Countdown UnpauseAt(DateTime now, DateTime unpauseWhen)
    {
        if (IsCompleted(now)) return this;
        if (IsFrozen(now)) return With(c => {
            c.FinishTimeTicks = unpauseWhen.Ticks + FrozenRemainder.Value;
            c.UnfreezeTimeTicks = unpauseWhen.Ticks;
        });

        var remainder = FinishTimeTicks - now.Ticks;
        return With(c =>
        {
            c.FinishTimeTicks = unpauseWhen.Ticks + remainder;
            c.FrozenRemainder = remainder;
            c.UnfreezeTimeTicks = unpauseWhen.Ticks;
        });
    }
}

internal class ServerPauseState : NetworkedCloneable<ServerPauseState>, IIdentifiedPacket<ClientPacketId>
{
    public const byte MAX_MESSAGE_LENGTH = byte.MaxValue;

    public const int MAX_COUNTDOWNS = 10;

    public ClientPacketId Identifier => ClientPacketId.ServerPauseState;

    public bool Single => true;

    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => true;

    // Active countdowns.
    public List<Countdown> Countdowns = [];
    // Whether the server is currently paused.
    public bool ServerPaused = false;
    // When the server should be unpaused, if currently paused.
    public long UnpauseTimeTicks = long.MaxValue;

    public void UpdateCountdowns(DateTime now) => Countdowns = [.. Countdowns.Where(c => !c.IsCompleted(now))];

    public bool IsServerPaused(out float? remainingSeconds)
    {
        remainingSeconds = null;
        if (!ServerPaused) return false;
        if (UnpauseTimeTicks == long.MaxValue) return true;

        var now = DateTime.UtcNow.Ticks;
        if (now >= UnpauseTimeTicks) return false;

        TimeSpan span = new(UnpauseTimeTicks - now);
        remainingSeconds = (float)span.TotalSeconds;
        return true;
    }
}
