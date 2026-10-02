using InvestmentResearch.Helpers;
using InvestmentResearch.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace InvestmentResearch.Controls;

/// <summary>
/// A lightweight closing-price line chart with a hover crosshair.
/// Swap for a charting library (e.g. LiveCharts2 or ScottPlot) when you need candlesticks or zooming.
/// </summary>
public sealed class PriceChart : UserControl
{
    private const double PadLeft = 8, PadRight = 64, PadTop = 12, PadBottom = 24;

    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
        nameof(Points), typeof(object), typeof(PriceChart),
        new PropertyMetadata(null, (d, _) => ((PriceChart)d).Redraw()));

    private readonly Canvas _canvas = new() { Background = new SolidColorBrush(Colors.Transparent) };
    private readonly Line _crosshair = new() { StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 3, 3 }, Visibility = Visibility.Collapsed };
    private readonly Ellipse _marker = new() { Width = 8, Height = 8, Visibility = Visibility.Collapsed };
    private readonly Border _tooltip = new() { Padding = new Thickness(8, 4, 8, 4), CornerRadius = new CornerRadius(4), Visibility = Visibility.Collapsed };
    private readonly TextBlock _tooltipText = new() { FontSize = 12 };

    private double _min, _max, _plotWidth, _plotHeight;

    public PriceChart()
    {
        Content = _canvas;
        _tooltip.Child = _tooltipText;
        SizeChanged += (_, _) => Redraw();
        ActualThemeChanged += (_, _) => Redraw();
        _canvas.PointerMoved += OnPointerMoved;
        _canvas.PointerExited += (_, _) => HideHover();
    }

    public IReadOnlyList<PricePoint>? Points
    {
        get => GetValue(PointsProperty) as IReadOnlyList<PricePoint>;
        set => SetValue(PointsProperty, value);
    }

    private void Redraw()
    {
        _canvas.Children.Clear();
        var points = Points;
        var width = ActualWidth;
        var height = ActualHeight;
        if (points is null || points.Count < 2 || width <= PadLeft + PadRight || height <= PadTop + PadBottom)
            return;

        _min = (double)points.Min(p => p.Close);
        _max = (double)points.Max(p => p.Close);
        if (_max - _min < 1e-9) { _max += 1; _min -= 1; }
        _plotWidth = width - PadLeft - PadRight;
        _plotHeight = height - PadTop - PadBottom;

        var secondary = ThemeBrush("TextFillColorSecondaryBrush");
        var gridBrush = ThemeBrush("DividerStrokeColorDefaultBrush");
        var lineBrush = Format.ChangeBrush(points[^1].Close >= points[0].Close);

        // Horizontal grid lines with price labels on the right.
        for (var i = 0; i <= 4; i++)
        {
            var y = PadTop + _plotHeight * i / 4;
            _canvas.Children.Add(new Line { X1 = PadLeft, X2 = PadLeft + _plotWidth, Y1 = y, Y2 = y, Stroke = gridBrush, StrokeThickness = 1 });
            AddLabel(Format.Price((decimal)(_max - (_max - _min) * i / 4)), PadLeft + _plotWidth + 8, y - 8, secondary);
        }

        var line = new Polyline { Stroke = lineBrush, StrokeThickness = 2, StrokeLineJoin = PenLineJoin.Round };
        var area = new Polygon { Fill = new SolidColorBrush(lineBrush.Color) { Opacity = 0.12 } };
        for (var i = 0; i < points.Count; i++)
        {
            var pt = new Point(X(i, points.Count), Y((double)points[i].Close));
            line.Points.Add(pt);
            area.Points.Add(pt);
        }
        area.Points.Add(new Point(X(points.Count - 1, points.Count), PadTop + _plotHeight));
        area.Points.Add(new Point(PadLeft, PadTop + _plotHeight));
        _canvas.Children.Add(area);
        _canvas.Children.Add(line);

        // First, middle and last dates along the bottom.
        foreach (var i in new[] { 0, points.Count / 2, points.Count - 1 })
        {
            var x = X(i, points.Count) - (i == 0 ? 0 : i == points.Count - 1 ? 60 : 30);
            AddLabel(points[i].Date.ToString("MMM d"), x, PadTop + _plotHeight + 4, secondary);
        }

        _crosshair.Stroke = secondary;
        _marker.Fill = lineBrush;
        _tooltip.Background = ThemeBrush("SolidBackgroundFillColorTertiaryBrush");
        _tooltipText.Foreground = ThemeBrush("TextFillColorPrimaryBrush");
        HideHover();
        _canvas.Children.Add(_crosshair);
        _canvas.Children.Add(_marker);
        _canvas.Children.Add(_tooltip);
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var points = Points;
        if (points is null || points.Count < 2 || _plotWidth <= 0)
            return;

        var x = e.GetCurrentPoint(_canvas).Position.X;
        var index = (int)Math.Round((x - PadLeft) / _plotWidth * (points.Count - 1));
        index = Math.Clamp(index, 0, points.Count - 1);
        var p = points[index];
        var px = X(index, points.Count);
        var py = Y((double)p.Close);

        _crosshair.X1 = _crosshair.X2 = px;
        _crosshair.Y1 = PadTop;
        _crosshair.Y2 = PadTop + _plotHeight;
        Canvas.SetLeft(_marker, px - 4);
        Canvas.SetTop(_marker, py - 4);
        _tooltipText.Text = $"{p.Date:d}   {Format.Price(p.Close)}";
        _tooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var tipX = px + 12 + _tooltip.DesiredSize.Width > PadLeft + _plotWidth ? px - 12 - _tooltip.DesiredSize.Width : px + 12;
        Canvas.SetLeft(_tooltip, tipX);
        Canvas.SetTop(_tooltip, PadTop);

        _crosshair.Visibility = _marker.Visibility = _tooltip.Visibility = Visibility.Visible;
    }

    private void HideHover() =>
        _crosshair.Visibility = _marker.Visibility = _tooltip.Visibility = Visibility.Collapsed;

    private double X(int index, int count) => PadLeft + _plotWidth * index / (count - 1);

    private double Y(double price) => PadTop + (_max - price) / (_max - _min) * _plotHeight;

    private void AddLabel(string text, double x, double y, Brush foreground)
    {
        var label = new TextBlock { Text = text, FontSize = 11, Foreground = foreground };
        Canvas.SetLeft(label, x);
        Canvas.SetTop(label, y);
        _canvas.Children.Add(label);
    }

    private static Brush ThemeBrush(string key) => (Brush)Application.Current.Resources[key];
}
