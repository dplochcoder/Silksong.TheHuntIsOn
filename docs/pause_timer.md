# Pause Timer Module

This module allows server-wide pauses and synchronized countdowns to be broadcast to all players. While the server is paused, respawn timers after death will stop ticking down.

Only authorized users can pause or unpause the server, or broadcast countdowns. Everything is configured through the `/pausetimer` or `/pt` command.

Puase timer coordination relies on accurate system clocks on server and client machines. If players have wildly divergent system clocks, they may encounter buggy behavior, or be unaffected by legitimate pauses.

## `/pt pause [delay]`

Pause the game, immediately or after `delay` seconds.

## `/pt unpause [delay]`

Unpause the game, immediately or after `delay` seconds.

## `/pt countdown SECONDS MESSAGE`

Broadcast a custom countdown message which goes away after the specified number of seconds.

## `/pt clearcountdowns`

Clear any active countdown messages.
