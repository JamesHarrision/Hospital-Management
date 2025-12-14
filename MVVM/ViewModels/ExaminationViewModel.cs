using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HosipitalManager.MVVM.Enums;
using HosipitalManager.MVVM.Messages;
using HosipitalManager.MVVM.Models;
using HosipitalManager.MVVM.Services;
using HospitalManager.MVVM.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace HospitalManager.MVVM.ViewModels
{
    public partial class ExaminationViewModel : ObservableObject
    {
        #region 1. Dependencies & Private Data
        private readonly LocalDatabaseService _databaseService;

        // Danh sách thuốc gốc (Cache để tìm kiếm)
        private List<MedicineProduct> _allMedicines;

        // Thuốc đang được chọn từ gợi ý (ẩn)
        private MedicineProduct _selectedMedicineProduct;
        #endregion

        #region 2. Properties: Main Data (Patient & Prescription Info)
        [ObservableProperty] private Patient _patient;
        [ObservableProperty] private string _diagnosis;
        [ObservableProperty] private string _doctorNotes;

        // Danh sách thuốc đã kê trong đơn này
        [ObservableProperty] private ObservableCollection<MedicationItem> _medications;
        #endregion

        #region 3. Properties: Medicine Search & Suggestion UI
        // Danh sách gợi ý (Hiện lên khi gõ)
        [ObservableProperty] private ObservableCollection<MedicineProduct> _filteredMedicines;

        // Biến ẩn/hiện Popup gợi ý
        [ObservableProperty] private bool _isSuggestionVisible;

        // Ô Nhập tên thuốc (Binding vào Entry trong XAML)
        private string _searchQuery;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    FilterMedicines(value); // Trigger lọc khi gõ
                }
            }
        }
        #endregion

        #region 4. Properties: Medicine Input Fields
        [ObservableProperty] private string _newDosage;
        [ObservableProperty] private int _newQuantity;
        [ObservableProperty] private string _newInstructions;

        [ObservableProperty] private ObservableCollection<string> _availableUnits;
        [ObservableProperty] private string _selectedUnit;
        #endregion

        #region 5. Constructor & Initialization
        public ExaminationViewModel(Patient patientData, LocalDatabaseService dbService)
        {
            Patient = patientData;
            _databaseService = dbService;

            InitializeCollections();
            LoadMedicineCatalog();
            LoadUnits();
        }

        private void InitializeCollections()
        {
            Medications = new ObservableCollection<MedicationItem>();
            FilteredMedicines = new ObservableCollection<MedicineProduct>();
        }

        private void LoadMedicineCatalog()
        {
            // Tự load danh mục thuốc
            // NOTE: Nên Inject Service này thay vì new trực tiếp nếu có thể
            var medService = new MedicineService();
            var listThuoc = medService.GetMedicineCatalog();
            _allMedicines = listThuoc.ToList();
        }

        private void LoadUnits()
        {
            AvailableUnits = new ObservableCollection<string>
            {
                "Viên", "Vỉ", "Hộp", "Chai", "Lọ", "Tuýp", "Gói", "Ống"
            };
            SelectedUnit = "Viên";
        }
        #endregion

        #region 6. Logic: Search & Filter Medicines
        private void FilterMedicines(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                FilteredMedicines.Clear();
                IsSuggestionVisible = false;
                return;
            }

            var lowerQuery = query.ToLower();

            // Lọc tối đa 5 kết quả để UI không bị lag
            var results = _allMedicines
                .Where(m => m.Name.ToLower().Contains(lowerQuery))
                .Take(5)
                .ToList();

            FilteredMedicines = new ObservableCollection<MedicineProduct>(results);
            IsSuggestionVisible = results.Count > 0;
        }

        [RelayCommand]
        private void SelectSuggestion(MedicineProduct selectedMed)
        {
            if (selectedMed == null) return;

            // 1. Fill thông tin vào UI
            SearchQuery = selectedMed.Name;
            _selectedMedicineProduct = selectedMed;

            // 2. Cập nhật Unit nếu chưa có
            if (!AvailableUnits.Contains(selectedMed.Unit))
            {
                AvailableUnits.Add(selectedMed.Unit);
            }
            SelectedUnit = selectedMed.Unit;

            // 3. Reset số lượng & Ẩn gợi ý
            NewQuantity = 1;
            IsSuggestionVisible = false;
            FilteredMedicines.Clear();
        }
        #endregion

        #region 7. Logic: Add/Remove Medications
        [RelayCommand]
        private void AddMedication()
        {
            // Validate sơ bộ
            if (string.IsNullOrWhiteSpace(SearchQuery) || NewQuantity <= 0)
            {
                // Có thể hiển thị Toast hoặc bỏ qua
                return;
            }

            // Kiểm tra nếu chưa chọn từ list nhưng nhập đúng tên
            if (_selectedMedicineProduct == null)
            {
                var match = _allMedicines.FirstOrDefault(m => m.Name.Equals(SearchQuery, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    _selectedMedicineProduct = match;
                }
                else
                {
                    // Trường hợp thuốc mới/lạ -> Tùy policy bệnh viện, ở đây tạm thời return
                    Shell.Current.DisplayAlert("Cảnh báo", "Thuốc này không có trong danh mục hệ thống.", "OK");
                    return;
                }
            }

            // Tính thành tiền
            decimal price = _selectedMedicineProduct.UnitPrice;
            decimal totalItemPrice = price * NewQuantity;

            // Thêm vào danh sách hiển thị
            Medications.Add(new MedicationItem
            {
                MedicationName = _selectedMedicineProduct.Name,
                Dosage = NewDosage,
                Quantity = NewQuantity,
                Instructions = NewInstructions,
                Unit = SelectedUnit,
                Price = totalItemPrice
            });

            // Reset Form nhập liệu thuốc
            ResetMedicationInput();
        }

        [RelayCommand]
        private void RemoveMedication(MedicationItem itemToRemove)
        {
            if (itemToRemove != null)
            {
                Medications.Remove(itemToRemove);
            }
        }

        private void ResetMedicationInput()
        {
            SearchQuery = string.Empty;
            NewDosage = string.Empty;
            NewQuantity = 0;
            NewInstructions = string.Empty;
            _selectedMedicineProduct = null;
            IsSuggestionVisible = false;
        }
        #endregion

        #region 8. Logic: Save (Finish) & Cancel
        [RelayCommand]
        private async Task FinishExamination()
        {
            if (Patient == null) return;

            // 1. Cập nhật trạng thái Bệnh nhân
            Patient.Status = "Hoàn thành điều trị"; // Nên dùng Enum hoặc Const thay vì string cứng
            await _databaseService.SavePatientAsync(Patient);

            // 2. Tạo Đơn thuốc Mới
            var newPrescription = new Prescription
            {
                PatientId = Patient.Id,
                PatientName = Patient.FullName,
                DoctorName = Patient.Doctorname,
                DatePrescribed = DateTime.Now,
                Status = PrescriptionStatus.Pending,
                Diagnosis = Diagnosis,
                DoctorNotes = DoctorNotes,
                Medications = new ObservableCollection<MedicationItem>(Medications)
            };

            // 3. Lưu Đơn thuốc
            await _databaseService.SavePrescriptionAsync(newPrescription);

            await Application.Current.MainPage.DisplayAlert("Thành công", "Hồ sơ và đơn thuốc đã được lưu.", "OK");

            // 4. Gửi message để reload các màn hình khác
            // Gợi ý: Nên tạo Class Message riêng thay vì string
            WeakReferenceMessenger.Default.Send(new ReloadPrescriptionsMessage(true));

            // 5. Đóng trang
            await Shell.Current.Navigation.PopModalAsync();
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await Shell.Current.Navigation.PopModalAsync();
        }
        #endregion
    }
}