# How to Tune HeadlessTickManager

### General Guidance

- **MinTickRate / MaxTickRate**  
  Sets the floor and ceiling for how fast the headless runs.  
  - Low-end servers: lower `MaxTickRate` (e.g. 120) to save CPU.  
  - High-end servers: keep at 144+ for smoother experience in crowded sessions.  

- **AddedTicksPerUser / AddedTicksPerWorld**  
  Controls how much extra load each user or active world adds.  
  - Bigger = more responsive but heavier CPU load.  
  - Smaller = lighter load, but large sessions may feel laggier.  

- **BusyWorldWeight / TopKWorlds**  
  Lets heavily populated worlds count more than small ones.  
  Great when hosting one or two big sessions rather than many small ones.  

- **Soft Caps & Diminish (PerWorldUserSoftCap / PerWorldDiminish)**  
  Prevents a single crowded world from blowing up the tick rate.  
  After the soft cap, extra users add less weight.  

- **Join Burst (JoinRateTicksPerJpm / JoinWindowSeconds)**  
  Temporarily boosts ticks when lots of people join at once.  
  Keeps mass spawns smooth without lag spikes.  

- **EMA Alpha / Hysteresis / Cooldowns**  
  Stability controls.  
  - Larger `EmaAlpha` = faster response but more jitter.  
  - Smaller = smoother, more stable, but slower to react.  
  - Hysteresis and cooldowns prevent oscillation.  

- **InstantIdleDrop**  
  - `true`: snaps down to `MinTickRate` as soon as everyone leaves (best for CPU).  
  - `false`: ramps down smoothly, keeping the session warmer for quick re-joins.  

💡 **Rule of Thumb**:  
- Defaults are fine for small groups or private headless use.  
- Tune upward (`MaxTickRate`, `AddedTicksPerUser`) for **large public events**.  
- Tune downward (lower `MaxTickRate`, higher diminish) if your **CPU/RAM is tight**.

---

## Detailed Option Reference

### Core limits

**MinTickRate / MaxTickRate**  
- Lower `MinTickRate` saves CPU when idle.  
- Higher `MaxTickRate` improves responsiveness in crowded worlds but costs CPU.  
- If CPU stays ≤30% even under load, raise `MaxTickRate`. If it pegs, lower it.  

---

### Load contributors

**AddedTicksPerUser**  
- Bigger = faster ramp as users join.  
- CPU-light worlds tolerate higher values; heavy sessions may need lower.  

**AddedTicksPerWorld**  
- Adds ticks per additional active world beyond the first.  
- Good for hosts running many small worlds.  

**ActiveWorldUserThreshold**  
- Users required for a world to count as active.  
- `1` = any visitor wakes the world.  
- `2–3` = ignore trickle traffic on weaker servers.  

---

### Busy-world weighting

**TopKWorlds**  
- Number of busiest worlds to weight heavily.  
- `2` fits a main world + a side world; raise for evenly spread traffic.  

**BusyWorldWeight**  
- Multiplier for those busiest worlds.  
- Higher = prioritizes the busiest spaces, but may starve others.  

**PerWorldUserSoftCap**  
- Users per world after which each extra counts less.  

**PerWorldDiminish**  
- Discount factor for users past the soft cap (0–1).  
- `0.35` = extras still count, but reduced.  

---

### Join-burst handling

**JoinRateTicksPerJpm**  
- Temporary ticks per joins-per-minute.  
- Higher helps with mass joins, but watch CPU.  

**JoinWindowSeconds**  
- Window for measuring joins/min.  
- Short = spikier response, long = smoother.  

---

### Smoothing & stability

**EmaAlpha (0–1)**  
- Higher = reacts faster, more jittery.  
- Lower = smoother, slower.  

**HysteresisTicks**  
- Minimum change before tick adjusts. Prevents 1-tick flapping.  

**MinChangeIntervalSeconds**  
- Minimum time between tick changes. Raise if ticks bounce too often.  

