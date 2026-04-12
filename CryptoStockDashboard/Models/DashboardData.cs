namespace CryptoStockDashboard.Models;

public class DashboardData
{
    public List<CryptoAsset> CryptoAssets { get; set; } = [];
    public List<StockQuote> StockQuotes { get; set; } = [];
    public List<PricePrediction> Predictions { get; set; } = [];
    public DateTime LastRefreshed { get; set; }
}
