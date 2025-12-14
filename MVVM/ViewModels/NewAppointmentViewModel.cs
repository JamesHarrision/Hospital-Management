using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HosipitalManager.Helpers;
using HosipitalManager.MVVM.Models;
using HosipitalManager.MVVM.Services;
using HospitalManager.MVVM.Messages;
using System.Collections.ObjectModel;
using static HospitalManager.MVVM.ViewModels.DashboardViewModel;

namespace HosipitalManager.MVVM.ViewModels
{
    public partial class NewAppointmentViewModel : ObservableObject
    {
        #region 1. Dependencies & Collections
        private readonly LocalDatabaseService _databaseService;

        // Danh sách bác sĩ để chọn (Binding lên Picker)
        public ObservableCollection<Doctor> Doctors { get; }
        #endregion

        #region 2. Properties: Input Fields
        [ObservableProperty] private Doctor _selectedDoctor;
        [ObservableProperty] private string _patientName;
        [ObservableProperty] private string _phoneNumber;
        [ObservableProperty] private string _note;

        // Mặc định là ngày hôm nay
        [ObservableProperty] private DateTime _date = DateTime.Today;

        // Mặc định là giờ hiện tại
        [ObservableProperty] private TimeSpan _time = DateTime.Now.TimeOfDay;
        #endregion

        #region 3. Constructor
        public NewAppointmentViewModel(LocalDatabaseService databaseService)
        {
            _databaseService = databaseService;

            // Lấy danh sách bác sĩ từ Singleton System (Cache sẵn)
            Doctors = HospitalSystem.Instance.Doctors;
        }
        #endregion

        #region 4. Commands
        [RelayCommand]
        public async Task Save()
        {
            // 1. Validate dữ liệu đầu vào
            if (!ValidateInput()) return;

            // 2. Tạo object Appointment mới
            var newAppt = CreateAppointmentObject();

            // 3. Lưu vào Database 
            await _databaseService.SaveAppointmentAsync(newAppt);

            // 4. Thông báo thành công & Refresh Dashboard
            await Shell.Current.DisplayAlert("Thành công", "Đã thêm lịch hẹn mới!", "OK");
            WeakReferenceMessenger.Default.Send(new DashboardRefreshMessage());

            // 5. Quay lại trang trước
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        public async Task Cancel()
        {
            await Shell.Current.GoToAsync("..");
        }
        #endregion

        #region 5. Helper Methods (Validation & Logic)
        /// <summary>
        /// Kiểm tra tính hợp lệ của dữ liệu nhập
        /// </summary>
        private bool ValidateInput()
        {
            // Kiểm tra bác sĩ
            if (SelectedDoctor == null)
            {
                ShowError("Vui lòng chọn bác sĩ");
                return false;
            }

            // Kiểm tra tên
            if (!ValidationHelper.IsValidName(PatientName))
            {
                ShowError("Tên bệnh nhân không được chứa số và không chứa ký tự đặc biệt.");
                return false;
            }

            // Kiểm tra SĐT
            if (!ValidationHelper.IsValidPhoneNumber(PhoneNumber))
            {
                ShowError("Số điện thoại phải bắt đầu bằng số 0 và có 10 chữ số.");
                return false;
            }

            // Kiểm tra Ngày (Quá khứ)
            if (Date.Date < DateTime.Now.Date)
            {
                ShowError("Không thể đặt lịch hẹn trong quá khứ.");
                return false;
            }

            // Kiểm tra Giờ (nếu là hôm nay)
            if (Date.Date == DateTime.Now.Date && Time < DateTime.Now.TimeOfDay)
            {
                ShowError("Giờ hẹn đã qua. Vui lòng chọn giờ khác.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tạo object Appointment từ các Properties hiện tại
        /// </summary>
        private Appointment CreateAppointmentObject()
        {
            return new Appointment
            {
                DoctorId = SelectedDoctor.Id.ToString(),
                DoctorName = SelectedDoctor.Name,
                DoctorObject = SelectedDoctor,
                PatientName = PatientName,
                PhoneNumber = PhoneNumber,
                Note = Note,
                AppointmentDate = Date,
                StartTime = Time,
                EndTime = Time.Add(TimeSpan.FromMinutes(30)), // Mặc định khám 30p
                Status = AppointmentStatus.Pending // Mặc định: Chờ xác nhận
            };
        }

        /// <summary>
        /// Hàm hiển thị lỗi nhanh
        /// </summary>
        private void ShowError(string message)
        {
            Shell.Current.DisplayAlert("Lỗi", message, "OK");
        }
        #endregion
    }
}