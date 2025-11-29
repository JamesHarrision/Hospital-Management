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

            WeakReferenceMessenger.Default.Register<DashboardRefreshMessage>(this, (r, m) =>
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

        [RelayCommand]
        public async Task ConfirmAppointment(Appointment appt)
        {
            if (appt == null) return;

            // --- BẮT ĐẦU ĐOẠN CHECK TRÙNG ---

            // 1. Lấy danh sách các lịch ĐÃ DUYỆT của bác sĩ này trong ngày hôm đó
            var conflicts = HospitalSystem.Instance.Appointments.Where(a =>
                a.Id != appt.Id && // Không so sánh với chính nó
                a.Status == AppointmentStatus.Upcoming && // Chỉ so với lịch đã duyệt
                a.Doctor.Id == appt.Doctor.Id && // Cùng bác sĩ
                a.AppointmentDate.Date == appt.AppointmentDate.Date // Cùng ngày
            ).ToList();

            // 2. Kiểm tra va chạm thời gian
            foreach (var existing in conflicts)
            {
                // Công thức trùng: (StartA < EndB) và (EndA > StartB)
                if (appt.StartTime < existing.EndTime && appt.EndTime > existing.StartTime)
                {
                    await Shell.Current.DisplayAlert("Trùng lịch!",
                        $"Bác sĩ {appt.Doctor.Name} đã bận từ {existing.StartTime:hh\\:mm} đến {existing.EndTime:hh\\:mm}.\nKhông thể duyệt lịch này.",
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

                // 2. Làm mới danh sách
                RefreshData();

                // 3. Gửi tin nhắn (Để dashboard xóa nếu lỡ nó đang hiện)
                WeakReferenceMessenger.Default.Send(new DashboardRefreshMessage());
            }
        }
    }
}