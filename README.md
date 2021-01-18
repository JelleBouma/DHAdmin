## "DHAdmin" - is a TeknoMW3 Dedicated server administrative plugin

DHAdmin is still in development (pre-release).

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

DHAdmin was made to expand upon DGAdmin with additional features to create custom game modes, it is compatible with your existing DGAdmin settings. However it does use a different InfinityScript version, so any other scripts will not be compatible unless you recompile them.
DHAdmin also features an improved maprotation system which fixes the problems I had with the normal MW3 map rotation: delay for reading dynamic properties, max 16 lines, can only select 1 map or all and highly predictable randomness.

## Features:

#### All DGAdmin features
DHAdmin is compatible with all DGAdmin settings and features everything DGAdmin does, for more info on DGAdmin see https://github.com/FredericaBernkastel/codmw3-server-DGAdmin-plugin

#### Improved map rotation
A line in the DSPL can now have multiple maps specified in format: map1,map2,mode,weight
You can specify as many lines and maps per line as you want, compatible with old DSPL files.
Set "settings_dspl" in "DHAdmin/settings.txt" to the DSPL you want to use (without extension).
The map rotation is fully random, however "settings_dsr_repeat" can be set to (dis)allow repeating the same DSR.

#### Map editing
Set "settings_map_edit" to mapedit files you want to use located in DHAdmin/MapEdit.
Mapedit files are structured as follows (repeating structure):
a mapname
mapedit objects you want to spawn in this map 1 line each

Where possible mapedit objects are as follows:


#### Achievements

#### Rewards

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
##### settings_extra_explodables=false
Whether extra explodables should be spawned on Hardhat, Dome, Carbon and Off Shore (Will be deprecated in the future).
##### settings_player_team=
Set to "axis" or "allies" to force players to play in this team. It is also possible to force a recipe class by adding the class number (1-5) for example: "axis0".
##### settings_killionaire=false
Enable killionaire game mode (Will be deprecated in the future).
##### settings_achievements=false
##### settings_track_achievements=
##### settings_rewards=
##### settings_rewards_weapon_list=
##### settings_score_start=0
##### settings_score_limit=0
##### settings_map_edit=
##### johnwoo_improved_reload=false
##### johnwoo_pistol_throw=false
##### johnwoo_momentum=false
<br>

## Commands:
DHAdmin has got **a lot** of chat commands.

From simple and fast to complex and powerful command syntaxes are supported.
<br><br>

Read the [official DGAdmin documentation](https://drive.google.com/file/d/0B4OfimTH0gRhaXJFYWRId0ZZaG8/view?usp=sharing) for advanced guidelines.
<br><br>


## Compiling from sources:

InfinityScript version: updated 1.5.3

Used Visual Studio Community 2017
