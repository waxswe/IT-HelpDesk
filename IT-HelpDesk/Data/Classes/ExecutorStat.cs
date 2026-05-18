using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IT_HelpDesk.Data.Classes
{
    public class ExecutorStat
    {
        public string FullName { get; set; }
        public string Profession { get; set; }
        public int TotalRequests { get; set; }
        public int AssignedRequests { get; set; }
        public int InProgressRequests { get; set; }
        public int CompletedRequests { get; set; }
        public string CompletionPercent { get; set; }
        public string LoadPercent { get; set; }
    }
}
