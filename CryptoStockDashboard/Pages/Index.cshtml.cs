using CryptoStockDashboard.Models;
using CryptoStockDashboard.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CryptoStockDashboard.Pages;

public class IndexModel : PageModel
{
    private readonly ICryptoService _cryptoService;
    private readonly IStockService _stockService;
    private readonly IPredictionService _predictionService;

    public List<CryptoAsset> TopCrypto { get; set; } = [];
    public List<StockQuote> TopStocks { get; set; } = [];
    public List<PricePrediction> TopPredictions { get; set; } = [];

    public IndexModel(ICryptoService cryptoService, IStockService stockService, IPredictionService predictionService)
    {
        _cryptoService = cryptoService;
        _stockService = stockService;
        _predictionService = predictionService;
    }

    public async Task OnGetAsync()
    {
        var cryptoTask = _cryptoService.GetTopCryptoAssetsAsync(5);
        var stockTask = _stockService.GetSnpStocksAsync();
        await Task.WhenAll(cryptoTask, stockTask);

        TopCrypto = await cryptoTask;
        TopStocks = (await stockTask).Take(5).ToList();

        TopPredictions = [];
        foreach (var crypto in TopCrypto.Take(3))
        {
            var history = await _cryptoService.GetHistoricalPricesAsync(crypto.Id);
            TopPredictions.Add(_predictionService.PredictPrice(crypto.Symbol, "Crypto", history, crypto.CurrentPrice));
        }
        foreach (var stock in TopStocks.Take(3))
        {
            var history = await _stockService.GetHistoricalPricesAsync(stock.Symbol);
            TopPredictions.Add(_predictionService.PredictPrice(stock.Symbol, "Stock", history, stock.Price));
        }
    }
}
