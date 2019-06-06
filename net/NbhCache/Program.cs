using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NbhCache
{
    class Program
    {
        static async Task Main(string[] args)
        {
            int[][] testMatrix = new int[][]
            {
                new int[] { 1, }
            };
            string[] keys = new string[200];

            for(int i = 0; i < keys.Length; i++)
            {
                keys[i] = Guid.NewGuid().ToString();
            }

            await TestCache(keys, new NonBlockingHitCache());

            await TestCache(keys, new SimpleLockCache());
        }

        private const int TotalThreads = 4;

        private static async Task TestCache(int numKeys, int numThreads, int expFreqSec, Cache cache)
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

            while (sw.ElapsedMilliseconds < 30000)
            {
                await threadGate.WaitAsync();

                var _ = cache.GetValueAsync(keys[rand.Next() % keys.Length]).ContinueWith((t) => threadGate.Release());
            }

            for (int i = 0; i < numThreads; i++)
            {
                await threadGate.WaitAsync();
            }

            Console.WriteLine(cache.GetType().Name);

            Console.WriteLine($"avg hit ticks:  {cache._totalHitTicks / cache._totalHit} total hit ms: {new TimeSpan(cache._totalHitTicks).TotalMilliseconds}");

            Console.WriteLine($"avg miss ticks: {cache._totalMissTicks / cache._totalMiss} total hit ms: {new TimeSpan(cache._totalMissTicks).TotalMilliseconds}");

            Console.WriteLine($"total ms: {new TimeSpan(cache._totalHitTicks + cache._totalMissTicks).TotalMilliseconds}");
        }
    }

    class NonBlockingHitCache : Cache
    {
        private Dictionary<string, CacheEntry> _primary = new Dictionary<string, CacheEntry>();
        private Dictionary<string, CacheEntry> _secondary = new Dictionary<string, CacheEntry>();
        private SemaphoreSlim _lock = new SemaphoreSlim(1, 1); 

        protected override async Task<bool> GetValueCoreAsync(string key)
        {
            if(!_primary.TryGetValue(key, out CacheEntry entry) || entry.expiry < DateTimeOffset.UtcNow)
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
                if(!_secondary.TryGetValue(key, out CacheEntry entry) || entry.expiry < DateTimeOffset.UtcNow)
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

    class SimpleLockCache : Cache
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

    abstract class Cache
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
