using HosipitalManager.MVVM.Models;
using HospitalManager.MVVM.Models;
using System.Collections.Generic;

namespace HospitalManager.MVVM.Messages;

/// <summary>
/// Message được gửi khi cần check-in một appointment vào hàng đợi khám
/// </summary>
public class RequestCheckInMessage
{
    public Appointment Appointment { get; }

    public RequestCheckInMessage(Appointment appointment)
    {
        Appointment = appointment;
    }
}

/// <summary>
/// Message yêu cầu refresh Dashboard
/// </summary>
public class DashboardRefreshMessage
{
}

