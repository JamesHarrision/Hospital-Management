using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalManager.MVVM.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;
using HosipitalManager.MVVM.Services; 
using HospitalManager.MVVM.Views;
using HosipitalManager.MVVM.Views;
using HosipitalManager.MVVM.Models;

namespace HospitalManager.MVVM.ViewModels;

// File này chỉ lo việc Xếp hàng và Tiếp nhận (Triage)
public partial class DashboardViewModel
{
    [ObservableProperty]
    private ObservableCollection<Patient> waitingQueue; // Hàng đợi

    [ObservableProperty]
    private bool isAddPatientPopupVisible = false;

    [ObservableProperty]
    private string popupTitle = "Tiếp nhận bệnh nhân mới";

    // Các trường nhập liệu
    [ObservableProperty]
    private string newPatientFullName;
    [ObservableProperty]
    private DateTime newPatientDateOfBirth = DateTime.Today;
    [ObservableProperty]
    private string newPatientGender;
    [ObservableProperty]
    private string newPatientPhoneNumber;
    [ObservableProperty]
    private string newPatientAddress;
    [ObservableProperty]
    private string newPatientStatus = "Chờ khám";
    [ObservableProperty]
    private string newPatientSeverity = "Bình thường";
    [ObservableProperty]
    private string newPatientSymptoms;

    // THÊM: Property cho Picker chọn bác sĩ
    [ObservableProperty]
    private Doctor selectedDoctor;

    [ObservableProperty]
    private ObservableCollection<Doctor> availableDoctors;

    // Biến kiểm soát UI
    [ObservableProperty]
    private bool isStatusEnabled; // Khóa/Mở khóa ô trạng thái

    // Danh sách lựa chọn
    public List<string> Genders { get; } = new List<string> { "Nam", "Nữ" };
    public List<string> StatusOptions { get; } = new List<string> { "Chờ khám", "Đang điều trị", "Hoàn thành điều trị" };
    public List<string> SeverityOptions { get; } = new List<string> { "Bình thường", "Gấp", "Khẩn cấp", "Cấp cứu" };

    private bool isEditing = false;
    private Patient patientToEdit;

    // --- CÁC HÀM LOGIC ---

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

        // So sánh code (critical, urgent...)
        string severity = patient.Severity?.ToLower() ?? "";

        if (severity == "critical" || severity == "cấp cứu") score += 1000;
        else if (severity == "emergency" || severity == "khẩn cấp") score += 500;
        else if (severity == "urgent" || severity == "gấp") score += 200;
        else if (severity == "medium") score += 50;

        if (patient.Age < 12) score += 20;
        if (patient.Age > 65) score += 20;

