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

        // ── BACKUP — runs before anything touches the DB ──────────────────
        // Creates a copy of the DB file dated today, e.g.:
        //   shopinventory.db.backup-20260330-1420
        // Location: same folder as the DB itself (FileSystem.AppDataDirectory)
        // On Windows: C:\Users\<user>\AppData\Local\<AppName>\
        // On Mac:     /Users/<user>/.local/share/<AppName>/
        // Keeps only the 5 most recent backups — older ones are deleted.
        try
        {
            if (File.Exists(dbPath))
            {
                var backupFolder = Path.GetDirectoryName(dbPath)!;
                var backupPath = dbPath + $".backup-{DateTime.Now:yyyyMMdd-HHmm}";

                // Only create one backup per minute (avoids duplicates on fast restarts)
                if (!File.Exists(backupPath))
                    File.Copy(dbPath, backupPath);

                // Delete old backups — keep only latest 5
                var oldBackups = Directory.GetFiles(backupFolder, "shopinventory.db.backup-*")
                    .OrderByDescending(f => f)
                    .Skip(5);

                foreach (var old in oldBackups)
                    File.Delete(old);
            }
        }
        catch (Exception ex)
        {
            // Never crash the app because of a backup failure
            System.Diagnostics.Debug.WriteLine($"[Backup] Failed: {ex.Message}");
        }

        // ── Error log — writes crashes to a readable text file ────────────
        // Location: same folder as the DB
        //   shopinventory.db  →  error.log  (same directory)
        // You can ask the client to send you this file when something breaks.
        var logPath = Path.Combine(FileSystem.AppDataDirectory, "error.log");
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            try
            {
                var msg = $"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}]\n{e.ExceptionObject}\n\n";
                File.AppendAllText(logPath, msg);
            }
            catch { }
        };

        // ── Services ──────────────────────────────────────────────────────
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite($"Filename={dbPath}"),
            ServiceLifetime.Scoped);

        builder.Services.AddTransient<IDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddSingleton<LoaderService>();
        builder.Services.AddSingleton<LicenseService>();
        builder.Services.AddSingleton<BillPdfService>();
        builder.Services.AddSingleton<CartService>();

        builder.Logging.AddDebug();

        var app = builder.Build();

        // ── Migration + SQLite tuning ─────────────────────────────────────
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.Database.Migrate();

            db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
            db.Database.ExecuteSqlRaw("PRAGMA synchronous=NORMAL;");
            db.Database.ExecuteSqlRaw("PRAGMA cache_size=-32000;");
            db.Database.ExecuteSqlRaw("PRAGMA temp_store=MEMORY;");
        }

        return app;
    }
}