namespace CryptoStockDashboard.Models
{
    public class CoinGeckoMarketData
    {
        public string? Id { get; set; }
        public string? Symbol { get; set; }
        public string? Name { get; set; }
        public string? Image { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("current_price")]
        public decimal CurrentPrice { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("market_cap")]
        public decimal MarketCap { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("total_volume")]
        public decimal TotalVolume { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("price_change_percentage_24h")]
        public decimal PriceChangePercentage24h { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("high_24h")]
        public decimal High24h { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("low_24h")]
        public decimal Low24h { get; set; }
    }
}
