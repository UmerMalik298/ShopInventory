using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using ShopInventory.Infrastructure.Data;
using ShopInventory.Infrastructure;

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


        //builder.Services.AddInfrastructure(); // Your custom DI extension

        builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();


        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "shopinventory.db");
        System.Diagnostics.Debug.WriteLine("DB PATH = " + dbPath);

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite($"Filename={dbPath}")
        );

      
        var app = builder.Build();

       
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        }

        return app;
    }
}
