using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.ModelVM.Payment
{
    public class StripeWebhookVM
    {
        public string EventType { get; set; } = null!;
        public string PaymentIntentId { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int BookingId { get; set; }
    }
}
