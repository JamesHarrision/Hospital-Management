using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HosipitalManager.Helpers;
using HosipitalManager.MVVM.Enums; // Đảm bảo có namespace này cho AppointmentStatus
using HosipitalManager.MVVM.Models;
using HosipitalManager.MVVM.Services;
using HospitalManager.MVVM.Messages;
using HospitalManager.MVVM.Models;
using System.Collections.ObjectModel;

namespace HospitalManager.MVVM.ViewModels;

public partial class DashboardViewModel
{
    // Biến tạm để xử lý Check-in từ Lịch hẹn
    private Appointment _pendingCheckInAppointment;

    #region 1. Properties: Pagination & List Data
    private const int PageSize = 10;

    [ObservableProperty] private int _patientCurrentPage = 1;
    [ObservableProperty] private int _patientTotalPages = 1;
    [ObservableProperty] private string _patientPageInfo; // Hiển thị "Trang 1 / 5"
    [ObservableProperty] private bool _canPatientGoBack;
    [ObservableProperty] private bool _canPatientGoNext;

    public ObservableCollection<Patient> Patients { get; set; } = new();
    public ObservableCollection<Patient> FilteredPatients { get; set; } = new();
    #endregion

    #region 2. Properties: Search
    [ObservableProperty] private string _searchText;

    // Hàm này tự động chạy khi SearchText thay đổi (MVVM Toolkit Hook)
    partial void OnSearchTextChanged(string value)
    {
        // Debounce nhẹ hoặc chạy async để tìm kiếm
        Task.Run(async () => await SearchPatient());
    }
    #endregion

