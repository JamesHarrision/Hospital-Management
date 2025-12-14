using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HosipitalManager.MVVM.Models;
using HosipitalManager.MVVM.Messages;
using HosipitalManager.MVVM.ViewModels;
using HospitalManager.MVVM.Models;
using HosipitalManager.MVVM.Enums;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using Colors = QuestPDF.Helpers.Colors;
using IContainer = QuestPDF.Infrastructure.IContainer;

namespace HospitalManager.MVVM.ViewModels;

public partial class DashboardViewModel
{
    #region Pagination Properties
    [ObservableProperty]
    private int presCurrentPage = 1;
    [ObservableProperty]
    private int presTotalPages = 1;
    [ObservableProperty]
    private string presPageInfo;
    [ObservableProperty]
    private bool canPresGoBack;
    [ObservableProperty]
    private bool canPresGoNext;
    #endregion

    #region Prescription Properties
    private List<Prescription> _allPrescriptions = new List<Prescription>();

    [ObservableProperty]
    private bool canIssuePrescription;
    [ObservableProperty]
    private bool isIssueButtonVisible;

    [ObservableProperty]
    private ObservableCollection<Prescription> prescriptions;

    [ObservableProperty]
    private string searchPrescriptionText;

    // Đơn thuốc đang chọn xem chi tiết
    [ObservableProperty]
    private Prescription selectedPrescription;

    // Biến bật/tắt popup xem chi tiết
    [ObservableProperty]
    private bool isPrescriptionDetailVisible;

    // Biến bật/tắt popup thêm thủ công
    [ObservableProperty]
    private bool isAddPrescriptionPopupVisible;
    [ObservableProperty]
    private string newPrescriptionPatientName;
    [ObservableProperty]
    private string newPrescriptionDoctorName;
    #endregion

    #region Property Changed Handlers
    partial void OnSearchPrescriptionTextChanged(string value)
    {
        Task.Run(async () => await SearchPrescriptions(value));
    }
    #endregion

