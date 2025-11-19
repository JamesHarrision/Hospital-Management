using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalManager.MVVM.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;
using HosipitalManager.MVVM.Services;

namespace HospitalManager.MVVM.ViewModels;

// File này chỉ lo việc Xếp hàng và Tiếp nhận
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
            _ => "normal"
        };
    }

    private double CalculatePriority(Patient patient)
    {
        double score = 0;
        if (patient.Severity == "critical") score += 1000;
        else if (patient.Severity == "emergency") score += 500;
        else if (patient.Severity == "urgent") score += 200;

        if (patient.Age < 12) score += 100;
        if (patient.Age > 65) score += 100;

        score -= patient.QueueOrder * 0.1;
        return score;
    }

    public void SortPatientQueue()
    {
        foreach (var p in WaitingQueue) p.PriorityScore = CalculatePriority(p);
        var sortedList = WaitingQueue.OrderByDescending(p => p.PriorityScore).ToList();
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

        // Logic khóa form
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

    [RelayCommand]
    private void SavePatient()
    {
        try
        {
            string severityCode = GetSeverityCode(NewPatientSeverity);

            if (isEditing && patientToEdit != null)
            {
                // Logic Sửa (Admin dùng)
                patientToEdit.FullName = NewPatientFullName;
                patientToEdit.DateOfBirth = NewPatientDateOfBirth;
                patientToEdit.Gender = NewPatientGender;
                patientToEdit.PhoneNumber = NewPatientPhoneNumber;
                patientToEdit.Address = NewPatientAddress;
                patientToEdit.Status = NewPatientStatus;
                patientToEdit.Severity = severityCode;
                patientToEdit.Symptoms = NewPatientSymptoms;
            }
            else
            {
                // Logic Thêm Mới (Tiếp nhận)
                var newPatient = new Patient
                {
                    Id = $"BN{new Random().Next(1000, 9999)}",
                    FullName = NewPatientFullName,
                    DateOfBirth = NewPatientDateOfBirth,
                    Gender = NewPatientGender,
                    PhoneNumber = NewPatientPhoneNumber,
                    Address = NewPatientAddress,
                    AdmittedDate = DateTime.Now,
                    Status = "Chờ khám", // Fix cứng
                    Severity = severityCode,
                    Symptoms = NewPatientSymptoms,
                    QueueOrder = WaitingQueue.Count + 1
                };
                WaitingQueue.Add(newPatient);
            }

            SortPatientQueue();
            CloseAddPatientPopup();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Lỗi: {ex.Message}");
        }
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
            // 1. Xử lý hàng đợi (như cũ)
            patient.Status = "Đang điều trị";
            Patients.Add(patient);
            WaitingQueue.Remove(patient);

            // 2. --- TẠO SERVICE KẾT NỐI ---
            // Truyền danh sách Prescriptions của Dashboard vào Service
            var presService = new PrescriptionService(Prescriptions);

            // 3. Khởi tạo VM Khám bệnh và đưa Service vào
            // (Đảm bảo ExaminationViewModel đã có constructor nhận Service nhé!)
            var examVM = new ExaminationViewModel(patient, presService);

            // 4. Chuyển trang
            var examPage = new HosipitalManager.MVVM.Views.ExaminationPageView(examVM);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.Navigation.PushModalAsync(examPage);
            });
        }
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
        isEditing = false;
        patientToEdit = null;
    }
}