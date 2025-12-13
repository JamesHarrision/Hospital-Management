using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HosipitalManager.MVVM.Models;
using HosipitalManager.MVVM.Services;
using System.Collections.ObjectModel;
using static HospitalManager.MVVM.ViewModels.DashboardViewModel;
using HosipitalManager.Helpers;

namespace HosipitalManager.MVVM.ViewModels
{
    public partial class NewAppointmentViewModel : ObservableObject
    {
        // Khai báo Service Database
        private readonly LocalDatabaseService _databaseService;
        // Danh sách bác sĩ để chọn
        public ObservableCollection<Doctor> Doctors { get; }

        // Các trường nhập liệu
        [ObservableProperty] Doctor _selectedDoctor;
        [ObservableProperty] string _patientName;
        [ObservableProperty] string _phoneNumber;
        [ObservableProperty] string _note;
        [ObservableProperty] DateTime _date = DateTime.Today;
        [ObservableProperty] TimeSpan _time = DateTime.Now.TimeOfDay;

        public NewAppointmentViewModel(LocalDatabaseService databaseService)
        {
            _databaseService = databaseService;
            // Lấy danh sách bác sĩ từ Service
            Doctors = HospitalSystem.Instance.Doctors;
        }

        [RelayCommand]
        public async Task Save()
        {
            // Validate cơ bản
            if (SelectedDoctor == null)
            {
                await Shell.Current.DisplayAlert("Thiếu thông tin", "Vui lòng chọn bác sĩ", "OK");
                return;
            }
            if (!ValidationHelper.IsValidName(PatientName))
            {
                await Shell.Current.DisplayAlert("Lỗi", "Tên bệnh nhân không được chứa số hay ký tự lạ.", "OK");
                return;
            }

            // Validate SĐT
            if (!ValidationHelper.IsValidPhoneNumber(PhoneNumber))
            {
                await Shell.Current.DisplayAlert("Lỗi", "Số điện thoại không đúng định dạng (10 số, bắt đầu bằng 0).", "OK");
                return;
            }

            // Validate Ngày hẹn (Không được chọn quá khứ)
            if (Date.Date < DateTime.Now.Date)
            {
                await Shell.Current.DisplayAlert("Lỗi", "Không thể đặt lịch hẹn trong quá khứ.", "OK");
                return;
            }

            // Nếu chọn ngày hôm nay thì giờ hẹn phải lớn hơn giờ hiện tại
            if (Date.Date == DateTime.Now.Date && Time < DateTime.Now.TimeOfDay)
            {
                await Shell.Current.DisplayAlert("Lỗi", "Giờ hẹn đã qua. Vui lòng chọn giờ khác.", "OK");
                return;
            }

            // Tạo object Appointment mới
            var newAppt = new Appointment
            {
                DoctorId = SelectedDoctor.Id.ToString(),
                DoctorName = SelectedDoctor.Name,// Gán object Doctor đã chọn
                DoctorObject = SelectedDoctor,
                PatientName = PatientName,
                PhoneNumber = PhoneNumber,
                Note = Note,
                AppointmentDate = Date,
                StartTime = Time,
                EndTime = Time.Add(TimeSpan.FromMinutes(30)), // Mặc định khám 30p
                Status = AppointmentStatus.Pending // Mặc định là Chờ xác nhận
            };

            // Lưu vào Database 
            await _databaseService.SaveAppointmentAsync(newAppt);

            await Shell.Current.DisplayAlert("Thành công", "Đã thêm lịch hẹn mới!", "OK");

            WeakReferenceMessenger.Default.Send(new DashboardRefreshMessage());

            // Quay lại trang trước
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        public async Task Cancel()
        {
            await Shell.Current.GoToAsync("..");
        }


    }
}