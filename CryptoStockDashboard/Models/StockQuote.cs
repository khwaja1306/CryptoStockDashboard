namespace CryptoStockDashboard.Models;

public class StockQuote
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Open { get; set; }
    public decimal PreviousClose { get; set; }
    public long Volume { get; set; }
    public DateTime LastUpdated { get; set; }
}
