using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using HosipitalManager.MVVM.Messages;
using HosipitalManager.MVVM.Models;
using HospitalManager.MVVM.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HosipitalManager.MVVM.ViewModels
{
    internal partial class RevenueViewModel : ObservableObject
    {
        // --- CÁC THÔNG SỐ KPI ---
        [ObservableProperty] private string bestRevenueMonth;
        [ObservableProperty] private string bestRevenueAmount;
        [ObservableProperty] private string busiestMonth;
        [ObservableProperty] private string busiestPatientCount;

        // --- CÁC PROPERTY CHO BIỂU ĐỒ (DÙNG OBSERVABLE PROPERTY) ---
        [ObservableProperty] private ISeries[] _revenueSeries;
        [ObservableProperty] private Axis[] _xAxes;
        [ObservableProperty] private Axis[] _yAxes;

        // DANH SÁCH GIÁ TRỊ THỰC TẾ ĐỂ CHART BINDING VÀO (QUAN TRỌNG)
        // Khi thay đổi giá trị trong list này, cột sẽ tự động cao lên/thấp xuống
        private ObservableCollection<double> _chartValues;

        // Danh sách hiển thị bảng chi tiết
        public ObservableCollection<MonthlyRevenue> YearlyStats { get; set; } = new();

        private List<Prescription> _prescriptions = new();

        public RevenueViewModel()
        {
            // 1. Khởi tạo dữ liệu rỗng
            InitializeData();

            // 2. Cấu hình biểu đồ 1 lần duy nhất
            SetupChart();

            // 3. Đăng ký lắng nghe sự kiện
            WeakReferenceMessenger.Default.Register<RevenueUpdateMessage>(this, (r, m) =>
            {
                UpdateRevenue(m.Value.Amount, m.Value.Date);
            });

            WeakReferenceMessenger.Default.Register<PrescriptionsLoadedMessage>(this, (r, m) =>
            {
                LoadRevenueFromPrescriptions(m.Value);
            });
        }

        private void InitializeData()
        {
            _chartValues = new ObservableCollection<double>();
            for (int i = 1; i <= 12; i++)
            {
                // Tạo dữ liệu bảng
                YearlyStats.Add(new MonthlyRevenue
                {
                    MonthName = $"Tháng {i}",
                    Amount = 0,
                    PatientCount = 0,
                    PercentageBar = 0
                });

                // Tạo dữ liệu biểu đồ (mặc định bằng 0)
                _chartValues.Add(0);
            }
        }

        private void SetupChart()
        {
            // Cấu hình trục X
            var months = YearlyStats.Select(s => s.MonthName).ToArray();
            XAxes = new[]
            {
                new Axis
                {
                    Labels = months,
                    LabelsRotation = 45,
                    TextSize = 12,
                    SeparatorsPaint = null
                }
            };

            // Cấu hình trục Y
            YAxes = new[]
            {
                new Axis
                {
                    Labeler = p => $"{p:N0} đ",
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1)
                }
            };

            // Cấu hình Series (Binding Values vào _chartValues)
            RevenueSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Name = "Doanh thu",
                    Values = _chartValues, // LIÊN KẾT TRỰC TIẾP VÀO ĐÂY
                    Fill = new SolidColorPaint(SKColors.Purple.WithAlpha(150)),
                    Stroke = new SolidColorPaint(SKColors.Purple) { StrokeThickness = 2 },
                    DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                    DataLabelsFormatter = p => $"{p.Coordinate.PrimaryValue:N0} đ",
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top
                }
            };
        }

        public void LoadRevenueFromPrescriptions(List<Prescription> prescriptions)
        {
            if (prescriptions == null) return;
            _prescriptions = prescriptions;

            // Reset về 0
            for (int i = 0; i < 12; i++)
            {
                YearlyStats[i].Amount = 0;
                YearlyStats[i].PatientCount = 0;
                _chartValues[i] = 0; // Reset biểu đồ
            }

            // Tính toán lại
            var issuedList = prescriptions.Where(p => p.Status == "Đã cấp").ToList();
            foreach (var p in issuedList)
            {
                int monthIndex = p.DatePrescribed.Month - 1;
                if (monthIndex >= 0 && monthIndex < 12)
                {
                    YearlyStats[monthIndex].Amount += p.TotalAmount;
                    YearlyStats[monthIndex].PatientCount++;
                }
            }

            SyncChartAndStats();
        }

        private void UpdateRevenue(decimal amount, DateTime date)
        {
            int monthIndex = date.Month - 1;
            if (monthIndex >= 0 && monthIndex < 12)
            {
                YearlyStats[monthIndex].Amount += amount;
                YearlyStats[monthIndex].PatientCount++;
            }
            SyncChartAndStats();
        }

        private void SyncChartAndStats()
        {
            double maxAmount = (double)YearlyStats.Max(x => x.Amount);

            for (int i = 0; i < 12; i++)
            {
                // 1. Cập nhật Progress Bar trong danh sách
                if (maxAmount > 0)
                    YearlyStats[i].PercentageBar = (double)YearlyStats[i].Amount / maxAmount;
                else
                    YearlyStats[i].PercentageBar = 0;

                // 2. CẬP NHẬT BIỂU ĐỒ (Quan trọng)
                // Chỉ cần gán giá trị mới, LiveCharts tự vẽ lại
                _chartValues[i] = (double)YearlyStats[i].Amount;
            }

            UpdateStatsCards();
        }

        private void UpdateStatsCards()
        {
            var bestMonth = YearlyStats.OrderByDescending(x => x.Amount).FirstOrDefault();
            if (bestMonth != null)
            {
                BestRevenueMonth = bestMonth.MonthName;
                BestRevenueAmount = $"{bestMonth.Amount:N0} đ";
            }

            var busiestMonth = YearlyStats.OrderByDescending(x => x.PatientCount).FirstOrDefault();
            if (busiestMonth != null)
            {
                BusiestMonth = busiestMonth.MonthName;
                BusiestPatientCount = $"{busiestMonth.PatientCount} Bệnh nhân";
            }
        }
    }
}