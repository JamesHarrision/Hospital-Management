using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HosipitalManager.MVVM.Models;
using HosipitalManager.MVVM.Services;
using HosipitalManager.MVVM.Views;
using HospitalManager.MVVM.Messages;
using System.Collections.ObjectModel;
using static HospitalManager.MVVM.ViewModels.DashboardViewModel;
using Appointment = HosipitalManager.MVVM.Models.Appointment;

namespace HosipitalManager.MVVM.ViewModels
{
    public partial class AppointmentViewModel : ObservableObject
    {
        #region Services & Fields
        private readonly LocalDatabaseService _databaseService;
        // Danh sách gốc (tham chiếu từ Database)
        private List<Appointment> _sourceAppointments = new();
        #endregion

        #region Properties
        // Danh sách hiển thị (sau khi lọc)
        [ObservableProperty]
        private ObservableCollection<Appointment> _filteredAppointments;

        [ObservableProperty]
        private bool _isBusy;

        // Các biến thống kê
        [ObservableProperty] int _totalCount;
        [ObservableProperty] int _upcomingCount;
        [ObservableProperty] int _pendingCount;
        [ObservableProperty] int _cancelledCount;

        // Tab hiện tại và từ khóa tìm kiếm
        [ObservableProperty] AppointmentStatus _currentTab = AppointmentStatus.Upcoming;
        [ObservableProperty] string _searchText;
        #endregion

        #region Constructor
        public AppointmentViewModel(LocalDatabaseService databaseService)
        {
            _databaseService = databaseService;
            // Lấy dữ liệu từ Singleton Service
            //_sourceAppointments = HospitalSystem.Instance.Appointments;

            // Khởi tạo danh sách hiển thị
            FilteredAppointments = new ObservableCollection<Appointment>();

            IsBusy = true;
            Task.Run(LoadData);

            WeakReferenceMessenger.Default.Register<DashboardRefreshMessage>(this, (r, m) =>
            {
                IsBusy = true;
                Task.Run(LoadData);
            });
        }
        #endregion

        #region Methods (Logic)
        /// <summary>
        /// Hàm tải dữ liệu chính, xử lý Async và Exception an toàn
        /// </summary>
        public async Task LoadData()
        {
            if (_databaseService == null) return;
            var appointmentsFromDb = await _databaseService.GetAppointmentsAsync();

            MapDoctorInfo(appointmentsFromDb);

            _sourceAppointments = appointmentsFromDb;

            // 3. Cập nhật giao diện trên MainThread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RefreshData();
                IsBusy = false;
            });
        }

        /// <summary>
        /// Gán object Doctor vào Appointment dựa trên DoctorId
        /// </summary>
        private void MapDoctorInfo(List<Appointment> appointments)
        {
            var doctors = HospitalSystem.Instance.Doctors; // Lấy danh sách bác sĩ tĩnh
            foreach (var appt in appointments)
            {
                if (!string.IsNullOrEmpty(appt.DoctorId))
                {
                    var doc = doctors.FirstOrDefault(d => d.Id == appt.DoctorId);
                    if (doc != null) appt.DoctorObject = doc;
                }
            }
        }

        /// <summary>
        /// Tính toán lại thống kê và lọc danh sách
        /// </summary>
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
            if (_sourceAppointments == null) return;
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

            FilteredAppointments.Clear();
            foreach (var item in query)
            {
                FilteredAppointments.Add(item);
            }
        }

        partial void OnSearchTextChanged(string value) => FilterData();
        #endregion

        #region Commands (Sự kiện người dùng)
        [RelayCommand]
        public void SwitchTab(string statusStr)
        {
            Task.Run(LoadData);
            if (Enum.TryParse(statusStr, out AppointmentStatus status))
            {
                CurrentTab = status;
                FilterData();
            }
        }

        [RelayCommand]
        public async Task NavigateToAdd()
        {
            // Điều hướng sang trang thêm mới
            await Shell.Current.GoToAsync(nameof(NewAppointmentPageView));
        }

        [RelayCommand]
        public void PerformSearch()
        {
            FilterData();
        }

        [RelayCommand]
        public async Task ConfirmAppointment(Appointment appt)
        {
            if (appt == null) return;

            try
            {
                // 1. Kiểm tra trùng lịch (Gọi Service đã nâng cấp)
                bool isConflict = await _databaseService.IsAppointmentConflictingAsync(appt);

                if (isConflict)
                {
                    await Shell.Current.DisplayAlert("Trùng lịch",
                        $"Bác sĩ {appt.DoctorName ?? "này"} đã có lịch hẹn khác trong khung giờ này.", "Đóng");
                    return;
                }

                // 2. Hỏi xác nhận
                bool confirm = await Shell.Current.DisplayAlert("Xác nhận",
                    $"Duyệt lịch hẹn của {appt.PatientName} lúc {appt.StartTime:hh\\:mm}?", "Duyệt", "Hủy");

                if (confirm)
                {
                    appt.Status = AppointmentStatus.Upcoming;
                    await _databaseService.SaveAppointmentAsync(appt);

                    // Refresh UI
                    RefreshData();
                    WeakReferenceMessenger.Default.Send(new DashboardRefreshMessage());

                    // (Optional) Thông báo nhỏ
                    // await Toast.Make("Đã duyệt lịch hẹn").Show(); 
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Lỗi", $"Không thể duyệt: {ex.Message}", "OK");
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
        public async Task OnAppearingAsync()
        {
            IsBusy = true;
            await LoadData();
        }
        #endregion
    }
}