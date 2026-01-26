# Architect Module

[Architect](https://github.com/cometcake575/Architect-Silksong) is a powerful level editor for Silksong with extensive ingame capabilities. While primarily intended for the development of custom challenges, it can also be used as a simple level modifier for the speedrunner vs. hunters format.

The Architect Module allows any number of Architect "Levels" (a set of modifications to the entirety of Pharloom) to be toggled on/off. Levels stored on the server are automatically downloaded by connected clients, they do not need to be manually distributed.

## Adding Levels to the Server

With Architect installed, make as many or as few edits to the world as you would like. When finished and saved, find the saved level data; on Windows, this is in "%APPDATA%/LocalLow/Team Cherry/Hollow Knight Silksong/Architect/Scenes". Make a copy of the Scenes directory, and rename it to something descriptive. The name of this directory will be used as the name of the 'Level' in TheHuntIsOn.

Create a directory named `Architect-Server` in the same directory where `Silksong.TheHuntIsOn.dll` is located, which depends on whether you are using manual mod installs, r2modman, or if you are running a dedicated server. Move the copied directory from the previous paragraph into this new folder.

That's it! When you start the server anew, it will scan the `Architect-Server` directory for levels and make them available in the UI for toggling on/off. If your server is already running, you can use the `/hunt update-architect` command to reload the levels live.

## Embedding Levels

Levels can also be embedded directly within `Silksong.TheHuntIsOn.dll`. To do this, place them in the `Resources/Data/Architect` directory, then do a new build and release.

Any levels placed within `Architect-Server` will *override* embedded levels by default, allowing you to make post-release hot fixes until a new release can be orchestrated.

## Limitations

SSMP is not designed for large downloads, and has an internal limit of 256KiB per packet sent. The Architect module is designed to send one set of scene modifications per packet, so if you go overboard with level-editing and make too many changes to a single scene, you may trip this limit.
