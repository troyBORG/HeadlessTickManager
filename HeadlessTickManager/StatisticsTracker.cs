using System;
using System.Collections.Generic;
using System.Linq;

namespace HeadlessTickManager;

/// <summary>
/// Tracks statistics about tick rate behavior over time for monitoring and health checks.
/// </summary>
public sealed class StatisticsTracker
{
    private readonly object gate = new();
    
    // Tick rate history (rolling window)
    private readonly Queue<int> tickHistory = new();
    private readonly int maxHistorySize = 300; // ~5 minutes at 1 sample/second
    
    // Aggregates
    private int peakTickRate = 0;
    private int minTickRate = int.MaxValue;
    private DateTime peakTickTime = DateTime.MinValue;
    private DateTime minTickTime = DateTime.MinValue;
    
    // Time tracking
    private DateTime lastSummaryTime = DateTime.UtcNow;
    
    // Counters
    private int tickChangeCount = 0;
    private int totalSamples = 0;
    
    public void RecordTick(int tickRate)
    {
        lock (gate)
        {
            totalSamples++;
            
            // Update peak/min
            if (tickRate > peakTickRate)
            {
                peakTickRate = tickRate;
                peakTickTime = DateTime.UtcNow;
            }
            if (tickRate < minTickRate)
            {
                minTickRate = tickRate;
                minTickTime = DateTime.UtcNow;
            }
            
            // Maintain rolling history
            tickHistory.Enqueue(tickRate);
            while (tickHistory.Count > maxHistorySize)
                tickHistory.Dequeue();
        }
    }
    
    public void RecordTickChange()
    {
        lock (gate)
        {
            tickChangeCount++;
        }
    }
    
    public StatisticsSnapshot GetSnapshot()
    {
        lock (gate)
        {
            var now = DateTime.UtcNow;
            var history = tickHistory.ToList();
            
            return new StatisticsSnapshot
            {
                CurrentTick = history.Count > 0 ? history.Last() : 0,
                AverageTick = history.Count > 0 ? (double)history.Sum() / history.Count : 0,
                PeakTick = peakTickRate,
                PeakTickTime = peakTickTime,
                MinTick = minTickRate == int.MaxValue ? 0 : minTickRate,
                MinTickTime = minTickTime == DateTime.MinValue ? DateTime.UtcNow : minTickTime,
                TickChangesPerHour = totalSamples > 0 ? (tickChangeCount * 3600.0) / Math.Max(1, (now - lastSummaryTime).TotalSeconds) : 0,
                TimeAtMax = GetTimeAtMax(),
                TimeAtMin = GetTimeAtMin(),
                SampleCount = totalSamples,
                HistorySize = history.Count
            };
        }
    }
    
    private DateTime lastMaxTickTime = DateTime.MinValue;
    private DateTime lastMinTickTime = DateTime.MinValue;
    
    public void UpdateMaxMinTracking(int currentTick, int minTick, int maxTick)
    {
        lock (gate)
        {
            var now = DateTime.UtcNow;
            if (currentTick >= maxTick)
                lastMaxTickTime = now;
            if (currentTick <= minTick)
                lastMinTickTime = now;
        }
    }
    
    public double GetTimeAtMax()
    {
        lock (gate)
        {
            if (lastMaxTickTime == DateTime.MinValue) return 0;
            return (DateTime.UtcNow - lastMaxTickTime).TotalSeconds;
        }
    }
    
    public double GetTimeAtMin()
    {
        lock (gate)
        {
            if (lastMinTickTime == DateTime.MinValue) return 0;
            return (DateTime.UtcNow - lastMinTickTime).TotalSeconds;
        }
    }
    
    public void ResetSummaryTime()
    {
        lock (gate)
        {
            lastSummaryTime = DateTime.UtcNow;
            tickChangeCount = 0;
        }
    }
}

public sealed class StatisticsSnapshot
{
    public int CurrentTick { get; set; }
    public double AverageTick { get; set; }
    public int PeakTick { get; set; }
    public DateTime PeakTickTime { get; set; }
    public int MinTick { get; set; }
    public DateTime MinTickTime { get; set; }
    public double TickChangesPerHour { get; set; }
    public double TimeAtMax { get; set; }
    public double TimeAtMin { get; set; }
    public int SampleCount { get; set; }
    public int HistorySize { get; set; }
}

