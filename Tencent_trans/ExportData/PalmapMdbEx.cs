using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using Tencent_trans.Common;
using Tencent_trans.PalmapEntity;

namespace Tencent_trans.ExportData
{
    class PalmapMdbEx
    {
        private IWorkspace fromWorkspace;
        private IWorkspace toWorkspace;
        private IWorkspace exportWorkspace;
        private IFeatureWorkspace fromFeatureWorkspace;
        private IFeatureWorkspace toFeatureWorkspace;
        private IFeatureWorkspace exportFeatureWorkspace;
        private ITransactions sdeTransactions;
        private ITransactionsOptions sdetTransactionsOptions;

        private string MapID;

        //private string Bld_ID;
        private IFeatureClass P_Frame_Polygon;

        private IFeatureClass P_Area_Polygon;
        private IFeatureClass P_Shop_Polygon;
        private IFeatureClass P_Door_Point;
        private IFeatureClass P_Escalator_Point;
        private IFeatureClass P_PublicService_Point;
        private IFeatureClass P_Lane_Line;
        private IFeatureClass P_LanePoint_Point;

        private string _HwBdid;
        private string _bldname;

        public PalmapMdbEx(string sdepath, string tempath, string workpath)
        {
            var conn = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={sdepath};Persist Security Info=False";
            var accessDb = new AccessHelper(conn);
            var mallname = accessDb.ExecuteRow("select * from Mall").Rows[0]["showname"]?.ToString();

            var workPath = ExportFile(mallname, tempath, workpath);
            IWorkspaceFactory exportFactory = new ShapefileWorkspaceFactoryClass();
            exportWorkspace = exportFactory.OpenFromFile(workPath, 0);
            IWorkspaceFactory toFactory = new AccessWorkspaceFactoryClass();
            toWorkspace = toFactory.OpenFromFile(sdepath, 0);
            toFeatureWorkspace = toWorkspace as IFeatureWorkspace;
            exportFeatureWorkspace = exportWorkspace as IFeatureWorkspace;
            Marshal.FinalReleaseComObject(toFactory);
            Marshal.FinalReleaseComObject(exportFactory);
        }

        public bool Export(string mdbPath)
        {
            ExportTable(mdbPath);
            return true;
        }