    #region Data Loading Methods
    /// <summary>
    /// Load danh sách prescriptions với phân trang
    /// </summary>
    private async Task LoadPrescriptions()
    {
        // Đảm bảo Collection đã được khởi tạo
        if (Prescriptions == null) Prescriptions = new ObservableCollection<Prescription>();

        int totalCount = await _databaseService.GetPrescriptionCountAsync();
        PresTotalPages = (int)Math.Ceiling((double)totalCount / PageSize);

        if (PresCurrentPage < 1) PresCurrentPage = 1;
        if (PresCurrentPage > PresTotalPages && PresTotalPages > 0) PresCurrentPage = PresTotalPages;

        var presList = await _databaseService.GetPrescriptionsPagedAsync(PresCurrentPage, PageSize);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Prescriptions.Clear();
            foreach (var p in presList)
            {
                // --- SỬA LỖI QUAN TRỌNG TẠI ĐÂY ---
                // Phải giải nén chuỗi JSON thành danh sách thuốc để UI hiển thị
                p.DeserializeMedicines();

                Prescriptions.Add(p);
            }

            // Lưu danh sách gốc để dùng cho tính năng tìm kiếm
            _allPrescriptions = presList;

            PresPageInfo = $"Trang {PresCurrentPage} / {PresTotalPages}";
            CanPresGoBack = PresCurrentPage > 1;
            CanPresGoNext = PresCurrentPage < PresTotalPages;
        });

        // Gửi tất cả prescriptions tới RevenueViewModel để tính doanh thu
        var allPrescriptions = await _databaseService.GetPrescriptionsAsync();
        // Cũng cần deserialize cho danh sách này nếu bên Revenue cần chi tiết thuốc
        foreach (var p in allPrescriptions) p.DeserializeMedicines();

        WeakReferenceMessenger.Default.Send(new PrescriptionsLoadedMessage(allPrescriptions));
    }

    /// <summary>
    /// Tìm kiếm prescriptions theo từ khóa
    /// </summary>
    private async Task SearchPrescriptions(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            await LoadPrescriptions(); // Gọi lại hàm load phân trang
            return;
        }

        // Tìm trong toàn bộ Database
        var searchResults = await _databaseService.SearchPrescriptionAsync(keyword);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Prescriptions.Clear();
            foreach (var p in searchResults)
            {
                // --- SỬA LỖI TƯƠNG TỰ CHO TÌM KIẾM ---
                p.DeserializeMedicines();
                Prescriptions.Add(p);
            }

            // Cập nhật UI phân trang để báo hiệu đang ở chế độ tìm kiếm
            PresPageInfo = $"Tìm thấy: {searchResults.Count} kết quả";

            // Vô hiệu hóa nút Next/Prev vì đang hiện tất cả kết quả
            CanPresGoBack = false;
            CanPresGoNext = false;
        });
    }
    #endregion

    #region Pagination Commands
    [RelayCommand]
    private async Task NextPresPage()
    {
        if (PresCurrentPage < PresTotalPages)
        {
            PresCurrentPage++;
            await LoadPrescriptions();
        }
    }

    [RelayCommand]
    private async Task PreviousPresPage()
    {
        if (PresCurrentPage > 1)
        {
            PresCurrentPage--;
            await LoadPrescriptions();
        }
    }
    #endregion

    #region Prescription Management Commands
    /// <summary>
    /// Xóa đơn thuốc
    /// </summary>
    [RelayCommand]
    private async Task DeletePrescription(Prescription prescriptionToDelete)
    {
        if (prescriptionToDelete == null) return;

        // Hỏi xác nhận trước khi xóa
        bool confirmed = await Application.Current.MainPage.DisplayAlert(
            "Xác nhận xóa",
            $"Bạn có chắc chắn muốn xóa đơn thuốc mã '{prescriptionToDelete.Id}' của bệnh nhân {prescriptionToDelete.PatientName}?",
            "Xóa",
            "Hủy");

        if (confirmed)
        {
            // Xóa trong Database
            await _databaseService.DeletePrescriptionAsync(prescriptionToDelete);

            // Xóa trên Giao diện
            Prescriptions.Remove(prescriptionToDelete);
        }
    }

    /// <summary>
    /// Cấp phát đơn thuốc và thu tiền
    /// </summary>
    [RelayCommand]
    private async Task IssuePrescription()
    {
        if (SelectedPrescription == null) return;

        // Hỏi xác nhận
        bool confirm = await Shell.Current.DisplayAlert("Thu Ngân",
            $"Xác nhận thu: {SelectedPrescription.TotalAmount:N0} đ\nvà cấp thuốc cho bệnh nhân?",
            "Thu tiền & Cấp", "Hủy");

        if (!confirm) return;

        // 1. Cập nhật Status
        SelectedPrescription.Status = PrescriptionStatus.Issued;

        // 2. Lưu vào Database (Phải Serialize lại trước khi lưu để đảm bảo JSON mới nhất)
        SelectedPrescription.SerializeMedicines();
        await _databaseService.UpdatePrescriptionAsync(SelectedPrescription);

        // 3. Ẩn nút cấp phát
        IsIssueButtonVisible = false;

        // 4. Gửi tin nhắn cập nhật doanh thu
        WeakReferenceMessenger.Default.Send(new RevenueUpdateMessage(SelectedPrescription.TotalAmount, DateTime.Now));

        // 5. Reload lại danh sách
        await LoadPrescriptions();

        await Shell.Current.DisplayAlert("Thành công", "Đã cập nhật trạng thái và doanh thu!", "OK");
    }

    /// <summary>
    /// Hiển thị chi tiết đơn thuốc
    /// </summary>
    [RelayCommand]
    private void ShowPrescriptionDetail(Prescription prescription)
    {
        if (prescription == null) return;

        // Đảm bảo dữ liệu thuốc đã được bung ra (dù đã gọi ở Load, gọi lại cho chắc chắn)
        prescription.DeserializeMedicines();

        SelectedPrescription = prescription;

        // Chỉ hiện nút cấp phát khi chưa cấp
        IsIssueButtonVisible = SelectedPrescription.Status == PrescriptionStatus.Pending;

        IsPrescriptionDetailVisible = true;
    }

    /// <summary>
    /// Đóng popup chi tiết
    /// </summary>
    [RelayCommand]
    private void ClosePrescriptionDetail()
    {
        IsPrescriptionDetailVisible = false;
        SelectedPrescription = null;
    }
    #endregion

    #region Manual Add Prescription (Legacy)
    /// <summary>
    /// Hiển thị popup thêm đơn thuốc thủ công
    /// </summary>
    [RelayCommand]
    private void ShowAddPrescriptionPopup()
    {
        IsAddPrescriptionPopupVisible = true;
    }

    /// <summary>
    /// Đóng popup thêm đơn thuốc
    /// </summary>
    [RelayCommand]
    private void CloseAddPrescriptionPopup()
    {
        IsAddPrescriptionPopupVisible = false;
        NewPrescriptionPatientName = string.Empty;
        NewPrescriptionDoctorName = string.Empty;
    }

    /// <summary>
    /// Lưu đơn thuốc mới (thêm thủ công)
    /// </summary>
    [RelayCommand]
    private void SavePrescription()
    {
        var newPrescription = new Prescription
        {
            Id = $"DT{new Random().Next(100, 999)}",
            PatientName = NewPrescriptionPatientName,
            DoctorName = NewPrescriptionDoctorName,
            DatePrescribed = DateTime.Now,
            Status = PrescriptionStatus.Pending
        };
        // Cần lưu xuống DB luôn nếu muốn đồng bộ
        // await _databaseService.SavePrescriptionAsync(newPrescription);

        _allPrescriptions.Add(newPrescription);
        Prescriptions.Add(newPrescription);
        CloseAddPrescriptionPopup();
    }
    #endregion

    #region Print Prescription
    /// <summary>
    /// In đơn thuốc ra PDF
    /// </summary>
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

                    // HEADER
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("BỆNH VIỆN UIT").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                            col.Item().Text("Địa chỉ: Khu phố 34, Phường Linh Xuân, Thành phố Hồ Chí Minh.").FontSize(10);
                        });
                    });

                    // CONTENT
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

                        // Bảng thuốc
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

                            // Footer bảng
                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(4).Element(FooterCellStyle).AlignRight().Text("TỔNG CỘNG:").Bold();
                                footer.Cell().Element(FooterCellStyle).AlignRight().Text($"{SelectedPrescription.TotalAmount:N0} VNĐ").Bold().FontColor(Colors.Red.Medium);

                                static IContainer FooterCellStyle(IContainer container) =>
                                    container.PaddingVertical(5).BorderTop(1).BorderColor(Colors.Black);
                            });
                        });

                        col.Item().Height(20);
                        col.Item().Text($"Ghi chú: {SelectedPrescription.DoctorNotes}").Italic();
                    });

                    // FOOTER
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
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Lỗi", "Không thể in đơn thuốc: " + ex.Message, "OK");
        }
    }
    #endregion
}