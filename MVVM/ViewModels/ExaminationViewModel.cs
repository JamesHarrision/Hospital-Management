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

    // Danh sách thuốc trong kho (để hiển thị lên Picker)
    [ObservableProperty]
    private ObservableCollection<MedicineProduct> availableMedicines;

    // Thuốc bác sĩ đang chọn trong Picker
    [ObservableProperty]
    private MedicineProduct selectedMedicineProduct;

    // DANH SÁCH ĐƠN VỊ TÍNH (Cố định hoặc load từ DB)
    [ObservableProperty]
    private ObservableCollection<string> availableUnits;

    // ĐƠN VỊ ĐANG ĐƯỢC CHỌN
    [ObservableProperty]
    private string selectedUnit;

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

        LoadMedicineCatalog();
        LoadUnits();
    }

    private void LoadUnits()
    {
        AvailableUnits = new ObservableCollection<string>
        {
            "Viên", "Vỉ", "Hộp", "Chai", "Lọ", "Tuýp", "Gói", "Ống"
        };
        SelectedUnit = "Viên"; // Mặc định
    }

    partial void OnSelectedMedicineProductChanged(MedicineProduct value)
    {
        if (value != null)
        {
            // Tự động chọn đơn vị mặc định của thuốc đó
            SelectedUnit = value.Unit;

            // reset số lượng về 1
            NewQuantity = 1;
        }
    }

    private void LoadMedicineCatalog()
    {
        AvailableMedicines = new ObservableCollection<MedicineProduct>
        {
            new MedicineProduct { Name = "Paracetamol 500mg", Unit = "Viên", UnitPrice = 1000 },
            new MedicineProduct { Name = "Panadol Extra", Unit = "Viên", UnitPrice = 1500 },
            new MedicineProduct { Name = "Vitamin C", Unit = "Vỉ", UnitPrice = 15000 },
            new MedicineProduct { Name = "Kháng sinh Augmentin", Unit = "Viên", UnitPrice = 25000 },
            new MedicineProduct { Name = "Thuốc ho Prospan", Unit = "Chai", UnitPrice = 85000 },
        };
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
        if (SelectedMedicineProduct == null)
            return; // Validation cơ bản

        // Tính toán thành tiền
        // Giá = Giá niêm yết * Số lượng
        decimal totalItemPrice = SelectedMedicineProduct.UnitPrice * NewQuantity;

        Medications.Add(new MedicationItem
        {
            MedicationName = SelectedMedicineProduct.Name,
            Dosage = NewDosage,
            Quantity = NewQuantity,
            Instructions = NewInstructions,
            Unit = SelectedUnit,
            Price = totalItemPrice,
        });

        // Xóa input fields sau khi thêm
        SelectedMedicineProduct = null;
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