        private void ExportTable(string mdbPath)
        {
            #region PalmapFeatureClass

            P_Frame_Polygon = toFeatureWorkspace.OpenFeatureClass("Frame_Polygon");
            P_Area_Polygon = toFeatureWorkspace.OpenFeatureClass("Area_Polygon");
            P_Shop_Polygon = toFeatureWorkspace.OpenFeatureClass("Shop_Polygon");
            P_Door_Point = toFeatureWorkspace.OpenFeatureClass("Door_Point");
            P_Escalator_Point = toFeatureWorkspace.OpenFeatureClass("Escalator_Point");
            P_PublicService_Point = toFeatureWorkspace.OpenFeatureClass("PublicService_Point");
            P_Lane_Line = toFeatureWorkspace.OpenFeatureClass("Lane_Line");
            P_LanePoint_Point = toFeatureWorkspace.OpenFeatureClass("LanePoint_Point");
            #endregion


            #region HuaweiFeatureClass

            var cityModel = exportFeatureWorkspace.OpenFeatureClass("base_indoor_city_model");
            var mPoi = exportFeatureWorkspace.OpenFeatureClass("base_indoor_m_poi");
            var indoorFl = exportFeatureWorkspace.OpenFeatureClass("base_indoor_fl");
            var indoorLink = exportFeatureWorkspace.OpenFeatureClass("base_indoor_link");
            var indoorNode = exportFeatureWorkspace.OpenFeatureClass("base_indoor_node");
            var indoorPoi = exportFeatureWorkspace.OpenFeatureClass("base_indoor_poi");
            var indoorRegion = exportFeatureWorkspace.OpenFeatureClass("base_indoor_region");
            var indoorSubRegion = exportFeatureWorkspace.OpenFeatureClass("base_indoor_sub_region");

            var indoorDoors = exportFeatureWorkspace.OpenTable("base_indoor_doors");
            var indoorStairs = exportFeatureWorkspace.OpenTable("base_indoor_stairs");

            #endregion

            var palmapData = new PalmapMdbData(mdbPath);

            //华为数据库地址
            var connstr = ConfigurationManager.ConnectionStrings["Huawei"].ConnectionString;
            var huaweiDbData = new HuaweiDbData(connstr);

            #region palmapTables

            var mall = palmapData.GetMall();
            _HwBdid = mall.HwId;
            var floors = palmapData.GetFloors().OrderBy(_ => _.Seq).ToList();
            var qualitys = palmapData.GetDataqualitys();
            var frames = palmapData.GetFrames();
            var shopPolygons = palmapData.GetShopPolygon();

            var doors = palmapData.GetDoorPoints();
            var escalators = palmapData.GetEscalatorPoints();
            var publicservices = palmapData.GetPublicServicePoints();
            var areas = palmapData.GetAreaPolygons();

            var nodes = palmapData.GetLaneLinePoint();
            var links = palmapData.GetLaneLine();
            var connections = palmapData.GetConnections();
            //数据库中的楼梯字典信息
            var dicEscalator = palmapData.GetDicEscaCategory();
            //华为poiAddress字典
            var dicAddress = GetFloorAddressDic(mall.ShowName, floors);
            //var diccategory = palmapData.GetDicCategory();
            //华为category列表
            var dicHuaweiCategory = huaweiDbData.GetHuaweiDicCategory().ToDictionary(_ => _.Category);
            //var dicHuaweiTexture = huaweiDbData.GetHuaweiTexture().ToDictionary(_ => _.Id);
            #endregion

            var cTime = qualitys.First().RenewData; //创建时间
            var mTime = qualitys.Last().RenewData; //修改时间
            //var founder = qualitys.First().Renewer; //创建人
            //var modifier = qualitys.Last().Renewer; //修改人
            const string founder = "palmap"; //创建人
            const string modifier = "palmap"; //修改人

            var dicFrame = frames.GroupBy(_ => _.Floorid)
                .ToDictionary(_ => _.First().Floorid, _ => _.ToList());
            var f0Floorid = floors.First(_ => _.Seq == 0).Floorid;
            var f0Feature = dicFrame[f0Floorid][0];
            var dicFloor = floors.GroupBy(_ => _.Floorid).ToDictionary(_ => _.First().Floorid, _ => _.ToList());

            //一层楼层id
            var floorF1 = floors.FirstOrDefault(_ => _.Name == "一层");
            var f1id = floorF1?.Floorid ?? 0;

            //base_indoor_city_model
            var foGeo = P_Frame_Polygon.GetFeature(f0Feature.Oid).Shape;
            if (f0Feature != null)
            {
                var newFeature = cityModel.CreateFeature();
                newFeature.Shape = foGeo;
                newFeature.Value[newFeature.Fields.FindField("m_poi_id")] = _HwBdid;
                newFeature.Value[newFeature.Fields.FindField("bd_id")] = _HwBdid;
                newFeature.Value[newFeature.Fields.FindField("category")] = GetCategory(mall.MallType);
                newFeature.Value[newFeature.Fields.FindField("up_num")] = floors.Count(_ => _.Altitude > 0);
                newFeature.Value[newFeature.Fields.FindField("dw_num")] = -floors.Count(_ => _.Altitude < 0);
                newFeature.Value[newFeature.Fields.FindField("c_time")] = cTime;
                newFeature.Value[newFeature.Fields.FindField("m_time")] = mTime;
                newFeature.Value[newFeature.Fields.FindField("s_data")] = "indoor_palmap"; //qualitys.First().MapSouce;
                newFeature.Value[newFeature.Fields.FindField("founder")] = founder;
                newFeature.Value[newFeature.Fields.FindField("modifier")] = modifier;
                newFeature.Store();
            }

            //base_indoor_m_poi
            var mPoiFeature = mPoi.CreateFeature();
            var f0Area = foGeo as IArea;
            mPoiFeature.Shape = f0Area?.Centroid;
            mPoiFeature.Value[mPoiFeature.Fields.FindField("m_poi_id")] = _HwBdid;
            mPoiFeature.Value[mPoiFeature.Fields.FindField("c_name")] = mall.ShowName;
            mPoiFeature.Value[mPoiFeature.Fields.FindField("e_name")] = mall.NameEN;
            mPoiFeature.Value[mPoiFeature.Fields.FindField("address")] = mall.Address;
            mPoiFeature.Value[mPoiFeature.Fields.FindField("category")] = GetCategory(mall.MallType);
            mPoiFeature.Value[mPoiFeature.Fields.FindField("phone")] = mall.Phone;
            mPoiFeature.Value[mPoiFeature.Fields.FindField("default")] = mall.DefaultFloor;
            mPoiFeature.Value[mPoiFeature.Fields.FindField("c_time")] = cTime;
            mPoiFeature.Value[mPoiFeature.Fields.FindField("m_time")] = mTime;
            mPoiFeature.Value[mPoiFeature.Fields.FindField("remark")] = mall.Description;
            mPoiFeature.Value[mPoiFeature.Fields.FindField("founder")] = founder;
            mPoiFeature.Value[mPoiFeature.Fields.FindField("modifier")] = modifier;
            mPoiFeature.Store();

            //dicFloorid
            var newDicFloor = new Dictionary<int, string>();
            var num = 1;
            foreach (var floor in floors.Where(_ => _.Seq != 0).GroupBy(_ => _.Floorid).ToList())
            {
                //var flId = $"{bdid}{num.ToString().PadLeft(2, '0')}";
                var flId = $"{dicFloor[floor.Key][0].HuaweiFlid}";
                newDicFloor.Add(floor.Key, flId);
                num++;
            }
            num = 1;

            //base_indoor_fl
            foreach (var frame in frames.Where(_ => _.Floorid != f0Floorid).ToList())
            {
                var currentFloor = dicFloor[frame.Floorid][0];
                var floornum = GetFloorNum(currentFloor);
                var geo = P_Frame_Polygon.GetFeature(frame.Oid).Shape;
                

                var newFeature = indoorFl.CreateFeature();
                //HuaweiDbData.SimplifyGeometry2(ref geo);
                newFeature.Shape = geo;
                newFeature.Value[newFeature.Fields.FindField("fl_id")] = newDicFloor[frame.Floorid];
                newFeature.Value[newFeature.Fields.FindField("fl_name")] = currentFloor.NameEn;
                newFeature.Value[newFeature.Fields.FindField("bd_id")] = _HwBdid;
                newFeature.Value[newFeature.Fields.FindField("f_descr")] = currentFloor.Style;
                newFeature.Value[newFeature.Fields.FindField("c_time")] = cTime;
                newFeature.Value[newFeature.Fields.FindField("m_time")] = mTime;
                newFeature.Value[newFeature.Fields.FindField("floornum")] = floornum;
                newFeature.Value[newFeature.Fields.FindField("elevation")] = 3; //需要修改
                //floors.FirstOrDefault(_ => _.Floorid == frame.Floorid).Altitude;
                newFeature.Value[newFeature.Fields.FindField("founder")] = founder;
                newFeature.Value[newFeature.Fields.FindField("modifier")] = modifier;
                newFeature.Store();
            }

            var dicShopPolygon = new Dictionary<int, string>();

            //base_indoor_region
            var shopPolygonBuffer = indoorRegion.Insert(true);
            foreach (var dicfloor in newDicFloor)
            {
                num = 1;
                foreach (var shopPolygon in shopPolygons.Where(_ => _.Floorid == dicfloor.Key))
                {
                    var name = GetNameByCategory(shopPolygon.Categoryid, shopPolygon.Display, dicHuaweiCategory);

                    var regionId = $"{dicfloor.Value}{num.ToString().PadLeft(4, '0')}";
                    var geo = P_Shop_Polygon.GetFeature(shopPolygon.Oid).Shape;
                    var newFeature = indoorRegion.CreateFeatureBuffer();
                    HuaweiDbData.SimplifyGeometry2(ref geo);
                    newFeature.Shape = geo;
                    newFeature.Value[newFeature.Fields.FindField("region_id")] = regionId;
                    newFeature.Value[newFeature.Fields.FindField("fl_id")] = newDicFloor[shopPolygon.Floorid];
                    newFeature.Value[newFeature.Fields.FindField("category")] = shopPolygon.Categoryid;
                    newFeature.Value[newFeature.Fields.FindField("c_name")] = name;
                    //string.IsNullOrEmpty(shopPolygon.Display)
                    //    ? dicHuaweiCategory[shopPolygon.Categoryid].CategoryName
                    //    : shopPolygon.Display;
                    newFeature.Value[newFeature.Fields.FindField("e_name")] = HuaweiDbData.BaiduTranslation(name);
                    // shopPolygon.Shopnameen;
                    //newFeature.Value[newFeature.Fields.FindField("wifi")] = string.Empty;
                    newFeature.Value[newFeature.Fields.FindField("height")] =
                        dicHuaweiCategory[shopPolygon.Categoryid].Height;
                    newFeature.Value[newFeature.Fields.FindField("texture")] =
                        GetTextureByCategory(shopPolygon.Categoryid, shopPolygon.Texture);
                    newFeature.Value[newFeature.Fields.FindField("c_time")] = cTime;
                    newFeature.Value[newFeature.Fields.FindField("m_time")] = mTime;
                    newFeature.Value[newFeature.Fields.FindField("founder")] = founder;
                    newFeature.Value[newFeature.Fields.FindField("modifier")] = modifier;
                    shopPolygonBuffer.InsertFeature(newFeature);
                    num++;
                    dicShopPolygon.Add(shopPolygon.Oid, regionId);
                }
            }
            shopPolygonBuffer.Flush();
            Marshal.FinalReleaseComObject(shopPolygonBuffer);

            //base_indoor_sub_region
            var areaPolygonBuffer = indoorSubRegion.Insert(true);
            foreach (var dicfloor in newDicFloor)
            {
                num = 1;
                foreach (var area in areas.Where(_ => _.FloorId == dicfloor.Key))
                {
                    var name = GetNameByCategory(area.CategoryId, area.RoomNum, dicHuaweiCategory);

                    var regionid = $"{dicfloor.Value}{num.ToString().PadLeft(4, '0')}";
                    //buffer insert
                    var geo = P_Area_Polygon.GetFeature(area.OId).Shape;
                    var newBuffer = indoorSubRegion.CreateFeatureBuffer();
                    HuaweiDbData.SimplifyGeometry2(ref geo);
                    newBuffer.Shape = geo;
                    newBuffer.Value[newBuffer.Fields.FindField("region_id")] = regionid;
                    newBuffer.Value[newBuffer.Fields.FindField("fl_id")] = dicfloor.Value;
                    newBuffer.Value[newBuffer.Fields.FindField("category")] = area.CategoryId;
                    newBuffer.Value[newBuffer.Fields.FindField("texture")] =
                        GetTextureByCategory(area.CategoryId, area.Texture);
                    newBuffer.Value[newBuffer.Fields.FindField("c_name")] = name;

                    newBuffer.Value[newBuffer.Fields.FindField("c_time")] = cTime;
                    newBuffer.Value[newBuffer.Fields.FindField("m_time")] = mTime;
                    newBuffer.Value[newBuffer.Fields.FindField("founder")] = founder;
                    newBuffer.Value[newBuffer.Fields.FindField("modifier")] = modifier;
                    areaPolygonBuffer.InsertFeature(newBuffer);
                    num++;
                }
            }
            areaPolygonBuffer.Flush();
            Marshal.FinalReleaseComObject(areaPolygonBuffer);

            //base_indoor_poi
            var indoorPoiBuffer = indoorPoi.Insert(true);
            foreach (var dicfloor in newDicFloor)
            {
                num = 0;
                //publicservice 点位信息
                foreach (var publicservice in publicservices.Where(_ => _.FloorId == dicfloor.Key))
                {
                    var name = GetNameByCategory(publicservice.CategoryId, publicservice.Name, dicHuaweiCategory);

                    var poiId = $"{dicfloor.Value}{(++num).ToString().PadLeft(4, '0')}";
                    var geo = P_PublicService_Point.GetFeature(publicservice.OId).Shape;
                    var newBuffer = indoorPoi.CreateFeatureBuffer();
                    newBuffer.Shape = geo;
                    newBuffer.Value[newBuffer.Fields.FindField("poi_id")] = poiId;
                    newBuffer.Value[newBuffer.Fields.FindField("fl_id")] = dicfloor.Value;
                    newBuffer.Value[newBuffer.Fields.FindField("c_name")] = name;
                    //string.IsNullOrEmpty(publicservice.Name)
                    //? dicHuaweiCategory[publicservice.CategoryId].CategoryName
                    //: publicservice.Name;
                    newBuffer.Value[newBuffer.Fields.FindField("e_name")] = HuaweiDbData.BaiduTranslation(name);
                    newBuffer.Value[newBuffer.Fields.FindField("category")] = publicservice.CategoryId;
                    newBuffer.Value[newBuffer.Fields.FindField("address")] = dicAddress[dicfloor.Key];
                    newBuffer.Value[newBuffer.Fields.FindField("phone")] = "";
                    newBuffer.Value[newBuffer.Fields.FindField("parentid")] = 0;
                    newBuffer.Value[newBuffer.Fields.FindField("rel_type")] = 0;
                    newBuffer.Value[newBuffer.Fields.FindField("c_time")] = cTime;
                    newBuffer.Value[newBuffer.Fields.FindField("m_time")] = mTime;
                    newBuffer.Value[newBuffer.Fields.FindField("priority")] = 0;
                    newBuffer.Value[newBuffer.Fields.FindField("founder")] = founder;
                    newBuffer.Value[newBuffer.Fields.FindField("modifier")] = modifier;
                    newBuffer.Value[newBuffer.Fields.FindField("sub_kind")] = publicservice.SubKind;
                    indoorPoiBuffer.InsertFeature(newBuffer);
                }

                //shopPolygon 面转为点位信息
                foreach (var shopPolygon in shopPolygons.Where(_ => _.Floorid == dicfloor.Key))
                {
                    //联通设施（电梯、楼梯、扶梯）、洗手间（男洗手间、女洗手间、残障洗手间）不导出poi点位
                    if (!dicHuaweiCategory[shopPolygon.Categoryid].IsPoi)
                        continue;
                    if (shopPolygon.Categoryid == 20005 && string.IsNullOrWhiteSpace(shopPolygon.RoomNum))
                        continue;


                    var poiId = $"{dicfloor.Value}{(++num).ToString().PadLeft(4, '0')}";
                    var geo = P_Shop_Polygon.GetFeature(shopPolygon.Oid).Shape as IArea;
                    var newBuffer = indoorPoi.CreateFeatureBuffer();
                    newBuffer.Shape = geo?.LabelPoint;
                    newBuffer.Value[newBuffer.Fields.FindField("poi_id")] = poiId;
                    newBuffer.Value[newBuffer.Fields.FindField("fl_id")] = dicfloor.Value;

                    var namecn = GetNameByCategory(shopPolygon.Categoryid, shopPolygon.Display, shopPolygon.RoomNum,
                        dicHuaweiCategory);

                    newBuffer.Value[newBuffer.Fields.FindField("c_name")] = namecn;
                    newBuffer.Value[newBuffer.Fields.FindField("e_name")] =
                        string.IsNullOrWhiteSpace(shopPolygon.RoomNum)
                            ? HuaweiDbData.BaiduTranslation(namecn)
                            : shopPolygon.RoomNum;

                    newBuffer.Value[newBuffer.Fields.FindField("category")] = shopPolygon.Categoryid;
                    newBuffer.Value[newBuffer.Fields.FindField("address")] = dicAddress[dicfloor.Key];
                    newBuffer.Value[newBuffer.Fields.FindField("phone")] = "";
                    newBuffer.Value[newBuffer.Fields.FindField("parentid")] = 0;
                    newBuffer.Value[newBuffer.Fields.FindField("rel_type")] = 0;
                    newBuffer.Value[newBuffer.Fields.FindField("c_time")] = cTime;
                    newBuffer.Value[newBuffer.Fields.FindField("m_time")] = mTime;
                    newBuffer.Value[newBuffer.Fields.FindField("priority")] = 0;
                    newBuffer.Value[newBuffer.Fields.FindField("founder")] = founder;
                    newBuffer.Value[newBuffer.Fields.FindField("modifier")] = modifier;
                    newBuffer.Value[newBuffer.Fields.FindField("sub_kind")] = "";
                    indoorPoiBuffer.InsertFeature(newBuffer);
                }
                //escalator 点位信息
                foreach (var escalator in escalators.Where(_ => _.FloorId == dicfloor.Key))
                {
                    var poiId = $"{dicfloor.Value}{(++num).ToString().PadLeft(4, '0')}";
                    var geo = P_Escalator_Point.GetFeature(escalator.OId).Shape;
                    var newBuffer = indoorPoi.CreateFeatureBuffer();
                    newBuffer.Shape = geo;
                    newBuffer.Value[newBuffer.Fields.FindField("poi_id")] = poiId;
                    newBuffer.Value[newBuffer.Fields.FindField("fl_id")] = dicfloor.Value;
                    newBuffer.Value[newBuffer.Fields.FindField("c_name")] = dicEscalator[escalator.Type].Display;
                    newBuffer.Value[newBuffer.Fields.FindField("e_name")] =
                        HuaweiDbData.BaiduTranslation(dicEscalator[escalator.Type].Display);
                    newBuffer.Value[newBuffer.Fields.FindField("category")] = dicEscalator[escalator.Type].CategoryId;
                    newBuffer.Value[newBuffer.Fields.FindField("address")] = dicAddress[dicfloor.Key];
                    newBuffer.Value[newBuffer.Fields.FindField("phone")] = "";
                    newBuffer.Value[newBuffer.Fields.FindField("parentid")] = 0;
                    newBuffer.Value[newBuffer.Fields.FindField("rel_type")] = 0;
                    newBuffer.Value[newBuffer.Fields.FindField("c_time")] = cTime;
                    newBuffer.Value[newBuffer.Fields.FindField("m_time")] = mTime;
                    newBuffer.Value[newBuffer.Fields.FindField("priority")] = 0;
                    newBuffer.Value[newBuffer.Fields.FindField("founder")] = founder;
                    newBuffer.Value[newBuffer.Fields.FindField("modifier")] = modifier;
                    newBuffer.Value[newBuffer.Fields.FindField("sub_kind")] = "";
                    indoorPoiBuffer.InsertFeature(newBuffer);
                }

                //area_polygon 面转点位信息（车位信息存放在area_polygon中）
                foreach (var area in areas.Where(_ => _.FloorId == dicfloor.Key))
                {
                    //
                    if (!dicHuaweiCategory[area.CategoryId].IsPoi ||
                        area.CategoryId == 91001001 && string.IsNullOrEmpty(area.RoomNum))
                        continue;

                    if (area.CategoryId == 20005 && string.IsNullOrWhiteSpace(area.RoomNum))
                        continue;

                    var poiId = $"{dicfloor.Value}{(++num).ToString().PadLeft(4, '0')}";
                    var geo = P_Area_Polygon.GetFeature(area.OId).Shape as IArea;
                    var newBuffer = indoorPoi.CreateFeatureBuffer();
                    newBuffer.Shape = geo.LabelPoint;//LabelPoint 保证该点在面中。//Centroid 重心有可能出现在面外
                    newBuffer.Value[newBuffer.Fields.FindField("poi_id")] = poiId;
                    newBuffer.Value[newBuffer.Fields.FindField("fl_id")] = dicfloor.Value;

                    var name = GetNameByCategory(area.CategoryId, area.Display, area.RoomNum, dicHuaweiCategory);

                    //var name=string.Empty;
                    //name = area.CategoryId == 25142 ? area.Display : area.RoomNum;
                    //if (area.CategoryId == 92004)
                    //    name = dicHuaweiCategory[area.CategoryId].CategoryName;
                    //var name = area.CategoryId == 25142 ? area.Display : area.RoomNum;

                    newBuffer.Value[newBuffer.Fields.FindField("c_name")] = name;
                    newBuffer.Value[newBuffer.Fields.FindField("e_name")] =
                        string.IsNullOrWhiteSpace(area.RoomNum)
                            ? HuaweiDbData.BaiduTranslation(name)
                            : area.RoomNum;
                    //HuaweiDbData.YoudaoTranslation(name);
                    newBuffer.Value[newBuffer.Fields.FindField("category")] = area.CategoryId;
                    newBuffer.Value[newBuffer.Fields.FindField("address")] = dicAddress[dicfloor.Key];
                    newBuffer.Value[newBuffer.Fields.FindField("phone")] = "";
                    newBuffer.Value[newBuffer.Fields.FindField("parentid")] = 0;
                    newBuffer.Value[newBuffer.Fields.FindField("rel_type")] = 0;
                    newBuffer.Value[newBuffer.Fields.FindField("c_time")] = cTime;
                    newBuffer.Value[newBuffer.Fields.FindField("m_time")] = mTime;
                    newBuffer.Value[newBuffer.Fields.FindField("priority")] = 0;
                    newBuffer.Value[newBuffer.Fields.FindField("founder")] = founder;
                    newBuffer.Value[newBuffer.Fields.FindField("modifier")] = modifier;
                    newBuffer.Value[newBuffer.Fields.FindField("sub_kind")] = "";
                    indoorPoiBuffer.InsertFeature(newBuffer);
                }
            }
            indoorPoiBuffer.Flush();
            Marshal.FinalReleaseComObject(indoorPoiBuffer);

            //base_indoor_node
            var indoorNodeBuffer = indoorNode.Insert(true);
            foreach (var dicfloor in newDicFloor)
            {
                num = 0;
                foreach (var node in nodes.Where(_ => _.FloorId == dicfloor.Key))
                {
                    var nodeid = $"{dicfloor.Value}{(++num).ToString().PadLeft(4, '0')}";
                    node.NodeId = nodeid;
                    var geo = P_LanePoint_Point.GetFeature(node.OId).Shape;
                    var newBuffer = indoorNode.CreateFeatureBuffer();
                    newBuffer.Shape = geo;


                    newBuffer.Value[newBuffer.Fields.FindField("node_id")] = nodeid;
                    newBuffer.Value[newBuffer.Fields.FindField("kind")] = GetKindOfNode(node, escalators, connections, publicservices); //kkkkkkkkkkkind
                    newBuffer.Value[newBuffer.Fields.FindField("fl_id")] = dicfloor.Value;
                    newBuffer.Value[newBuffer.Fields.FindField("c_time")] = cTime;
                    newBuffer.Value[newBuffer.Fields.FindField("m_time")] = mTime;
                    newBuffer.Value[newBuffer.Fields.FindField("text")] = "";
                    newBuffer.Value[newBuffer.Fields.FindField("founder")] = founder;
                    newBuffer.Value[newBuffer.Fields.FindField("modifier")] = modifier;
                    indoorNodeBuffer.InsertFeature(newBuffer);
                }
            }
            indoorNodeBuffer.Flush();
            Marshal.FinalReleaseComObject(indoorNodeBuffer);

            //base_indoor_link
            var indoorLinkBuffer = indoorLink.Insert(true);
            foreach (var dicfloor in newDicFloor)
            {
                num = 0;
                foreach (var link in links.Where(_ => _.FloorId == dicfloor.Key))
                {
                    var linkid = $"{dicfloor.Value}{(++num).ToString().PadLeft(4, '0')}";
                    var soureNode = nodes.FirstOrDefault(_ => _.OId == link.StartPoint);
                    var targetNode = nodes.FirstOrDefault(_ => _.OId == link.EndPoint);
                    var geo = P_Lane_Line.GetFeature(link.OId).Shape;
                    var newBuffer = indoorLink.CreateFeatureBuffer();
                    newBuffer.Shape = geo;
                    newBuffer.Value[newBuffer.Fields.FindField("link_id")] = linkid;
                    newBuffer.Value[newBuffer.Fields.FindField("kind")] =
                        GetKindBySourceTargetNode(soureNode, targetNode, f1id, publicservices); //"02"; //默认
                    newBuffer.Value[newBuffer.Fields.FindField("source")] = soureNode.NodeId;
                    newBuffer.Value[newBuffer.Fields.FindField("target")] = targetNode.NodeId;
                    newBuffer.Value[newBuffer.Fields.FindField("fl_id")] = dicfloor.Value;
                    newBuffer.Value[newBuffer.Fields.FindField("direction")] = link.Direction;
                    newBuffer.Value[newBuffer.Fields.FindField("class")] = 2; //默认
                    newBuffer.Value[newBuffer.Fields.FindField("c_time")] = cTime;
                    newBuffer.Value[newBuffer.Fields.FindField("m_time")] = mTime;
                    newBuffer.Value[newBuffer.Fields.FindField("length")] =
                        Math.Round(link.Length, 3); // (geo as ILine).Length;
                    newBuffer.Value[newBuffer.Fields.FindField("text")] = "";
                    newBuffer.Value[newBuffer.Fields.FindField("founder")] = founder;
                    newBuffer.Value[newBuffer.Fields.FindField("modifier")] = modifier;
                    indoorLinkBuffer.InsertFeature(newBuffer);
                }
            }
            indoorLinkBuffer.Flush();
            Marshal.FinalReleaseComObject(indoorLinkBuffer);

            //base_indoor_doors
            var indoorDoorsBuffer = indoorDoors.Insert(true);
            foreach (var dicfloor in newDicFloor)
            {
                foreach (var door in doors.Where(_ => _.FloorId == dicfloor.Key))
                {
                    var newBuffer = indoorDoors.CreateRowBuffer();
                    newBuffer.Value[newBuffer.Fields.FindField("node_id")] =
                        nodes.FirstOrDefault(_ => _.DoorId == door.OId)?.NodeId;
                    if (string.IsNullOrEmpty(nodes.FirstOrDefault(_ => _.DoorId == door.OId)?.NodeId))
                    {
                        Program.logger.Debug($"表[Door_Point]中的OID为[{door.OId}]不在LanePoint_Point中");
                    }
                    newBuffer.Value[newBuffer.Fields.FindField("region_id")] = dicShopPolygon[door.ShopPolygonId];
                    newBuffer.Value[newBuffer.Fields.FindField("door_width")] = 1; //门宽需校准
                    newBuffer.Value[newBuffer.Fields.FindField("exit")] = "0";
                    newBuffer.Value[newBuffer.Fields.FindField("islock")] = "0";
                    newBuffer.Value[newBuffer.Fields.FindField("outdoor")] = "0";
                    newBuffer.Value[newBuffer.Fields.FindField("time")] = "";
                    newBuffer.Value[newBuffer.Fields.FindField("c_time")] = cTime;
                    newBuffer.Value[newBuffer.Fields.FindField("m_time")] = mTime;
                    newBuffer.Value[newBuffer.Fields.FindField("text")] = "";
                    newBuffer.Value[newBuffer.Fields.FindField("founder")] = founder;
                    newBuffer.Value[newBuffer.Fields.FindField("modifier")] = modifier;
                    indoorDoorsBuffer.InsertRow(newBuffer);
                }

                foreach (var publicService in publicservices.Where(_ => _.FloorId == dicfloor.Key &&
                                                                        (_.CategoryId == 25062 ||
                                                                         _.CategoryId == 25140)))
                {
                    var newBuffer = indoorDoors.CreateRowBuffer();
                    newBuffer.Value[newBuffer.Fields.FindField("node_id")] =
                        nodes.FirstOrDefault(_ => _.PublicServiceId == publicService.OId)?.NodeId;
                    if (string.IsNullOrEmpty(nodes.FirstOrDefault(_ => _.PublicServiceId == publicService.OId)?.NodeId))
                    {
                        Program.logger.Debug($"表[PubulicService_Point]中的OID为[{publicService.OId}]不在LanePoint_Point中");
                    }
                    newBuffer.Value[newBuffer.Fields.FindField("region_id")] = "";
                    newBuffer.Value[newBuffer.Fields.FindField("door_width")] = 1; //门宽需校准
                    newBuffer.Value[newBuffer.Fields.FindField("exit")] = "0";
                    newBuffer.Value[newBuffer.Fields.FindField("islock")] = "0";
                    newBuffer.Value[newBuffer.Fields.FindField("outdoor")] = publicService.FloorId == f1id ? "1" : "0";
                    newBuffer.Value[newBuffer.Fields.FindField("time")] = "";
                    newBuffer.Value[newBuffer.Fields.FindField("c_time")] = cTime;
                    newBuffer.Value[newBuffer.Fields.FindField("m_time")] = mTime;
                    newBuffer.Value[newBuffer.Fields.FindField("text")] = "";
                    newBuffer.Value[newBuffer.Fields.FindField("founder")] = founder;
                    newBuffer.Value[newBuffer.Fields.FindField("modifier")] = modifier;
                    indoorDoorsBuffer.InsertRow(newBuffer);
                }
            }
            indoorDoorsBuffer.Flush();
            Marshal.FinalReleaseComObject(indoorDoorsBuffer);

            //base_indoor_stairs
            var indoorStairsBuffer = indoorStairs.Insert(true);
            foreach (var escalator in escalators)
            {
                var nodeid = nodes.FirstOrDefault(_ => _.EscalatorId == escalator.OId)?.NodeId;
                if (string.IsNullOrWhiteSpace(nodeid))
                    continue;
                var newBuffer = indoorStairs.CreateRowBuffer();
                newBuffer.Value[newBuffer.Fields.FindField("node_id")] = nodeid;
                var fromConnection = connections.FirstOrDefault(_ => _.To == escalator.ConnectionId && !_.OneWay);
                var toConnection = connections.FirstOrDefault(_ => _.From == escalator.ConnectionId && _.To != 0);

                var direction = 0;
                var tostairs = new List<string>();
                if (toConnection != null)
                {
                    tostairs.Add(nodes.FirstOrDefault(_ => _.EscalatorId == toConnection.To)?.NodeId);
                    direction = 2;
                }
                if (fromConnection != null)
                {
                    tostairs.Add(nodes.FirstOrDefault(_ => _.EscalatorId == fromConnection.From)?.NodeId);
                    direction = 1;
                }
                if (tostairs.Count == 2)
                {
                    direction = 0;
                }
                newBuffer.Value[newBuffer.Fields.FindField("direction")] = direction;
                newBuffer.Value[newBuffer.Fields.FindField("to_stairs")] = string.Join("|", tostairs);
                newBuffer.Value[newBuffer.Fields.FindField("c_time")] = cTime;
                newBuffer.Value[newBuffer.Fields.FindField("m_time")] = mTime;
                newBuffer.Value[newBuffer.Fields.FindField("text")] = "";
                newBuffer.Value[newBuffer.Fields.FindField("founder")] = founder;
                newBuffer.Value[newBuffer.Fields.FindField("modifier")] = modifier;
                indoorStairsBuffer.InsertRow(newBuffer);
            }
            indoorStairsBuffer.Flush();
            Marshal.FinalReleaseComObject(indoorStairsBuffer);
        }

