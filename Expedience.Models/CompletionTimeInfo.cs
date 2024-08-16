using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expedience.Models
{
    public class CompletionTimeInfo
    {
        public CompletionTimeInfo()
        {
            
        }
        public CompletionTimeInfo(TimeSpan completionTime)
        {
            Hours = completionTime.Hours;
            Minutes = completionTime.Minutes;
            Seconds = completionTime.Seconds;
            Milliseconds = completionTime.Milliseconds;
            CompletionTimeText = completionTime.ToString(@"hh\:mm\:ss\.fff");
        }

        public int Hours { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }
        public int Milliseconds { get; set; }
        public string CompletionTimeText { get; set; }
    }
}
