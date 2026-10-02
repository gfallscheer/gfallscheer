using InvestmentResearch.Models;
using InvestmentResearch.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace InvestmentResearch;

public sealed partial class MainWindow : Window
{
    private CancellationTokenSource? _searchCts;

    public MainWindow()
    {
        InitializeComponent();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 860));
        ContentFrame.Navigate(typeof(WatchlistPage));
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var target = args.IsSettingsSelected ? typeof(SettingsPage)
            : (args.SelectedItem as NavigationViewItem)?.Tag is "watchlist" ? typeof(WatchlistPage)
            : null;

        // Selection is also synced from ContentFrame_Navigated; don't navigate twice.
        if (target is not null && ContentFrame.CurrentSourcePageType != target)
            ContentFrame.Navigate(target);
    }

    private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        if (ContentFrame.CanGoBack)
            ContentFrame.GoBack();
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        NavView.IsBackEnabled = ContentFrame.CanGoBack;
        NavView.SelectedItem = e.SourcePageType == typeof(WatchlistPage) ? WatchlistNavItem
            : e.SourcePageType == typeof(SettingsPage) ? NavView.SettingsItem
            : null;
    }

    private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
            return;

        _searchCts?.Cancel();
        var cts = _searchCts = new CancellationTokenSource();
        var text = sender.Text.Trim();
        if (text.Length == 0)
        {
            sender.ItemsSource = null;
            return;
        }

        try
        {
            // Debounce so we don't burn API quota on every keystroke.
            await Task.Delay(400, cts.Token);
            var results = await App.MarketData.SearchAsync(text, cts.Token);
            if (!cts.IsCancellationRequested)
                sender.ItemsSource = results;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            sender.ItemsSource = null;
        }
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var symbol = args.ChosenSuggestion is SymbolMatch match ? match.Symbol : args.QueryText.Trim();
        if (symbol.Length == 0)
            return;

        _searchCts?.Cancel();
        sender.Text = "";
        sender.ItemsSource = null;
        ContentFrame.Navigate(typeof(StockDetailPage), symbol.ToUpperInvariant());
    }
}