        public int GetFloorNum(Floor floor)
        {
            var starts = floor.NameEn.Substring(0, 1);
            var floornum = string.Empty;
            switch (starts)
            {
                case "F":
                    floornum = floor.NameEn.Substring(1);
                    break;
                case "B":
                    floornum = "-" + floor.NameEn.Substring(1);
                    break;
                case "M":
                    floornum = floor.NameEn.Substring(1);
                    break;
                case "N":
                    floornum = floor.NameEn.Substring(1);
                    break;
            }
            return Convert.ToInt32(floornum);
        }

        private Dictionary<int, string> GetFloorAddressDic(string address, List<Floor> floors)
        {
            var dic = new Dictionary<int, string>();
            foreach (var floor in floors)
            {
                dic.Add(floor.Floorid, $"{address}-{floor.Name}");
            }
            return dic;
        }

        private string GetKindOfNode(LaneLinePoint node, List<EscalatorPoint> escalators, List<Connection> connections,
            List<PublicServicePoint> publicServices)
        {
            var kind = "10";
            if (node.DoorId != 0)
            {
                kind = "16";
            }
            else if (node.EscalatorId != 0)
            {
                var escalator = escalators.FirstOrDefault(_ => _.OId == node.EscalatorId);
                var connection = connections.FirstOrDefault(_ => _.From == node.EscalatorId);
                switch (escalator?.Type)
                {
                    case 1:
                        switch (connection?.Usage)
                        {
                            case 2:
                                kind = "13";
                                break;
                            default:
                                kind = "12";
                                break;
                        }
                        break;
                    case 2:
                        kind = "14";
                        break;
                    case 3:
                        kind = "14";
                        break;
                    case 4:
                        kind = "17";
                        break;
                }
            }
            else if (node.PublicServiceId != 0)
            {
                var publicService = publicServices.FirstOrDefault(_ => _.OId == node.PublicServiceId);
                if (publicService?.CategoryId == 25062 ||
                    publicService?.CategoryId == 25140)
                    kind = "16";
                //if (publicService?.CategoryId == 25011)  //category=25011,node点kind值设为12
                //    kind = "12";
            }
            return kind;
        }

