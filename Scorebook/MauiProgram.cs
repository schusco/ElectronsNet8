using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Logging;
using Scorebook.Components;
using Scorebook.Coordinators;
using Scorebook.Services;
using System.Reflection;

namespace Scorebook
{
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
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Segoe MDL2 Assets", "SegoeIcon");
                });
            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddTransient<ScorebookViewModel>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<FieldView>();
            builder.Services.AddTransient<Scoreboard>();
            builder.Services.AddTransient<CommandArea>();
            builder.Services.AddTransient<LineupSidebar>();
            builder.Services.AddTransient<RosterCoordinator>();
            builder.Services.AddTransient<GameCoordinator>();
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName().Name;
            using var stream = assembly.GetManifestResourceStream($"{assemblyName}.appsettings.json");
            var configBuilder = new ConfigurationBuilder().AddJsonStream(stream);
                
#if DEBUG
            builder.Logging.AddDebug();
            using var devStream = assembly.GetManifestResourceStream($"{assemblyName}.appsettings.Development.json");
            if (devStream != null)
            {
                configBuilder.AddJsonStream(devStream);
            }

#endif
            var configuration = configBuilder.Build();
            builder.Configuration.AddConfiguration(configuration);
            return builder.Build();
        }
    }
}
