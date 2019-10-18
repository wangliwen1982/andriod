using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Tencent_trans.PalmapEntity;

namespace Tencent_trans.Common
{
    public class PalmapDataBaseData
    {
        private readonly NpgsqlHelper _palmapDb;

        public PalmapDataBaseData(NpgsqlHelper palmapDb)
        {
            _palmapDb = palmapDb;
        }

        public Mall GetMall(string mapid)
        {
            var mallTable = _palmapDb.ExecuteDataTable($"select * from Mall where mapid = '{mapid}'");
            var mall = mallTable.AsEnumerable().Select(_ => new Mall
            {
                UDID = _.Field<int>("UDID"),
                MapID = _.Field<string>("MapID"),
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
                //OpeningTime = _.Field<string>("OpeningTime"),
                OpeningTime = _.Field<string>("TencentTime"),
                DefaultFloor = _.Field<string>("DefaultFloor"),
                Latitude = _.Field<decimal>("Latitude"),
                Longitude = _.Field<decimal>("Longitude"),
                Membership = _.Field<Int16>("Membership"),
                MembershipPolicy = _.Field<string>("MembershipPolicy"),
                ByBus = _.Field<string>("ByBus"),
                ByMetro = _.Field<string>("ByMetro"),
                CarPark = _["CarPark"] == DBNull.Value ? 0 : _.Field<Int16>("CarPark"),
                ParkingSpace = _["ParkingSpace"] == DBNull.Value ? 0 : _.Field<Int16>("ParkingSpace"),
                ParkingCharge = _.Field<string>("ParkingCharge"),
                //Office_hours= _.Field<string>("OpeningTime")
            }).First();
            return mall;
        }

        public List<Floor> GetFloors(string mapid)
        {
            var floorTable = _palmapDb.ExecuteDataTable(
                $"select f.objid,f.seq,f.number,f.name,a.altitude,f.style from floors f left join altitude a on f.altitudeid=a.objid where f.mapid='{mapid}' and a.mapid='{mapid}' order by a.altitude ");
            var floors = floorTable.AsEnumerable().Select(_ => new Floor
            {
                Floorid = _.Field<int>("objid"),
                Seq = _.Field<short>("seq"),
                Number = _.Field<string>("number"),
                Name = _.Field<string>("name"),
                Style = _.Field<string>("style") == null
                    ? null
                    : _.Field<string>("style").Replace("（", "(").Replace("）", ")"),
                Altitude = _.Field<decimal>("altitude")
            }).ToList();
            return floors;
        }

        public List<DataQuality> GetDataqualitys(string mapid)
        {
            var dataqualityTable = _palmapDb
                .ExecuteDataTable($"select * from dataquality where mapid='{mapid}' order by objid");
            var dataqualitys = dataqualityTable.AsEnumerable().Select(_ => new DataQuality()
            {
                Collector = _.Field<string>("collector"),
                RenewData = _.Field<DateTime>("renewdate").ToString("yyyyMMdd"),
                MapSouce = _.Field<string>("mapsource"),
                Renewer = _.Field<string>("renewer")
            }).ToList();
            return dataqualitys;
        }

        public List<Frame> GetFrames(string mapid)
        {
            var frameTable =
                _palmapDb.ExecuteDataTable($"select objectid,floorid from frame_polygon where mapid='{mapid}'");
            var frames = frameTable.AsEnumerable().Select(_ =>
                new Frame
                {
                    Oid = _.Field<int>("objectid"),
                    Floorid = _.Field<int>("floorid")
                }).ToList();
            return frames;
        }

