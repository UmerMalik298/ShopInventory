using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopInventory.App.Services;
using ShopInventory.Application.Interfaces;
using ShopInventory.Infrastructure.Configuration;
using ShopInventory.Infrastructure.Data;

namespace ShopInventory.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddHttpClient("ApiClient", client =>
        {
            client.BaseAddress = new Uri("https://localhost:7281/");
        });

        builder.Services.AddScoped(sp =>
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient"));

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "shopinventory.db");
        System.Diagnostics.Debug.WriteLine("DB PATH = " + dbPath);

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite($"Filename={dbPath}"));

        builder.Services.AddScoped<IDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        builder.Services.AddInfrastructure(builder.Configuration);

        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Services.AddSingleton<LicenseService>();
        builder.Logging.AddDebug();

        var app = builder.Build();

        // Scope only for migration
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
        }

        // Start sync with its own fresh scope
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var sync = scope.ServiceProvider.GetRequiredService<ISyncService>();
                Console.WriteLine(">>> About to call SyncAsync");
                sync.SyncAsync();
                Console.WriteLine(">>> SyncAsync completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($">>> SYNC CATCH: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($">>> INNER: {ex.InnerException?.Message}");
            }
        });

        return app;
    }
}