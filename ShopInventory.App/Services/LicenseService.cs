// ================================================================
// LicenseService.cs — ShopInventory Pro (MAUI - Cross Platform)
// Supports: net9.0-windows, net9.0-android
// AL-HAJJ Corporation
// ================================================================

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
        public bool IsTrial { get; set; }
        public int DaysRemaining { get; set; }
        public bool CanStartTrial { get; set; } = true;
    }

    public class LicenseService
    {
        // Must match "SHOP" SecretKey in LicenseKeyGenerator
        private const string SecretKey = "AlHajj@ShopInventory#2024$SecretKey!";

        private static readonly string LicenseFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AlHajj",
            "ShopInventory",
            "license.dat"
        );

        private static readonly string TrialFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AlHajj",
            "ShopInventory",
            "trial.dat"
        );

        // ── Machine ID (Platform-Aware) ───────────────────────────
        public string GetMachineId()
        {
#if WINDOWS
            return GetWindowsMachineId();
#elif ANDROID
            return GetAndroidMachineId();
#else
            return GetFallbackMachineId();
#endif
        }

#if WINDOWS
        private string GetWindowsMachineId()
        {
            try
            {
                var components = new List<string>();

                // CPU ID
                using (var s = new System.Management.ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
                    foreach (var o in s.Get())
                        components.Add(o["ProcessorId"]?.ToString() ?? "");

                // Motherboard serial
                using (var s = new System.Management.ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard"))
                    foreach (var o in s.Get())
                        components.Add(o["SerialNumber"]?.ToString() ?? "");

                // Windows System UUID
                using (var s = new System.Management.ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct"))
                    foreach (var o in s.Get())
                        components.Add(o["UUID"]?.ToString() ?? "");

                var combined = string.Join("|", components.Where(c => !string.IsNullOrWhiteSpace(c)));
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(combined + SecretKey));
                return Convert.ToHexString(hash)[..16];
            }
            catch
            {
                return GetFallbackMachineId();
            }
        }
#endif

#if ANDROID
        private string GetAndroidMachineId()
        {
            try
            {
                // Android ID — unique per device+app install, stable and does not require permissions
                var androidId = Android.Provider.Settings.Secure.GetString(
                    Android.App.Application.Context.ContentResolver,
                    Android.Provider.Settings.Secure.AndroidId
                ) ?? "";

                // Build fingerprint — hardware model + manufacturer (does not change)
                var buildFingerprint = Android.OS.Build.Fingerprint ?? "";
                var combined = $"{androidId}|{buildFingerprint}";
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(combined + SecretKey));
                return Convert.ToHexString(hash)[..16];
            }
            catch
            {
                return GetFallbackMachineId();
            }
        }
#endif

        private string GetFallbackMachineId()
        {
            var fallback = Environment.MachineName + Environment.UserName;
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(fallback + SecretKey));
            return Convert.ToHexString(hash)[..16];
        }

        // ── Validate a key entered by client ──────────────────────
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

                if (machineId != GetMachineId())
                    return new LicenseResult
                    {
                        Status = LicenseStatus.WrongMachine,
                        Message = "This license is for a different device. Contact AL-HAJJ support."
                    };

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

        // ── 7-Day Free Trial Support ─────────────────────────────
        public bool CanStartTrial()
        {
            return !File.Exists(TrialFilePath);
        }

        public LicenseResult StartTrial()
        {
            if (!CanStartTrial())
            {
                return new LicenseResult
                {
                    Status = LicenseStatus.Expired,
                    Message = "Free trial has already been used on this device. Please purchase a license.",
                    CanStartTrial = false
                };
            }

            try
            {
                var machineId = GetMachineId();
                var now = DateTime.Now;
                var expiry = now.AddDays(7);
                var sigInput = $"{machineId}|{now:yyyy-MM-dd HH:mm:ss}|{expiry:yyyy-MM-dd HH:mm:ss}|{SecretKey}";
                var sig = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sigInput)));

                var trial = new TrialData
                {
                    MachineId = machineId,
                    StartDate = now,
                    ExpiryDate = expiry,
                    Signature = sig
                };

                var dir = Path.GetDirectoryName(TrialFilePath)!;
                Directory.CreateDirectory(dir);
                File.WriteAllText(TrialFilePath, JsonSerializer.Serialize(trial, new JsonSerializerOptions { WriteIndented = true }));

                var info = new LicenseInfo
                {
                    ClientName = "7-Day Free Trial",
                    MachineId = machineId,
                    ExpiryDate = expiry,
                    ActivatedOn = now
                };

                return new LicenseResult
                {
                    Status = LicenseStatus.Valid,
                    Info = info,
                    IsTrial = true,
                    DaysRemaining = 7,
                    CanStartTrial = false,
                    Message = "7-Day Free Trial activated successfully!"
                };
            }
            catch (Exception ex)
            {
                return new LicenseResult
                {
                    Status = LicenseStatus.NotActivated,
                    Message = $"Failed to activate free trial: {ex.Message}",
                    CanStartTrial = true
                };
            }
        }

        // ── Check on every app launch ─────────────────────────────
        public LicenseResult CheckCurrentLicense()
        {
            // 1. Check paid license first
            try
            {
                if (File.Exists(LicenseFilePath))
                {
                    var json = File.ReadAllText(LicenseFilePath);
                    var saved = JsonSerializer.Deserialize<SavedLicense>(json);
                    if (saved != null)
                    {
                        var result = ValidateLicenseKey(saved.Key);
                        if (result.IsValid)
                        {
                            result.CanStartTrial = false;
                            if (result.Info != null)
                            {
                                result.DaysRemaining = Math.Max(0, (result.Info.ExpiryDate.Date - DateTime.Now.Date).Days);
                            }
                            return result;
                        }
                        else if (result.Status == LicenseStatus.Expired)
                        {
                            result.CanStartTrial = false;
                            return result;
                        }
                    }
                }
            }
            catch
            {
                // Fall through to trial check
            }

            // 2. Check 7-day free trial
            try
            {
                if (File.Exists(TrialFilePath))
                {
                    var json = File.ReadAllText(TrialFilePath);
                    var trial = JsonSerializer.Deserialize<TrialData>(json);
                    if (trial != null)
                    {
                        var machineId = GetMachineId();
                        var sigInput = $"{trial.MachineId}|{trial.StartDate:yyyy-MM-dd HH:mm:ss}|{trial.ExpiryDate:yyyy-MM-dd HH:mm:ss}|{SecretKey}";
                        var expectedSig = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sigInput)));

                        if (trial.MachineId == machineId && trial.Signature == expectedSig)
                        {
                            var now = DateTime.Now;
                            if (now <= trial.ExpiryDate)
                            {
                                var daysRemaining = Math.Max(0, (trial.ExpiryDate.Date - now.Date).Days);
                                return new LicenseResult
                                {
                                    Status = LicenseStatus.Valid,
                                    IsTrial = true,
                                    DaysRemaining = daysRemaining,
                                    CanStartTrial = false,
                                    Info = new LicenseInfo
                                    {
                                        ClientName = "7-Day Free Trial",
                                        MachineId = machineId,
                                        ExpiryDate = trial.ExpiryDate,
                                        ActivatedOn = trial.StartDate
                                    },
                                    Message = $"Free Trial active ({daysRemaining} days remaining)."
                                };
                            }
                            else
                            {
                                return new LicenseResult
                                {
                                    Status = LicenseStatus.Expired,
                                    IsTrial = true,
                                    DaysRemaining = 0,
                                    CanStartTrial = false,
                                    Message = $"Your 7-day free trial expired on {trial.ExpiryDate:dd MMM yyyy}. Please contact AL-HAJJ Corporation to get a full license key."
                                };
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fall through
            }

            return new LicenseResult
            {
                Status = LicenseStatus.NotActivated,
                Message = "No license found. Please start your 7-day free trial or activate with a license key.",
                CanStartTrial = !File.Exists(TrialFilePath)
            };
        }

        // ── Helpers ───────────────────────────────────────────────
        private void SaveLicense(LicenseInfo info, string key)
        {
            var dir = Path.GetDirectoryName(LicenseFilePath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(LicenseFilePath,
                JsonSerializer.Serialize(new SavedLicense { Key = key, SavedAt = DateTime.Now }));
        }

        private string? DecryptLicenseKey(string key)
        {
            try
            {
                var base64 = key.Trim()
                    .Replace("\r", "").Replace("\n", "")
                    .Replace(" ", "").Replace("\t", "");

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
            catch { return null; }
        }

        private class SavedLicense
        {
            public string Key { get; set; } = "";
            public DateTime SavedAt { get; set; }
        }

        private class TrialData
        {
            public string MachineId { get; set; } = "";
            public DateTime StartDate { get; set; }
            public DateTime ExpiryDate { get; set; }
            public string Signature { get; set; } = "";
        }
    }
}