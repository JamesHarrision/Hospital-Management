using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System.Text.Json; 
using System.Collections.ObjectModel;
using HosipitalManager.MVVM.Models;

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
        public string Status { get; set; }

        // 2. Các property tự động sinh code (ObservableProperty)
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