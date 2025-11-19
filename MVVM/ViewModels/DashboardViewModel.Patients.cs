using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HosipitalManager.MVVM.Models;
using System.Collections.ObjectModel;

namespace HosipitalManager.MVVM.ViewModels;

public partial class DashboardViewModel
{

    // Hàm nạp dữ liệu mẫu (nếu cần)
    private void LoadSamplePatients()
    {
        Patients.Add(new Patient
        {
            Id = "BN001",
            FullName = "Nguyễn Văn An",
            DateOfBirth = new DateTime(1990, 5, 15),
            Gender = "Nam",
            PhoneNumber = "0901234567",
            Address = "123 Đường ABC...",
            AdmittedDate = DateTime.Today.AddDays(-5),
            Status = "Đang điều trị",
            Severity = "normal"
        });
        // ... thêm các mẫu khác nếu muốn
    }
}