        public string ExportFile(string mapname, string temPath, string workpath)
        {
            var mapPath = System.IO.Path.Combine(workpath, mapname);
            if (Directory.Exists(mapPath))
            {
                Directory.Delete(mapPath, true);
            }
            Directory.CreateDirectory(mapPath);

            var files = Directory.GetFiles(temPath);
            foreach (var f in files)
            {
                var filename = System.IO.Path.Combine(temPath, System.IO.Path.GetFileName(f));
                var destFile = System.IO.Path.Combine(mapPath, System.IO.Path.GetFileName(f));
                File.Copy(filename, destFile, true);
            }
            return mapPath;
        }


        public Func<int, List<int>, bool> IsInList = (p, a) =>
        {
            if (a.Contains(p))
                return true;
            return false;
        };

        public string GetNameByCategory(int categoryid, string name, Dictionary<int, DicHuaweiCategory> dichuawei)
        {
            var result = string.Empty;
            switch (categoryid)
            {
                case 25001:
                    result = "中空";
                    break;
                case 25133:
                    result = "女洗手间";
                    break;
                case 25075:
                    result = "男洗手间";
                    break;
                case 25134:
                    result = "楼梯";
                    break;
                case 25000:
                    result = "禁止区";
                    break;
                case 25135:
                    result = "扶梯";
                    break;
                //case 24000:
                //    result = "房间";
                //    break;
                case 24000:
                    result = "空铺";
                    break;
                case 25136:
                    result = "电梯";
                    break;
                case 25015:
                    result = "残障洗手间";
                    break;
                case 28022:
                    result = "机要室";
                    break;
                case 28008:
                    result = "仓库";
                    break;
                case 20005:
                    result = string.IsNullOrWhiteSpace(name) ? "车位" : name;
                    break;
                default:
                    result = string.IsNullOrWhiteSpace(name) ? dichuawei[categoryid].CategoryName : name;
                    break;
            }
            return result;
        }

