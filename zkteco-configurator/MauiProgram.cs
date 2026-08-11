using Easy_Logger.Providers;
using Microsoft.Extensions.Logging;
using zkteco_configurator.Services;

namespace zkteco_configurator
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
				});

			builder.Services.AddMauiBlazorWebView();

			builder.Services.AddSingleton<SavedDeviceService>();

			#if DEBUG
			builder.Logging.ClearProviders();
			builder.Logging.AddBrowserLogger(x =>
			{
				x.LogLevels = [LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Critical];
			});

			builder.Services.AddBlazorWebViewDeveloperTools();
			#endif

			return builder.Build();
		}
	}
}
