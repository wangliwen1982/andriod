using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using Tencent_trans.Common;
using Tencent_trans.PalmapEntity;

namespace Tencent_trans
{
    class ProductEx
    {
        private IWorkspace fromWorkspace;
        private IWorkspace toWorkspace;
        private IWorkspace exportWorkspace;
        private IFeatureWorkspace fromFeatureWorkspace;
        private IFeatureWorkspace toFeatureWorkspace;
        private IFeatureWorkspace exportFeatureWorkspace;
        private ITransactions sdeTransactions;
        private ITransactionsOptions sdetTransactionsOptions;
        //private Dictionary<int, string> Altitude = new Dictionary<int, string>();

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

        private IFeatureClass T_Building;
        private IFeatureClass T_Cell;
        private IFeatureClass T_FL_Bound;
        private IFeatureClass T_FL_Region;
        private IFeatureClass T_Link;
        private IFeatureClass T_Logic_Region;
        private IFeatureClass T_Node;
        private IFeatureClass T_POI;

        private ITable T_Door;
        private ITable T_Elevator;
        private ITable T_Facility;
        private ITable T_Park;
        private ITable T_Shop;
        private ITable T_FL_Relation;

        private string _bldid;
        private string _bldname;

        public ProductEx(string sdepath, string exportpath)
        {
            //IWorkspaceFactory fromFactory = new AccessWorkspaceFactoryClass();
            //fromWorkspace = fromFactory.OpenFromFile(mdbpath, 0);
            //new FileGDBWorkspaceFactoryClass();
            //new AccessWorkspaceFactoryClass();

            IWorkspaceFactory exportFactory = new ShapefileWorkspaceFactoryClass();
            exportWorkspace = exportFactory.OpenFromFile(exportpath, 0);
            IWorkspaceFactory toFactory = new SdeWorkspaceFactoryClass();
            toWorkspace = toFactory.OpenFromFile(sdepath, 0);
            

            //fromFeatureWorkspace = fromWorkspace as IFeatureWorkspace;
            toFeatureWorkspace = toWorkspace as IFeatureWorkspace;
            exportFeatureWorkspace = exportWorkspace as IFeatureWorkspace;

            sdeTransactions = (ITransactions) toWorkspace;
            sdetTransactionsOptions = (ITransactionsOptions) toFeatureWorkspace;
            if (sdetTransactionsOptions.AutoCommitInterval != 0)
            {
                sdetTransactionsOptions.AutoCommitInterval = 0;
            }

            //toFeatureWorkspace.OpenFeatureClass("");
            //ITable tt = toFeatureWorkspace.OpenTable("");
            ////ICursor
            //IQueryFilter qf=new QueryFilterClass();
            //qf.WhereClause = "";
            //ISpatialFilter sf=new SpatialFilterClass();
            //sf.Geometry = null;
            //sf.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
            //sf.WhereClause = "";

            //Marshal.FinalReleaseComObject(fromFactory);
            Marshal.FinalReleaseComObject(toFactory);
            Marshal.FinalReleaseComObject(exportFactory);
        }

        public bool CleanFileGDB()
        {
            List<string> layers = GetTables(exportWorkspace);
            foreach (var layer in layers)
            {
                ITable toTable = exportFeatureWorkspace.OpenTable(layer);

                IQueryFilter f = new QueryFilterClass();
                f.WhereClause = "1=1";
                toTable.DeleteSearchedRows(f);
            }
            Program.logger.Debug("清理数据库，准备导出数据...");
            return true;
        }

        private List<string> GetTables(IWorkspace pWorkspace)
        {
            List<string> lst = new List<string>();
            IEnumDataset pEnumDataset = pWorkspace.get_Datasets(esriDatasetType.esriDTAny);
            pEnumDataset.Reset();
            IDataset pDataset;
            while ((pDataset = pEnumDataset.Next()) != null)
            {
                switch (pDataset.Type)
                {
                    case esriDatasetType.esriDTFeatureDataset:
                        IFeatureClassContainer pFeatureClassContainer = (IFeatureClassContainer)pDataset;
                        IEnumFeatureClass pEnumFeatureClass = pFeatureClassContainer.Classes;
                        IFeatureClass pFeatureClass;
                        while ((pFeatureClass = pEnumFeatureClass.Next()) != null)
                        {
                            IDataset dt = pFeatureClass as IDataset;
                            lst.Add(dt.Name);
                        }
                        break;
                    case esriDatasetType.esriDTTable:
                        lst.Add(pDataset.Name);
                        break;
                    default:
                        break;
                }
            }
            return lst;
        }

        public bool Export(string mapid)
        {
            MapID = mapid;
            ExportTable(mapid);
            return true;
        }