        public string GetNameByCategory(int categoryid, string name, string roomnum,
            Dictionary<int, DicHuaweiCategory> dichuawei)
        {
            var result = string.Empty;
            switch (categoryid)
            {
                case 25001:
                    result = "中空";
                    break;
                case 25133:
                    result = "女洗手间";
                    break;
                case 25075:
                    result = "男洗手间";
                    break;
                case 25134:
                    result = "楼梯";
                    break;
                case 25000:
                    result = "禁止区";
                    break;
                case 25135:
                    result = "扶梯";
                    break;
                //case 24000:
                //    result = "房间";
                //    break;
                case 24000:
                    result = "空铺";
                    break;
                case 25136:
                    result = "电梯";
                    break;
                case 25015:
                    result = "残障洗手间";
                    break;
                case 28022:
                    result = "机要室";
                    break;
                case 28008:
                    result = "仓库";
                    break;
                case 20005:
                    result = string.IsNullOrWhiteSpace(roomnum) ? string.Empty : roomnum;
                    break;
                default:
                    if (!string.IsNullOrWhiteSpace(roomnum)) result = roomnum;
                    else
                    {
                        result = string.IsNullOrEmpty(name) ? dichuawei[categoryid].CategoryName : name;
                    }
                    break;
            }
            return result;
        }

