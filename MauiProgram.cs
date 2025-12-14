using HosipitalManager.MVVM.Services;
using HosipitalManager.MVVM.ViewModels;
using HosipitalManager.MVVM.Views;
using HospitalManager.MVVM.ViewModels;
using HospitalManager.MVVM.Views;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.Extensions.Logging;
using QuestPDF.Infrastructure;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace HosipitalManager
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseLiveCharts()
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("fa-solid-900.ttf", "FontAwesomeSolid");
                });
            builder.Services.AddSingleton<LocalDatabaseService>();
            builder.Services.AddSingleton<RevenueViewModel>();
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<DashboardPageView>();
            builder.Services.AddTransient<NewAppointmentViewModel>();
            builder.Services.AddTransient<NewAppointmentPageView>();
            builder.Services.AddTransient<AppointmentViewModel>();
            QuestPDF.Settings.License = LicenseType.Community;

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
