using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvestmentResearch.Helpers;
using InvestmentResearch.Models;
using InvestmentResearch.Services;

namespace InvestmentResearch.ViewModels;

public sealed record KeyStat(string Label, string Value);

public partial class StockDetailViewModel(MarketDataProvider marketData, SettingsService settings) : ObservableObject
{
    public ObservableCollection<KeyStat> Fundamentals { get; } = [];

    public ObservableCollection<KeyStat> Technicals { get; } = [];

    [ObservableProperty]
    private string _symbol = "";

    [ObservableProperty]
    private string _companyName = "";

    [ObservableProperty]
    private string _subtitle = "";

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private string _priceText = Format.Missing;

    [ObservableProperty]
    private string _changeText = "";

    [ObservableProperty]
    private string _asOfText = "";

    [ObservableProperty]
    private bool _isUp = true;

    [ObservableProperty]
    private IReadOnlyList<PricePoint> _history = [];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WatchlistButtonText))]
    private bool _isInWatchlist;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public string WatchlistButtonText => IsInWatchlist ? "Remove from watchlist" : "Add to watchlist";

    public async Task LoadAsync(string symbol)
    {
        Symbol = symbol.Trim().ToUpperInvariant();
        CompanyName = Symbol;
        IsInWatchlist = settings.IsInWatchlist(Symbol);
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var quoteTask = marketData.GetQuoteAsync(Symbol);
            var historyTask = marketData.GetDailyHistoryAsync(Symbol);
            var overviewTask = marketData.GetOverviewAsync(Symbol);

            var quote = await quoteTask;
            if (quote is null)
            {
                ErrorMessage = $"No data found for \"{Symbol}\".";
                return;
            }
            ApplyQuote(quote);

            History = await historyTask;
            var overview = await overviewTask;
            ApplyOverview(overview);
            BuildTechnicals(quote, History);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleWatchlist()
    {
        if (IsInWatchlist)
            settings.RemoveFromWatchlist(Symbol);
        else
            settings.AddToWatchlist(Symbol);
        IsInWatchlist = settings.IsInWatchlist(Symbol);
    }

    private void ApplyQuote(StockQuote quote)
    {
        PriceText = Format.Price(quote.Price);
        ChangeText = $"{Format.Signed(quote.Change)} ({Format.SignedPercent(quote.ChangePercent)})";
        AsOfText = $"As of {quote.LatestTradingDay:d}";
        IsUp = quote.Change >= 0;
    }

    private void ApplyOverview(CompanyOverview? o)
    {
        Fundamentals.Clear();
        if (o is null)
        {
            Subtitle = "";
            Description = "No company overview is available for this symbol (it may be an ETF or fund).";
            return;
        }

        CompanyName = string.IsNullOrWhiteSpace(o.Name) ? Symbol : o.Name;
        Subtitle = string.Join(" · ", new[] { o.Exchange, o.Sector, o.Industry }.Where(s => !string.IsNullOrWhiteSpace(s)));
        Description = o.Description;

        Fundamentals.Add(new("Market cap", Format.Large(o.MarketCap)));
        Fundamentals.Add(new("P/E (TTM)", Format.Ratio(o.PERatio)));
        Fundamentals.Add(new("Forward P/E", Format.Ratio(o.ForwardPE)));
        Fundamentals.Add(new("EPS (TTM)", Format.Price(o.EPS)));
        Fundamentals.Add(new("Dividend yield", Format.Percent(o.DividendYield)));
        Fundamentals.Add(new("Profit margin", Format.Percent(o.ProfitMargin)));
        Fundamentals.Add(new("Beta", Format.Ratio(o.Beta)));
        Fundamentals.Add(new("52-week high", Format.Price(o.Week52High)));
        Fundamentals.Add(new("52-week low", Format.Price(o.Week52Low)));
        Fundamentals.Add(new("Analyst target", Format.Price(o.AnalystTargetPrice)));
    }

    private void BuildTechnicals(StockQuote quote, IReadOnlyList<PricePoint> history)
    {
        Technicals.Clear();
        Technicals.Add(new("Open", Format.Price(quote.Open)));
        Technicals.Add(new("Day range", $"{Format.Price(quote.Low)} – {Format.Price(quote.High)}"));
        Technicals.Add(new("Previous close", Format.Price(quote.PreviousClose)));
        Technicals.Add(new("Volume", Format.Large(quote.Volume)));
        Technicals.Add(new("20-day SMA", Format.Price(Analytics.SimpleMovingAverage(history, 20))));
        Technicals.Add(new("50-day SMA", Format.Price(Analytics.SimpleMovingAverage(history, 50))));
        Technicals.Add(new("RSI (14)", Format.Ratio(Analytics.Rsi(history))));
        Technicals.Add(new($"{history.Count}-day return", Format.SignedPercent(Analytics.PeriodReturn(history))));
        Technicals.Add(new("Volatility (ann.)", Format.Percent(Analytics.AnnualizedVolatility(history))));
        Technicals.Add(new("Max drawdown", Format.Percent(Analytics.MaxDrawdown(history))));
    }
}
