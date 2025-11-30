using CommunityToolkit.Mvvm.Messaging.Messages;
using HosipitalManager.MVVM.Models;
using HospitalManager.MVVM.Models;
using System.Collections.Generic;

namespace HosipitalManager.MVVM.Messages
{
    public class PrescriptionsLoadedMessage : ValueChangedMessage<List<Prescription>>
    {
        public PrescriptionsLoadedMessage(List<Prescription> value) : base(value) { }
    }
}