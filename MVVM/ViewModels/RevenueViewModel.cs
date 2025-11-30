using CommunityToolkit.Mvvm.ComponentModel;
using HosipitalManager.MVVM.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging; 
using HosipitalManager.MVVM.Messages;  

namespace HosipitalManager.MVVM.ViewModels
{
    internal partial class RevenueViewModel : ObservableObject
    {
        [ObservableProperty] private string bestRevenueMonth;
        [ObservableProperty] private string bestRevenueAmount;

        [ObservableProperty] private string busiestMonth;
        [ObservableProperty] private string busiestPatientCount;

        [ObservableProperty] private string topDoctorName;
        [ObservableProperty] private string topDoctorRevenue;

        // THÊM: Dữ liệu cho biểu đồ cột
        public ISeries[] RevenueSeries { get; set; }

        // THÊM: Các nhãn (label) cho trục X (tên các tháng)
        public string[] XAxisLabels { get; set; }

        // THÊM: Các thiết lập cho trục X
        public Axis[] XAxes { get; set; }

        // THÊM: Các thiết lập cho trục Y
        public Axis[] YAxes { get; set; }

        // Danh sách chi tiết để hiển thị bảng
        public ObservableCollection<MonthlyRevenue> YearlyStats { get; set; } = new();

        public RevenueViewModel()
        {
            LoadFakeData();
            LoadChartData();

            WeakReferenceMessenger.Default.Register<RevenueUpdateMessage>(this, (r, m) =>
            {
                UpdateRevenue(m.Value.Amount, m.Value.Date);
            });
        }
        private void UpdateRevenue(decimal amount, DateTime date)
        {
            // 1. Cộng tiền vào dữ liệu thống kê
            var targetMonth = $"Tháng {date.Month}";
            var stat = YearlyStats.FirstOrDefault(x => x.MonthName == targetMonth);
            if (stat != null)
            {
                stat.Amount += amount;
                stat.PatientCount++;
            }

            // 2. Vẽ lại biểu đồ (Cần thiết để UI cập nhật)
            var revenues = YearlyStats.Select(s => (double)s.Amount).ToArray();
            RevenueSeries[0].Values = revenues; // Cập nhật mảng giá trị cho biểu đồ

            // Cập nhật thẻ Top Doanh thu nếu cần
            var best = YearlyStats.OrderByDescending(x => x.Amount).First();
            BestRevenueAmount = $"{best.Amount:N0} đ";
        }

        private void RefreshChart()
        {
            // Lấy dữ liệu mới nhất từ YearlyStats
            var revenues = YearlyStats.Select(s => (double)s.Amount).ToArray();

            // Cập nhật lại RevenueSeries
            // Lưu ý: LiveCharts cần thay đổi instance hoặc dùng ObservableValue để trigger UI update
            // Ở đây mình tạo mới Series cho đơn giản và chắc chắn
            RevenueSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Name = "Doanh thu",
                    Values = revenues,
                    Fill = new SolidColorPaint(SKColors.Purple.WithAlpha(150)),
                    Stroke = new SolidColorPaint(SKColors.Purple),
                    DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                    DataLabelsFormatter = p => $"{p.Coordinate.PrimaryValue:N0} đ",
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top
                }
            };

