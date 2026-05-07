using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using PlantillaDotNet.Application.Interfaces;
using PlantillaDotNet.Maui.Auth;
using PlantillaDotNet.Maui.Data;
using PlantillaDotNet.Maui.Services;
using PlantillaDotNet.UI.Auth;

namespace PlantillaDotNet.Maui;

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

        const string apiBaseUrl = "https://10.0.2.2:5001"; // Android emulator → localhost
        builder.Services.AddSingleton(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

        builder.Services.AddAuthorizationCore();
        builder.Services.AddSingleton<MauiJwtAuthStateProvider>();
        builder.Services.AddSingleton<AuthenticationStateProvider>(sp => sp.GetRequiredService<MauiJwtAuthStateProvider>());
        builder.Services.AddSingleton<ITokenManager>(sp => sp.GetRequiredService<MauiJwtAuthStateProvider>());

        builder.Services.AddSingleton<ISyncService, MauiSyncService>();
        builder.Services.AddSingleton(typeof(IOfflineRepository<>), typeof(SqliteRepository<>));

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
