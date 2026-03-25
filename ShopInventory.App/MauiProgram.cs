using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopInventory.App.Services;
using ShopInventory.Application.Interfaces;
using ShopInventory.Infrastructure.Configuration;
using ShopInventory.Infrastructure.Data;
using ShopInventory.Infrastructure.Services;

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


        #if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        #endif

        builder.Services.AddHttpClient("ApiClient", client =>
        {
            client.BaseAddress = new Uri("https://localhost:7281/");
        });

        builder.Services.AddScoped(sp =>
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient"));

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "shopinventory.db");
        System.Diagnostics.Debug.WriteLine("DB PATH = " + dbPath);

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite($"Filename={dbPath}"),
              ServiceLifetime.Transient);

        builder.Services.AddTransient<IDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddSingleton<LoaderService>();
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Services.AddSingleton<LicenseService>();
        builder.Logging.AddDebug();
      
        builder.Services.AddSingleton<BillPdfService>();
        builder.Services.AddSingleton<CartService>();
        var app = builder.Build();

        // Scope only for migration
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
        }

        // 24-hour auto sync loop
        //_ = Task.Run(async () =>
        //{
        //    await Task.Delay(TimeSpan.FromSeconds(10)); // wait for app to load
        //    while (true)
        //    {
        //        try
        //        {
        //            using var scope = app.Services.CreateScope();
        //            var sync = scope.ServiceProvider.GetRequiredService<ISyncService>();
        //            await sync.SyncAsync();
        //            System.Diagnostics.Debug.WriteLine($"[AutoSync] Done at {DateTime.UtcNow}");
        //        }
        //        catch (Exception ex)
        //        {
        //            System.Diagnostics.Debug.WriteLine($"[AutoSync] Error: {ex.Message}");
        //        }
        //        await Task.Delay(TimeSpan.FromHours(24));
        //    }
        //});
        return app;
    }
}