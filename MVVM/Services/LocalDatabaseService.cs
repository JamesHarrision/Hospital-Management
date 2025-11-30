using HosipitalManager.Helpers;
using HosipitalManager.MVVM.Models;
using HospitalManager.MVVM.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HosipitalManager.MVVM.Services
{
    public class LocalDatabaseService
    {
        private SQLiteAsyncConnection _database;

        async Task Init()
        {
            if (_database != null)
                return;
            // Khởi tạo kết nối SQLite
            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "hospital_data.db3");
            _database = new SQLiteAsyncConnection(databasePath);
            // Tạo bảng nếu chưa tồn tại
            await _database.CreateTableAsync<Patient>();
            await _database.CreateTableAsync<Prescription>();


            // Đảm bảo dữ liệu được thêm vào
            await SeedData.EnsurePopulated(_database);
        }

        /// Lấy danh sách tất cả bệnh nhân
        public async Task<List<Patient>> GetPatientsAsync()
        {
            await Init();
            return await _database.Table<Patient>().ToListAsync();
        }

        // Lấy bệnh nhân theo ID
        public async Task<Patient> GetPatientByIdAsync(string id)
        {
            await Init();
            return await _database.Table<Patient>().Where(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task SavePatientAsync(Patient patient)
        {
            await Init();

            // 1. PHÒNG HỜ: Nếu lỡ bên ViewModel có lỗi làm ID bị rỗng -> Tự tạo ID chữa cháy để không crash app
            if (string.IsNullOrEmpty(patient.Id))
            {
                patient.Id = $"BN{new Random().Next(1000, 9999)}";
            }

            // 2. Kiểm tra tồn tại (Chỉ gọi DB 1 lần cho tối ưu)
            var existingPatient = await GetPatientByIdAsync(patient.Id);

            if (existingPatient != null)
            {
                // Đã có -> Cập nhật thông tin
                await _database.UpdateAsync(patient);
            }
            else
            {
                // Chưa có -> Thêm mới
                await _database.InsertAsync(patient);
            }
        }
        public async Task DeletePatientAsync(Patient patient)
        {
            await Init();
            await _database.DeleteAsync(patient);
        }

        public async Task<List<Prescription>> GetPrescriptionsByPatientIdAsync(string patientId)
        {
            await Init();

            // Lấy list từ DB
            var list = await _database.Table<Prescription>()
                                      .Where(p => p.PatientId == patientId) // Lọc theo ID bệnh nhân
                                      .ToListAsync();

            // Bung chuỗi JSON ra thành List thuốc để dùng
            foreach (var item in list)
            {
                item.DeserializeMedicines();
            }

            return list;
        }
        public async Task SavePrescriptionAsync(Prescription prescription)
        {
            await Init();

            // Ép list thuốc thành chuỗi JSON trước khi lưu
            prescription.SerializeMedicines();

            if (string.IsNullOrEmpty(prescription.Id))
            {
                var allPrescriptions = await _database.Table<Prescription>().ToListAsync();

                int nextNumber = 1000; // Mặc định bắt đầu từ 1000

                if (allPrescriptions.Count > 0)
                {
                    // Tìm mã lớn nhất hiện tại (Lọc những mã bắt đầu bằng "DT")
                    var maxId = allPrescriptions
                        .Select(p => p.Id)
                        .Where(id => id != null && id.StartsWith("DT") && id.Length > 2)
                        .Select(id => int.TryParse(id.Substring(2), out int n) ? n : 0)
                        .Max();

                    nextNumber = maxId + 1;
                }
                prescription.Id = $"DT{nextNumber}";

                // Nếu là đơn mới, gán ngày tạo là hiện tại (nếu chưa có)
                if (prescription.DatePrescribed == default)
                {
                    prescription.DatePrescribed = DateTime.Now;
                }
                var existing = await _database.Table<Prescription>()
                                                  .Where(p => p.Id == prescription.Id)
                                                  .FirstOrDefaultAsync();

                if (existing != null)
                {
                    await _database.UpdateAsync(prescription);
                }
                else
                {
                    await _database.InsertAsync(prescription);
                }
            }
        }
        public async Task<List<Prescription>> GetPrescriptionsAsync()
        {
            await Init();

            // Lấy tất cả đơn thuốc, sắp xếp giảm dần theo ngày (mới nhất lên đầu)
            var list = await _database.Table<Prescription>()
                                      .OrderByDescending(p => p.DatePrescribed)
                                      .ToListAsync();

            // Quan trọng: Phải bung chuỗi JSON ra thành List thuốc
            foreach (var item in list)
            {
                item.DeserializeMedicines();
            }

            return list;
        }
        public async Task DeletePrescriptionAsync(Prescription prescription)
        {
            await Init();
            await _database.DeleteAsync(prescription);
        }


        // --PAGINATION--
        // --- PHẦN BỆNH NHÂN ---
        public async Task<List<Patient>> GetPatientsPagedAsync(int pageIndex, int pageSize)
        {
            await Init();
            return await _database.Table<Patient>()
                          .OrderByDescending(p => p.AdmittedDate) // Sắp xếp mới nhất lên đầu
                          .Skip((pageIndex - 1) * pageSize)       // Bỏ qua các trang trước
                          .Take(pageSize)                         // Lấy số lượng cần thiết
                          .ToListAsync();
        }
        // Đếm tổng số bệnh nhân
        public async Task<int> GetPatientCountAsync()
        {
            await Init();
            return await _database.Table<Patient>().CountAsync();
        }

        // --- PHẦN ĐƠN THUỐC ---
        public async Task<List<Prescription>> GetPrescriptionsPagedAsync(int pageIndex, int pageSize)
        {
            await Init();
            var list = await _database.Table<Prescription>()
                                      .OrderByDescending(p => p.DatePrescribed)
                                      .Skip((pageIndex - 1) * pageSize)
                                      .Take(pageSize)
                                      .ToListAsync();

            // Bung JSON thuốc ra
            foreach (var item in list) item.DeserializeMedicines();

            return list;
        }
        public async Task<int> GetPrescriptionCountAsync()
        {
            await Init();
            return await _database.Table<Prescription>().CountAsync();
        }
    }
}