        // Trừ điểm theo thứ tự hàng đợi để đảm bảo ai đến trước (số nhỏ) ưu tiên hơn
        score -= patient.QueueOrder * 0.1;
        return score;
    }

    public void SortPatientQueue()
    {
        if (WaitingQueue == null || WaitingQueue.Count == 0) return;

        // Tính điểm và sắp xếp lại
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

    // --- COMMANDS ---

    [RelayCommand]
    private void ShowAddPatientPopup()
    {
        isEditing = false;
        PopupTitle = "Tiếp nhận bệnh nhân mới";
        ClearPopupForm();

        // Logic khóa form khi tiếp nhận mới
        NewPatientStatus = "Chờ khám";
        IsStatusEnabled = false;

        // THÊM: Load danh sách bác sĩ
        LoadDoctorsList();

        IsAddPatientPopupVisible = true;
    }

    [RelayCommand]
    private void CloseAddPatientPopup()
    {
        IsAddPatientPopupVisible = false;
        ClearPopupForm();
    }

    // THÊM: Hàm load danh sách bác sĩ
    private void LoadDoctorsList()
    {
        AvailableDoctors = HospitalSystem.Instance.Doctors;
        SelectedDoctor = null; // Reset lựa chọn
    }

    [RelayCommand]
    private async Task CallPatient(Patient patient)
    {
        if (patient == null) return;

        bool isConfirmed = await Application.Current.MainPage.DisplayAlert(
            "Xác nhận khám",
            $"Mời bệnh nhân {patient.FullName} vào khám ngay?",
            "Gọi ngay",
            "Hủy");

        if (isConfirmed)
        {
            // 1. Cập nhật trạng thái và Lưu vào Database
            patient.Status = "Đang điều trị";
            await _databaseService.SavePatientAsync(patient);

            // 2. Xóa khỏi hàng đợi trên giao diện
            WaitingQueue.Remove(patient);

            // 3. Khởi tạo VM Khám bệnh
            // Truyền _databaseService (đã có ở file chính) vào ExaminationViewModel
            var examVM = new ExaminationViewModel(patient, _databaseService);

            // 4. Chuyển trang
            var examPage = new ExaminationPageView(examVM);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.Navigation.PushModalAsync(examPage);
            });
        }
    }

    /// <summary>
    /// XỬ LÝ TIẾP NHẬN BỆNH NHÂN TỪ LỊCH HẹN
    /// Được gọi khi nhấn nút "Tiếp nhận" trên thẻ lịch hẹn
    /// </summary>
    public async void HandleCheckInFromAppointment(Appointment appt)
    {
        if (appt == null) return;

        // 1. Tạo hồ sơ bệnh nhân mới
        var newPatient = new Patient
        {
            FullName = appt.PatientName,
            Id = "P" + DateTime.Now.Ticks.ToString().Substring(12), // Tạo ID ngẫu nhiên
            Gender = "Khác",
            PhoneNumber = appt.PhoneNumber,
            Address = "Chưa cập nhật",
            Symptoms = appt.Note ?? "Đặt lịch hẹn trước",
            Status = "Chờ khám",
            Severity = "normal",
            PriorityScore = 10,
            Doctorname = appt.DoctorObject?.Name ?? appt.DoctorName ?? "Chưa chỉ định"
        };

        // 2. QUAN TRỌNG: Lưu vào Database trước để không bị mất dữ liệu
        // (Giả sử bạn có hàm SavePatientAsync trong Service, nếu chưa có thì xem phần dưới)
        if (_databaseService != null)
        {
            await _databaseService.SavePatientAsync(newPatient);
        }

        // 3. Cập nhật UI (Thêm vào danh sách hiện tại thay vì tạo mới)
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Kiểm tra nếu WaitingQueue chưa được khởi tạo thì khởi tạo nó
            if (WaitingQueue == null)
                WaitingQueue = new ObservableCollection<Patient>();

            // Chỉ dùng lệnh Add, TUYỆT ĐỐI KHÔNG dùng "WaitingQueue = new..."
            WaitingQueue.Add(newPatient);

            // 4. Sắp xếp lại hàng đợi
            SortPatientQueue();
        });
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
        SelectedDoctor = null; // THÊM: Reset picker
        isEditing = false;
        patientToEdit = null;
    }

    [RelayCommand]
    private async Task ConfirmAddPatient()
    {
        // 1. Kiểm tra dữ liệu nhập
        if (string.IsNullOrWhiteSpace(NewPatientFullName))
        {
            await Shell.Current.DisplayAlert("Lỗi", "Vui lòng nhập tên bệnh nhân", "OK");
            return;
        }

        // 2. Tạo đối tượng Patient từ Form nhập liệu
        var newPatient = new Patient
        {
            FullName = NewPatientFullName,
            Id = "P" + DateTime.Now.Ticks.ToString().Substring(12),
            DateOfBirth = NewPatientDateOfBirth,
            Gender = NewPatientGender ?? "Khác",
            PhoneNumber = NewPatientPhoneNumber,
            Address = NewPatientAddress,
            Status = NewPatientStatus,
            Severity = GetSeverityCode(NewPatientSeverity), // Chuyển đổi "Cấp cứu" -> "critical"
            Symptoms = NewPatientSymptoms,
            Doctorname = SelectedDoctor?.Name ?? "Chưa chỉ định", // Lấy từ Picker
            PriorityScore = 10 // Tính toán lại sau
        };

        // 3. Tính điểm ưu tiên chuẩn xác
        newPatient.PriorityScore = CalculatePriority(newPatient);

        // 4. Lưu vào Database (QUAN TRỌNG: Lưu trước khi thêm vào UI)
        if (_databaseService != null)
        {
            await _databaseService.SavePatientAsync(newPatient);
        }

        // 5. Thêm vào danh sách hiển thị (FIX LỖI GHI ĐÈ Ở ĐÂY)
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (WaitingQueue == null)
                WaitingQueue = new ObservableCollection<Patient>();

            // TUYỆT ĐỐI KHÔNG DÙNG: WaitingQueue = new...
            // PHẢI DÙNG: .Add()
            WaitingQueue.Add(newPatient);

            // 6. Sắp xếp lại hàng đợi
            SortPatientQueue();

            // 7. Đóng Popup và xóa form
            CloseAddPatientPopup();
        });
    }
}