using CommunityToolkit.Mvvm.Messaging;
using HosipitalManager.MVVM.Models;
using HosipitalManager.MVVM.Services;
using HosipitalManager.MVVM.Messages; // Đảm bảo namespace này đúng với project của bạn
using Microsoft.Maui.Controls.Shapes;
using System.Linq;
using static HospitalManager.MVVM.ViewModels.DashboardViewModel;
using HospitalManager.MVVM.Messages;

namespace HosipitalManager.MVVM.Views;

public partial class DashboardContentView : ContentView
{
    // Cấu hình thời gian làm việc
    private readonly TimeSpan _startHour = TimeSpan.FromHours(7); // 7:00 AM
    private readonly TimeSpan _endHour = TimeSpan.FromHours(19);  // 7:00 PM
    private readonly int _slotDurationMinutes = 30;

    private LocalDatabaseService _databaseService;

    public DashboardContentView()
    {
        InitializeComponent();

        // Lấy Service Database
        if (Application.Current != null)
        {
            _databaseService = Application.Current.Handler?.MauiContext?.Services.GetService<LocalDatabaseService>()
                               ?? IPlatformApplication.Current?.Services.GetService<LocalDatabaseService>();
        }

        // Vẽ lịch khi khởi tạo
        Task.Run(RenderSchedule);

        // Đăng ký nhận tin nhắn Reload
        WeakReferenceMessenger.Default.Register<DashboardRefreshMessage>(this, (r, m) =>
        {
            Task.Run(RenderSchedule);
        });
    }

    private void RefreshCalendar_Clicked(object sender, EventArgs e)
    {
        Task.Run(RenderSchedule);
    }

    private async Task RenderSchedule()
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ScheduleGrid.Children.Clear();
            ScheduleGrid.RowDefinitions.Clear();
            ScheduleGrid.ColumnDefinitions.Clear();

