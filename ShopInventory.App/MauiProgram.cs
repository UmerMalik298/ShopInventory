using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

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
