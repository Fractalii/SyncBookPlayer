using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SyncBookPlayer.ViewModel;

namespace SyncBookPlayer
{
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
            //builder.Services.AddTransient<PlayerViewModel>();
            //builder.Services.AddTransient<BookPlayer>();
            return builder.Build();
        }
    }
}
