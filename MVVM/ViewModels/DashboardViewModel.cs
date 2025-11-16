using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HosipitalManager.MVVM.Models;
using HospitalManager.MVVM.Models;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

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

    [ObservableProperty]
    private bool isAddPatientPopupVisible = false;

    [ObservableProperty]
    private string newPatientFullName;

    [ObservableProperty]
    private DateTime newPatientDateOfBirth = DateTime.Today;

    [ObservableProperty]
    private string newPatientGender;

    [ObservableProperty]
    private string newPatientPhoneNumber;

    [ObservableProperty]
    private string newPatientAddress;

    [ObservableProperty]
    private string newPatientStatus = "Đang điều trị";

    // Danh sách đơn thuốc để binding với CollectionView
    [ObservableProperty] // (Dùng [ObservableProperty] nếu bạn có MVVM Toolkit)
    private ObservableCollection<Prescription> prescriptions;

    // Thuộc tính kiểm soát việc hiển thị popup "Thêm Đơn Thuốc"
    [ObservableProperty]
    private bool isAddPrescriptionPopupVisible;

    // (Thêm các thuộc tính cho form thêm đơn thuốc)
    [ObservableProperty]
    private string newPrescriptionPatientName;
    [ObservableProperty]
    private string newPrescriptionDoctorName;

    public List<string> Genders { get; } = new List<string> { "Nam", "Nữ" };
    public List<string> StatusOptions { get; } = new List<string> { "Đang điều trị", "Đã xuất viện", "Chờ khám" };


    
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


        Prescriptions = new ObservableCollection<Prescription>();
        Patients = new ObservableCollection<Patient>();
        LoadPrescriptions();
        LoadSamplePatients();
    }

    private void LoadPrescriptions()
    {
        Prescriptions.Add(new Prescription
        {
            Id = "DT001",
            PatientName = "Nguyễn Văn An",
            DoctorName = "BS. Trần Thị B",
            DatePrescribed = new DateTime(2025, 11, 15),
            Status = "Đã cấp"
        });
        Prescriptions.Add(new Prescription
        {
            Id = "DT002",
            PatientName = "Lê Thị Cẩm",
            DoctorName = "BS. Nguyễn Văn X",
            DatePrescribed = new DateTime(2025, 11, 16),
            Status = "Chưa cấp"
        });
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

    [RelayCommand]
    private void ShowAddPatientPopup()
    {
        IsAddPatientPopupVisible = true;
    }

    [RelayCommand]
    private void CloseAddPatientPopup()
    {
        IsAddPatientPopupVisible = false;
        NewPatientFullName = string.Empty;
        NewPatientDateOfBirth = DateTime.Today;
        NewPatientGender = null;
        NewPatientPhoneNumber = string.Empty;
        NewPatientAddress = string.Empty;
        NewPatientStatus = "Đang điều trị";
    }

    [RelayCommand]
    private void SavePatient()
    {
        try
        {
            // 1. Tạo đối tượng Patient mới từ các thuộc tính
            var newPatient = new Patient
            {
                Id = $"BN{new Random().Next(100, 999)}",
                FullName = this.NewPatientFullName,
                DateOfBirth = this.NewPatientDateOfBirth,
                Gender = this.NewPatientGender,
                PhoneNumber = this.NewPatientPhoneNumber,
                Address = this.NewPatientAddress,
                AdmittedDate = DateTime.Today,
                Status = this.NewPatientStatus
            };

            // 2. Thêm vào danh sách
            Patients.Add(newPatient);

            // 3. Đóng và reset pop-up
            CloseAddPatientPopup();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Lỗi khi lưu bệnh nhân mới: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowAddPrescriptionPopup()
    {
        IsAddPrescriptionPopupVisible = true;
    }

    [RelayCommand]
    private void CloseAddPrescriptionPopup()
    {
        IsAddPrescriptionPopupVisible = false;

        // SỬA LỖI: Thêm reset trường cho popup đơn thuốc
        NewPrescriptionPatientName = string.Empty;
        NewPrescriptionDoctorName = string.Empty;
    }

    [RelayCommand]
    private void SavePrescription()
    {
        var newPrescription = new Prescription
        {
            // SỬA LỖI: Dùng _random static, không hardcode "DT003"
            Id = $"DT{new Random().Next(100, 999)}",
            PatientName = NewPrescriptionPatientName,
            DoctorName = NewPrescriptionDoctorName,
            DatePrescribed = DateTime.Now,
            Status = "Chưa cấp"
        };
        Prescriptions.Add(newPrescription);

        CloseAddPrescriptionPopup();
    }

}
