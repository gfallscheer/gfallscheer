using System.Text.Json;

namespace InvestmentResearch.Services;

public sealed class AppSettings
{
    public string? AlphaVantageApiKey { get; set; }
    public List<string> Watchlist { get; set; } = ["AAPL", "MSFT", "NVDA"];
}

/// <summary>
/// Persists settings and the watchlist to %LocalAppData%\InvestmentResearch\settings.json.
/// </summary>
public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public SettingsService()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "InvestmentResearch");
        Directory.CreateDirectory(folder);
        FilePath = Path.Combine(folder, "settings.json");
        Current = Load();
    }

    public string FilePath { get; }

    public AppSettings Current { get; }

    public bool IsInWatchlist(string symbol) =>
        Current.Watchlist.Contains(symbol, StringComparer.OrdinalIgnoreCase);

    public bool AddToWatchlist(string symbol)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        if (symbol.Length == 0 || IsInWatchlist(symbol))
            return false;
        Current.Watchlist.Add(symbol);
        Save();
        return true;
    }

    public void RemoveFromWatchlist(string symbol)
    {
        Current.Watchlist.RemoveAll(s => s.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        Save();
    }

    public void Save() => File.WriteAllText(FilePath, JsonSerializer.Serialize(Current, JsonOptions));

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch (JsonException)
        {
            // Corrupt file: start over with defaults.
        }
        return new AppSettings();
    }
}
