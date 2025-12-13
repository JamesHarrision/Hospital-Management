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
using HospitalManager.MVVM.Models;
using HosipitalManager.MVVM.Enums;

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

        // Backing fields + properties for chart bindings (explicit, avoid source-generator issues)
        private ISeries[] _revenueSeries;
        public ISeries[] RevenueSeries
        {
            get => _revenueSeries;
            set => SetProperty(ref _revenueSeries, value);
        }

        private string[] _xAxisLabels;
        public string[] XAxisLabels
        {
            get => _xAxisLabels;
            set => SetProperty(ref _xAxisLabels, value);
        }

        private Axis[] _xAxes;
        public Axis[] XAxes
        {
            get => _xAxes;
            set => SetProperty(ref _xAxes, value);
        }

        private Axis[] _yAxes;
        public Axis[] YAxes
        {
            get => _yAxes;
            set => SetProperty(ref _yAxes, value);
        }

        // Danh sách chi tiết để hiển thị bảng
        public ObservableCollection<MonthlyRevenue> YearlyStats { get; set; } = new();

        // Lưu danh sách đơn thuốc để tính toán doanh thu
        private List<HospitalManager.MVVM.Models.Prescription> _prescriptions = new();

        public RevenueViewModel()
        {
            InitializeYearlyStats();
            LoadChartData();

            // Listen for single-prescription updates (when user issues a prescription)
            WeakReferenceMessenger.Default.Register<RevenueUpdateMessage>(this, (r, m) =>
            {
                UpdateRevenue(m.Value.Amount, m.Value.Date);
            });

            // Listen for the full prescriptions list when Dashboard loads prescriptions
            WeakReferenceMessenger.Default.Register<PrescriptionsLoadedMessage>(this, (r, m) =>
            {
                // m.Value is List<Prescription>
                LoadRevenueFromPrescriptions(m.Value);
            });
        }

        /// <summary>
        /// Tải dữ liệu doanh thu từ danh sách đơn thuốc đã được cấp
        /// </summary>
        public void LoadRevenueFromPrescriptions(List<Prescription> prescriptions)
        {
            if (prescriptions == null || prescriptions.Count == 0)
                return;

            _prescriptions = prescriptions;

            // Đặt lại tất cả Amount về 0
            foreach (var stat in YearlyStats)
            {
                stat.Amount = 0;
                stat.PatientCount = 0;
            }

            // Tính toán doanh thu từ các đơn thuốc đã được cấp
            var issuedPrescriptions = prescriptions
                .Where(p => p.Status == PrescriptionStatus.Issued)
                .ToList();

            foreach (var prescription in issuedPrescriptions)
            {
                var monthName = $"Tháng {prescription.DatePrescribed.Month}";
                var monthStat = YearlyStats.FirstOrDefault(x => x.MonthName == monthName);

                if (monthStat != null)
                {
                    monthStat.Amount += prescription.TotalAmount;
                    monthStat.PatientCount++;
                }
            }

            // Tính toán độ dài thanh hiển thị
            double maxAmount = (double)YearlyStats.Max(x => x.Amount);
            if (maxAmount > 0)
            {
                foreach (var item in YearlyStats)
                {
                    item.PercentageBar = (double)item.Amount / maxAmount;
                }
            }

            // Cập nhật các thẻ Card thống kê
            UpdateStatsCards();

            // Vẽ lại biểu đồ
            RefreshChart();
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

            // 2. Tính toán lại độ dài thanh
            double maxAmount = (double)YearlyStats.Max(x => x.Amount);
            if (maxAmount > 0)
            {
                foreach (var item in YearlyStats)
                {
                    item.PercentageBar = (double)item.Amount / maxAmount;
                }
            }

            // 3. Vẽ lại biểu đồ
            RefreshChart();

            // 4. Cập nhật thẻ Top Doanh thu
            UpdateStatsCards();
        }

        private void RefreshChart()
        {
            // Lấy dữ liệu mới nhất từ YearlyStats
            var revenues = YearlyStats.Select(s => (double)s.Amount).ToArray();

            // Cập nhật lại RevenueSeries
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
        }

        private void UpdateStatsCards()
        {
            // Tính toán lại tháng cao điểm nhất
            var bestMonth = YearlyStats.OrderByDescending(x => x.Amount).FirstOrDefault();
            if (bestMonth != null)
            {
                BestRevenueMonth = bestMonth.MonthName;
                BestRevenueAmount = $"{bestMonth.Amount:N0} đ";
            }

            // Tính toán tháng bận rộn nhất
            var busiestMonth = YearlyStats.OrderByDescending(x => x.PatientCount).FirstOrDefault();
            if (busiestMonth != null)
            {
                BusiestMonth = busiestMonth.MonthName;
                BusiestPatientCount = $"{busiestMonth.PatientCount} Bệnh nhân";
            }

            // Tính toán bác sĩ có doanh thu cao nhất
            if (_prescriptions != null && _prescriptions.Any())
            {
                var issuedPrescriptions = _prescriptions
                    .Where(p => p.Status == PrescriptionStatus.Issued)
                    .GroupBy(p => p.DoctorName)
                    .OrderByDescending(g => g.Sum(p => p.TotalAmount))
                    .FirstOrDefault();

                if (issuedPrescriptions != null)
                {
                    TopDoctorName = issuedPrescriptions.Key;
                    var totalRevenue = issuedPrescriptions.Sum(p => p.TotalAmount);
                    TopDoctorRevenue = $"{totalRevenue:N0} đ";
                }
            }
        }

        private void LoadChartData()
        {
            // Lấy danh sách doanh thu từ YearlyStats
            var revenues = YearlyStats.Select(s => (double)s.Amount).ToArray();
            var months = YearlyStats.Select(s => s.MonthName).ToArray();

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

            XAxisLabels = months;

            XAxes = new[]
            {
                new Axis
                {
                    Labels = XAxisLabels,
                    LabelsRotation = 45,
                    TextSize = 12,
                    SeparatorsPaint = null
                }
            };

            YAxes = new[]
            {
                new Axis
                {
                    Labeler = p => $"{p:N0} đ",
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1)
                }
            };
        }

        /// <summary>
        /// Khởi tạo danh sách các tháng với doanh thu ban đầu = 0
        /// </summary>
        private void InitializeYearlyStats()
        {
            var rawData = new List<MonthlyRevenue>
            {
                new MonthlyRevenue { MonthName = "Tháng 1", Amount = 0, PatientCount = 0 },
                new MonthlyRevenue { MonthName = "Tháng 2", Amount = 0, PatientCount = 0 },
                new MonthlyRevenue { MonthName = "Tháng 3", Amount = 0, PatientCount = 0 },
                new MonthlyRevenue { MonthName = "Tháng 4", Amount = 0, PatientCount = 0 },
                new MonthlyRevenue { MonthName = "Tháng 5", Amount = 0, PatientCount = 0 },
                new MonthlyRevenue { MonthName = "Tháng 6", Amount = 0, PatientCount = 0 },
                new MonthlyRevenue { MonthName = "Tháng 7", Amount = 0, PatientCount = 0 },
                new MonthlyRevenue { MonthName = "Tháng 8", Amount = 0, PatientCount = 0 },
                new MonthlyRevenue { MonthName = "Tháng 9", Amount = 0, PatientCount = 0 },
                new MonthlyRevenue { MonthName = "Tháng 10", Amount = 0, PatientCount = 0 },
                new MonthlyRevenue { MonthName = "Tháng 11", Amount = 0, PatientCount = 0 },
                new MonthlyRevenue { MonthName = "Tháng 12", Amount = 0, PatientCount = 0 },
            };

            foreach (var item in rawData)
            {
                item.PercentageBar = 0;
                YearlyStats.Add(item);
            }
        }
    }
}
