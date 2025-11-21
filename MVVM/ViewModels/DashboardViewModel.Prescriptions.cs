using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HosipitalManager.MVVM.Models;
using System.Collections.ObjectModel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using Colors = QuestPDF.Helpers.Colors;
using IContainer = QuestPDF.Infrastructure.IContainer;

namespace HospitalManager.MVVM.ViewModels;

public partial class DashboardViewModel
{
    // Danh sách đơn thuốc (Chính chủ)
    [ObservableProperty]
    private ObservableCollection<Prescription> prescriptions;

    // Đơn thuốc đang chọn xem chi tiết
    [ObservableProperty]
    private Prescription selectedPrescription;

    // Biến bật/tắt popup xem chi tiết
    [ObservableProperty]
    private bool isPrescriptionDetailVisible;

    // Biến bật/tắt popup thêm thủ công (giữ lại nếu cần)
    [ObservableProperty]
    private bool isAddPrescriptionPopupVisible;
    [ObservableProperty]
    private string newPrescriptionPatientName;
    [ObservableProperty]
    private string newPrescriptionDoctorName;

    // --- CÁC HÀM LOAD DỮ LIỆU ---
    private void LoadPrescriptions()
    {
        // Dữ liệu mẫu ban đầu
        Prescriptions.Add(new Prescription
        {
            Id = "DT001",
            PatientName = "Nguyễn Văn An",
            DoctorName = "BS. Trần Thị B",
            DatePrescribed = new DateTime(2025, 11, 15),
            Status = "Đã cấp"
        });
    }

    // --- CÁC COMMAND XỬ LÝ ---

    // 1. Xem chi tiết đơn thuốc
    [RelayCommand]
    private void ShowPrescriptionDetail(Prescription prescription)
    {
        if (prescription == null) return;
        SelectedPrescription = prescription;
        IsPrescriptionDetailVisible = true;
    }

    [RelayCommand]
    private void ClosePrescriptionDetail()
    {
        IsPrescriptionDetailVisible = false;
        SelectedPrescription = null;
    }

    // 2. Thêm đơn thuốc thủ công (Popup cũ)
    [RelayCommand]
    private void ShowAddPrescriptionPopup()
    {
        IsAddPrescriptionPopupVisible = true;
    }

    [RelayCommand]
    private void CloseAddPrescriptionPopup()
    {
        IsAddPrescriptionPopupVisible = false;
        NewPrescriptionPatientName = string.Empty;
        NewPrescriptionDoctorName = string.Empty;
    }

    [RelayCommand]
    private void SavePrescription()
    {
        var newPrescription = new Prescription
        {
            Id = $"DT{new Random().Next(100, 999)}",
            PatientName = NewPrescriptionPatientName,
            DoctorName = NewPrescriptionDoctorName,
            DatePrescribed = DateTime.Now,
            Status = "Chưa cấp"
        };
        Prescriptions.Add(newPrescription);
        CloseAddPrescriptionPopup();
    }

    [RelayCommand]
    private async Task PrintPrescription()
    {
        if (SelectedPrescription == null)
            return;
        try
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(5, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(13).FontFamily("Times New Roman").LineHeight(1.5f));

                    // HEADER //
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("BỆNH VIỆN UIT").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                            col.Item().Text("Địa chỉ: Khu phố 34, Phường Linh Xuân, Thành phố Hồ Chí Minh.").FontSize(10);
                        });
                    });

                    // CONTENT //
                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Item().AlignCenter().Text("ĐƠN THUỐC").FontSize(24).Bold();
                        col.Item().Height(20);

                        // Thông tin Bệnh nhân
                        col.Item().Text($"Mã đơn: {SelectedPrescription.Id}");
                        col.Item().Text($"Bệnh nhân: {SelectedPrescription.PatientName}");
                        col.Item().Text($"Bác sĩ: {SelectedPrescription.DoctorName}");
                        col.Item().Text($"Ngày kê: {SelectedPrescription.DatePrescribed:dd/MM/yyyy}");
                        col.Item().Height(20);

                        //Kẻ bảng thuốc
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30); // STT
                                columns.RelativeColumn(4);  // Tên
                                columns.RelativeColumn(2);  // Liều
                                columns.RelativeColumn(1);  // SL
                                columns.RelativeColumn(3);  // Thành tiền
                            });

                            // Header bảng
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("STT");
                                header.Cell().Element(CellStyle).Text("Tên thuốc");
                                header.Cell().Element(CellStyle).Text("Liều dùng");
                                header.Cell().Element(CellStyle).Text("SL");
                                header.Cell().Element(CellStyle).AlignRight().Text("Thành tiền");

                                static IContainer CellStyle(IContainer container) =>
                                    container.DefaultTextStyle(x => x.SemiBold()).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                            });

                            // Nội dung bảng
                            int stt = 1;
                            // Giả sử SelectedPrescription có list Medications (nếu chưa có thì bạn cần thêm vào Model)
                            if (SelectedPrescription.Medications != null)
                            {
                                foreach (var med in SelectedPrescription.Medications)
                                {
                                    table.Cell().Element(CellStyle).Text(stt++.ToString());
                                    table.Cell().Element(CellStyle).Text(med.MedicationName);
                                    table.Cell().Element(CellStyle).Text(med.Dosage);
                                    table.Cell().Element(CellStyle).Text(med.Quantity.ToString());
                                    table.Cell().Element(CellStyle).AlignRight().Text($"{med.Price:N0} đ");

                                    static IContainer CellStyle(IContainer container) =>
                        container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                                }
                            }

                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(4).Element(CellStyle).AlignRight().Text("TỔNG CỘNG:").Bold();
                                footer.Cell().Element(CellStyle).AlignRight().Text($"{SelectedPrescription.TotalAmount:N0} VNĐ").Bold().FontColor(Colors.Red.Medium);

                                static IContainer CellStyle(IContainer container) =>
                                    container.PaddingVertical(5).BorderTop(1).BorderColor(Colors.Black);
                            });

                            static IContainer FooterStyle(IContainer container) =>
                    container.PaddingVertical(5).BorderTop(1).BorderColor(Colors.Black);
                        });

                        col.Item().Height(20);
                        col.Item().Text($"Ghi chú: {SelectedPrescription.DoctorNotes}").Italic();
                    });

                    // FOOTER // 
                    page.Footer().AlignRight().Column(col =>
                    {
                        col.Item().Text($"Ngày {DateTime.Now.Day} tháng {DateTime.Now.Month} năm {DateTime.Now.Year}");
                        col.Item().Text("Bác sĩ điều trị").Bold();
                        col.Item().Height(60);
                        col.Item().Text(SelectedPrescription.DoctorName).Bold();
                    });
                });        
            });

            // Lưu và mở file
            string fileName = $"DonThuoc_{SelectedPrescription.Id}.pdf";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            document.GeneratePdf(filePath);

            await Launcher.Default.OpenAsync(new OpenFileRequest("In Đơn Thuốc", new ReadOnlyFile(filePath)));
        }
        catch(Exception ex)
        {
            await Shell.Current.DisplayAlert("Lỗi", "Không thể in đơn thuốc: " + ex.Message, "OK");
        }

    }
}