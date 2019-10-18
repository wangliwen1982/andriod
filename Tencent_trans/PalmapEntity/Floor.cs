using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tencent_trans.PalmapEntity
{
    public class Floor
    {
        public int Floorid { get; set; }

        //排序字段，使楼层按从最低到最高排列；必填字段
        public short Seq { get; set; }

        public string Number { get; set; }
        public string Name { get; set; }
        public string Style { get; set; }
        public decimal Altitude { get; set; }
        public string NameEn { get; set; }
        //华为楼层id
        public string HuaweiFlid { get; set; }
    }
}
