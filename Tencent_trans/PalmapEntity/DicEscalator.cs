using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tencent_trans.PalmapEntity
{
    public class DicEscalator
    {
        public int Type { get; set; }
        public string Display { get; set; }
        //true：单向;false:双向
        public bool OneWay { get; set; }
        public int CategoryId { get; set; }
    }
}
