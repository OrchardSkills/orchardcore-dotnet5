using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OrchardCore.DynamicCache.Models;
using OrchardCore.Environment.Cache;

namespace OrchardCore.DynamicCache.Services
{
    public class DefaultDynamicCacheService : IDynamicCacheService
    {
        private readonly ICacheContextManager _cacheContextManager;
        private readonly IDynamicCache _dynamicCache;
        private readonly IServiceProvider _serviceProvider;
        private readonly CacheOptions _cacheOptions;

        private readonly Dictionary<string, string> _localCache = new Dictionary<string, string>();

        public DefaultDynamicCacheService(
            ICacheContextManager cacheContextManager,
            IDynamicCache dynamicCache,
            IServiceProvider serviceProvider,
            IOptions<CacheOptions> options)
        {
            _cacheContextManager = cacheContextManager;
            _dynamicCache = dynamicCache;
            _serviceProvider = serviceProvider;
            _cacheOptions = options.Value;
        }

        public async Task<string> GetCachedValueAsync(CacheContext context)
        {
            if (!_cacheOptions.Enabled)
            {
                return null;
            }

            var cacheKey = await GetCacheKey(context);

            context = await GetCachedContextAsync(cacheKey);
            if (context == null)
            {
                // We don't know the context, so we must treat this as a cache miss
                return null;
            }

            var content = await GetCachedStringAsync(cacheKey);

            return content;
        }

        public async Task SetCachedValueAsync(CacheContext context, string value)
        {
            if (!_cacheOptions.Enabled)
            {
                return;
            }

            var cacheKey = await GetCacheKey(context);

            _localCache[cacheKey] = value;
            var esi = JsonConvert.SerializeObject(CacheContextModel.FromCacheContext(context));

            await Task.WhenAll(
                SetCachedValueAsync(cacheKey, value, context),
                SetCachedValueAsync(GetCacheContextCacheKey(cacheKey), esi, context)
            );
        }

        public Task TagRemovedAsync(string tag, IEnumerable<string> keys)
        {
            return Task.WhenAll(keys.Select(key => _dynamicCache.RemoveAsync(key)));
        }

        private async Task SetCachedValueAsync(string cacheKey, string value, CacheContext context)
        {
            var bytes = Encoding.UTF8.GetBytes(value);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = context.ExpiresOn,
                SlidingExpiration = context.ExpiresSliding,
                AbsoluteExpirationRelativeToNow = context.ExpiresAfter
            };

            // Default duration is sliding expiration (permanent as long as it's used)
            if (!options.AbsoluteExpiration.HasValue && !options.SlidingExpiration.HasValue && !options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                options.SlidingExpiration = new TimeSpan(0, 1, 0);
            }

            await _dynamicCache.SetAsync(cacheKey, bytes, options);

            // Lazy load to prevent cyclic dependency
            var tagCache = _serviceProvider.GetRequiredService<ITagCache>();
            await tagCache.TagAsync(cacheKey, context.Tags.ToArray());
        }

        private async Task<string> GetCacheKey(CacheContext context)
        {
            var cacheEntries = context.Contexts.Count > 0
                ? await _cacheContextManager.GetDiscriminatorsAsync(context.Contexts)
                : Enumerable.Empty<CacheContextEntry>();

            if (!cacheEntries.Any())
            {
                return context.CacheId;
            }

            var key = context.CacheId + "/" + cacheEntries.GetContextHash();
            return key;
        }

        private string GetCacheContextCacheKey(string cacheKey)
        {
            return "cachecontext-" + cacheKey;
        }

        private async Task<string> GetCachedStringAsync(string cacheKey)
        {
            if (_localCache.TryGetValue(cacheKey, out var content))
            {
                return content;
            }

            var bytes = await _dynamicCache.GetAsync(cacheKey);
            if (bytes == null)
            {
                return null;
            }

            return Encoding.UTF8.GetString(bytes);
        }

        private async Task<CacheContext> GetCachedContextAsync(string cacheKey)
        {
            var cachedValue = await GetCachedStringAsync(GetCacheContextCacheKey(cacheKey));

            if (cachedValue == null)
            {
                return null;
            }

            var esiModel = JsonConvert.DeserializeObject<CacheContextModel>(cachedValue);
            return esiModel.ToCacheContext();
        }
    }
}
