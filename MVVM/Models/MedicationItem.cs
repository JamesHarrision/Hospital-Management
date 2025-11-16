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
        public string Quantity { get; set; }
        public string Instructions { get; set; }
    }
}