    #region 3. Methods: Load & Search Logic
    public async Task LoadPatients()
    {
        if (_databaseService == null) return;

        // Tính toán phân trang
        int totalCount = await _databaseService.GetPatientCountAsync();
        PatientTotalPages = (int)Math.Ceiling((double)totalCount / PageSize);

        if (PatientCurrentPage < 1) PatientCurrentPage = 1;
        if (PatientCurrentPage > PatientTotalPages && PatientTotalPages > 0) PatientCurrentPage = PatientTotalPages;

        var patientList = await _databaseService.GetPatientsPagedAsync(PatientCurrentPage, PageSize);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Patients.Clear();
            FilteredPatients.Clear();

            foreach (var patient in patientList)
            {
                Patients.Add(patient);
                FilteredPatients.Add(patient);
            }

            UpdatePatientPaginationUI();
        });
    }

    private void UpdatePatientPaginationUI()
    {
        PatientPageInfo = $"Trang {PatientCurrentPage} / {PatientTotalPages}";
        CanPatientGoBack = PatientCurrentPage > 1;
        CanPatientGoNext = PatientCurrentPage < PatientTotalPages;
    }

    private async Task SearchPatient()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadPatients();
            return;
        }

        var searchResults = await _databaseService.SearchPatientAsync(SearchText);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Patients.Clear();
            FilteredPatients.Clear();

            foreach (var p in searchResults)
            {
                Patients.Add(p);
                FilteredPatients.Add(p);
            }

            PatientPageInfo = $"Tìm thấy: {searchResults.Count} kết quả";
            CanPatientGoBack = false;
            CanPatientGoNext = false;
        });
    }
    #endregion

    #region 4. Commands: Pagination
    [RelayCommand]
    private async Task NextPatientPage()
    {
        if (PatientCurrentPage < PatientTotalPages)
        {
            PatientCurrentPage++;
            await LoadPatients();
        }
    }

    [RelayCommand]
    private async Task PreviousPatientPage()
    {
        if (PatientCurrentPage > 1)
        {
            PatientCurrentPage--;
            await LoadPatients();
        }
    }
    #endregion

    #region 5. Commands: CRUD (Add/Edit/Delete/Save)

    // Nút mở Popup Edit (Load dữ liệu cũ lên Form)
    [RelayCommand]
    private void ShowEditPatientPopup(Patient patient)
    {
        if (patient == null) return;

        // Sử dụng biến _isEditing và _patientToEdit từ file DashboardViewModel.cs (Partial)
        _isEditing = true;
        _patientToEdit = patient;

        PopupTitle = $"Sửa hồ sơ: {patient.FullName}";

        // Map dữ liệu vào các Property (Viết Hoa)
        NewPatientFullName = patient.FullName;
        NewPatientDateOfBirth = patient.DateOfBirth;
        NewPatientGender = patient.Gender;
        NewPatientPhoneNumber = patient.PhoneNumber;
        NewPatientAddress = patient.Address;
        NewPatientStatus = patient.Status;
        NewPatientSeverity = GetSeverityCode(patient.Severity); // Hàm này ở file kia
        NewPatientSymptoms = patient.Symptoms;

        // MỞ KHÓA cho phép sửa trạng thái khi Edit
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
            await _databaseService.DeletePatientAsync(patientToDelete);

            // Xóa trực tiếp trên UI để khỏi load lại DB
            Patients.Remove(patientToDelete);
            FilteredPatients.Remove(patientToDelete);
        }
    }

    [RelayCommand]
    public async Task SavePatient()
    {
        // --- 1. VALIDATION ---
        // Sửa lỗi: Dùng Property viết Hoa (NewPatient...) thay vì biến local
        if (!ValidationHelper.IsValidName(NewPatientFullName))
        {
            await ShowError("Họ tên không hợp lệ. Vui lòng không nhập số hoặc ký tự đặc biệt.");
            return;
        }
        if (!ValidationHelper.IsValidPhoneNumber(NewPatientPhoneNumber))
        {
            await ShowError("Số điện thoại phải bắt đầu bằng số 0 và đủ 10 chữ số.");
            return;
        }
        var dobCheck = ValidationHelper.IsValidDateOfBirth(NewPatientDateOfBirth);
        if (!dobCheck.IsValid)
        {
            await ShowError(dobCheck.Message);
            return;
        }
        if (!ValidationHelper.IsValidLength(NewPatientAddress, 5))
        {
            await ShowError("Địa chỉ quá ngắn, vui lòng nhập cụ thể hơn.");
            return;
        }

        // --- 2. XỬ LÝ DATABASE ---
        try
        {
            Patient patientToSave;
            // GetSeverityCode nằm ở file DashboardViewModel.cs (Partial) -> Vẫn gọi được vì cùng class
            string severityCode = GetSeverityCode(NewPatientSeverity);

            // --- CASE A: EDIT MODE ---
            // Sửa lỗi: Dùng _isEditing và _patientToEdit (có dấu gạch dưới)
            if (_isEditing && _patientToEdit != null)
            {
                patientToSave = _patientToEdit;
                UpdatePatientInfoMap(patientToSave, severityCode); // Map dữ liệu mới vào object cũ

                await _databaseService.SavePatientAsync(patientToSave);
            }
            // --- CASE B: ADD NEW MODE ---
            else
            {
                // Kiểm tra trùng SĐT
                var allPatients = await _databaseService.GetPatientsAsync();
                var duplicatePhone = allPatients.FirstOrDefault(p => p.PhoneNumber == NewPatientPhoneNumber);

                if (duplicatePhone != null)
                {
                    bool continueAdd = await Application.Current.MainPage.DisplayAlert(
                        "Trùng số điện thoại",
                        $"SĐT {NewPatientPhoneNumber} đã tồn tại ({duplicatePhone.FullName}). Tạo mới?",
                        "Tạo mới", "Hủy");
                    if (!continueAdd) return;
                }

                // Tạo mới
                patientToSave = new Patient();
                UpdatePatientInfoMap(patientToSave, severityCode);

                // Sinh ID: BN1001, BN1002...
                patientToSave.Id = GenerateNewPatientId(allPatients);

                // Mặc định
                patientToSave.AdmittedDate = DateTime.Now;
                patientToSave.Status = "Chờ khám";

                // Xử lý Check-in từ Lịch hẹn (Pending Appointment)
                if (_pendingCheckInAppointment != null)
                {
                    patientToSave.Doctorname = _pendingCheckInAppointment.DoctorName;

                    // Cập nhật lịch hẹn -> Completed
                    _pendingCheckInAppointment.Status = AppointmentStatus.Completed;
                    await _databaseService.UpdateAppointmentAsync(_pendingCheckInAppointment);

                    // Refresh Dashboard Calendar
                    WeakReferenceMessenger.Default.Send(new DashboardRefreshMessage());
                    _pendingCheckInAppointment = null;
                }
                else
                {
                    patientToSave.Doctorname = SelectedDoctor?.Name ?? "Chưa chỉ định";
                }

                // Tính điểm ưu tiên (Hàm này ở file Partial kia)
                patientToSave.PriorityScore = CalculatePriority(patientToSave);

                await _databaseService.SavePatientAsync(patientToSave);
            }

            // --- 3. REFRESH UI ---
            await LoadPatients();     // Refresh List chính
            await LoadWaitingQueue(); // Refresh Queue bên Dashboard (Hàm này ở file Partial kia)

            // Close Popup & Cleanup
            IsAddPatientPopupVisible = false;
            _patientToEdit = null;
            ClearPopupForm(); // Hàm này ở file Partial kia
        }
        catch (Exception ex)
        {
            await ShowError("Chi tiết lỗi: " + ex.Message);
        }
    }
    #endregion

    #region 6. Helper Methods
    private async Task ShowError(string msg)
    {
        await Application.Current.MainPage.DisplayAlert("Lỗi nhập liệu", msg, "OK");
    }

    /// <summary>
    /// Map dữ liệu từ Form (Properties) vào Object Patient
    /// </summary>
    private void UpdatePatientInfoMap(Patient p, string severityCode)
    {
        p.FullName = NewPatientFullName;
        p.DateOfBirth = NewPatientDateOfBirth;
        p.Gender = NewPatientGender;
        p.PhoneNumber = NewPatientPhoneNumber;
        p.Address = NewPatientAddress;
        p.Symptoms = NewPatientSymptoms;
        p.Severity = severityCode;

        // Nếu đang sửa thì cho phép cập nhật Status từ Dropdown
        if (_isEditing)
        {
            p.Status = NewPatientStatus;
        }
    }

    private string GenerateNewPatientId(List<Patient> allPatients)
    {
        int nextNumber = 1000;
        if (allPatients != null && allPatients.Count > 0)
        {
            var maxId = allPatients
                .Where(p => !string.IsNullOrEmpty(p.Id) && p.Id.StartsWith("BN") && p.Id.Length > 2)
                .Select(p =>
                {
                    if (int.TryParse(p.Id.Substring(2), out int n)) return n;
                    return 0;
                })
                .DefaultIfEmpty(0)
                .Max();
            nextNumber = maxId + 1;
        }
        return $"BN{nextNumber}";
    }

    public async Task LoadWaitingQueue()
    {
        if (_databaseService == null) return;

        // Logic load hàng đợi (Dùng lại WaitingQueue ở file partial kia)
        var allPatients = await _databaseService.GetPatientsAsync();
        var waitingList = allPatients.Where(p => p.Status == "Chờ khám").ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Truy cập WaitingQueue (Property ở file Partial kia)
            if (WaitingQueue == null) WaitingQueue = new ObservableCollection<Patient>();

            WaitingQueue.Clear();
            foreach (var p in waitingList)
            {
                WaitingQueue.Add(p);
            }
            SortPatientQueue(); // Hàm ở file Partial kia
        });
    }
    #endregion
}