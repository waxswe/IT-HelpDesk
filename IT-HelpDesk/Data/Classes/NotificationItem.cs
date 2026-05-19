using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IT_HelpDesk.Data.Classes
{
    public class NotificationItem
    {
        public int NotificationID { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public int? RequestID { get; set; }
        public int? InitiatorID { get; set; }
        public string TemplateKey { get; set; }
    }
}
