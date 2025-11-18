using CommunityToolkit.Mvvm.ComponentModel;
using HosipitalManager.MVVM.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace HosipitalManager.MVVM.ViewModels
{
    public partial class PrescriptionViewModel : ObservableObject
    {
        // Danh sách đơn thuốc
        [ObservableProperty]
        private ObservableCollection<Prescription> prescriptions;

        // Đơn thuốc đang được chọn để xem chi tiết
        [ObservableProperty]
        private Prescription selectedPrescription;

        // Bật/tắt popup chi tiết
        [ObservableProperty]
        private bool isPrescriptionDetailVisible;

        public ICommand ShowAddPrescriptionPopupCommand { get; }  
        public ICommand ShowPrescriptionDetailCommand { get; }
        public ICommand ClosePrescriptionDetailCommand { get; }
        public PrescriptionViewModel()
        {
            ShowPrescriptionDetailCommand = new Command<Prescription>(OnShowDetail);
            ClosePrescriptionDetailCommand = new Command(OnCloseDetail);

            LoadFakeData();
        }

        void OnCloseDetail()
        {
            IsPrescriptionDetailVisible = false;
        }
        private void LoadFakeData()
        {
            Prescriptions = new ObservableCollection<Prescription>
            {
                new Prescription
                {
                    Id = "DT0001",
                    PatientId = "BN001",
                    PatientName = "Trần Thị B",
                    DoctorId = "BS001",
                    DoctorName = "BS. Nguyễn Văn A",
                    DatePrescribed = DateTime.Today,
                    Status = "Đã cấp",
                    Medications = 
                    {
                        new MedicationItem
                        {
                            MedicineName = "Paracetamol 500mg",
                            Dosage = "1 viên",
                            Usage = "Uống 3 lần/ngày",
                            Days = 5,
                            Quantity = 15,
                            Note = "Uống sau ăn khi đau/sốt"
                        },
                        new MedicationItem
                        {
                            MedicineName = "Amoxicillin 500mg",
                            Dosage = "1 viên",
                            Usage = "Uống 2 lần/ngày",
                            Days = 7,
                            Quantity = 14,
                            Note = "Uống đủ liều, không tự ý ngưng"
                        }
                    }
                },

                new Prescription
                {
                    Id = "DT0002",
                    PatientId = "BN002",
                    PatientName = "Nguyễn Văn D",
                    DoctorId = "BS002",
                    DoctorName = "BS. Lê Văn C",
                    DatePrescribed = DateTime.Today.AddDays(-1),
                    Status = "Chưa cấp",
                    Medications =
                    {
                        new MedicationItem
                        {
                            MedicineName = "Omeprazole 20mg",
                            Dosage = "1 viên",
                            Usage = "Uống sáng trước ăn",
                            Days = 14,
                            Quantity = 14,
                            Note = "Không tự ý bỏ thuốc"
                        }
                    }
                }
            };
        }

        private void OnShowDetail(Prescription prescription)
        {
            SelectedPrescription = prescription;
            IsPrescriptionDetailVisible = true;
        }
    }
}
