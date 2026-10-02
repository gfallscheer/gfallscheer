using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvestmentResearch.Helpers;
using InvestmentResearch.Services;

namespace InvestmentResearch.ViewModels;

public partial class WatchlistViewModel(MarketDataProvider marketData, SettingsService settings) : ObservableObject
{
    public ObservableCollection<WatchlistItemViewModel> Items { get; } = [];

    public bool IsUsingSampleData => marketData.IsUsingSampleData;

    [ObservableProperty]
    private string _newSymbol = "";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public async Task InitializeAsync()
    {
        Items.Clear();
        foreach (var symbol in settings.Current.Watchlist)
            Items.Add(new WatchlistItemViewModel(symbol));
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var symbol = NewSymbol.Trim().ToUpperInvariant();
        if (symbol.Length == 0 || !settings.AddToWatchlist(symbol))
            return;

        NewSymbol = "";
        var item = new WatchlistItemViewModel(symbol);
        Items.Add(item);
        await LoadQuoteAsync(item);
    }

    [RelayCommand]
    private void Remove(WatchlistItemViewModel item)
    {
        settings.RemoveFromWatchlist(item.Symbol);
        Items.Remove(item);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            // Sequential on purpose: free API tiers throttle bursts of requests.
            foreach (var item in Items.ToList())
                await LoadQuoteAsync(item);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadQuoteAsync(WatchlistItemViewModel item)
    {
        try
        {
            var quote = await marketData.GetQuoteAsync(item.Symbol);
            if (quote is null)
                item.SetUnavailable();
            else
                item.Update(quote);
        }
        catch (Exception ex)
        {
            item.SetUnavailable();
            ErrorMessage ??= $"Could not load {item.Symbol}: {ex.Message}";
        }
    }
}

public partial class WatchlistItemViewModel(string symbol) : ObservableObject
{
    public string Symbol { get; } = symbol;

    [ObservableProperty]
    private string _priceText = "…";

    [ObservableProperty]
    private string _changeText = "";

    [ObservableProperty]
    private string _volumeText = "";

    [ObservableProperty]
    private bool _isUp = true;

    public void Update(Models.StockQuote quote)
    {
        PriceText = Format.Price(quote.Price);
        ChangeText = $"{Format.Signed(quote.Change)} ({Format.SignedPercent(quote.ChangePercent)})";
        VolumeText = Format.Large(quote.Volume);
        IsUp = quote.Change >= 0;
    }

    public void SetUnavailable()
    {
        PriceText = Format.Missing;
        ChangeText = "";
        VolumeText = "";
    }
}
