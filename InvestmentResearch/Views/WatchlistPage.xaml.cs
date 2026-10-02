using InvestmentResearch.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;

namespace InvestmentResearch.Views;

public sealed partial class WatchlistPage : Page
{
    public WatchlistPage()
    {
        InitializeComponent();
    }

    public WatchlistViewModel ViewModel { get; } = new(App.MarketData, App.Settings);

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.InitializeAsync();
    }

    private void Watchlist_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is WatchlistItemViewModel item)
            Frame.Navigate(typeof(StockDetailPage), item.Symbol);
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: WatchlistItemViewModel item })
            ViewModel.RemoveCommand.Execute(item);
    }

    private void NewSymbol_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            ViewModel.AddCommand.Execute(null);
            e.Handled = true;
        }
    }
}
