using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quinnlytics.Data;
using Quinnlytics.Services;
using Quinnlytics.ViewModels;
using Quinnlytics.Views;

namespace Quinnlytics;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        // Register DbContext
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite("Data Source=quinnlytics.db")
        );

        // Register Views
        builder.Services.AddSingleton<ConfigurationPage>();

        // Register ViewModels
        builder.Services.AddSingletonWithShellRoute<MainPage, MainViewModel>(nameof(MainPage));

        // Register Services
        builder.Services.AddScoped<IRiotApiService, RiotApiService>();
        builder.Services.AddScoped<IDatabaseService, DatabaseService>();

        return builder.Build();
    }
}
