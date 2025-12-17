# HeadlessTickManager

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/) that dynamically adjusts the tick rate of the headless client based on world activity and user counts.

This project started as an exploration of [DynamicTickRate](https://github.com/Raidriar796/DynamicTickRate).  
That mod served as a reference point — especially for handling user join/leave events and world counting syntax.  

HeadlessTickManager builds on those ideas with additional math, stability features, and logging improvements.  
Some code was adapted and extended with the help of an LLM to implement new smoothing and weighting logic.  

---

## ✨ What it does

* Runs low (30 ticks/sec) when idle → saves CPU.
* Scales higher when worlds are busy.
* Adds a short boost when many users join at once (to keep existing players smooth).
* Uses smoothing and cooldowns to avoid wild fluctuations.
* Applies diminishing returns so big worlds don't explode tick rate.
* **NEW in v2.0.3:** Configuration validation on startup.
* **NEW in v2.0.3:** Statistics tracking and periodic status summaries.
* **NEW in v2.0.3:** Health check warnings for configuration issues.
* **NEW in v2.0.3:** Configurable logging with rate limiting to reduce log bloat.

Default range: **30 → 90 ticks/sec** 
---

## 📥 Installation

1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
2. Place `HeadlessTickManager.dll` into the `rml_mods` folder inside your headless install.  
   (If the folder doesn’t exist, launch once with RML and it will be created.)
3. Restart your headless server.
4. Check logs for lines to confirm it’s running.


---

## 📊 How tick math works

```
tickRate = MinTickRate
         + (WeightedUsers × AddedTicksPerUser)
         + ((ActiveWorlds − 1) × AddedTicksPerWorld)
         + JoinBonus
```

* **WeightedUsers** = users per world, capped at `PerWorldUserSoftCap`, then diminished.
* **Busy worlds** = top `K` worlds get an extra multiplier (`BusyWorldWeight`).
* **JoinBonus** = recent joins/min × `JoinRateTicksPerJpm`, capped.
* Final value is smoothed with EMA and clamped between Min and Max.

### Example

* 1 world with 20 users → \~50 ticks.
* 4 worlds × 50 users each → \~90 ticks (capped).
* 1 big world with 80 users → \~80 ticks.

---

## ⚙️ Default values

| Key                      | Default | Description                           |
| ------------------------ | ------- | ------------------------------------- |
| MinTickRate              | 30      | Floor when idle                       |
| MaxTickRate              | 90      | Ceiling under heavy load              |
| AddedTicksPerUser        | 0.65    | Extra ticks per user                  |
| AddedTicksPerWorld       | 1.8     | Extra ticks per world (after first)   |
| TopKWorlds               | 2       | Number of busiest worlds to weight    |
| BusyWorldWeight          | 1.7     | Weight for top worlds                 |
| PerWorldUserSoftCap      | 16      | Users per world before diminishing    |
| PerWorldDiminish         | 0.40    | Fractional contribution past soft cap |
| JoinRateTicksPerJpm      | 2.5     | Tick bonus per join/minute            |
| JoinRateMaxBonusTicks    | 12      | Cap on join burst ticks               |
| JoinWindowSeconds        | 25      | How far back to look for joins        |
| EmaAlpha                 | 0.28    | Smoothing factor                      |
| HysteresisTicks          | 2       | Minimum delta before change           |
| MinChangeIntervalSeconds | 4       | Minimum interval between changes      |
| BigJumpThreshold         | 12      | Change size considered a big jump     |
| BigJumpCooldownSeconds   | 10      | Cooldown after a big jump             |
| InstantIdleDrop          | true    | Immediately drop to min if idle       |
| LogOnChange              | true    | Log when tick rate changes            |
| EnablePeriodicSummaries  | true    | Enable periodic status summaries      |
| EnableHealthWarnings     | true    | Enable health check warnings          |

**Note:** Default values may differ slightly from the table above. Check the startup summary in logs for actual values being used.

---


## 📜 Logs

### Tick Rate Changes

The mod logs tick rate changes with rate limiting (max once per 15 seconds) to reduce log bloat. Normal changes show simply:
```
[INFO] [ResoniteModLoader/HeadlessTickManager] 62 ticks
```

Big jumps (≥BigJumpThreshold) show additional details:
```
[INFO] [ResoniteModLoader/HeadlessTickManager] 62 ticks (Δ+12, 2 worlds, 0.8 joins/min)
```

### Startup Summary

On initialization, the mod logs a detailed startup summary with:
- Version information
- Tick rate range (Min/Max)
- Initial state (worlds and users)
- Full configuration values
- Logging configuration state

### Periodic Status Summaries

Every 2 hours (if enabled), the mod logs a status summary including:
- Current and average tick rate
- Peak and minimum tick rates reached
- Active worlds and total users
- Tick change frequency

### Health Check Warnings

The mod automatically warns about potential issues (if enabled):
- ⚠ Extended periods at maximum tick rate (suggests raising `MaxTickRate`)
- ⚠ High tick rate fluctuation (suggests stability tuning)

### Configuration Validation

On startup, the mod validates all configuration values and warns about:
- Invalid ranges (e.g., MinTickRate >= MaxTickRate)
- Out-of-bounds values (e.g., EmaAlpha outside 0-1)
- Potentially problematic combinations

### Logging Configuration

You can control logging behavior via config:
- `LogOnChange`: Enable/disable tick change logging (default: true)
- `EnablePeriodicSummaries`: Enable/disable periodic summaries (default: true)
- `EnableHealthWarnings`: Enable/disable health warnings (default: true)

---

## 🛠 Maintainer

* Author: **troyBORG**
* Repo: [github.com/troyBORG/HeadlessTickManager](https://github.com/troyBORG/HeadlessTickManager)

Pull requests and issue reports welcome!
