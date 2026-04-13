using CryptoStockDashboard.Models;
using CryptoStockDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CryptoStockDashboard.Pages;

[Authorize]
public class PredictionsModel : PageModel
{
    private readonly ICryptoService _cryptoService;
    private readonly IStockService _stockService;
    private readonly IPredictionService _predictionService;

    public List<PricePrediction> CryptoPredictions { get; set; } = [];
    public List<PricePrediction> StockPredictions { get; set; } = [];

    public PredictionsModel(ICryptoService cryptoService, IStockService stockService, IPredictionService predictionService)
    {
        _cryptoService = cryptoService;
        _stockService = stockService;
        _predictionService = predictionService;
    }

    public async Task OnGetAsync()
    {
        var cryptos = await _cryptoService.GetTopCryptoAssetsAsync(5);
        foreach (var crypto in cryptos)
        {
            var history = await _cryptoService.GetHistoricalPricesAsync(crypto.Id);
            CryptoPredictions.Add(_predictionService.PredictPrice(crypto.Symbol, "Crypto", history, crypto.CurrentPrice));
        }

        var stocks = await _stockService.GetSnpStocksAsync();
        foreach (var stock in stocks.Take(10))
        {
            var history = await _stockService.GetHistoricalPricesAsync(stock.Symbol);
            StockPredictions.Add(_predictionService.PredictPrice(stock.Symbol, "Stock", history, stock.Price));
        }
    }
}
