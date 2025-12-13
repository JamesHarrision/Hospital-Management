using HosipitalManager.MVVM.ViewModels;

namespace HosipitalManager.MVVM.Views;

public partial class NewAppointmentPageView : ContentPage
{
	public NewAppointmentPageView(NewAppointmentViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }
}