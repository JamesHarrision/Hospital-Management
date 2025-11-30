using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HosipitalManager.MVVM.Messages
{
    public class RevenueUpdateMessage : ValueChangedMessage<(decimal Amount, DateTime Date)>
    {
        public RevenueUpdateMessage((decimal Amount, DateTime Date) value) : base(value) { }
    }
}