        public int GetTextureByCategory(int categoryid, int texture)
        {
            var result = 0;
            switch (categoryid)
            {
                case 25133:
                    result = 1101;
                    break;
                case 25134:
                    result = 1000;
                    break;
                case 25136:
                    result = 3000;
                    break;
                case 25075:
                    result = 1101;
                    break;
                case 25015:
                    result = 1101;
                    break;
                default:
                    result = texture;
                    break;
            }
            return result;
        }

        public string GetKindBySourceTargetNode(LaneLinePoint sourceNode, LaneLinePoint targetNode, int f1id,
            List<PublicServicePoint> publicServices)
        {
            if (sourceNode == null)
            {

            }
            if (sourceNode.PublicServiceId != 0 && sourceNode.FloorId == f1id)
            {
                var publicService = publicServices.FirstOrDefault(_ => _.OId == sourceNode.PublicServiceId);
                if (publicService?.CategoryId == 25062 || publicService?.CategoryId == 25140)
                {
                    return "04";
                }
            }
            if (targetNode.PublicServiceId != 0 && targetNode.FloorId == f1id)
            {
                var publicService = publicServices.FirstOrDefault(_ => _.OId == targetNode.PublicServiceId);
                if (publicService?.CategoryId == 25062 || publicService?.CategoryId == 25140)
                {
                    return "04";
                }
            }
            return "02";
        }

