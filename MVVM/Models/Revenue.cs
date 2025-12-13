using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HosipitalManager.MVVM.Models
{
    public partial class MonthlyRevenue : ObservableObject
    {
        [ObservableProperty] private string monthName;
        [ObservableProperty] private decimal amount;
        [ObservableProperty] private int patientCount;
        [ObservableProperty] private double percentageBar;
    }
}
