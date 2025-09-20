# HeadlessTickManager

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/) that dynamically adjusts the tick rate of the headless client based on world activity and user counts.

This project started as an exploration of [DynamicTickRate](https://github.com/Raidriar796/DynamicTickRate).  
That mod served as a reference point — especially for handling user join/leave events and world counting syntax.  

HeadlessTickManager builds on those ideas with additional math, stability features, and logging improvements.  
Some code was adapted and extended with the help of an LLM to implement new smoothing and weighting logic.  

---

## 🚀 Features

- **Active-world only logic**  
  Empty worlds no longer raise tick rate; ticks stay at `MinTickRate` until someone joins.

- **Busy-world weighting**  
  Populated worlds count more heavily; extra users past a soft cap contribute less.

- **Join-burst handling**  
  Temporary tick boosts during short bursts of joins to prevent desyncs.

- **EMA smoothing + hysteresis + cooldowns**  
  Prevents “flapping” when users join/leave rapidly. Tick changes are stable and gradual.

- **InstantIdleDrop**  
  - When enabled: instantly drop to `MinTickRate` if all worlds are empty.  
  - When disabled: smoothly ramp down using EMA smoothing.

- **⚡ Clean logging**  
  Readable log output with emoji markers, so tick changes are easy to spot at a glance.

---

## 📜 Example Log Output

```
⚡ [HeadlessTickManager] → 30 ticks (idle; activeWorlds=0)  
⚡ [HeadlessTickManager] → 42 ticks (raw=31.2, ema=42.0, activeWorlds=1, joins/min=0.00)  
⚡ [HeadlessTickManager] → 53 ticks (raw=73.4, ema=52.8, activeWorlds=4, joins/min=9.60)  
```

---

## ⚙️ Config Options

- `Enable`  
  Enables or disables the mod.

- `MinTickRate`  
  Lowest tick rate allowed (default: **30**).

- `MaxTickRate`  
  Highest tick rate allowed (default: **144**).

- `AddedTicksPerUser`  
  Extra ticks per non-host user (weighted).

- `AddedTicksPerWorld`  
  Extra ticks per additional **active** world beyond the first.

- `ActiveWorldUserThreshold`  
  Minimum non-host users required before a world counts as active.

- `TopKWorlds` / `BusyWorldWeight`  
  Apply more weight to the busiest worlds when calculating ticks.

- `PerWorldUserSoftCap` / `PerWorldDiminish`  
  Soft cap and diminishing returns per-world.

- `JoinRateTicksPerJpm` / `JoinWindowSeconds`  
  Join-burst handling: extra ticks per join-per-minute within the window.

- `EmaAlpha`, `HysteresisTicks`, `MinChangeIntervalSeconds`,  
  `BigJumpThreshold`, `BigJumpCooldownSeconds`  
  Fine-grained smoothing and stability options.

- `LogOnChange`  
  Whether to log ⚡ tick changes (default: **true**).

- `InstantIdleDrop`  
  - Default: `false`  
  - `true`: Snap instantly to `MinTickRate` when no non-host users remain.  
  - `false`: Glide back down gradually.
---

## 📖 Further Reading

For a deeper explanation of each config option and example drop-in configs (Conservative / Balanced / Aggressive), see [🔗 ConfigTuningGuide.md](ConfigTuningGuide.md).

## 🛠️ tickwatch.sh

This repo also includes **`tickwatch.sh`**, a helper script to make monitoring tick changes and user activity easier.  
You may need to edit the `logdir="$HOME/.steam/steam/steamapps/common/Resonite/Headless/Logs"` if your headless puts its log somewhere else..

### Features
- ✅ Pretty-prints tick changes from **HeadlessTickManager** logs.  
- 👥 Shows user joins and leaves inline, with world names and user IDs.  
- 🌎 Detects and reports **world restarts** instead of mislabeling them as the headless user joining.  
- ⚡ Explains what each tick field (`raw`, `ema`, `activeWorlds`, `joins/min`) means at the top of output.  
- 🔄 Supports both one-shot view (last N lines) and live following (`-f`).  
- 🎛️ Options:
  - `-f` or `--follow` → follow live log updates  
  - `-H` or `--hide-host` → hide the headless “self-join” lines  

### Example
```bash
# Show the last 30 relevant lines
./tickwatch.sh

# Follow the log live, hiding headless restart joins
./tickwatch.sh -f -H

# Or a simple command to just run in a terminal to monitor your server..
while true; do clear && ./tickwatch.sh; sleep 30; done
```

<img width="1560" height="848" alt="image" src="https://github.com/user-attachments/assets/0b84cf0a-a20c-4f50-a868-8d4c510207be" />


---

## 📥 Installation

1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
2. Place `HeadlessTickManager.dll` into the `rml_mods` folder inside your headless install.  
   (If the folder doesn’t exist, launch once with RML and it will be created.)
3. Restart your headless server.
4. Check logs for ⚡ lines to confirm it’s running.

---

## ⚖️ License

Licensed under the [MIT License](./LICENSE).  
DynamicTickRate remains the work of its original author.  
HeadlessTickManager is a separate project inspired by it, with expanded functionality and design choices.
