using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using Npgsql;
using NpgsqlTypes;
using ESRI.ArcGIS.esriSystem;
using NLog;
using NLog.Targets;
using Path = System.IO.Path;

//using ESRI.ArcGIS.ADF;

namespace Tencent_trans
{
    class Product
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

        public Product(string sdepath, string exportpath,int item)
        {
            //IWorkspaceFactory fromFactory = new AccessWorkspaceFactoryClass();
            //fromWorkspace = fromFactory.OpenFromFile(mdbpath, 0);
            IWorkspaceFactory exportFactory = new FileGDBWorkspaceFactoryClass();
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
            //Marshal.FinalReleaseComObject(fromFactory);
            Marshal.FinalReleaseComObject(toFactory);
            Marshal.FinalReleaseComObject(exportFactory);
        }

        public Product(string mdbpath, string sdepath)
        {
            IWorkspaceFactory fromFactory = new AccessWorkspaceFactoryClass();
            fromWorkspace = fromFactory.OpenFromFile(mdbpath, 0);
            IWorkspaceFactory toFactory = new SdeWorkspaceFactoryClass();
            toWorkspace = toFactory.OpenFromFile(sdepath, 0);

            fromFeatureWorkspace = fromWorkspace as IFeatureWorkspace;
            toFeatureWorkspace = toWorkspace as IFeatureWorkspace;

            sdeTransactions = (ITransactions)toWorkspace;
            sdetTransactionsOptions = (ITransactionsOptions)toFeatureWorkspace;
            if (sdetTransactionsOptions.AutoCommitInterval != 0)
            {
                sdetTransactionsOptions.AutoCommitInterval = 0;
            }

            Marshal.FinalReleaseComObject(fromFactory);
            Marshal.FinalReleaseComObject(toFactory);
        }

        public bool Export(string mapid)
        {
            MapID = mapid;
            ExportTable(mapid);
            return true;
        }

        public void ExportWithFME(string mapid)
        {
            Dictionary<string,string>DIC_FL_Relation=new Dictionary<string, string>();
            ICursor fc =exportFeatureWorkspace.OpenTable("fl_relation").Search(null, true);
            IRow row;
            while ((row=fc.NextRow())!=null)
            {
                DIC_FL_Relation.Add(row.Value[row.Fields.FindField("relation_id")].ToString(),
                    row.Value[row.Fields.FindField("insitu_name")].ToString());
            }
            Marshal.FinalReleaseComObject(fc);
            string fmepath = "";
            string tencentpath = "";
            string outpath = System.IO.Path.Combine(Application.StartupPath, "Template", "Out");
            string fgdb = System.IO.Path.Combine(Application.StartupPath, "Template", "Tencent.gdb");
            string nullmif = System.IO.Path.Combine(Application.StartupPath, "Template", "MIDMIF");
            string fmw = System.IO.Path.Combine(Application.StartupPath, "Template", "filegdb2mif.fmw");
            if (Directory.Exists(outpath))
            {
                Directory.Delete(outpath,true);
            }
            Thread.Sleep(100);
            Directory.CreateDirectory(outpath);

            //调用FME脚本
            Process p=new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
            p.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
            p.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
            p.StartInfo.RedirectStandardError = true;//重定向标准错误输出
            p.StartInfo.CreateNoWindow = true;//不显示程序窗口
            //p.StartInfo.Arguments = @"fme ""C:\Users\songb\Documents\My FME Workspaces\filegdb2mif.fmw"" &exit";
            p.Start();//启动程序

            p.StandardInput.WriteLine(
                $@"fme ""{fmw}"" --SourceDataset {fgdb} --DestDataset {outpath} &exit");
            p.StandardInput.AutoFlush = true;

            p.WaitForExit();//等待程序执行完退出进程
            string output = p.StandardOutput.ReadToEnd();
            p.Close();

            Program.logger.Debug(output);
            Program.logger.Debug($"{mapid}导出MIDMIF完成");

            ChangeEncode(outpath);
            ChangeMID(outpath);
            ChangeMIF(outpath);
            DirectoryInfo di = new DirectoryInfo(outpath);
            DirectoryInfo dimif = new DirectoryInfo(nullmif);
            foreach (var path in di.GetDirectories())
            {
                if (DIC_FL_Relation.ContainsKey(path.Name))
                {
                    string outdi = Path.Combine(di.FullName, DIC_FL_Relation[path.Name]);
                    path.MoveTo(outdi);
                    foreach (var file in dimif.GetFiles())
                    {
                        if (!File.Exists(Path.Combine(outdi, file.Name)))
                        {
                            File.Copy(file.FullName, Path.Combine(outdi, file.Name), false);
                        }
                    }
                }
                else
                {
                    foreach (var file in path.GetFiles())
                    {
                        if (file.Name.Contains("Building"))
                        {
                            file.MoveTo(System.IO.Path.Combine(di.FullName, $"{_bldid}_Building_{_bldname}{file.Extension}"));
                        }
                        else
                        {
                            file.MoveTo(System.IO.Path.Combine(di.FullName, file.Name));
                        }
                    }
                    path.Delete();
                }
            }
            string workpath = Path.Combine(Application.StartupPath, "Work");

            if (!Directory.Exists(workpath))
            {
                Directory.CreateDirectory(workpath);
            }

            Directory.Move(outpath, Path.Combine(workpath, $"{_bldid}_{_bldname}"));
            //string outzip = Path.Combine(Application.StartupPath, "Output", $"out.zip");
            //if (File.Exists(outzip))
            //{
            //    File.Delete(outzip);
            //}
            //ZipFile.CreateFromDirectory(workpath, outzip);
            Program.logger.Debug($"{mapid}导出文件完成");
        }

