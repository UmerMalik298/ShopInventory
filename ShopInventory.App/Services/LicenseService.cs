using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ShopInventory.App.Services
{
    public class LicenseInfo
    {
        public string ClientName { get; set; } = "";
        public string MachineId { get; set; } = "";
        public DateTime ExpiryDate { get; set; }
        public DateTime ActivatedOn { get; set; }
    }

    public enum LicenseStatus
    {
        Valid,
        Expired,
        InvalidKey,
        WrongMachine,
        NotActivated
    }

    public class LicenseResult
    {
        public LicenseStatus Status { get; set; }
        public LicenseInfo? Info { get; set; }
        public string Message { get; set; } = "";
        public bool IsValid => Status == LicenseStatus.Valid;
    }

    public class LicenseService
    {
        // YOUR SECRET KEY - change this to something only you know
        // Must match exactly in LicenseKeyGenerator tool
        private const string SecretKey = "ShopInventory@YourName#2024$SecretKey!";

        private static readonly string LicenseFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ShopInventory",
            "license.dat"
        );

        // Get unique machine fingerprint using hardware info
        public string GetMachineId()
        {
            try
            {
                var components = new List<string>();

                // CPU ID
                using (var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
                {
                    foreach (var obj in searcher.Get())
                        components.Add(obj["ProcessorId"]?.ToString() ?? "");
                }

                // Motherboard serial
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard"))
                {
                    foreach (var obj in searcher.Get())
                        components.Add(obj["SerialNumber"]?.ToString() ?? "");
                }

                // Windows installation ID (stable across reboots)
                using (var searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct"))
                {
                    foreach (var obj in searcher.Get())
                        components.Add(obj["UUID"]?.ToString() ?? "");
                }

                var combined = string.Join("|", components.Where(c => !string.IsNullOrWhiteSpace(c)));
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(combined + SecretKey));
                return Convert.ToHexString(hash)[..16]; // 16-char machine ID
            }
            catch
            {
                // Fallback to machine name + username hash
                var fallback = Environment.MachineName + Environment.UserName;
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(fallback + SecretKey));
                return Convert.ToHexString(hash)[..16];
            }
        }

        // Validate a license key entered by client
        public LicenseResult ValidateLicenseKey(string licenseKey)
        {
            try
            {
                var decoded = DecryptLicenseKey(licenseKey);
                if (decoded == null)
                    return new LicenseResult { Status = LicenseStatus.InvalidKey, Message = "Invalid license key format." };

                var parts = decoded.Split('|');
                if (parts.Length != 3)
                    return new LicenseResult { Status = LicenseStatus.InvalidKey, Message = "Invalid license key." };

                var machineId = parts[0];
                var clientName = parts[1];
                var expiryDate = DateTime.Parse(parts[2]);
                var currentMachineId = GetMachineId();

                if (machineId != currentMachineId)
                    return new LicenseResult { Status = LicenseStatus.WrongMachine, Message = "This license key is for a different machine. Contact support." };

                if (expiryDate < DateTime.Now)
                    return new LicenseResult
                    {
                        Status = LicenseStatus.Expired,
                        Message = $"License expired on {expiryDate:dd MMM yyyy}. Please renew.",
                        Info = new LicenseInfo { ClientName = clientName, MachineId = machineId, ExpiryDate = expiryDate }
                    };

                var info = new LicenseInfo
                {
                    ClientName = clientName,
                    MachineId = machineId,
                    ExpiryDate = expiryDate,
                    ActivatedOn = DateTime.Now
                };

                SaveLicense(info, licenseKey);

                return new LicenseResult { Status = LicenseStatus.Valid, Info = info, Message = "License activated successfully." };
            }
            catch
            {
                return new LicenseResult { Status = LicenseStatus.InvalidKey, Message = "Invalid license key." };
            }
        }

        // Check license on every app launch
        public LicenseResult CheckCurrentLicense()
        {
            try
            {
                if (!File.Exists(LicenseFilePath))
                    return new LicenseResult { Status = LicenseStatus.NotActivated, Message = "No license found. Please activate." };

                var json = File.ReadAllText(LicenseFilePath);
                var saved = JsonSerializer.Deserialize<SavedLicense>(json);
                if (saved == null)
                    return new LicenseResult { Status = LicenseStatus.NotActivated, Message = "License file corrupted." };

                // Re-validate the stored key
                return ValidateLicenseKey(saved.Key);
            }
            catch
            {
                return new LicenseResult { Status = LicenseStatus.NotActivated, Message = "Could not read license." };
            }
        }

        private void SaveLicense(LicenseInfo info, string key)
        {
            var dir = Path.GetDirectoryName(LicenseFilePath)!;
            Directory.CreateDirectory(dir);
            var saved = new SavedLicense { Key = key, SavedAt = DateTime.Now };
            File.WriteAllText(LicenseFilePath, JsonSerializer.Serialize(saved));
        }

        private string? DecryptLicenseKey(string key)
        {
            try
            {
                // Strip only whitespace and newlines (in case of copy/paste artifacts)
                var base64 = key.Trim()
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Replace(" ", "")
                    .Replace("\t", "");

                // Re-add Base64 padding if missing
                switch (base64.Length % 4)
                {
                    case 2: base64 += "=="; break;
                    case 3: base64 += "="; break;
                }
                var bytes = Convert.FromBase64String(base64);
                var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(SecretKey));

                using var aes = Aes.Create();
                aes.Key = keyBytes;
                aes.IV = keyBytes[..16];
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                var decrypted = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                return null;
            }
        }

        private class SavedLicense
        {
            public string Key { get; set; } = "";
            public DateTime SavedAt { get; set; }
        }
    }
}