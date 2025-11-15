using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using HospitalManager.MVVM.Models;
using Microsoft.Maui.Graphics;

namespace HospitalManager.MVVM.ViewModels; 

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

    public ObservableCollection<Patient> Patients { get; set; }

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

        Patients = new ObservableCollection<Patient>();
        LoadSamplePatients();
    }

    private void LoadSamplePatients()
    {
        Patients.Add(new Patient
        {
            Id = "BN001",
            FullName = "Nguyễn Văn An",
            DateOfBirth = new DateTime(1990, 5, 15),
            Gender = "Nam",
            PhoneNumber = "0901234567",
            Address = "123 Đường ABC, Quận 1, TP.HCM",
            AdmittedDate = DateTime.Today.AddDays(-5),
            Status = "Đang điều trị"
        });
        Patients.Add(new Patient
        {
            Id = "BN002",
            FullName = "Trần Thị Bình",
            DateOfBirth = new DateTime(1985, 10, 2),
            Gender = "Nữ",
            PhoneNumber = "0987654321",
            Address = "456 Đường XYZ, Quận 3, TP.HCM",
            AdmittedDate = DateTime.Today.AddDays(-2),
            Status = "Đang điều trị"
        });
        Patients.Add(new Patient
        {
            Id = "BN003",
            FullName = "Lê Văn Cường",
            DateOfBirth = new DateTime(2001, 1, 30),
            Gender = "Nam",
            PhoneNumber = "0123456789",
            Address = "789 Đường DEF, Quận 10, TP.HCM",
            AdmittedDate = DateTime.Today.AddDays(-10),
            Status = "Đã xuất viện"
        });
    }
}