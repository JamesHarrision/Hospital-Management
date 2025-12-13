using HosipitalManager.MVVM.ViewModels;

namespace HosipitalManager.MVVM.Views;

public partial class AppointmentPageView : ContentView
{
    public AppointmentPageView()
    {
        InitializeComponent();
        var vm = IPlatformApplication.Current?.Services.GetService<AppointmentViewModel>();
        BindingContext = vm;

        // 3. Dùng sự kiện Loaded thay cho OnAppearing
        this.Loaded += AppointmentPageView_Loaded;
    }
    private async void AppointmentPageView_Loaded(object sender, EventArgs e)
    {
        Loaded -= AppointmentPageView_Loaded;
        if (BindingContext is AppointmentViewModel vm)
        {
            // Gọi hàm load data mỗi khi View này được load lên cây giao diện
            await vm.LoadData();
        }
    }
}