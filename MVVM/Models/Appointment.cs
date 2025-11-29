namespace HosipitalManager.MVVM.Models
{
    public enum AppointmentStatus
    {
        Upcoming, // Sắp tới
        Pending,  // Chờ xác nhận
        Completed, // Đã hoàn thành
        Cancelled // Đã hủy
    }

    public class Appointment
    {
        public int Id { get; set; }
        public Doctor Doctor { get; set; }

        public string PatientName { get; set; }
        public string PhoneNumber { get; set; }
        public string Note { get; set; }

        // Thời gian
        public DateTime AppointmentDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public AppointmentStatus Status { get; set; }
    }
}