namespace CryptoStockDashboard.Models;

public class PricePoint
{
    public DateTime Date { get; set; }
    public float Price { get; set; }
}

public class TimeSeriesForecastInput
{
    public float Price { get; set; }
}

public class TimeSeriesForecastOutput
{
    public float[] ForecastedPrice { get; set; } = [];
    public float[] ConfidenceLowerBound { get; set; } = [];
    public float[] ConfidenceUpperBound { get; set; } = [];
}
