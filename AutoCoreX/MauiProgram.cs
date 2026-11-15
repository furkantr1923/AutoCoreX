using System.Globalization;
using Microsoft.Extensions.Logging;
using AutoCoreX.Services;

namespace AutoCoreX;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var kultur = new CultureInfo("tr-TR");
        CultureInfo.DefaultThreadCurrentCulture = kultur;
        CultureInfo.DefaultThreadCurrentUICulture = kultur;
        CultureInfo.CurrentCulture = kultur;
        CultureInfo.CurrentUICulture = kultur;

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddSingleton<Veri>();
        builder.Services.AddSingleton<Ayarlar>();
        builder.Services.AddSingleton<Cihaz>();
        builder.Services.AddSingleton<ObdServisi>();
        builder.Services.AddSingleton<GpsServisi>();
        builder.Services.AddSingleton<RaporServisi>();
        builder.Services.AddScoped<Efekt>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
