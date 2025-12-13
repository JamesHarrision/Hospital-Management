using CommunityToolkit.Mvvm.ComponentModel;
using HosipitalManager.MVVM.Models;
using HospitalManager.MVVM.Models;
using System.Collections.ObjectModel;
using Microsoft.Maui.ApplicationModel;
using HosipitalManager.MVVM.Enums;

namespace HosipitalManager.MVVM.Services
{
    public class PrescriptionService
    {
        // Tham chiếu đến danh sách đơn thuốc của Dashboard
        private readonly ObservableCollection<Prescription> _targetCollection;

        public PrescriptionService(ObservableCollection<Prescription> targetCollection)
        {
            _targetCollection = targetCollection;
        }

        public void CreateAndSavePrescription(
            Patient patient,
            string diagnosis,
            string doctorNotes,
            IEnumerable<MedicationItem> medications)
        {
            // 1. Tạo đơn thuốc mới
            var newPrescription = new Prescription
            {
                Id = $"DT{DateTime.Now.Ticks.ToString().Substring(12)}",
                PatientId = patient.Id,
                PatientName = patient.FullName,
                DoctorId = "BS001",
                DoctorName =  $"BS. {patient.Doctorname}",
                DatePrescribed = DateTime.Now,
                Status = PrescriptionStatus.Pending,

                // Lưu kết quả khám
                Diagnosis = diagnosis,
                DoctorNotes = doctorNotes,

                // Lưu danh sách thuốc
                Medications = new ObservableCollection<MedicationItem>(medications)
            };

            // 2. QUAN TRỌNG: Thêm vào danh sách TRÊN MAIN THREAD
            // Điều này đảm bảo giao diện Tab Đơn thuốc nhận được tín hiệu cập nhật ngay lập tức
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _targetCollection.Add(newPrescription);
            });
        }
    }
}
