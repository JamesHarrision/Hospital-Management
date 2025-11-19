using HosipitalManager.MVVM.Views;
using HospitalManager.MVVM.Views;

namespace HosipitalManager
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }
    }
}
