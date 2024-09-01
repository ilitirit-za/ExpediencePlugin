using System;
using System.Diagnostics;

namespace Expedience.Models
{
    public class CurrentDutyInfo
    {
        public CurrentDutyInfo(Stopwatch stopwatch)
        {
            _stopwatch = stopwatch;
            StartTime = DateTime.UtcNow;
        }

        private Stopwatch _stopwatch;
        public PlayerInfo Player { get; set; }
        public int TerritoryId { get; set; }
        public string PlaceName { get; set; }
        public string ContentName { get; set; }
        public List<GroupMemberInfo> PartyMembers { get; set; } = new();
        public DateTime StartTime { get; }
        public DateTime EndTime { get; private set; }
        public bool IsUnrestricted { get; set; }
        public bool HasNpcMembers { get; set; }
        public bool HasEcho { get; set; }

        public string DataCenter { get; set; }
        public bool IsMinILevel { get; set; }
		public int? ContentFinderConditionId { get; set; }

		public void EndDuty()
        {
            _stopwatch.Stop();
            EndTime = DateTime.UtcNow;
        }

        public TimeSpan GetDuration()
        { 
            return _stopwatch.Elapsed;
        }
    }
}
