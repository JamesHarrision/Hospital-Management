using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HosipitalManager.Helpers;
using HosipitalManager.MVVM.Models;
using HosipitalManager.MVVM.Services;
using HosipitalManager.MVVM.Views;
using HospitalManager.MVVM.Models;
using HospitalManager.MVVM.Views;
using Microsoft.Maui.ApplicationModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Maui.Media;

namespace HospitalManager.MVVM.ViewModels;

public partial class DashboardViewModel
{
    // Lưu ý: _databaseService được khai báo ở phần Partial khác của DashboardViewModel
    // Nếu chưa có, hãy đảm bảo class chính có field: private readonly LocalDatabaseService _databaseService;

    #region 1. Properties: Queue & UI State
    [ObservableProperty]
    private ObservableCollection<Patient> _waitingQueue;

    [ObservableProperty]
    private bool _isAddPatientPopupVisible = false;

    [ObservableProperty]
    private string _popupTitle = "Tiếp nhận bệnh nhân mới";

    [ObservableProperty]
    private bool _isStatusEnabled; // Khóa/Mở khóa ô trạng thái (khi Edit)
    #endregion

    #region 2. Properties: New Patient Form (Input Fields)
    [ObservableProperty] private string _newPatientFullName;
    [ObservableProperty] private DateTime _newPatientDateOfBirth = DateTime.Today;
    [ObservableProperty] private string _newPatientGender;
    [ObservableProperty] private string _newPatientPhoneNumber;
    [ObservableProperty] private string _newPatientAddress;
    [ObservableProperty] private string _newPatientStatus = "Chờ khám";
    [ObservableProperty] private string _newPatientSeverity = "Bình thường";
    [ObservableProperty] private string _newPatientSymptoms;

    // Doctor Selection
    [ObservableProperty] private Doctor _selectedDoctor;
    [ObservableProperty] private ObservableCollection<Doctor> _availableDoctors;

    // Editing State
    private bool _isEditing = false;
    private Patient _patientToEdit;
    #endregion

    #region 3. Collections for Picker (Read-only)
    public List<string> Genders { get; } = new List<string> { "Nam", "Nữ", "Khác" };
    public List<string> StatusOptions { get; } = new List<string> { "Chờ khám", "Đang điều trị", "Hoàn thành điều trị" };
    public List<string> SeverityOptions { get; } = new List<string> { "Bình thường", "Gấp", "Khẩn cấp", "Cấp cứu" };
    #endregion

    #region 4. Logic: Priority & Sorting
    private string GetSeverityCode(string displayName)
    {
        return displayName switch
        {
            "Cấp cứu" => "critical",
            "Khẩn cấp" => "emergency",
            "Gấp" => "urgent",
            "Trung bình" => "medium",
            _ => "normal"
        };
    }

    private double CalculatePriority(Patient patient)
    {
        if (patient == null) return 0;
        double score = 10;

        // Điểm theo mức độ nghiêm trọng
        string severity = patient.Severity?.ToLower() ?? "";
        if (severity == "critical" || severity == "cấp cứu") score += 1000;
        else if (severity == "emergency" || severity == "khẩn cấp") score += 500;
        else if (severity == "urgent" || severity == "gấp") score += 200;
        else if (severity == "medium") score += 50;

        // Điểm ưu tiên theo tuổi (Trẻ em < 12 hoặc Người già > 65)
        if (patient.Age < 12) score += 20;
        if (patient.Age > 65) score += 20;

        // Trừ điểm theo thứ tự hàng đợi (đến trước được ưu tiên hơn nếu cùng mức độ)
        score -= patient.QueueOrder * 0.1;
        return score;
    }

    public void SortPatientQueue()
    {
        if (WaitingQueue == null || WaitingQueue.Count == 0) return;

        // Sắp xếp lại dựa trên PriorityScore
        var sortedList = WaitingQueue
            .Select(p =>
            {
                p.PriorityScore = CalculatePriority(p);
                return p;
            })
            .OrderByDescending(p => p.PriorityScore)
            .ToList();

        WaitingQueue.Clear();
        foreach (var p in sortedList) WaitingQueue.Add(p);
    }
    #endregion

    #region 5. Commands: Popup Management
    [RelayCommand]
    private void ShowAddPatientPopup()
    {
        _isEditing = false;
        _patientToEdit = null;
        PopupTitle = "Tiếp nhận bệnh nhân mới";

        ClearPopupForm();
        LoadDoctorsList();

        // Khóa Status khi tạo mới (mặc định là Chờ khám)
        NewPatientStatus = "Chờ khám";
        IsStatusEnabled = false;

        IsAddPatientPopupVisible = true;
    }

    [RelayCommand]
    private void CloseAddPatientPopup()
    {
        IsAddPatientPopupVisible = false;
        ClearPopupForm();
    }
    #endregion

