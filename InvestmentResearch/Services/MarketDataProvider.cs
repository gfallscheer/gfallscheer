using System.Collections.Concurrent;
using InvestmentResearch.Models;

namespace InvestmentResearch.Services;

/// <summary>
/// The service the UI talks to. Uses Alpha Vantage when an API key is configured,
/// otherwise sample data, and caches results to stay within API rate limits.
/// </summary>
public sealed class MarketDataProvider(SettingsService settings) : IMarketDataService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly MockMarketDataService _mock = new();
    private readonly ConcurrentDictionary<string, (DateTime FetchedAt, object Value)> _cache = new();
    private AlphaVantageService? _alphaVantage;
    private string? _alphaVantageKey;

    public bool IsUsingSampleData => string.IsNullOrWhiteSpace(settings.Current.AlphaVantageApiKey);

    public Task<StockQuote?> GetQuoteAsync(string symbol, CancellationToken ct = default) =>
        CachedAsync($"quote:{symbol}", s => s.GetQuoteAsync(symbol, ct));

    public Task<IReadOnlyList<PricePoint>> GetDailyHistoryAsync(string symbol, CancellationToken ct = default) =>
        CachedAsync($"daily:{symbol}", s => s.GetDailyHistoryAsync(symbol, ct));

    public Task<CompanyOverview?> GetOverviewAsync(string symbol, CancellationToken ct = default) =>
        CachedAsync($"overview:{symbol}", s => s.GetOverviewAsync(symbol, ct));

    public Task<IReadOnlyList<SymbolMatch>> SearchAsync(string keywords, CancellationToken ct = default) =>
        CachedAsync($"search:{keywords.ToLowerInvariant()}", s => s.SearchAsync(keywords, ct));

    private IMarketDataService Current
    {
        get
        {
            var key = settings.Current.AlphaVantageApiKey?.Trim();
            if (string.IsNullOrEmpty(key))
                return _mock;
            if (_alphaVantage is null || _alphaVantageKey != key)
            {
                _alphaVantage = new AlphaVantageService(key);
                _alphaVantageKey = key;
                _cache.Clear();
            }
            return _alphaVantage;
        }
    }

    private async Task<T> CachedAsync<T>(string key, Func<IMarketDataService, Task<T>> fetch)
    {
        var service = Current;
        key = $"{(service == _mock ? "mock" : "av")}:{key.ToUpperInvariant()}";

        if (_cache.TryGetValue(key, out var entry) && DateTime.UtcNow - entry.FetchedAt < CacheDuration)
            return (T)entry.Value;

        var value = await fetch(service);
        if (value is not null)
            _cache[key] = (DateTime.UtcNow, value);
        return value;
    }
}
