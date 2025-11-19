using HosipitalManager.MVVM.Views;

namespace HosipitalManager
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new DashboardPageView();
        }
    }
}
