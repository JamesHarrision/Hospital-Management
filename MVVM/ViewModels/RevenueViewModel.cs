using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HosipitalManager.MVVM.Enums;
using HosipitalManager.MVVM.Messages;
using HosipitalManager.MVVM.Models;
using HosipitalManager.MVVM.Services;
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
        #region 1. Properties: KPIs & Statistics
        // ==========================================================
        // Các thuộc tính hiển thị thẻ thống kê (Card)
        // ==========================================================
        [ObservableProperty] private string bestRevenueMonth;
        [ObservableProperty] private string bestRevenueAmount;

        [ObservableProperty] private string busiestMonth;
        [ObservableProperty] private string busiestPatientCount;

        [ObservableProperty] private string topDoctorName;
        [ObservableProperty] private string topDoctorRevenue;
        #endregion

        #region 2. Properties: Chart Configuration
        // ==========================================================
        // Các thuộc tính cấu hình biểu đồ LiveChartsCore
        // ==========================================================
        [ObservableProperty] private ISeries[] _revenueSeries;
        [ObservableProperty] private Axis[] _xAxes;
        [ObservableProperty] private Axis[] _yAxes;

        // "Backing store" cho dữ liệu biểu đồ. Thay đổi ở đây -> Chart tự update
        private ObservableCollection<double> _chartValues;
        #endregion

        #region 3. Properties: Data Collections
        // ==========================================================
        // Dữ liệu danh sách hiển thị và lưu trữ nội bộ
        // ==========================================================

        /// <summary>
        /// Danh sách hiển thị bảng chi tiết bên dưới biểu đồ
        /// </summary>
        public ObservableCollection<MonthlyRevenue> YearlyStats { get; set; } = new();

        /// <summary>
        /// Lưu danh sách đơn thuốc gốc (private)
        /// </summary>
        private List<Prescription> _prescriptions = new();
        #endregion

        #region 4. Constructor

        public RevenueViewModel() : this(new LocalDatabaseService())
        {
        }
        public RevenueViewModel(LocalDatabaseService databaseService)
        {
            _databaseService = databaseService;

            InitializeData();
            SetupChart();
            RegisterMessages();

            _ = RefreshData(); // fire & forget
        }   
        #endregion

        #region 5. Initialization Methods
        private void InitializeData()
        {
            _chartValues = new ObservableCollection<double>();

            for (int i = 1; i <= 12; i++)
            {
                // Tạo dữ liệu cho bảng
                YearlyStats.Add(new MonthlyRevenue
                {
                    MonthName = $"Tháng {i}",
                    Amount = 0,
                    PatientCount = 0,
                    PercentageBar = 0
                });

                // Tạo dữ liệu cho biểu đồ (Mặc định 0)
                _chartValues.Add(0);
            }
        }

        private void SetupChart()
        {
            // Lấy Label từ danh sách tháng
            var months = YearlyStats.Select(s => s.MonthName).ToArray();

            // Cấu hình Trục X
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

            // Cấu hình Trục Y
            YAxes = new[]
            {
                new Axis
                {
                    Labeler = p => $"{p:N0} đ",
                    TextSize = 12,
                    SeparatorsPaint = new SolidColorPaint(SKColors.LightGray, 1)
                }
            };

            // Cấu hình Series
            // QUAN TRỌNG: Binding Values vào _chartValues
            RevenueSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Name = "Doanh thu",
                    Values = _chartValues, // Liên kết trực tiếp
                    Fill = new SolidColorPaint(SKColors.Purple.WithAlpha(150)),
                    Stroke = new SolidColorPaint(SKColors.Purple) { StrokeThickness = 2 },
                    DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                    DataLabelsFormatter = p => $"{p.Coordinate.PrimaryValue:N0} đ",
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top
                }
            };
        }
        #endregion

        #region 6. Messaging Logic
        private void RegisterMessages()
        {
            // Lắng nghe khi có 1 đơn thuốc mới được cập nhật lẻ
            WeakReferenceMessenger.Default.Register<RevenueUpdateMessage>(this, (r, m) =>
            {
                UpdateSingleRevenue(m.Value.Amount, m.Value.Date);
            });

            // Lắng nghe khi load lại toàn bộ danh sách đơn thuốc
            WeakReferenceMessenger.Default.Register<PrescriptionsLoadedMessage>(this, (r, m) =>
            {
                LoadRevenueFromPrescriptions(m.Value);
            });
        }
        #endregion

        #region 7. Core Business Logic (Data Processing)
        /// <summary>
        /// Xử lý load lại toàn bộ danh sách
        /// </summary>
        public void LoadRevenueFromPrescriptions(List<Prescription> prescriptions)
        {
            if (prescriptions == null) return;
            _prescriptions = prescriptions;

            // 1. Reset toàn bộ về 0
            for (int i = 0; i < 12; i++)
            {
                YearlyStats[i].Amount = 0;
                YearlyStats[i].PatientCount = 0;
                _chartValues[i] = 0; // Reset biểu đồ
            }

            // 2. Lọc danh sách đã cấp (Issued)
            var issuedList = prescriptions
                .Where(p => p.Status == PrescriptionStatus.Issued) // Sử dụng Enum
                .ToList();

            // 3. Cộng dồn dữ liệu
            foreach (var p in issuedList)
            {
                int monthIndex = p.DatePrescribed.Month - 1;
                if (monthIndex >= 0 && monthIndex < 12)
                {
                    YearlyStats[monthIndex].Amount += p.TotalAmount;
                    YearlyStats[monthIndex].PatientCount++;
                }
            }

            // 4. Đồng bộ hiển thị
            SyncChartAndStats();
        }

        /// <summary>
        /// Xử lý cập nhật đơn lẻ (RevenueUpdateMessage)
        /// </summary>
        private void UpdateSingleRevenue(decimal amount, DateTime date)
        {
            int monthIndex = date.Month - 1;
            if (monthIndex >= 0 && monthIndex < 12)
            {
                YearlyStats[monthIndex].Amount += amount;
                YearlyStats[monthIndex].PatientCount++;
            }
            SyncChartAndStats();
        }
        #endregion

        #region 8. UI Synchronization & Calculations
        /// <summary>
        /// Hàm đồng bộ dữ liệu tính toán và biểu đồ (Core Logic)
        /// </summary>
        private void SyncChartAndStats()
        {
            double maxAmount = (double)YearlyStats.Max(x => x.Amount);

            for (int i = 0; i < 12; i++)
            {
                // 1. Cập nhật Progress Bar (UI Bảng)
                if (maxAmount > 0)
                    YearlyStats[i].PercentageBar = (double)YearlyStats[i].Amount / maxAmount;
                else
                    YearlyStats[i].PercentageBar = 0;

                // 2. CẬP NHẬT BIỂU ĐỒ
                // Chỉ cần gán giá trị vào Index tương ứng, LiveCharts tự vẽ lại
                _chartValues[i] = (double)YearlyStats[i].Amount;
            }

            // 3. Tính toán các thẻ Card thống kê
            UpdateStatsCards();
        }

        private void UpdateStatsCards()
        {
            // --- Logic Thống kê Tháng ---
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

            // --- Logic Top Doctor ---
            if (_prescriptions != null && _prescriptions.Any())
            {
                var topDoctorGroup = _prescriptions
                    .Where(p => p.Status == PrescriptionStatus.Issued) // Dùng Enum
                    .GroupBy(p => p.DoctorName)
                    .OrderByDescending(g => g.Sum(p => p.TotalAmount))
                    .FirstOrDefault();

                if (topDoctorGroup != null)
                {
                    TopDoctorName = topDoctorGroup.Key;
                    var totalRevenue = topDoctorGroup.Sum(p => p.TotalAmount);
                    TopDoctorRevenue = $"{totalRevenue:N0} đ";
                }
                else
                {
                    TopDoctorName = "N/A";
                    TopDoctorRevenue = "0 đ";
                }
            }
        }
        #endregion

        #region 9. Refresh Configuration
        private readonly LocalDatabaseService _databaseService;

        [RelayCommand]
        public async Task RefreshData()
        {
            if (_databaseService == null) return;

            // Lấy danh sách mới nhất từ Database
            var prescriptions = await _databaseService.GetPrescriptionsAsync();

            // Xử lý deserialization (quan trọng nếu dùng MedicinesJson)
            foreach (var p in prescriptions)
            {
                p.DeserializeMedicines();
            }

            // Gọi hàm tính toán lại (Logic cũ của bạn)
            LoadRevenueFromPrescriptions(prescriptions);
        }
        #endregion
    }
}