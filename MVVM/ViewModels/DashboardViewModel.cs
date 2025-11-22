using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HosipitalManager.MVVM.Models;
using HosipitalManager.MVVM.Services;
using HospitalManager.MVVM.Models;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Linq;

namespace HospitalManager.MVVM.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    // Dữ liệu thống kê
    [ObservableProperty]
    private ObservableCollection<SummaryCard> summaryCards;

    // Hoạt động gần đây (Có thể giữ hoặc bỏ nếu đã dùng Queue)
    [ObservableProperty]
    private ObservableCollection<RecentActivity> recentActivities;

    [ObservableProperty]
    private string userName = "Dr. Khang";

    [ObservableProperty]
    private string userAvatar = "person_placeholder.png";

    //DANH MỤC THUỐC CÓ SẴN (Giả lập kho thuốc của bệnh viện)
    [ObservableProperty]
    private ObservableCollection<MedicineProduct> availableMedicines;

    // Thuốc đang được chọn trong Dropdown (Picker)
    [ObservableProperty]
    private MedicineProduct selectedMedicineProduct;

    // Trạng thái menu (Mở/Đóng)
    [ObservableProperty]
    private bool isMenuExpanded = true;

    // Độ rộng menu: Mở = 250, Đóng = 70
    [ObservableProperty]
    private double sidebarWidth = 250;

    // Góc xoay của nút mũi tên (0 độ hoặc 180 độ)
    [ObservableProperty]
    private double menuArrowRotation = 0;

    // Độ mờ của chữ (1 = hiện, 0 = ẩn)
    [ObservableProperty]
    private double menuTextOpacity = 1;

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsMenuExpanded = !IsMenuExpanded;

        if (IsMenuExpanded)
        {
            // MỞ RỘNG
            SidebarWidth = 250;
            MenuArrowRotation = 0;   // Mũi tên quay về trái
            MenuTextOpacity = 1;     // Hiện chữ
        }
        else
        {
            // THU NHỎ
            SidebarWidth = 70;       // Chỉ đủ chỗ cho Icon
            MenuArrowRotation = 180; // Mũi tên quay sang phải
            MenuTextOpacity = 0;     // Ẩn chữ
        }
    }

    public DashboardViewModel()
    {
        // 1. Khởi tạo các danh sách
        SummaryCards = new ObservableCollection<SummaryCard>();
        RecentActivities = new ObservableCollection<RecentActivity>();

        Patients = new ObservableCollection<Patient>();      // Database
        WaitingQueue = new ObservableCollection<Patient>();  // Hàng đợi RỖNG
        Prescriptions = new ObservableCollection<Prescription>();

        

        // 2. Nạp dữ liệu (Từ các file partial khác)
        LoadSummaryCards();
        LoadPrescriptions();
        LoadSamplePatients(); // Load database mẫu
        LoadMedicineCatalog();

        foreach (var p in Patients.Where(p => p.Status == "Chờ khám"))
        {
            WaitingQueue.Add(p);
        }
    }

    private void LoadMedicineCatalog()
    {
        // 1. Tạo Service
        var medService = new MedicineService();
        // 2. Lấy dữ liệu và đổ vào ObservableCollection
        var listFromService = medService.GetMedicineCatalog();
        AvailableMedicines = new ObservableCollection<MedicineProduct>(listFromService);
    }

    private void LoadSummaryCards()
    {
        SummaryCards = new ObservableCollection<SummaryCard>
        {
            new SummaryCard {
                Title = "Tổng số Bệnh nhân",
                Value = "4,250",
                Icon = "person.png",
                ChangePercentage = "+12% so với tháng trước",
                CardColor = Color.FromArgb("#36A2EB") // Màu xanh dương
            },
            new SummaryCard {
                Title = "Lịch hẹn hôm nay",
                Value = "52",
                Icon = "calendar.png",
                ChangePercentage = "+5% so với hôm qua",
                CardColor = Color.FromArgb("#FF6384") // Màu đỏ hồng
            },
            new SummaryCard {
                Title = "Phòng trống",
                Value = "15",
                Icon = "bed.png",
                ChangePercentage = "25% Đã sử dụng",
                CardColor = Color.FromArgb("#4BC0C0") // Màu xanh ngọc
            },
            new SummaryCard {
                Title = "Doanh thu (Tháng)",
                Value = "$350,000",
                Icon = "cash.png",
                ChangePercentage = "-2% so với mục tiêu",
                CardColor = Color.FromArgb("#FF9F40") // Màu cam
            }
        };
    }
}