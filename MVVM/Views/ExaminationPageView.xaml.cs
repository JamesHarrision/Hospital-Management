using HospitalManager.MVVM.ViewModels; 
using Microsoft.Maui.Controls;

namespace HosipitalManager.MVVM.Views;

public partial class ExaminationPageView : ContentPage
{
    public ExaminationPageView(ExaminationViewModel vm)
    {
        this.BindingContext = vm;
        InitializeComponent();
    }
}