using HosipitalManager.Helpers.Seeding;
using HospitalManager.MVVM.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HosipitalManager.Helpers
{
    public static class SeedData
    {
        public static async Task EnsurePopulated(SQLiteAsyncConnection database)
        {
            try
            {

                var patientCount = await database.Table<Patient>().CountAsync();

                if (patientCount == 0)
                {
                    // 1. Lấy dữ liệu mẫu
                    var patients = PatientSeeder.GeneratePatients();
                    var prescriptions = PrescriptionSeeder.GeneratePrescriptions(patients);

                    // 2. Dùng vòng lặp để Insert từng người một
                    // (Thay vì InsertOrReplaceAllAsync không có)

                    foreach (var p in patients)
                    {
                        await database.InsertOrReplaceAsync(p);
                    }

                    foreach (var pre in prescriptions)
                    {
                        await database.InsertOrReplaceAsync(pre);
                    }

                    System.Diagnostics.Debug.WriteLine("--- ĐÃ SEEDING XONG (Dùng vòng lặp)! ---");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi Seeding: {ex.Message}");
            }
        }
    }
}
