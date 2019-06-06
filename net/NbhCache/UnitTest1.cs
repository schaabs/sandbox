using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
    public class Tests
    {
        StreamWriter _outfile;
        public Tests()
        {
            
        }

        [OneTimeSetUp]
        public async Task Setup()
        {
            _outfile = new StreamWriter(@"d:\temp\nbf_analysis.csv");

            await _outfile.WriteLineAsync("Type,Avg Hit Time (ticks),Avg Miss Time (ticks),Total Hit Time (ms),Total Miss Time (ms), Total Requests Time (ms),Total Hits,Total Misses,Total Requests");
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            await _outfile.FlushAsync();

            _outfile.Close();

            _outfile.Dispose();
        }


        [NonParallelizable]
        [Test]
        [Combinatorial]
        public async Task TestNbh(
            [Values(1, 4, 16, 64)]int numKeys,
            [Values(1, 4, 16, 64)]int numThreads,
            [Values(5, 10, 30)]int expFreqSec)
        {
            await TestCache(numKeys, numThreads, expFreqSec, new NonBlockingHitCache());
            Assert.Pass();
        }

        [NonParallelizable]
        [Test]
        [Combinatorial]
        public async Task TestSimple(
            [Values(1, 4, 16, 64)]int numKeys,
            [Values(1, 4, 16, 64)]int numThreads,
            [Values(5, 10, 30)]int expFreqSec)
        {
            await TestCache(numKeys, numThreads, expFreqSec, new SimpleLockCache());
            Assert.Pass();
        }

        public async Task TestCache(int numKeys, int numThreads, int expFreqSec, Cache cache)
        {
            SemaphoreSlim threadGate = new SemaphoreSlim(numThreads, numThreads);

            Random rand = new Random(929293);

            cache._expSec = Convert.ToDouble(expFreqSec);

            string[] keys = new string[numKeys];

            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = Guid.NewGuid().ToString();
            }

            Stopwatch sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < ?120000?)
            {
                await threadGate.WaitAsync();

                var _ = cache.GetValueAsync(keys[rand.Next() % keys.Length]).ContinueWith((t) => threadGate.Release());
            }

            for (int i = 0; i < numThreads; i++)
            {
                await threadGate.WaitAsync();
            }

            long avgHitTicks = cache._totalHitTicks / cache._totalHit;

            long avgMissTicks = cache._totalMissTicks / cache._totalMiss;

            double totalHitMs = new TimeSpan(cache._totalHitTicks).TotalMilliseconds;

            double totalMissMs = new TimeSpan(cache._totalMissTicks).TotalMilliseconds;

            double totalMs = new TimeSpan(cache._totalHitTicks + cache._totalMissTicks).TotalMilliseconds;

            long totalHits = cache._totalHit;

            long totalMisses = cache._totalMiss;

            long totalReq = cache._totalHit + cache._totalMiss;

            string line = $"{cache.GetType().Name},{avgHitTicks},{avgMissTicks},{totalHitMs},{totalMissMs},{totalMs},{totalHits},{totalMisses},{totalReq}";

            await _outfile.WriteLineAsync(line);

            Console.WriteLine(line);
        }

        public class NonBlockingHitCache : Cache
        {
            private Dictionary<string, CacheEntry> _primary = new Dictionary<string, CacheEntry>();
            private Dictionary<string, CacheEntry> _secondary = new Dictionary<string, CacheEntry>();
            private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

            protected override async Task<bool> GetValueCoreAsync(string key)
            {
                if (!_primary.TryGetValue(key, out CacheEntry entry) || entry.expiry < DateTimeOffset.UtcNow)
                {
                    entry = await HandleMiss(key);

                    return false;
                }

                return true;
            }

            private async Task<CacheEntry> HandleMiss(string key)
            {
                await _lock.WaitAsync();

                try
                {
                    if (!_secondary.TryGetValue(key, out CacheEntry entry) || entry.expiry < DateTimeOffset.UtcNow)
                    {
                        entry = new CacheEntry(_expSec);

                        _secondary[key] = entry;

                        _primary = _secondary;

                        _secondary = new Dictionary<string, CacheEntry>(_secondary);
                    }

                    return entry;
                }
                finally
                {
                    _lock.Release();
                }
            }
        }

        public class SimpleLockCache : Cache
        {
            private Dictionary<string, CacheEntry> _primary = new Dictionary<string, CacheEntry>();

            private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

            protected override async Task<bool> GetValueCoreAsync(string key)
            {
                await _lock.WaitAsync();

                try
                {
                    if (!_primary.TryGetValue(key, out CacheEntry entry) || entry.expiry < DateTimeOffset.UtcNow)
                    {
                        entry = new CacheEntry(_expSec);

                        _primary[key] = entry;

                        return false;
                    }

                    return true;
                }
                finally
                {
                    _lock.Release();
                }
            }
        }

        public abstract class Cache
        {
            public long _totalHit = 0;
            public long _totalHitTicks = 0;
            public long _totalMiss = 0;
            public long _totalMissTicks;
            public double _expSec = 5;

            public async Task GetValueAsync(string key)
            {
                Stopwatch sw = Stopwatch.StartNew();

                bool hit = await GetValueCoreAsync(key);

                sw.Stop();

                if (hit)
                {
                    Interlocked.Increment(ref _totalHit);

                    Interlocked.Add(ref _totalHitTicks, sw.ElapsedTicks);
                }
                else
                {
                    Interlocked.Increment(ref _totalMiss);

                    Interlocked.Add(ref _totalMissTicks, sw.ElapsedTicks);
                }
            }

            protected abstract Task<bool> GetValueCoreAsync(string key);
        }

        class CacheEntry
        {
            public CacheEntry(double expSec)
            {
                expiry = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(expSec);
                value = Guid.NewGuid().ToString();
            }
            public string value;
            public DateTimeOffset expiry;
        }
    }
}