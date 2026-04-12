using CryptoStockDashboard.Models;

namespace CryptoStockDashboard.Services;

public interface ICryptoService
{
    /// <summary>
    /// Retrieves the top cryptocurrency assets ranked by market capitalization in descending order.
    /// </summary>
    /// <param name="count">The number of top crypto assets to retrieve. Defaults to 10.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a list of
    /// <see cref="CryptoAsset"/> objects with current market data including price, volume, and 24-hour changes.
    /// Falls back to generated data if the external API call fails.
    /// </returns>
    Task<List<CryptoAsset>> GetTopCryptoAssetsAsync(int count = 10);

    /// <summary>
    /// Retrieves historical daily prices in USD for a specified cryptocurrency over a given time period.
    /// </summary>
    /// <param name="coinId">The unique identifier of the cryptocurrency (e.g., "bitcoin", "ethereum").</param>
    /// <param name="days">The number of past days of price history to retrieve. Defaults to 30.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a list of
    /// <see cref="decimal"/> values representing the historical prices in USD, ordered chronologically.
    /// Falls back to generated data if the external API call fails.
    /// </returns>
    Task<List<decimal>> GetHistoricalPricesAsync(string coinId, int days = 30);

    /// <summary>
    /// Computes RSI and MACD technical indicators for a cryptocurrency using its historical price data.
    /// </summary>
    /// <param name="coinId">The unique identifier of the cryptocurrency (e.g., "bitcoin", "ethereum").</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// <see cref="TechnicalIndicators"/> object with RSI, MACD line, signal line, and histogram values.
    /// </returns>
    Task<TechnicalIndicators> GetTechnicalIndicatorsAsync(string coinId);
}
