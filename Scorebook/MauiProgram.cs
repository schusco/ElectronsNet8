using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Logging;
using ScoreboardApi.Client.Services;
using Scorebook.Components;
using Scorebook.Coordinators;
using Scorebook.Services;
using CommunityToolkit.Maui;
using ApiService = Scorebook.Services.ApiService;

namespace Scorebook
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>().UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Segoe MDL2 Assets", "SegoeIcon");
                });
#if DEBUG
            string baseAddress = "https://h503cfkn-7249.usw3.devtunnels.ms/";
#else
            string baseAddress = "https://webservices.electronsbaseball.com/";
#endif                  
            builder.Services.AddElectronsApiClients<LocalStorageService>(baseAddress);
            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddTransient<ScorebookViewModel>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<FieldView>();
            builder.Services.AddTransient<Scoreboard>();
            builder.Services.AddTransient<CommandArea>();
            builder.Services.AddTransient<LineupSidebar>();
            builder.Services.AddTransient<RosterCoordinator>();
            builder.Services.AddTransient<GameCoordinator>();
            builder.Services.AddSingleton<GameUpdateManager>();
            var info = new UserInfo { DeviceInfo = $"Game_Engine_{DeviceInfo.Platform}_{DeviceInfo.DeviceType}", PlayerId = Constants.PlayerId };
            builder.Services.AddSingleton<IUserInfo>(info);            

#if DEBUG
            builder.Logging.AddDebug();
#endif
            
            return builder.Build();
        }
    }
}
