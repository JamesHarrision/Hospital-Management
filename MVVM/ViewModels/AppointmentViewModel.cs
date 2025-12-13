using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HosipitalManager.MVVM.Models;
using HosipitalManager.MVVM.Services;
using HosipitalManager.MVVM.Views;
using System.Collections.ObjectModel;
using static HospitalManager.MVVM.ViewModels.DashboardViewModel;

namespace HosipitalManager.MVVM.ViewModels
{
    public partial class AppointmentViewModel : ObservableObject
    {
        private readonly LocalDatabaseService _databaseService;
        // Danh sách gốc (tham chiếu từ Database)
        private List<Appointment> _sourceAppointments = new();

        // Danh sách hiển thị (sau khi lọc)
        [ObservableProperty]
        private ObservableCollection<Appointment> _filteredAppointments;

        // Các biến thống kê
        [ObservableProperty] int _totalCount;
        [ObservableProperty] int _upcomingCount;
        [ObservableProperty] int _pendingCount;
        [ObservableProperty] int _cancelledCount;

        // Tab hiện tại và từ khóa tìm kiếm
        [ObservableProperty] AppointmentStatus _currentTab = AppointmentStatus.Upcoming;
        [ObservableProperty] string _searchText;

        public AppointmentViewModel(LocalDatabaseService databaseService)
        {
            _databaseService = databaseService;
            // Lấy dữ liệu từ Singleton Service
            //_sourceAppointments = HospitalSystem.Instance.Appointments;

            // Khởi tạo danh sách hiển thị
            FilteredAppointments = new ObservableCollection<Appointment>();

            Task.Run(LoadData);

            WeakReferenceMessenger.Default.Register<DashboardRefreshMessage>(this, (r, m) =>
            {
                Task.Run(LoadData);
            });
        }

        public async Task LoadData()
        {
            var appointmentsFromDb = await _databaseService.GetAppointmentsAsync();

            foreach (var appt in appointmentsFromDb)
            {
                if (!string.IsNullOrEmpty(appt.DoctorId))
                {
                    // Tìm bác sĩ trong danh sách tĩnh bằng ID
                    var doc = HospitalSystem.Instance.Doctors.FirstOrDefault(d => d.Id == appt.DoctorId);
                    if (doc != null)
                    {
                        appt.DoctorObject = doc; // Gán vào để Binding lên View
                    }
                }
            }

            _sourceAppointments = appointmentsFromDb;

            // 3. Cập nhật giao diện trên MainThread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RefreshData();
            });
        }

        // Hàm làm mới dữ liệu (gọi khi vào trang hoặc khi có thay đổi)
        public void RefreshData()
        {
            UpdateStats();
            FilterData();
        }

        private void UpdateStats()
        {
            TotalCount = _sourceAppointments.Count;
            UpcomingCount = _sourceAppointments.Count(a => a.Status == AppointmentStatus.Upcoming);
            PendingCount = _sourceAppointments.Count(a => a.Status == AppointmentStatus.Pending);
            CancelledCount = _sourceAppointments.Count(a => a.Status == AppointmentStatus.Cancelled);
        }

        private void FilterData()
        {
            var query = _sourceAppointments.AsEnumerable();

            // 1. Lọc theo Tab
            query = query.Where(a => a.Status == CurrentTab);

            // 2. Lọc theo Search Text (nếu có)
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var lowerText = SearchText.ToLower();
                query = query.Where(a =>
                    (a.DoctorObject != null && a.DoctorName.ToLower().Contains(lowerText)) ||
                    (a.PatientName != null && a.PatientName.ToLower().Contains(lowerText)));
            }

            FilteredAppointments = new ObservableCollection<Appointment>(query);
        }

        // --- COMMANDS ---

        [RelayCommand]
        public void SwitchTab(string statusStr)
        {
            if (Enum.TryParse(statusStr, out AppointmentStatus status))
            {
                CurrentTab = status;
                FilterData();
            }
        }

        [RelayCommand]
        public void PerformSearch()
        {
            FilterData();
        }

        [RelayCommand]
        public async Task NavigateToAdd()
        {
            // Điều hướng sang trang thêm mới
            await Shell.Current.GoToAsync(nameof(NewAppointmentPageView));
        }

        [RelayCommand]
        public async Task ConfirmAppointment(Appointment appt)
        {
            if (appt == null) return;

            // --- BẮT ĐẦU ĐOẠN CHECK TRÙNG ---

            // 1. Lấy danh sách các lịch ĐÃ DUYỆT của bác sĩ này trong ngày hôm đó
            var conflicts = _sourceAppointments.Where(a =>
                a.Id != appt.Id && // Không so sánh với chính nó
                a.Status == AppointmentStatus.Upcoming && // Chỉ so với lịch đã duyệt
                a.DoctorId == appt.DoctorId && // Cùng bác sĩ
                a.AppointmentDate.Date == appt.AppointmentDate.Date // Cùng ngày
            ).ToList();

            // 2. Kiểm tra va chạm thời gian
            foreach (var existing in conflicts)
            {
                // Công thức trùng: (StartA < EndB) và (EndA > StartB)
                if (appt.StartTime < existing.EndTime && appt.EndTime > existing.StartTime)
                {
                    await Shell.Current.DisplayAlert("Trùng lịch!",
                        $"Bác sĩ {appt.DoctorName} đã bận từ {existing.StartTime:hh\\:mm} đến {existing.EndTime:hh\\:mm}.\nKhông thể duyệt lịch này.",
                        "Đóng");
                    return; // Dừng lại ngay, không cho duyệt
                }
            }
            // --- KẾT THÚC ĐOẠN CHECK TRÙNG ---

            // Nếu không trùng thì hỏi xác nhận
            bool confirm = await Shell.Current.DisplayAlert("Xác nhận",
                $"Duyệt lịch hẹn của {appt.PatientName} lúc {appt.StartTime:hh\\:mm}?", "Duyệt", "Hủy");

            if (confirm)
            {
                appt.Status = AppointmentStatus.Upcoming;
                await _databaseService.SaveAppointmentAsync(appt);
                RefreshData();
                WeakReferenceMessenger.Default.Send(new DashboardRefreshMessage());
            }
        }

        [RelayCommand]
        public async Task CancelAppointment(Appointment appt)
        {
            if (appt == null) return;

            bool confirm = await Shell.Current.DisplayAlert("Hủy lịch",
                $"Bạn chắc chắn muốn hủy lịch của {appt.PatientName}?", "Đồng ý", "Thoát");

            if (confirm)
            {
                // 1. Chuyển trạng thái sang Cancelled
                appt.Status = AppointmentStatus.Cancelled;

                await _databaseService.SaveAppointmentAsync(appt);
                // 2. Làm mới danh sách
                RefreshData();

                // 3. Gửi tin nhắn (Để dashboard xóa nếu lỡ nó đang hiện)
                WeakReferenceMessenger.Default.Send(new DashboardRefreshMessage());
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            // Gọi hàm lọc dữ liệu ngay lập tức
            FilterData();
        }
        public async Task OnAppearingAsync()
        {
            await LoadData();
        }
    }
}