using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HosipitalManager.MVVM.Messages
{
    public class ReloadPrescriptionsMessage : ValueChangedMessage<bool>
    {
       public ReloadPrescriptionsMessage(bool value) : base(value) { }
    }
}
