# DynamicTickRate

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/) that adjusts the tick rate of the headless client based on user and world count.

## Requirements
- [Headless Client](https://wiki.resonite.com/Headless_Server_Software)
- [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader)

## Installation
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
2. Place [DynamicTickRate.dll](https://github.com/Raidriar796/DynamicTickRate/releases/latest/download/DynamicTickRate.dll) into your `rml_mods` folder inside of the headless installation. You can create it if it's missing, or if you launch the client once with ResoniteModLoader installed it will create the folder for you.
3. Start the client. If you want to verify that the mod is working you can check the headless client's logs.

## Config Options

- `Enable`
  - Determines if the mod runs or not.

- `MinTickRate`
  - The lowest tick rate the mod is allowed to set.

- `MaxTickRate`
  - The highest tick rate the mod is allowed to set.

- `AddedTicksPerUser`
  - How much the tick rate is increased for every user that joins.

- `AddedTickPerWorld`
  - How much the tick rate is increase for every world that's opened beyond the first world.
