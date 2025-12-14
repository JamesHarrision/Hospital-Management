using HosipitalManager.MVVM.ViewModels;

namespace HosipitalManager.MVVM.Views;

public partial class RevenueContentView : ContentView
{
	public RevenueContentView()
	{
        InitializeComponent();
        if (IPlatformApplication.Current?.Services != null)
        {
            var viewModel = IPlatformApplication.Current.Services.GetService<RevenueViewModel>();
            this.BindingContext = viewModel;
        }
    }
}