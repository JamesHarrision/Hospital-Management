using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalManager.MVVM.Models;
using System.Collections.ObjectModel;

namespace HospitalManager.MVVM.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    // Database chính thức
    public ObservableCollection<Patient> Patients { get; set; } = new();


    public ObservableCollection<Patient> FilteredPatients { get; set; } = new();

    [ObservableProperty]
    private string searchText;

    // Hàm này tự động chạy khi SearchText thay đổi (tính năng của MVVM Toolkit)
    partial void OnSearchTextChanged(string value)
    {
        SearchPatient();
    }
    // Hàm nạp dữ liệu mẫu (nếu cần)
    private void LoadSamplePatients()
    {
        // Tạo danh sách dữ liệu mẫu
    var samples = new List<Patient>
    {
        new Patient 
        { 
            Id = "BN001", 
            FullName = "Nguyễn Văn An", 
            DateOfBirth = new DateTime(1990, 5, 15), 
            Gender = "Nam", 
            PhoneNumber = "0901234567", 
            Address = "123 Đường ABC, Q.1, TP.HCM", 
            AdmittedDate = DateTime.Today.AddDays(-5), 
            Status = "Đang điều trị", 
            Severity = "normal" 
        },
        new Patient 
        { 
            Id = "BN002", 
            FullName = "Trần Thị Bích", 
            DateOfBirth = new DateTime(1995, 8, 20), 
            Gender = "Nữ", 
            PhoneNumber = "0909876543", 
            Address = "456 Đường Lê Lợi, Q.1, TP.HCM", 
            AdmittedDate = DateTime.Today.AddDays(-2), 
            Status = "Đang điều trị", 
            Severity = "critical" 
        },
        new Patient 
        { 
            Id = "BN003", 
            FullName = "Lê Văn Cường", 
            DateOfBirth = new DateTime(1985, 12, 10), 
            Gender = "Nam", 
            PhoneNumber = "0912345678", 
            Address = "789 Đường Nguyễn Trãi, Q.5, TP.HCM", 
            AdmittedDate = DateTime.Today.AddDays(-10), 
            Status = "Đang điều trị", 
            Severity = "medium" 
        },
        new Patient 
        { 
            Id = "BN004", 
            FullName = "Phạm Minh Duy", 
            DateOfBirth = new DateTime(2001, 3, 5), 
            Gender = "Nam", 
            PhoneNumber = "0987654321", 
            Address = "321 Đường Trần Hưng Đạo, Q.1, TP.HCM", 
            AdmittedDate = DateTime.Today.AddDays(-1), 
            Status = "Chờ khám", 
            Severity = "normal" 
        },
        new Patient 
        { 
            Id = "BN005", 
            FullName = "Hoàng Thị Em", 
            DateOfBirth = new DateTime(1978, 7, 25), 
            Gender = "Nữ", 
            PhoneNumber = "0933445566", 
            Address = "654 Đường 3/2, Q.10, TP.HCM", 
            AdmittedDate = DateTime.Today.AddDays(-15), 
            Status = "Đã xuất viện", 
            Severity = "normal" 
        },
        new Patient 
        { 
            Id = "BN006", 
            FullName = "Ngô Văn Fương", 
            DateOfBirth = new DateTime(1999, 11, 11), 
            Gender = "Nam", 
            PhoneNumber = "0977889900", 
            Address = "12 Đường Phan Đăng Lưu, Q.Phú Nhuận", 
            AdmittedDate = DateTime.Today.AddDays(-3), 
            Status = "Đang điều trị", 
            Severity = "critical" 
        },
        new Patient 
        { 
            Id = "BN007", 
            FullName = "Vũ Thị Giang", 
            DateOfBirth = new DateTime(1982, 9, 9), 
            Gender = "Nữ", 
            PhoneNumber = "0966554433", 
            Address = "99 Đường Võ Văn Ngân, TP.Thủ Đức", 
            AdmittedDate = DateTime.Today.AddDays(-7), 
            Status = "Đang điều trị", 
            Severity = "medium" 
        },
        new Patient 
        { 
            Id = "BN008", 
            FullName = "Đặng Văn Hùng", 
            DateOfBirth = new DateTime(1993, 4, 30), 
            Gender = "Nam", 
            PhoneNumber = "0944332211", 
            Address = "55 Đường Phạm Văn Đồng, Q.Gò Vấp", 
            AdmittedDate = DateTime.Today.AddDays(-4), 
            Status = "Chờ phẫu thuật", 
            Severity = "critical" 
        },
        new Patient 
        { 
            Id = "BN009", 
            FullName = "Bùi Thị Yến", 
            DateOfBirth = new DateTime(1960, 1, 1), 
            Gender = "Nữ", 
            PhoneNumber = "0911223344", 
            Address = "88 Đường Hậu Giang, Q.6, TP.HCM", 
            AdmittedDate = DateTime.Today.AddDays(-20), 
            Status = "Đang điều trị", 
            Severity = "normal" 
        },
        new Patient 
        { 
            Id = "BN010", 
            FullName = "Đoàn Văn Khanh", 
            DateOfBirth = new DateTime(2005, 6, 15), 
            Gender = "Nam", 
            PhoneNumber = "0999888777", 
            Address = "22 Đường Lý Thường Kiệt, Q.Tân Bình", 
            AdmittedDate = DateTime.Today.AddDays(0), 
            Status = "Mới nhập viện", 
            Severity = "medium" 
        }
    };

    // Thêm tất cả vào danh sách chính
    foreach (var p in samples)
    {
        Patients.Add(p);
        FilteredPatients.Add(p);
    }
    }

    private void SearchPatient()
    {
        FilteredPatients.Clear(); // Xóa danh sách hiển thị cũ

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            // Nếu ô tìm kiếm rỗng, hiển thị lại tất cả từ danh sách gốc
            foreach (var patient in Patients)
            {
                FilteredPatients.Add(patient);
            }
        }
        else
        {
            // Chuyển về chữ thường để tìm không phân biệt hoa thường
            string keyword = SearchText.ToLower();

            foreach (var patient in Patients)
            {
                // Tìm theo Tên HOẶC theo Mã (Id)
                if (patient.FullName.ToLower().Contains(keyword) ||
                    patient.Id.ToLower().Contains(keyword))
                {
                    FilteredPatients.Add(patient);
                }
            }
        }
    }

    [RelayCommand]
    private void ShowEditPatientPopup(Patient patient)
    {
        if (patient == null) return;

        isEditing = true;
        PopupTitle = $"Sửa hồ sơ: {patient.FullName}";
        patientToEdit = patient;

        // Load dữ liệu lên form
        NewPatientFullName = patient.FullName;
        NewPatientDateOfBirth = patient.DateOfBirth;
        NewPatientGender = patient.Gender;
        NewPatientPhoneNumber = patient.PhoneNumber;
        NewPatientAddress = patient.Address;
        NewPatientStatus = patient.Status;
        // Map severity code back to Display Name if needed
        NewPatientSeverity = "Bình thường";
        NewPatientSymptoms = patient.Symptoms;

        // MỞ KHÓA cho phép sửa trạng thái
        IsStatusEnabled = true;

        IsAddPatientPopupVisible = true;
    }

    [RelayCommand]
    private async Task DeletePatient(Patient patientToDelete)
    {
        if (patientToDelete == null) return;

        bool confirmed = await Application.Current.MainPage.DisplayAlert(
            "Xác nhận xóa",
            $"Bạn có chắc chắn muốn xóa hồ sơ '{patientToDelete.FullName}'?",
            "Xóa",
            "Hủy");

        if (confirmed)
        {
            Patients.Remove(patientToDelete);
            FilteredPatients.Remove(patientToDelete);
        }
    }
}