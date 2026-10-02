using InvestmentResearch.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace InvestmentResearch.Views;

public sealed partial class StockDetailPage : Page
{
    public StockDetailPage()
    {
        InitializeComponent();
    }

    public StockDetailViewModel ViewModel { get; } = new(App.MarketData, App.Settings);

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string symbol && symbol.Length > 0)
            await ViewModel.LoadAsync(symbol);
    }
}
