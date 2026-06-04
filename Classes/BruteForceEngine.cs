using System;
using System.Threading;
using System.Threading.Tasks;

namespace BruteForce
{
    public class BruteForceEngine
    {
        private readonly BruteForceGenerator _generator;
        private readonly PasswordValidator _validator;
        private readonly PerformanceLogger _logger;

        public int ThreadCount { get; private set; }

        private CancellationTokenSource _cts;
        private volatile bool _found;
        private volatile string _foundPassword;
        private long _totalAttempts;

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

        public void StartMultiThread(string targetHash)
        {
            _cts = new CancellationTokenSource();
            _found = false;
            _foundPassword = null;
            _totalAttempts = 0;

            _logger.Start();

            // Split by LENGTH, not by total index
            // Each thread handles specific password lengths
            // Thread 0: length 1,2,3
            // Thread 1: length 4
            // Thread 2: length 5
            // Thread 3+: length 6 split into chunks

            Task[] tasks = new Task[ThreadCount];
            var token = _cts.Token;

            // lengths 1-3 on thread 0
            tasks[0] = Task.Run(() => SearchLength(1, 3, targetHash, token), token);

            // length 4 on thread 1 (if available)
            if (ThreadCount > 1)
                tasks[1] = Task.Run(() => SearchLength(4, 4, targetHash, token), token);

            // length 5 on thread 2 (if available)
            if (ThreadCount > 2)
                tasks[2] = Task.Run(() => SearchLength(5, 5, targetHash, token), token);

            // length 6 split across remaining threads
            int remaining = Math.Max(1, ThreadCount - 3);
            long len6total = Pow62(6);
            long chunk = len6total / remaining;

            for (int t = 0; t < remaining; t++)
            {
                long start = t * chunk;
                long end = (t == remaining - 1) ? len6total : start + chunk;
                int tt = t;
                int idx = Math.Min(3 + t, ThreadCount - 1);
                tasks[idx] = Task.Run(() => SearchLength6Range(start, end, targetHash, token), token);
            }

            // fill any unused task slots
            for (int i = 0; i < tasks.Length; i++)
                if (tasks[i] == null)
                    tasks[i] = Task.CompletedTask;

            Task.Run(() =>
            {
                try { Task.WaitAll(tasks); }
                catch (AggregateException) { }
                finally { FinishRun("Multi-Thread"); }
            });

            Task.Run(() => ReportProgress(_cts.Token));
        }

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
                SearchLength(1, 6, targetHash, token);
                FinishRun("Single-Thread");
            }, token);

            Task.Run(() => ReportProgress(_cts.Token));
        }

        private void SearchLength(int fromLen, int toLen, string targetHash, CancellationToken token)
        {
            for (int len = fromLen; len <= toLen; len++)
            {
                long total = Pow62(len);
                char[] buffer = new char[len];
                char[] charset = BruteForceGenerator.CHARSET;
                int b = charset.Length;

                for (long i = 0; i < total; i++)
                {
                    if (token.IsCancellationRequested || _found) return;

                    long idx = i;
                    for (int j = len - 1; j >= 0; j--)
                    {
                        buffer[j] = charset[idx % b];
                        idx /= b;
                    }

                    string candidate = new string(buffer);
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
        }

        private void SearchLength6Range(long start, long end, string targetHash, CancellationToken token)
        {
            char[] charset = BruteForceGenerator.CHARSET;
            int b = charset.Length;
            char[] buffer = new char[6];

            for (long i = start; i < end; i++)
            {
                if (token.IsCancellationRequested || _found) return;

                long idx = i;
                for (int j = 5; j >= 0; j--)
                {
                    buffer[j] = charset[idx % b];
                    idx /= b;
                }

                string candidate = new string(buffer);
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

        private long Pow62(int exp)
        {
            long result = 1;
            for (int i = 0; i < exp; i++) result *= 62;
            return result;
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

        public void Stop()
        {
            _cts?.Cancel();
            OnStopped?.Invoke("Stopped by user.");
        }

        public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;
    }
}