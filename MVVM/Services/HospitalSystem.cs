using HosipitalManager.MVVM.Models;
using System.Collections.ObjectModel;

namespace HosipitalManager.MVVM.Services
{
    public class HospitalSystem
    {
        // Singleton pattern để dữ liệu tồn tại xuyên suốt ứng dụng chạy
        private static HospitalSystem _instance;
        public static HospitalSystem Instance => _instance ??= new HospitalSystem();

        public ObservableCollection<Doctor> Doctors { get; private set; }

        private HospitalSystem()
        {
            InitializeData();
        }

        private void InitializeData()
        {
            // 1. Tạo dữ liệu Bác sĩ giả lập
            Doctors = new ObservableCollection<Doctor>
            {
                new Doctor { Id = "BS001", Name = "BS. Mai Trọng Khang", Specialization = "Thần Kinh", ImageSource = "doctor1.png" },
                new Doctor { Id = "BS002", Name = "BS. Nguyễn Ngọc Quý", Specialization = "Nha Khoa", ImageSource = "doctor2.png" },
            };

            // 2. Tạo dữ liệu Lịch hẹn giả lập
            
        }
    }
}