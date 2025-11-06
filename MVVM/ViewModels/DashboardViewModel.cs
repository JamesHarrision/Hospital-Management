using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using HospitalManager.MVVM.Models;
using Microsoft.Maui.Graphics;

namespace HospitalManager.MVVM.ViewModels; // SỬA: HospitalManagement -> HospitalManager

public partial class DashboardViewModel : ObservableObject
{
    // Dữ liệu giả lập cho các thẻ tóm tắt, sử dụng lớp SummaryCard từ Models
    [ObservableProperty]
    private ObservableCollection<SummaryCard> summaryCards;

    // Dữ liệu giả lập cho danh sách hoạt động, sử dụng lớp RecentActivity từ Models
    [ObservableProperty]
    private ObservableCollection<RecentActivity> recentActivities;

    [ObservableProperty]
    private string userName = "Dr. Jane Doe"; // Tên người dùng hiển thị

    [ObservableProperty]
    private string userAvatar = "person_placeholder.png"; // Placeholder cho Avatar

    public DashboardViewModel()
    {
        // Khởi tạo dữ liệu giả lập (Mock Data)
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

        RecentActivities = new ObservableCollection<RecentActivity>
        {
            new RecentActivity {
                Description = "Đã lên lịch hẹn cho bệnh nhân Nguyễn Văn A",
                Time = "10 phút trước",
                DoctorName = "Dr. Smith"
            },
            new RecentActivity {
                Description = "Cập nhật hồ sơ bệnh án cho bệnh nhân Lê Thị B",
                Time = "30 phút trước",
                DoctorName = "Dr. Johnson"
            },
            new RecentActivity {
                Description = "Đã xác nhận nhập viện khẩn cấp",
                Time = "1 giờ trước",
                DoctorName = "Dr. Williams"
            }
        };
    }
}