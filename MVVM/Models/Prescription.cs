using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HosipitalManager.MVVM.Models
{
    public partial class Prescription : ObservableObject
    {
        public string Id { get; set; }
        public string PatientId { get; set; }
        public string PatientName { get; set; }
        public string DoctorId { get; set; }
        public string DoctorName { get; set; }
        public DateTime DatePrescribed { get; set; }
        public string Status { get; set; }

        // 2. Dùng [ObservableProperty] để tự động sinh ra property public 'Diagnosis'
        // (Viết thường chữ cái đầu)
        [ObservableProperty]
        private string diagnosis;

        // 3. Dùng [ObservableProperty] để tự động sinh ra property public 'DoctorNotes'
        [ObservableProperty]
        private string doctorNotes;

        // Một đơn thuốc có nhiều loại thuốc
        public List<MedicationItem> Medications { get; set; } = new List<MedicationItem>();

        public decimal TotalAmount => Medications?.Sum(m => m.Price) ?? 0;
    }
}
