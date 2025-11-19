namespace HosipitalManager.MVVM.Views;
using HosipitalManager.MVVM.Models;
using HosipitalManager.MVVM.ViewModels;
using System.Collections.ObjectModel;

public partial class DashboardPageView : ContentPage
{
    // Màu mặc định của tab (trong suốt để thấy gradient)
    readonly Color _normalColor = Colors.Transparent;

    // Màu hover
    readonly Color _hoverColor = Color.FromRgba(255, 255, 255, 0.25);

    // Màu tab đang được chọn
    readonly Color _activeColor = Color.FromRgba(255, 255, 255, 0.45);

    // Tab hiện đang được chọn
    Border? _selectedTab;
    public DashboardPageView()
    {
        InitializeComponent();
        BindingContext = new DashboardViewModel();
    }

    /// <summary>
    /// Xử lý sự kiện khi nhấn vào một nút trên Side-menu
    /// </summary>
    /// <summary>
    /// Xử lý sự kiện khi nhấn vào một tab trên Side-menu
    /// </summary>
    /// 

    private void OnTabPointerEntered(object sender, PointerEventArgs e)
    {
        if (sender is Border tab)
        {
            if (tab == _selectedTab) return; // Tab đang chọn thì không hover
            tab.BackgroundColor = _hoverColor;
        }
    }

    private void OnTabPointerExited(object sender, PointerEventArgs e)
    {
        if (sender is Border tab)
        {
            if (tab == _selectedTab) return;
            tab.BackgroundColor = _normalColor;
        }
    }
    private void OnTabClicked(object sender, TappedEventArgs e)
    {
        //// Lấy 'CommandParameter' từ 'sender'
        //if (sender is BindableObject bindable && bindable.BindingContext is string tabName)
        //{
        //    // Dùng 'tabName' này
        //}
        //// Hoặc lấy từ 'TappedEventArgs' (cách khác)
        //else if (e.Parameter is string tabNameFromParam)
        //{
        //    tabName = tabNameFromParam;
        //}
        //else
        //{
        //    return; // Không lấy được tên tab
        //}

        string? tabName = null;

        // Cách 1: Lấy CommandParameter
        if (e.Parameter is string cp)
        {
            tabName = cp;
        }
        else return;

        // Deselect tab trước đó
        if (_selectedTab != null)
            _selectedTab.BackgroundColor = _normalColor;

        // Gán tab mới được chọn
        if (sender is Border clickedTab)
        {
            _selectedTab = clickedTab;
            _selectedTab.BackgroundColor = _activeColor;
        }

        // 1. Ẩn tất cả các view nội dung
        DashboardContentView.IsVisible = false;
        PatientsContentView.IsVisible = false;
        PrescriptionsContentView.IsVisible = false;
        AppointmentsContentView.IsVisible = false;

        switch (tabName)
        {
            case "Dashboard":
                DashboardContentView.IsVisible = true;
                PageTitleLabel.Text = "Tổng quan bệnh viện";
                
                break;
            case "Patients":
                PatientsContentView.IsVisible = true;
                PageTitleLabel.Text = "Quản lý Bệnh nhân";
                
                break;
            case "Prescriptions":
                PrescriptionsContentView.IsVisible = true;
                PageTitleLabel.Text = "Quản lý Đơn thuốc";
                Console.WriteLine("Switched to Prescriptions tab");
                break;
            case "Appointments":
                AppointmentsContentView.IsVisible = true;
                PageTitleLabel.Text = "Quản lý Lịch hẹn";
                Console.WriteLine("Hello World");
                break;
        }
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if(BindingContext is DashboardViewModel vm)
        {
            await vm.LoadPatientsFromDbAsync();
        }    
    }
}