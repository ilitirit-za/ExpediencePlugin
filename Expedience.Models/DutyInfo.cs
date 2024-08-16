using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expedience.Models
{
    public class DutyInfo
    {
        public int TerritoryId { get; set; }
        public string ContentName { get; set; }
        public bool IsUnrestricted { get; set; }
        public bool HasEcho { get; set; }
        public bool IsNpcSupported { get; set; }
        public bool IsMinILevel { get; set; }
        public string PlaceName { get; set; }
    }
}
