using CryptoStockDashboard.Models;
using CryptoStockDashboard.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CryptoStockDashboard.Pages;

public class CryptoModel : PageModel
{
    private readonly ICryptoService _cryptoService;

    public List<CryptoAsset> CryptoAssets { get; set; } = [];

    public CryptoModel(ICryptoService cryptoService)
    {
        _cryptoService = cryptoService;
    }

    public async Task OnGetAsync()
    {
        CryptoAssets = await _cryptoService.GetTopCryptoAssetsAsync(10);

        foreach (var crypto in CryptoAssets)
        {
            crypto.Indicators = await _cryptoService.GetTechnicalIndicatorsAsync(crypto.Id);
        }
    }
}
