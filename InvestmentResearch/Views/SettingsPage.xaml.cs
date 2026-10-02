using InvestmentResearch.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace InvestmentResearch.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    public SettingsViewModel ViewModel { get; } = new(App.MarketData, App.Settings);
}
