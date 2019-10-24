using Microsoft.Extensions.Caching.Memory;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DnsClient
{
    internal class ResponseCache
    {
        public ResponseCache(bool enabled = true, TimeSpan? minimumTimout = null, IMemoryCache memoryCache = null)
        {
            Enabled = enabled;
            MinimumTimout = minimumTimout;

            if(memoryCache != null)
            {
                cachePrefix = "DnsResponseCache.";
                _cache = memoryCache;
            }

            else
            {
                _cache = new MemoryCache(new MemoryCacheOptions
                {
                });
            }
        }

        private static readonly TimeSpan s_infiniteTimeout = Timeout.InfiniteTimeSpan;

        // max is 24 days
        private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

        private readonly string cachePrefix = string.Empty;
        private readonly IMemoryCache _cache;
        private TimeSpan? _minimumTimeout;

        public bool Enabled { get; set; } = true;

        public TimeSpan? MinimumTimout
        {
            get { return _minimumTimeout; }
            set
            {
                if (value.HasValue &&
                    (value < TimeSpan.Zero || value > s_maxTimeout) && value != s_infiniteTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _minimumTimeout = value;
            }
        }

        public string GetKey(DnsQuestion question, NameServer server)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            return string.Concat(cachePrefix, server.Address.ToString(), "#", server.Port.ToString(), "_", question.QueryName.Value, ":", (short)question.QuestionClass, ":", (short)question.QuestionType);
        }

        public IDnsQueryResponse GetOrCreate(Func<string> keyFactory, Func<IDnsQueryResponse> factory, bool bypass = false)
        {
            if (!Enabled || bypass)
                return factory();

            var key = keyFactory();

            if (key == null)
                throw new ArgumentNullException(key);

            if (_cache.TryGetValue(key, out ResponseEntry entry))
                return entry.Response;

            var response = factory();

            entry = CreateCacheEntry(response);

            if (entry != null)
            {
                using var cacheEntry = _cache.CreateEntry(key);
                cacheEntry.SlidingExpiration = TimeSpan.FromMilliseconds(entry.TTL);
                cacheEntry.SetValue(entry);
            }

            return response;
        }

        public async Task<IDnsQueryResponse> GetOrCreateAsync(Func<string> keyFactory, Func<Task<IDnsQueryResponse>> factory, bool bypass = false)
        {
            if (!Enabled || bypass)
                return await factory();

            var key = keyFactory();

            if (key == null)
                throw new ArgumentNullException(key);

            if (_cache.TryGetValue(key, out ResponseEntry entry))
                return entry.Response;

            var response = await factory();

            entry = CreateCacheEntry(response);

            if (entry != null)
            {
                using var cacheEntry = _cache.CreateEntry(key);
                cacheEntry.SlidingExpiration = TimeSpan.FromMilliseconds(entry.TTL);
                cacheEntry.SetValue(entry);
            }

            return response;
        }

        private ResponseEntry CreateCacheEntry(IDnsQueryResponse response)
        {
            if (response != null && !response.HasError)
            {
                var all = response.AllRecords;

                if (all.Any())
                {
                    // in millis
                    double minTtl = all.Min(p => p.InitialTimeToLive) * 1000d;

                    if (MinimumTimout == Timeout.InfiniteTimeSpan)
                        minTtl = s_maxTimeout.TotalMilliseconds;
                    else if (MinimumTimout.HasValue && minTtl < MinimumTimout.Value.TotalMilliseconds)
                        minTtl = (long)MinimumTimout.Value.TotalMilliseconds;

                    if (minTtl < 1d)
                        return null;

                    return new ResponseEntry(response, minTtl);
                }
            }

            return null;
        }

        private class ResponseEntry
        {
            public bool IsExpiredFor(DateTimeOffset forDate) => forDate >= ExpiresAt;

            public DateTimeOffset ExpiresAt { get; }

            public DateTimeOffset Created { get; }

            public double TTL { get; set; }

            public IDnsQueryResponse Response { get; }

            public ResponseEntry(IDnsQueryResponse response, double ttlInMS)
            {
                Debug.Assert(response != null);
                Debug.Assert(ttlInMS >= 0);

                Response = response;
                TTL = ttlInMS;
                Created = DateTimeOffset.UtcNow;
                ExpiresAt = Created.AddMilliseconds(TTL);
            }
        }
    }
}