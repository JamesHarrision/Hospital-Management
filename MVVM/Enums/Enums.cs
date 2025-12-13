using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HosipitalManager.MVVM.Enums
{
    // Trạng thái đơn thuốc
    public enum PrescriptionStatus
    {
        Pending,  // Chưa cấp
        Issued,   // Đã cấp
        Cancelled // Đã hủy (Dự phòng cho tương lai)
    }

    // Trạng thái bệnh nhân
    public enum PatientStatus
    {
        Waiting,    // Chờ khám
        Examining,  // Đang khám
        Done        // Đã về
    }
}
