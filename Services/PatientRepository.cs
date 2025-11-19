using HosipitalManager.MVVM.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.LifecycleEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.AllJoyn;

namespace HosipitalManager.Services
{
    public class PatientRepository
    {
        private readonly DbService _db = new DbService();

        public async Task<List<Patient>> GetAllAsync()
        {
            var result = new List<Patient>();
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(@"
                SELECT PatientID, PatientName, DateOfBirth, Gender, PhoneNumber, PatientAddress,
                       AdmittedDate, Status, Severity, Symptoms, PriorityScore, QueueOrder
                FROM PATIENT
                ORDER BY QueueOrder, PriorityScore DESC;", conn);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var patient = new Patient()
                {
                    Id = reader.GetString(0),
                    FullName = reader.GetString(1),
                    DateOfBirth = reader.IsDBNull(2) ? default : reader.GetDateTime(2),
                    Gender = reader.IsDBNull(3) ? null : reader.GetString(3),
                    PhoneNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Address = reader.IsDBNull(5) ? null : reader.GetString(5),
                    AdmittedDate = reader.IsDBNull(6) ? default : reader.GetDateTime(6),
                    Status = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Severity = reader.IsDBNull(8) ? null : reader.GetString(8),
                    Symptoms = reader.IsDBNull(9) ? null : reader.GetString(9),
                    PriorityScore = reader.IsDBNull(10) ? 0 : reader.GetDouble(10),
                    QueueOrder = reader.IsDBNull(11) ? 0 : reader.GetInt32(11),
                };

                result.Add(patient);
            }
            return result;
        }

        public async Task AddAsync(Patient p)
        {
            using var conn = _db.GetConnection(); //Kết nối tới SQL Server( mới có dây thôi)

            //cmd = 1 câu lệnh sql
            using var cmd = new SqlCommand(@"
                        INSERT INTO PATIENT
                        (PatientID, PatientName, DateOfBirth, Gender, PhoneNumber, PatientAddress,
                         AdmittedDate, Status, Severity, Symptoms, PriorityScore, QueueOrder)
                        VALUES
                        (@PatientID, @PatientName, @DateOfBirth, @Gender, @PhoneNumber, @PatientAddress,
                         @AdmittedDate, @Status, @Severity, @Symptoms, @PriorityScore, @QueueOrder);
                    ", conn);

            cmd.Parameters.AddWithValue("@PatientID", p.Id);
            cmd.Parameters.AddWithValue("@PatientName", p.FullName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DateOfBirth",
                p.DateOfBirth == default ? (object)DBNull.Value : p.DateOfBirth);
            cmd.Parameters.AddWithValue("@Gender", p.Gender ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@PhoneNumber", p.PhoneNumber ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@PatientAddress", p.Address ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@AdmittedDate",
                p.AdmittedDate == default ? DateTime.Now : p.AdmittedDate);
            cmd.Parameters.AddWithValue("@Status", p.Status ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Severity", p.Severity ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Symptoms", p.Symptoms ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@PriorityScore", p.PriorityScore);
            cmd.Parameters.AddWithValue("@QueueOrder", p.QueueOrder);

            await conn.OpenAsync(); //Mở kết nối 
            await cmd.ExecuteNonQueryAsync(); //Thực thi lệnh sql không trả về dữ liệu
        }

        public async Task UpdateAsync(Patient p)
        {
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(@"
                        UPDATE PATIENT 
                        SET 
                            PatientName    = @PatientName,
                            DateOfBirth    = @DateOfBirth,
                            Gender         = @Gender,
                            PhoneNumber    = @PhoneNumber,
                            PatientAddress = @PatientAddress,
                            AdmittedDate   = @AdmittedDate,
                            Status         = @Status,
                            Severity       = @Severity,
                            Symptoms       = @Symptoms,
                            PriorityScore  = @PriorityScore,
                            QueueOrder     = @QueueOrder
                        WHERE PatientID = @PatientID;
                            ", conn);
            cmd.Parameters.AddWithValue("@PatientID", p.Id);
            cmd.Parameters.AddWithValue("@PatientName", p.FullName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DateOfBirth", p.DateOfBirth == default ? (object)DBNull.Value : p.DateOfBirth);
            cmd.Parameters.AddWithValue("@Gender", p.Gender ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@PhoneNumber", p.PhoneNumber ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@PatientAddress", p.Address ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@AdmittedDate", p.AdmittedDate == default ? DateTime.Now : p.AdmittedDate);
            cmd.Parameters.AddWithValue("@Status", p.Status ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Severity", p.Severity ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Symptoms", p.Symptoms ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@PriorityScore", p.PriorityScore);
            cmd.Parameters.AddWithValue("@QueueOrder", p.QueueOrder);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(string patientID)
        {
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand("DELETE FROM PATIENT WHERE PatientID = @PatientID", conn);

            cmd.Parameters.AddWithValue("@PatientID", patientID);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<string> GetNextPatientIDAsync()
        {
            using var conn = _db.GetConnection();
            using var cmd = new SqlCommand(@"
                SELECT MAX(PatientID)
                FROM PATIENT
                WHERE PatientID LIKE 'BN%'", conn);

            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync(); // trả về cái cột đầu tiên

            if(result == DBNull.Value || result == null)
            {
                return "BN001"; 
            }

            string lastID = result.ToString();
            int number = int.Parse(lastID.Substring(2));
            number++;

            return "BN" + number.ToString("D3");
        }
    }
}
