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
DHAdmin is compatible with all DHAdmin settings and features everything DGAdmin does, for more info on DGAdmin see https://github.com/FredericaBernkastel/codmw3-server-DGAdmin-plugin

#### Improved map rotation
A line in the DSPL can now have multiple maps specified in format: map1,map2,mode,weight
You can specify as many lines and maps per line as you want, compatible with old DSPL files.
Set "settings_dspl" in "DHAdmin/settings.txt" to the DSPL you want to use (without extension).
The map rotation is fully random, however "settings_dsr_repeat" can be set to (dis)allow repeating the same DSR.

#### Map editing

#### Achievements

#### Rewards

#### Damage changes

#### Additional settings

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
