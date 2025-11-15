// Nằm trong file: /MVVM/Models/Patient.cs
namespace HospitalManager.MVVM.Models
{
    public class Patient
    {
        public string Id { get; set; } // Mã bệnh nhân
        public string FullName { get; set; } // Tên đầy đủ
        public DateTime DateOfBirth { get; set; } // Ngày sinh
        public string Gender { get; set; } // Giới tính (Nam/Nữ)
        public string PhoneNumber { get; set; } // Số điện thoại
        public string Address { get; set; } // Địa chỉ
        public DateTime AdmittedDate { get; set; } // Ngày nhập viện
        public string Status { get; set; } // Tình trạng (Đang điều trị, Đã xuất viện...)

        // Thuộc tính tính toán (computed property) để hiển thị tuổi
        public int Age => DateTime.Today.Year - DateOfBirth.Year - (DateTime.Today.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);
    }
}