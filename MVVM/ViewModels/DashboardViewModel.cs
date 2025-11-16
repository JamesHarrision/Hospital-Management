using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using HospitalManager.MVVM.Models;
using Microsoft.Maui.Graphics;
using HospitalManager.MVVM.Models; 
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;

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
    private string userName = "Dr. Khang"; // Tên người dùng hiển thị

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

    public List<string> Genders { get; } = new List<string> { "Nam", "Nữ" };
    public List<string> StatusOptions { get; } = new List<string> { "Đang điều trị", "Đã xuất viện", "Chờ khám" };

    [ObservableProperty]
    private string popupTitle = "Thêm Bệnh nhân mới"; // Tiêu đề động cho pop-up

    private bool isEditing = false; // Cờ để biết đang Thêm hay Sửa
    private Patient patientToEdit; // Lưu bệnh nhân đang được sửa

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

    [RelayCommand]
    private void ShowAddPatientPopup()
    {
        isEditing = false; // Đặt chế độ là "Thêm mới"
        PopupTitle = "Thêm Bệnh nhân mới"; // Đặt tiêu đề

        // Xóa form trước khi mở
        ClearPopupForm();
        IsAddPatientPopupVisible = true;
    }

    [RelayCommand]
    private void ShowEditPatientPopup(Patient patient)
    {
        if (patient == null) return;

        isEditing = true; // Đặt chế độ là "Sửa"
        PopupTitle = $"Sửa thông tin: {patient.FullName}"; // Đặt tiêu đề
        patientToEdit = patient; // Lưu bệnh nhân đang sửa

        // Nạp (load) dữ liệu của bệnh nhân vào form
        NewPatientFullName = patient.FullName;
        NewPatientDateOfBirth = patient.DateOfBirth;
        NewPatientGender = patient.Gender;
        NewPatientPhoneNumber = patient.PhoneNumber;
        NewPatientAddress = patient.Address;
        NewPatientStatus = patient.Status;

        IsAddPatientPopupVisible = true; // Mở pop-up
    }

    [RelayCommand]
    private void CloseAddPatientPopup()
    {
        IsAddPatientPopupVisible = false;
        ClearPopupForm();
    }

    private void ClearPopupForm()
    {
        // Xóa dữ liệu đã nhập trong form
        NewPatientFullName = string.Empty;
        NewPatientDateOfBirth = DateTime.Today;
        NewPatientGender = null;
        NewPatientPhoneNumber = string.Empty;
        NewPatientAddress = string.Empty;
        NewPatientStatus = "Đang điều trị";

        // Reset trạng thái
        isEditing = false;
        patientToEdit = null;
    }

    [RelayCommand]
    private void SavePatient()
    {
        try
        {
            if (isEditing) // Trường hợp SỬA
            {
                // Cập nhật trực tiếp properties của 'patientToEdit'
                // Vì 'Patient' là ObservableObject, UI sẽ tự động cập nhật!
                patientToEdit.FullName = NewPatientFullName;
                patientToEdit.DateOfBirth = NewPatientDateOfBirth;
                patientToEdit.Gender = NewPatientGender;
                patientToEdit.PhoneNumber = NewPatientPhoneNumber;
                patientToEdit.Address = NewPatientAddress;
                patientToEdit.Status = NewPatientStatus;

                Debug.WriteLine($"Đã cập nhật: {patientToEdit.FullName}");
            }
            else // Trường hợp THÊM MỚI (logic cũ)
            {
                var newPatient = new Patient
                {
                    Id = $"BN{new Random().Next(100, 999)}",
                    FullName = NewPatientFullName,
                    DateOfBirth = NewPatientDateOfBirth,
                    Gender = NewPatientGender,
                    PhoneNumber = NewPatientPhoneNumber,
                    Address = NewPatientAddress,
                    AdmittedDate = DateTime.Today,
                    Status = NewPatientStatus
                };
                Patients.Add(newPatient);
                Debug.WriteLine($"Đã thêm mới: {newPatient.FullName}");
            }

            // Đóng và reset pop-up
            CloseAddPatientPopup();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Lỗi khi lưu bệnh nhân mới: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeletePatient(Patient patientToDelete)
    {
        if (patientToDelete == null) return;

        // HỎI XÁC NHẬN (Quan trọng!)
        // Cần một cách để lấy trang hiện tại, chúng ta sẽ dùng Shell.Current.DisplayAlert
        bool confirmed = await Application.Current.MainPage.DisplayAlert(
            "Xác nhận xóa",
            $"Bạn có chắc chắn muốn xóa bệnh nhân '{patientToDelete.FullName}'?",
            "Xóa",
            "Hủy");

        if (confirmed)
        {
            // Xóa bệnh nhân khỏi danh sách
            // ObservableCollection sẽ tự động cập nhật UI
            Patients.Remove(patientToDelete);
        }
    }
}