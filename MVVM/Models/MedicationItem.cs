using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HosipitalManager.MVVM.Models
{
    public class MedicationItem
    {
        public string MedicationName { get; set; }  // Tên thuốc
        public string Dosage { get; set; }        // Liều dùng (500mg, 1 viên,...)
        public string Usage { get; set; }         // Cách dùng (2 lần/ngày,...)
        public decimal Price { get; set; }        // Giá tiền
        public int Quantity { get; set; }         // Số lượng
        public string Note { get; set; }          // Ghi chú

        public string Unit { get; set; }
        public string Instructions { get; set; }
    }
}
