using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HosipitalManager.MVVM.Models;
using HosipitalManager.MVVM.Services;
using HosipitalManager.MVVM.Views;
using System.Collections.ObjectModel;

namespace HosipitalManager.MVVM.ViewModels
{
    public partial class AppointmentViewModel : ObservableObject
    {
        // Danh sách gốc (tham chiếu từ Service)
        private ObservableCollection<Appointment> _sourceAppointments;

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

        public AppointmentViewModel()
        {
            // Lấy dữ liệu từ Singleton Service
            _sourceAppointments = HospitalSystem.Instance.Appointments;

            // Khởi tạo danh sách hiển thị
            FilteredAppointments = new ObservableCollection<Appointment>();

            // Tính toán và hiển thị lần đầu
            RefreshData();
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
                    (a.Doctor != null && a.Doctor.Name.ToLower().Contains(lowerText)) ||
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
    }
}