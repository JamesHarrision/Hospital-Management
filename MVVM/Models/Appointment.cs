using SQLite;

namespace HosipitalManager.MVVM.Models
{
    public enum AppointmentStatus
    {
        Upcoming, // Sắp tới, 0
        Pending,  // Chờ xác nhận, 1
        Completed, // Đã hoàn thành, 2
        Cancelled // Đã hủy, 3 
    }

    [Table("Appointments")]
    public class Appointment
    {
        [PrimaryKey] // Tự động tăng ID: 1, 2, 3...
        public string Id { get; set; }
        public string DoctorId { get; set; }
        public string DoctorName { get; set; }

        [Ignore]
        public Doctor DoctorObject { get; set; }

        public string PatientName { get; set; }
        public string PhoneNumber { get; set; }
        public string Note { get; set; }

        // Thời gian
        public DateTime AppointmentDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public AppointmentStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}