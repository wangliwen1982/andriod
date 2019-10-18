using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using Tencent_trans.PalmapEntity;

namespace Tencent_trans.Common
{
    /// <summary>
    /// 华为Mdb数据导出(texture)
    /// </summary>
    public class PalmapMdbData
    {
        private readonly AccessHelper _accessDb;

        private readonly NpgsqlHelper _ngpDb;

        public PalmapMdbData(string mdbPath)
        {
            var conn = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={mdbPath};Persist Security Info=False";
            _accessDb = new AccessHelper(conn);
            var connStr = ConfigurationManager.ConnectionStrings["Palmap"].ConnectionString;
            _ngpDb = new NpgsqlHelper(connStr);
        }

        public Mall GetMall()
        {
            var mallTable = _accessDb.ExecuteRow("select * from Mall");
            var mall = mallTable.AsEnumerable().Select(_ => new Mall
            {
                //UDID = _["UDID"] == DBNull.Value ? 0 : _.Field<int>("UDID"),
                NameCN = _.Field<string>("NameCN") == null
                    ? null
                    : _.Field<string>("NameCN").Replace("（", "(").Replace("）", ")"),
                NameEN = _.Field<string>("NameEN") == null
                    ? null
                    : _.Field<string>("NameEN").Replace("（", "(").Replace("）", ")"),
                ShowName = _.Field<string>("ShowName") == null
                    ? null
                    : _.Field<string>("ShowName").Replace("（", "(").Replace("）", ")"),
                MallType = _.Field<string>("MallType"),
                Description = _.Field<string>("Description"),
                Country = _.Field<string>("Country"),
                Province = _.Field<string>("Province"),
                City = _.Field<string>("City"),
                District = _.Field<string>("District"),
                BusinessDistrict = _.Field<string>("BusinessDistrict"),
                Address = _.Field<string>("Address") == null
                    ? null
                    : _.Field<string>("Address").Replace("（", "(").Replace("）", ")"),
                ZipCode = _.Field<string>("ZipCode"),
                AreaCode = _.Field<string>("AreaCode"),
                Phone = _.Field<string>("Phone"),
                WebSite = _.Field<string>("WebSite"),
                Email = _.Field<string>("Email"),
                OpeningTime = _.Field<string>("OpeningTime"),
                DefaultFloor = _.Field<string>("DefaultFloor"),
                Latitude = Convert.ToDecimal(_["Latitude"]),
                Longitude = Convert.ToDecimal(_["Longitude"]),
                //Membership = Convert.ToBoolean("Membership"),
                MembershipPolicy = _.Field<string>("MembershipPolicy"),
                ByBus = _.Field<string>("ByBus"),
                ByMetro = _.Field<string>("ByMetro"),
                ParkingCharge = _.Field<string>("ParkingCharge") ?? string.Empty,
                HwId = _["HW_ID"]?.ToString()
                //Office_hours= _.Field<string>("OpeningTime")
            }).First();
            return mall;
        }

        public List<Floor> GetFloors()
        {
            var floorTable = _accessDb.ExecuteRow(
                "select f.id,f.seq,f.number,f.name,f.nameen,f.huaweiflid,a.altitude,f.style from floors f left join altitude a on f.altitudeid=a.id");
            var floors = floorTable.AsEnumerable().Select(_ => new Floor
            {
                Floorid = Convert.ToInt32(_["id"]),
                Seq = Convert.ToSByte(_["seq"]),
                Number = _.Field<string>("number"),
                Name = _.Field<string>("name"),
                NameEn = _.Field<string>("nameen"),
                Style = _.Field<string>("style")?.Replace("（", "(").Replace("）", ")"),
                Altitude = _["altitude"] == DBNull.Value ? 0 : Convert.ToInt32(_["altitude"]),
                HuaweiFlid = _["huaweiflid"]?.ToString()
                //Floorid = _.Field<int>("id"),
                //Seq = _.Field<short>("seq"),
                //Number = _.Field<string>("number"),
                //Name = _.Field<string>("name"),
                //Style = _.Field<string>("style") == null
                //    ? null
                //    : _.Field<string>("style").Replace("（", "(").Replace("）", ")"),
                //Altitude =Convert.ToDecimal("altitude")
            }).ToList();
            return floors;
        }

        public List<DataQuality> GetDataqualitys()
        {
            var dataqualityTable = _accessDb.ExecuteRow("select * from dataquality order by id");
            var dataqualitys = dataqualityTable.AsEnumerable().Select(_ => new DataQuality()
            {
                Collector = _["collector"]?.ToString(),
                RenewData = _["renewdate"] != DBNull.Value
                    ? _.Field<DateTime>("renewdate").ToString("yyyyMMdd")
                    : string.Empty,
                MapSouce = _["mapsource"]?.ToString(),
                Renewer = _["renewer"]?.ToString()
            }).ToList();
            return dataqualitys;
        }

        public List<Frame> GetFrames()
        {
            var frameTable =
                _accessDb.ExecuteRow("select objectid,floorid from frame_polygon");
            var frames = frameTable.AsEnumerable().Select(_ =>
                new Frame
                {
                    Oid = _.Field<int>("objectid"),
                    Floorid = _.Field<int>("floorid")
                }).ToList();
            return frames;
        }

