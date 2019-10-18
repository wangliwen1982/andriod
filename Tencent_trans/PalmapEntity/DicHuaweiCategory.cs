using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tencent_trans.PalmapEntity
{
    public class DicHuaweiCategory
    {
        public int Category { get; set; }

        public int Height { get; set; }

        public string CategoryName { get; set; }

        //是否导出室内poi点位(true:导出)
        public bool IsPoi { get; set; }
    }
}
