using CryptoStockDashboard.Hubs;
using CryptoStockDashboard.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

builder.Services.AddMemoryCache();

// Register HttpClient and services
builder.Services.AddHttpClient<ICryptoService, CryptoService>(client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "CryptoStockDashboard/1.0");
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddHttpClient<IStockService, StockService>(client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddSingleton<IPredictionService, PredictionService>();
builder.Services.AddHostedService<DashboardBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapHub<DashboardHub>("/dashboardHub");

app.Run();
