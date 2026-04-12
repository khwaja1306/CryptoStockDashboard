namespace CryptoStockDashboard.Models;

public class PricePrediction
{
    public string Symbol { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty; // "Crypto" or "Stock"
    public decimal CurrentPrice { get; set; }
    public decimal PredictedPrice { get; set; }
    public decimal PredictedChangePercent { get; set; }
    public string Signal { get; set; } = string.Empty; // "Buy", "Sell", "Hold"
    public float Confidence { get; set; }
    public DateTime PredictionDate { get; set; }
    public List<decimal> HistoricalPrices { get; set; } = [];
    public List<decimal> PredictedPrices { get; set; } = [];
}
