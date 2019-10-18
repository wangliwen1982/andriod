using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tencent_trans.PalmapEntity
{
    public class Shops
    {
        public int ShopId { get; set; }
        public string Booth { get; set; }
        public string ShopNameCn { get; set; }
        public string ShopNameEn { get; set; }
        public string Display { get; set; }
        public int CategoryId { get; set; }
        public int FloorId { get; set; }
        public string RoomNum { get; set; }
        public string Phone { get; set; }
        public int BrandId { get; set; }
    }
}
