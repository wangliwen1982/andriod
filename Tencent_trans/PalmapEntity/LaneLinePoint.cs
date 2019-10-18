using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tencent_trans.PalmapEntity
{
    public class LaneLinePoint
    {
        public int OId { get; set; }
        public int ObjId { get; set; }
        public string Name { get; set; }
        public int FloorId { get; set; }
        public int DoorId { get; set; }
        public int EscalatorId { get; set; }
        public int PublicServiceId { get; set; }
        public string NodeId { get; set; }
    }
}
