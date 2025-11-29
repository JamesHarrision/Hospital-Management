using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HosipitalManager.MVVM.Models;
using HosipitalManager.MVVM.Services;
using System.Collections.ObjectModel;
using static HospitalManager.MVVM.ViewModels.DashboardViewModel;

namespace HosipitalManager.MVVM.ViewModels
{
    public partial class NewAppointmentViewModel : ObservableObject
    {
        // Danh sách bác sĩ để chọn
        public ObservableCollection<Doctor> Doctors { get; }

        // Các trường nhập liệu
        [ObservableProperty] Doctor _selectedDoctor;
        [ObservableProperty] string _patientName;
        [ObservableProperty] string _phoneNumber;
        [ObservableProperty] string _note;
        [ObservableProperty] DateTime _date = DateTime.Today;
        [ObservableProperty] TimeSpan _time = DateTime.Now.TimeOfDay;

        public NewAppointmentViewModel()
        {
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
            if (string.IsNullOrWhiteSpace(PatientName))
            {
                await Shell.Current.DisplayAlert("Thiếu thông tin", "Vui lòng nhập tên bệnh nhân", "OK");
                return;
            }

            // Tạo object Appointment mới
            var newAppt = new Appointment
            {
                Doctor = SelectedDoctor, // Gán object Doctor đã chọn
                PatientName = PatientName,
                PhoneNumber = PhoneNumber,
                Note = Note,
                AppointmentDate = Date,
                StartTime = Time,
                EndTime = Time.Add(TimeSpan.FromMinutes(30)), // Mặc định khám 30p
                Status = AppointmentStatus.Pending // Mặc định là Chờ xác nhận
            };

            // Lưu vào Service
            HospitalSystem.Instance.AddAppointment(newAppt);

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