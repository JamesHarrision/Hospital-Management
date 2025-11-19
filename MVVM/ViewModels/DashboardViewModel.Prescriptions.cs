using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HosipitalManager.MVVM.Models;
using System.Collections.ObjectModel;

namespace HospitalManager.MVVM.ViewModels;

public partial class DashboardViewModel
{
    // Danh sách đơn thuốc (Chính chủ)
    [ObservableProperty]
    private ObservableCollection<Prescription> prescriptions;

    // Đơn thuốc đang chọn xem chi tiết
    [ObservableProperty]
    private Prescription selectedPrescription;

    // Biến bật/tắt popup xem chi tiết
    [ObservableProperty]
    private bool isPrescriptionDetailVisible;

    // Biến bật/tắt popup thêm thủ công (giữ lại nếu cần)
    [ObservableProperty]
    private bool isAddPrescriptionPopupVisible;
    [ObservableProperty]
    private string newPrescriptionPatientName;
    [ObservableProperty]
    private string newPrescriptionDoctorName;

    // --- CÁC HÀM LOAD DỮ LIỆU ---
    private void LoadPrescriptions()
    {
        // Dữ liệu mẫu ban đầu
        Prescriptions.Add(new Prescription
        {
            Id = "DT001",
            PatientName = "Nguyễn Văn An",
            DoctorName = "BS. Trần Thị B",
            DatePrescribed = new DateTime(2025, 11, 15),
            Status = "Đã cấp"
        });
    }

    // --- CÁC COMMAND XỬ LÝ ---

    // 1. Xem chi tiết đơn thuốc
    [RelayCommand]
    private void ShowPrescriptionDetail(Prescription prescription)
    {
        if (prescription == null) return;
        SelectedPrescription = prescription;
        IsPrescriptionDetailVisible = true;
    }

    [RelayCommand]
    private void ClosePrescriptionDetail()
    {
        IsPrescriptionDetailVisible = false;
        SelectedPrescription = null;
    }

    // 2. Thêm đơn thuốc thủ công (Popup cũ)
    [RelayCommand]
    private void ShowAddPrescriptionPopup()
    {
        IsAddPrescriptionPopupVisible = true;
    }

    [RelayCommand]
    private void CloseAddPrescriptionPopup()
    {
        IsAddPrescriptionPopupVisible = false;
        NewPrescriptionPatientName = string.Empty;
        NewPrescriptionDoctorName = string.Empty;
    }

    [RelayCommand]
    private void SavePrescription()
    {
        var newPrescription = new Prescription
        {
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