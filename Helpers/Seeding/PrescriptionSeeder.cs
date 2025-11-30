using HosipitalManager.MVVM.Models;
using HospitalManager.MVVM.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HosipitalManager.Helpers.Seeding
{
    public static class PrescriptionSeeder
    {
        private static readonly (string Diagnosis, string Medicine)[] PrescriptionsData = new[]
        {
            ("Viêm họng cấp", "Paracetamol 500mg"),
            ("Cao huyết áp", "Amlodipin 5mg"),
            ("Rối loạn tiêu hóa", "Berberin"),
            ("Viêm phế quản", "Kháng sinh Amoxicillin"),
            ("Sốt siêu vi", "Vitamin C + Panadol"),
            ("Đau dạ dày", "Omeprazol 20mg"),
            ("Dị ứng thời tiết", "Loratadin 10mg")
        };

        private static readonly string[] Doctors = new[]
        {
            "BS. Mai Trọng Khang",
            "BS. Nguyễn Ngọc Quý"
        };

        public static List<Prescription> GeneratePrescriptions(List<Patient> patients)
        {
            var list = new List<Prescription>();
            int index = 0;

            foreach (var patient in patients)
            {
                // QUAN TRỌNG: Nếu bệnh nhân đang chờ khám thì KHÔNG tạo đơn thuốc
                if (patient.Status == "Chờ khám")
                    continue;

                // Tạo đơn thuốc cho người đã khám
                var data = PrescriptionsData[index % PrescriptionsData.Length];
                var currentDoctor = Doctors[index % Doctors.Length];

                var prescription = new Prescription
                {
                    Id = $"DT{1000 + index}",
                    PatientId = patient.Id,
                    PatientName = patient.FullName,
                    DoctorName = currentDoctor,

                    // Ngày kê đơn phải trùng hoặc sau ngày nhập viện
                    DatePrescribed = patient.AdmittedDate.AddHours(2),

                    // Trạng thái đơn thuốc: Đã điều trị xong thì chắc chắn đã cấp thuốc
                    Status = (patient.Status == "Hoàn thành điều trị") ? "Đã cấp" : "Chưa cấp",

                    Diagnosis = data.Diagnosis,
                    DoctorNotes = $"Điều trị tích cực. Tái khám khi cần.",

                    Medications = new ObservableCollection<MedicationItem>
                    {
                        new MedicationItem
                        {
                            MedicationName = data.Medicine,
                            Quantity = 10,
                            Unit = "Viên",
                            Price = 5000,
                            Dosage = "Sáng 1, Tối 1"
                        }
                    }
                };

                prescription.CalculateTotal();
                prescription.SerializeMedicines();

                list.Add(prescription);
                index++;
            }

            return list;
        }
    }
}
