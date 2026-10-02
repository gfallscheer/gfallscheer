using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvestmentResearch.Services;

namespace InvestmentResearch.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly MarketDataProvider _marketData;
    private readonly SettingsService _settings;

    public SettingsViewModel(MarketDataProvider marketData, SettingsService settings)
    {
        _marketData = marketData;
        _settings = settings;
        _apiKey = settings.Current.AlphaVantageApiKey ?? "";
        _statusMessage = DescribeSource();
    }

    public string SettingsFilePath => _settings.FilePath;

    [ObservableProperty]
    private string _apiKey;

    [ObservableProperty]
    private string _statusMessage;

    [RelayCommand]
    private void Save()
    {
        _settings.Current.AlphaVantageApiKey = string.IsNullOrWhiteSpace(ApiKey) ? null : ApiKey.Trim();
        _settings.Save();
        StatusMessage = "Saved. " + DescribeSource();
    }

    private string DescribeSource() => _marketData.IsUsingSampleData
        ? "Using offline sample data."
        : "Using live data from Alpha Vantage.";
}
