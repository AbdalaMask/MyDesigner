using Microsoft.Extensions.Logging;
using ClassicToolkit.Maui;
using CommunityToolkit.Maui;
using HMIControl.Core;
using VagabondK.Indicators.Maui;


namespace __PROJECT_NAME__
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseHMIControlCore()
                .UseMauiCommunityToolkit()
                .UseVagabondIndicators()
                .UseClassicToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