        public List<ShopPolygon> GetShopPolygon()
        {
            var shopPolygonTable =
                _accessDb.ExecuteRow(
                    "SELECT sp.objectid,sp.floorid,sp.shopid,sp.categoryid,s.display,s.shopnamecn," +
                    "s.shopnameen,s.categoryid as shopcategoryid,s.texture,s.roomnum " +
                    "FROM shop_polygon sp " +
                    "LEFT JOIN shops s ON sp.shopid = s.id");
            var shopPolygons = shopPolygonTable.AsEnumerable().Select(_ =>
                new ShopPolygon
                {
                    Oid = _.Field<int>("objectid"),
                    Floorid = _["floorid"] != DBNull.Value ? Convert.ToInt32(_["floorid"]) : 0,
                    Shopid = _["shopid"] != DBNull.Value ? Convert.ToInt32(_["shopid"]) : 0,
                    Categoryid = _["categoryid"] == DBNull.Value
                        ? _.Field<int>("shopcategoryid")
                        : _.Field<int>("categoryid"),
                    Display = _.Field<string>("display"),
                    Shopnamecn = _.Field<string>("shopnamecn"),
                    Shopnameen = _.Field<string>("shopnameen"),
                    Texture = _["texture"] == DBNull.Value ? 0 : _.Field<int>("texture"),
                    RoomNum = _["roomnum"]?.ToString()
                }).ToList();
            return shopPolygons;
        }

        public List<Shops> GetShops()
        {
            var shopsTable =
                _accessDb.ExecuteRow("select * from shops");
            var shops = shopsTable.AsEnumerable().Select(_ => new Shops
            {
                ShopId = _.Field<int>("id"),
                Booth = _.Field<string>("booth"),
                ShopNameCn = _.Field<string>("shopnamecn")?.Trim().Replace("（", "(").Replace("）", ")"),
                ShopNameEn = _.Field<string>("shopnameen")?.Trim().Replace("（", "(").Replace("）", ")"),
                Display = _.Field<string>("display")?.Replace("（", "(").Replace("）", ")"),
                CategoryId = _["categoryid"] != DBNull.Value ? _.Field<int>("categoryid") : 0,
                FloorId = _["floorid"] != DBNull.Value ? _.Field<int>("floorid") : 0,
                RoomNum = _.Field<string>("roomnum")?.Replace("（", "(").Replace("）", ")"),
                BrandId = _.Field<int>("brandid")
            }).ToList();
            return shops;
        }

        public List<Connection> GetConnections()
        {
            var dicEscalator = GetDicEscaCategory();

            var connectionTable = _accessDb.ExecuteRow("select * from connections");
            var connections = connectionTable.AsEnumerable().Select(_ => new Connection
            {
                From = _.Field<int>("id"),
                Type = _.Field<int>("type"),
                To = _["to"] == DBNull.Value ? 0 : _.Field<int>("to"),
                Usage = _["usage"] == DBNull.Value ? 0 : _.Field<int>("usage"),
                FloorId = _.Field<int>("floorid"),
                OneWay = dicEscalator[_.Field<int>("type")].OneWay
            }).ToList();
            return connections;
        }

        public List<LaneLine> GetLaneLine()
        {
            var lanelineTable = _accessDb.ExecuteRow("Select * from lane_line");
            var lanelines = lanelineTable.AsEnumerable().Select(_ => new LaneLine
            {
                OId = _.Field<int>("objectid"),
                Name = _.Field<string>("name"),
                FloorId = _.Field<int>("floorid"),
                Direction = Convert.ToInt32(_["direction"]), // == DBNull.Value ? 0 : _.Field<int>("direction"),
                Detail = _.Field<int>("detail"),
                Weight = _.Field<int>("weight"),
                StartPoint = _.Field<int>("startpointid"),
                EndPoint = _.Field<int>("endpointid"),
                Length = Convert.ToDecimal(_["shape_Length"])
            }).ToList();
            return lanelines;
        }

        public List<LaneLinePoint> GetLaneLinePoint()
        {
            var lanelinePointTable = _accessDb.ExecuteRow("Select * from lanepoint_point");
            var lanelinePoints = lanelinePointTable.AsEnumerable().Select(_ => new LaneLinePoint
            {
                OId = _.Field<int>("objectid"),
                Name = _.Field<string>("name") ?? string.Empty,
                FloorId = _.Field<int>("floorid"),
                DoorId = _["doorid"] == DBNull.Value ? 0 : _.Field<int>("doorid"),
                EscalatorId = _["escalatorid"] == DBNull.Value ? 0 : _.Field<int>("escalatorid"),
                PublicServiceId = _["publicserviceid"] == DBNull.Value ? 0 : _.Field<int>("publicserviceid"),
            }).ToList();
            return lanelinePoints;
        }

