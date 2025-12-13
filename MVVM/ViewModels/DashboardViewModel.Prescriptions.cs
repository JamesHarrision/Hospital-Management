using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HosipitalManager.MVVM.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.ObjectModel;
using Colors = QuestPDF.Helpers.Colors;
using IContainer = QuestPDF.Infrastructure.IContainer;
using CommunityToolkit.Mvvm.Messaging; 
using HosipitalManager.MVVM.Messages;
using HosipitalManager.MVVM.ViewModels;
using HospitalManager.MVVM.Models;
using System.Threading.Tasks;

namespace HospitalManager.MVVM.ViewModels;

public partial class DashboardViewModel
{
    // PAGINATION // 
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
    // END PAGINATION //

    private List<Prescription> _allPrescriptions = new List<Prescription>();
    // Danh sách đơn thuốc (Chính chủ)

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

    // Biến bật/tắt popup thêm thủ công (giữ lại nếu cần)
    [ObservableProperty]
    private bool isAddPrescriptionPopupVisible;
    [ObservableProperty]
    private string newPrescriptionPatientName;
    [ObservableProperty]
    private string newPrescriptionDoctorName;

    partial void OnSearchPrescriptionTextChanged(string value)
    {
        SearchPrescriptions();
    }

    // --- CÁC HÀM LOAD DỮ LIỆU ---
    private async Task LoadPrescriptions()
    {
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
                Prescriptions.Add(p);
            }

            // Lưu danh sách gốc để dùng cho tính năng tìm kiếm (nếu có)
            _allPrescriptions = presList;

            PresPageInfo = $"Trang {PresCurrentPage} / {PresTotalPages}";
            CanPresGoBack = PresCurrentPage > 1;
            CanPresGoNext = PresCurrentPage < PresTotalPages;
        });

        // Gửi tất cả prescriptions tới RevenueViewModel để tính doanh thu (chạy không đồng bộ)
        // Lấy toàn bộ danh sách từ DB (không chỉ trang hiện tại)
        var allPrescriptions = await _databaseService.GetPrescriptionsAsync();
        WeakReferenceMessenger.Default.Send(new PrescriptionsLoadedMessage(allPrescriptions));
    }

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

    [RelayCommand]
    private async Task DeletePrescription(Prescription prescriptionToDelete)
    {
        if (prescriptionToDelete == null) return;

        // 1. Hỏi xác nhận trước khi xóa
        bool confirmed = await Application.Current.MainPage.DisplayAlert(
            "Xác nhận xóa",
            $"Bạn có chắc chắn muốn xóa đơn thuốc mã '{prescriptionToDelete.Id}' của bệnh nhân {prescriptionToDelete.PatientName}?",
            "Xóa",
            "Hủy");

        if (confirmed)
        {
            // 2. Xóa trong Database
            await _databaseService.DeletePrescriptionAsync(prescriptionToDelete);

            // 3. Xóa trên Giao diện (để không phải load lại toàn bộ)
            Prescriptions.Remove(prescriptionToDelete);

            // (Tùy chọn) Nếu bạn có list gốc _allPrescriptions thì xóa trong đó nữa
            // _allPrescriptions.Remove(prescriptionToDelete);
        }
    }

    private void SearchPrescriptions()
    {
        if (string.IsNullOrWhiteSpace(SearchPrescriptionText))
        {
            // Reset về danh sách gốc
            if (_allPrescriptions != null && _allPrescriptions.Any())
            {
                Prescriptions = new ObservableCollection<Prescription>(_allPrescriptions);
            }
        }
        else
        {
            // Chuyển từ khóa về chữ thường để tìm không phân biệt hoa thường
            var keyword = SearchPrescriptionText.ToLower();

            // Lọc dữ liệu: Tìm theo Tên BN hoặc Tên Bác Sĩ hoặc Mã đơn
            var filtered = _allPrescriptions.Where(p =>
                p.PatientName.ToLower().Contains(keyword) ||
                p.DoctorName.ToLower().Contains(keyword) ||  // <--- Đã thêm tìm theo Bác sĩ
                p.Id.ToLower().Contains(keyword)
            ).ToList();

            Prescriptions = new ObservableCollection<Prescription>(filtered);
        }
    }

    // --- CÁC COMMAND XỬ LÝ ---

    // 1. Xem chi tiết đơn thuốc
    [RelayCommand]
    private async Task IssuePrescription()
    {
        if (SelectedPrescription == null) return;

        // Hỏi xác nhận
        bool confirm = await Shell.Current.DisplayAlert("Thu Ngân",
            $"Xác nhận thu: {SelectedPrescription.TotalAmount:N0} đ\nvà cấp thuốc cho bệnh nhân?",
            "Thu tiền & Cấp", "Hủy");

        if (!confirm) return;

        // 1. Cập nhật Status (Nhờ ObservableProperty ở Model, UI tự đổi màu/chữ ngay lập tức)
        SelectedPrescription.Status = "Đã cấp";

        // 2. LƯU VÀO DATABASE NGAY LẬP TỨC (QUAN TRỌNG!)
        await _databaseService.UpdatePrescriptionAsync(SelectedPrescription);

        // 3. Ẩn nút cấp phát đi
        IsIssueButtonVisible = false;

        // 4. Gửi tiền sang RevenueViewModel
        // Lưu ý: TotalAmount giờ đã tính đúng (Price * Quantity)
        WeakReferenceMessenger.Default.Send(new RevenueUpdateMessage((SelectedPrescription.TotalAmount, DateTime.Now)));

        // 5. Reload lại danh sách prescriptions từ DB để đồng bộ
        await LoadPrescriptions();

        await Shell.Current.DisplayAlert("Thành công", "Đã cập nhật trạng thái và doanh thu!", "OK");
    }
    [RelayCommand]
    private void ShowPrescriptionDetail(Prescription prescription)
    {
        if (prescription == null) return;
        SelectedPrescription = prescription;

        // Logic ẩn/hiện nút: Chỉ hiện khi chưa cấp
        IsIssueButtonVisible = SelectedPrescription.Status == "Chưa cấp";

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
        _allPrescriptions.Add(newPrescription);
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