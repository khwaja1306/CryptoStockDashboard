using CryptoStockDashboard.Models;
using CryptoStockDashboard.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CryptoStockDashboard.Pages;

public class StocksModel : PageModel
{
    private readonly IStockService _stockService;

    public List<StockQuote> Stocks { get; set; } = [];

    public StocksModel(IStockService stockService)
    {
        _stockService = stockService;
    }

    public async Task OnGetAsync()
    {
        Stocks = await _stockService.GetSnpStocksAsync();
    }
}
