using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HosipitalManager.MVVM.Services;
using CommunityToolkit.Mvvm.Messaging;
using HosipitalManager.MVVM.Models;
using HospitalManager.MVVM.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;


namespace HospitalManager.MVVM.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    //PAGINATION 
    // Cấu hình số dòng mỗi trang
    private const int PageSize = 10;

    // --- CÁC BIẾN PHÂN TRANG BỆNH NHÂN ---
    [ObservableProperty]
    private int patientCurrentPage = 1;

    [ObservableProperty]
    private int patientTotalPages = 1;

    [ObservableProperty]
    private string patientPageInfo; // Hiển thị "Trang 1 / 5"

    [ObservableProperty]
    private bool canPatientGoBack;

    [ObservableProperty]
    private bool canPatientGoNext;

    //END PAGINATION


    // Database chính thức
    //Danh sách bệnh nhân để hiển thị lên màn hình (Binding)
    public ObservableCollection<Patient> Patients { get; set; } = new();

    // Hàm lấy dữ liệu từ SQLite
    public async Task LoadPatients()
    {
        int totalCount = await _databaseService.GetPatientCountAsync();
        PatientTotalPages = (int)Math.Ceiling((double)totalCount / PageSize); // Tính tổng số trang (lấy tổng số bệnh nhân / số dòng mỗi trang)

        // Đảm bảo trang hiện tại hợp lệ
        if (PatientCurrentPage < 1) PatientCurrentPage = 1;
        if (PatientCurrentPage > PatientTotalPages && PatientTotalPages > 0) PatientCurrentPage = PatientTotalPages;

        var patientList = await _databaseService.GetPatientsPagedAsync(PatientCurrentPage, PageSize);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Patients.Clear();
            FilteredPatients.Clear();
            WaitingQueue.Clear();
            foreach (var patient in patientList)
            {
                Patients.Add(patient);
                FilteredPatients.Add(patient);

                if (patient.Status == "Chờ khám")
                {
                    WaitingQueue.Add(patient);
                }
            }
            SortPatientQueue();

            UpdatePatientPaginationUI();
        });
    }

    private void UpdatePatientPaginationUI()
    {
        PatientPageInfo = $"Trang {PatientCurrentPage} / {PatientTotalPages}";
        CanPatientGoBack = PatientCurrentPage > 1;
        CanPatientGoNext = PatientCurrentPage < PatientTotalPages;
    }

    // --- CÁC LỆNH CHUYỂN TRANG ---
    [RelayCommand]
    private async Task NextPatientPage()
    {
        if (PatientCurrentPage < PatientTotalPages)
        {
            PatientCurrentPage++;
            await LoadPatients();
        }
    }

    [RelayCommand]
    private async Task PreviousPatientPage()
    {
        if (PatientCurrentPage > 1)
        {
            PatientCurrentPage--;
            await LoadPatients();
        }
    }

    [RelayCommand]
    public async Task SavePatient()
    {
        // 1. Validate dữ liệu đầu vào (Cơ bản)
        if (string.IsNullOrWhiteSpace(NewPatientFullName))
        {
            await Application.Current.MainPage.DisplayAlert("Lỗi", "Vui lòng nhập họ tên", "OK");
            return;
        }

        try
        {
            Patient patientToSave;
            string severityCode = GetSeverityCode(NewPatientSeverity);
            bool isNewRecord = false;

            // --- TRƯỜNG HỢP 1: SỬA THÔNG TIN (Edit) ---
            if (isEditing && patientToEdit != null)
            {
                patientToSave = patientToEdit;
                // Cập nhật các trường thông tin
                UpdatePatientInfo(patientToSave, severityCode);
            }
            // --- TRƯỜNG HỢP 2: THÊM MỚI / TIẾP NHẬN TỪ LỊCH HẸN ---
            else
            {
                isNewRecord = true;
                // Lấy danh sách từ DB để kiểm tra trùng và tạo ID (Logic Code 1)
                var allPatients = await _databaseService.GetPatientsAsync();

                // A. Kiểm tra bệnh nhân cũ dựa trên SĐT (Logic Code 2 nhưng dùng dữ liệu DB)
                var existingPatient = allPatients.FirstOrDefault(p => p.PhoneNumber == NewPatientPhoneNumber);

                if (existingPatient != null)
                {
                    // Nếu đã có hồ sơ -> Dùng lại ID cũ, tạo object mới để đẩy vào hàng đợi
                    patientToSave = existingPatient;
                    // Cập nhật thông tin mới nhất vào hồ sơ cũ
                    UpdatePatientInfo(patientToSave, severityCode);
                }
                else
                {
                    // Nếu chưa có -> Tạo mới hoàn toàn
                    patientToSave = new Patient();
                    UpdatePatientInfo(patientToSave, severityCode);

                    // B. Logic tạo ID tự động tăng (Logic Code 1 - Quan trọng)
                    int nextNumber = 1000;
                    if (allPatients.Count > 0)
                    {
                        var maxId = allPatients
                            .Select(p => p.Id)
                            .Where(id => !string.IsNullOrEmpty(id) && id.StartsWith("BN") && id.Length > 2)
                            .Select(id => int.TryParse(id.Substring(2), out int n) ? n : 0)
                            .Max();
                        nextNumber = maxId + 1;
                    }
                    patientToSave.Id = $"BN{nextNumber}";
                }

                // C. Thiết lập các thông số cho Hàng đợi & Lịch hẹn (Logic Code 2)
                patientToSave.AdmittedDate = DateTime.Now;
                patientToSave.QueueOrder = WaitingQueue.Count + 1;
                patientToSave.Status = "Chờ khám";

                // Lấy tên bác sĩ từ lịch hẹn (nếu có check-in từ lịch hẹn)
                patientToSave.Doctorname = _pendingCheckInAppointment.DoctorName ?? "Chưa chỉ định";

                // Xử lý Lịch hẹn (Đổi trạng thái & Gửi tin nhắn cập nhật Dashboard)
                if (_pendingCheckInAppointment != null)
                {
                    _pendingCheckInAppointment.Status = AppointmentStatus.Completed;

                    // Cập nhật trạng thái lịch hẹn vào DB (Nếu cần thiết)
                    // await _databaseService.UpdateAppointmentAsync(_pendingCheckInAppointment); 

                    WeakReferenceMessenger.Default.Send(new DashboardRefreshMessage());
                    _pendingCheckInAppointment = null; // Reset biến tạm
                }
            }

            // 3. Tính điểm ưu tiên (Logic Code 2)
            patientToSave.PriorityScore = CalculatePriority(patientToSave);

            // 4. Lưu vào Database (Logic Code 1)
            await _databaseService.SavePatientAsync(patientToSave);

            // 5. Cập nhật giao diện
            if (isNewRecord && !WaitingQueue.Contains(patientToSave))
            {
                WaitingQueue.Add(patientToSave);
            }
                
            SortPatientQueue(); // Sắp xếp lại hàng đợi
            await LoadPatients(); // Load lại danh sách tổng từ DB để đồng bộ

            // 6. Dọn dẹp form
            IsAddPatientPopupVisible = false;
            patientToEdit = null;
            ClearForm();

            // (Tùy chọn) Thông báo
            // await Shell.Current.DisplayAlert("Thành công", "Đã lưu thông tin bệnh nhân!", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi SavePatient: {ex.Message}");
            await Application.Current.MainPage.DisplayAlert("Lỗi", "Không thể lưu bệnh nhân: " + ex.Message, "OK");
        }
    }

    // Hàm phụ trợ để map dữ liệu (giúp code chính gọn hơn)
    private void UpdatePatientInfo(Patient p, string severityCode)
    {
        p.FullName = NewPatientFullName;
        p.DateOfBirth = NewPatientDateOfBirth;
        p.Gender = NewPatientGender;
        p.PhoneNumber = NewPatientPhoneNumber;
        p.Address = NewPatientAddress;
        p.Symptoms = NewPatientSymptoms;
        p.Severity = severityCode;

        // Nếu đang Edit thì giữ nguyên Status cũ, nếu New thì status sẽ được set ở logic chính
        if (isEditing)
        {
            p.Status = NewPatientStatus;
        }
    }

    private void ClearForm()
    {
        NewPatientFullName = string.Empty;
        NewPatientPhoneNumber = string.Empty;
        NewPatientAddress = string.Empty;
        NewPatientSymptoms = string.Empty;
        NewPatientDateOfBirth = DateTime.Today;
    }

    public ObservableCollection<Patient> FilteredPatients { get; set; } = new();

    [ObservableProperty]
    private string searchText;

    // Hàm này tự động chạy khi SearchText thay đổi (tính năng của MVVM Toolkit)
    partial void OnSearchTextChanged(string value)
    {
        Task.Run(async () => await SearchPatient());
    }
    // Hàm nạp dữ liệu mẫu (nếu cần)
    //private void LoadSamplePatients()
    //{
    //    // Tạo danh sách dữ liệu mẫu
    //    var samples = new List<Patient>
    //{
    //    new Patient
    //    {
    //        Id = "BN001",
    //        FullName = "Nguyễn Văn An",
    //        DateOfBirth = new DateTime(1990, 5, 15),
    //        Gender = "Nam",
    //        PhoneNumber = "0901234567",
    //        Address = "123 Đường ABC, Q.1, TP.HCM",
    //        AdmittedDate = DateTime.Today.AddDays(-5),
    //        Status = "Đang điều trị",
    //        Severity = "normal"
    //    },
    //    new Patient
    //    {
    //        Id = "BN002",
    //        FullName = "Trần Thị Bích",
    //        DateOfBirth = new DateTime(1995, 8, 20),
    //        Gender = "Nữ",
    //        PhoneNumber = "0909876543",
    //        Address = "456 Đường Lê Lợi, Q.1, TP.HCM",
    //        AdmittedDate = DateTime.Today.AddDays(-2),
    //        Status = "Đang điều trị",
    //        Severity = "critical"
    //    },
    //    new Patient
    //    {
    //        Id = "BN003",
    //        FullName = "Lê Văn Cường",
    //        DateOfBirth = new DateTime(1985, 12, 10),
    //        Gender = "Nam",
    //        PhoneNumber = "0912345678",
    //        Address = "789 Đường Nguyễn Trãi, Q.5, TP.HCM",
    //        AdmittedDate = DateTime.Today.AddDays(-10),
    //        Status = "Đang điều trị",
    //        Severity = "medium"
    //    },
    //    new Patient
    //    {
    //        Id = "BN004",
    //        FullName = "Phạm Minh Duy",
    //        DateOfBirth = new DateTime(2001, 3, 5),
    //        Gender = "Nam",
    //        PhoneNumber = "0987654321",
    //        Address = "321 Đường Trần Hưng Đạo, Q.1, TP.HCM",
    //        AdmittedDate = DateTime.Today.AddDays(-1),
    //        Status = "Chờ khám",
    //        Severity = "normal"
    //    },
    //    new Patient
    //    {
    //        Id = "BN005",
    //        FullName = "Hoàng Thị Em",
    //        DateOfBirth = new DateTime(1978, 7, 25),
    //        Gender = "Nữ",
    //        PhoneNumber = "0933445566",
    //        Address = "654 Đường 3/2, Q.10, TP.HCM",
    //        AdmittedDate = DateTime.Today.AddDays(-15),
    //        Status = "Đã xuất viện",
    //        Severity = "normal"
    //    },
    //    new Patient
    //    {
    //        Id = "BN006",
    //        FullName = "Ngô Văn Fương",
    //        DateOfBirth = new DateTime(1999, 11, 11),
    //        Gender = "Nam",
    //        PhoneNumber = "0977889900",
    //        Address = "12 Đường Phan Đăng Lưu, Q.Phú Nhuận",
    //        AdmittedDate = DateTime.Today.AddDays(-3),
    //        Status = "Đang điều trị",
    //        Severity = "critical"
    //    },
    //    new Patient
    //    {
    //        Id = "BN007",
    //        FullName = "Vũ Thị Giang",
    //        DateOfBirth = new DateTime(1982, 9, 9),
    //        Gender = "Nữ",
    //        PhoneNumber = "0966554433",
    //        Address = "99 Đường Võ Văn Ngân, TP.Thủ Đức",
    //        AdmittedDate = DateTime.Today.AddDays(-7),
    //        Status = "Đang điều trị",
    //        Severity = "medium"
    //    },
    //    new Patient
    //    {
    //        Id = "BN008",
    //        FullName = "Đặng Văn Hùng",
    //        DateOfBirth = new DateTime(1993, 4, 30),
    //        Gender = "Nam",
    //        PhoneNumber = "0944332211",
    //        Address = "55 Đường Phạm Văn Đồng, Q.Gò Vấp",
    //        AdmittedDate = DateTime.Today.AddDays(-4),
    //        Status = "Chờ phẫu thuật",
    //        Severity = "critical"
    //    },
    //    new Patient
    //    {
    //        Id = "BN009",
    //        FullName = "Bùi Thị Yến",
    //        DateOfBirth = new DateTime(1960, 1, 1),
    //        Gender = "Nữ",
    //        PhoneNumber = "0911223344",
    //        Address = "88 Đường Hậu Giang, Q.6, TP.HCM",
    //        AdmittedDate = DateTime.Today.AddDays(-20),
    //        Status = "Đang điều trị",
    //        Severity = "normal"
    //    },
    //    new Patient
    //    {
    //        Id = "BN010",
    //        FullName = "Đoàn Văn Khanh",
    //        DateOfBirth = new DateTime(2005, 6, 15),
    //        Gender = "Nam",
    //        PhoneNumber = "0999888777",
    //        Address = "22 Đường Lý Thường Kiệt, Q.Tân Bình",
    //        AdmittedDate = DateTime.Today.AddDays(0),
    //        Status = "Mới nhập viện",
    //        Severity = "medium"
    //    }
    //};

    //    // Thêm tất cả vào danh sách chính
    //    foreach (var p in samples)
    //    {
    //        Patients.Add(p);
    //        FilteredPatients.Add(p);
    //    }
    //}

    private async Task SearchPatient()
    {
        if(string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadPatients();
            return;
        }

        var searchResults = await _databaseService.SearchPatientAsync(SearchText);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            // 1. Dọn sạch danh sách hiển thị cũ
            Patients.Clear();
            FilteredPatients.Clear();

            // Lưu ý: Tạm thời không clear WaitingQueue khi tìm kiếm để tránh mất danh sách chờ
            // Hoặc nếu muốn đồng bộ thì xử lý riêng

            // 2. Đổ kết quả tìm được vào danh sách
            foreach (var p in searchResults)
            {
                Patients.Add(p);
                FilteredPatients.Add(p);
            }

            // 3. Cập nhật giao diện Phân trang để báo hiệu đang ở chế độ Tìm kiếm
            PatientPageInfo = $"Tìm thấy: {searchResults.Count} kết quả";

            // Vô hiệu hóa nút Next/Prev vì đang hiện tất cả kết quả rồi
            CanPatientGoBack = false;
            CanPatientGoNext = false;
        });
    }

    [RelayCommand]
    private void ShowEditPatientPopup(Patient patient)
    {
        if (patient == null) return;

        isEditing = true;
        PopupTitle = $"Sửa hồ sơ: {patient.FullName}";
        patientToEdit = patient;

        // Load dữ liệu lên form
        NewPatientFullName = patient.FullName;
        NewPatientDateOfBirth = patient.DateOfBirth;
        NewPatientGender = patient.Gender;
        NewPatientPhoneNumber = patient.PhoneNumber;
        NewPatientAddress = patient.Address;
        NewPatientStatus = patient.Status;
        // Map severity code back to Display Name if needed
        NewPatientSeverity = GetSeverityCode(patient.Severity);
        NewPatientSymptoms = patient.Symptoms;

        // MỞ KHÓA cho phép sửa trạng thái
        IsStatusEnabled = true;

        IsAddPatientPopupVisible = true;
    }

    [RelayCommand]
    private async Task DeletePatient(Patient patientToDelete)
    {
        if (patientToDelete == null) return;

        bool confirmed = await Application.Current.MainPage.DisplayAlert(
            "Xác nhận xóa",
            $"Bạn có chắc chắn muốn xóa hồ sơ '{patientToDelete.FullName}'?",
            "Xóa",
            "Hủy");

        if (confirmed)
        {
            await _databaseService.DeletePatientAsync(patientToDelete);

            Patients.Remove(patientToDelete);
            FilteredPatients.Remove(patientToDelete);
        }
    }


    private Appointment _pendingCheckInAppointment;

    // Message để nhận yêu cầu Check-in từ DashboardContentView
    public class RequestCheckInMessage
    {
        public Appointment Appointment { get; set; }
        public RequestCheckInMessage(Appointment appt) { Appointment = appt; }
    }

    private void OpenCheckInPopup(Appointment appt)
    {
        _pendingCheckInAppointment = appt;

        // 1. Điền thông tin từ Lịch hẹn vào các biến Binding của Popup
        PopupTitle = "Tiếp nhận Bệnh nhân"; // Đổi tiêu đề
        NewPatientFullName = appt.PatientName;
        NewPatientPhoneNumber = appt.PhoneNumber;
        NewPatientSymptoms = appt.Note;

        // Các trường mặc định khác
        NewPatientDateOfBirth = DateTime.Today; // Hoặc tính từ appt nếu có
        NewPatientAddress = "";
        NewPatientStatus = "Chờ khám";
        NewPatientSeverity = "Bình thường";

        // 2. Hiện Popup (Binding IsAddPatientPopupVisible = true)
        IsAddPatientPopupVisible = true;
    }

    public async Task LoadWaitingQueue()
    {
        // Gọi xuống Service mới viết ở Bước 1
        var waitingList = await _databaseService.GetWaitingPatientsAsync();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            WaitingQueue.Clear();
            foreach (var p in waitingList)
            {
                WaitingQueue.Add(p);
            }
            // Gọi lại hàm sắp xếp để chắc chắn đúng thứ tự
            SortPatientQueue();
        });
    }
}