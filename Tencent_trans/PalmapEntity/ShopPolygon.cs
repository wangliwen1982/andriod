using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tencent_trans.PalmapEntity
{
    public class ShopPolygon
    {
        public int Oid { get; set; }
        public int ObjId { get; set; }
        public int Floorid { get; set; }
        public int Categoryid { get; set; }
        public int? Shopid { get; set; }
        public int Texture { get; set; }
        public string Display { get; set; }
        public string Shopnamecn { get; set; }
        public string Shopnameen { get; set; }
        public string Type { get; set; }
        public string RoomNum { get; set; }
    }
}
