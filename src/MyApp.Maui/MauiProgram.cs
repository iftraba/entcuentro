using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using MyApp.Application.Interfaces;
using MyApp.Maui.Auth;
using MyApp.Maui.Data;
using MyApp.Maui.Services;
using MyApp.UI.Auth;

namespace MyApp.Maui;

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
