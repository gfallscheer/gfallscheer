using InvestmentResearch.Services;
using Microsoft.UI.Xaml;

namespace InvestmentResearch;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    public static SettingsService Settings { get; } = new();

    public static MarketDataProvider MarketData { get; } = new(Settings);

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
