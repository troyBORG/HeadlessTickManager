# DynamicTickRate (AI Enhanced Fork)

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/) that adjusts the tick rate of the headless client based on user and world count.

⚠️ **Disclaimer**  
This is an **unofficial fork** of [Raidriar796/DynamicTickRate](https://github.com/Raidriar796/DynamicTickRate).  
It is **not associated with or endorsed by the original author**.  
The upstream repository is the canonical source. This fork is maintained separately and will not submit pull requests upstream.  

The original author has asked that no LLM- or AI-generated code be contributed back to their project.  
This fork was modified with the assistance of an LLM and exists solely for personal/server use and experimentation.  

---

## 🚀 Enhancements in this fork

- **Active-world only logic**  
  Empty worlds no longer increase tick rate — tick stays at `MinTickRate` until someone joins.

- **Busy-world weighting**  
  Heavily populated worlds count more toward tick rate. After a soft cap, extra users contribute less (diminishing returns).

- **Join-burst handling**  
  Short bursts of user joins temporarily boost tick rate to avoid desyncs.

- **EMA smoothing + hysteresis + cooldown**  
  Prevents tick rate flapping when users join/leave rapidly. Tick adjustments are gradual and stable.

- **Configurable caps**  
  `MinTickRate` and `MaxTickRate` remain enforced. Defaults: **30–144**.

- **⚡ Logging**  
  Optional log messages with emoji marker whenever the tick rate changes, making it easy to spot in headless logs.

- **InstantIdleDrop**
  - When true, the tick rate snaps immediately back to `MinTickRate` if all worlds are empty (no non-host users).  
  - When false, the tick rate smoothly glides back down using EMA smoothing and hysteresis.  

---

## Requirements
- [Headless Client](https://wiki.resonite.com/Headless_Server_Software)
- [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader)

---

## Installation
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
2. Place the built `DynamicTickRate.dll` into your `rml_mods` folder inside of the headless installation.  
   If missing, launch the client once with ResoniteModLoader and it will create the folder.
3. Start the headless client. Check logs for ⚡ lines to verify the mod is working.

---

## Config Options

- `Enable`  
  Enables or disables the mod.

- `MinTickRate`  
  Lowest tick rate allowed (default: **30**).

- `MaxTickRate`  
  Highest tick rate allowed (default: **144**).

- `AddedTicksPerUser`  
  How much tick rate increases per non-host user (weighted).

- `AddedTicksPerWorld`  
  How much tick rate increases per extra **active** world beyond the first.

- `ActiveWorldUserThreshold`  
  Minimum number of non-host users required for a world to count as active (default: **1**).

- `TopKWorlds` / `BusyWorldWeight`  
  Heavily weights the busiest worlds in tick calculations.

- `PerWorldUserSoftCap` / `PerWorldDiminish`  
  Soft cap and diminishing returns for per-world user counts.

- `JoinRateTicksPerJpm` / `JoinWindowSeconds`  
  Join-burst handling (ticks added per join-per-minute, measurement window).

- `EmaAlpha`, `HysteresisTicks`, `MinChangeIntervalSeconds`,  
  `BigJumpThreshold`, `BigJumpCooldownSeconds`  
  Smoothing and stability options.

- `LogOnChange`  
  Logs ⚡ lines when tick changes (default: **true**).

- `InstantIdleDrop`
  - Default: `false`
  - When `true`, the tick rate will snap immediately down to `MinTickRate` whenever all worlds are empty (no non-host users).
  - When `false`, the tick rate will glide back down gradually using EMA smoothing and hysteresis.

---

## ⚖️ License
Licensed under the [MIT License](./LICENSE), inherited from the original project.  
The original author retains credit for their work.  
This fork is maintained independently and may diverge significantly.
