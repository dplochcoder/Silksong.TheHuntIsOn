# Events Module

The Events module maps various instances of Speedrunner progression to Hunter rewards, allowing movement and combat abilities to escalate steadily whilst remaining fair to early game speedrunners.

The default set of events and rewards are configured in `Resources/Data/default-events.json`. The format should be self-evident; rewards are a list of items, and an associated server-wide message, coupled with a specific speedrunner progression point.

## Syncing

Rewards are persistent on the server. Whenever a speedrunner achieves a certain progression and triggers a reward, all hunters on the server get that reward whether they are currently connected or not, allowing new hunters to automatically catch up.

To reset this progression, authorized users can run the `/hunt reset` command at any time. Note that this will not work well if any speedrunners are still connected on saves full of progression, since that progression will immediately re-trigger all the hunter rewards.

## Changing Rewards

Rewards can be changed either by modifying the embedded `default-events.json` and making a new release, or else placing a custom `events.json` in the same folder as `Silksong.TheHuntIsOn.dll`.

The full sets of speedrunner events are defined [here](../Modules/EventsModule/SpeedrunnerBoolEvent.cs) and [here](../Modules/EventsModule/SpeedrunnerCountEventType.cs). For the second file, events must be written with a number, e.g. "Masks6" or "SilkSpools10" to indicate rewards should be given when the speedrunner has a total of 6 masks or 10 silk spools, respectively.

The full set of hunter rewards is defined [here](../Modules/EventsModule/HunterItemGrantType.cs). Rewards like Masks and SilkSpools can be repeated to give multiple of them at a specific point.

After changing the `events.json` file on a live server, any authorized user can run the `/hunt update-events` command to reload the file.
