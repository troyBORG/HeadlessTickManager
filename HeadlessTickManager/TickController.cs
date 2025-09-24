using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Core; // MathX.Clamp
using FrooxEngine;
using ResoniteModLoader;

namespace HeadlessTickManager;

public sealed class TickTuning
{
    // Hard caps
    public int MinTickRate = 30;    // recommended floor
    public int MaxTickRate = 144;   // safe ceiling; raise if you have CPU headroom

    // Linear-ish parts
    public float AddedTicksPerUser = 0.6f;        // each non-host user (weighted)
    public float AddedTicksPerExtraWorld = 1.5f;  // per extra ACTIVE world beyond the first

    // What counts as "active world"?
    public int ActiveWorldUserThreshold = 1;      // >=1 non-host user -> active

    // Busy-world shaping
    public int   TopKWorlds = 2;                  // focus on top N busiest worlds
    public float BusyWorldWeight = 1.75f;         // boost the busiest worlds
    public int   PerWorldUserSoftCap = 16;        // beyond this, marginal users diminish
    public float PerWorldDiminish = 0.35f;        // weight for users above soft cap (0..1)

    // Burst-join shaping
    public float JoinRateTicksPerJpm = 4.0f;      // tick boost per join-per-minute
    public int   JoinWindowSeconds = 45;          // shorter window = more responsive

    public int JoinRateMaxBonusTicks = 40;        // safety clamp for join-burst scaling


    // Stability
    public float EmaAlpha = 0.22f;                // 0..1 (higher = more reactive)
    public int   HysteresisTicks = 2;             // ignore tiny differences
    public int   MinChangeIntervalSeconds = 5;
    public int   BigJumpThreshold = 12;           // large changes trigger cooldown
    public int   BigJumpCooldownSeconds = 12;

    // Logging
    public bool LogOnChange = true;

    // NEW: snap to Min immediately when no active worlds
    public bool InstantIdleDrop = false;
}

public sealed class TickController
{
    private readonly StandaloneFrooxEngineRunner runner;
    private readonly TickTuning T;

    // Track non-host users per world
    private readonly Dictionary<World, int> nonHostUsers = new();

    // Recent join timestamps (UTC)
    private readonly Queue<DateTime> recentJoins = new();

    // Smoothing / application state
    private double emaTick;
    private int lastAppliedTick;
    private DateTime lastChangeAt = DateTime.MinValue;
    private DateTime cooldownUntil = DateTime.MinValue;

    private readonly object gate = new();

    public TickController(StandaloneFrooxEngineRunner runner, TickTuning tuning, int initialTick)
    {
        this.runner = runner;
        this.T = tuning;

        emaTick = initialTick;
        lastAppliedTick = initialTick;
        lastChangeAt = DateTime.UtcNow;
    }

    public void OnWorldAdded(World w)
    {
        lock (gate)
        {
            if (!nonHostUsers.ContainsKey(w))
            {
                var nh = Math.Max(0, w.UserCount - 1); // host excluded
                nonHostUsers[w] = nh;
                Recompute_NoLock();
            }
        }
    }

    public void OnWorldRemoved(World w)
    {
        lock (gate)
        {
            if (nonHostUsers.Remove(w))
                Recompute_NoLock();
        }
    }

    public void OnUserJoin(World w)
    {
        lock (gate)
        {
            if (!nonHostUsers.ContainsKey(w))
                nonHostUsers[w] = 0;

            nonHostUsers[w] = Math.Max(0, nonHostUsers[w] + 1);

            var now = DateTime.UtcNow;
            recentJoins.Enqueue(now);
            TrimJoinWindow_NoLock(now);

            Recompute_NoLock();
        }
    }

    public void OnUserLeave(World w)
    {
        lock (gate)
        {
            if (!nonHostUsers.ContainsKey(w))
                nonHostUsers[w] = 0;

            nonHostUsers[w] = Math.Max(0, nonHostUsers[w] - 1);
            Recompute_NoLock();
        }
    }

    private void TrimJoinWindow_NoLock(DateTime now)
    {
        var window = TimeSpan.FromSeconds(Math.Max(1, T.JoinWindowSeconds));
        while (recentJoins.Count > 0 && (now - recentJoins.Peek()) > window)
            recentJoins.Dequeue();
    }

