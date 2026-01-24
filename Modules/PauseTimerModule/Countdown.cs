using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Networking.Packet;
using System;

namespace Silksong.TheHuntIsOn.Modules.PauseTimerModule;

internal class Countdown : NetworkedCloneable<Countdown>
{
    public const int MAX_MESSAGE_LENGTH = byte.MaxValue;

    // Scheduled time for the countdown to expire.
    public long FinishTimeTicks;
    // If set, always show this amount as the remaining time.
    public long? FrozenRemainder;
    // If set, override FrozenRemainder and start counting down again after this time.
    public long? UnfreezeTimeTicks;
    // Message to accompany the countdown.
    public string Message = "<untitled>";

    public override void ReadData(IPacket packet)
    {
        FinishTimeTicks.ReadData(packet);
        FrozenRemainder.ReadData(packet);
        UnfreezeTimeTicks.ReadData(packet);
        Message = packet.ReadString();
    }

    public override void WriteData(IPacket packet)
    {
        FinishTimeTicks.WriteData(packet);
        FrozenRemainder.WriteData(packet);
        UnfreezeTimeTicks.WriteData(packet);
        Message.WriteData(packet);
    }

    public bool IsFrozen(DateTime now) => FrozenRemainder.HasValue && (!UnfreezeTimeTicks.HasValue || UnfreezeTimeTicks.Value > now.Ticks);

    public bool IsCompleted(DateTime now) => !IsFrozen(now) && now.Ticks >= FinishTimeTicks;

    public bool GetDisplayTime(out float seconds)
    {
        var now = DateTime.UtcNow;
        if (IsFrozen(now) || !IsCompleted(now))
        {
            TimeSpan span = new(IsFrozen(now) ? FrozenRemainder!.Value : (FinishTimeTicks - now.Ticks));
            seconds = (float)span.TotalSeconds;
            return true;
        }

        seconds = 0;
        return false;
    }

    public Countdown Pause(DateTime now)
    {
        if (IsCompleted(now)) return this;
        if (IsFrozen(now)) return With(c => c.UnfreezeTimeTicks = null);

        return With(c =>
        {
            c.FrozenRemainder = FinishTimeTicks - now.Ticks;
            c.UnfreezeTimeTicks = null;
        });
    }

    public Countdown UnpauseAt(DateTime now, DateTime unpauseWhen)
    {
        if (IsCompleted(now)) return this;
        if (IsFrozen(now)) return With(c => {
            c.FinishTimeTicks = unpauseWhen.Ticks + FrozenRemainder!.Value;
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