**BigJumpThreshold / BigJumpCooldownSeconds**  
- Allows big jumps quickly, then cools down to avoid thrash.  

---

### Behavior & logging

**InstantIdleDrop**  
- `true`: snap to idle instantly when all worlds empty.  
- `false`: glide down gradually.  

**LogOnChange**  
- Only logs ⚡ when tick actually changes.  
- Keeps logs clean and readable.  


## Example Configs

### 1) Conservative (low-end VPS / many small worlds)

```json
{
  "version": "1.0.0",
  "values": {
    "Enable": true,
    "MinTickRate": 30,
    "MaxTickRate": 120,
    "AddedTicksPerUser": 0.5,
    "AddedTicksPerWorld": 1.5,
    "ActiveWorldUserThreshold": 1,
    "TopKWorlds": 2,
    "BusyWorldWeight": 1.5,
    "PerWorldUserSoftCap": 12,
    "PerWorldDiminish": 0.45,
    "JoinRateTicksPerJpm": 2.5,
    "JoinWindowSeconds": 20,
    "EmaAlpha": 0.22,
    "HysteresisTicks": 2,
    "MinChangeIntervalSeconds": 4,
    "BigJumpThreshold": 14,
    "BigJumpCooldownSeconds": 12,
    "LogOnChange": true,
    "InstantIdleDrop": true
  }
}
```

<details>
<summary>Why these settings?</summary>

Lower peaks, stronger smoothing, and more conservative scaling. Best for limited CPU/RAM environments, or when hosting lots of small/lightweight worlds. Keeps the tick rate modest and avoids spikes.
</details>

---

### 2) Balanced (tested config)

```json
{
  "version": "1.0.0",
  "values": {
    "Enable": true,
    "MinTickRate": 30,
    "MaxTickRate": 160,
    "AddedTicksPerUser": 0.7,
    "AddedTicksPerWorld": 2.0,
    "ActiveWorldUserThreshold": 1,
    "TopKWorlds": 2,
    "BusyWorldWeight": 1.75,
    "PerWorldUserSoftCap": 16,
    "PerWorldDiminish": 0.35,
    "JoinRateTicksPerJpm": 3.5,
    "JoinWindowSeconds": 25,
    "EmaAlpha": 0.28,
    "HysteresisTicks": 1,
    "MinChangeIntervalSeconds": 3,
    "BigJumpThreshold": 12,
    "BigJumpCooldownSeconds": 10,
    "LogOnChange": true,
    "InstantIdleDrop": true
  }
}
```

<details>
<summary>Why these settings?</summary>

This is a balanced, real-world tested config. Works well for medium to large sessions without stressing modern CPUs. Keeps responsiveness snappy, but still has smoothing and safety caps to prevent runaway tick rates.
</details>

---

### 3) Aggressive (high-end CPU / single busy world)

```json
{
  "version": "1.0.0",
  "values": {
    "Enable": true,
    "MinTickRate": 30,
    "MaxTickRate": 200,
    "AddedTicksPerUser": 0.9,
    "AddedTicksPerWorld": 2.5,
    "ActiveWorldUserThreshold": 1,
    "TopKWorlds": 3,
    "BusyWorldWeight": 2.0,
    "PerWorldUserSoftCap": 20,
    "PerWorldDiminish": 0.25,
    "JoinRateTicksPerJpm": 4.5,
    "JoinWindowSeconds": 22,
    "EmaAlpha": 0.35,
    "HysteresisTicks": 1,
    "MinChangeIntervalSeconds": 2,
    "BigJumpThreshold": 10,
    "BigJumpCooldownSeconds": 8,
    "LogOnChange": true,
    "InstantIdleDrop": true
  }
}
```

<details>
<summary>Why these settings?</summary>

Faster response, looser caps, and bigger surges for crowd spikes. Best for powerful hardware (dedicated CPUs) or single massive public worlds. Sacrifices efficiency for maximum smoothness and responsiveness.
</details>

