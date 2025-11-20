using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HosipitalManager.MVVM.Models
{
    public class MonthlyRevenue
    {
        public string MonthName { get; set; }
        public decimal Amount { get; set; }
        public int PatientCount {  get; set; }
        public double PercentageBar { get; set; }
    }

    public class TopDoctor
    {
        public string Name { get; set; }
        public string Avatar { get; set; } 
        public decimal TotalRevenue { get; set; }
        public string KpiStatus { get; set; } 
    }


}
