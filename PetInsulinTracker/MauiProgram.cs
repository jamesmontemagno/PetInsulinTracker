using Microsoft.Extensions.Logging;
using PetInsulinTracker.Services;
using PetInsulinTracker.ViewModels;
using PetInsulinTracker.Views;

namespace PetInsulinTracker;

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
			});

		// Services
		builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
		builder.Services.AddSingleton<INotificationService, NotificationService>();
		builder.Services.AddSingleton<HttpClient>();
		builder.Services.AddSingleton<ISyncService, SyncService>();

		// ViewModels
		builder.Services.AddTransient<PetListViewModel>();
		builder.Services.AddTransient<AddEditPetViewModel>();
		builder.Services.AddTransient<PetDetailViewModel>();
		builder.Services.AddTransient<InsulinLogViewModel>();
		builder.Services.AddTransient<FeedingLogViewModel>();
		builder.Services.AddTransient<WeightLogViewModel>();
		builder.Services.AddTransient<VetInfoViewModel>();
		builder.Services.AddTransient<ScheduleViewModel>();
		builder.Services.AddTransient<ShareViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();

		// Views
		builder.Services.AddTransient<PetListPage>();
		builder.Services.AddTransient<AddEditPetPage>();
		builder.Services.AddTransient<PetDetailPage>();
		builder.Services.AddTransient<InsulinLogPage>();
		builder.Services.AddTransient<FeedingLogPage>();
		builder.Services.AddTransient<WeightLogPage>();
		builder.Services.AddTransient<VetInfoPage>();
		builder.Services.AddTransient<SchedulePage>();
		builder.Services.AddTransient<SharePage>();
		builder.Services.AddTransient<SettingsPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
