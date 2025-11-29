using HosipitalManager.MVVM.Views;

namespace HosipitalManager
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(NewAppointmentPageView), typeof(NewAppointmentPageView));
        }
    }
}
