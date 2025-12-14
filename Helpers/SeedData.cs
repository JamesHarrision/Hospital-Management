using HosipitalManager.Helpers.Seeding;
using HosipitalManager.MVVM.Models;
using HospitalManager.MVVM.Models; // Chứa Patient, Prescription, Appointment
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HosipitalManager.Helpers
{
    public static class SeedData
    {
        public static async Task EnsurePopulated(SQLiteAsyncConnection database)
        {
            try
            {
                // ==========================================================
                // 1. SEED PATIENTS (BỆNH NHÂN)
                // ==========================================================
                var patientCount = await database.Table<Patient>().CountAsync();

                // Biến lưu danh sách để dùng cho các bước sau
                List<Patient> patients = new List<Patient>();

                if (patientCount == 0)
                {
                    // Tạo dữ liệu giả
                    patients = PatientSeeder.GeneratePatients();

                    // Insert NHANH bằng InsertAllAsync (Thay vì vòng lặp)
                    await database.InsertAllAsync(patients);

                    System.Diagnostics.Debug.WriteLine($"--- Đã Seed {patients.Count} Bệnh nhân ---");
                }
                else
                {
                    // Nếu đã có DB, lấy danh sách ra để dùng tạo đơn thuốc/lịch hẹn
                    patients = await database.Table<Patient>().ToListAsync();
                }

                // ==========================================================
                // 2. SEED PRESCRIPTIONS (ĐƠN THUỐC)
                // ==========================================================
                var prescriptionCount = await database.Table<Prescription>().CountAsync();

                if (prescriptionCount == 0 && patients.Any())
                {
                    // Tạo đơn thuốc dựa trên danh sách bệnh nhân đã có (để khớp ID)
                    var prescriptions = PrescriptionSeeder.GeneratePrescriptions(patients);

                    await database.InsertAllAsync(prescriptions);

                    System.Diagnostics.Debug.WriteLine($"--- Đã Seed {prescriptions.Count} Đơn thuốc ---");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($" LỖI CRITICAL KHI SEED DATA: {ex.Message}");
                // In StackTrace để dễ debug
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            }
        }
    }
}