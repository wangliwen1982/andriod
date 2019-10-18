using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tencent_trans.PalmapEntity
{
    public class EscalatorPoint
    {
        public int OId { get; set; }
        public int ObjId { get; set; }
        public int FloorId { get; set; }
        public int ConnectionId { get; set; }
        public int Type { get; set; }
        public string Name { get; set; }
        public int CategoryId { get; set; } = 0;
    }
}
