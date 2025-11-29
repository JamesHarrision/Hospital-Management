using HosipitalManager.MVVM.Models;
using System.Collections.ObjectModel;

namespace HosipitalManager.MVVM.Services
{
    public class HospitalSystem
    {
        // Singleton pattern để dữ liệu tồn tại xuyên suốt ứng dụng chạy
        private static HospitalSystem _instance;
        public static HospitalSystem Instance => _instance ??= new HospitalSystem();

        public ObservableCollection<Doctor> Doctors { get; private set; }
        public ObservableCollection<Appointment> Appointments { get; private set; }

        private HospitalSystem()
        {
            InitializeData();
        }

        private void InitializeData()
        {
            // 1. Tạo dữ liệu Bác sĩ giả lập
            Doctors = new ObservableCollection<Doctor>
            {
                new Doctor { Id = 1, Name = "Mai Trọng Khang", Specialization = "Thần Kinh", ImageSource = "doctor1.png" },
                new Doctor { Id = 2, Name = "Nguyễn Ngọc Quý", Specialization = "Nha Khoa", ImageSource = "doctor2.png" },
            };

            // 2. Tạo dữ liệu Lịch hẹn giả lập
            Appointments = new ObservableCollection<Appointment>
            {
                new Appointment
                {
                    Id = 1,
                    Doctor = Doctors[0], 
                    PatientName = "Trần Văn Nam",
                    PhoneNumber = "0901234567",
                    AppointmentDate = DateTime.Today.AddDays(3), // Ngày mai
                    StartTime = new TimeSpan(14, 0, 0),
                    EndTime = new TimeSpan(14, 45, 0),
                    Status = AppointmentStatus.Upcoming
                },
                new Appointment
                {
                    Id = 2,
                    Doctor = Doctors[1],
                    PatientName = "Phạm Thị Mai",
                    PhoneNumber = "0987654321",
                    AppointmentDate = DateTime.Today,
                    StartTime = new TimeSpan(9, 30, 0),
                    EndTime = new TimeSpan(10, 15, 0),
                    Status = AppointmentStatus.Pending
                }
            };
        }

        public void AddAppointment(Appointment appointment)
        {
            // Giả lập ID tự tăng
            appointment.Id = Appointments.Count > 0 ? Appointments.Max(a => a.Id) + 1 : 1;
            Appointments.Add(appointment);
        }
    }
}