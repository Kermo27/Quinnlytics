using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quinnlytics.Data;
using Quinnlytics.Services;
using Quinnlytics.ViewModels;
using Quinnlytics.Views;
using Microsoft.Extensions.Http;

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
        builder.Services.AddSingleton<MainPage>();

        // Register ViewModels
        builder.Services.AddSingletonWithShellRoute<MainPage, MainViewModel>(nameof(MainPage));
        builder.Services.AddSingletonWithShellRoute<AddPlayerPage, AddPlayerViewModel>(nameof(AddPlayerPage));
        builder.Services.AddSingleton<PlayerViewModel>();
        
        // Register Services
        builder.Services.AddSingleton<IRiotApiService, RiotApiService>();
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        builder.Services.AddSingleton<IGameVersionService, GameVersionService>();
        
        builder.Services.AddHttpClient("RiotApiClient", client =>
        {
            client.BaseAddress = new Uri("https://ddragon.leagueoflegends.com/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        return builder.Build();
    }
}
