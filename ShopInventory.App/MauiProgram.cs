using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using ShopInventory.Application.Interfaces;
using ShopInventory.Infrastructure;
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


        builder.Services.AddScoped<ShopInventory.Application.Interfaces.IDbContext>(
    sp => sp.GetRequiredService<ApplicationDbContext>()
);
        builder.Services.AddHttpClient("ApiClient", client =>
        {
            client.BaseAddress = new Uri("https://localhost:7281/");
        });

        builder.Services.AddScoped(sp =>
            sp.GetRequiredService<IHttpClientFactory>()
              .CreateClient("ApiClient"));


        builder.Services.AddInfrastructure(builder.Configuration); // Your custom DI extension

        builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();


        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "shopinventory.db");
        System.Diagnostics.Debug.WriteLine("DB PATH = " + dbPath);
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite($"Filename={dbPath}"));
        



                     var app = builder.Build();


        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();

            // Auto-sync in background if online
            var sync = scope.ServiceProvider.GetRequiredService<ISyncService>();
            _ = Task.Run(async () => await sync.SyncAsync());
        }

        return app;
    }
}
