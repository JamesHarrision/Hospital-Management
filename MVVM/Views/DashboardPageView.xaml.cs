namespace HospitalManager.MVVM.Views;

public partial class DashboardPageView : ContentPage
{
    // Biến theo dõi trạng thái của Side-menu
    private bool isMenuVisible = true;

    public DashboardPageView()
    {
        InitializeComponent();
        BindingContext = new HospitalManager.MVVM.ViewModels.DashboardViewModel();
    }

    /// <summary>
    /// Xử lý sự kiện khi nhấn vào một nút trên Side-menu
    /// </summary>
    /// <summary>
    /// Xử lý sự kiện khi nhấn vào một tab trên Side-menu
    /// </summary>
    private void OnTabClicked(object sender, TappedEventArgs e)
    {
        // Lấy 'CommandParameter' từ 'sender'
        if (sender is BindableObject bindable && bindable.BindingContext is string tabName)
        {
            // Dùng 'tabName' này
        }
        // Hoặc lấy từ 'TappedEventArgs' (cách khác)
        else if (e.Parameter is string tabNameFromParam)
        {
            tabName = tabNameFromParam;
        }
        else
        {
            return; // Không lấy được tên tab
        }


        // 1. Ẩn tất cả các view nội dung
        DashboardContentView.IsVisible = false;
        PatientsContentView.IsVisible = false;
        DoctorsContentView.IsVisible = false;
        AppointmentsContentView.IsVisible = false;

        // 2. Reset màu nền của TẤT CẢ các tab về màu mặc định (màu trắng)
        DashboardTab.BackgroundColor = Colors.White;
        PatientsTab.BackgroundColor = Colors.White;
        DoctorsTab.BackgroundColor = Colors.White;
        AppointmentsTab.BackgroundColor = Colors.White;

        // 3. Hiển thị view tương ứng, cập nhật tiêu đề VÀ đổi màu tab được chọn
        // (Chúng ta dùng màu #F4F7FC - màu nền của trang - để làm màu 'active')
        Color activeColor = Color.FromHex("#C6E2FF");

        switch (tabName)
        {
            case "Dashboard":
                DashboardContentView.IsVisible = true;
                PageTitleLabel.Text = "Tổng quan bệnh viện";
                DashboardTab.BackgroundColor = activeColor; // Đặt màu active
                break;
            case "Patients":
                PatientsContentView.IsVisible = true;
                PageTitleLabel.Text = "Quản lý Bệnh nhân";
                PatientsTab.BackgroundColor = activeColor; // Đặt màu active
                break;
            case "Doctors":
                DoctorsContentView.IsVisible = true;
                PageTitleLabel.Text = "Quản lý Bác sĩ";
                DoctorsTab.BackgroundColor = activeColor; // Đặt màu active
                break;
            case "Appointments":
                AppointmentsContentView.IsVisible = true;
                PageTitleLabel.Text = "Quản lý Lịch hẹn";
                AppointmentsTab.BackgroundColor = activeColor; // Đặt màu active
                break;
        }
    }
}