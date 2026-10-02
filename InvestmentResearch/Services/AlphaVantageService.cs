using System.Globalization;
using System.Text.Json;
using InvestmentResearch.Models;

namespace InvestmentResearch.Services;

/// <summary>
/// Market data from https://www.alphavantage.co. The free tier is limited to
/// 25 requests per day, so <see cref="MarketDataProvider"/> caches responses.
/// </summary>
public sealed class AlphaVantageService(string apiKey) : IMarketDataService
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://www.alphavantage.co/"),
        Timeout = TimeSpan.FromSeconds(20),
    };

    public async Task<StockQuote?> GetQuoteAsync(string symbol, CancellationToken ct = default)
    {
        using var doc = await QueryAsync("GLOBAL_QUOTE", $"symbol={Uri.EscapeDataString(symbol)}", ct).ConfigureAwait(false);
        if (!doc.RootElement.TryGetProperty("Global Quote", out var q) || !q.TryGetProperty("01. symbol", out _))
            return null;

        return new StockQuote(
            Str(q, "01. symbol"),
            Dec(q, "05. price") ?? 0,
            Dec(q, "09. change") ?? 0,
            Dec(q, "10. change percent") ?? 0,
            Dec(q, "02. open") ?? 0,
            Dec(q, "03. high") ?? 0,
            Dec(q, "04. low") ?? 0,
            Dec(q, "08. previous close") ?? 0,
            long.TryParse(Str(q, "06. volume"), out var vol) ? vol : 0,
            DateTime.TryParse(Str(q, "07. latest trading day"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var day) ? day : DateTime.Today);
    }

    public async Task<IReadOnlyList<PricePoint>> GetDailyHistoryAsync(string symbol, CancellationToken ct = default)
    {
        using var doc = await QueryAsync("TIME_SERIES_DAILY", $"symbol={Uri.EscapeDataString(symbol)}&outputsize=compact", ct).ConfigureAwait(false);
        if (!doc.RootElement.TryGetProperty("Time Series (Daily)", out var series))
            return [];

        var points = new List<PricePoint>();
        foreach (var day in series.EnumerateObject())
        {
            if (!DateTime.TryParseExact(day.Name, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                continue;
            var bar = day.Value;
            points.Add(new PricePoint(
                date,
                Dec(bar, "1. open") ?? 0,
                Dec(bar, "2. high") ?? 0,
                Dec(bar, "3. low") ?? 0,
                Dec(bar, "4. close") ?? 0,
                long.TryParse(Str(bar, "5. volume"), out var vol) ? vol : 0));
        }
        points.Sort((a, b) => a.Date.CompareTo(b.Date));
        return points;
    }

    public async Task<CompanyOverview?> GetOverviewAsync(string symbol, CancellationToken ct = default)
    {
        using var doc = await QueryAsync("OVERVIEW", $"symbol={Uri.EscapeDataString(symbol)}", ct).ConfigureAwait(false);
        var o = doc.RootElement;
        // ETFs and unknown symbols return an empty object.
        if (o.ValueKind != JsonValueKind.Object || !o.TryGetProperty("Symbol", out _))
            return null;

        return new CompanyOverview(
            Str(o, "Symbol"),
            Str(o, "Name"),
            Str(o, "Description"),
            Str(o, "Exchange"),
            Str(o, "Sector"),
            Str(o, "Industry"),
            Dec(o, "MarketCapitalization"),
            Dec(o, "PERatio"),
            Dec(o, "ForwardPE"),
            Dec(o, "EPS"),
            Dec(o, "DividendYield") * 100,
            Dec(o, "Beta"),
            Dec(o, "52WeekHigh"),
            Dec(o, "52WeekLow"),
            Dec(o, "ProfitMargin") * 100,
            Dec(o, "AnalystTargetPrice"));
    }

    public async Task<IReadOnlyList<SymbolMatch>> SearchAsync(string keywords, CancellationToken ct = default)
    {
        using var doc = await QueryAsync("SYMBOL_SEARCH", $"keywords={Uri.EscapeDataString(keywords)}", ct).ConfigureAwait(false);
        if (!doc.RootElement.TryGetProperty("bestMatches", out var matches))
            return [];

        return matches.EnumerateArray()
            .Select(m => new SymbolMatch(Str(m, "1. symbol"), Str(m, "2. name"), Str(m, "3. type"), Str(m, "4. region"), Str(m, "8. currency")))
            .ToList();
    }

    private async Task<JsonDocument> QueryAsync(string function, string parameters, CancellationToken ct)
    {
        var url = $"query?function={function}&{parameters}&apikey={Uri.EscapeDataString(apiKey)}";
        using var response = await Http.GetAsync(url, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

        // Alpha Vantage reports errors and rate limiting with HTTP 200 and one of these keys.
        if (doc.RootElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var key in new[] { "Error Message", "Note", "Information" })
            {
                if (doc.RootElement.TryGetProperty(key, out var message))
                {
                    var text = message.GetString() ?? key;
                    doc.Dispose();
                    throw new MarketDataException(text);
                }
            }
        }
        return doc;
    }

    private static string Str(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    private static decimal? Dec(JsonElement obj, string name) =>
        decimal.TryParse(Str(obj, name).TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;
}
