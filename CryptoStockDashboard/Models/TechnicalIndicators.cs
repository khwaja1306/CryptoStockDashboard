namespace CryptoStockDashboard.Models;

public class TechnicalIndicators
{
    public string AssetId { get; set; } = string.Empty;
    public decimal Rsi { get; set; }
    public decimal MacdLine { get; set; }
    public decimal SignalLine { get; set; }
    public decimal MacdHistogram { get; set; }
}
