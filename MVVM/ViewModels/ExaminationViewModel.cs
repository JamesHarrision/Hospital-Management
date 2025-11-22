using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HosipitalManager.MVVM.Services; // Namespace Service
using HosipitalManager.MVVM.Models;   // Namespace Model cho MedicineProduct
using HospitalManager.MVVM.Models;    // Namespace Model cho Patient/Prescription
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;

namespace HospitalManager.MVVM.ViewModels;

public partial class ExaminationViewModel : ObservableObject
{
    // --- CÁC PROPERTY CƠ BẢN ---
    [ObservableProperty]
    private Patient patient;

    [ObservableProperty]
    private string diagnosis;

    [ObservableProperty]
    private string doctorNotes;

    [ObservableProperty]
    private ObservableCollection<MedicationItem> medications;

    // --- KHU VỰC AUTO-SUGGEST (GỢI Ý THUỐC) ---

    // 1. Danh sách gốc (Private, không cần hiện lên UI)
    private List<MedicineProduct> _allMedicines;

    // 2. Danh sách gợi ý (Hiện lên khi gõ)
    [ObservableProperty]
    private ObservableCollection<MedicineProduct> filteredMedicines;

    // 3. Biến ẩn/hiện Popup gợi ý
    [ObservableProperty]
    private bool isSuggestionVisible;

    // 4. Ô Nhập tên thuốc (Binding vào Entry trong XAML)
    private string _searchQuery;
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                FilterMedicines(value); // Tự động lọc khi gõ
            }
        }
    }

    // --- CÁC TRƯỜNG NHẬP LIỆU KHÁC ---

    [ObservableProperty]
    private string newDosage;

    [ObservableProperty]
    private int newQuantity;

    [ObservableProperty]
    private string newInstructions;

    // Đơn vị tính
    [ObservableProperty]
    private ObservableCollection<string> availableUnits;
    [ObservableProperty]
    private string selectedUnit;

    // Thuốc đang được chọn (ẩn)
    private MedicineProduct _selectedMedicineProduct;

    // Service
    private readonly PrescriptionService _prescriptionService;

    // --- CONSTRUCTOR ---
    public ExaminationViewModel(
        Patient patientData,
        PrescriptionService service,
        ObservableCollection<MedicineProduct> fullMedicineCatalog)
    {
        Patient = patientData;
        _prescriptionService = service;

        // Khởi tạo danh sách
        Medications = new ObservableCollection<MedicationItem>();
        FilteredMedicines = new ObservableCollection<MedicineProduct>();

        // Lưu danh sách gốc để lọc
        _allMedicines = fullMedicineCatalog.ToList();

        LoadUnits();
    }

    private void LoadUnits()
    {
        AvailableUnits = new ObservableCollection<string> { "Viên", "Vỉ", "Hộp", "Chai", "Lọ", "Tuýp", "Gói", "Ống" };
        SelectedUnit = "Viên";
    }

    // --- LOGIC LỌC THUỐC ---
    private void FilterMedicines(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            FilteredMedicines.Clear();
            IsSuggestionVisible = false;
            return;
        }

        var lowerQuery = query.ToLower();
        var results = _allMedicines
            .Where(m => m.Name.ToLower().Contains(lowerQuery))
            .Take(5)
            .ToList();

        FilteredMedicines = new ObservableCollection<MedicineProduct>(results);

        // Chỉ hiện Popup nếu có kết quả
        IsSuggestionVisible = results.Count > 0;
    }

    // --- LOGIC CHỌN THUỐC TỪ GỢI Ý ---
    [RelayCommand]
    private void SelectSuggestion(MedicineProduct selectedMed)
    {
        if (selectedMed == null) return;

        // 1. Điền tên vào ô nhập
        SearchQuery = selectedMed.Name;

        // 2. Lưu object thuốc để dùng cho nút Thêm
        _selectedMedicineProduct = selectedMed;

        // 3. XỬ LÝ ĐƠN VỊ (FIX LỖI KHÔNG HIỆN ĐƠN VỊ)
        // Kiểm tra xem đơn vị của thuốc có trong danh sách chưa
        if (!AvailableUnits.Contains(selectedMed.Unit))
        {
            // Nếu chưa có, thêm vào danh sách để Picker có thể hiển thị
            AvailableUnits.Add(selectedMed.Unit);
        }
        // Sau đó mới gán (Lúc này Picker sẽ nhận diện được)
        SelectedUnit = selectedMed.Unit;

        // Reset số lượng
        NewQuantity = 1;

        // 4. Ẩn danh sách gợi ý
        IsSuggestionVisible = false;
        FilteredMedicines.Clear();
    }

    // --- LOGIC THÊM THUỐC VÀO ĐƠN ---
    [RelayCommand]
    private void AddMedication()
    {
        // Validation: Phải chọn thuốc từ gợi ý hoặc nhập đúng tên
        if (_selectedMedicineProduct == null)
        {
            // Nếu người dùng nhập tay đúng tên thuốc trong kho thì vẫn chấp nhận
            var match = _allMedicines.FirstOrDefault(m => m.Name.Equals(SearchQuery, System.StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                _selectedMedicineProduct = match;
            }
            else
            {
                // Nếu không tìm thấy thuốc, có thể báo lỗi hoặc return
                return;
            }
        }

        // Tính thành tiền
        decimal totalItemPrice = _selectedMedicineProduct.UnitPrice * NewQuantity;

        Medications.Add(new MedicationItem
        {
            MedicationName = _selectedMedicineProduct.Name,
            Dosage = NewDosage,
            Quantity = NewQuantity,
            Instructions = NewInstructions,
            Unit = SelectedUnit,
            //UnitPrice = _selectedMedicineProduct.UnitPrice, // Lưu đơn giá gốc
            Price = totalItemPrice // (Nếu model MedicationItem có trường này)
        });

        // Reset Form
        SearchQuery = string.Empty;
        NewDosage = string.Empty;
        NewQuantity = 0;
        NewInstructions = string.Empty;
        _selectedMedicineProduct = null;
        IsSuggestionVisible = false;
    }

    [RelayCommand]
    private void RemoveMedication(MedicationItem itemToRemove)
    {
        if (itemToRemove != null) Medications.Remove(itemToRemove);
    }

    [RelayCommand]
    private async Task FinishExamination()
    {
        if (Patient != null)
        {
            Patient.Status = "Hoàn thành điều trị";
            _prescriptionService.CreateAndSavePrescription(Patient, Diagnosis, DoctorNotes, Medications);
        }
        await Application.Current.MainPage.DisplayAlert("Hoàn tất", "Đã lưu hồ sơ.", "OK");
        await Shell.Current.Navigation.PopModalAsync();
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await Shell.Current.Navigation.PopModalAsync();
    }
}