    #region 6. Commands: Save & Update Logic
    [RelayCommand]
    private async Task ConfirmAddPatient()
    {
        // 1. Validation
        if (!ValidateForm()) return;

        // 2. Xử lý Save
        if (_isEditing && _patientToEdit != null)
        {
            // Logic Update (Nếu cần thiết sau này)
            UpdateExistingPatientObject(_patientToEdit);
            await _databaseService.SavePatientAsync(_patientToEdit);
        }
        else
        {
            // Logic Create New
            var newPatient = CreatePatientFromForm();
            await _databaseService.SavePatientAsync(newPatient);

            // Thêm vào UI Queue
            if (WaitingQueue == null) WaitingQueue = new ObservableCollection<Patient>();
            WaitingQueue.Add(newPatient);
        }

        // 3. Sắp xếp & Đóng Popup
        SortPatientQueue();
        CloseAddPatientPopup();

        await Application.Current.MainPage.DisplayAlert("Thành công", "Đã lưu hồ sơ bệnh nhân.", "OK");
    }

    /// <summary>
    /// Tạo object Patient từ dữ liệu trên Form
    /// </summary>
    private Patient CreatePatientFromForm()
    {
        return new Patient
        {
            // Tạo ID ngẫu nhiên hoặc dùng Guid
            Id = "P" + DateTime.Now.Ticks.ToString().Substring(12),
            FullName = NewPatientFullName,
            DateOfBirth = NewPatientDateOfBirth,
            Gender = NewPatientGender,
            PhoneNumber = NewPatientPhoneNumber,
            Address = NewPatientAddress,
            Status = NewPatientStatus,
            Symptoms = NewPatientSymptoms,

            // Map Severity Display -> Code
            Severity = GetSeverityCode(NewPatientSeverity),

            // Map Doctor
            Doctorname = SelectedDoctor?.Name ?? "Chưa chỉ định",

            QueueOrder = WaitingQueue?.Count ?? 0 // Gán thứ tự tạm thời
        };
    }

    private void UpdateExistingPatientObject(Patient p)
    {
        p.FullName = NewPatientFullName;
        p.DateOfBirth = NewPatientDateOfBirth;
        p.Gender = NewPatientGender;
        p.PhoneNumber = NewPatientPhoneNumber;
        p.Address = NewPatientAddress;
        p.Symptoms = NewPatientSymptoms;
        p.Severity = GetSeverityCode(NewPatientSeverity);
        p.Doctorname = SelectedDoctor?.Name ?? p.Doctorname;
    }
    #endregion

    #region 7. Commands: Triage Actions (Call & Check-in)

    [RelayCommand]
    private async Task CallPatient(Patient patient)
    {
        if (patient == null) return;

        bool isConfirmed = await Application.Current.MainPage.DisplayAlert(
            "Xác nhận khám",
            $"Mời bệnh nhân {patient.FullName} vào khám ngay?",
            "Gọi ngay", "Hủy");

        if (isConfirmed)
        {
            // Update Status -> DB
            patient.Status = "Đang điều trị";
            await _databaseService.SavePatientAsync(patient);

            // Remove from Queue UI
            WaitingQueue.Remove(patient);

            // Navigate to Exam Page
            var examVM = new ExaminationViewModel(patient, _databaseService);
            var examPage = new ExaminationPageView(examVM);

            await Shell.Current.Navigation.PushModalAsync(examPage);
        }
    }

    /// <summary>
    /// XỬ LÝ TIẾP NHẬN TỪ LỊCH HẸN (Được gọi từ Tab Lịch hẹn)
    /// </summary>
    public async void HandleCheckInFromAppointment(Appointment appt)
    {
        if (appt == null) return;

        var newPatient = new Patient
        {
            Id = "P" + DateTime.Now.Ticks.ToString().Substring(12),
            FullName = appt.PatientName,
            Gender = "Khác", // Default vì Appointment chưa chắc có Gender
            PhoneNumber = appt.PhoneNumber,
            Address = "Chưa cập nhật",
            Symptoms = appt.Note ?? "Đặt lịch hẹn trước",
            Status = "Chờ khám",
            Severity = "normal",
            Doctorname = appt.DoctorObject?.Name ?? appt.DoctorName ?? "Chưa chỉ định",
            PriorityScore = 10
        };

        // Lưu DB
        if (_databaseService != null)
        {
            await _databaseService.SavePatientAsync(newPatient);
        }

        // Cập nhật UI Queue
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (WaitingQueue == null) WaitingQueue = new ObservableCollection<Patient>();
            WaitingQueue.Add(newPatient);
            SortPatientQueue();
        });
    }
    #endregion

    #region 8. Helper Methods
    private void LoadDoctorsList()
    {
        // Lấy từ Singleton System
        AvailableDoctors = HospitalSystem.Instance.Doctors;
        SelectedDoctor = null;
    }

    private void ClearPopupForm()
    {
        NewPatientFullName = string.Empty;
        NewPatientDateOfBirth = DateTime.Today;
        NewPatientGender = null;
        NewPatientPhoneNumber = string.Empty;
        NewPatientAddress = string.Empty;
        NewPatientStatus = "Chờ khám";
        NewPatientSeverity = "Bình thường";
        NewPatientSymptoms = string.Empty;
        SelectedDoctor = null;

        _isEditing = false;
        _patientToEdit = null;
    }

    private bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(NewPatientFullName))
        {
            Shell.Current.DisplayAlert("Lỗi", "Vui lòng nhập tên bệnh nhân", "OK");
            return false;
        }
        if (string.IsNullOrWhiteSpace(NewPatientPhoneNumber))
        {
            Shell.Current.DisplayAlert("Lỗi", "Vui lòng nhập số điện thoại", "OK");
            return false;
        }
        return true;
    }
    #endregion
}