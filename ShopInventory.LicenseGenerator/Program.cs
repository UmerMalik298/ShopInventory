// ================================================================
// LicenseKeyGenerator - Console app for YOU (the developer)
// Create a new Console App project: ShopInventory.LicenseGenerator
// Run this on your machine to generate keys for each client
// ================================================================

using System.Security.Cryptography;
using System.Text;

// YOUR SECRET KEY - must match exactly what's in LicenseService.cs
const string SecretKey = "ShopInventory@YourName#2024$SecretKey!";

Console.Clear();
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔════════════════════════════════════════╗");
Console.WriteLine("║   ShopInventory License Key Generator  ║");
Console.WriteLine("╚════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine();

while (true)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("Client Machine ID: ");
    Console.ResetColor();
    var machineId = Console.ReadLine()?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(machineId))
    {
        Console.WriteLine("Machine ID cannot be empty.");
        continue;
    }

    ClientRecord? existing = null;
    // Show stored client info if this machine was registered before
    var historyFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clients.json");
    var clients = new List<ClientRecord>();
    if (File.Exists(historyFile))
    {
        try { clients = System.Text.Json.JsonSerializer.Deserialize<List<ClientRecord>>(File.ReadAllText(historyFile)) ?? new(); }
        catch { clients = new(); }
    }

    existing = clients.FirstOrDefault(c => c.MachineId == machineId);
    if (existing != null)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine("  ── Known Device ──────────────────────────");
        Console.WriteLine($"  Client     : {existing.ClientName}");
        Console.WriteLine($"  Location   : {existing.Location}");
        Console.WriteLine($"  First seen : {existing.FirstSeen:dd MMM yyyy}");
        Console.WriteLine($"  Last key   : {existing.LastKeyDate:dd MMM yyyy}  (expires {existing.LastExpiry:dd MMM yyyy})");
        Console.WriteLine($"  Keys issued: {existing.KeyCount}");
        Console.WriteLine("  ──────────────────────────────────────────");
        Console.ResetColor();
        Console.WriteLine();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  (New device — not seen before)");
        Console.ResetColor();
    }

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("Client Name: ");
    Console.ResetColor();
    var clientName = Console.ReadLine()?.Trim() ?? "Client";

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("License duration in months (default 12): ");
    Console.ResetColor();
    var monthsInput = Console.ReadLine()?.Trim();
    int months = int.TryParse(monthsInput, out var m) ? m : 12;

    var expiryDate = DateTime.Now.AddMonths(months);
    var key = GenerateLicenseKey(machineId, clientName, expiryDate);

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("════════════════════════════════════════");
    Console.WriteLine($"  Client     : {clientName}");
    Console.WriteLine($"  Machine ID : {machineId}");
    Console.WriteLine($"  Expires    : {expiryDate:dd MMM yyyy}");
    Console.WriteLine();
    Console.WriteLine("  LICENSE KEY:");
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine();
    Console.WriteLine($"  {FormatKey(key)}");
    Console.WriteLine();
    Console.WriteLine("  (Copy the entire key above exactly as shown)");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("════════════════════════════════════════");
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine("Send this key to your client via WhatsApp/SMS.");

    // Save/update client record
    if (existing != null)
    {
        existing.LastKeyDate = DateTime.Now;
        existing.LastExpiry = expiryDate;
        existing.KeyCount++;
        // Update name/location if changed
        if (existing.ClientName != clientName && clientName != "Client")
            existing.ClientName = clientName;
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("  Client location/city (optional, press Enter to skip): ");
        Console.ResetColor();
        var location = Console.ReadLine()?.Trim() ?? "";
        clients.Add(new ClientRecord
        {
            MachineId = machineId,
            ClientName = clientName,
            Location = location,
            FirstSeen = DateTime.Now,
            LastKeyDate = DateTime.Now,
            LastExpiry = expiryDate,
            KeyCount = 1
        });
    }
    File.WriteAllText(historyFile, System.Text.Json.JsonSerializer.Serialize(clients, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

    Console.WriteLine();
    Console.Write("Generate another key? (y/n): ");
    if (Console.ReadLine()?.Trim().ToLower() != "y") break;
    Console.WriteLine();
}


static string GenerateLicenseKey(string machineId, string clientName, DateTime expiryDate)
{
    var data = $"{machineId}|{clientName}|{expiryDate:yyyy-MM-dd HH:mm:ss}";
    var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(SecretKey));

    using var aes = Aes.Create();
    aes.Key = keyBytes;
    aes.IV = keyBytes[..16];
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;

    using var encryptor = aes.CreateEncryptor();
    var dataBytes = Encoding.UTF8.GetBytes(data);
    var encrypted = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
    return Convert.ToBase64String(encrypted);
}

static string FormatKey(string key)
{
    // Just display the raw Base64 key as-is
    // No formatting - client copies the whole thing
    return key;
}

// ── Data model ──────────────────────────────────────────────────
record ClientRecord
{
    public string MachineId { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string Location { get; set; } = "";
    public DateTime FirstSeen { get; set; }
    public DateTime LastKeyDate { get; set; }
    public DateTime LastExpiry { get; set; }
    public int KeyCount { get; set; }
}