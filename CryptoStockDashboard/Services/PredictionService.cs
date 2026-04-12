using CryptoStockDashboard.Models;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;

namespace CryptoStockDashboard.Services;

public class PredictionService : IPredictionService
{
    private readonly MLContext _mlContext;
    private readonly ILogger<PredictionService> _logger;

    public PredictionService(ILogger<PredictionService> logger)
    {
        _mlContext = new MLContext(seed: 0);
        _logger = logger;
    }

    public PricePrediction PredictPrice(string symbol, string assetType, List<decimal> historicalPrices, decimal currentPrice)
    {
        try
        {
            if (historicalPrices.Count < 5)
                return CreateFallbackPrediction(symbol, assetType, currentPrice, historicalPrices);

            return ForecastWithSsa(symbol, assetType, historicalPrices, currentPrice);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ML.NET prediction failed for {Symbol}, using moving average fallback", symbol);
            return CreateMovingAveragePrediction(symbol, assetType, historicalPrices, currentPrice);
        }
    }

    private PricePrediction ForecastWithSsa(string symbol, string assetType, List<decimal> historicalPrices, decimal currentPrice)
    {
        const int horizon = 7;
        var data = historicalPrices.Select(p => new TimeSeriesForecastInput { Price = (float)p }).ToList();
        var dataView = _mlContext.Data.LoadFromEnumerable(data);

        var windowSize = Math.Min(data.Count / 2, 10);
        var seriesLength = data.Count;

        var pipeline = _mlContext.Forecasting.ForecastBySsa(
            outputColumnName: nameof(TimeSeriesForecastOutput.ForecastedPrice),
            inputColumnName: nameof(TimeSeriesForecastInput.Price),
            windowSize: Math.Max(windowSize, 2),
            seriesLength: seriesLength,
            trainSize: seriesLength,
            horizon: horizon,
            confidenceLowerBoundColumn: nameof(TimeSeriesForecastOutput.ConfidenceLowerBound),
            confidenceUpperBoundColumn: nameof(TimeSeriesForecastOutput.ConfidenceUpperBound),
            confidenceLevel: 0.95f);

        var model = pipeline.Fit(dataView);
        var forecastEngine = model.CreateTimeSeriesEngine<TimeSeriesForecastInput, TimeSeriesForecastOutput>(_mlContext);
        var forecast = forecastEngine.Predict();

        var predictedPrices = forecast.ForecastedPrice.Select(p => (decimal)p).ToList();
        var avgPredicted = predictedPrices.Count > 0 ? predictedPrices.Average() : currentPrice;
        var changePercent = currentPrice != 0 ? (avgPredicted - currentPrice) / currentPrice * 100 : 0;

        var confidence = CalculateConfidence(forecast);
        var signal = DetermineSignal(changePercent, confidence);

        return new PricePrediction
        {
            Symbol = symbol,
            AssetType = assetType,
            CurrentPrice = currentPrice,
            PredictedPrice = Math.Round(avgPredicted, 2),
            PredictedChangePercent = Math.Round(changePercent, 2),
            Signal = signal,
            Confidence = confidence,
            PredictionDate = DateTime.UtcNow,
            HistoricalPrices = historicalPrices.TakeLast(14).ToList(),
            PredictedPrices = predictedPrices.Select(p => Math.Round(p, 2)).ToList()
        };
    }

    private PricePrediction CreateMovingAveragePrediction(string symbol, string assetType, List<decimal> historicalPrices, decimal currentPrice)
    {
        var ma7 = historicalPrices.TakeLast(7).Average();
        var ma14 = historicalPrices.TakeLast(14).Average();
        var trend = ma7 - ma14;
        var predicted = currentPrice + trend;

        var changePercent = currentPrice != 0 ? (predicted - currentPrice) / currentPrice * 100 : 0;
        var signal = DetermineSignal(changePercent, 0.55f);

        var predictedPrices = new List<decimal>();
        var p = currentPrice;
        var dailyTrend = trend / 7;
        for (int i = 0; i < 7; i++)
        {
            p += dailyTrend;
            predictedPrices.Add(Math.Round(p, 2));
        }

        return new PricePrediction
        {
            Symbol = symbol,
            AssetType = assetType,
            CurrentPrice = currentPrice,
            PredictedPrice = Math.Round(predicted, 2),
            PredictedChangePercent = Math.Round(changePercent, 2),
            Signal = signal,
            Confidence = 0.55f,
            PredictionDate = DateTime.UtcNow,
            HistoricalPrices = historicalPrices.TakeLast(14).ToList(),
            PredictedPrices = predictedPrices
        };
    }

    private static PricePrediction CreateFallbackPrediction(string symbol, string assetType, decimal currentPrice, List<decimal> historicalPrices)
    {
        return new PricePrediction
        {
            Symbol = symbol,
            AssetType = assetType,
            CurrentPrice = currentPrice,
            PredictedPrice = currentPrice,
            PredictedChangePercent = 0,
            Signal = "Hold",
            Confidence = 0.3f,
            PredictionDate = DateTime.UtcNow,
            HistoricalPrices = historicalPrices,
            PredictedPrices = [currentPrice]
        };
    }

    private static float CalculateConfidence(TimeSeriesForecastOutput forecast)
    {
        if (forecast.ForecastedPrice.Length == 0 || forecast.ConfidenceLowerBound.Length == 0)
            return 0.5f;

        var avgSpread = 0f;
        for (int i = 0; i < forecast.ForecastedPrice.Length; i++)
        {
            var range = forecast.ConfidenceUpperBound[i] - forecast.ConfidenceLowerBound[i];
            var mid = Math.Abs(forecast.ForecastedPrice[i]);
            avgSpread += mid > 0 ? range / mid : 1f;
        }
        avgSpread /= forecast.ForecastedPrice.Length;

        // Narrow spread = high confidence, wide spread = low confidence
        var confidence = Math.Clamp(1f - avgSpread, 0.3f, 0.95f);
        return (float)Math.Round(confidence, 2);
    }

    private static string DetermineSignal(decimal changePercent, float confidence)
    {
        if (confidence < 0.4f) return "Hold";
        return changePercent switch
        {
            > 2m => "Buy",
            < -2m => "Sell",
            _ => "Hold"
        };
    }
}