            // 1. Định nghĩa Cột (8 cột: 1 cột giờ + 7 cột thứ)
            ScheduleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = 50 });
            for (int i = 0; i < 7; i++)
            {
                ScheduleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            }

            // 2. Vẽ Header (Thứ 2 -> CN)
            string[] days = { "Giờ", "T2", "T3", "T4", "T5", "T6", "T7", "CN" };
            ScheduleGrid.RowDefinitions.Add(new RowDefinition { Height = 40 });

            for (int i = 0; i < days.Length; i++)
            {
                var label = new Label
                {
                    Text = days[i],
                    FontAttributes = FontAttributes.Bold,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = Color.FromArgb("#1D2E4E")
                };
                ScheduleGrid.Add(label, i, 0);
            }

            // 3. Vẽ Dòng thời gian (Slot)
            int totalSlots = (int)((_endHour - _startHour).TotalMinutes / _slotDurationMinutes);

            for (int row = 0; row < totalSlots; row++)
            {
                ScheduleGrid.RowDefinitions.Add(new RowDefinition { Height = 50 });

                // Hiển thị giờ
                TimeSpan time = _startHour.Add(TimeSpan.FromMinutes(row * _slotDurationMinutes));
                var timeLabel = new Label
                {
                    Text = time.ToString(@"hh\:mm"),
                    FontSize = 11,
                    TextColor = Colors.Gray,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Start,
                    Margin = new Thickness(0, 5, 0, 0)
                };
                ScheduleGrid.Add(timeLabel, 0, row + 1);

                // Kẻ ô lưới
                for (int col = 1; col <= 7; col++)
                {
                    var border = new BoxView { Color = Colors.White, Margin = 1 };
                    ScheduleGrid.Add(border, col, row + 1);
                }
            }
        });

        // 4. Đặt các Lịch hẹn vào lưới
        if (_databaseService == null) return;

        var appointments = await _databaseService.GetAppointmentsAsync();

        // Map Doctor Object nếu cần
        foreach (var appt in appointments)
        {
            // Giả sử bạn có singleton chứa danh sách bác sĩ
            if (!string.IsNullOrEmpty(appt.DoctorId) && HospitalSystem.Instance != null)
            {
                var doc = HospitalSystem.Instance.Doctors.FirstOrDefault(d => d.Id == appt.DoctorId);
                if (doc != null) appt.DoctorObject = doc;
            }
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            DateTime today = DateTime.Today;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime mondayDate = today.AddDays(-1 * diff).Date;
            DateTime sundayDate = mondayDate.AddDays(7);

            foreach (var appt in appointments)
            {
                // --- QUAN TRỌNG: LỌC LỊCH ---
                // Chỉ hiện lịch "Upcoming". Nếu status là Completed/CheckedIn thì sẽ KHÔNG hiện.
                if (appt.Status != AppointmentStatus.Upcoming)
                    continue;

                // Kiểm tra ngày trong tuần
                if (appt.AppointmentDate.Date < mondayDate || appt.AppointmentDate.Date >= sundayDate)
                    continue;

                // Tính toán vị trí
                int colIndex = appt.AppointmentDate.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)appt.AppointmentDate.DayOfWeek;

                if (appt.StartTime < _startHour || appt.StartTime >= _endHour) continue;

                double minutesFromStart = (appt.StartTime - _startHour).TotalMinutes;
                int rowIndex = (int)(minutesFromStart / _slotDurationMinutes) + 1;

                double durationMinutes = (appt.EndTime - appt.StartTime).TotalMinutes;
                int span = (int)Math.Ceiling(durationMinutes / _slotDurationMinutes);
                if (span < 1) span = 1;

                // Tạo thẻ
                var card = CreateAppointmentCard(appt);

                ScheduleGrid.Add(card);
                Grid.SetColumn(card, colIndex);
                Grid.SetRow(card, rowIndex);
                Grid.SetRowSpan(card, span);
            }
        });
    }

    private View CreateAppointmentCard(Appointment appt)
    {
        Color bgColor = Color.FromArgb("#E3F2FD");
        Color stripeColor = Color.FromArgb("#2196F3");

        var border = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 5 },
            StrokeThickness = 0,
            BackgroundColor = bgColor,
            Margin = new Thickness(1),
            Padding = new Thickness(0)
        };

        var contentLayout = new VerticalStackLayout
        {
            Padding = new Thickness(5),
            Spacing = 2,
            Children =
            {
                new Label
                {
                    Text = appt.PatientName,
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.Black,
                    LineBreakMode = LineBreakMode.TailTruncation
                },
                new Label
                {
                    Text = $"{appt.StartTime:hh\\:mm} - {appt.EndTime:hh\\:mm}",
                    FontSize = 10,
                    TextColor = Color.FromArgb("#1D2E4E")
                },
                new Label
                {
                    Text = $"{appt.DoctorName}",
                    FontSize = 9,
                    TextColor = Colors.Gray,
                    LineBreakMode = LineBreakMode.TailTruncation
                }
            }
        };

        var container = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = 4 },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };

        var stripe = new BoxView { Color = stripeColor, VerticalOptions = LayoutOptions.Fill };

        container.Add(stripe, 0, 0);
        container.Add(contentLayout, 1, 0);
        border.Content = container;

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) =>
        {
            await ProcessCheckIn(appt);
        };
        border.GestureRecognizers.Add(tapGesture);

        return border;
    }

    /// <summary>
    /// Xử lý Check-in: Kiểm tra giờ -> Đổi trạng thái -> Thêm vào hàng đợi -> Xóa khỏi lịch
    /// </summary>
    private async Task ProcessCheckIn(Appointment appt)
    {
        DateTime now = DateTime.Now;
        DateTime appointmentTime = appt.AppointmentDate.Date + appt.StartTime;
        double diffMinutes = (now - appointmentTime).TotalMinutes;

        // 1. Kiểm tra thời gian (Sớm 10p, Trễ 10p)
        if (diffMinutes < -10)
        {
            int minutesEarly = (int)Math.Abs(diffMinutes);
            await Shell.Current.DisplayAlert("Chưa đến giờ",
                $"Bệnh nhân đến quá sớm ({minutesEarly} phút).\nVui lòng đợi đến {appointmentTime.AddMinutes(-10):HH:mm}.", "Đóng");
            return;
        }

        if (diffMinutes > 10)
        {
            int minutesLate = (int)diffMinutes;
            await Shell.Current.DisplayAlert("Quá giờ",
                $"Đã quá giờ hẹn {minutesLate} phút. Vui lòng xếp hàng thủ công.", "Đóng");
            return;
        }

        // 2. Xác nhận tiếp nhận
        bool isConfirmed = await Shell.Current.DisplayAlert(
              "Tiếp nhận",
              $"Tiếp nhận bệnh nhân {appt.PatientName}?\n(Lịch hẹn lúc {appt.StartTime:hh\\:mm})",
              "Tiếp nhận",
              "Hủy");

        if (!isConfirmed) return;

        // 3. LOGIC QUAN TRỌNG: Cập nhật trạng thái để xóa khỏi lịch
        // Chuyển từ Upcoming -> Completed (hoặc CheckedIn nếu có trong Enum)
        appt.Status = AppointmentStatus.Completed;

        // Lưu xuống Database
        if (_databaseService != null)
        {
            await _databaseService.UpdateAppointmentAsync(appt);
        }

        // 4. Gửi tin nhắn cho ViewModel thêm vào hàng đợi (Waiting Queue)
        WeakReferenceMessenger.Default.Send(new RequestCheckInMessage(appt));

        // 5. Vẽ lại lịch ngay lập tức (Lúc này lịch hẹn đã là Completed nên sẽ biến mất)
        await Task.Run(RenderSchedule);
    }
}