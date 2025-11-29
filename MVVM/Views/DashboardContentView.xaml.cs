using CommunityToolkit.Mvvm.Messaging;
using HosipitalManager.MVVM.Models;
using HosipitalManager.MVVM.Services;
using Microsoft.Maui.Controls.Shapes;
using static HospitalManager.MVVM.ViewModels.DashboardViewModel;

namespace HosipitalManager.MVVM.Views;

public partial class DashboardContentView : ContentView
{
    // Cấu hình thời gian làm việc
    private readonly TimeSpan _startHour = TimeSpan.FromHours(7); // Bắt đầu 7h sáng
    private readonly TimeSpan _endHour = TimeSpan.FromHours(19);  // Kết thúc 19h tối
    private readonly int _slotDurationMinutes = 30; // Mỗi ô là 30 phút

    public DashboardContentView()
    {
        InitializeComponent();

        // Vẽ lịch khi khởi tạo
        RenderSchedule();

        // Đăng ký nhận tin nhắn để vẽ lại lịch khi có thay đổi (Thêm/Xóa lịch)
        WeakReferenceMessenger.Default.Send(new DashboardRefreshMessage());
    }

    private void RefreshCalendar_Clicked(object sender, EventArgs e)
    {
        RenderSchedule();
    }

    private void RenderSchedule()
    {
        ScheduleGrid.Children.Clear();
        ScheduleGrid.RowDefinitions.Clear();
        ScheduleGrid.ColumnDefinitions.Clear();

        // 1. Định nghĩa Cột (8 cột: 1 cột giờ + 7 cột thứ)
        ScheduleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = 50 }); // Cột giờ
        for (int i = 0; i < 7; i++)
        {
            ScheduleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        }

        // 2. Vẽ Header (Thứ 2 -> CN)
        string[] days = { "Giờ", "T2", "T3", "T4", "T5", "T6", "T7", "CN" };
        ScheduleGrid.RowDefinitions.Add(new RowDefinition { Height = 40 }); // Dòng Header

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
            ScheduleGrid.Add(label, i, 0); // Cột i, Dòng 0
        }

        // 3. Vẽ Dòng thời gian (Slot)
        int totalSlots = (int)((_endHour - _startHour).TotalMinutes / _slotDurationMinutes);

        for (int row = 0; row < totalSlots; row++)
        {
            ScheduleGrid.RowDefinitions.Add(new RowDefinition { Height = 50 }); // Chiều cao mỗi ô 30p

            // Hiển thị giờ ở cột đầu tiên (Cột 0)
            TimeSpan time = _startHour.Add(TimeSpan.FromMinutes(row * _slotDurationMinutes));

            // Chỉ hiện giờ chẵn (VD: 8:00, 9:00) hoặc hiện tất cả tùy bạn
            var timeLabel = new Label
            {
                Text = time.ToString(@"hh\:mm"),
                FontSize = 11,
                TextColor = Colors.Gray,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 5, 0, 0)
            };
            ScheduleGrid.Add(timeLabel, 0, row + 1); // +1 vì dòng 0 là Header

            // Vẽ đường kẻ mờ cho các ô còn lại
            for (int col = 1; col <= 7; col++)
            {
                var border = new BoxView { Color = Colors.White, Margin = 1 }; // Tạo hiệu ứng ô lưới
                ScheduleGrid.Add(border, col, row + 1);
            }
        }

        // 4. Đặt các Lịch hẹn vào lưới
       var appointments = HospitalSystem.Instance.Appointments;

        // Lấy ngày đầu tuần hiện tại (để tính toán đúng cột cho T2, T3...)
        DateTime today = DateTime.Today;
        int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        DateTime mondayDate = today.AddDays(-1 * diff).Date;
        DateTime sundayDate = mondayDate.AddDays(7);

        foreach (var appt in appointments)
        {
            // Nếu không phải Upcoming, bỏ qua ngay.
            if (appt.Status != AppointmentStatus.Upcoming)
                continue;

            // Kiểm tra ngày trong tuần
            if (appt.AppointmentDate.Date < mondayDate || appt.AppointmentDate.Date >= sundayDate)
                continue;

            // Tính Cột (Thứ)
            // DayOfWeek: Sunday=0, Monday=1... Saturday=6
            // Ta muốn: Monday=1 (Cột 1), ..., Sunday=7 (Cột 7)
            int colIndex = appt.AppointmentDate.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)appt.AppointmentDate.DayOfWeek;

            // Tính Dòng (Giờ)
            if (appt.StartTime < _startHour || appt.StartTime >= _endHour) continue; // Ngoài giờ làm việc

            double minutesFromStart = (appt.StartTime - _startHour).TotalMinutes;
            int rowIndex = (int)(minutesFromStart / _slotDurationMinutes) + 1; // +1 do Header

            // Tính RowSpan (Thời lượng khám chiếm bao nhiêu ô)
            double durationMinutes = (appt.EndTime - appt.StartTime).TotalMinutes;
            int span = (int)Math.Ceiling(durationMinutes / _slotDurationMinutes);
            if (span < 1) span = 1;

            // Tạo Card Lịch hẹn
            var card = CreateAppointmentCard(appt);

            // Thêm vào Grid
            ScheduleGrid.Add(card);
            Grid.SetColumn(card, colIndex);
            Grid.SetRow(card, rowIndex);
            Grid.SetRowSpan(card, span);
        }
    }

    private View CreateAppointmentCard(Appointment appt)
    {
        // Vì chỉ hiện Upcoming nên ta cố định màu Xanh (hoặc logic màu khác tùy bạn)
        Color bgColor = Color.FromArgb("#E3F2FD");
        Color stripeColor = Color.FromArgb("#2196F3");

        var border = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 5 },
            StrokeThickness = 0,
            BackgroundColor = bgColor,
            Margin = new Thickness(1),
            Padding = new Thickness(5)
        };

        var content = new VerticalStackLayout
        {
            Spacing = 2,
            Children =
        {
            // Tên Bệnh nhân
            new Label {
                Text = appt.PatientName,
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Black,
                LineBreakMode = LineBreakMode.TailTruncation
            },
            
            // --- THAY ĐỔI 2: HIỆN NGÀY VÀ GIỜ ---
            // Ví dụ: 15/05 (09:00 - 09:30)
            new Label {
                Text = $"{appt.AppointmentDate:dd/MM} ({appt.StartTime:hh\\:mm}-{appt.EndTime:hh\\:mm})",
                FontSize = 10,
                TextColor = Color.FromArgb("#1D2E4E"),
                FontAttributes = FontAttributes.Italic
            },

            // Tên Bác sĩ (Tùy chọn)
            new Label {
                Text = $"BS. {appt.Doctor.Name}",
                FontSize = 9,
                TextColor = Colors.Gray
            }
        }
        };

        // Layout chứa thanh màu bên trái
        var container = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition { Width = 4 }, new ColumnDefinition { Width = GridLength.Star } } };
        var stripe = new BoxView { Color = stripeColor, VerticalOptions = LayoutOptions.Fill };

        container.Add(stripe, 0, 0);
        container.Add(content, 1, 0);
        border.Content = container;

        return border;
    }
}