        public List<ShopPolygon> GetShopPolygon(string mapid)
        {
            var shopPolygonTable =
                _palmapDb.ExecuteDataTable(
                    "SELECT sp.objectid,sp.floorid,sp.shopid,sp.categoryid,s.display,s.shopnamecn," +
                    "sp.objid,s.shopnameen,s.categoryid as shopcategoryid " +
                    "FROM shop_polygon sp " +
                    "LEFT JOIN shops s ON sp.shopid = s.objid AND sp.mapid = s.mapid " +
                    $"WHERE sp.mapid = '{mapid}'");
            var shopPolygons = shopPolygonTable.AsEnumerable().Select(_ =>
                new ShopPolygon
                {
                    Oid = _.Field<int>("objectid"),
                    ObjId = _.Field<int>("objid"),
                    Floorid = _.Field<int>("floorid"),
                    Shopid = _.Field<int?>("shopid"),
                    Categoryid = _["categoryid"] == DBNull.Value
                        ? _.Field<int>("shopcategoryid")
                        : _.Field<int>("categoryid"),
                    Display = _.Field<string>("display"),
                    Shopnamecn = _.Field<string>("shopnamecn"),
                    Shopnameen = _.Field<string>("shopnameen")
                }).ToList();
            return shopPolygons;
        }

        public List<Shops> GetShops(string mapid)
        {
            var shopsTable =
                _palmapDb.ExecuteDataTable($"select * from shops where mapid='{mapid}'");
            //var shops = shopsTable.AsEnumerable().Select(_ => new Shops
            //{
            //    ShopId = _.Field<int>("objid"),
            //    Booth = _.Field<string>("booth"),
            //    ShopNameCn = _.Field<string>("shopnamecn") == null
            //        ? string.Empty
            //        : _.Field<string>("shopnamecn").Trim().Replace("（", "(").Replace("）", ")"),
            //    ShopNameEn = _.Field<string>("shopnameen") == null
            //        ? string.Empty
            //        : _.Field<string>("shopnameen").Trim().Replace("（", "(").Replace("）", ")"),
            //    Display = _.Field<string>("display") == null
            //        ? null
            //        : _.Field<string>("display").Replace("（", "(").Replace("）", ")"),
            //    CategoryId = _.Field<int>("categoryid"),
            //    FloorId = _.Field<int>("floorid"),
            //    RoomNum = _.Field<string>("roomnum") == null
            //        ? null
            //        : _.Field<string>("roomnum").Replace("（", "(").Replace("）", ")"),
            //    BrandId = _.Field<int>("brandid")
            //}).ToList();
            var shops = shopsTable.AsEnumerable().Select(_ => new Shops
            {
                ShopId = _.Field<int>("objid"),
                Booth = _.Field<string>("booth"),
                ShopNameCn = _.Field<string>("shopnamecn")?.Trim().Replace("（", "(").Replace("）", ")"),
                ShopNameEn = _.Field<string>("shopnameen")?.Trim().Replace("（", "(").Replace("）", ")"),
                Display = _.Field<string>("display")?.Replace("（", "(").Replace("）", ")"),
                CategoryId = _.Field<int>("categoryid"),
                FloorId = _.Field<int>("floorid"),
                RoomNum = _.Field<string>("roomnum")?.Replace("（", "(").Replace("）", ")"),
                BrandId = _.Field<int>("brandid")
            }).ToList();
            return shops;
        }

        public List<Connection> GetConnections(string mapid)
        {
            var connectionTable = _palmapDb.ExecuteDataTable($"select * from connections where mapid = '{mapid}'");
            var connections = connectionTable.AsEnumerable().Select(_ => new Connection
            {
                From = _.Field<int>("objid"),
                Type = _.Field<int>("type"),
                To = _["to_"] == DBNull.Value ? 0 : _.Field<int>("to_"),
                Usage = _["usage"] == DBNull.Value ? 0 : _.Field<int>("usage"),
                FloorId = _.Field<int>("floorid")
            }).ToList();
            return connections;
        }

        public List<LaneLine> GetLaneLine(string mapid)
        {
            var lanelineTable = _palmapDb.ExecuteDataTable($"Select * from lane_line where mapid={mapid}");
            var lanelines = lanelineTable.AsEnumerable().Select(_ => new LaneLine
            {
                OId = _.Field<int>("objectid"),
                Name = _.Field<string>("name"),
                FloorId = _.Field<int>("floorid"),
                Direction = _.Field<int>("direction"),
                Detail = _.Field<int>("detail"),
                Weight = _.Field<int>("weight"),
                StartPoint = _.Field<int>("startpointid"),
                EndPoint = _.Field<int>("endpointid")
            }).ToList();
            return lanelines;
        }

