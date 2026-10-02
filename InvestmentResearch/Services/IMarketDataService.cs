using InvestmentResearch.Models;

namespace InvestmentResearch.Services;

public interface IMarketDataService
{
    Task<StockQuote?> GetQuoteAsync(string symbol, CancellationToken ct = default);

    /// <summary>Daily bars, oldest first.</summary>
    Task<IReadOnlyList<PricePoint>> GetDailyHistoryAsync(string symbol, CancellationToken ct = default);

    Task<CompanyOverview?> GetOverviewAsync(string symbol, CancellationToken ct = default);

    Task<IReadOnlyList<SymbolMatch>> SearchAsync(string keywords, CancellationToken ct = default);
}
