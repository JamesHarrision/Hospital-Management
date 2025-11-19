using CommunityToolkit.Mvvm.ComponentModel;
using HosipitalManager.MVVM.Models;
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

        foreach (var p in Patients.Where(p => p.Status == "Chờ khám"))
        {
            WaitingQueue.Add(p);
        }
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