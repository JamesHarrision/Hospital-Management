                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HosipitalManager.MVVM.Models;
using HosipitalManager.MVVM.Services;
using HospitalManager.MVVM.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace HospitalManager.MVVM.ViewModels;

public partial class ExaminationViewModel : ObservableObject
{
    // --- 1. SERVICE DATABASE (THAY THẾ SERVICE CŨ) ---
    private readonly LocalDatabaseService _databaseService;

    // --- CÁC PROPERTY CƠ BẢN ---
    [ObservableProperty]
    private Patient patient;

    [ObservableProperty]
    private string diagnosis;

    [ObservableProperty]
    private string doctorNotes;

    // Danh sách thuốc đã kê trong đơn này
    [ObservableProperty]
    private ObservableCollection<MedicationItem> medications;

    // --- KHU VỰC AUTO-SUGGEST (GỢI Ý THUỐC) ---

    // Danh sách gốc (Private, load từ MedicineService)
    private List<MedicineProduct> _allMedicines;

    // Danh sách gợi ý (Hiện lên khi gõ)
    [ObservableProperty]
    private ObservableCollection<MedicineProduct> filteredMedicines;

    // Biến ẩn/hiện Popup gợi ý
    [ObservableProperty]
    private bool isSuggestionVisible;

    // Ô Nhập tên thuốc (Binding vào Entry trong XAML)
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

    // --- CONSTRUCTOR MỚI ---
    // Chỉ nhận Patient và LocalDatabaseService
    public ExaminationViewModel(Patient patientData, LocalDatabaseService dbService)
    {
        Patient = patientData;
        _databaseService = dbService;

        // Khởi tạo các danh sách
        Medications = new ObservableCollection<MedicationItem>();
        FilteredMedicines = new ObservableCollection<MedicineProduct>();

        // 1. Tự load danh mục thuốc (Không cần truyền từ ngoài vào)
        var medService = new MedicineService();
        var listThuoc = medService.GetMedicineCatalog();
        
        // Lưu vào biến gốc để dùng cho tìm kiếm
        _allMedicines = listThuoc.ToList();

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
        // Lọc trong danh sách _allMedicines đã load
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

        // 3. Xử lý đơn vị
        if (!AvailableUnits.Contains(selectedMed.Unit))
        {
            AvailableUnits.Add(selectedMed.Unit);
        }
        SelectedUnit = selectedMed.Unit;

        // Reset số lượng mặc định
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
            var match = _allMedicines.FirstOrDefault(m => m.Name.Equals(SearchQuery, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                _selectedMedicineProduct = match;
            }
            else
            {
                // Nếu thuốc lạ chưa có trong kho -> Có thể return hoặc thông báo
                // Ở đây ta tạm return
                return;
            }
        }

        // Tính thành tiền (Nếu Model MedicineProduct có UnitPrice)
        decimal price = _selectedMedicineProduct.UnitPrice; // Giả sử có trường này
        decimal totalItemPrice = price * NewQuantity;

        Medications.Add(new MedicationItem
        {
            MedicationName = _selectedMedicineProduct.Name,
            Dosage = NewDosage,
            Quantity = NewQuantity,
            Instructions = NewInstructions,
            Unit = SelectedUnit,
            Price = totalItemPrice
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

    // --- LOGIC HOÀN TẤT KHÁM (LƯU DATABASE) ---
    [RelayCommand]
    private async Task FinishExamination()
    {
        if (Patient == null) return;

        // 1. Cập nhật trạng thái Bệnh nhân -> Lưu DB
        Patient.Status = "Hoàn thành điều trị";
        await _databaseService.SavePatientAsync(Patient);

        // 2. Tạo Đơn thuốc Mới
        var newPrescription = new Prescription
        {
            // ID sẽ được tự động tạo bên Service (DT1000...)
            PatientId = Patient.Id,
            PatientName = Patient.FullName,
            DoctorName = Patient.Doctorname,
            DatePrescribed = DateTime.Now,
            Status = "Đã cấp",
            
            // Thông tin khám
            Diagnosis = Diagnosis,
            DoctorNotes = DoctorNotes,
            
            // Danh sách thuốc (Copy sang)
            Medications = new ObservableCollection<MedicationItem>(Medications)
        };

        // 3. Lưu Đơn thuốc vào DB
        await _databaseService.SavePrescriptionAsync(newPrescription);

        await Application.Current.MainPage.DisplayAlert("Hoàn tất", "Đã lưu hồ sơ và đơn thuốc thành công!", "OK");

        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Send("ReloadPrescriptions");
        // Đóng trang
        await Shell.Current.Navigation.PopModalAsync();
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await Shell.Current.Navigation.PopModalAsync();
    }
}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           