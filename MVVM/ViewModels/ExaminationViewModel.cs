using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HosipitalManager.MVVM.Models;
using HosipitalManager.MVVM.Services;
using HospitalManager.MVVM.Models;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace HospitalManager.MVVM.ViewModels;

public partial class ExaminationViewModel : ObservableObject
{
    // Bệnh nhân đang được khám
    [ObservableProperty]
    private Patient patient;

    // Các trường nhập liệu của Bác sĩ
    [ObservableProperty]
    private string diagnosis; // Chẩn đoán

    [ObservableProperty]
    private string doctorNotes; // Ghi chú / Lời dặn

    public ExaminationViewModel(Patient patientData)
    {
        // Gán dữ liệu bắt buộc ngay lập tức
        Patient = patientData;
        // Có thể gán giá trị mặc định cho Diagnosis/Notes nếu cần
    }

    [ObservableProperty]
    private ObservableCollection<MedicationItem> medications = new ObservableCollection<MedicationItem>();

    // Input fields for adding a new medication
    [ObservableProperty]
    private string newMedicationName;

    [ObservableProperty]
    private string newDosage;

    [ObservableProperty]
    private int newQuantity;

    [ObservableProperty]
    private string newInstructions; // Instructions (Hướng dẫn sử dụng)

    private readonly PrescriptionService _prescriptionService;

    // CONSTRUCTOR MỚI: Nhận thêm PrescriptionService
    public ExaminationViewModel(Patient patientData, PrescriptionService service)
    {
        Patient = patientData;
        _prescriptionService = service;

        // Khởi tạo danh sách thuốc rỗng
        Medications = new ObservableCollection<MedicationItem>();
    }

    [RelayCommand]
    private async Task FinishExamination()
    {
        if (Patient != null)
        {
            // 1. Cập nhật trạng thái bệnh nhân
            Patient.Status = "Hoàn thành điều trị";

            // 2. GỌI SERVICE ĐỂ LƯU ĐƠN THUỐC
            _prescriptionService.CreateAndSavePrescription(
                Patient,
                Diagnosis,
                DoctorNotes,
                Medications
            );
        }

        await Application.Current.MainPage.DisplayAlert("Hoàn tất", "Đã lưu hồ sơ bệnh án thành công.", "OK");
        await Shell.Current.Navigation.PopModalAsync();
    }

    [RelayCommand]
    private async Task Cancel()
    {
        // Dùng PopModalAsync để quay lại Dashboard
        await Shell.Current.Navigation.PopModalAsync();
    }

    [RelayCommand]
    private void AddMedication()
    {
        if (string.IsNullOrWhiteSpace(NewMedicationName))
            return; // Validation cơ bản

        Medications.Add(new MedicationItem
        {
            MedicationName = NewMedicationName,
            Dosage = NewDosage,
            Quantity = NewQuantity,
            Instructions = NewInstructions
        });

        // Xóa input fields sau khi thêm
        NewMedicationName = string.Empty;
        NewDosage = string.Empty;
        NewQuantity = 0;
        NewInstructions = string.Empty;
    }

    [RelayCommand]
    private void RemoveMedication(MedicationItem itemToRemove)
    {
        if (itemToRemove != null)
        {
            Medications.Remove(itemToRemove);
        }
    }
}