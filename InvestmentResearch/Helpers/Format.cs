using System.Globalization;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace InvestmentResearch.Helpers;

/// <summary>Display formatting. Also callable from XAML via x:Bind function bindings.</summary>
public static class Format
{
    private static readonly SolidColorBrush UpBrush = new(ColorHelper.FromArgb(255, 16, 137, 62));
    private static readonly SolidColorBrush DownBrush = new(ColorHelper.FromArgb(255, 209, 52, 56));

    public const string Missing = "—";

    public static SolidColorBrush ChangeBrush(bool isUp) => isUp ? UpBrush : DownBrush;

    public static string Price(decimal? value) =>
        value is null ? Missing : value.Value.ToString("N2", CultureInfo.CurrentCulture);

    public static string Signed(decimal value) =>
        value.ToString("+#,##0.00;-#,##0.00;0.00", CultureInfo.CurrentCulture);

    /// <summary>Formats a value already expressed in percent units, e.g. 1.5 → "+1.50%".</summary>
    public static string SignedPercent(decimal? value) =>
        value is null ? Missing : Signed(value.Value) + "%";

    public static string Percent(decimal? value) =>
        value is null ? Missing : value.Value.ToString("N2", CultureInfo.CurrentCulture) + "%";

    public static string Ratio(decimal? value) =>
        value is null ? Missing : value.Value.ToString("N2", CultureInfo.CurrentCulture);

    public static string Large(decimal? value)
    {
        if (value is null)
            return Missing;
        var v = value.Value;
        var abs = Math.Abs(v);
        return abs switch
        {
            >= 1_000_000_000_000m => $"{v / 1_000_000_000_000m:N2}T",
            >= 1_000_000_000m => $"{v / 1_000_000_000m:N2}B",
            >= 1_000_000m => $"{v / 1_000_000m:N2}M",
            >= 1_000m => $"{v / 1_000m:N1}K",
            _ => v.ToString("N0", CultureInfo.CurrentCulture),
        };
    }
}
