using InvestmentResearch.Models;

namespace InvestmentResearch.Services;

/// <summary>
/// Deterministic, offline sample data so the app works without an API key.
/// The same symbol always produces the same price history.
/// </summary>
public sealed class MockMarketDataService : IMarketDataService
{
    private static readonly (string Symbol, string Name, string Sector, string Industry)[] Companies =
    [
        ("AAPL", "Apple Inc.", "Technology", "Consumer Electronics"),
        ("MSFT", "Microsoft Corporation", "Technology", "Software - Infrastructure"),
        ("NVDA", "NVIDIA Corporation", "Technology", "Semiconductors"),
        ("GOOGL", "Alphabet Inc.", "Communication Services", "Internet Content & Information"),
        ("AMZN", "Amazon.com Inc.", "Consumer Cyclical", "Internet Retail"),
        ("META", "Meta Platforms Inc.", "Communication Services", "Internet Content & Information"),
        ("TSLA", "Tesla Inc.", "Consumer Cyclical", "Auto Manufacturers"),
        ("BRK.B", "Berkshire Hathaway Inc.", "Financial Services", "Insurance - Diversified"),
        ("JPM", "JPMorgan Chase & Co.", "Financial Services", "Banks - Diversified"),
        ("V", "Visa Inc.", "Financial Services", "Credit Services"),
        ("JNJ", "Johnson & Johnson", "Healthcare", "Drug Manufacturers"),
        ("XOM", "Exxon Mobil Corporation", "Energy", "Oil & Gas Integrated"),
        ("KO", "The Coca-Cola Company", "Consumer Defensive", "Beverages - Non-Alcoholic"),
        ("WMT", "Walmart Inc.", "Consumer Defensive", "Discount Stores"),
        ("COST", "Costco Wholesale Corporation", "Consumer Defensive", "Discount Stores"),
    ];

    public Task<StockQuote?> GetQuoteAsync(string symbol, CancellationToken ct = default)
    {
        var history = BuildHistory(symbol);
        var last = history[^1];
        var prev = history[^2];
        var change = last.Close - prev.Close;
        return Task.FromResult<StockQuote?>(new StockQuote(
            symbol.ToUpperInvariant(), last.Close, change, Math.Round(change / prev.Close * 100, 4),
            last.Open, last.High, last.Low, prev.Close, last.Volume, last.Date));
    }

    public Task<IReadOnlyList<PricePoint>> GetDailyHistoryAsync(string symbol, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<PricePoint>>(BuildHistory(symbol));

    public Task<CompanyOverview?> GetOverviewAsync(string symbol, CancellationToken ct = default)
    {
        var upper = symbol.ToUpperInvariant();
        var known = Companies.FirstOrDefault(c => c.Symbol == upper);
        var rng = new Random(StableHash(upper));
        var history = BuildHistory(upper);
        var price = history[^1].Close;
        var eps = Math.Round(price / (decimal)(12 + rng.NextDouble() * 30), 2);

        return Task.FromResult<CompanyOverview?>(new CompanyOverview(
            upper,
            known.Name ?? $"{upper} Corp. (sample)",
            "Sample data. Add an Alpha Vantage API key in Settings to see real company information.",
            "NASDAQ",
            known.Sector ?? "Technology",
            known.Industry ?? "Software",
            Math.Round(price * (decimal)(1e8 + rng.NextDouble() * 3e10), 0),
            Math.Round(price / eps, 2),
            Math.Round(price / eps * 0.85m, 2),
            eps,
            Math.Round((decimal)(rng.NextDouble() * 3), 2),
            Math.Round((decimal)(0.6 + rng.NextDouble()), 2),
            history.Max(p => p.High),
            history.Min(p => p.Low),
            Math.Round((decimal)(5 + rng.NextDouble() * 30), 2),
            Math.Round(price * 1.1m, 2)));
    }

    public Task<IReadOnlyList<SymbolMatch>> SearchAsync(string keywords, CancellationToken ct = default)
    {
        var results = Companies
            .Where(c => c.Symbol.Contains(keywords, StringComparison.OrdinalIgnoreCase)
                     || c.Name.Contains(keywords, StringComparison.OrdinalIgnoreCase))
            .Select(c => new SymbolMatch(c.Symbol, c.Name, "Equity", "United States", "USD"))
            .ToList();
        return Task.FromResult<IReadOnlyList<SymbolMatch>>(results);
    }

    private static List<PricePoint> BuildHistory(string symbol, int days = 100)
    {
        var rng = new Random(StableHash(symbol.ToUpperInvariant()));
        var price = 20 + rng.NextDouble() * 480;
        var drift = (rng.NextDouble() - 0.45) * 0.004;
        var vol = 0.01 + rng.NextDouble() * 0.025;

        var dates = new List<DateTime>();
        for (var d = DateTime.Today; dates.Count < days; d = d.AddDays(-1))
            if (d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                dates.Add(d);
        dates.Reverse();

        var points = new List<PricePoint>(days);
        foreach (var date in dates)
        {
            var open = price;
            price *= 1 + drift + vol * Gaussian(rng);
            var high = Math.Max(open, price) * (1 + rng.NextDouble() * vol / 2);
            var low = Math.Min(open, price) * (1 - rng.NextDouble() * vol / 2);
            points.Add(new PricePoint(date, R(open), R(high), R(low), R(price), (long)(5e6 + rng.NextDouble() * 5e7)));
        }
        return points;

        static decimal R(double v) => Math.Round((decimal)v, 2);
    }

    private static double Gaussian(Random rng) =>
        Math.Sqrt(-2 * Math.Log(1 - rng.NextDouble())) * Math.Cos(2 * Math.PI * rng.NextDouble());

    // string.GetHashCode is randomized per process, so use FNV-1a for repeatable data.
    private static int StableHash(string s)
    {
        unchecked
        {
            var hash = (int)2166136261;
            foreach (var c in s)
                hash = (hash ^ c) * 16777619;
            return hash;
        }
    }
}
