using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using CommunityToolkit.Mvvm.Messaging;

using HosipitalManager.MVVM.Models;

using HosipitalManager.MVVM.Services;

using HospitalManager.MVVM.Models;

using Microsoft.Maui.Graphics;

using System.Collections.ObjectModel;

using System.Linq;



namespace HospitalManager.MVVM.ViewModels;



public partial class DashboardViewModel : ObservableObject

{

    private readonly LocalDatabaseService _databaseService;



    // Dữ liệu thống kê

    [ObservableProperty]

    private ObservableCollection<SummaryCard> summaryCards;



    // Hoạt động gần đây (Có thể giữ hoặc bỏ nếu đã dùng Queue)

    [ObservableProperty]

    private ObservableCollection<RecentActivity> recentActivities;



    [ObservableProperty]

    private string userName = "Dr. Khang";



    [ObservableProperty]

    private string userAvatar = "person_placeholder.png";



    //DANH MỤC THUỐC CÓ SẴN (Giả lập kho thuốc của bệnh viện)

    [ObservableProperty]

    private ObservableCollection<MedicineProduct> availableMedicines;



    // Thuốc đang được chọn trong Dropdown (Picker)

    [ObservableProperty]

    private MedicineProduct selectedMedicineProduct;



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

    public DashboardViewModel() { }

    public DashboardViewModel(LocalDatabaseService databaseService)
    {
        _databaseService = databaseService;

        // 1. Khởi tạo các danh sách (Bắt buộc để không bị lỗi Null)
        SummaryCards = new ObservableCollection<SummaryCard>();
        Patients = new ObservableCollection<Patient>();
        WaitingQueue = new ObservableCollection<Patient>();
        Prescriptions = new ObservableCollection<Prescription>();
        AvailableMedicines = new ObservableCollection<MedicineProduct>();

        // 2. Load dữ liệu
        LoadSummaryCards();
        LoadMedicineCatalog();

        // Load dữ liệu nặng thì chạy ngầm
        Task.Run(async () => await ReloadAllData());

        // --- ĐĂNG KÝ MESSENGER (CHỈ ĐƯỢC CÓ 1 LẦN CHO MỖI LOẠI TIN NHẮN) ---

        // Đăng ký nhận lệnh Reload
        WeakReferenceMessenger.Default.Register<DashboardViewModel, string>(this, (r, message) =>
        {
            if (message == "ReloadPrescriptions")
            {
                Task.Run(async () => await r.LoadPatients());
            }
        });

        // Đăng ký nhận lệnh Check-in (SỬA LỖI TẠI ĐÂY)
        // Phải có chữ "DashboardViewModel" trong dấu < > để 'r' hiểu đúng kiểu dữ liệu
        WeakReferenceMessenger.Default.Register<DashboardViewModel, RequestCheckInMessage>(this, (r, m) =>
        {
            // Bây giờ bạn có thể gọi cả 2 hàm nếu muốn, hoặc chọn 1 trong 2

            // Cách 1: Thêm vào hàng đợi (Logic chính)
            r.HandleCheckInFromAppointment(m.Appointment);

            // Cách 2: Mở Popup (Nếu bạn muốn hiện popup thay vì thêm thẳng)
            // r.OpenCheckInPopup(m.Appointment); 
        });
    }

    private async Task ReloadAllData()
    {
        var t1 = LoadPatients();      // Load bảng phân trang

        var t2 = LoadWaitingQueue();  // Load hàng đợi bên trái

        await Task.WhenAll(t1, t2);   // Đợi cả 2 xong

    }

    // Hàm xử lý logic chuyển đổi

    private void AddToWaitingQueue(Appointment appt)
    {
        // 1. Tạo hồ sơ bệnh nhân từ thông tin lịch hẹn

        var newPatient = new Patient

        {

            FullName = appt.PatientName,

            Id = "P" + DateTime.Now.Ticks.ToString().Substring(12), // ID giả lập

            //Age = 0, // Chưa có thông tin

            Gender = "Khác",

            PhoneNumber = appt.PhoneNumber,

            Address = "Chưa cập nhật",

            Symptoms = appt.Note ?? "Đặt lịch hẹn trước", // Lý do khám

            //TimeIn = DateTime.Now.ToString("HH:mm"),

            Status = "Chờ khám",

            Severity = "normal", // Mặc định bình thường

            PriorityScore = 10,  // Điểm ưu tiên mặc định

            Doctorname = appt.DoctorObject.Name // Gán luôn bác sĩ đã hẹn

        };


        // 2. Thêm vào hàng đợi
        WaitingQueue.Add(newPatient);


        // 3. (Tùy chọn) Sắp xếp lại hàng đợi theo độ ưu tiên
        SortPatientQueue();
    }

    private void LoadMedicineCatalog()
    {

        // 1. Tạo Service

        var medService = new MedicineService();

        // 2. Lấy dữ liệu và đổ vào ObservableCollection

        var listFromService = medService.GetMedicineCatalog();

        AvailableMedicines = new ObservableCollection<MedicineProduct>(listFromService);
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

                CardColor = Color.FromArgb("#36A2EB") // Màu xanh dương

            },

            new SummaryCard {

                Title = "Lịch hẹn hôm nay",

                Value = "52",

                Icon = "calendar.png",

                ChangePercentage = "+5% so với hôm qua",

                CardColor = Color.FromArgb("#FF6384") // Màu đỏ hồng

            },

            new SummaryCard {

                Title = "Phòng trống",

                Value = "15",

                Icon = "bed.png",

                ChangePercentage = "25% Đã sử dụng",

                CardColor = Color.FromArgb("#4BC0C0") // Màu xanh ngọc

            },

            new SummaryCard {

                Title = "Doanh thu (Tháng)",

                Value = "$350,000",

                Icon = "cash.png",

                ChangePercentage = "-2% so với mục tiêu",

                CardColor = Color.FromArgb("#FF9F40") // Màu cam

            }

        };

    }



    public class DashboardRefreshMessage { }



    public class AddPatientToQueueMessage

    {

        public Appointment Appointment { get; set; }

        public AddPatientToQueueMessage(Appointment appt) { Appointment = appt; }

    }

}