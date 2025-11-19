using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HosipitalManager.Services
{
    public class DbService
    {
        private readonly string _connectionString =
                "Server=localhost\\SQLEXPRESS;Database=HospitalManager;Trusted_Connection=True;TrustServerCertificate=True;";
        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
