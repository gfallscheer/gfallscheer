using InvestmentResearch.Models;

namespace InvestmentResearch.Helpers;

/// <summary>Technical indicators computed from daily bars (oldest first).</summary>
public static class Analytics
{
    public static decimal? SimpleMovingAverage(IReadOnlyList<PricePoint> points, int period)
    {
        if (points.Count < period)
            return null;
        return points.Skip(points.Count - period).Average(p => p.Close);
    }

    /// <summary>Percent change from the first to the last close.</summary>
    public static decimal? PeriodReturn(IReadOnlyList<PricePoint> points)
    {
        if (points.Count < 2 || points[0].Close == 0)
            return null;
        return (points[^1].Close / points[0].Close - 1) * 100;
    }

    /// <summary>Annualized volatility of daily log returns, in percent.</summary>
    public static decimal? AnnualizedVolatility(IReadOnlyList<PricePoint> points)
    {
        if (points.Count < 3)
            return null;

        var returns = new List<double>(points.Count - 1);
        for (var i = 1; i < points.Count; i++)
        {
            if (points[i - 1].Close > 0 && points[i].Close > 0)
                returns.Add(Math.Log((double)(points[i].Close / points[i - 1].Close)));
        }
        if (returns.Count < 2)
            return null;

        var mean = returns.Average();
        var variance = returns.Sum(r => (r - mean) * (r - mean)) / (returns.Count - 1);
        return (decimal)(Math.Sqrt(variance) * Math.Sqrt(252) * 100);
    }

    /// <summary>Largest peak-to-trough decline, in percent (a negative number).</summary>
    public static decimal? MaxDrawdown(IReadOnlyList<PricePoint> points)
    {
        if (points.Count < 2)
            return null;

        var peak = points[0].Close;
        var maxDrawdown = 0m;
        foreach (var p in points)
        {
            peak = Math.Max(peak, p.Close);
            if (peak > 0)
                maxDrawdown = Math.Min(maxDrawdown, (p.Close / peak - 1) * 100);
        }
        return maxDrawdown;
    }

    /// <summary>Relative Strength Index using Wilder's smoothing.</summary>
    public static decimal? Rsi(IReadOnlyList<PricePoint> points, int period = 14)
    {
        if (points.Count <= period)
            return null;

        decimal gain = 0, loss = 0;
        for (var i = 1; i <= period; i++)
        {
            var change = points[i].Close - points[i - 1].Close;
            if (change > 0) gain += change; else loss -= change;
        }
        gain /= period;
        loss /= period;

        for (var i = period + 1; i < points.Count; i++)
        {
            var change = points[i].Close - points[i - 1].Close;
            gain = (gain * (period - 1) + Math.Max(change, 0)) / period;
            loss = (loss * (period - 1) + Math.Max(-change, 0)) / period;
        }

        if (loss == 0)
            return 100;
        return 100 - 100 / (1 + gain / loss);
    }
}