        public List<LaneLinePoint> GetLaneLinePoint(string mapid)
        {
            var lanelinePointTable = _palmapDb.ExecuteDataTable($"Select * from lanepoint_point where mapid={mapid}");
            var lanelinePoints = lanelinePointTable.AsEnumerable().Select(_ => new LaneLinePoint
            {
                OId = _.Field<int>("objectid"),
                ObjId = _.Field<int>("objid"),
                Name = _.Field<string>("name") ?? string.Empty,
                FloorId = _.Field<int>("floorid"),
                DoorId = _["doorid"] == DBNull.Value ? 0 : _.Field<int>("doorid"),
                EscalatorId = _["escalatorid"] == DBNull.Value ? 0 : _.Field<int>("escalatorid"),
                PublicServiceId = _["publicserviceid"] == DBNull.Value ? 0 : _.Field<int>("publicserviceid"),
            }).ToList();
            return lanelinePoints;
        }

        public List<PublicServicePoint> GetPublicServicePoints(string mapid)
        {
            var pspTable = _palmapDb.ExecuteDataTable($"select * from publicservice_point where mapid = '{mapid}'");
            var publicservices = pspTable.AsEnumerable().Select(_ => new PublicServicePoint
            {
                OId = _.Field<int>("objectid"),
                ObjId = _.Field<int>("objid"),
                Name = _.Field<string>("name") ?? string.Empty,
                Type = _.Field<int>("type"),
                FloorId = _.Field<int>("floorid")
            }).ToList();
            return publicservices;
        }

        public List<EscalatorPoint> GetEscalatorPoints(string mapid)
        {
            var epTable = _palmapDb.ExecuteDataTable($"select * from escalator_point where mapid = '{mapid}'");
            var escalators = epTable.AsEnumerable().Select(_ => new EscalatorPoint
            {
                OId = _.Field<int>("objectid"),
                ObjId = _.Field<int>("objid"),
                FloorId = _.Field<int>("floorid"),
                ConnectionId = _.Field<int>("connectionid")
            }).ToList();
            return escalators;
        }

        public List<DoorPoint> GetDoorPoints(string mapid)
        {
            var dpTable = _palmapDb.ExecuteDataTable($"select * from door_point where mapid = '{mapid}'");
            var doors = dpTable.AsEnumerable().Select(_ => new DoorPoint
            {
                OId = _.Field<int>("objectid"),
                ObjId = _.Field<int>("objid"),
                Name = _.Field<string>("name"),
                ShopPolygonId = _.Field<int>("shoppolygonid"),
                Type = _.Field<int>("type"),
                FloorId = _.Field<int>("floorid")
            }).ToList();
            return doors;
        }

        public List<AreaPolygon> GetAreaPolygons(string mapid)
        {
            var areaPolygonTable = _palmapDb.ExecuteDataTable(
                "SELECT ap.objectid,ap.floorid,ap.categoryid,s.display,s.shopnamecn," +
                "s.shopnameen,s.categoryid as shopcategoryid " +
                "FROM area_polygon ap " +
                "LEFT JOIN shops s ON ap.booth = s.booth AND ap.mapid = s.mapid " +
                $"WHERE ap.mapid = '{mapid}'");
            var areaPolygons = areaPolygonTable.AsEnumerable().Select(_ => new AreaPolygon
            {
                OId = _.Field<int>("objectid"),
                FloorId = _.Field<int>("floorid"),
                ShopId = _.Field<int>("shopid"),
                CategoryId = _["categoryid"] == DBNull.Value
                    ? _.Field<int>("shopcategoryid")
                    : _.Field<int>("categoryid"),
                Display = _.Field<string>("display")
            }).ToList();
            return areaPolygons;
        }
    }
}

