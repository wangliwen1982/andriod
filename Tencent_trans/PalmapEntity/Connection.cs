using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tencent_trans.PalmapEntity
{
    public class Connection
    {
        public int From { get; set; }
        public int Type { get; set; }
        public string Name { get; set; }
        public int Usage { get; set; }
        public int To { get; set; }
        public int FloorId { get; set; }
        //true：单向;false:双向
        public bool OneWay { get; set; }
    }
}
