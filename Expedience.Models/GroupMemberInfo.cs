using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expedience.Models
{
    public class GroupMemberInfo
    {
        public string ClassJob { get; set; }
        public bool IsNpc { get; set; }
        public int Level { get; set; }
        public int GroupNumber { get; set; }
        public bool IsPlayer { get; set; }
    }
}
