using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HosipitalManager.MVVM.Models
{
    public class MedicationItem
    {
        public string MedicationName { get; set; }
        public string Dosage { get; set; }
        public string Usage { get; set; }
        public decimal Price { get; set; }       // Đơn giá
        public int Quantity { get; set; }        // Số lượng
        public decimal Total => Price * Quantity;
        public string Unit { get; set; }
        public string Instructions { get; set; }
    }
}