            // Thông báo cho View biết RevenueSeries đã thay đổi để vẽ lại
            OnPropertyChanged(nameof(RevenueSeries));
        }
        private void UpdateStatsCards()
        {
            // Tính toán lại tháng cao điểm nhất
            var bestMonth = YearlyStats.OrderByDescending(x => x.Amount).First();
            BestRevenueMonth = bestMonth.MonthName;
            BestRevenueAmount = $"{bestMonth.Amount:N0} đ";
        }
        private void LoadChartData()
        {
            // Lấy danh sách doanh thu từ rawData (đã có từ LoadFakeData)
            var revenues = YearlyStats.Select(s => (double)s.Amount).ToArray();
            var months = YearlyStats.Select(s => s.MonthName).ToArray();

            RevenueSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Name = "Doanh thu",
                    Values = revenues,
                    Fill = new SolidColorPaint(SKColors.Purple.WithAlpha(150)), // Màu tím mờ
                    Stroke = new SolidColorPaint(SKColors.Purple), // Viền tím đậm
                    DataLabelsPaint = new SolidColorPaint(SKColors.Black), // Màu chữ trên cột
                    DataLabelsFormatter = p => $"{p.Coordinate.PrimaryValue:N0} đ", // Định dạng hiển thị trên cột
                    // p là một điểm trên biểu đồ (ChartPoint).

                     //Coordinate là tọa độ của điểm đó.

                     // PrimaryValue là giá trị trục chính (trục Y - Doanh thu).
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top // Hiện trên đỉnh cột
                }
            };

            XAxisLabels = months;

            XAxes = new[]
            {
                new Axis
                {
                    Labels = XAxisLabels,
                    LabelsRotation = 45, // Xoay nhãn để dễ đọc nếu nhiều tháng
                    TextSize = 12,
                    SeparatorsPaint = null // Không vẽ đường kẻ dọc
                }
            };

            YAxes = new[]
            {
                new Axis
                {
                    Labeler = p => $"{p:N0} đ", // Định dạng hiển thị trên trục Y (ví dụ: 120,000,000 đ)
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1) // Vẽ đường kẻ ngang
                }
            };
        }

        private void LoadFakeData()
        {
            // 1. Giả lập dữ liệu 6 tháng đầu năm
            var rawData = new List<MonthlyRevenue>
            {
                new MonthlyRevenue { MonthName = "Tháng 1", Amount = 120000000, PatientCount = 450 },
                new MonthlyRevenue { MonthName = "Tháng 2", Amount = 150000000, PatientCount = 500 },
                new MonthlyRevenue { MonthName = "Tháng 3", Amount = 90000000,  PatientCount = 320 },
                new MonthlyRevenue { MonthName = "Tháng 4", Amount = 200000000, PatientCount = 600 }, // Cao nhất
                new MonthlyRevenue { MonthName = "Tháng 5", Amount = 180000000, PatientCount = 550 },
                new MonthlyRevenue { MonthName = "Tháng 6", Amount = 110000000, PatientCount = 400 },
                new MonthlyRevenue { MonthName = "Tháng 7", Amount = 100000000, PatientCount = 310 },
                new MonthlyRevenue { MonthName = "Tháng 8", Amount = 19000000, PatientCount = 300 },
                new MonthlyRevenue { MonthName = "Tháng 9", Amount = 18000000, PatientCount = 250},
                new MonthlyRevenue { MonthName = "Tháng 10", Amount = 1000000, PatientCount = 100 },
                new MonthlyRevenue { MonthName = "Tháng 11", Amount = 1100000, PatientCount = 150 },
                new MonthlyRevenue { MonthName = "Tháng 12", Amount = 11000000, PatientCount = 220 },
            };

            // Tính toán độ dài thanh hiển thị (lấy max làm chuẩn 100%)
            double maxVal = (double)rawData.Max(x => x.Amount);

            foreach (var item in rawData)
            {
                item.PercentageBar = (double)item.Amount / maxVal;
                YearlyStats.Add(item);
            }

            // 2. Tính toán các thẻ Card thống kê
            var bestMonth = rawData.OrderByDescending(x => x.Amount).First();
            BestRevenueMonth = bestMonth.MonthName;
            BestRevenueAmount = $"{bestMonth.Amount:N0} đ";

            var busyMonth = rawData.OrderByDescending(x => x.PatientCount).First();
            BusiestMonth = busyMonth.MonthName;
            BusiestPatientCount = $"{busyMonth.PatientCount} Bệnh nhân";

            // 3. Giả lập Top Bác sĩ
            TopDoctorName = "Dr. Khang";
            TopDoctorRevenue = "500.000.000 đ";
        }
    }
}
