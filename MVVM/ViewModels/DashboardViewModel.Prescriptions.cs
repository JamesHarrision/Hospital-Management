using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HosipitalManager.MVVM.Models;
using System.Collections.ObjectModel;

namespace HosipitalManager.MVVM.ViewModels;

public partial class DashboardViewModel
{
    [ObservableProperty]
    private ObservableCollection<Prescription> prescriptions;

    [ObservableProperty]
    private bool isAddPrescriptionPopupVisible;

    [ObservableProperty]
    private string newPrescriptionPatientName;
    [ObservableProperty]
    private string newPrescriptionDoctorName;

    private void LoadPrescriptions()
    {
        Prescriptions.Add(new Prescription
        {
            Id = "DT001",
            PatientName = "Nguyễn Văn An",
            DoctorName = "BS. Trần Thị B",
            DatePrescribed = new DateTime(2025, 11, 15),
            Status = "Đã cấp"
        });
    }

    //[RelayCommand]
    //private void ShowAddPrescriptionPopup()
    //{
    //    IsAddPrescriptionPopupVisible = true;
    //}

    //[RelayCommand]
    //private void CloseAddPrescriptionPopup()
    //{
    //    IsAddPrescriptionPopupVisible = false;
    //    NewPrescriptionPatientName = string.Empty;
    //    NewPrescriptionDoctorName = string.Empty;
    //}

    //[RelayCommand]
    //private void SavePrescription()
    //{
    //    var newPrescription = new Prescription
    //    {
    //        Id = $"DT{new Random().Next(100, 999)}",
    //        PatientName = NewPrescriptionPatientName,
    //        DoctorName = NewPrescriptionDoctorName,
    //        DatePrescribed = DateTime.Now,
    //        Status = "Chưa cấp"
    //    };
    //    Prescriptions.Add(newPrescription);
    //    CloseAddPrescriptionPopup();
    //}
}