        public bool CheckData(string mapid)
        {
            //FileTarget target = LogManager.Configuration.FindTargetByName<FileTarget>("t2");
            //target.FileName = @"${basedir}/logs/" + $"{mapid}.log";


            Program.logger.Info($"{mapid}----开始检查----");
            int i = 0;

            T_POI = exportFeatureWorkspace.OpenFeatureClass("poi");
            T_Cell = exportFeatureWorkspace.OpenFeatureClass("cell_function");

            IQueryDef2 def = exportFeatureWorkspace.CreateQueryDef() as IQueryDef2;
            def.Tables = "poi";
            def.SubFields = "";
            def.WhereClause = "name='' or name is null";

            ICursor cursor = def.Evaluate();
            IRow row;
            while ((row = cursor.NextRow()) != null)
            {
                i++;
                Program.logger.Info($"POI图层Name字段为空：{row.Value[row.Fields.FindField("poi_id")]}");
            }

            def.Tables = "cell_function left outer join poi on cell_function.unique_id=poi.area_id";
            def.SubFields = "cell_function.unique_id,poi.area_id";
            def.WhereClause = "poi.area_id is null";
            cursor = def.Evaluate();
            while ((row = cursor.NextRow()) != null)
            {
                i++;
                Program.logger.Info($"Cell图层没有关联POI：{row.Value[row.Fields.FindField("cell_function.unique_id")]}");
            }

            def.Tables = "poi";
            def.SubFields = "area_id,COUNT(area_id)";
            def.WhereClause = "area_id is not null and area_id <> '' GROUP BY area_id HAVING COUNT(area_id) > 1";
            //def.PostfixClause = "GROUP BY area_id having COUNT(area_id)>1";
            cursor = def.Evaluate();
            while ((row = cursor.NextRow()) != null)
            {
                i++;
                Program.logger.Info($"POI图层Area_id字段重复：{row.Value[row.Fields.FindField("area_id")]}");
            }

            //T_Node = exportFeatureWorkspace.OpenFeatureClass("cell_function");
            //T_Cell = exportFeatureWorkspace.OpenFeatureClass("cell_function");

            Program.logger.Info($"{mapid}----共检查出{i}个错误");
            Program.logger.Info($"{mapid}----检查结束");
            //IFeatureCursor cellCursor = T_Cell.Search(null, true);
            //IFeature cell;
            //while ((cell = cellCursor.NextFeature()) != null)
            //{
            //    IQueryFilter queryFilter=new QueryFilterClass();
            //    queryFilter.WhereClause = $"area_id = '{cell.Value[cell.Fields.FindField("unique_id")]}'";
            //    int count = T_POI.FeatureCount(queryFilter);

            //    Program.logger.Debug();
            //}
            if (i>0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }


        public void ChangeEncode(string path)
        {
            DirectoryInfo di = new DirectoryInfo(path);
            foreach (var file in di.GetFiles())
            {
                File.WriteAllText(file.FullName,
                    File.ReadAllText(file.FullName, Encoding.Default), new UTF8Encoding(false));
                //File.WriteAllText(System.IO.Path.Combine(outpath, file.Name),
                //    File.ReadAllText(file.FullName, Encoding.Default), new UTF8Encoding(false));

            }
            foreach (var fl in di.GetDirectories())
            {
                foreach (var file in fl.GetFiles())
                {
                    File.WriteAllText(file.FullName,
                        File.ReadAllText(file.FullName, Encoding.Default), new UTF8Encoding(false));
                    //File.WriteAllText(System.IO.Path.Combine(outpath, fl.Name, file.Name),
                    //    File.ReadAllText(file.FullName, Encoding.Default), new UTF8Encoding(false));
                }
            }
        }

        public void ChangeMID(string path)
        {
            string[] files = Directory.GetFiles(path, "*.mid", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                List<string> outList=new List<string>();
                var lines = File.ReadLines(file);
                foreach (var line in lines)
                {
                    var split = Regex.Split(line,",");
                    var re = string.Join(",", split.Select(_ => _.Contains(@"""") ? _ : $@"""{_}""").ToArray());

                    outList.Add(re);
                }
                File.WriteAllLines(file, outList);
            }
        }

        public void ChangeMIF(string path)
        {
            string[] files = Directory.GetFiles(path, "*.MIF", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                List<string> outList = new List<string>();
                var lines = File.ReadLines(file);
                bool flg = true;
                foreach (var line in lines)
                {
                    outList.Add(line);
                    if (line.ToLower() == "data")
                    {
                        outList.Add(string.Empty);
                    }
                }
                File.WriteAllLines(file, outList);
            }
        }

        public void Split()
        {

        }

        public bool InPut()
        {
            try
            {
                sdeTransactions.StartTransaction();
                List<string> layers = GetTables().Select(_ => _.Substring(_.LastIndexOf('.') + 1))
                    .Where(_ => !_.ToLower().StartsWith("t_") && !_.ToLower().StartsWith("dic")).ToList();
                GetWYID();

                foreach (var layer in layers)
                {
                    InputTable(layer);
                }
                sdeTransactions.CommitTransaction();
                return true;
            }
            catch (Exception e)
            {
                Program.logger.Debug(e);
                sdeTransactions.AbortTransaction();
                return false;
            }
            
        }

        public bool Clean()
        {
            List<string> layers = GetTables().Select(_ => _.Substring(_.LastIndexOf('.') + 1))
                .Where(_ => !_.ToLower().StartsWith("t_") && !_.ToLower().StartsWith("dic")).ToList();
            foreach (var layer in layers)
            {
                ITable toTable = toFeatureWorkspace.OpenTable(layer);

                IQueryFilter f = new QueryFilterClass();
                f.WhereClause = "1=1";
                toTable.DeleteSearchedRows(f);
            }
            return true;
        }

        public bool Clean(string whereClause)
        {
            List<string> layers = GetTables().Select(_ => _.Substring(_.LastIndexOf('.') + 1))
                .Where(_ => !_.ToLower().StartsWith("t_") && !_.ToLower().StartsWith("dic")).ToList();
            foreach (var layer in layers)
            {
                ITable toTable = toFeatureWorkspace.OpenTable(layer);

                IQueryFilter f = new QueryFilterClass();
                f.WhereClause = whereClause;
                toTable.DeleteSearchedRows(f);
            }
            return true;
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

        private void GetWYID()
        {
            ITable fromTable = fromFeatureWorkspace.OpenTable("Mall");
            ICursor fromCursor = fromTable.Search(null, true);

            IRow fromRow;
            while ((fromRow = fromCursor.NextRow()) != null)
            {
                MapID = fromRow.Value[fromRow.Fields.FindField("mapid")].ToString();
            }
            Marshal.FinalReleaseComObject(fromCursor);

            if (MapID.Trim() == string.Empty)
            {
                throw new Exception("MapID为空");
            }
        }

        private List<string> GetTables()
        {
            List<string> lst = new List<string>();
            IEnumDataset pEnumDataset = toWorkspace.get_Datasets(esriDatasetType.esriDTAny);
            pEnumDataset.Reset();
            IDataset pDataset;
            while ((pDataset = pEnumDataset.Next()) != null)
            {
                switch (pDataset.Type)
                {
                    case esriDatasetType.esriDTFeatureDataset:
                        IFeatureClassContainer pFeatureClassContainer = (IFeatureClassContainer) pDataset;
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

        private void InputTable(string from)
        {
            //string to = "palmap.sde." + from;
            string to = from;


            #region MyRegion

            //IQueryDef def = mFeatureWorkspace.CreateQueryDef();

            //def.Tables = string.Format("{0}, {1}", "Floors", "Altitude");
            //def.SubFields = string.Format("{0}.*", "Floors");
            //def.WhereClause = string.Format("{0}.{1} = {2}.{3} AND {2}.{4} = 0", new object[] { "Floors", "AltitudeID", "Altitude", "ID", "Altitude" });

            //ICursor floor = def.Evaluate();

            //IRow row = floor.NextRow();

            #endregion

            try
            {
                ITable fromTable = fromFeatureWorkspace.OpenTable(from);
                ITable toTable = toFeatureWorkspace.OpenTable(to);

                IQueryFilter f = new QueryFilterClass();
                f.WhereClause = $"mapid = '{MapID}'";
                toTable.DeleteSearchedRows(f);

                var fromColumns = GetColumns(fromTable);
                var toColumns = GetColumns(toTable);
                ICursor fromCursor = fromTable.Search(null, true);
                ICursor sdeCursor = toTable.Insert(true);
                IRow fromRow;
                while ((fromRow = fromCursor.NextRow()) != null)
                {
                    IRowBuffer sdeFeatureBuffer = toTable.CreateRowBuffer();
                    IFields fromFields = fromRow.Fields;
                    //var fromColumn = GetColumns(fromTable);
                    for (var i = 0; i < fromFields.FieldCount; i++)
                    {
                        var fromField = fromFields.Field[i];
                        var fieldName = fromField.Name.ToLower();
                        if (toColumns.ContainsKey(fieldName))
                        {
                            sdeFeatureBuffer.Value[toColumns[fieldName]] = fromRow.Value[i];
                        }
                        if (fieldName == "to")
                        {
                            sdeFeatureBuffer.Value[toColumns["to_"]] = fromRow.Value[i];
                        }
                    }
                    if (toColumns.ContainsKey("objid"))
                    {
                        sdeFeatureBuffer.Value[toColumns["objid"]] = fromRow.OID;
                    }
                    if (toColumns.ContainsKey("mapid"))
                    {
                        sdeFeatureBuffer.Value[toColumns["mapid"]] = MapID;
                    }
                    //if (toColumns.ContainsKey("to_"))
                    //{
                    //    sdeFeatureBuffer.Value[toColumns["to_"]] = fromRow.Value[fromColumns["to"]];
                    //}

                    sdeCursor.InsertRow(sdeFeatureBuffer);
                    sdeCursor.Flush();
                }
                Marshal.FinalReleaseComObject(sdeCursor);
                Marshal.FinalReleaseComObject(fromCursor);

            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private void ExportTable(string mapid)
        {
            #region FeatureClass

            P_Frame_Polygon = toFeatureWorkspace.OpenFeatureClass("Frame_Polygon");
            P_Area_Polygon = toFeatureWorkspace.OpenFeatureClass("Area_Polygon");
            P_Shop_Polygon = toFeatureWorkspace.OpenFeatureClass("Shop_Polygon");
            P_Door_Point = toFeatureWorkspace.OpenFeatureClass("Door_Point");
            P_Escalator_Point = toFeatureWorkspace.OpenFeatureClass("Escalator_Point");
            P_PublicService_Point = toFeatureWorkspace.OpenFeatureClass("PublicService_Point");
            P_Lane_Line = toFeatureWorkspace.OpenFeatureClass("Lane_Line");
            P_LanePoint_Point = toFeatureWorkspace.OpenFeatureClass("LanePoint_Point");

            T_Building = exportFeatureWorkspace.OpenFeatureClass("building");
            T_Cell = exportFeatureWorkspace.OpenFeatureClass("Cell_Function");
            T_FL_Bound = exportFeatureWorkspace.OpenFeatureClass("FL_Bound");
            T_FL_Region = exportFeatureWorkspace.OpenFeatureClass("FL_Region");
            T_Link = exportFeatureWorkspace.OpenFeatureClass("Link");
            T_Logic_Region = exportFeatureWorkspace.OpenFeatureClass("Logic_Region");
            T_Node = exportFeatureWorkspace.OpenFeatureClass("Node");
            T_POI = exportFeatureWorkspace.OpenFeatureClass("POI");
            T_Door = exportFeatureWorkspace.OpenTable("Door");
            T_Elevator = exportFeatureWorkspace.OpenTable("Elevator");
            T_Facility = exportFeatureWorkspace.OpenTable("Facility");
            T_Park = exportFeatureWorkspace.OpenTable("Parking");
            T_Shop = exportFeatureWorkspace.OpenTable("Shop");
            T_FL_Relation = exportFeatureWorkspace.OpenTable("FL_Relation");

            #endregion

            NpgsqlHelper PalmapDB =
                new NpgsqlHelper(ConfigurationManager.ConnectionStrings["Palmap"].ConnectionString);

            #region Mall

            var MallTable = PalmapDB.ExecuteDataTable($"select * from Mall where mapid = '{mapid}'");
            var Mall = MallTable.AsEnumerable().Select(_ => new
            {
                UDID = _.Field<int>("UDID"),
                MapID = _.Field<string>("MapID"),
                NameCN = _.Field<string>("NameCN")==null?null: _.Field<string>("NameCN").Replace("（", "(").Replace("）", ")"),
                NameEN = _.Field<string>("NameEN")==null?null: _.Field<string>("NameEN").Replace("（", "(").Replace("）", ")"),
                ShowName = _.Field<string>("ShowName")==null?null: _.Field<string>("ShowName").Replace("（", "(").Replace("）", ")"),
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

            #endregion

            #region

            DataTable AltitudeTable = PalmapDB.ExecuteDataTable(
                $"select f.objid,f.seq,f.number,f.name,a.altitude,f.style from floors f left join altitude a on f.altitudeid=a.objid where f.mapid='{mapid}' and a.mapid='{mapid}' order by a.altitude ");
            int F1 = AltitudeTable.Select("altitude > 0").FirstOrDefault()?["seq"] == null
                ? Convert.ToInt16(AltitudeTable.Select("altitude < 0").Last()["seq"]) + 1
                : Convert.ToInt16(AltitudeTable.Select("altitude > 0").FirstOrDefault()["seq"]);
            var Floors = AltitudeTable.AsEnumerable().Select(_ => new
            {
                FloorID = _.Field<int>("objid"),
                Seq = _.Field<short>("seq"),
                Number = _.Field<string>("number"),
                Name = _.Field<string>("name"),
                Style = _.Field<string>("style")==null?null: _.Field<string>("style").Replace("（", "(").Replace("）", ")"),
                Altitude = _.Field<decimal>("altitude"),
                FL_ID = _.Field<short>("seq") == 0
                    ? string.Empty
                    : (_.Field<short>("seq") >= F1
                        ? "1" + (_.Field<short>("seq") - F1 + 1).ToString().PadLeft(3, '0')
                        : "0" + (F1 - _.Field<short>("seq")).ToString().PadLeft(3, '0')),
                Seq_ID = _.Field<short>("seq") == 0
                    ? 0
                    : _.Field<short>("seq") >= F1
                        ? _.Field<short>("seq") - F1 + 1
                        : _.Field<short>("seq") - F1
            }).ToDictionary(_ => _.FloorID, _ => _);

            var FL_ID = Floors.ToDictionary(_ => _.Key, _ => _.Value.FL_ID);

            var DIC_Category = PalmapDB.ExecuteDataTable("select * from dic_category").AsEnumerable().Select(_ => new
            {
                ID = _.Field<int>("ID"),
                Category = _.Field<string>("Category"),
                TencentID = _.Field<string>("TencentID"),
                TypeName = _.Field<string>("分类名称"),
                Cell_Type = _.Field<string>("cell_type"),
                Cell_TypeName = _.Field<string>("cell_type名称")
            }).ToDictionary(_ => _.ID, _ => _);

            var DIC_City = PalmapDB.ExecuteDataTable("select * from dic_city").AsEnumerable().Select(_ => new
            {
                City = _.Field<string>("城市"),
                CityCode = _.Field<int?>("城市编码"),
                Province = _.Field<string>("省份"),
                ProvinceCode = _.Field<int>("省编码"),
            }).ToList();

            var DIC_MallType = PalmapDB.ExecuteDataTable("select * from dic_malltype").AsEnumerable().Select(_ => new
            {
                PalmapID = _.Field<int>("palmapid"),
                Name = _.Field<string>("name"),
                TencentID = _.Field<int>("腾讯")
            }).ToDictionary(_ => _.Name, _ => _);

            var Shops = PalmapDB.ExecuteDataTable($"select * from shops where mapid='{mapid}'").AsEnumerable().Select(
                _ => new
                {
                    ShopID = _.Field<int>("objid"),
                    ShopNameCN = _.Field<string>("shopnamecn") == null
                        ? string.Empty
                        : _.Field<string>("shopnamecn").Trim().Replace("（", "(").Replace("）", ")"),
                    ShopNameEN = _.Field<string>("shopnameen") == null
                        ? string.Empty
                        : _.Field<string>("shopnameen").Trim().Replace("（", "(").Replace("）", ")"),
                    Booth = _.Field<string>("booth"),
                    Display = _.Field<string>("display")==null?null: _.Field<string>("display").Replace("（", "(").Replace("）", ")"),
                    Specialname = _.Field<short>("specialname"),
                    Categoryid = _.Field<int>("categoryid"),
                    FloorID = _.Field<int>("floorid"),
                    RoomNum = _.Field<string>("roomnum")==null?null: _.Field<string>("roomnum").Replace("（", "(").Replace("）", ")"),
                    Alias= _.Field<string>("alias")
                }).ToDictionary(_ => _.ShopID, _ => _);

            var ShopPolygon = PalmapDB.ExecuteDataTable($"select * from shop_polygon where mapid='{mapid}'")
                .AsEnumerable().Select(_ => new
                {
                    ShopPolygonID = _.Field<int>("objid"),
                    ObjectID= _.Field<int>("objectid"),
                    ShopID = _.Field<int?>("shopid"),
                    Booth = _.Field<string>("booth"),
                    Categoryid = _.Field<int?>("categoryid"),
                    FloorID = _.Field<int>("floorid"),
                    FeatureID = _.Field<string>("featureid")
                }).ToDictionary(_ => _.ShopPolygonID, _ => _);
            ;

            var DataQuality = PalmapDB
                .ExecuteDataTable($"select * from dataquality where mapid='{mapid}' order by objid").AsEnumerable()
                .Select(_ => new
                {
                    Collector = _.Field<string>("collector"),
                    RenewDate = _.Field<DateTime>("renewdate").ToString("yyyy-MM-dd HH:mm:ss"),
                    MapSource = _.Field<string>("mapsource")
                }).ToList();
            var Collect_Date = DataQuality.First();
            var Update_Date = DataQuality.Last();

            //var c = PalmapDB.ExecuteDataTable("select e.objid,c.objid as from,c.to_ as to,c.type from escalator_point e left join connections c on e.connectionid=c.objid").Columns["objid"].DataType;
            //var d = PalmapDB.ExecuteDataTable("select e.objid,c.objid as from,c.to_ as to,c.type from escalator_point e left join connections c on e.connectionid=c.objid").Columns["from"].DataType;
            //var e = PalmapDB.ExecuteDataTable("select e.objid,c.objid as from,c.to_ as to,c.type from escalator_point e left join connections c on e.connectionid=c.objid").Columns["to"].DataType;
            //var g = PalmapDB.ExecuteDataTable("select e.objid,c.objid as from,c.to_ as to,c.type from escalator_point e left join connections c on e.connectionid=c.objid").Columns["type"].DataType;

            var DIC_EscalatorType = PalmapDB.ExecuteDataTable("select * from dic_escalatortype").AsEnumerable().Select(
                _ => new
                {
                    ID = _.Field<int>("id"),
                    Display = _.Field<string>("display"),
                    Oneway = _.Field<Int16>("oneway"),
                    CategoryID = _.Field<int>("categoryid")
                }).ToDictionary(_ => _.ID, _ => _);
            var Escalator_Connection = PalmapDB
                .ExecuteDataTable(
                    $"select e.objid,c.objid as from,c.to_ as to,c.type from escalator_point e left join connections c on e.connectionid=c.objid  where e.mapid='{mapid}' and c.mapid='{mapid}'")
                .AsEnumerable().Select(
                    _ => new
                    {
                        ID = _.Field<int>("objid"),
                        From = _.Field<int?>("from"),
                        To = _.Field<int?>("to"),
                        Type = _.Field<int>("type")
                    }).ToDictionary(_ => _.ID, _ => _);

            var DIC_PublicServiceType = PalmapDB.ExecuteDataTable($"select * from dic_publicservicetype").AsEnumerable()
                .Select(
                    _ => new
                    {
                        ID = _.Field<int>("objectid"),
                        Display = _.Field<string>("display"),
                        CategoryID = _.Field<int>("categoryid")
                    }).ToDictionary(_ => _.ID, _ => _);

            var Connections = PalmapDB.ExecuteDataTable($"select * from connections where mapid = '{mapid}'")
                .AsEnumerable().Select(_ => new
                {
                    From = _.Field<int>("objid"),
                    Type = _.Field<int>("type"),
                    To = _["to_"] == DBNull.Value ? 0 : _.Field<int>("to_"),
                    Oneway = DIC_EscalatorType[_.Field<int>("type")].Oneway
                });

            var Escalator_Point = PalmapDB.ExecuteDataTable($"select * from escalator_point where mapid = '{mapid}'")
                .AsEnumerable().Select(_ => new
                {
                    OID = _.Field<int>("objectid"),
                    ID= _.Field<int>("objid"),
                    FloorID = _.Field<int>("floorid"),
                    ConnectionID = _.Field<int>("connectionid"),
                    FeatureID = _.Field<string>("featureid")
                }).ToDictionary(_ => _.ConnectionID, _ => _);

            string BLD_ID = string.Format("{0}{1}{2}",
                DIC_City.Where(_ => _.City.StartsWith(Mall.City)).First().CityCode, "002",
                Mall.UDID.ToString().PadLeft(5, '0'));
            _bldid = BLD_ID;
            _bldname = Mall.NameCN;

            #endregion

            Program.logger.Debug($"{mapid}读取数据完成...");

            ICursor exportCursor;

            DeleteTable(T_FL_Relation);
            var RelationColumns = GetColumns(T_FL_Relation);
            exportCursor = T_FL_Relation.Insert(true);
            foreach (var Floor in Floors.Values)
            {
                if (Floor.Seq != 0)
                {
                    IRowBuffer buffer = T_FL_Relation.CreateRowBuffer();
                    buffer.Value[RelationColumns["relation_id"]] = BLD_ID + Floor.FL_ID;
                    buffer.Value[RelationColumns["bld_id"]] = BLD_ID;
                    buffer.Value[RelationColumns["seq_id"]] = Floor.Seq_ID;
                    buffer.Value[RelationColumns["insitu_name"]] = Floor.Number;
                    exportCursor.InsertRow(buffer);
                }
            }
            exportCursor.Flush();
            Marshal.FinalReleaseComObject(exportCursor);

            DeleteTable(T_Building);
            DeleteTable(T_FL_Bound);
            DeleteTable(T_FL_Region);
            var BuildingColumns = GetColumns(T_Building as ITable);
            var FL_BoundColumns = GetColumns(T_FL_Bound as ITable);
            var Frames = PalmapDB.ExecuteDataTable($"select objectid,floorid from frame_polygon where mapid='{mapid}'")
                .AsEnumerable().Select(
                    _ =>
                        new
                        {
                            OID = _.Field<int>("objectid"),
                            FloorID = _.Field<int>("floorid")
                        });
            var FL_Frames = Frames.GroupBy(_ => _.FloorID).ToDictionary(_ => _.First().FloorID, _ => _.ToList());

            foreach (var frame in FL_Frames)
            {
                IGeometry geo = P_Frame_Polygon.GetFeature(frame.Value[0].OID).Shape;
                if (Floors[frame.Key].Seq == 0)
                {
                    IFeature newFeature = T_Building.CreateFeature();
                    newFeature.Shape = geo;
                    newFeature.Value[BuildingColumns["bld_id"]] = BLD_ID;
                    newFeature.Value[BuildingColumns["name_cn"]] = Mall.NameCN;
                    newFeature.Value[BuildingColumns["name_en"]] = Mall.NameEN;
                    //newFeature.Value[BuildingColumns["alias"]]
                    newFeature.Value[BuildingColumns["type"]] = DIC_MallType?[Mall.MallType].TencentID;
                    //newFeature.Value[BuildingColumns["outer_poi"]]
                    //newFeature.Value[BuildingColumns["base_height"]]
                    newFeature.Value[BuildingColumns["office_hours"]] = Mall.OpeningTime;
                    newFeature.Value[BuildingColumns["g_fl"]] =
                        Convert.ToInt32(Regex.Replace(Floors.Last().Value.Number, "[a-z]", "",
                            RegexOptions.IgnoreCase));
                    newFeature.Value[BuildingColumns["ug_fl"]] = Floors.Where(_ => _.Value.Altitude < 0).Count();
                    newFeature.Value[BuildingColumns["parking_lot"]] = Mall.CarPark.Equals(0) ? 0 : 4;
                    newFeature.Value[BuildingColumns["add_cn"]] = Mall.Address;
                    newFeature.Value[BuildingColumns["web_site"]] = Mall.WebSite;
                    newFeature.Value[BuildingColumns["city"]] = Mall.City;
                    newFeature.Value[BuildingColumns["update_date"]] = Update_Date.RenewDate;
                    IPoint pt = getLatLon(newFeature.ShapeCopy);
                    newFeature.Value[BuildingColumns["lat"]] = pt.Y;
                    newFeature.Value[BuildingColumns["lon"]] = pt.X;
                    newFeature.Value[BuildingColumns["default_fl"]] = BLD_ID +
                                                                      Floors.Where(_ =>
                                                                              _.Value.Number == Mall.DefaultFloor)
                                                                          .FirstOrDefault().Value.FL_ID;
                    newFeature.Store();
                }
                else
                {
                    //IGeometry geo = P_Frame_Polygon.GetFeature(frame.Value[0].OID).Shape;
                    string has_region = "0";
                    IFeature newFeature = T_FL_Bound.CreateFeature();
                    if (frame.Value.Count == 1)
                    {
                        newFeature.Shape = geo;
                    }
                    else
                    {
                        if (Floors[frame.Key].Altitude > 0)
                        {
                            newFeature.Shape = P_Frame_Polygon.GetFeature(FL_Frames[Floors.First(_ => _.Value.Seq == 0).Key][0].OID).Shape;
                        }
                        else
                        {
                            newFeature.Shape = UnionGeo(frame.Value.Select(_=>_.OID.ToString()).ToList(), P_Frame_Polygon);
                        }
                        int i = 1;
                        foreach (var region in frame.Value)
                        {
                            IFeature fl_region = T_FL_Region.CreateFeature();
                            fl_region.Shape = P_Frame_Polygon.GetFeature(region.OID).Shape;
                            fl_region.Value[fl_region.Fields.FindField("region_id")] =
                                BLD_ID + FL_ID[frame.Key] + "00" + i.ToString().PadLeft(5, '0');
                            fl_region.Value[fl_region.Fields.FindField("bld_id")] = BLD_ID;
                            fl_region.Value[fl_region.Fields.FindField("fl_id")] = BLD_ID + FL_ID[frame.Key] ;
                            //fl_region.Value[fl_region.Fields.FindField("name")] = "";
                            fl_region.Store();
                            i++;
                        }
                        has_region = "1";
                    }
                    newFeature.Value[FL_BoundColumns["fl_id"]] = BLD_ID + Floors[frame.Key].FL_ID;
                    newFeature.Value[FL_BoundColumns["bld_id"]] = BLD_ID;
                    newFeature.Value[FL_BoundColumns["fl_seq"]] = Floors[frame.Key].Seq_ID;
                    newFeature.Value[FL_BoundColumns["name"]] = Floors[frame.Key].Number;
                    newFeature.Value[FL_BoundColumns["alias"]] = Floors[frame.Key].Style;
                    newFeature.Value[FL_BoundColumns["data_src"]] =
                        TransDataSrc(Update_Date.MapSource); //Update_Date.MapSource;
                    newFeature.Value[FL_BoundColumns["height"]] = 5;
                    newFeature.Value[FL_BoundColumns["has_region"]] = has_region;
                    newFeature.Value[FL_BoundColumns["collect_date"]] = Collect_Date.RenewDate;
                    //newFeature.Value[FL_BoundColumns["collect_meth"]]
                    newFeature.Value[FL_BoundColumns["update_date"]] = Update_Date.RenewDate;
                    newFeature.Store();

                }
                Console.Write("");
            }

            Program.logger.Debug($"{mapid}导出外框完成...");

            DeleteTable(T_POI);
            var PoiColumns = GetColumns(T_POI as ITable);

            //Logic_Region
            DeleteTable(T_Logic_Region);
            DeleteTable(T_Shop);
            DeleteTable(T_Park);
            DeleteTable(T_Elevator);
            var Logic_RegionColumns = GetColumns(T_Logic_Region as ITable);
            var AreaPolygons = PalmapDB
                .ExecuteDataTable($"select objectid,floorid,shopid from area_polygon where mapid='{mapid}'")
                .AsEnumerable()
                .Select(_ =>
                    new
                    {
                        OID = _.Field<int>("objectid"),
                        FloorID = _.Field<int>("floorid"),
                        ShopID = _.Field<int>("shopid")
                    }).ToList().GroupBy(_ => _.FloorID).ToDictionary(_ => _.First().FloorID, _ => _.ToList());
            foreach (var area_FL in AreaPolygons)
            {
                //var area_lst = area_FL.ToList();
                for (int i = 0; i < area_FL.Value.Count; i++)
                {
                    var area = area_FL.Value[i];
                    IGeometry geo = P_Area_Polygon.GetFeature(area.OID).Shape;
                    IFeature fea = T_Logic_Region.CreateFeature();
                    fea.Shape = geo;
                    string lg_id = BLD_ID + FL_ID[area.FloorID] + "01" + (i + 1).ToString().PadLeft(5, '0');
                    fea.Value[Logic_RegionColumns["lg_id"]] = lg_id;
                    fea.Value[Logic_RegionColumns["bld_id"]] = BLD_ID;
                    fea.Value[Logic_RegionColumns["fl_id"]] = BLD_ID + FL_ID[area.FloorID];
                    //f.Value[Logic_RegionColumns["region_id"]]
                    fea.Store();

                    var shop = Shops[area.ShopID];
                    string uniqueid = BLD_ID + FL_ID[area.FloorID] + "055" + (i + 1).ToString().PadLeft(4, '0');

                    IFeature pt = T_POI.CreateFeature();
                    pt.Shape = getLabelPoint(geo);
                    pt.Value[PoiColumns["poi_id"]] = uniqueid;
                    //pt.Value[PoiColumns["uni_poi_id"]]
                    pt.Value[PoiColumns["bld_id"]] = BLD_ID;
                    pt.Value[PoiColumns["fl_id"]] = BLD_ID + FL_ID[area.FloorID];

                    pt.Value[PoiColumns["cell_type"]] = DIC_Category[shop.Categoryid].Cell_Type;
                    //pt.Value[PoiColumns["height"]]
                    //pt.Value[PoiColumns["parent_id"]]
                    pt.Value[PoiColumns["booth_id"]] = shop.RoomNum;
                    pt.Value[PoiColumns["poi_code"]] = DIC_Category[shop.Categoryid].TencentID;

                    pt.Value[PoiColumns["name"]] = shop.ShopNameCN == string.Empty ? shop.ShopNameEN : shop.ShopNameCN;
                    pt.Value[PoiColumns["name_cn"]] = shop.ShopNameCN;
                    pt.Value[PoiColumns["name_en"]] = shop.ShopNameEN;

                    pt.Value[PoiColumns["alias"]] = shop.Alias;
                    pt.Value[PoiColumns["tel"]] = Mall.Phone;
                    pt.Value[PoiColumns["office_hours"]] = Mall.OpeningTime;
                    //pt.Value[PoiColumns["photos"]]
                    pt.Value[PoiColumns["collect_date"]] = Collect_Date.RenewDate;
                    pt.Value[PoiColumns["update_date"]] = Update_Date.RenewDate;
                    pt.Value[PoiColumns["lg_id"]] = lg_id;
                    //pt.Value[PoiColumns["area_id"]]
                    //pt.Value[PoiColumns["node_id"]] = GetNode_ID(pt.Shape, BLD_ID + FL_ID[floorid]);


                    //pt.Value[PoiColumns["node_id"]]=
                    pt.Store();

                }
            }

            Program.logger.Debug($"{mapid}逻辑分区外框完成...");

            //Cell
            DeleteTable(T_Cell);
            var CellColumns = GetColumns(T_Cell as ITable);
            var Shop_polygonColumns = GetColumns(P_Shop_Polygon as ITable);
            Dictionary<string, string> Shop_Polygon_Unique = new Dictionary<string, string>();
            IFeatureCursor cellCursor = T_Cell.Insert(true);
            foreach (var OID in GetOIDs($"mapid = '{mapid}'", P_Shop_Polygon))
            {
                IFeatureBuffer cell = T_Cell.CreateFeatureBuffer();
                IFeature old = P_Shop_Polygon.GetFeature(OID);
                cell.Shape = old.Shape;
                int floorid = Convert.ToInt16(old.Value[Shop_polygonColumns["floorid"]]);
                string featureid = Convert.ToString(old.Value[Shop_polygonColumns["featureid"]]);
                string floor_t = FL_ID[floorid];

                string uniqueid = BLD_ID + floor_t + "02" +
                                  featureid.Substring(featureid.Length - 4, 4).PadLeft(5, '0');

                cell.Value[CellColumns["unique_id"]] = uniqueid;

                cellCursor.InsertFeature(cell);
                //cell.Store();
                Shop_Polygon_Unique.Add(featureid, uniqueid);
            }
            cellCursor.Flush();
            Marshal.FinalReleaseComObject(cellCursor);

            Program.logger.Debug($"{mapid}导出Cell完成...");

            //LanePoint
            var NodeColumns = GetColumns(T_Node as ITable);
            var LinkColumns = GetColumns(T_Link as ITable);

            Dictionary<string, string> LanePoint_Unique = new Dictionary<string, string>();
            Dictionary<int, string> objid_LanePoint = new Dictionary<int, string>();
            DeleteTable(T_Node);
            //laneLine_Point
            IFeatureCursor lanepointCursor = T_Node.Insert(true);
            var LanePointColumns = GetColumns(P_LanePoint_Point as ITable);
            foreach (var OID in GetOIDs($"mapid = '{mapid}'", P_LanePoint_Point))
            {
                IFeatureBuffer node = T_Node.CreateFeatureBuffer();
                IFeature old = P_LanePoint_Point.GetFeature(OID);
                node.Shape = old.Shape;

                int objid = Convert.ToInt32(old.Value[LanePointColumns["objid"]]);
                int floorid = Convert.ToInt16(old.Value[LanePointColumns["floorid"]]);
                string featureid = Convert.ToString(old.Value[LanePointColumns["nodeid"]]);
                //int con_id = Convert.ToInt32(old.Value[LanePointColumns["connectionid"]]);

                string uniqueid = BLD_ID + FL_ID[floorid] + "03" +
                                  featureid.Substring(featureid.Length - 4, 4).PadLeft(5, '0');

                node.Value[NodeColumns["fl_id"]] = BLD_ID + FL_ID[floorid];
                node.Value[NodeColumns["node_id"]] = uniqueid;
                node.Value[NodeColumns["bld_id"]] = BLD_ID;
                node.Value[NodeColumns["type"]] = 1;



                //node.Value[NodeColumns["connect_node"]] = connect_node;
                lanepointCursor.InsertFeature(node);
                //node.Store();
                LanePoint_Unique.Add(featureid, uniqueid);
                objid_LanePoint.Add(objid, uniqueid);
            }
            lanepointCursor.Flush();
            Marshal.FinalReleaseComObject(lanepointCursor);

            //LaneLine
            DeleteTable(T_Link);
            IFeatureCursor lanelineCursor = T_Link.Insert(true);
            var LaneLineColumns = GetColumns(P_Lane_Line as ITable);
            foreach (var OID in GetOIDs($"mapid = '{mapid}'", P_Lane_Line))
            {
                IFeatureBuffer link = T_Link.CreateFeatureBuffer();
                IFeature old = P_Lane_Line.GetFeature(OID);
                link.Shape = old.Shape;

                int floorid = Convert.ToInt16(old.Value[LaneLineColumns["floorid"]]);
                string featureid = Convert.ToString(old.Value[LaneLineColumns["linkid"]]);

                int startpointid = Convert.ToInt32(old.Value[LaneLineColumns["startpointid"]]);
                int endpointid = Convert.ToInt32(old.Value[LaneLineColumns["endpointid"]]);

                string uniqueid = BLD_ID + FL_ID[floorid] + "04" +
                                  featureid.Substring(featureid.Length - 4, 4).PadLeft(5, '0');

                link.Value[LinkColumns["bld_id"]] = BLD_ID;
                link.Value[LinkColumns["fl_id"]] = BLD_ID + FL_ID[floorid];
                link.Value[LinkColumns["s_node_id"]] = objid_LanePoint[startpointid];
                link.Value[LinkColumns["end_node_id"]] = objid_LanePoint[endpointid];
                link.Value[LinkColumns["link_id"]] = uniqueid;
                link.Value[LinkColumns["length"]] = Math.Round(getLength(old.Shape), 2);
                link.Value[LinkColumns["type"]] = old.Value[LaneLineColumns["detail"]];
                //int direction = Convert.ToInt16(old.Value[LaneLineColumns["detail"]]);
                link.Value[LinkColumns["direction"]] = Convert.ToInt16(old.Value[LaneLineColumns["direction"]]) + 1;
                link.Value[LinkColumns["is_slope"]] = 0;
                link.Value[LinkColumns["is_stairs"]] = 0;
                link.Value[LinkColumns["is_barr_free"]] = 1;
                link.Value[LinkColumns["category"]] = old.Value[LaneLineColumns["weight"]];
                lanelineCursor.InsertFeature(link);
                //link.Store();
            }
            lanelineCursor.Flush();
            Marshal.FinalReleaseComObject(lanelineCursor);

            Program.logger.Debug($"{mapid}导出路网完成...");

            //POI
            DeleteTable(T_Door);
            DeleteTable(T_Elevator);
            DeleteTable(T_Facility);

            //Door
            var DoorColumns = GetColumns(P_Door_Point as ITable);
            var T_DoorColumns = GetColumns(T_Door);
            foreach (var OID in GetOIDs($"mapid = '{mapid}'", P_Door_Point))
            {
                IFeature pt = T_POI.CreateFeature();

                IFeature old = P_Door_Point.GetFeature(OID);
                pt.Shape = old.Shape;

                int floorid = Convert.ToInt16(old.Value[DoorColumns["floorid"]]);
                string featureid = Convert.ToString(old.Value[DoorColumns["featureid"]]);
                int shoppolygonid = Convert.ToInt32(old.Value[DoorColumns["shoppolygonid"]]);

                string uniqueid = BLD_ID + FL_ID[floorid] + "052" +
                                  featureid.Substring(featureid.Length - 4, 4).PadLeft(4, '0');

                string poi_code = string.Empty;
                string namecn = string.Empty;
                string nameen = string.Empty;
                string boothid = string.Empty;
                string celltype = string.Empty;

                if (ShopPolygon[shoppolygonid].Categoryid == null)
                {
                    var shopinfo = Shops[(int)ShopPolygon[shoppolygonid].ShopID];
                    //poi_code = DIC_Category[shopinfo.Categoryid].TencentID;
                    if (shopinfo.Categoryid != 24000)
                    {
                        namecn = shopinfo.ShopNameCN == string.Empty ? shopinfo.ShopNameCN : shopinfo.ShopNameCN + "出入口";
                        nameen = shopinfo.ShopNameEN == string.Empty ? shopinfo.ShopNameEN : shopinfo.ShopNameEN + "出入口";
                    }
                    else
                    {
                        namecn = DIC_Category[shopinfo.Categoryid].TypeName + "出入口";
                    }
                    //boothid = shopinfo.Booth;
                 
                    //celltype = DIC_Category[shopinfo.Categoryid].Cell_Type;
                }
                else
                {
                    namecn = "空间单元主门";
                }
                poi_code = "00101003";
                celltype = "030000";

                //string name = string.Empty;
                //if (namecn.Equals(string.Empty))
                //{
                //    name = nameen;
                //}
                //else
                //{
                //    name = namecn;
                //}
                //poi_code = 

                var doorshop = ShopPolygon[shoppolygonid];

                pt.Value[PoiColumns["parent_id"]] = BLD_ID + FL_ID[doorshop.FloorID] + "051" +
                                                    doorshop.FeatureID.Substring(doorshop.FeatureID.Length - 4, 4)
                                                        .PadLeft(4, '0');

                if (ShopPolygon[shoppolygonid].Categoryid != null)
                {
                    ISpatialFilter pSF = new SpatialFilterClass();
                    pSF.Geometry = P_Shop_Polygon.GetFeature(doorshop.ObjectID).Shape;
                    pSF.WhereClause = $"floorid={doorshop.FloorID} and mapid='{mapid}'";
                    pSF.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains;
                    IFeatureCursor exCursor = P_PublicService_Point.Search(pSF, true);
                    IFeatureCursor exCursor2 = P_Escalator_Point.Search(pSF, true);
                    IFeature ex = exCursor.NextFeature();
                    IFeature ex2 = exCursor2.NextFeature();
                    if (ex != null)
                    {
                        string pub_featureid = ex.Value[ex.Fields.FindField("featureid")].ToString();
                        pt.Value[PoiColumns["parent_id"]] = BLD_ID + FL_ID[doorshop.FloorID] + "053" +
                                                            pub_featureid.Substring(pub_featureid.Length - 4, 4)
                                                                .PadLeft(4, '0');
                    }
                    else if (ex2 != null)
                    {
                        string esc_featureid = ex2.Value[ex2.Fields.FindField("featureid")].ToString();
                        pt.Value[PoiColumns["parent_id"]] = BLD_ID + FL_ID[doorshop.FloorID] + "054" +
                                                            esc_featureid.Substring(esc_featureid.Length - 4, 4)
                                                                .PadLeft(4, '0');
                    }
                    Marshal.FinalReleaseComObject(exCursor);
                    Marshal.FinalReleaseComObject(exCursor2);
                }

                pt.Value[PoiColumns["poi_id"]] = uniqueid;
                //pt.Value[PoiColumns["uni_poi_id"]]
                pt.Value[PoiColumns["bld_id"]] = BLD_ID;
                pt.Value[PoiColumns["fl_id"]] = BLD_ID + FL_ID[floorid];

                pt.Value[PoiColumns["cell_type"]] = celltype;
                //pt.Value[PoiColumns["height"]]

                pt.Value[PoiColumns["booth_id"]] = boothid;
                pt.Value[PoiColumns["poi_code"]] = poi_code;

                pt.Value[PoiColumns["name"]] = namecn == string.Empty ? nameen : namecn;
                pt.Value[PoiColumns["name_cn"]] = namecn;
                pt.Value[PoiColumns["name_en"]] = nameen;

                //pt.Value[PoiColumns["alias"]]
                pt.Value[PoiColumns["tel"]] = Mall.Phone;
                pt.Value[PoiColumns["office_hours"]] = Mall.OpeningTime;
                //pt.Value[PoiColumns["photos"]]
                pt.Value[PoiColumns["collect_date"]] = Collect_Date.RenewDate;
                pt.Value[PoiColumns["update_date"]] = Update_Date.RenewDate;
                //pt.Value[PoiColumns["lg_id"]]
                //pt.Value[PoiColumns["area_id"]]
                pt.Value[PoiColumns["node_id"]] = GetNode_ID(pt.Shape, BLD_ID + FL_ID[floorid], null);

                pt.Value[PoiColumns["collect_date"]] = Collect_Date.RenewDate;
                pt.Value[PoiColumns["update_date"]] = Update_Date.RenewDate;

                //pt.Value[PoiColumns["node_id"]]=
                pt.Store();
            }
            Program.logger.Debug($"{mapid}导出Door完成...");
            //Facility
            var PublicServiceColumns = GetColumns(P_PublicService_Point as ITable);
            var T_FacilityColumns = GetColumns(T_Facility);
            foreach (var OID in GetOIDs($"mapid = '{mapid}'", P_PublicService_Point))
            {
                IFeature pt = T_POI.CreateFeature();
                IFeature old = P_PublicService_Point.GetFeature(OID);
                pt.Shape = old.Shape;

                int floorid = Convert.ToInt16(old.Value[PublicServiceColumns["floorid"]]);
                string featureid = Convert.ToString(old.Value[PublicServiceColumns["featureid"]]);
                int type = Convert.ToInt32(old.Value[PublicServiceColumns["type"]]);
                string pub_name = Convert.ToString(old.Value[PublicServiceColumns["name"]]);

                string uniqueid = BLD_ID + FL_ID[floorid] + "053" +
                                  featureid.Substring(featureid.Length - 4, 4).PadLeft(4, '0');

                pt.Value[PoiColumns["poi_id"]] = uniqueid;
                pt.Value[PoiColumns["bld_id"]] = BLD_ID;
                pt.Value[PoiColumns["fl_id"]] = BLD_ID + FL_ID[floorid];


                pt.Value[PoiColumns["poi_code"]] = DIC_Category[DIC_PublicServiceType[type].CategoryID].TencentID;
                pt.Value[PoiColumns["cell_type"]] = DIC_Category[DIC_PublicServiceType[type].CategoryID].Cell_Type;
                pt.Value[PoiColumns["name"]] = DIC_PublicServiceType[type].Display;

                int[] doorInts = {10, 16, 19, 41, 52, 66, 67, 68, 105, 124, 126};
                if (doorInts.Contains(type))
                {
                    if (pub_name.Trim() != "")
                    {
                        pt.Value[PoiColumns["name"]] = pub_name;
                    }
                }
                //pt.Value[LinkColumns["height"]]
                pt.Value[PoiColumns["node_id"]] = GetNode_ID(pt.Shape, BLD_ID + FL_ID[floorid], null);
                pt.Value[PoiColumns["area_id"]] = GetCell_ID(pt.Shape, BLD_ID + FL_ID[floorid]);
                if (string.IsNullOrEmpty(pt.Value[PoiColumns["area_id"]].ToString()))
                {
                    pt.Value[PoiColumns["cell_type"]] = "000000";
                }

                pt.Value[PoiColumns["collect_date"]] = Collect_Date.RenewDate;
                pt.Value[PoiColumns["update_date"]] = Update_Date.RenewDate;

                pt.Store();
            }
            Program.logger.Debug($"{mapid}导出Facility完成...");
            //Escalator
            var EscalatorColumns = GetColumns(P_Escalator_Point as ITable);
            var T_ElevatorColumns = GetColumns(T_Elevator);
            foreach (var OID in GetOIDs($"mapid = '{mapid}'", P_Escalator_Point))
            {
                IFeature pt = T_POI.CreateFeature();
                IFeature old = P_Escalator_Point.GetFeature(OID);
                pt.Shape = old.Shape;

                int floorid = Convert.ToInt16(old.Value[EscalatorColumns["floorid"]]);
                int con_id = Convert.ToInt16(old.Value[EscalatorColumns["connectionid"]]);
                string featureid = Convert.ToString(old.Value[EscalatorColumns["featureid"]]);
                int type = Escalator_Connection[Convert.ToInt32(old.Value[EscalatorColumns["objid"]])].Type;

                string uniqueid = BLD_ID + FL_ID[floorid] + "054" +
                                  featureid.Substring(featureid.Length - 4, 4).PadLeft(4, '0');

                pt.Value[PoiColumns["poi_id"]] = uniqueid;
                pt.Value[PoiColumns["bld_id"]] = BLD_ID;
                pt.Value[PoiColumns["fl_id"]] = BLD_ID + FL_ID[floorid];

                pt.Value[PoiColumns["poi_code"]] = DIC_Category[DIC_EscalatorType[type].CategoryID].TencentID;
                pt.Value[PoiColumns["cell_type"]] = DIC_Category[DIC_EscalatorType[type].CategoryID].Cell_Type;
                pt.Value[PoiColumns["name"]] = DIC_EscalatorType[type].Display;
                string node_id = GetNode_ID(pt.Shape, BLD_ID + FL_ID[floorid], null);
                pt.Value[PoiColumns["node_id"]] = node_id;

                pt.Value[PoiColumns["collect_date"]] = Collect_Date.RenewDate;
                pt.Value[PoiColumns["update_date"]] = Update_Date.RenewDate;

                pt.Value[PoiColumns["area_id"]] = GetCell_ID(pt.Shape, BLD_ID + FL_ID[floorid]);
                if (string.IsNullOrEmpty(pt.Value[PoiColumns["area_id"]].ToString()))
                {
                    pt.Value[PoiColumns["cell_type"]] = "000000";
                }
                //string connect_node = string.Empty;

                //foreach (var con in Connections.Where(_ => _.ID == con_id && _.To != 0))
                //{
                //    connect_node +=
                //        BLD_ID + FL_ID[Escalator_Point[con.To].FloorID] + "03" +
                //        Escalator_Point[con.To].FeatureID.Substring(Escalator_Point[con.To].FeatureID.Length - 4, 4)
                //            .PadLeft(5, '0') + "|";
                //}
                //foreach (var con in Connections.Where(_ => _.To == con_id && _.Oneway == 0))
                //{
                //    connect_node +=
                //        BLD_ID + FL_ID[Escalator_Point[con.ID].FloorID] + "03" +
                //        Escalator_Point[con.ID].FeatureID.Substring(Escalator_Point[con.ID].FeatureID.Length - 4, 4)
                //            .PadLeft(5, '0') + "|";
                //}
                //connect_node = connect_node.TrimEnd('|');

                //pt.Value[LinkColumns["poi_code"]]=
                //pt.Value[LinkColumns["cell_type"]]
                //pt.Value[LinkColumns["height"]]

                pt.Store();

                
            }
            Program.logger.Debug($"{mapid}导出Escalator完成...");
            var T_shopColumns = GetColumns(T_Shop);
            List<string> fl_lst = new List<string>();
            foreach (var OID in GetOIDs($"mapid = '{mapid}'", P_Shop_Polygon))
            {
                IFeature pt = T_POI.CreateFeature();
                IFeature old = P_Shop_Polygon.GetFeature(OID);
                pt.Shape = getLabelPoint(old.Shape);

                int floorid = Convert.ToInt16(old.Value[Shop_polygonColumns["floorid"]]);
                string featureid = Convert.ToString(old.Value[Shop_polygonColumns["featureid"]]);

                string uniqueid = BLD_ID + FL_ID[floorid] + "051" +
                                  featureid.Substring(featureid.Length - 4, 4).PadLeft(4, '0');

                pt.Value[PoiColumns["poi_id"]] = uniqueid;
                pt.Value[PoiColumns["bld_id"]] = BLD_ID;
                pt.Value[PoiColumns["fl_id"]] = BLD_ID + FL_ID[floorid];


                int objid = Convert.ToInt32(old.Value[Shop_polygonColumns["objid"]]);
                //int categoryid= Convert.ToInt32(old.Value[Shop_polygonColumns["shopid"]]);

                string poi_code = string.Empty;
                string namecn = string.Empty;
                string nameen = string.Empty;
                string boothid = string.Empty;
                string celltype = string.Empty;
                string alias = string.Empty;

                if (ShopPolygon[objid].Categoryid == null)
                {
                    var shop = Shops[(int) ShopPolygon[objid].ShopID];
                    poi_code = DIC_Category[shop.Categoryid].TencentID;
                    alias = shop.Alias;
                    if (shop.Categoryid != 24000)
                    {
                        namecn = shop.ShopNameCN;
                        nameen = shop.ShopNameEN;
                    }
                    else
                    {
                        namecn = DIC_Category[shop.Categoryid].TypeName;
                    }
                    boothid = shop.RoomNum;
                    celltype = DIC_Category[shop.Categoryid].Cell_Type;
                    if (shop.Specialname==1)
                    {
                        fl_lst.Add(uniqueid);
                    }
                }
                else
                {
                    int categoryid = (int) ShopPolygon[objid].Categoryid;
                    poi_code = DIC_Category[categoryid].TencentID;
                    namecn = DIC_Category[categoryid].TypeName;
                    //namecn = dic_shop[shoppolygon_shops[shoppolygonid]]["分类名称"].ToString();
                    //boothid = ShopPolygon[objid].Booth;
                    celltype = DIC_Category[categoryid].Cell_Type;
                }

                pt.Value[PoiColumns["poi_code"]] = poi_code;
                pt.Value[PoiColumns["cell_type"]] = celltype;
                pt.Value[PoiColumns["name"]] = namecn == string.Empty ? nameen : namecn;
                pt.Value[PoiColumns["name_cn"]] = namecn;
                pt.Value[PoiColumns["name_en"]] = nameen;
                pt.Value[PoiColumns["alias"]] = alias;
                pt.Value[PoiColumns["booth_id"]] = boothid;

                //pt.Value[PoiColumns["alias"]]
                pt.Value[PoiColumns["tel"]] = Mall.Phone;
                pt.Value[PoiColumns["office_hours"]] = Mall.OpeningTime;
                //pt.Value[PoiColumns["photos"]]
                pt.Value[PoiColumns["collect_date"]] = Collect_Date.RenewDate;
                pt.Value[PoiColumns["update_date"]] = Update_Date.RenewDate;
                //pt.Value[PoiColumns["lg_id"]]
                pt.Value[PoiColumns["area_id"]] = Shop_Polygon_Unique[featureid];

                pt.Store();

                bool del = false;
                if (ShopPolygon[objid].ShopID == null)
                {
                    ISpatialFilter pSF = new SpatialFilterClass();
                    pSF.Geometry = old.Shape;
                    pSF.WhereClause = $"floorid={floorid} and mapid = '{mapid}' ";
                    pSF.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains;
                    
                    IFeatureCursor exCursor = P_PublicService_Point.Search(pSF, true);
                    IFeature ex = exCursor.NextFeature();
                    IFeatureCursor exCursor2 = P_Escalator_Point.Search(pSF, true);
                    IFeature ex2 = exCursor2.NextFeature();

                    if (ex != null || ex2 != null)
                    {
                        pt.Delete();
                        del = true;
                    }
                    Marshal.FinalReleaseComObject(exCursor);
                    Marshal.FinalReleaseComObject(exCursor2);

                    if (!del)
                    {
                        if (poi_code.StartsWith("00102") || poi_code.StartsWith("00103"))
                        {
                            ISpatialFilter pSF2 = new SpatialFilterClass();
                            pSF2.Geometry = old.Shape;
                            pSF2.WhereClause = $"FL_ID='{BLD_ID + FL_ID[floorid]}' AND (Poi_code like '00102%' OR Poi_code like '00103%')";
                            pSF2.SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;
                            IFeatureCursor poiCursor = T_POI.Search(pSF2, true);
                            IFeature f;
                            while ((f = poiCursor.NextFeature()) != null)
                            {
                                f.Value[PoiColumns["parent_id"]] = uniqueid;
                                f.Value[PoiColumns["cell_type"]] = "030000";
                                f.Value[PoiColumns["poi_code"]] = "00101003";
                                f.Value[PoiColumns["name"]] = "空间单元主门";
                                f.Value[PoiColumns["area_id"]] = null;
                                f.Store();
                            }
                            Marshal.FinalReleaseComObject(poiCursor);
                            
                        }
                    }
                }
                else
                {
                    #region POI路网

                    //商铺 路网结点
                    //IFeature node = T_Node.CreateFeature();
                    //node.Shape = pt.Shape;
                    //string uniqueidnode = BLD_ID + FL_ID[floorid] + "031" +
                    //                      featureid.Substring(featureid.Length - 4, 4).PadLeft(4, '0');

                    //node.Value[NodeColumns["fl_id"]] = BLD_ID + FL_ID[floorid];
                    //node.Value[NodeColumns["node_id"]] = uniqueidnode;
                    //node.Value[NodeColumns["bld_id"]] = BLD_ID;
                    //node.Value[NodeColumns["type"]] = 1;
                    //node.Store();

                    //pt.Value[PoiColumns["node_id"]] = uniqueidnode;


                    //var laneline = GetOIDs($"shoppolygonid={objid} and mapid='{mapid}'", P_Door_Point);
                    //if (laneline.Count > 0)
                    //{
                    //    IFeature link = T_Link.CreateFeature();
                    //    IFeature startlink = P_Door_Point.GetFeature(laneline[0]);

                    //    //门 路网结点
                    //    string uniqueidnode2 = GetNode_ID(startlink.Shape, BLD_ID + FL_ID[floorid]);

                    //IFeature node2 = T_Node.CreateFeature();
                    //node2.Shape = startlink.Shape;
                    //string uniqueidnode2 = BLD_ID + FL_ID[floorid] + "032" +
                    //                      featureid.Substring(featureid.Length - 4, 4).PadLeft(4, '0');

                    //node.Value[NodeColumns["fl_id"]] = BLD_ID + FL_ID[floorid];
                    //node.Value[NodeColumns["node_id"]] = uniqueidnode2;
                    //node.Value[NodeColumns["bld_id"]] = BLD_ID;
                    //node.Value[NodeColumns["type"]] = 1;
                    //node.Store();

                    //    IPolyline pl = new PolylineClass();
                    //    IPointCollection pc = pl as IPointCollection;
                    //    pc.AddPoint(startlink.Shape as IPoint);
                    //    pc.AddPoint(node.Shape as IPoint);

                    //    link.Shape = pl;

                    //    link.Value[LinkColumns["bld_id"]] = BLD_ID;
                    //    link.Value[LinkColumns["fl_id"]] = BLD_ID + FL_ID[floorid];
                    //    link.Value[LinkColumns["s_node_id"]] = uniqueidnode;
                    //    link.Value[LinkColumns["end_node_id"]] = uniqueidnode2;
                    //    link.Value[LinkColumns["link_id"]] = uniqueid;
                    //    link.Value[LinkColumns["length"]] = getLength(pl);
                    //    //link.Value[LinkColumns["type"]] = old.Value[LaneLineColumns["detail"]];
                    //    //int direction = Convert.ToInt16(old.Value[LaneLineColumns["detail"]]);
                    //    //link.Value[LinkColumns["direction"]] =
                    //    // Convert.ToInt16(old.Value[LaneLineColumns["direction"]]) + 1;
                    //    link.Value[LinkColumns["is_slope"]] = 0;
                    //    link.Value[LinkColumns["is_stairs"]] = 0;
                    //    link.Value[LinkColumns["is_barr_free"]] = 1;
                    //    link.Store();
                    //}

                    #endregion




                    //pt.Store();
                    //if (poi_code.StartsWith("003"))
                    //{
                    //    exportCursor = T_Shop.Insert(true);
                    //    IRowBuffer r = T_Shop.CreateRowBuffer();
                    //    r.Value[T_shopColumns["shop_id"]] = uniqueid;
                    //    r.Value[T_shopColumns["brand_id"]] = 0;
                    //    r.Value[T_shopColumns["brand_name"]] = namecn;
                    //    r.Value[T_shopColumns["brand_en"]] = nameen;
                    //    r.Value[T_shopColumns["collect_date"]] = Collect_Date.RenewDate;
                    //    r.Value[T_shopColumns["update_date"]] = Collect_Date.RenewDate;
                    //    r.Value[T_shopColumns["name_fl"]] = Shops[(int) ShopPolygon[objid].ShopID].Specialname;
                    //    exportCursor.InsertRow(r);
                    //    exportCursor.Flush();
                    //    Marshal.FinalReleaseComObject(exportCursor);
                    //}
                }

                if (!del)
                {
                    IQueryFilter pSF = new QueryFilterClass();
                    pSF.WhereClause =
                        $"poi_id <> '{uniqueid}' AND area_id = '{Shop_Polygon_Unique[featureid]}'";
                    IFeatureCursor poiCursor = T_POI.Search(pSF, true);
                    IFeature f;
                    while ((f = poiCursor.NextFeature()) != null)
                    {
                        f.Value[PoiColumns["area_id"]] = null;
                        f.Store();
                    }
                    Marshal.FinalReleaseComObject(poiCursor);
                }

                #region
                //if (poi_code.StartsWith("005"))
                //{
                //    exportCursor = T_Park.Insert(true);
                //    IRowBuffer row = T_Park.CreateRowBuffer();
                //    row.Value[row.Fields.FindField("parking_id")] = uniqueid;
                //    //row.Value[T_DoorColumns["pass_restriction"]] = 1;
                //    //row.Value[T_DoorColumns["user_restriction"]] = 1;
                //    //row.Value[T_DoorColumns["barr_free"]] = 0;
                //    //row.Value[T_DoorColumns["is_emer"]] = 1;
                //    //row.Value[row.Fields.FindField("step_count")] = -1;
                //    row.Value[row.Fields.FindField("collect_date")] = Collect_Date.RenewDate;
                //    row.Value[row.Fields.FindField("update_date")] = Update_Date.RenewDate;
                //    //pt.Value[LinkColumns["poi_code"]]=
                //    //pt.Value[LinkColumns["cell_type"]]
                //    //pt.Value[LinkColumns["height"]]
                //    //pt.Value[LinkColumns["both_id"]]
                //    exportCursor.InsertRow(row);
                //    exportCursor.Flush();
                //    Marshal.FinalReleaseComObject(exportCursor);

                //}
                #endregion
            }
            Program.logger.Debug($"{mapid}导出POI完成...");

            Program.logger.Debug($"{mapid}开始导出Connections...");
            foreach (var caculator in Escalator_Point.Values)
            {
                IGeometry pt = P_Escalator_Point.GetFeature(caculator.OID).Shape;

                string connect_node = string.Empty;
                List<int> Elev_ids=new List<int>(); 
                foreach (var con in Connections.Where(_ => _.From == caculator.ConnectionID && _.To != 0))
                {
                    //connect_node +=
                    //    BLD_ID + FL_ID[Escalator_Point[con.To].FloorID] + "03" +
                    //    Escalator_Point[con.To].FeatureID.Substring(Escalator_Point[con.To].FeatureID.Length - 4, 4)
                    //        .PadLeft(5, '0') + "|";
                    Elev_ids.Add(Escalator_Point[con.To].OID);
                }
                foreach (var con in Connections.Where(_ => _.To == caculator.ConnectionID && _.Oneway == 0))
                {
                    //connect_node +=
                    //    BLD_ID + FL_ID[Escalator_Point[con.From].FloorID] + "03" +
                    //    Escalator_Point[con.From].FeatureID.Substring(Escalator_Point[con.From].FeatureID.Length - 4, 4)
                    //        .PadLeft(5, '0') + "|";
                    Elev_ids.Add(Escalator_Point[con.From].OID);
                }
                foreach (var item in Elev_ids)
                {
                    IFeature pt2 = P_Escalator_Point.GetFeature(item);
                    int floorid = Convert.ToInt32(pt2.Value[pt2.Fields.FindField("floorid")]);
                    connect_node += GetNode_ID(pt2.Shape, BLD_ID + FL_ID[floorid], null) + "|";
                }
                connect_node = connect_node.TrimEnd('|');

                GetNode_ID(pt, BLD_ID + FL_ID[caculator.FloorID], connect_node);
            }
            Program.logger.Debug($"{mapid}导出Connections完成...");

            //更新Logic_ID
            foreach (var item in Floors.Values)
            {
                if (item.Seq != 0)
                {
                    SpatialJoin(T_FL_Region, $"fl_id = '{BLD_ID + item.FL_ID}'", T_Logic_Region,
                        $"fl_id = '{BLD_ID + item.FL_ID}'", "region_id", "region_id");
                    SpatialJoin(T_FL_Region, $"fl_id = '{BLD_ID + item.FL_ID}'", T_POI,
                        $"fl_id = '{BLD_ID + item.FL_ID}' and cell_type = '030000'", "region_id", "region_id");

                    SpatialJoin(T_POI,
                        $"fl_id = '{BLD_ID + item.FL_ID}' AND poi_code <> '00101003' AND poi_code LIKE '00101%'",
                        T_Node,
                        $"fl_id = '{BLD_ID + item.FL_ID}' AND type = '1'", "3", "type");

                    DelOutdoorLine(BLD_ID + item.FL_ID, item.FloorID);
                    DelOutdoorPoint(BLD_ID + item.FL_ID, item.FloorID);
                }
            }
            Program.logger.Debug($"{mapid}更新Logic_ID完成...");

            foreach (var OID in GetOIDs(T_POI))
            {
                IFeature poi = T_POI.GetFeature(OID);

                string poi_id = poi.Value[PoiColumns["poi_id"]].ToString();
                string poi_code = poi.Value[PoiColumns["poi_code"]].ToString();
                string name= poi.Value[PoiColumns["name"]].ToString();

                IRowBuffer row;
                switch (poi_code.Substring(0, 3))
                {
                    case "001":
                        switch (poi_code.Substring(0, 5))
                        {
                            case "00101":
                                exportCursor = T_Door.Insert(true);
                                row = T_Door.CreateRowBuffer();
                                row.Value[T_DoorColumns["door_id"]] = poi_id;
                                row.Value[T_DoorColumns["pass_restriction"]] = 1;
                                row.Value[T_DoorColumns["user_restriction"]] = 1;
                                //row.Value[T_DoorColumns["barr_free"]] = 0;
                                row.Value[T_DoorColumns["is_emer"]] = 1;
                                //row.Value[T_DoorColumns["step_count"]] = -1;
                                row.Value[T_DoorColumns["collect_date"]] = Collect_Date.RenewDate;
                                row.Value[T_DoorColumns["update_date"]] = Update_Date.RenewDate;
                                exportCursor.InsertRow(row);
                                exportCursor.Flush();
                                Marshal.FinalReleaseComObject(exportCursor);
                                break;
                            case "00102":
                            case "00103":
                                exportCursor = T_Elevator.Insert(true);
                                row = T_Elevator.CreateRowBuffer();
                                row.Value[T_ElevatorColumns["elev_id"]] = poi_id;
                                row.Value[T_ElevatorColumns["pass_restriction"]] = 1;
                                //row.Value[T_DoorColumns["user_restriction"]] = uniqueid;
                                row.Value[T_ElevatorColumns["barr_free"]] = 0;
                                row.Value[T_ElevatorColumns["is_emer"]] = 0;
                                row.Value[T_ElevatorColumns["step_count"]] = -1;
                                row.Value[T_ElevatorColumns["collect_date"]] = Collect_Date.RenewDate;
                                row.Value[T_ElevatorColumns["update_date"]] = Update_Date.RenewDate;
                                exportCursor.InsertRow(row);
                                exportCursor.Flush();
                                Marshal.FinalReleaseComObject(exportCursor);
                                break;
                        }
                        break;
                    case "002":
                        exportCursor = T_Facility.Insert(true);
                        row = T_Facility.CreateRowBuffer();
                        row.Value[T_FacilityColumns["facil_id"]] = poi_id;
                        //row.Value[T_FacilityColumns["pass_restriction"]] = 1;
                        //row.Value[T_DoorColumns["user_restriction"]] = uniqueid;
                        row.Value[T_FacilityColumns["operator"]] = name;
                        row.Value[T_FacilityColumns["for_disabled"]] = 0;
                        row.Value[T_FacilityColumns["collect_date"]] = Collect_Date.RenewDate;
                        row.Value[T_FacilityColumns["update_date"]] = Update_Date.RenewDate;
                        exportCursor.InsertRow(row);
                        exportCursor.Flush();
                        Marshal.FinalReleaseComObject(exportCursor);
                        break;
                    case "003":
                        exportCursor = T_Shop.Insert(true);
                        row = T_Shop.CreateRowBuffer();
                        row.Value[T_shopColumns["shop_id"]] = poi_id;
                        row.Value[T_shopColumns["brand_id"]] = 0;
                        row.Value[T_shopColumns["brand_name"]] = name;
                        row.Value[T_shopColumns["brand_en"]] = poi.Value[PoiColumns["name_en"]].ToString(); ;
                        row.Value[T_shopColumns["collect_date"]] = Collect_Date.RenewDate;
                        row.Value[T_shopColumns["update_date"]] = Collect_Date.RenewDate;
                        row.Value[T_shopColumns["name_fl"]] = fl_lst.Contains(poi_id) ? 1 : 0;
                        exportCursor.InsertRow(row);
                        exportCursor.Flush();
                        Marshal.FinalReleaseComObject(exportCursor);
                        break;
                    case "005":
                        exportCursor = T_Park.Insert(true);
                        row = T_Park.CreateRowBuffer();
                        row.Value[row.Fields.FindField("parking_id")] = poi_id;
                        //row.Value[T_DoorColumns["pass_restriction"]] = 1;
                        //row.Value[T_DoorColumns["user_restriction"]] = 1;
                        //row.Value[T_DoorColumns["barr_free"]] = 0;
                        //row.Value[T_DoorColumns["is_emer"]] = 1;
                        //row.Value[row.Fields.FindField("step_count")] = -1;
                        row.Value[row.Fields.FindField("collect_date")] = Collect_Date.RenewDate;
                        row.Value[row.Fields.FindField("update_date")] = Update_Date.RenewDate;
                        //pt.Value[LinkColumns["poi_code"]]=
                        //pt.Value[LinkColumns["cell_type"]]
                        //pt.Value[LinkColumns["height"]]
                        //pt.Value[LinkColumns["both_id"]]
                        exportCursor.InsertRow(row);
                        exportCursor.Flush();
                        Marshal.FinalReleaseComObject(exportCursor);
                        break;
                }
            }
            Program.logger.Debug($"{mapid}导出属性表完成...");
            #region MyRegion

            //IFeatureCursor lgCursor = T_Logic_Region.Search(null, true);
            //IFeature lgFeature;
            //while ((lgFeature = lgCursor.NextFeature()) != null)
            //{
            //    string lg_id = lgFeature.Value[Logic_RegionColumns["lg_id"]].ToString();
            //    string fl_id = lgFeature.Value[Logic_RegionColumns["fl_id"]].ToString();

            //    ISpatialFilter sf = new SpatialFilterClass();
            //    sf.Geometry = lgFeature.Shape;
            //    sf.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
            //    sf.WhereClause = $"fl_id='{fl_id}'";
            //    IFeatureCursor poiCursor = T_POI.Search(sf, true);
            //    IFeature poiFeature;
            //    while ((poiFeature = poiCursor.NextFeature()) != null)
            //    {
            //        string poi_lg_id = poiFeature.Value[PoiColumns["lg_id"]].ToString();
            //        poiFeature.Value[PoiColumns["lg_id"]] = poi_lg_id += ("|" + lg_id).TrimStart('|');
            //        poiFeature.Store();
            //    }
            //    Marshal.FinalReleaseComObject(poiCursor);
            //}
            //Marshal.FinalReleaseComObject(lgCursor);

            #endregion

        }

        private Dictionary<string, int> GetColumns(ITable row)
        {
            Dictionary<string, int> Columns = new Dictionary<string, int>();
            for (int i = 0; i < row.Fields.FieldCount; i++)
            {
                IField field = row.Fields.Field[i];
                Columns.Add(field.Name.ToLower(), i);
            }
            if (Columns.ContainsKey("objectid"))
            {
                Columns.Remove("objectid");
            }
            if (Columns.ContainsKey("shape_length"))
            {
                Columns.Remove("shape_length");
            }
            if (Columns.ContainsKey("shape_area"))
            {
                Columns.Remove("shape_area");
            }
            return Columns;
        }

        private IDictionary InsertColumns(ITable row)
        {
            Dictionary<string, int> Columns = new Dictionary<string, int>();
            for (int i = 0; i < row.Fields.FieldCount; i++)
            {
                IField field = row.Fields.Field[i];
                Columns.Add(field.Name.ToLower(), i);
            }
            if (Columns.ContainsKey("objectid"))
            {
                Columns.Remove("objectid");
            }
            return Columns.ToDictionary(_ => _.Key, _ => new
            {
                FieldName = _.Key,
                FieldIdx = _.Value,
                FieldObject = (object)null
            });
        }

        private IGeometry ConvertWKBToGeometry(byte[] wkb)
        {
            IGeometry geom;
            int countin = wkb.GetLength(0);
            IGeometryFactory3 factory = new GeometryEnvironment() as IGeometryFactory3;
            factory.CreateGeometryFromWkbVariant(wkb, out geom, out countin);
            return geom;
        }

        private List<int> GetOIDs(string WhereClause, IFeatureClass pFeatureClass)
        {
            List<int> lst=new List<int>();

            IQueryFilter qf = new QueryFilterClass();
            qf.WhereClause = WhereClause;

            IFeatureCursor cursor = pFeatureClass.Search(qf, true);
            IFeature f;
            while ((f = cursor.NextFeature()) != null)
            {
                lst.Add(f.OID);
            }
            Marshal.FinalReleaseComObject(cursor);

            return lst;
        }

        private List<int> GetOIDs(IFeatureClass pFeatureClass)
        {
            return GetOIDs("1=1", pFeatureClass);
        }

        private void DeleteTable(IFeatureClass pFeatureClass)
        {
            ITable pTable=pFeatureClass as ITable;
            pTable.DeleteSearchedRows(null);
        }

        private void DeleteTable(ITable pTable)
        {
            pTable.DeleteSearchedRows(null);
        }

        private IPoint getLabelPoint(IGeometry geo)
        {
            IArea a = geo as IArea;
            return a.LabelPoint;
        }

        private IPoint getLatLon(IGeometry geo)
        {
            ISpatialReferenceFactory6 spatialReferenceFactory6 = new SpatialReferenceEnvironmentClass();
            ISpatialReference pSR =
                spatialReferenceFactory6.CreateGeographicCoordinateSystem((int) esriSRGeoCSType.esriSRGeoCS_WGS1984);
            IGeometry p = getLabelPoint(geo);
            p.Project(pSR);
            return (IPoint) p;
        }
        private double getLength(IGeometry geo)
        {
            IPolyline l = geo as IPolyline;
            return l.Length;
        }
        private void Export_T_Cell()
        {
            
        }

        private void Export_T_Building()
        {
            
        }

        enum MapSource
        {
            采集设备 = 0,
            CAD图 = 1,
            水牌图 = 2,
            消防图 = 3
        }

        private string GetNode_ID(IGeometry pt, string FL_ID,string connect_node)
        {
            ISpatialFilter sf = new SpatialFilterClass();
            sf.Geometry = pt;
            sf.WhereClause = $"fl_id='{FL_ID}'";
            sf.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
            IFeatureCursor fcCursor = T_Node.Search(sf, true);
            IFeature f = fcCursor.NextFeature();
            if (connect_node != null)
            {
                if (f != null)
                {
                    if (connect_node == String.Empty)
                    {
                        f.Value[f.Fields.FindField("type")] = 1;
                    }
                    else
                    {
                        f.Value[f.Fields.FindField("type")] = 2;
                    }
                    f.Value[f.Fields.FindField("connect_node")] = connect_node;
                    f.Store();
                }
                else
                {
                    
                }
            }
            if (f != null)
            {
                return f.Value[f.Fields.FindField("node_id")].ToString();
            }
            else
            {
                return null;
            }
        }

        private string GetCell_ID(IGeometry pt, string FL_ID)
        {
            ISpatialFilter sf = new SpatialFilterClass();
            sf.Geometry = pt;
            sf.WhereClause = $"unique_id like '{FL_ID}%'";
            sf.SpatialRel = esriSpatialRelEnum.esriSpatialRelWithin;
            IFeatureCursor fcCursor = T_Cell.Search(sf, true);
            IFeature f = fcCursor.NextFeature();

            if (f != null)
            {
                return f.Value[f.Fields.FindField("unique_id")].ToString();
            }
            else
            {
                return null;
            }
        }

        private string TransDataSrc(string Data_src)
        {
            switch (Data_src)
            {
                case "导购图":
                    return "02";
                case "消防图":
                    return "03";
                case "CAD":
                    return "01";
                default:
                    return "02";
            }
        }
        private string TransOfficeHours(string officeHours)
        {
            string[] split= officeHours.Split(' ');
            foreach (var item in split)
            {
                string[] day = item.Split('：');
                if (day[0] == "每天")
                {
                    
                }
                else
                {
                    
                }
               // string times = day[1].Split('-');
            }
            return null;

        }

        private IGeometry UnionGeo(List<string> OIDs, IFeatureClass pFeatureClass)
        {
            IQueryFilter qf = new QueryFilterClass();
            qf.WhereClause = $"objectid in ({string.Join(",", OIDs.ToArray()).Trim(',')})";
            object o = Type.Missing;
            IGeoDataset pGeoDataset = pFeatureClass as IGeoDataset;
            IGeometry geometryBag = new GeometryBagClass();
            geometryBag.SpatialReference = pGeoDataset.SpatialReference;

            IGeometryCollection pGeoColl = geometryBag as IGeometryCollection;

            IFeatureCursor pFeatureCursor = pFeatureClass.Search(qf, true);

            if (pFeatureCursor != null)
            {
                IFeature pFeature = null;
                while ((pFeature = pFeatureCursor.NextFeature()) != null)
                {
                    pGeoColl.AddGeometry(pFeature.ShapeCopy, ref o, ref o);
                    Marshal.FinalReleaseComObject(pFeature);
                    pFeature = null;
                }
                Marshal.FinalReleaseComObject(pFeatureCursor);
                pFeatureCursor = null;
            }
            ITopologicalOperator pTopo = null;
            switch (pFeatureClass.ShapeType)
            {
                case esriGeometryType.esriGeometryPolygon:
                    pTopo = new PolygonClass();
                    break;
                case esriGeometryType.esriGeometryPolyline:
                    pTopo = new PolylineClass();
                    break;
                case esriGeometryType.esriGeometryPoint:
                    pTopo = new MultipointClass();
                    break;
            }
            pTopo.ConstructUnion(geometryBag as IEnumGeometry);
           
            geometryBag = null;
            pGeoColl = null;
            o = null;
            pGeoDataset = null;

            return pTopo.ConvexHull();
        }

        private void SpatialJoin(IFeatureClass JoinFc, string JoinWC, IFeatureClass TargetFc, string TargetWC,
            string JoinField, string TargetField)
        {
            int joinIndex = JoinFc.FindField(JoinField);
            int targetIndex = TargetFc.FindField(TargetField);
            if (targetIndex != -1)
            {
                IQueryFilter pQueryFilter = new QueryFilterClass();
                pQueryFilter.WhereClause = JoinWC;
                ISpatialFilter pSpatialFilter = new SpatialFilterClass();
                pSpatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains;
                pSpatialFilter.WhereClause = TargetWC;
                IFeatureCursor joinCursor = JoinFc.Search(pQueryFilter, true);
                IFeature joinFeature;
                while ((joinFeature = joinCursor.NextFeature()) != null)
                {
                    pSpatialFilter.Geometry = joinFeature.ShapeCopy;
                    IFeatureCursor targetCursor = TargetFc.Update(pSpatialFilter, true);
                    IFeature targetFeature;
                    while ((targetFeature = targetCursor.NextFeature()) != null)
                    {
                        targetFeature.Value[targetIndex] = joinIndex == -1 ? JoinField : joinFeature.Value[joinIndex];
                        targetCursor.UpdateFeature(targetFeature);
                        Marshal.FinalReleaseComObject(targetFeature);
                    }
                    targetCursor.Flush();
                    Marshal.FinalReleaseComObject(joinFeature);
                    Marshal.FinalReleaseComObject(targetCursor);
                }
                Marshal.FinalReleaseComObject(joinCursor);
            }
        }

        /// <summary>
        /// 删除室外路网
        /// </summary>
        /// <param name="FLID"></param>
        /// <param name="FloorID"></param>
        private void DelOutdoorLine(string FLID,int FloorID)
        {
            IQueryFilter query=new QueryFilterClass();
            query.WhereClause = $"floorid = {FloorID} and mapid = '{MapID}'";
            //query.WhereClause = $" mapid = '{MapID}' or mapid= '0491'";

            IFeatureCursor frameCursor = P_Frame_Polygon.Search(query, true);
            int frameCount = P_Frame_Polygon.FeatureCount(query);
            List<int> delLink=new List<int>();
            List<int> delNode = new List<int>();

            IFeature frame;
            while ((frame = frameCursor.NextFeature()) != null)
            {

                IRelationalOperator relation = frame.Shape as IRelationalOperator;

                ISpatialFilter spatial = new SpatialFilterClass();
                spatial.Geometry = frame.Shape;
                spatial.SpatialRel = esriSpatialRelEnum.esriSpatialRelUndefined;
                spatial.WhereClause = $"FL_ID = '{FLID}'";
                IFeatureCursor linkCursor = T_Link.Update(spatial, true);
                IFeature link;
                while ((link = linkCursor.NextFeature()) != null)
                {
                    if (!relation.Contains(link.Shape))
                    {
                        delLink.Add(link.OID);
                        //linkCursor.DeleteFeature();
                    }
                }
                Marshal.FinalReleaseComObject(linkCursor);

                IFeatureCursor nodeCursor = T_Node.Update(spatial, true);
                IFeature node;
                while ((node = nodeCursor.NextFeature()) != null)
                {
                    if (relation.Disjoint(node.Shape))
                    {
                        delNode.Add(node.OID);
                        //nodeCursor.DeleteFeature();
                    }
                }
                Marshal.FinalReleaseComObject(nodeCursor);
            }
            Marshal.FinalReleaseComObject(frameCursor);

            var delLinkOID = delLink.GroupBy(_ => _, _ => _).ToDictionary(_ => _.First(), _ => _.ToList());

            var delNodeOID = delNode.GroupBy(_ => _, _ => _).ToDictionary(_ => _.First(), _ => _.ToList());
               // .Where(_ => _.Value.Count < frameCount);
            foreach (var oid in delLinkOID)
            {
                if (oid.Value.Count == frameCount)
                {
                    T_Link.GetFeature(oid.Key).Delete();
                }
            }
            foreach (var oid in delNodeOID)
            {
                if (oid.Value.Count == frameCount)
                {
                    T_Node.GetFeature(oid.Key).Delete();
                }
            }



            //ITable link=T_Link as ITable;
            //link.DeleteSearchedRows(spatial);

        }

        /// <summary>
        /// 删除孤立点
        /// </summary>
        /// <param name="FLID"></param>
        /// <param name="FloorID"></param>
        private void DelOutdoorPoint(string FLID, int FloorID)
        {
            IQueryFilter query = new QueryFilterClass();
            query.WhereClause = $"floorid = {FloorID} and mapid = '{MapID}'";
            //query.WhereClause = $" mapid = '{MapID}' or mapid= '0491'";

            IFeatureCursor frameCursor = P_Frame_Polygon.Search(query, true);

            IFeature frame;
            while ((frame = frameCursor.NextFeature()) != null)
            {
                ISpatialFilter spatial = new SpatialFilterClass();
                spatial.Geometry = frame.Shape;
                spatial.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                spatial.WhereClause = $"FL_ID = '{FLID}'";

                IFeatureCursor nodeCursor = T_Node.Update(spatial, true);
                IFeature node;
                while ((node = nodeCursor.NextFeature()) != null)
                {
                    ISpatialFilter pointFilter=new SpatialFilterClass();
                    pointFilter.Geometry = node.Shape;
                    pointFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

                    if (T_Link.FeatureCount(pointFilter) == 0)
                    {
                        nodeCursor.DeleteFeature();
                    }
                }
                Marshal.FinalReleaseComObject(nodeCursor);
            }
            Marshal.FinalReleaseComObject(frameCursor);
        }
    }
}
