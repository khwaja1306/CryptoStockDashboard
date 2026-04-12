using CryptoStockDashboard.Models;

namespace CryptoStockDashboard.Services;

public interface IStockService
{
    Task<List<StockQuote>> GetSnpStocksAsync();
    Task<List<decimal>> GetHistoricalPricesAsync(string symbol, int days = 30);
}
