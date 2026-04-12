// ================================================================
// AL-HAJJ Corporation — License Key Generator
// Your Company  : AL-HAJJ Corporation
// Your Clients  : SheerRabbani (ShopInventory), Gujjar (PetrolPump)
// ================================================================

using System.Security.Cryptography;
using System.Text;

// ── Client Software Registry ──────────────────────────────────────
// Each entry = one client company and their software product
// Add new clients here as you build more software
var clientSoftware = new List<ClientSoftware>
{
    new ClientSoftware
    {
        Id          = 1,
        ClientName  = "SheerRabbani Autos",
        SoftwareName = "ShopInventory Pro",
        Description = "Retail & Shop Inventory Management",
        Code        = "SHOP",
        SecretKey   = "AlHajj@ShopInventory#2024$SecretKey!",
        IdMethod    = MachineIdMethod.WindowsWMI
    },
    new ClientSoftware
    {
        Id          = 2,
        ClientName  = "Gujjar Petroleum",
        SoftwareName = "PetrolPump Manager",
        Description = "Petrol Pump & Fuel Station Management",
        Code        = "PETRO",
        SecretKey   = "GujjarPetroleum@UmerFarooq#2024$PK!",
        IdMethod    = MachineIdMethod.MauiDeviceInfo
    },
    // ── Add future clients here ───────────────────────────────────
    // new ClientSoftware
    // {
    //     Id          = 3,
    //     ClientName  = "ABC Pharmacy",
    //     SoftwareName = "PharmaCare",
    //     Description = "Pharmacy & Medicine Inventory",
    //     Code        = "PHARM",
    //     SecretKey   = "AlHajj@PharmaCare#2024$SecretKey!",
    //     IdMethod    = MachineIdMethod.MauiDeviceInfo
    // },
};

// ── Main ──────────────────────────────────────────────────────────
Console.Clear();
PrintBanner();

while (true)
{
    var selected = ChooseClientSoftware(clientSoftware);
    if (selected == null) break;

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkCyan;
    Console.WriteLine($"  ▶  Client   : {selected.ClientName}");
    Console.WriteLine($"  ▶  Software : {selected.SoftwareName}");
    Console.ResetColor();
    Console.WriteLine();

    // Prompt for device/machine ID
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write(selected.IdMethod == MachineIdMethod.MauiDeviceInfo
        ? "  Device ID (shown on app's activation screen) : "
        : "  Machine ID (shown on app's activation screen): ");
    Console.ResetColor();

    var deviceId = Console.ReadLine()?.Trim() ?? "";
    if (string.IsNullOrWhiteSpace(deviceId))
    {
        PrintError("Device ID cannot be empty.");
        continue;
    }

    // MAUI DeviceInfo apps always return uppercase — normalize here too
    if (selected.IdMethod == MachineIdMethod.MauiDeviceInfo)
        deviceId = deviceId.ToUpper();

    // Load this client's activation history
    var historyFile = GetHistoryFilePath(selected.Code);
    var records = LoadRecords(historyFile);
    var existing = records.FirstOrDefault(r => r.DeviceId == deviceId);

    if (existing != null)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine("  ── Known Device ──────────────────────────────────────");
        Console.WriteLine($"  User       : {existing.UserName}");
        Console.WriteLine($"  Location   : {existing.Location}");
        Console.WriteLine($"  First seen : {existing.FirstSeen:dd MMM yyyy}");
        Console.WriteLine($"  Last key   : {existing.LastKeyDate:dd MMM yyyy}  (expires {existing.LastExpiry:dd MMM yyyy})");
        Console.WriteLine($"  Keys issued: {existing.KeyCount}");
        Console.WriteLine("  ──────────────────────────────────────────────────────");
        Console.ResetColor();
        Console.WriteLine();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("  (New device — not seen before)");
        Console.ResetColor();
    }

    // End-user name (the person at the client who uses the software)
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("  End User Name (e.g. Ali / Manager / Branch 2): ");
    Console.ResetColor();
    var userName = Console.ReadLine()?.Trim() ?? "";
    if (string.IsNullOrWhiteSpace(userName))
        userName = existing?.UserName ?? selected.ClientName;

    // Duration
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("  Duration (e.g. 7 days / 3 months / 1 year) [default 12 months]: ");
    Console.ResetColor();
    var expiryDate = ParseDuration(Console.ReadLine()?.Trim().ToLower());

    // Generate
    var licenseKey = GenerateLicenseKey(deviceId, userName, expiryDate, selected.SecretKey);

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("  ╔══════════════════════════════════════════════════════╗");
    Console.WriteLine($"  ║  AL-HAJJ Corporation — License Key                  ║");
    Console.WriteLine("  ╠══════════════════════════════════════════════════════╣");
    Console.WriteLine($"  ║  Client   : {selected.ClientName,-41}║");
    Console.WriteLine($"  ║  Software : {selected.SoftwareName,-41}║");
    Console.WriteLine($"  ║  User     : {userName,-41}║");
    Console.WriteLine($"  ║  Device   : {deviceId,-41}║");
    Console.WriteLine($"  ║  Expires  : {expiryDate:dd MMM yyyy}                                   ║");
    Console.WriteLine("  ╠══════════════════════════════════════════════════════╣");
    Console.WriteLine("  ║  LICENSE KEY:                                        ║");
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine();
    Console.WriteLine($"  {licenseKey}");
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("  ║  Copy the entire key above and send via WhatsApp     ║");
    Console.WriteLine("  ╚══════════════════════════════════════════════════════╝");
    Console.ResetColor();
    Console.WriteLine();

    // Save record
    if (existing != null)
    {
        existing.LastKeyDate = DateTime.Now;
        existing.LastExpiry = expiryDate;
        existing.KeyCount++;
        if (!string.IsNullOrWhiteSpace(userName)) existing.UserName = userName;
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("  Location/Branch (optional, Enter to skip): ");
        Console.ResetColor();
        var location = Console.ReadLine()?.Trim() ?? "";
        records.Add(new ActivationRecord
        {
            DeviceId = deviceId,
            UserName = userName,
            Location = location,
            FirstSeen = DateTime.Now,
            LastKeyDate = DateTime.Now,
            LastExpiry = expiryDate,
            KeyCount = 1
        });
    }
    SaveRecords(historyFile, records);

    Console.WriteLine();
    Console.Write("  Generate another key? (y/n): ");
    if (Console.ReadLine()?.Trim().ToLower() != "y") break;
    Console.WriteLine();
    PrintBanner();
}

Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine();
Console.WriteLine("  AL-HAJJ Corporation. Goodbye.");
Console.ResetColor();
Console.WriteLine();