        /// <summary>
        /// 获取建筑类型
        /// </summary>
        /// <param name="mallType"></param>
        /// <returns></returns>
        public int GetCategory(string mallType)
        {
            int category = 1002;
            if(mallType== "综合商场")
            {
                category = 1203;
            }
            if (mallType == "飞机场")
            {
                category = 1502;
            }
            return category;
        }


        private IGeometry getLatLon(IGeometry geo)
        {
            ISpatialReferenceFactory6 spatialReferenceFactory6 = new SpatialReferenceEnvironmentClass();
            ISpatialReference pSR =
                spatialReferenceFactory6.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_Beijing1954);
            geo.Project(pSR);
            //IGeometry p = getLabelPoint(geo);
            //p.Project(pSR);
            return geo;
        }

        private IPoint getLabelPoint(IGeometry geo)
        {
            IArea a = geo as IArea;
            return a.LabelPoint;
        }

        //将平面坐标转换为地理坐标WGS1984(经纬度)
        public IPolyline GetGeo(IGeometry pg)
        {
            //case esriGeometryType.esriGeometryPolyline:
            IPolyline pt = new PolylineClass();
            //IPoint pt = new PointClass();
            //pt.PutCoords(pg.Envelope.XMax, pg.Envelope.YMax);    //这里IGeometry对象可换成IPoint等对象
            pt.Densify(pg.Envelope.XMax, pg.Envelope.YMax);
            ISpatialReferenceFactory2 spatialReferenceFact = new SpatialReferenceEnvironmentClass();
            IGeometry geo = (IGeometry)pt;
            geo.SpatialReference = spatialReferenceFact.CreateProjectedCoordinateSystem(pg.SpatialReference.FactoryCode);
            ISpatialReference pSR = spatialReferenceFact.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);
            geo.Project(pSR);
            return pt;
        }
    }
}
