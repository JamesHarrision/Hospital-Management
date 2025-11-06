namespace HospitalManager.MVVM.Views;
public partial class DashboardPageView : ContentPage
{
	public DashboardPageView()
	{
		InitializeComponent();
		BindingContext = new HospitalManager.MVVM.ViewModels.DashboardViewModel();
    }
}