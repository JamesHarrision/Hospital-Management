using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HosipitalManager.MVVM.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HosipitalManager.MVVM.ViewModels;

// File này chỉ lo việc Xếp hàng và Tiếp nhận
public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Patient> waitingQueue; // Hàng đợi

    [ObservableProperty]
    private bool isAddPatientPopupVisible = false;

    [ObservableProperty]
    private string popupTitle = "Tiếp nhận bệnh nhân mới";

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
    private async Task ShowAddPatientPopup()
    {
        isEditing = false;
        PopupTitle = "Tiếp nhận bệnh nhân mới";
        ClearPopupForm();

        EditingPatient = new Patient()
        {
            Id = await _patientRepository.GetNextPatientIDAsync(),
            Status = "Chờ khám",
            AdmittedDate = DateTime.Now
        };
        isStatusEnabled = false;
        IsAddPatientPopupVisible = true;
    }

    [RelayCommand]
    private void CloseAddPatientPopup()
    {
        IsAddPatientPopupVisible = false;
        ClearPopupForm();
    }

    [RelayCommand]
    private async Task SavePatient()
    {
        try
        {
            if (EditingPatient == null || string.IsNullOrWhiteSpace(EditingPatient.FullName))
            {
                await Application.Current.MainPage.DisplayAlert("Lỗi", "Vui lòng nhập họ tên bệnh nhân.", "OK");
                return;
            }

            EditingPatient.Severity = GetSeverityCode(EditingPatient.Severity ?? "Bình thường");
            // Kiểm tra bệnh nhân đã có trong danh sách?
            var existing = Patients.FirstOrDefault(p => p.Id == EditingPatient.Id);

            if (existing == null)
            {
                // Bệnh nhân mới
                EditingPatient.QueueOrder = WaitingQueue.Count + 1;
                EditingPatient.PriorityScore = CalculatePriority(EditingPatient);

                await _patientRepository.AddAsync(EditingPatient);
                Patients.Add(EditingPatient);
                WaitingQueue.Add(EditingPatient);
            }
            else
            {
                // ============= Cập nhật vào DB =============
                await _patientRepository.UpdateAsync(EditingPatient);

                // Cập nhật vào ObservableCollection (UI sẽ tự refresh)
                existing.FullName = EditingPatient.FullName;
                existing.DateOfBirth = EditingPatient.DateOfBirth;
                existing.Gender = EditingPatient.Gender;
                existing.PhoneNumber = EditingPatient.PhoneNumber;
                existing.Address = EditingPatient.Address;
                existing.AdmittedDate = EditingPatient.AdmittedDate;
                existing.Status = EditingPatient.Status;
                existing.Severity = EditingPatient.Severity;
                existing.Symptoms = EditingPatient.Symptoms;
                existing.PriorityScore = EditingPatient.PriorityScore;
                existing.QueueOrder = EditingPatient.QueueOrder;
            }

            // Đóng popup (nếu bạn muốn)
            IsAddPatientPopupVisible = false;
            ClearPopupForm();
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task CallPatient(Patient patient)
    {
        if (patient == null) return;

        bool isConfirmed = await Application.Current.MainPage.DisplayAlert(
            "Xác nhận",
            $"Mời bệnh nhân {patient.FullName} vào khám ngay?",
            "Gọi khám",
            "Hủy");

        if (isConfirmed)
        {
            patient.Status = "Đang điều trị";
            await _patientRepository.UpdateAsync(patient);
            WaitingQueue.Remove(patient); // Xóa khỏi hàng đợi
        }
        
    }

    private void ClearPopupForm()
    {
        EditingPatient = null;
        isEditing = false;
        patientToEdit = null;
    }
}