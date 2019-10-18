using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tencent_trans.PalmapEntity
{
    public class Mall
    {
        public int UDID { get; set; }

        //华为id
        public string HwId { get; set; }
        public string MapID { get; set; }
        public string NameCN { get; set; }
        public string NameEN { get; set; }
        public string ShowName { get; set; }
        public string MallType { get; set; }
        public string Description { get; set; }
        public string Country { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string BusinessDistrict { get; set; }
        public string Address { get; set; }
        public string ZipCode { get; set; }
        public string AreaCode { get; set; }
        public string Phone { get; set; }
        public string WebSite { get; set; }
        public string Email { get; set; }
        public string OpeningTime { get; set; }
        public string DefaultFloor { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public int Membership { get; set; }
        public string MembershipPolicy { get; set; }
        public string ByBus { get; set; }
        public string ByMetro { get; set; }
        public int CarPark { get; set; }
        public int ParkingSpace { get; set; }
        public string ParkingCharge { get; set; }
    }
}
