using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Core.Application.Pipelines.Caching;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>, ICachableRequest
{
    private readonly CacheSettings _cacheSettings;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(IDistributedCache cache, IConfiguration configuration, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cacheSettings = configuration.GetSection("CacheSettings").Get<CacheSettings>() ?? throw new InvalidOperationException();
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request.ByPassCache)
        {
            return await next();
        }

        TResponse response;
        byte[]? cachedResponse = await _cache.GetAsync(request.CacheKey, cancellationToken); // cache'den okuyoruz.
        if (cachedResponse != null)  // eğer cachede data varsa
        {
            response = JsonSerializer.Deserialize<TResponse>(Encoding.Default.GetString(cachedResponse)); // cachedaki datayı deserialize yapalım
            _logger.LogInformation($"Fetched from Cache -> {request.CacheKey}");
        }
        else  // cachede data yoksa , hem cache e eklememiz ve döndürmemiz lazım 
        {
            response = await getResponseAndAddToCache(request, next, cancellationToken);
        }
        return response;
    }

    /// <summary>
    /// Cache'de veri yoksa ilgili metodu çalıştırarak veritabanından veriyi çeker ve cache ' e yazar.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="next"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<TResponse?> getResponseAndAddToCache(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // veritabanından datayı alacağız ve cache ' e ekleyeceğiz.  
        TResponse response = await next();
        TimeSpan slidingExpiration = request.SlidingExpiration ?? TimeSpan.FromDays(_cacheSettings.SlidingExpiration);
        DistributedCacheEntryOptions cacheOptions = new()
        {
            SlidingExpiration = slidingExpiration,
        };

        byte[] serializeData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));

        await _cache.SetAsync(request.CacheKey, serializeData, cacheOptions, cancellationToken);
        _logger.LogInformation($"Added to Cache: {request.CacheKey}");

        // Eğer istek içerisinde cacheGroupKey varsa, gidip cache group'una da cache i yazmamız gerekir. 
        if (request.CacheGroupKey != null)
            await addCacheKeyToGroup(request, slidingExpiration, cancellationToken);

        return response;

    }


    // Cache GroupKey - cacheKeys[]
    private async Task addCacheKeyToGroup(TRequest request, TimeSpan slidingExpiration, CancellationToken cancellationToken)
    {
        byte[]? cacheGroupCache = await _cache.GetAsync(key: request.CacheGroupKey!, cancellationToken);
        HashSet<string> cacheKeysInGroup;
        if (cacheGroupCache != null)
        {
            cacheKeysInGroup = JsonSerializer.Deserialize<HashSet<string>>(Encoding.Default.GetString(cacheGroupCache))!;
            if (!cacheKeysInGroup.Contains(request.CacheKey))
                cacheKeysInGroup.Add(request.CacheKey);
        }
        else
            cacheKeysInGroup = new HashSet<string>(new[] { request.CacheKey });
        byte[] newCacheGroupCache = JsonSerializer.SerializeToUtf8Bytes(cacheKeysInGroup);

        byte[]? cacheGroupCacheSlidingExpirationCache = await _cache.GetAsync(
            key: $"{request.CacheGroupKey}SlidingExpiration",
            cancellationToken
        );
        int? cacheGroupCacheSlidingExpirationValue = null;
        if (cacheGroupCacheSlidingExpirationCache != null)
            cacheGroupCacheSlidingExpirationValue = Convert.ToInt32(Encoding.Default.GetString(cacheGroupCacheSlidingExpirationCache));
        if (cacheGroupCacheSlidingExpirationValue == null || slidingExpiration.TotalSeconds > cacheGroupCacheSlidingExpirationValue)
            cacheGroupCacheSlidingExpirationValue = Convert.ToInt32(slidingExpiration.TotalSeconds);
        byte[] serializeCachedGroupSlidingExpirationData = JsonSerializer.SerializeToUtf8Bytes(cacheGroupCacheSlidingExpirationValue);

        DistributedCacheEntryOptions cacheOptions =
            new() { SlidingExpiration = TimeSpan.FromSeconds(Convert.ToDouble(cacheGroupCacheSlidingExpirationValue)) };

        await _cache.SetAsync(key: request.CacheGroupKey!, newCacheGroupCache, cacheOptions, cancellationToken);
        _logger.LogInformation($"Added to Cache -> {request.CacheGroupKey}");

        await _cache.SetAsync(
            key: $"{request.CacheGroupKey}SlidingExpiration",
            serializeCachedGroupSlidingExpirationData,
            cacheOptions,
            cancellationToken
        );
        _logger.LogInformation($"Added to Cache -> {request.CacheGroupKey}SlidingExpiration");
    }


}

