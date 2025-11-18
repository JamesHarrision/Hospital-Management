using CommunityToolkit.Mvvm.Input;
using HospitalManager.MVVM.Models;
using System.Collections.ObjectModel;

namespace HospitalManager.MVVM.ViewModels;

public partial class DashboardViewModel
{
    // Database chính thức
    public ObservableCollection<Patient> Patients { get; set; }

    // Hàm nạp dữ liệu mẫu (nếu cần)
    private void LoadSamplePatients()
    {
        Patients.Add(new Patient
        {
            Id = "BN001",
            FullName = "Nguyễn Văn An",
            DateOfBirth = new DateTime(1990, 5, 15),
            Gender = "Nam",
            PhoneNumber = "0901234567",
            Address = "123 Đường ABC...",
            AdmittedDate = DateTime.Today.AddDays(-5),
            Status = "Đang điều trị",
            Severity = "normal"
        });
        // ... thêm các mẫu khác nếu muốn
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
        }
    }
}