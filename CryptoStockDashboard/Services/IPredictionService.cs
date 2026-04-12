using CryptoStockDashboard.Models;

namespace CryptoStockDashboard.Services;

public interface IPredictionService
{
    PricePrediction PredictPrice(string symbol, string assetType, List<decimal> historicalPrices, decimal currentPrice);
}