        private void ExportTable(string mapid)
        {
            var bdid = "";
            var m_poi_id = "";

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

            #endregion

            var palmapDb =
                new NpgsqlHelper(ConfigurationManager.ConnectionStrings["Palmap"].ConnectionString);
            var palmapData = new PalmapDataBaseData(palmapDb);

            #region palmapTables

            var mall = palmapData.GetMall(mapid);
            var floors = palmapData.GetFloors(mapid);
            var qualitys = palmapData.GetDataqualitys(mapid);
            var frames = palmapData.GetFrames(mapid);
            var shopPolygons = palmapData.GetShopPolygon(mapid);

            var shops = palmapData.GetShops(mapid);
            var doors = palmapData.GetDoorPoints(mapid);
            var escalators = palmapData.GetEscalatorPoints(mapid);
            var publicservices = palmapData.GetPublicServicePoints(mapid);
            var areas = palmapData.GetAreaPolygons(mapid);

            #endregion

            var cTime = qualitys.First().RenewData; //创建时间
            var mTime = qualitys.Last().RenewData; //修改时间
            var founder = qualitys.First().Renewer; //创建人
            var modifier = qualitys.Last().Renewer; //修改人

            var dicFrame = frames.GroupBy(_ => _.Floorid)
                .ToDictionary(_ => _.First().Floorid, _ => _.ToList());
            var f0Floorid = floors.First(_ => _.Seq == 0).Floorid;
            var f0Feature = dicFrame[f0Floorid][0];
            var dicFloor = floors.GroupBy(_ => _.Floorid).ToDictionary(_ => _.First().Floorid, _ => _.ToList());
            



            //base_indoor_city_model
            if (f0Feature != null)
            {
                IGeometry geo = P_Frame_Polygon.GetFeature(f0Feature.Oid).Shape;
                IFeature newFeature = cityModel.CreateFeature();
                newFeature.Shape = geo;
                newFeature.Value[newFeature.Fields.FindField("m_poi_id")] = m_poi_id;
                newFeature.Value[newFeature.Fields.FindField("bd_id")] = bdid;
                newFeature.Value[newFeature.Fields.FindField("category")] = bdid;
                newFeature.Value[newFeature.Fields.FindField("up_num")] = floors.Count(_ => _.Altitude > 0);
                newFeature.Value[newFeature.Fields.FindField("dw_num")] = floors.Count(_ => _.Altitude < 0);
                newFeature.Value[newFeature.Fields.FindField("c_time")] = cTime;
                newFeature.Value[newFeature.Fields.FindField("m_time")] = mTime;
                newFeature.Value[newFeature.Fields.FindField("s_data")] = qualitys.First().MapSouce;
                newFeature.Value[newFeature.Fields.FindField("founder")] = founder;
                newFeature.Value[newFeature.Fields.FindField("modifier")] = modifier;
                newFeature.Store();
            }

            //base_indoor_m_poi
            IFeature mPoiFeature = mPoi.CreateFeature();
            mPoiFeature.Value[mPoiFeature.Fields.FindField("m_poi_id")] = m_poi_id;
            mPoiFeature.Value[mPoiFeature.Fields.FindField("c_name")] = mall.ShowName;
            mPoiFeature.Value[mPoiFeature.Fields.FindField("e_name")] = mall.NameEN;
            mPoiFeature.Value[mPoiFeature.Fields.FindField("address")] = mall.Address;
            mPoiFeature.Value[mPoiFeature.Fields.FindField("category")] = "类别需要更改";
            mPoiFeature.Value[mPoiFeature.Fields.FindField("phone")] = mall.Phone;
            mPoiFeature.Value[mPoiFeature.Fields.FindField("default")] = mall.DefaultFloor;
            mPoiFeature.Value[mPoiFeature.Fields.FindField("c_time")] = cTime;
            mPoiFeature.Value[mPoiFeature.Fields.FindField("m_time")] = mTime;
            mPoiFeature.Value[mPoiFeature.Fields.FindField("remark")] = mall.Description;
            mPoiFeature.Value[mPoiFeature.Fields.FindField("founder")] = founder;
            mPoiFeature.Value[mPoiFeature.Fields.FindField("modifier")] = modifier;

            //dicFloorid
            var newDicFloor = new Dictionary<int, string>();
            var num = 1;
            foreach (var floor in floors.GroupBy(_ => _.Floorid).ToList())
            {
                var flId = $"{bdid}{num.ToString().PadLeft(5, '0')}";
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
                newFeature.Shape = geo;
                newFeature.Value[newFeature.Fields.FindField("fl_id")] = newDicFloor[frame.Floorid];
                newFeature.Value[newFeature.Fields.FindField("fl_name")] = currentFloor.Number;
                newFeature.Value[newFeature.Fields.FindField("bd_id")] = bdid;
                newFeature.Value[newFeature.Fields.FindField("f_descr")] = currentFloor.Style;
                newFeature.Value[newFeature.Fields.FindField("c_time")] = cTime;
                newFeature.Value[newFeature.Fields.FindField("m_time")] = mTime;
                newFeature.Value[newFeature.Fields.FindField("floornum")] = floornum;
                newFeature.Value[newFeature.Fields.FindField("elevation")] = 3;
                newFeature.Value[newFeature.Fields.FindField("founder")] = founder;
                newFeature.Value[newFeature.Fields.FindField("modifier")] = modifier;
                newFeature.Store();
            }

            var dicShopPolygon = new Dictionary<int, string>();

            //base_indoor_region
            foreach (var shopPolygon in shopPolygons)
            {
                var regionId = $"{newDicFloor[shopPolygon.Floorid]}{num.ToString().PadLeft(5, '0')}";
                var geo = P_Shop_Polygon.GetFeature(shopPolygon.Oid).Shape;
                var newFeature = indoorRegion.CreateFeature();
                newFeature.Shape = geo;
                newFeature.Value[newFeature.Fields.FindField("region_id")] = regionId;
                newFeature.Value[newFeature.Fields.FindField("fl_id")] = newDicFloor[shopPolygon.Floorid];
                newFeature.Value[newFeature.Fields.FindField("category")] = "";
                newFeature.Value[newFeature.Fields.FindField("c_name")] = shopPolygon.Shopnamecn ?? string.Empty;
                newFeature.Value[newFeature.Fields.FindField("e_name")] = shopPolygon.Shopnameen ?? string.Empty;
                //newFeature.Value[newFeature.Fields.FindField("wifi")] = string.Empty;
                newFeature.Value[newFeature.Fields.FindField("height")] = 3;
                newFeature.Value[newFeature.Fields.FindField("texture")] = shopPolygon.Texture;
                newFeature.Value[newFeature.Fields.FindField("c_time")] = cTime;
                newFeature.Value[newFeature.Fields.FindField("m_time")] = mTime;
                newFeature.Value[newFeature.Fields.FindField("founder")] = founder;
                newFeature.Value[newFeature.Fields.FindField("modifier")] = modifier;
                newFeature.Store();
                num++;
                dicShopPolygon.Add(shopPolygon.ObjId, regionId);
            }
            num = 1;

            //base_indoor_poi
            foreach (var publicservice in publicservices)
            {
                var poiId = $"{newDicFloor[publicservice.FloorId]}{num.ToString().PadLeft(5, '0')}";
                var geo = P_PublicService_Point.GetFeature(publicservice.OId).Shape;
            }

            //base_indoor_sub_region
            num = 1;
            foreach (var area in areas)
            {
                var regionid = $"{newDicFloor[area.FloorId]}{num.ToString().PadLeft(5, '0')}";
                var geo = P_Area_Polygon.GetFeature(area.OId).Shape;
                var newFeature = indoorSubRegion.CreateFeature();
                newFeature.Shape = geo;
                newFeature.Value[newFeature.Fields.FindField("region_id")] = regionid;
                newFeature.Value[newFeature.Fields.FindField("fl_id")] = newDicFloor[area.FloorId];
                newFeature.Value[newFeature.Fields.FindField("category")] = area.CategoryId;
                newFeature.Value[newFeature.Fields.FindField("texture")] = area.Texture;
                newFeature.Value[newFeature.Fields.FindField("c_name")] = area.Display;
                newFeature.Value[newFeature.Fields.FindField("c_time")] = cTime;
                newFeature.Value[newFeature.Fields.FindField("m_time")] = mTime;
                newFeature.Value[newFeature.Fields.FindField("founder")] = founder;
                newFeature.Value[newFeature.Fields.FindField("modifier")] = modifier;
            }
            //base_indoor_doors
            foreach (var door in doors)
            {
                
            }

        }

        public int GetFloorNum(Floor floor)
        {
            var starts = floor.Number.Substring(0, 1);
            var floornum = string.Empty;
            switch (starts)
            {
                case "F":
                    floornum = floor.Number.Substring(1);
                    break;
                case "B":
                    floornum = "-" + floor.Number.Substring(1);
                    break;
                case "M":
                    floornum = floor.Number.Substring(1);
                    break;
                case "N":
                    floornum = floor.Number.Substring(1);
                    break;
            }
            return Convert.ToInt32(floornum);
        }


    }
}
