namespace InvestmentResearch.Models;

public sealed record StockQuote(
    string Symbol,
    decimal Price,
    decimal Change,
    decimal ChangePercent,
    decimal Open,
    decimal High,
    decimal Low,
    decimal PreviousClose,
    long Volume,
    DateTime LatestTradingDay);

public sealed record PricePoint(
    DateTime Date,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume);

public sealed record CompanyOverview(
    string Symbol,
    string Name,
    string Description,
    string Exchange,
    string Sector,
    string Industry,
    decimal? MarketCap,
    decimal? PERatio,
    decimal? ForwardPE,
    decimal? EPS,
    decimal? DividendYield,
    decimal? Beta,
    decimal? Week52High,
    decimal? Week52Low,
    decimal? ProfitMargin,
    decimal? AnalystTargetPrice);

public sealed record SymbolMatch(string Symbol, string Name, string Type, string Region, string Currency)
{
    public string Description => $"{Name} · {Region}";
}

public sealed class MarketDataException(string message) : Exception(message);
