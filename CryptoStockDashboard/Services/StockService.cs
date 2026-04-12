using CryptoStockDashboard.Models;

namespace CryptoStockDashboard.Services;

public class StockService : IStockService
{
    private readonly ILogger<StockService> _logger;
    private readonly HttpClient _httpClient;

    private static readonly (string Symbol, string Name, decimal BasePrice)[] SnpStocks =
    [
        ("AAPL", "Apple Inc.", 189.50m),
        ("MSFT", "Microsoft Corp.", 420.30m),
        ("GOOGL", "Alphabet Inc.", 155.70m),
        ("AMZN", "Amazon.com Inc.", 185.40m),
        ("NVDA", "NVIDIA Corp.", 870.50m),
        ("META", "Meta Platforms Inc.", 505.60m),
        ("TSLA", "Tesla Inc.", 175.80m),
        ("BRK.B", "Berkshire Hathaway B", 410.20m),
        ("JPM", "JPMorgan Chase & Co.", 198.30m),
        ("V", "Visa Inc.", 282.40m),
        ("UNH", "UnitedHealth Group", 525.10m),
        ("JNJ", "Johnson & Johnson", 156.80m),
        ("WMT", "Walmart Inc.", 165.30m),
        ("PG", "Procter & Gamble", 162.50m),
        ("MA", "Mastercard Inc.", 458.70m)
    ];

    public StockService(HttpClient httpClient, ILogger<StockService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<List<StockQuote>> GetSnpStocksAsync()
    {
        // Generate realistic simulated stock data with market-hour-aware variations
        var random = new Random(DateTime.UtcNow.Minute);
        var stocks = SnpStocks.Select(s =>
        {
            var changePercent = Math.Round((decimal)(random.NextDouble() * 6 - 3), 2);
            var change = Math.Round(s.BasePrice * changePercent / 100, 2);
            var price = Math.Round(s.BasePrice + change, 2);

            return new StockQuote
            {
                Symbol = s.Symbol,
                Name = s.Name,
                Price = price,
                Change = change,
                ChangePercent = changePercent,
                High = Math.Round(price * (1 + (decimal)(random.NextDouble() * 0.02)), 2),
                Low = Math.Round(price * (1 - (decimal)(random.NextDouble() * 0.02)), 2),
                Open = Math.Round(s.BasePrice + (decimal)(random.NextDouble() * 4 - 2), 2),
                PreviousClose = s.BasePrice,
                Volume = (long)(random.NextDouble() * 50000000 + 5000000),
                LastUpdated = DateTime.UtcNow
            };
        }).ToList();

        return Task.FromResult(stocks);
    }

    public Task<List<decimal>> GetHistoricalPricesAsync(string symbol, int days = 30)
    {
        var stock = SnpStocks.FirstOrDefault(s => s.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        var basePrice = stock.BasePrice > 0 ? stock.BasePrice : 150m;

        var random = new Random(symbol.GetHashCode());
        var prices = new List<decimal>();
        var current = basePrice;

        for (int i = 0; i < days; i++)
        {
            current += (decimal)(random.NextDouble() * 10 - 5);
            current = Math.Max(current, basePrice * 0.85m);
            prices.Add(Math.Round(current, 2));
        }

        return Task.FromResult(prices);
    }
}
