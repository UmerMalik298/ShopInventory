using Microsoft.Extensions.DependencyInjection;
using ShopInventory.App.Services;

namespace ShopInventory.App;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ShopSettingsService _settingsService;
    private readonly LicenseService _licenseService;

    public App(
        IServiceProvider serviceProvider,
        ShopSettingsService settingsService,
        LicenseService licenseService)
    {
        InitializeComponent();

        _serviceProvider = serviceProvider;
        _settingsService = settingsService;
        _licenseService = licenseService;
    }

    protected override Window CreateWindow(
        IActivationState? activationState)
    {
        var loadingPage = new ContentPage
        {
            Content = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 15,

                Children =
                {
                    new ActivityIndicator
                    {
                        IsRunning = true
                    },

                    new Label
                    {
                        Text = "Starting application..."
                    }
                }
            }
        };

        var window = new Window(loadingPage)
        {
            Title = string.Empty
        };

        LoadStartupPageAsync(window);

        return window;
    }

    private async void LoadStartupPageAsync(Window window)
    {
        try
        {
            // Run your existing licence/security validation here.
            //
            // Replace this comment with your actual LicenseService method.
            //
            // Example:
            //
            // var licenceIsValid =
            //     await _licenseService.ValidateAsync();
            //
            // if (!licenceIsValid)
            // {
            //     window.Page = licencePage;
            //     return;
            // }

            var settings =
                await _settingsService.GetSettingsAsync();

            var setupIsComplete =
                settings is not null &&
                settings.IsConfigured &&
                !string.IsNullOrWhiteSpace(settings.ShopName) &&
                !string.IsNullOrWhiteSpace(settings.LogoPath) &&
                File.Exists(settings.LogoPath);

            if (!setupIsComplete)
            {
                window.Title = "Shop Setup";

                window.Page = _serviceProvider
                    .GetRequiredService<ShopSetupPage>();

                return;
            }

            window.Title = string.Empty;

            window.Page = _serviceProvider
                .GetRequiredService<MainPage>();
        }
        catch (Exception ex)
        {
            window.Page = new ContentPage
            {
                Content = new VerticalStackLayout
                {
                    Padding = 30,
                    VerticalOptions = LayoutOptions.Center,
                    Spacing = 15,

                    Children =
                    {
                        new Label
                        {
                            Text = "The application could not start.",
                            FontSize = 24,
                            FontAttributes = FontAttributes.Bold,
                            HorizontalTextAlignment =
                                TextAlignment.Center
                        },

                        new Label
                        {
                            Text = ex.Message,
                            HorizontalTextAlignment =
                                TextAlignment.Center
                        }
                    }
                }
            };
        }
    }
}