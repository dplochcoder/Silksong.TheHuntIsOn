using Silksong.TheHuntIsOn.SsmpAddon;
using Silksong.TheHuntIsOn.SsmpAddon.PacketUtil;
using SSMP.Networking.Packet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Silksong.TheHuntIsOn.Modules.PauseTimerModule;

internal class ServerPauseState : IIdentifiedPacket<ClientPacketId>
{
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

    public void ReadData(IPacket packet)
    {
        Countdowns.ReadData(packet);
        ServerPaused.ReadData(packet);
        UnpauseTimeTicks.ReadData(packet);
    }

    public void WriteData(IPacket packet)
    {
        Countdowns.WriteData(packet);
        ServerPaused.WriteData(packet);
        UnpauseTimeTicks.WriteData(packet);
    }

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
