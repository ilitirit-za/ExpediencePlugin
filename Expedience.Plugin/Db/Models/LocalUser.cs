using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expedience.Db.Models
{
    public class LocalUser
    {
        public int WorldId { get; set; }
        public string UserHash { get; set; }
        public string UserName { get; set; }
    }
}
