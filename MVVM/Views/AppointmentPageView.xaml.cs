using HosipitalManager.MVVM.ViewModels;

namespace HosipitalManager.MVVM.Views;

public partial class AppointmentPageView : ContentView
{
    private AppointmentViewModel _viewModel;

    public AppointmentPageView()
    {
        InitializeComponent();

        // Khởi tạo và gán ViewModel
        _viewModel = new AppointmentViewModel();
        BindingContext = _viewModel;
    }
}