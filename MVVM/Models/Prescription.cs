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

        // --- SỬA ĐỔI QUAN TRỌNG ---
        // Chuyển Status thành ObservableProperty để UI tự động cập nhật khi đổi giá trị
        private string _status;
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value); // Hàm này giúp giao diện tự cập nhật
        }

        [ObservableProperty]
        private string diagnosis;

        [ObservableProperty]
        private string doctorNotes;

        public List<MedicationItem> Medications { get; set; } = new List<MedicationItem>();

        // --- SỬA ĐỔI QUAN TRỌNG ---
        // Tổng tiền = Giá * Số lượng
        public decimal TotalAmount => Medications?.Sum(m => m.Price * m.Quantity) ?? 0;
    }
}
