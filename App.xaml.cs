using HosipitalManager.MVVM.Services;
using HosipitalManager.MVVM.Views;
using HospitalManager.MVVM.Views;
using Microsoft.Maui.Controls;

namespace HosipitalManager
{
    // App.xaml.cs
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Hiện màn hình Loading
            MainPage = new ContentPage
            {
                Content = new ActivityIndicator
                {
                    IsRunning = true,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };

            InitializeDataAsync();
        }

        private async void InitializeDataAsync()
        {
            // 1. Chờ hàm static Init chạy xong (Tạo bảng + Seed Data)
            // Vì _database giờ là static nên hàm Init static truy cập được, không còn lỗi.
            await LocalDatabaseService.Init();

            // 2. Vào màn hình chính (Lúc này DB đã có data fake sẵn sàng)
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage = new AppShell();
            });
        }
    }
}