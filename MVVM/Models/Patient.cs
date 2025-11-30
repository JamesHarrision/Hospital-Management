// Nằm trong file: /MVVM/Models/Patient.cs
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace HospitalManager.MVVM.Models
{
    // 1. Thêm 'partial' và kế thừa 'ObservableObject'
    public partial class Patient : ObservableObject
    {
        // 2. Chuyển tất cả properties sang [ObservableProperty]
        // (Đây là cách MVVM Toolkit tự động tạo code INotifyPropertyChanged)
        [ObservableProperty]
        private string id;

        [ObservableProperty]
        private string fullName;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Age))] // 3. Thông báo cho 'Age' cập nhật khi 'DateOfBirth' thay đổi
        private DateTime dateOfBirth;

        [ObservableProperty]
        private string gender;

        [ObservableProperty]
        private string phoneNumber;

        [ObservableProperty]
        private string address;

        [ObservableProperty]
        private DateTime admittedDate;

        [ObservableProperty]
        private string status;

        [ObservableProperty]
        private string doctorname;

        /// <summary>
        /// Xác định mức độ nghiêm trọng của bệnh nhân
        /// </summary>
        [ObservableProperty]
        private string severity; // Values: normal, urgent, emergency, critical

        [ObservableProperty]
        private string symptoms;

        [ObservableProperty]
        private double priorityScore; // Điểm để sắp xếp

        [ObservableProperty]
        private int queueOrder;


        // 4. Property 'Age' vẫn là read-only
        public int Age => DateOfBirth == default ? 0 : DateTime.Today.Year - DateOfBirth.Year - (DateTime.Today.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);
    }
}