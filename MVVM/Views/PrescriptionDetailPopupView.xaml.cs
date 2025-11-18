using HosipitalManager.MVVM.ViewModels;

namespace HosipitalManager.MVVM.Views;

public partial class PrescriptionDetailPopupView : ContentView
{
	public PrescriptionDetailPopupView()
	{
		InitializeComponent();
        BindingContext = new PrescriptionViewModel();
    }
}