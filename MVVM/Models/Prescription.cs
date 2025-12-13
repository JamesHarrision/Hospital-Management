using CommunityToolkit.Mvvm.ComponentModel;
using HosipitalManager.MVVM.Enums;
using HosipitalManager.MVVM.Models;
using SQLite;
using System.Collections.ObjectModel;
using System.Text.Json; 

namespace HospitalManager.MVVM.Models
{
    public partial class Prescription : ObservableObject
    {
        // 1. Khóa chính cho SQLite
        [PrimaryKey]
        public string Id { get; set; }

        // Khóa ngoại để biết đơn này của ai
        public string PatientId { get; set; }
        public string PatientName { get; set; }
        public string DoctorId { get; set; }
        public string DoctorName { get; set; }
        public DateTime DatePrescribed { get; set; }

        // --- SỬA ĐỔI QUAN TRỌNG ---
        // Chuyển Status thành ObservableProperty để UI tự động cập nhật khi đổi giá trị
        private string _status;
        public PrescriptionStatus Status { get; set; }

        // Property phụ dùng để hiển thị lên màn hình (Binding)
        [Ignore] // Không lưu vào Database, chỉ để hiển thị
        public string StatusDisplay => Status switch
        {
            PrescriptionStatus.Pending => "Chưa cấp",
            PrescriptionStatus.Issued => "Đã cấp",
            PrescriptionStatus.Cancelled => "Đã hủy",
            _ => "Không xác định"
        };

        // Property phụ để đổi màu chữ trạng thái (Binding)
        [Ignore]
        public string StatusColor => Status switch
        {
            PrescriptionStatus.Pending => "#E57373", // Đỏ nhạt
            PrescriptionStatus.Issued => "#81C784",  // Xanh lá
            _ => "#9E9E9E" // Xám
        };

        // 2. Dùng [ObservableProperty] để tự động sinh ra property public 'Diagnosis'
        // (Viết thường chữ cái đầu)
        [ObservableProperty]
        private string diagnosis;

        [ObservableProperty]
        private string doctorNotes;

        // Lưu tổng tiền vào DB luôn để sau này thống kê cho dễ
        [ObservableProperty]
        private decimal totalAmount;


        // --- PHẦN QUAN TRỌNG CHO DATABASE ---

        // 3. DANH SÁCH THUỐC (Dùng cho giao diện)
        // Thêm [Ignore] để SQLite không cố lưu trực tiếp cái List này (vì nó không hỗ trợ lưu List)
        [Ignore]
        public ObservableCollection<MedicationItem> Medications { get; set; } = new();

        // 4. CHUỖI JSON (Dùng để lưu vào Database)
        // SQLite sẽ lưu danh sách thuốc dưới dạng một chuỗi văn bản dài
        public string MedicinesJson { get; set; }

        // 5. Hàm tự động tính tổng tiền (Gọi khi cần cập nhật)
        public void CalculateTotal()
        {
            if (Medications != null)
            {
                TotalAmount = Medications.Sum(m => m.Price * m.Quantity);
            }
        }

        // 6. Hàm ép danh sách thuốc thành chuỗi JSON (Gọi trước khi LƯU vào DB)
        public void SerializeMedicines()
        {
            try
            {
                MedicinesJson = JsonSerializer.Serialize(Medications);
                CalculateTotal(); // Tiện tay tính tổng tiền luôn
            }
            catch (Exception)
            {
                MedicinesJson = "[]";
            }
        }
        //public List<MedicationItem> Medications { get; set; } = new List<MedicationItem>();

        // 7. Hàm bung chuỗi JSON ra thành danh sách (Gọi sau khi LẤY từ DB lên)
        public void DeserializeMedicines()
        {
            try
            {
                if (!string.IsNullOrEmpty(MedicinesJson))
                {
                    var list = JsonSerializer.Deserialize<ObservableCollection<MedicationItem>>(MedicinesJson);
                    Medications = list ?? new ObservableCollection<MedicationItem>();
                }
            }
            catch (Exception)
            {
                Medications = new ObservableCollection<MedicationItem>();
            }
        }
    }
}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     