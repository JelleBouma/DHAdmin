## "DHAdmin" - is a TeknoMW3 Dedicated Server Administrative Plugin

DHAdmin was made to expand upon DGAdmin with additional features to create custom game modes, it is compatible with any existing DGAdmin settings. However it does use a different InfinityScript version, so any other scripts will not be compatible unless you recompile them.
See [Features](https://github.com/JelleBouma/DHAdmin#features) for information about DHAdmin features.

DHAdmin is still in development (pre-release). This readme has not been fully updated/written yet, but there are examples available in the doc folder.
You can start using DHAdmin already, better documentation and more features will become available in the future.
For a live demo you can check out my server "deathstroyers game modes" although it is not always online.

## License

This project is licensed under GPLv3. Please see the LICENSE file.

##### Technologies used:

- DGAdmin v3.5.1. By F. Bernkastel (https://github.com/FredericaBernkastel/codmw3-server-DGAdmin-plugin)
- InfinityScript 1.5.3 updated. By Slvr11 (https://github.com/Slvr11)

## Special thanks and acknowledgements

- Slvr11, LastDemon99 (https://github.com/LastDemon99) and Diavolo (https://github.com/diamante0018) for helping with questions I had about TeknoMW3 and InfinityScript.
- Frederica Bernkastel for creating DGAdmin.
- Skullman and Night Kid for helping test DHAdmin features.
<br>

## Features:

#### All DGAdmin features
DHAdmin is compatible with all DGAdmin settings and features everything DGAdmin does, for more info on DGAdmin see https://github.com/FredericaBernkastel/codmw3-server-DGAdmin-plugin

#### Improved map rotation
A line in the DSPL can now have multiple maps specified in format: map1,map2,mode,weight
You can specify as many lines and maps per line as you want, compatible with old DSPL files.
This new map rotation system removes the need for the unreliable delay when checking the current game mode that plagues DGAdmin.
Set "settings_dspl" in "DHAdmin/settings.txt" to the DSPL you want to use (without extension).
The map rotation is fully random, however "settings_dsr_repeat" can be set to (dis)allow repeating the same DSR.

#### Map editing
Set "settings_map_edit" to mapedit files you want to use located in DHAdmin/MapEdit.
Mapedit files are structured as follows (repeating structure):
```
a mapname
mapedit objects you want to spawn in this map, 1 line each, parameters are separated by '|'
```

Possible mapedit objects are as follows:
- The name of an fx or model. If it is an fx it should be prefixed with `fx:`. Any fx or model that the map has can be spawned this way.
  1. x, y and z coordinates separated by ','
  2. x, y and z angles separated by ','
- `collision` a care package that can not be picked up but can be used for collision.
  1. x, y and z coordinates separated by ','
  2. x, y and z angles separated by ','
  3. visibility of the care package either `true` or `false`
- `explodable` an explodable (red) barrel, will only work when spawned on server start.
  1. x, y and z coordinates separated by ','
  2. x, y and z angles separated by ','
- `weapon` a weapon that can be picked up by the player if they are in proximity (a hud message will be shown to the player) and press the use key ('f' by default).
  1. x, y and z coordinates separated by ','
  2. x, y and z angles separated by ','
  3. list of weapons that can spawn (one will be randomly selected from this list)
  4. `constant` to let everyone pick up the weapon as much as they want or `death` to respawn the weapon pickup when the carrier dies.
  5. `true` to take away all other weapons from whoever picks up this weapon, `false` otherwise.
  6. `yaw`, `roll` or `pitch` to rotate the weapon pickup. Leave empty if you don't want to rotate it.
  7. Rotation speed in seconds, you can leave this empty if you don't want to rotate it.
- `objective` a search and destroy objective.
  1. x, y and z coordinates separated by ','
  2. x, y and z angles separated by ','
  3. x, y and z coordinates of planted bomb separated by ','
  4. x, y and z of planted bomb angles separated by ','
  5. name of this objective
  6. visibility of the objective, either `true` or `false`. When `true` the objective will look as in search and destroy: a pallet with cases of warheads.
  7. the icon on the map
  8. the time in seconds for the bomb to explode
  9. the time in seconds it takes to plant or defuse a bomb

For examples please check out the map edit examples in doc/MapEdit Examples.
<br>
In the future I will make it so that map editing can easily be done using in-game commands.

#### Achievements
Achievements are additional emblem sized shaders shown to your victims next to your callsign.
They are earned by reaching or exceeding a specified amount of achievement progress.
Please see "doc/Achievement Examples/" for examples of achievements and "doc/Rewards Examples/" for examples of how achievement progress can be tracked.

#### Rewards
The reward system gives you a lot of flexibility in creating game modes.
It lets you assign a reward (positive, negative or inbetween) to a player whenever they complete a specified mission (for example: making a kill). For now, please see the examples for more detail.
<br>
Mission types: "shoot", "kill", "die", "win", "pickup", "objective_destroy", "topscore"
<br>
Reward types: "speed", "score", "weapon", "perks", "fx", "chat" and achievement progress.

#### Damage changes

#### Additional settings
##### settings_unlimited_stock=false
Set to true to enable unlimited stock for primary and secondary weapons (unlimited ammo with reloading).
##### settings_unlimited_grenades=false
Set to true to enable unlimited lethal and tactical grenades/throwables.
##### settings_jump_height=39
Set to a different integer to change the jump height, 39 is the unchanged modern warfare 3 jump height.
##### settings_movement_speed=1
Set to a different floating point number to change the movement speed scale.
##### settings_dspl=default
The map rotation dspl file name (without path and extension). The file should be located in either admin/ or players2/ and have the extension .dspl.
##### settings_dsr_repeat=true
Whether repeating the same dsr/mode is allowed.
##### settings_objective=
The game objective shown to players in the escape menu.
##### settings_didyouknow=
The hint text in the loading screen, the text defined here will only be shown near the end of loading.
##### settings_dropped_weapon_pickup=true
Whether weapons dropped by dead players can be picked up.
##### settings_player_team=
Set to "axis" or "allies" to force players to play in this team, skipping past any selection screens. It is also possible to force a recipe class by adding the class number (1-5) for example: "axis0".
##### settings_achievements=false
Enable or disable the achievement system.
##### settings_rewards=
The missions and their rewards (see Rewards).
##### settings_rewards_weapon_list=
A list of weapons to be enumerated over by rewards weapon:previous and weapon:next, players start at the first weapon.
##### settings_score_start=0
The score at which a player starts.
##### settings_score_limit=0
Set to a positive number for a custom score limit, reach this score to win and end the game. 0 means no custom scorelimit.
##### settings_map_edit=
Names (without extensions) of map edit files to be used seperated by ',', map edit files should be txt files located in scripts/DHAdmin/MapEdit.
##### johnwoo_improved_reload=false
If set to true, akimbo reloading with less than full ammo will divide the bullets evenly among the guns instead of the default mw3 behaviour of filling one gun fully first.
##### johnwoo_pistol_throw=false
If set to true, empty pistols (handguns or g18s) can be thrown like throwing knives.
##### johnwoo_momentum=false
If set to true damage you deal is increased while you are in midair. Also, falling damage is reduced by 50% (Will be deprecated in the future).
<br>

## Commands:
DHAdmin has got **a lot** of chat commands.

From simple and fast to complex and powerful command syntaxes are supported.
<br><br>

Read the [official DGAdmin documentation](https://drive.google.com/file/d/0B4OfimTH0gRhaXJFYWRId0ZZaG8/view?usp=sharing) for advanced guidelines.
<br><br>


## Compiling from sources:

InfinityScript version: updated 1.5.3

Used Visual Studio Community 2019

.NET Framework 4.0
