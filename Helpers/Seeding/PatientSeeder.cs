using HospitalManager.MVVM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HosipitalManager.Helpers.Seeding
{
    public static class PatientSeeder
    {
        private static readonly (string Name, string Gender)[] PatientData = new[]
                {
            ("Nguyễn Văn An", "Nam"), ("Trần Thị Bích", "Nữ"), ("Lê Văn Cường", "Nam"), ("Phạm Thị Dung", "Nữ"), ("Hoàng Văn Em", "Nam"),
            ("Vũ Thị Giang", "Nữ"), ("Đặng Văn Hùng", "Nam"), ("Bùi Thị Hoa", "Nữ"), ("Ngô Văn Khánh", "Nam"), ("Dương Thị Lan", "Nữ"),
            ("Lý Văn Minh", "Nam"), ("Đỗ Thị Ngọc", "Nữ"), ("Hồ Văn Nam", "Nam"), ("Võ Thị Oanh", "Nữ"), ("Phan Văn Phúc", "Nam"),
            ("Trương Thị Quyên", "Nữ"), ("Nguyễn Văn Quân", "Nam"), ("Trần Thị Sương", "Nữ"), ("Lê Văn Sơn", "Nam"), ("Phạm Thị Thu", "Nữ"),
            ("Hoàng Văn Tùng", "Nam"), ("Vũ Thị Uyên", "Nữ"), ("Đặng Văn Vinh", "Nam"), ("Bùi Thị Xuân", "Nữ"), ("Ngô Văn Yên", "Nam"),
            ("Nguyễn Hữu Nghĩa", "Nam"), ("Trần Ngọc Mai", "Nữ"), ("Lê Quốc Bảo", "Nam"), ("Phạm Thanh Hằng", "Nữ"), ("Hoàng Đức Thắng", "Nam"),
            ("Vũ Kim Anh", "Nữ"), ("Đặng Tuấn Kiệt", "Nam"), ("Bùi Phương Thảo", "Nữ"), ("Ngô Minh Hiếu", "Nam"), ("Dương Thúy Vi", "Nữ"),
            ("Lý Trọng Tấn", "Nam"), ("Đỗ Hồng Nhung", "Nữ"), ("Hồ Tấn Tài", "Nam"), ("Võ Mỹ Linh", "Nữ"), ("Phan Thành Đạt", "Nam"),
            ("Trương Bích Trâm", "Nữ"), ("Nguyễn Duy Khang", "Nam"), ("Trần Bảo Ngọc", "Nữ"), ("Lê Hoàng Dũng", "Nam"), ("Phạm Minh Thư", "Nữ"),
            ("Hoàng Anh Tú", "Nam"), ("Vũ Khánh Ly", "Nữ"), ("Đặng Quang Huy", "Nam"), ("Bùi Thanh Trúc", "Nữ"), ("Ngô Kiến Huy", "Nam")
        };

        private static readonly string[] SymptomsList = new[]
        {
            "Sốt cao, đau họng, mệt mỏi trong người.",      // 1. Cảm cúm/Viêm họng
            "Đau bụng âm ỉ, buồn nôn, ăn không tiêu.",      // 2. Tiêu hóa
            "Ho khan kéo dài, tức ngực, khó thở nhẹ.",      // 3. Hô hấp
            "Hoa mắt, chóng mặt, đau đầu dữ dội.",          // 4. Thần kinh/Huyết áp
            "Đau nhức xương khớp, tê bì chân tay, khó ngủ." // 5. Cơ xương khớp
        };

        private static readonly string[] Cities = { "Hà Nội", "TP.HCM", "Đà Nẵng", "Cần Thơ", "Hải Phòng", "Nha Trang", "Huế", "Vũng Tàu", "Đồng Tháp", "Vĩnh Long" };
        private static readonly string[] Severities = { "normal", "medium", "urgent", "critical" };
        private static readonly string[] Doctors = new[]
        {
            "BS. Mai Trọng Khang",
            "BS. Nguyễn Ngọc Quý"
        };
        public static List<Patient> GeneratePatients()
        {
            var list = new List<Patient>();
            int index = 0;
            int total = PatientData.Length;

            foreach (var data in PatientData)
            {
                // LOGIC MỚI: Chỉ định trạng thái dựa trên thứ tự
                string status;

                // Nếu là 5 người cuối cùng -> Cho vào hàng đợi (Chờ khám)
                if (index >= total - 5)
                {
                    status = "Chờ khám";
                }
                else
                {
                    // Những người trước đó: 50% Đang điều trị, 50% Hoàn thành
                    status = (index % 2 == 0) ? "Đang điều trị" : "Hoàn thành điều trị";
                }

                var patient = new Patient
                {
                    Id = $"BN{1000 + index}",
                    FullName = data.Name,
                    Gender = data.Gender,
                    Address = Cities[index % Cities.Length],
                    Status = status,
                    Severity = Severities[index % Severities.Length],
                    AdmittedDate = (status == "Chờ khám") ? DateTime.Now : DateTime.Now.AddDays(-(index % 60 + 1)),

                    DateOfBirth = DateTime.Today.AddYears(-(20 + (index % 40))),
                    PhoneNumber = $"090{index:D7}",
                    QueueOrder = (status == "Chờ khám") ? (index - (total - 5) + 1) : 0, // Chỉ đánh số hàng đợi cho người chờ
                    Symptoms = SymptomsList[index % SymptomsList.Length],
                    Doctorname = Doctors[index % Doctors.Length]
                };

                list.Add(patient);
                index++;
            }

            return list;
        }
    }
}
    

