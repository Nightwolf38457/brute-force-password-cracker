using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace BruteForce
{
    /// <summary>
    /// Logs and compares performance between single-thread and multi-thread brute force runs.
    /// </summary>
    public class PerformanceLogger
    {
        public class LogEntry
        {
            public string Mode { get; set; }
            public int ThreadCount { get; set; }
            public TimeSpan Elapsed { get; set; }
            public long AttemptsCount { get; set; }
            public string FoundPassword { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private readonly List<LogEntry> _entries = new List<LogEntry>();
        private Stopwatch _stopwatch = new Stopwatch();

        public void Start() => _stopwatch.Restart();
        public void Stop() => _stopwatch.Stop();

        public TimeSpan GetElapsedTime() => _stopwatch.Elapsed;

        /// <summary>
        /// Records a completed brute-force run result.
        /// </summary>
        public void LogResult(string mode, int threadCount, long attempts, string foundPassword)
        {
            _stopwatch.Stop();
            _entries.Add(new LogEntry
            {
                Mode = mode,
                ThreadCount = threadCount,
                Elapsed = _stopwatch.Elapsed,
                AttemptsCount = attempts,
                FoundPassword = foundPassword,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// Produces a formatted comparison string between single-thread and multi-thread results.
        /// </summary>
        public string GetComparisonReport()
        {
            if (_entries.Count == 0)
                return "No runs recorded yet.";

            var sb = new StringBuilder();
            sb.AppendLine("=== PERFORMANCE COMPARISON REPORT ===");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            LogEntry single = null, multi = null;
            foreach (var e in _entries)
            {
                if (e.Mode == "Single-Thread") single = e;
                if (e.Mode == "Multi-Thread") multi = e;
            }

            if (single != null)
            {
                sb.AppendLine("--- Single-Thread ---");
                sb.AppendLine($"  Time:     {single.Elapsed.TotalMilliseconds:F1} ms");
                sb.AppendLine($"  Attempts: {single.AttemptsCount:N0}");
                sb.AppendLine($"  Password: {single.FoundPassword}");
            }

            if (multi != null)
            {
                sb.AppendLine("--- Multi-Thread ---");
                sb.AppendLine($"  Threads:  {multi.ThreadCount}");
                sb.AppendLine($"  Time:     {multi.Elapsed.TotalMilliseconds:F1} ms");
                sb.AppendLine($"  Attempts: {multi.AttemptsCount:N0}");
                sb.AppendLine($"  Password: {multi.FoundPassword}");
            }

            if (single != null && multi != null && multi.Elapsed.TotalMilliseconds > 0)
            {
                double speedup = single.Elapsed.TotalMilliseconds / multi.Elapsed.TotalMilliseconds;
                sb.AppendLine();
                sb.AppendLine($"  Speedup (Single/Multi): {speedup:F2}x");
            }

            sb.AppendLine("=====================================");
            return sb.ToString();
        }

        public List<LogEntry> GetEntries() => new List<LogEntry>(_entries);
    }
}