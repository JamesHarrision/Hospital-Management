using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HosipitalManager.MVVM.Models;
using HosipitalManager.Services;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.ObjectModel;
using System.Linq;         
using System.Threading.Tasks;

namespace HosipitalManager.MVVM.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    // Dữ liệu thống kê
    [ObservableProperty]
    private ObservableCollection<SummaryCard> summaryCards = new();

    // Hoạt động gần đây
    [ObservableProperty]
    private ObservableCollection<RecentActivity> recentActivities = new();

    [ObservableProperty]
    private string userName = "Dr. Khang";

    [ObservableProperty]
    private string userAvatar = "person_placeholder.png";

    private readonly PatientRepository _patientRepository = new PatientRepository();

    // Danh sách bệnh nhân hiển thị trên UI
    public ObservableCollection<Patient> Patients { get; } = new();

    [ObservableProperty]
    private Patient editingPatient;

    [ObservableProperty]
    private bool isAddEditPopupVisible;

    public DashboardViewModel()
    {
        WaitingQueue = new ObservableCollection<Patient>();      // Hàng đợi
        Prescriptions = new ObservableCollection<Prescription>(); // Đơn thuốc

        // 2. Nạp dữ liệu tĩnh (card, đơn thuốc mẫu nếu có)
        LoadSummaryCards();
        LoadPrescriptions();
    }


    // Mở popup SỬA bệnh nhân (dùng cho nút edit trên mỗi dòng)
    [RelayCommand]
    private void ShowEditPatientPopup(Patient patient)
    {
        if (patient == null) return;

        // Tạo bản copy để chỉnh, tránh sửa trực tiếp object trong list
        EditingPatient = new Patient
        {
            Id = patient.Id,
            FullName = patient.FullName,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            PhoneNumber = patient.PhoneNumber,
            Address = patient.Address,
            AdmittedDate = patient.AdmittedDate,
            Status = patient.Status,
            Severity = patient.Severity,
            Symptoms = patient.Symptoms,
            PriorityScore = patient.PriorityScore,
            QueueOrder = patient.QueueOrder
        };

        IsAddEditPopupVisible = true;
    }

    private void LoadSummaryCards()
    {
        SummaryCards = new ObservableCollection<SummaryCard>
        {
            new SummaryCard {
                Title = "Tổng số Bệnh nhân",
                Value = "4,250",
                Icon = "person.png",
                ChangePercentage = "+12% so với tháng trước",
                CardColor = Color.FromArgb("#36A2EB")
            },
            new SummaryCard {
                Title = "Lịch hẹn hôm nay",
                Value = "52",
                Icon = "calendar.png",
                ChangePercentage = "+5% so với hôm qua",
                CardColor = Color.FromArgb("#FF6384")
            },
            new SummaryCard {
                Title = "Phòng trống",
                Value = "15",
                Icon = "bed.png",
                ChangePercentage = "25% Đã sử dụng",
                CardColor = Color.FromArgb("#4BC0C0")
            },
            new SummaryCard {
                Title = "Doanh thu (Tháng)",
                Value = "$350,000",
                Icon = "cash.png",
                ChangePercentage = "-2% so với mục tiêu",
                CardColor = Color.FromArgb("#FF9F40")
            }
        };
    }

    // Load dữ liệu bệnh nhân từ SQL Server
    public async Task LoadPatientsFromDbAsync()
    {
        try
        {
            var list = await _patientRepository.GetAllAsync();

            Patients.Clear();
            foreach (var p in list)
            {
                Patients.Add(p);
            }

            if (WaitingQueue != null)
            {
                WaitingQueue.Clear();
                foreach (var p in Patients)
                {
                    WaitingQueue.Add(p);
                }
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Lỗi",
                $"Không thể tải danh sách bệnh nhân:\n{ex.Message}",
                "OK");
        }
    }


    // XÓA bệnh nhân – gắn với DeletePatientCommand
    [RelayCommand]
    public async Task DeletePatientAsync(Patient p)
    {
        if (p == null)
            return;

        bool accept = await Application.Current.MainPage.DisplayAlert(
            "Xác nhận",
            $"Bạn có chắc muốn xóa bệnh nhân {p.FullName}?",
            "Xóa", "Hủy");

        if (!accept) return;

        await _patientRepository.DeleteAsync(p.Id);
        Patients.Remove(p);
    }
}
