using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HosipitalManager.MVVM.Services;
using CommunityToolkit.Mvvm.Messaging;
using HosipitalManager.MVVM.Models;
using HospitalManager.MVVM.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using HosipitalManager.Helpers;


namespace HospitalManager.MVVM.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    #region Properties - Pagination & Lists
    private const int PageSize = 10;

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
    public ObservableCollection<Patient> Patients { get; set; } = new();
    public ObservableCollection<Patient> FilteredPatients { get; set; } = new();
    #endregion
    private Appointment _pendingCheckInAppointment;

    #region Properties - Search
    [ObservableProperty]
    private string searchText;
    // Hàm này tự động chạy khi SearchText thay đổi (tính năng của MVVM Toolkit)
    partial void OnSearchTextChanged(string value)
    {
        Task.Run(async () => await SearchPatient());
    }
    #endregion

    // Hàm lấy dữ liệu từ SQLite
    public async Task LoadPatients()
    {
        // ... (Code tính toán phân trang giữ nguyên) ...
        int totalCount = await _databaseService.GetPatientCountAsync();
        PatientTotalPages = (int)Math.Ceiling((double)totalCount / PageSize);

        if (PatientCurrentPage < 1) PatientCurrentPage = 1;
        if (PatientCurrentPage > PatientTotalPages && PatientTotalPages > 0) PatientCurrentPage = PatientTotalPages;

        var patientList = await _databaseService.GetPatientsPagedAsync(PatientCurrentPage, PageSize);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Patients.Clear();
            FilteredPatients.Clear();

            foreach (var patient in patientList)
            {
                Patients.Add(patient);
                FilteredPatients.Add(patient);
            }

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
        // --- VALIDATION ---
        if (!ValidationHelper.IsValidName(newPatientFullName))
        {
            await Application.Current.MainPage.DisplayAlert("Lỗi nhập liệu",
            "Họ tên không hợp lệ. Vui lòng không nhập số hoặc ký tự đặc biệt.", "OK");
            return;
        }
        if (!ValidationHelper.IsValidPhoneNumber(newPatientPhoneNumber))
        {
            await Application.Current.MainPage.DisplayAlert("Lỗi nhập liệu",
            "Số điện thoại phải bắt đầu bằng số 0 và đủ 10 chữ số.", "OK");
            return;
        }
        var dobCheck = ValidationHelper.IsValidDateOfBirth(newPatientDateOfBirth);
        if (!dobCheck.IsValid)
        {
            await Application.Current.MainPage.DisplayAlert("Lỗi ngày sinh", dobCheck.Message, "OK");
            return;
        }
        if (!ValidationHelper.IsValidLength(NewPatientAddress, 5))
        {
            await Application.Current.MainPage.DisplayAlert("Lỗi nhập liệu",
                "Địa chỉ quá ngắn, vui lòng nhập cụ thể hơn.", "OK");
            return;
        }
        // --- HẾT VALIDATION, BẮT ĐẦU XỬ LÝ DATABASE ---
        try
        {
            Patient patientToSave;
            string severityCode = GetSeverityCode(NewPatientSeverity);

            // --- TRƯỜNG HỢP 1: ĐANG SỬA (EDIT MODE) ---
            if (isEditing && patientToEdit != null)
            {
                patientToSave = patientToEdit;
                UpdatePatientInfo(patientToSave, severityCode);

                // Lưu cập nhật
                await _databaseService.SavePatientAsync(patientToSave);
            }
            // --- TRƯỜNG HỢP 2: THÊM MỚI (ADD MODE) ---
            else
            {
                // 1. Kiểm tra xem SĐT đã tồn tại chưa
                var allPatients = await _databaseService.GetPatientsAsync();
                var duplicatePhone = allPatients.FirstOrDefault(p => p.PhoneNumber == NewPatientPhoneNumber);

                if (duplicatePhone != null)
                {
                    // NẾU TRÙNG SĐT -> HỎI NGƯỜI DÙNG (Thay vì tự động ghi đè)
                    bool continueAdd = await Application.Current.MainPage.DisplayAlert(
                        "Trùng số điện thoại",
                        $"Số điện thoại {NewPatientPhoneNumber} đã tồn tại (Bệnh nhân: {duplicatePhone.FullName}).\nBạn có muốn tạo hồ sơ MỚI không?",
                        "Tạo mới",
                        "Hủy");

                    if (!continueAdd) return; // Nếu hủy thì dừng lại
                }

                // 2. Tạo đối tượng mới hoàn toàn
                patientToSave = new Patient();
                UpdatePatientInfo(patientToSave, severityCode);

                // 3. Tạo ID Mới (Logic BNxxxx chuẩn của bạn)
                int nextNumber = 1000;
                if (allPatients.Count > 0)
                {
                    var maxId = allPatients
                        .Where(p => !string.IsNullOrEmpty(p.Id) && p.Id.StartsWith("BN"))
                        .Select(p =>
                        {
                            // Xử lý an toàn hơn để tránh lỗi nếu ID không đúng định dạng
                            if (int.TryParse(p.Id.Substring(2), out int n)) return n;
                            return 0;
                        })
                        .DefaultIfEmpty(0) // Tránh lỗi nếu danh sách rỗng sau filter
                        .Max();
                    nextNumber = maxId + 1;
                }
                patientToSave.Id = $"BN{nextNumber}";

                // 4. Các thông tin mặc định
                patientToSave.AdmittedDate = DateTime.Now;
                patientToSave.Status = "Chờ khám";

                // Xử lý Bác sĩ & Lịch hẹn (Giữ nguyên logic của bạn)
                if (_pendingCheckInAppointment != null)
                {
                    // 1. Lấy tên bác sĩ (như cũ)
                    patientToSave.Doctorname = _pendingCheckInAppointment.DoctorName;

                    // 2. Đổi trạng thái lịch hẹn thành Completed
                    _pendingCheckInAppointment.Status = AppointmentStatus.Completed;

                    // 3. QUAN TRỌNG: Lưu trạng thái mới này xuống Database
                    // (Nếu thiếu dòng này, tắt app mở lại lịch hẹn vẫn hiện lù lù)
                    await _databaseService.UpdateAppointmentAsync(_pendingCheckInAppointment);

                    // 4. Gửi tin nhắn để màn hình Lịch (DashboardContentView) vẽ lại (xóa cái thẻ lịch đi)
                    WeakReferenceMessenger.Default.Send(new DashboardRefreshMessage());

                    // 5. Reset biến tạm
                    _pendingCheckInAppointment = null;
                }
                else if (SelectedDoctor != null)
                    patientToSave.Doctorname = SelectedDoctor.Name;
                else
                    patientToSave.Doctorname = "Chưa chỉ định";

                // Tính điểm
                patientToSave.PriorityScore = CalculatePriority(patientToSave);

                // 5. LƯU MỚI VÀO DB
                await _databaseService.SavePatientAsync(patientToSave);
            }

            // --- CẬP NHẬT GIAO DIỆN ---
            // Gọi 2 hàm Load riêng biệt như đã bàn trước đó
            await LoadPatients();     // Cập nhật list tổng
            await LoadWaitingQueue(); // Cập nhật hàng đợi

            // Dọn dẹp
            IsAddPatientPopupVisible = false;
            patientToEdit = null;
            ClearPopupForm();
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Lỗi", "Chi tiết: " + ex.Message, "OK");
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

    public class RequestCheckInMessage
    {
        public Appointment Appointment { get; set; }
        public RequestCheckInMessage(Appointment appt) { Appointment = appt; }
    }

    private async Task LoadWaitingQueue()
    {
        if (_databaseService == null) return;

        // Lấy toàn bộ danh sách bệnh nhân
        var allPatients = await _databaseService.GetPatientsAsync();

        // Lọc ra những người có trạng thái "Chờ khám"
        var waitingList = allPatients.Where(p => p.Status == "Chờ khám").ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Khởi tạo nếu chưa có (chỉ chạy 1 lần đầu)
            if (WaitingQueue == null) WaitingQueue = new ObservableCollection<Patient>();

            // Xóa danh sách cũ đi để nạp mới (Tránh trùng lặp)
            WaitingQueue.Clear();

            // Thêm lại từng người từ DB
            foreach (var p in waitingList)
            {
                WaitingQueue.Add(p);
            }

            SortPatientQueue(); // Sắp xếp lại
        });
    }
}