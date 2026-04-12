using System.Net;
using System.Text.Json;
using CryptoStockDashboard.Models;
using Microsoft.Extensions.Caching.Memory;

namespace CryptoStockDashboard.Services;

public class CryptoService : ICryptoService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CryptoService> _logger;
    private readonly IMemoryCache _cache;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private const int MaxRetries = 3;

    public CryptoService(HttpClient httpClient, ILogger<CryptoService> logger, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
    }

    public async Task<List<CryptoAsset>> GetTopCryptoAssetsAsync(int count = 10)
    {
        var cacheKey = $"crypto_top_{count}";
        if (_cache.TryGetValue(cacheKey, out List<CryptoAsset>? cached) && cached is not null)
            return cached;

        try
        {
            var url = $"https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd&order=market_cap_desc&per_page={count}&page=1&sparkline=false&price_change_percentage=24h";
            var response = await GetStringWithRetryAsync(url);
            var coins = JsonSerializer.Deserialize<List<CoinGeckoMarketData>>(response, JsonOptions);

            if (coins is null)
                return GenerateFallbackCryptoData();

            var result = coins.Select(c => new CryptoAsset
            {
                Id = c.Id ?? string.Empty,
                Symbol = c.Symbol?.ToUpper() ?? string.Empty,
                Name = c.Name ?? string.Empty,
                CurrentPrice = c.CurrentPrice,
                MarketCap = c.MarketCap,
                Volume24h = c.TotalVolume,
                PriceChangePercent24h = c.PriceChangePercentage24h,
                High24h = c.High24h,
                Low24h = c.Low24h,
                ImageUrl = c.Image ?? string.Empty,
                LastUpdated = DateTime.UtcNow
            }).ToList();

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(1));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch crypto data from CoinGecko, using fallback data");
            return GenerateFallbackCryptoData();
        }
    }

    public async Task<List<decimal>> GetHistoricalPricesAsync(string coinId, int days = 30)
    {
        var cacheKey = $"crypto_history_{coinId}_{days}";
        if (_cache.TryGetValue(cacheKey, out List<decimal>? cached) && cached is not null)
            return cached;

        try
        {
            var url = $"https://api.coingecko.com/api/v3/coins/{coinId}/market_chart?vs_currency=usd&days={days}";
            var response = await GetStringWithRetryAsync(url);
            using var doc = JsonDocument.Parse(response);
            var prices = doc.RootElement.GetProperty("prices");

            var result = prices.EnumerateArray()
                .Select(p => (decimal)p[1].GetDouble())
                .ToList();

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch historical crypto prices for {CoinId}", coinId);
            return GenerateFallbackHistoricalPrices();
        }
    }

    /// <summary>
    /// Generates a deterministic list of fallback cryptocurrency market data to use when the CoinGecko API is unavailable.
    /// </summary>
    /// <remarks>
    /// Uses a fixed random seed (42) to produce consistent, reproducible results across calls.
    /// Each asset's price is varied by up to ±5% from its base price, and additional market statistics
    /// (market cap, 24h volume, price change percentage, 24h high/low) are randomly generated around the adjusted price.
    /// The fallback dataset includes 10 well-known cryptocurrencies: BTC, ETH, BNB, SOL, XRP, ADA, DOGE, DOT, AVAX, and LINK.
    /// </remarks>
    /// <returns>
    /// A <see cref="List{T}"/> of <see cref="CryptoAsset"/> objects containing simulated market data for 10 major cryptocurrencies.
    /// </returns>
    private static List<CryptoAsset> GenerateFallbackCryptoData()
    {
        var random = new Random(42);
        var cryptos = new (string Id, string Symbol, string Name, decimal BasePrice)[]
        {
            ("bitcoin", "BTC", "Bitcoin", 67500m),
            ("ethereum", "ETH", "Ethereum", 3450m),
            ("binancecoin", "BNB", "BNB", 580m),
            ("solana", "SOL", "Solana", 145m),
            ("ripple", "XRP", "XRP", 0.52m),
            ("cardano", "ADA", "Cardano", 0.45m),
            ("dogecoin", "DOGE", "Dogecoin", 0.12m),
            ("polkadot", "DOT", "Polkadot", 7.2m),
            ("avalanche-2", "AVAX", "Avalanche", 35m),
            ("chainlink", "LINK", "Chainlink", 14.5m)
        };

        return cryptos.Select(c =>
        {
            var variation = (decimal)(random.NextDouble() * 0.1 - 0.05);
            var price = c.BasePrice * (1 + variation);
            return new CryptoAsset
            {
                Id = c.Id,
                Symbol = c.Symbol,
                Name = c.Name,
                CurrentPrice = Math.Round(price, 2),
                MarketCap = Math.Round(price * (decimal)(random.NextDouble() * 1000000000 + 100000000), 0),
                Volume24h = Math.Round((decimal)(random.NextDouble() * 5000000000 + 500000000), 0),
                PriceChangePercent24h = Math.Round((decimal)(random.NextDouble() * 10 - 5), 2),
                High24h = Math.Round(price * 1.03m, 2),
                Low24h = Math.Round(price * 0.97m, 2),
                ImageUrl = string.Empty,
                LastUpdated = DateTime.UtcNow
            };
        }).ToList();
    }

    private static List<decimal> GenerateFallbackHistoricalPrices()
    {
        var random = new Random(42);
        var prices = new List<decimal>();
        var basePrice = 67000m;
        for (int i = 0; i < 30; i++)
        {
            basePrice += (decimal)(random.NextDouble() * 2000 - 1000);
            prices.Add(Math.Round(basePrice, 2));
        }
        return prices;
    }

    public async Task<TechnicalIndicators> GetTechnicalIndicatorsAsync(string coinId)
    {
        // MACD needs 26+9 = 35 periods minimum; fetch 60 days for a comfortable buffer
        var prices = await GetHistoricalPricesAsync(coinId, days: 60);

        return new TechnicalIndicators
        {
            AssetId = coinId,
            Rsi = CalculateRsi(prices),
            MacdLine = CalculateMacdLine(prices),
            SignalLine = CalculateSignalLine(prices),
            MacdHistogram = CalculateMacdLine(prices) - CalculateSignalLine(prices)
        };
    }

    private static decimal CalculateRsi(List<decimal> prices, int period = 14)
    {
        if (prices.Count <= period)
            return 50m; // neutral default when insufficient data

        decimal avgGain = 0m, avgLoss = 0m;

        for (int i = 1; i <= period; i++)
        {
            var change = prices[i] - prices[i - 1];
            if (change >= 0) avgGain += change;
            else avgLoss -= change;
        }

        avgGain /= period;
        avgLoss /= period;

        for (int i = period + 1; i < prices.Count; i++)
        {
            var change = prices[i] - prices[i - 1];
            if (change >= 0)
            {
                avgGain = (avgGain * (period - 1) + change) / period;
                avgLoss = avgLoss * (period - 1) / period;
            }
            else
            {
                avgGain = avgGain * (period - 1) / period;
                avgLoss = (avgLoss * (period - 1) - change) / period;
            }
        }

        if (avgLoss == 0m)
            return 100m;

        var rs = avgGain / avgLoss;
        return Math.Round(100m - 100m / (1m + rs), 2);
    }

    private static decimal CalculateEma(List<decimal> prices, int period)
    {
        if (prices.Count == 0)
            return 0m;

        var multiplier = 2m / (period + 1);
        var ema = prices[0];

        for (int i = 1; i < prices.Count; i++)
        {
            ema = (prices[i] - ema) * multiplier + ema;
        }

        return ema;
    }

    private static decimal CalculateMacdLine(List<decimal> prices)
    {
        return CalculateEma(prices, 12) - CalculateEma(prices, 26);
    }

    private static decimal CalculateSignalLine(List<decimal> prices)
    {
        if (prices.Count < 26)
            return 0m;

        // Build MACD series, then take 9-period EMA of it
        var macdSeries = new List<decimal>();
        for (int i = 26; i <= prices.Count; i++)
        {
            var slice = prices.GetRange(0, i);
            macdSeries.Add(CalculateEma(slice, 12) - CalculateEma(slice, 26));
        }

        return CalculateEma(macdSeries, 9);
    }

    private async Task<string> GetStringWithRetryAsync(string url)
    {
        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            using var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (attempt == MaxRetries)
                    response.EnsureSuccessStatusCode();

                var retryAfter = response.Headers.RetryAfter?.Delta
                    ?? TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));

                _logger.LogWarning("CoinGecko rate limit hit, retrying in {Delay}s (attempt {Attempt}/{Max})",
                    retryAfter.TotalSeconds, attempt + 1, MaxRetries);

                await Task.Delay(retryAfter);
                continue;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        throw new HttpRequestException("Max retries exceeded for CoinGecko API");
    }
}