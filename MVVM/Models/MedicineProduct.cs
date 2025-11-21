using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HosipitalManager.MVVM.Models
{
    public class MedicineProduct
    {
        public string Name { get; set; }      // Tên thuốc (VD: Panadol)
        public string Unit { get; set; }      // Đơn vị (Viên, Vỉ)
        public decimal UnitPrice { get; set; } // Đơn giá (VD: 2000)
    }
}
