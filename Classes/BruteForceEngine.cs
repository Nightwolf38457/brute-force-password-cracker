using System;
using System.Threading;
using System.Threading.Tasks;

namespace BruteForce
{
    /// <summary>
    /// Core brute-force engine. Supports both single-thread and multi-thread (CPU-1 threads) modes.
    /// Uses CancellationToken to stop all threads immediately upon finding the password.
    /// </summary>
    public class BruteForceEngine
    {
        private readonly BruteForceGenerator _generator;
        private readonly PasswordValidator _validator;
        private readonly PerformanceLogger _logger;

        public int ThreadCount { get; private set; }

        private CancellationTokenSource _cts;
        private volatile bool _found;
        private string _foundPassword;
        private long _totalAttempts;

        // Events for GUI updates
        public event Action<long, string> OnProgress;
        public event Action<string, string> OnFound;
        public event Action<string> OnStopped;

        public BruteForceEngine(BruteForceGenerator generator, PasswordValidator validator, PerformanceLogger logger)
        {
            _generator = generator;
            _validator = validator;
            _logger = logger;
            ThreadCount = Math.Max(1, Environment.ProcessorCount - 1);
        }

        /// <summary>
        /// Starts multi-threaded brute force. Each thread handles a partition
        /// of the total search space demonstrating true parallel execution.
        /// </summary>
        public void StartMultiThread(string targetHash)
        {
            _cts = new CancellationTokenSource();
            _found = false;
            _foundPassword = null;
            _totalAttempts = 0;

            _logger.Start();
            long total = _generator.GetTotalCombinations();
            long chunkSize = total / ThreadCount;

            Task[] tasks = new Task[ThreadCount];
            for (int t = 0; t < ThreadCount; t++)
            {
                long start = t * chunkSize;
                long end = (t == ThreadCount - 1) ? total : start + chunkSize;
                var token = _cts.Token;

                tasks[t] = Task.Run(() => SearchRange(start, end, targetHash, token), token);
            }

            Task.Run(() =>
            {
                try { Task.WaitAll(tasks); }
                catch (AggregateException) { }
                finally { FinishRun("Multi-Thread"); }
            });

            Task.Run(() => ReportProgress(_cts.Token));
        }

        /// <summary>
        /// Starts single-thread brute force sequentially for performance comparison.
        /// </summary>
        public void StartSingleThread(string targetHash)
        {
            _cts = new CancellationTokenSource();
            _found = false;
            _foundPassword = null;
            _totalAttempts = 0;

            _logger.Start();
            var token = _cts.Token;

            Task.Run(() =>
            {
                SearchRange(0, _generator.GetTotalCombinations(), targetHash, token);
                FinishRun("Single-Thread");
            }, token);

            Task.Run(() => ReportProgress(_cts.Token));
        }

        /// <summary>
        /// Searches the index range [start, end) for a matching combination.
        /// </summary>
        private void SearchRange(long start, long end, string targetHash, CancellationToken token)
        {
            for (long i = start; i < end; i++)
            {
                if (token.IsCancellationRequested || _found) return;

                string candidate = _generator.GetCombinationAt(i);
                if (candidate == null) return;

                Interlocked.Increment(ref _totalAttempts);

                if (_validator.Validate(candidate, targetHash))
                {
                    _found = true;
                    _foundPassword = candidate;
                    _cts.Cancel();
                    return;
                }
            }
        }

        private void FinishRun(string mode)
        {
            string result = _found ? _foundPassword : "(not found)";
            _logger.LogResult(mode, ThreadCount, Interlocked.Read(ref _totalAttempts), result);
            string report = _logger.GetComparisonReport();
            OnFound?.Invoke(_foundPassword, report);
        }

        private void ReportProgress(CancellationToken token)
        {
            while (!token.IsCancellationRequested && !_found)
            {
                long attempts = Interlocked.Read(ref _totalAttempts);
                string elapsed = _logger.GetElapsedTime().ToString(@"mm\:ss\.ff");
                OnProgress?.Invoke(attempts, elapsed);
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Stops all running threads immediately.
        /// </summary>
        public void Stop()
        {
            _cts?.Cancel();
            OnStopped?.Invoke("Stopped by user.");
        }

        public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;
    }
}