// ── Functions ─────────────────────────────────────────────────────

static void PrintBanner()
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine();
    Console.WriteLine("  ╔══════════════════════════════════════════════════════╗");
    Console.WriteLine("  ║            AL-HAJJ CORPORATION                       ║");
    Console.WriteLine("  ║            Software License Key Generator            ║");
    Console.WriteLine("  ║            Your trusted software partner             ║");
    Console.WriteLine("  ╚══════════════════════════════════════════════════════╝");
    Console.ResetColor();
    Console.WriteLine();
}

static ClientSoftware? ChooseClientSoftware(List<ClientSoftware> catalog)
{
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("  ┌──────────────────────────────────────────────────────────────┐");
    Console.WriteLine("  │  Select Client Software                                      │");
    Console.WriteLine("  ├────┬─────────────────────┬────────────────────────────────── ┤");
    Console.WriteLine("  │ #  │ Client               │ Software                          │");
    Console.WriteLine("  ├────┼─────────────────────┼───────────────────────────────────┤");
    foreach (var p in catalog)
        Console.WriteLine($"  │ [{p.Id}] │ {p.ClientName,-20} │ {p.SoftwareName,-33} │");
    Console.WriteLine("  │ [0] │ Exit                                                    │");
    Console.WriteLine("  └──────────────────────────────────────────────────────────────┘");
    Console.ResetColor();
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("  Enter choice: ");
    Console.ResetColor();

    var input = Console.ReadLine()?.Trim();
    if (!int.TryParse(input, out int choice) || choice == 0) return null;

    var selected = catalog.FirstOrDefault(p => p.Id == choice);
    if (selected == null)
    {
        PrintError("Invalid choice. Try again.");
        return ChooseClientSoftware(catalog);
    }
    return selected;
}

static DateTime ParseDuration(string? input)
{
    if (string.IsNullOrWhiteSpace(input)) return DateTime.Now.AddMonths(12);
    var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    int value = 12; string unit = "month";
    if (parts.Length >= 1 && int.TryParse(parts[0], out int v)) value = v;
    if (parts.Length >= 2) unit = parts[1];
    return unit switch
    {
        "day" or "days" => DateTime.Now.AddDays(value),
        "month" or "months" => DateTime.Now.AddMonths(value),
        "year" or "years" => DateTime.Now.AddYears(value),
        _ => DateTime.Now.AddMonths(12)
    };
}

static string GenerateLicenseKey(string deviceId, string userName, DateTime expiry, string secret)
{
    var data = $"{deviceId}|{userName}|{expiry:yyyy-MM-dd HH:mm:ss}";
    var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    using var aes = Aes.Create();
    aes.Key = keyBytes; aes.IV = keyBytes[..16];
    aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
    using var enc = aes.CreateEncryptor();
    var dataBytes = Encoding.UTF8.GetBytes(data);
    return Convert.ToBase64String(enc.TransformFinalBlock(dataBytes, 0, dataBytes.Length));
}

static string GetHistoryFilePath(string code) =>
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"activations_{code.ToLower()}.json");

static List<ActivationRecord> LoadRecords(string path)
{
    if (!File.Exists(path)) return new();
    try { return System.Text.Json.JsonSerializer.Deserialize<List<ActivationRecord>>(File.ReadAllText(path)) ?? new(); }
    catch { return new(); }
}

static void SaveRecords(string path, List<ActivationRecord> records) =>
    File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(records,
        new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

static void PrintError(string msg)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"  ✗ {msg}");
    Console.ResetColor();
}


// ── Models ────────────────────────────────────────────────────────

enum MachineIdMethod { WindowsWMI, MauiDeviceInfo }

class ClientSoftware
{
    public int Id { get; set; }
    public string ClientName { get; set; } = "";   // e.g. SheerRabbani Autos
    public string SoftwareName { get; set; } = "";   // e.g. ShopInventory Pro
    public string Description { get; set; } = "";
    public string Code { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public MachineIdMethod IdMethod { get; set; }
}

record ActivationRecord
{
    public string DeviceId { get; set; } = "";
    public string UserName { get; set; } = "";   // end user at the client
    public string Location { get; set; } = "";   // branch or city
    public DateTime FirstSeen { get; set; }
    public DateTime LastKeyDate { get; set; }
    public DateTime LastExpiry { get; set; }
    public int KeyCount { get; set; }
}