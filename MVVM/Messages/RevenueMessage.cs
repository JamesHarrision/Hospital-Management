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
        // Constructor 1: Nhận vào 1 Tuple (Cách dùng cũ: new RevenueUpdateMessage((amount, date)))
        public RevenueUpdateMessage((decimal Amount, DateTime Date) value) : base(value) { }

        // Constructor 2: Nhận vào 2 tham số riêng biệt (Cái bạn đang cần bổ sung)
        // Cách dùng: new RevenueUpdateMessage(500000, DateTime.Now)
        public RevenueUpdateMessage(decimal amount, DateTime date) : base((amount, date))
        {
        }
    }
}