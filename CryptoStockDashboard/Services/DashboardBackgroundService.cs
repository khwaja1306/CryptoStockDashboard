using CryptoStockDashboard.Hubs;
using CryptoStockDashboard.Models;
using CryptoStockDashboard.Services;
using Microsoft.AspNetCore.SignalR;

namespace CryptoStockDashboard.Services;

public class DashboardBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<DashboardHub> _hubContext;
    private readonly ILogger<DashboardBackgroundService> _logger;
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(30);

    public DashboardBackgroundService(
        IServiceScopeFactory scopeFactory,
        IHubContext<DashboardHub> hubContext,
        ILogger<DashboardBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Dashboard background service started, refreshing every {Interval}s", RefreshInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await BroadcastDashboardData(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting dashboard data");
            }

            await Task.Delay(RefreshInterval, stoppingToken);
        }
    }

    private async Task BroadcastDashboardData(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var cryptoService = scope.ServiceProvider.GetRequiredService<ICryptoService>();
        var stockService = scope.ServiceProvider.GetRequiredService<IStockService>();
        var predictionService = scope.ServiceProvider.GetRequiredService<IPredictionService>();

        var cryptoTask = cryptoService.GetTopCryptoAssetsAsync();
        var stockTask = stockService.GetSnpStocksAsync();

        await Task.WhenAll(cryptoTask, stockTask);

        var cryptoAssets = await cryptoTask;
        var stocks = await stockTask;

        // Generate predictions for top assets
        var predictions = new List<PricePrediction>();

        foreach (var crypto in cryptoAssets.Take(5))
        {
            var history = await cryptoService.GetHistoricalPricesAsync(crypto.Id);
            var prediction = predictionService.PredictPrice(crypto.Symbol, "Crypto", history, crypto.CurrentPrice);
            predictions.Add(prediction);
            crypto.Indicators = await cryptoService.GetTechnicalIndicatorsAsync(crypto.Id);
        }

        foreach (var stock in stocks.Take(5))
        {
            var history = await stockService.GetHistoricalPricesAsync(stock.Symbol);
            var prediction = predictionService.PredictPrice(stock.Symbol, "Stock", history, stock.Price);
            predictions.Add(prediction);
        }

        var dashboardData = new DashboardData
        {
            CryptoAssets = cryptoAssets,
            StockQuotes = stocks,
            Predictions = predictions,
            LastRefreshed = DateTime.UtcNow
        };

        await _hubContext.Clients.All.SendAsync("ReceiveDashboardUpdate", dashboardData, ct);
        _logger.LogDebug("Broadcast dashboard data: {CryptoCount} crypto, {StockCount} stocks, {PredCount} predictions",
            cryptoAssets.Count, stocks.Count, predictions.Count);
    }
}