    private void Recompute_NoLock()
    {
        var now = DateTime.UtcNow;

        // Snapshot (non-host counts)
        var perWorldAll = nonHostUsers.Values.Where(v => v >= 0).ToList();
        var perWorldActive = perWorldAll.Where(v => v >= T.ActiveWorldUserThreshold).ToList();

        // === Idle branch: no active worlds
        if (perWorldActive.Count == 0)
        {
            TrimJoinWindow_NoLock(now);

            int candidateIdle;
            if (T.InstantIdleDrop)
            {
                // Snap to Min immediately and sync EMA so it doesn't bounce back up
                candidateIdle = T.MinTickRate;
                emaTick = T.MinTickRate;
            }
            else
            {
                // Glide smoothly toward Min
                emaTick = T.EmaAlpha * T.MinTickRate + (1.0 - T.EmaAlpha) * emaTick;
                candidateIdle = (int)Math.Round(MathX.Clamp(emaTick, T.MinTickRate, T.MaxTickRate));
            }

            // Respect cooldown/hysteresis
            if (now >= cooldownUntil &&
                (Math.Abs(candidateIdle - lastAppliedTick) >= T.HysteresisTicks ||
                 (now - lastChangeAt).TotalSeconds >= T.MinChangeIntervalSeconds))
            {
                lastAppliedTick = candidateIdle;
                lastChangeAt = now;
                runner.TickRate = lastAppliedTick;

                if (T.LogOnChange)
                    Msg($"{lastAppliedTick} ticks (idle; activeWorlds=0)");
            }
            return;
        }

        // === Active branch: only consider ACTIVE worlds
        perWorldActive.Sort((a, b) => b.CompareTo(a)); // busiest first

        int activeWorldCount = perWorldActive.Count;
        int extraActiveWorlds = Math.Max(0, activeWorldCount - 1);

        // Busy-world weighting with diminishing returns
        double busyContribution = 0.0;
        for (int i = 0; i < perWorldActive.Count; i++)
        {
            int users = perWorldActive[i];
            int baseUsers = Math.Min(users, T.PerWorldUserSoftCap);
            int overflow = Math.Max(0, users - T.PerWorldUserSoftCap);
            double weightedUsers = baseUsers + overflow * T.PerWorldDiminish;

            double weight = (i < T.TopKWorlds) ? T.BusyWorldWeight : 1.0;
            busyContribution += weight * weightedUsers;
        }

        double userTicks  = busyContribution * T.AddedTicksPerUser;
        double worldTicks = extraActiveWorlds * T.AddedTicksPerExtraWorld;

        // Join-rate bump
        TrimJoinWindow_NoLock(now);
        double joinsPerMinute = recentJoins.Count * (60.0 / Math.Max(1, T.JoinWindowSeconds));
        double joinTicks = joinsPerMinute * T.JoinRateTicksPerJpm;
        joinTicks = Math.Min(joinTicks, T.JoinRateMaxBonusTicks);

        // Raw target and clamp
        double raw = T.MinTickRate + userTicks + worldTicks + joinTicks;
        raw = MathX.Clamp(raw, T.MinTickRate, T.MaxTickRate);

        // EMA smoothing
        emaTick = T.EmaAlpha * raw + (1.0 - T.EmaAlpha) * emaTick;
        int candidate = (int)Math.Round(emaTick);

        // Cooldown / hysteresis
        if (now < cooldownUntil) return;

        if (Math.Abs(candidate - lastAppliedTick) < T.HysteresisTicks &&
            (now - lastChangeAt).TotalSeconds < T.MinChangeIntervalSeconds)
            return;

        int delta = candidate - lastAppliedTick;
        lastAppliedTick = candidate;
        lastChangeAt = now;
        runner.TickRate = lastAppliedTick;

       if (T.LogOnChange)
    {
        Msg(
            $"Applied {lastAppliedTick} ticks " +
            $"(raw={raw:F1}, ema={emaTick:F1}, activeWorlds={activeWorldCount}, joins/min={joinsPerMinute:F2})"
        );
    }


        if (Math.Abs(delta) >= T.BigJumpThreshold)
            cooldownUntil = now.AddSeconds(T.BigJumpCooldownSeconds);
    }
}
