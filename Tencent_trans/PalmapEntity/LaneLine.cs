using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tencent_trans.PalmapEntity
{
    public class LaneLine
    {
        public int OId { get; set; }
        public string Name { get; set; }
        public int FloorId { get; set; }

        /// <summary>
        /// 道路方向。0表示双向，1表示顺方向（延画线方向），2表示逆方向（延道路方向）
        /// </summary>
        public int Direction { get; set; }

        /// <summary>
        /// 道路类型。1表示步行，2表示汽车通行
        /// </summary>
        public int Detail { get; set; }

        public int Weight { get; set; }
        public int StartPoint { get; set; }
        public int EndPoint { get; set; }
        public decimal Length { get; set; }

    }
}
