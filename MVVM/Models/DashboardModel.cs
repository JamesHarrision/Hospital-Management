using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Graphics;

namespace HosipitalManager.MVVM.Models; 

// Lớp dữ liệu cho các thẻ tóm tắt trên Dashboard
public partial class SummaryCard : ObservableObject
{
    [ObservableProperty]
    private string title;

    [ObservableProperty]
    private string value;

    [ObservableProperty]
    private string icon;

    [ObservableProperty]
    private string changePercentage;

    [ObservableProperty]
    private Color cardColor;
}

// Lớp dữ liệu cho danh sách hoạt động gần đây
public partial class RecentActivity : ObservableObject
{
    [ObservableProperty]
    private string description;

    [ObservableProperty]
    private string time;

    [ObservableProperty]
    private string doctorName;
}