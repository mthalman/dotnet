// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

/// <summary>
/// The inbuilt ASP.NET implementation of <see cref="HybridCache"/>.
/// </summary>
internal sealed class DefaultHybridCache : HybridCache
{
    private readonly IDistributedCache _backendCache;
    private readonly IServiceProvider _services;
    private readonly HybridCacheOptions _options;

    public DefaultHybridCache(IOptions<HybridCacheOptions> options, IDistributedCache backendCache, IServiceProvider services)
    {
        _backendCache = backendCache ?? throw new ArgumentNullException(nameof(backendCache));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _options = options.Value;
    }

    internal HybridCacheOptions Options => _options;

    public override ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> underlyingDataCallback, HybridCacheEntryOptions? options = null, IReadOnlyCollection<string>? tags = null, CancellationToken token = default)
        => underlyingDataCallback(state, token); // pass-thru without caching for initial API pass

    public override ValueTask RemoveKeyAsync(string key, CancellationToken token = default)
        => default; // no cache, nothing to remove

    public override ValueTask RemoveTagAsync(string tag, CancellationToken token = default)
        => default; // no cache, nothing to remove

    public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, IReadOnlyCollection<string>? tags = null, CancellationToken token = default)
        => default; // no cache, nothing to set

    internal IHybridCacheSerializer<T> GetSerializer<T>()
    {
        // unused API, primarily intended to show configuration is working;
        // the real version would memoize the result
        var service = _services.GetService<IHybridCacheSerializer<T>>();
        if (service is null)
        {
            foreach (var factory in _services.GetServices<IHybridCacheSerializerFactory>())
            {
                if (factory.TryCreateSerializer<T>(out var current))
                {
                    service = current;
                }
            }
        }
        return service ?? throw new InvalidOperationException("No serializer configured for type: " + typeof(T).Name);
    }
}
