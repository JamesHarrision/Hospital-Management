using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HosipitalManager.MVVM.Models;
using HosipitalManager.MVVM.Services;
using HospitalManager.MVVM.Models;
using HospitalManager.MVVM.Messages;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Linq;

namespace HospitalManager.MVVM.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    #region Fields & Services
    private readonly LocalDatabaseService _databaseService;
    #endregion

    #region Properties - UI & Sidebar
    // --- Thông tin người dùng ---
    [ObservableProperty]
    private string userName = "Dr. Khang";
    [ObservableProperty]
    private string userAvatar = "person_placeholder.png";

    // --- Trạng thái Sidebar ---
    // Trạng thái menu (Mở/Đóng)
    [ObservableProperty]
    private bool isMenuExpanded = true;
    // Độ rộng menu: Mở = 250, Đóng = 70
    [ObservableProperty]
    private double sidebarWidth = 250;
    // Góc xoay của nút mũi tên (0 độ hoặc 180 độ)
    [ObservableProperty]
    private double menuArrowRotation = 0;
    // Độ mờ của chữ (1 = hiện, 0 = ẩn)
    [ObservableProperty]
    private double menuTextOpacity = 1;

    // --- Danh mục thuốc (Dùng chung cho dropdown) ---
    [ObservableProperty]
    private ObservableCollection<MedicineProduct> availableMedicines;
    [ObservableProperty]
    private MedicineProduct selectedMedicineProduct;
    #endregion

    #region Constructor
    public DashboardViewModel() { }

    public DashboardViewModel(LocalDatabaseService databaseService)
    {
        _databaseService = databaseService;

        InitializeCollections();

        LoadMedicineCatalog();

        // Load dữ liệu nặng thì chạy ngầm
        Task.Run(async () => await ReloadAllData());

        // Đăng ký nhận lệnh Reload
        WeakReferenceMessenger.Default.Register<DashboardViewModel, string>(this, (r, message) =>
        {
            if (message == "ReloadPrescriptions")
            {
                Task.Run(async () => await r.LoadPatients());
            }
        });

        // Đăng ký nhận lệnh Check-in 
        WeakReferenceMessenger.Default.Register<DashboardViewModel, RequestCheckInMessage>(this, (r, m) =>
        {
            r.HandleCheckInFromAppointment(m.Appointment);
        });
    }
    #endregion

    #region Methods - Data Loading
    private void InitializeCollections()
    {
        // Khởi tạo các danh sách (Bắt buộc để không bị lỗi Null)
        Patients = new ObservableCollection<Patient>();
        WaitingQueue = new ObservableCollection<Patient>();
        Prescriptions = new ObservableCollection<Prescription>();
        AvailableMedicines = new ObservableCollection<MedicineProduct>();
    }
    /// <summary>
    /// Hàm gọi tất cả các hàm Load từ các file Partial khác
    /// </summary>
    private async Task ReloadAllData()
    {
        if (_databaseService == null) return;

        // 1. Tải danh sách bệnh nhân
        var t1 = LoadPatients();

        // 2. Tải hàng đợi
        var t2 = LoadWaitingQueue();

        // 3. --- QUAN TRỌNG: THÊM DÒNG NÀY ---
        // Tải danh sách đơn thuốc ngay khi vào App
        var t3 = LoadPrescriptions();

        // Chờ tất cả tải xong
        await Task.WhenAll(t1, t2, t3);
    }

    // Hàm xử lý logic chuyển đổi
    private void LoadMedicineCatalog()
    {
        // 1. Tạo Service
        var medService = new MedicineService();
        // 2. Lấy dữ liệu và đổ vào ObservableCollection
        var listFromService = medService.GetMedicineCatalog();

        AvailableMedicines = new ObservableCollection<MedicineProduct>(listFromService);
    }
    #endregion

    #region Methods - Sidebar Logic
    [RelayCommand]
    private void ToggleSidebar()
    {
        IsMenuExpanded = !IsMenuExpanded;
        if (IsMenuExpanded)
        {
            // MỞ RỘNG
            SidebarWidth = 250;
            MenuArrowRotation = 0;   // Mũi tên quay về trái
            MenuTextOpacity = 1;     // Hiện chữ
        }
        else
        {
            // THU NHỎ
            SidebarWidth = 70;       // Chỉ đủ chỗ cho Icon
            MenuArrowRotation = 180; // Mũi tên quay sang phải
            MenuTextOpacity = 0;     // Ẩn chữ
        }
    }
    #endregion
}