        public List<PublicServicePoint> GetPublicServicePoints()
        {
            var dicPublic = GetDicPublicService();

            var pspTable = _accessDb.ExecuteRow("select * from publicservice_point");
            var publicservices = pspTable.AsEnumerable().Select(_ => new PublicServicePoint
            {
                OId = _.Field<int>("objectid"),
                Name = _["name"]?.ToString(),
                Type = _["type"] != DBNull.Value ? _.Field<int>("type") : 0,
                FloorId = _.Field<int>("floorid"),
                CategoryId = _["categoryid"] == DBNull.Value || Convert.ToInt32(_["categoryid"]) == 0
                    ? dicPublic[Convert.ToInt32(_["type"])].CategoryId
                    : Convert.ToInt32(_["categoryid"]),
                SubKind = _["sub_kind"]?.ToString()
            }).ToList();
            return publicservices;
        }

        public List<EscalatorPoint> GetEscalatorPoints()
        {
            var epTable =
                _accessDb.ExecuteRow("select e.objectid,e.floorid,e.connectionid,c.type,c.name from escalator_point e inner join connections c on e.connectionid=c.id");
            var escalators = epTable.AsEnumerable().Select(_ => new EscalatorPoint
            {
                OId = _.Field<int>("objectid"),
                FloorId = _.Field<int>("floorid"),
                ConnectionId = _.Field<int>("connectionid"),
                Type = _.Field<int>("type"),
                Name = _["name"]?.ToString()
            }).ToList();
            return escalators;
        }

        public List<DoorPoint> GetDoorPoints()
        {
            var dpTable = _accessDb.ExecuteRow("select * from door_point");
            var doors = dpTable.AsEnumerable().Select(_ => new DoorPoint
            {
                OId = _.Field<int>("objectid"),
                Name = _.Field<string>("name"),
                ShopPolygonId = _["shoppolygonid"] != DBNull.Value ? _.Field<int>("shoppolygonid") : 0,
                Type = _.Field<int>("type"),
                FloorId = _.Field<int>("floorid")
            }).ToList();
            return doors;
        }

        public List<AreaPolygon> GetAreaPolygons()
        {
            var areaPolygonTable = _accessDb.ExecuteRow(
                "SELECT ap.objectid,ap.shopid,ap.floorid,ap.categoryid,s.display,s.shopnamecn," +
                "s.shopnameen,s.categoryid as shopcategoryid,s.texture,s.roomnum " +
                "FROM area_polygon ap " +
                "LEFT JOIN shops s ON ap.booth = s.booth");
            var areaPolygons = areaPolygonTable.AsEnumerable().Select(_ =>
                new AreaPolygon
                {
                    OId = _.Field<int>("objectid"),
                    FloorId = _["floorid"] != DBNull.Value ? _.Field<int>("floorid") : 0,
                    ShopId = _["shopid"] == DBNull.Value ? 0 : _.Field<int>("shopid"),
                    CategoryId = _["categoryid"] != DBNull.Value
                        ?Convert.ToInt32(_["categoryid"])
                        :Convert.ToInt32(_["shopcategoryid"]),
                    Display = _.Field<string>("display"),
                    ShopNameCn = _["shopnamecn"]?.ToString(),
                    ShopNameEn = _["shopnameen"]?.ToString(),
                    Texture = _["texture"] == DBNull.Value ? 0 : _.Field<int>("texture"),
                    RoomNum = _["roomnum"]?.ToString()
                }).ToList();
            return areaPolygons;
        }

        /// <summary>
        /// 获取数据库中dic_escalator数据.
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, DicEscalator> GetDicEscaCategory()
        {
            var table = _ngpDb.ExecuteDataTable("select id,display,oneway,categoryid from sde.dic_escalatortype")
                .AsEnumerable().Select(_ => new DicEscalator
                {
                    Type = _.Field<int>("id"),
                    Display = _["display"]?.ToString(),
                    OneWay = _["oneway"] != DBNull.Value && Convert.ToInt32(_["oneway"]) != 0,
                    CategoryId = _.Field<int>("categoryid"),
                }).ToDictionary(_ => _.Type);
            return table;
        }

        /// <summary>
        /// 获取数据库中dic_escalator数据.
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, DicPublicService> GetDicPublicService()
        {
            var table = _ngpDb.ExecuteDataTable("select objectid,display,categoryid from sde.dic_publicservicetype")
                .AsEnumerable().Select(_ => new DicPublicService
                {
                    Type = _.Field<int>("objectid"),
                    Display = _["display"]?.ToString(),
                    CategoryId = _.Field<int>("categoryid"),
                }).ToDictionary(_ => _.Type);
            return table;
        }

        /// <summary>
        /// 获取数据库中dic_category数据.
        /// </summary>
        /// <returns></returns>
        public List<DicCategory> GetDicCategory()
        {
            var table = _ngpDb.ExecuteDataTable("select id,category,subcategory from sde.dic_category")
                .AsEnumerable().Select(_ => new DicCategory
                {
                    CategoryId = _.Field<int>("id"),
                    CategoryName = _["category"]?.ToString(),
                    SubCategory = _["subcategory"]?.ToString(),
                }).ToList();
            return table;
        }

    }
}
