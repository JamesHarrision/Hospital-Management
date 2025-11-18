using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HosipitalManager.MVVM.Models
{
    public class Prescription
    {
        public string Id { get; set; } // Mã đơn thuốc
        public string PatientId { get; set; }
        public string PatientName { get; set; } // Tên bệnh nhân
        public string DoctorId { get; set; }
        public string DoctorName { get; set; } // Bác sĩ kê đơn
        public DateTime DatePrescribed { get; set; } // Ngày kê đơn
        public string Status { get; set; } // Trạng thái (vd: "Đã cấp", "Chưa cấp")

        // Một đơn thuốc có nhiều loại thuốc
        public List<MedicationItem> Medications { get; set; } = new List<MedicationItem>();
    }
}
