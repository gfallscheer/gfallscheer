# Investment Research (WinUI 3)

A starter desktop app for researching stocks, built with C#, WinUI 3 (Windows App SDK) and MVVM.

## Features

- **Watchlist**: add and remove symbols and see the last price, daily change and volume. It's saved between sessions.
- **Symbol search**: type a ticker or company name in the search box at the top of the navigation pane.
- **Stock detail page**:
  - Price chart (last ~100 trading days) with a hover crosshair
  - Fundamentals: market cap, P/E, forward P/E, EPS, dividend yield, margin, beta, 52-week range, analyst target
  - Technicals: SMA 20/50, RSI(14), period return, annualized volatility, max drawdown
  - Company description
- **Live or sample data**: uses [Alpha Vantage](https://www.alphavantage.co) when you enter an API key in Settings. Without a key it uses repeatable offline sample data, so the app runs out of the box.

## Getting started

1. Install **Visual Studio 2022** (17.10 or later) with the **WinUI application development** workload. In older versions this is ".NET desktop development" plus "Windows App SDK C# Templates".
2. Open `InvestmentResearch.sln`.
3. Set the platform to **x64** (or ARM64) and press **F5**. The project is *unpackaged*, so it runs as a normal `.exe` with no MSIX deployment step.
4. Optional: get a free key at <https://www.alphavantage.co/support/#api-key> and paste it into **Settings**.

## Project layout

```
InvestmentResearch/
├── App.xaml(.cs)              App startup; creates the shared services
├── MainWindow.xaml(.cs)       NavigationView shell, search box, frame navigation
├── Models/                    StockQuote, PricePoint, CompanyOverview, SymbolMatch
├── Services/
│   ├── IMarketDataService.cs  The data-source abstraction
│   ├── AlphaVantageService.cs Real data over HTTP + JSON
│   ├── MockMarketDataService.cs  Offline sample data
│   ├── MarketDataProvider.cs  Picks live or sample data, caches responses for 5 minutes
│   └── SettingsService.cs     API key and watchlist JSON in %LocalAppData%\InvestmentResearch
├── ViewModels/                CommunityToolkit.Mvvm view models (one per page)
├── Views/                     WatchlistPage, StockDetailPage, SettingsPage
├── Controls/PriceChart.cs     Dependency-free line chart drawn on a Canvas
└── Helpers/                   Number formatting and technical indicators
```

## Ideas for next steps

- **Another data provider**: implement `IMarketDataService` for Polygon, Finnhub, Financial Modeling Prep, IEX and so on, then return it from `MarketDataProvider`.
- **Richer charts**: replace `PriceChart` with [LiveCharts2](https://livecharts.dev) or [ScottPlot](https://scottplot.net) for candlesticks, volume bars, zoom and selectable ranges (1M / 6M / 1Y / 5Y).
- **Financial statements**: Alpha Vantage `INCOME_STATEMENT`, `BALANCE_SHEET` and `CASH_FLOW` endpoints, shown in a tab per statement.
- **News and sentiment**: the `NEWS_SENTIMENT` endpoint.
- **Portfolio tracking**: positions, cost basis, P&L and allocation, stored in SQLite (`Microsoft.Data.Sqlite`).
- **Notes**: a per-symbol notes / investment thesis editor.
- **Secure the API key**: store it with `Windows.Security.Credentials.PasswordVault` instead of plain JSON.
- **Dependency injection**: move to `Microsoft.Extensions.DependencyInjection` as the app grows.
- **Unit tests**: `Helpers/Analytics.cs` and the JSON parsing in `AlphaVantageService` are good first candidates.

> For research and educational purposes only. Not investment advice.
