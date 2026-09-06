using ShopInventory.Domain.Entities.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShopInventory.App.Services
{
    public class ShopSettingsService
    {
        private const string SettingsFileName = "shop-settings.json";

        private string SettingsFilePath =>
            Path.Combine(FileSystem.AppDataDirectory, SettingsFileName);

        public async Task<ShopSettings?> GetSettingsAsync()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    return null;
                }

                var json = await File.ReadAllTextAsync(SettingsFilePath);

                return JsonSerializer.Deserialize<ShopSettings>(json);
            }
            catch
            {
                return null;
            }
        }

        public async Task SaveSettingsAsync(
            string shopName,
            string selectedLogoPath,
            string themeColor = "#cc0000")
        {
            if (string.IsNullOrWhiteSpace(shopName))
            {
                throw new ArgumentException("Shop name is required.");
            }

            if (string.IsNullOrWhiteSpace(selectedLogoPath) ||
                !File.Exists(selectedLogoPath))
            {
                throw new ArgumentException("Please select a valid logo.");
            }

            var logoDirectory = Path.Combine(
                FileSystem.AppDataDirectory,
                "ShopAssets");

            Directory.CreateDirectory(logoDirectory);

            var extension = Path.GetExtension(selectedLogoPath);

            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".png";
            }

            var savedLogoPath = Path.Combine(
                logoDirectory,
                $"shop-logo{extension}");

            File.Copy(
                selectedLogoPath,
                savedLogoPath,
                overwrite: true);

            var settings = new ShopSettings
            {
                ShopName = shopName.Trim(),
                LogoPath = savedLogoPath,
                ThemeColor = string.IsNullOrWhiteSpace(themeColor) ? "#cc0000" : themeColor.Trim(),
                IsConfigured = true
            };

            var json = JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            await File.WriteAllTextAsync(SettingsFilePath, json);
        }

        public async Task ClearSettingsAsync()
        {
            if (File.Exists(SettingsFilePath))
            {
                File.Delete(SettingsFilePath);
            }

            var logoDirectory = Path.Combine(
                FileSystem.AppDataDirectory,
                "ShopAssets");

            if (Directory.Exists(logoDirectory))
            {
                Directory.Delete(logoDirectory, recursive: true);
            }

            await Task.CompletedTask;
        }

        public async Task<string?> GetLogoDataUrlAsync()
        {
            var settings = await GetSettingsAsync();

            if (settings is null ||
                string.IsNullOrWhiteSpace(settings.LogoPath) ||
                !File.Exists(settings.LogoPath))
            {
                return null;
            }

            var bytes = await File.ReadAllBytesAsync(settings.LogoPath);
            var extension = Path.GetExtension(settings.LogoPath).ToLowerInvariant();

            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".svg" => "image/svg+xml",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                _ => "image/png"
            };

            return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
        }
    }

}
