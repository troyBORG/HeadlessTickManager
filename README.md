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
* Applies diminishing returns so big worlds don’t explode tick rate.

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

---


## 📜 Logs

The mod prints when tick rate changes, e.g.:

```
[INFO] [ResoniteModLoader/HeadlessTickManager] 62 ticks (raw=61.8, ema=61.9, activeWorlds=2, joins/min=0.80)
```

---

## 🛠 Maintainer

* Author: **troyBORG**
* Repo: [github.com/troyBORG/HeadlessTickManager](https://github.com/troyBORG/HeadlessTickManager)

Pull requests and issue reports welcome!
