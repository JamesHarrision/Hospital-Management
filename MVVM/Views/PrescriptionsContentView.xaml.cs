using HosipitalManager.MVVM.ViewModels;

namespace HosipitalManager.MVVM.Views;

public partial class PrescriptionsContentView : ContentView
{
	public PrescriptionsContentView()
	{
		InitializeComponent();
		BindingContext = new PrescriptionViewModel();
	}
}