using Microsoft.Extensions.DependencyInjection;
using ShopInventory.App.Services;

namespace ShopInventory.App;

public partial class ShopSetupPage : ContentPage
{
    private readonly ShopSettingsService _settingsService;
    private readonly IServiceProvider _serviceProvider;

    private string? _selectedLogoPath;

    public ShopSetupPage(
        ShopSettingsService settingsService,
        IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _settingsService = settingsService;
        _serviceProvider = serviceProvider;
    }

    private async void OnSelectLogoClicked(
        object sender,
        EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(
                new PickOptions
                {
                    PickerTitle = "Select shop logo",
                    FileTypes = FilePickerFileType.Images
                });

            if (result is null)
            {
                return;
            }

            _selectedLogoPath = result.FullPath;

            LogoPreview.Source =
                ImageSource.FromFile(_selectedLogoPath);
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Error",
                $"Could not select the logo: {ex.Message}",
                "OK");
        }
    }

    private async void OnSaveClicked(
        object sender,
        EventArgs e)
    {
        var shopName = ShopNameEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(shopName))
        {
            await DisplayAlert(
                "Required",
                "Please enter the shop name.",
                "OK");

            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedLogoPath))
        {
            await DisplayAlert(
                "Required",
                "Please select the shop logo.",
                "OK");

            return;
        }

        try
        {
            Loader.IsVisible = true;
            Loader.IsRunning = true;

            await _settingsService.SaveSettingsAsync(
                shopName,
                _selectedLogoPath);

            var mainPage =
                _serviceProvider.GetRequiredService<MainPage>();

            var window =
                Microsoft.Maui.Controls.Application.Current?
                    .Windows
                    .FirstOrDefault();

            if (window is null)
            {
                throw new InvalidOperationException(
                    "Application window was not found.");
            }

 
            window.Page = mainPage;
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Error",
                ex.Message,
                "OK");
        }
        finally
        {
            Loader.IsVisible = false;
            Loader.IsRunning = false;
        }
    }
}