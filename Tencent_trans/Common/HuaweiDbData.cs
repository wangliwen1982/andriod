using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ESRI.ArcGIS.Geometry;
using Newtonsoft.Json;
using Tencent_trans.PalmapEntity;

namespace Tencent_trans.Common
{
    public class HuaweiDbData
    {
        private readonly NpgsqlHelper _ngpDb;
        private static readonly TextInfo _info = Thread.CurrentThread.CurrentCulture.TextInfo;

        public HuaweiDbData(string connStr)
        {
            _ngpDb = new NpgsqlHelper(connStr);
        }

        public List<DicHuaweiCategory> GetHuaweiDicCategory()
        {
            var table = _ngpDb.ExecuteDataTable("select categoryname,category,height,poi from sde.dic_huawei_category")
                .AsEnumerable().Select(_ => new DicHuaweiCategory
                {
                    Category = _.Field<int>("category"),
                    Height = _.Field<int>("height"),
                    IsPoi = Convert.ToInt32(_["poi"]) != 0,
                    CategoryName = _["categoryname"]?.ToString()
                }).ToList();
            return table;
        }

        public List<DicHuaweiTexture> GetHuaweiTexture()
        {
            var table = _ngpDb.ExecuteDataTable("select textureid,texture from sde.dic_huawei_texture")
                .AsEnumerable().Select(_ => new DicHuaweiTexture
                {
                    Id = _.Field<int>("textureid"),
                    Texture = _["texture"]?.ToString()
                }).ToList();
            return table;
        }


        public static string YoudaoTranslation(string q)
        {
            //TextInfo Info = Thread.CurrentThread.CurrentCulture.TextInfo;
            try
            {
                if (string.IsNullOrEmpty(q)) return string.Empty;
                if (q.Equals("男洗手间")) return "Men's Room";
                string appKey = "667f1333bb1c315a";
                string from = "zh-CHS";
                string to = "en";
                string salt = DateTime.Now.Millisecond.ToString();
                string appSecret = "bzrShs8uAWiZR61TJktZ6xVrLrKO0FkJ";
                MD5 md5 = new MD5CryptoServiceProvider();
                string md5Str = appKey + q + salt + appSecret;
                byte[] output = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(md5Str));
                string sign = BitConverter.ToString(output).Replace("-", "");

                string url =
                    string.Format("http://openapi.youdao.com/api?appKey={0}&q={1}&from={2}&to={3}&sign={4}&salt={5}",
                        appKey, System.Web.HttpUtility.UrlDecode(q, System.Text.Encoding.GetEncoding("UTF-8")), from,
                        to,
                        sign, salt);
                WebRequest translationWebRequest = WebRequest.Create(url);

                WebResponse response = null;

                response = translationWebRequest.GetResponse();
                Stream stream = response.GetResponseStream();

                Encoding encode = Encoding.GetEncoding("utf-8");

                StreamReader reader = new StreamReader(stream, encode);
                string result = reader.ReadToEnd();

                dynamic re = JsonConvert.DeserializeObject<dynamic>(result);
                var trans = re["translation"][0];

                return _info.ToTitleCase(trans.ToString());
            }
            catch (Exception e)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 判断图形是否为拓扑简单化
        /// </summary>
        /// <param name="geometry"></param>
        /// <remarks>只适用于point, multipoint, polyline, polygon</remarks>
        public static bool IsSimpleGeometry(IGeometry geometry)
        {
            bool result = false;
            esriGeometryType pGeometryType = geometry.GeometryType;
            switch (pGeometryType)
            {
                case esriGeometryType.esriGeometryMultipoint:
                case esriGeometryType.esriGeometryPolygon:
                case esriGeometryType.esriGeometryPolyline:
                    ITopologicalOperator2 topo2 = geometry as ITopologicalOperator2;
                    topo2.IsKnownSimple_2 = false;
                    result = topo2.IsSimple;
                    break;
                case esriGeometryType.esriGeometryPoint:
                    ITopologicalOperator topo = geometry as ITopologicalOperator;
                    result = topo.IsSimple;
                    break;
            }
            return result;
        }

        /// <summary>
        /// 图形拓扑简单化
        /// </summary>
        /// <param name="geometry"></param>
        /// <remarks>只适用于point, multipoint, polyline, polygon</remarks>
        public static void SimplifyGeometry(ref IGeometry geometry)
        {
            esriGeometryType pGeometryType = geometry.GeometryType;

            switch (pGeometryType)
            {
                case esriGeometryType.esriGeometryMultipoint:
                case esriGeometryType.esriGeometryPolygon:
                case esriGeometryType.esriGeometryPolyline:

                    ITopologicalOperator2 topo2 = geometry as ITopologicalOperator2;
                    topo2.IsKnownSimple_2 = false;
                    topo2.Simplify();
                    break;

                case esriGeometryType.esriGeometryPoint:

                    ITopologicalOperator topo = geometry as ITopologicalOperator;
                    topo.Simplify();
                    break;
                //default:
                //    throw new SimplyfyException("SimplifyGeometry方法只适用于point, multipoint, polyline, polygon！");
            }
        }

        /// <summary>
        /// 将图形拓扑简单化
        /// </summary>
        /// <param name="geometry"></param>
        public static void SimplifyGeometry2(ref IGeometry geometry)
        {
            bool result;
            esriGeometryType pGeometryType = geometry.GeometryType;
            switch (pGeometryType)
            {
                case esriGeometryType.esriGeometryMultipoint:
                case esriGeometryType.esriGeometryPolygon:
                case esriGeometryType.esriGeometryPolyline:
                    ITopologicalOperator2 topo2 = geometry as ITopologicalOperator2;
                    topo2.IsKnownSimple_2 = false;
                    result = topo2.IsSimple;
                    if (!result)
                    {
                        topo2.Simplify();
                    }
                    break;
                case esriGeometryType.esriGeometryPoint:
                    ITopologicalOperator topo = geometry as ITopologicalOperator;
                    result = topo.IsSimple;
                    if (!result) topo.Simplify();
                    break;
            }
        }

        public static string BaiduTranslation(string q)
        {
            try
            {
                string translation = string.Empty;
                if (string.IsNullOrEmpty(q)) return string.Empty;
                switch (q)
                {
                    case "休息区":
                        translation = "Rest Area";
                        break;
                    case "按摩椅":
                        translation = "Massage Chair";
                        break;
                    case "服务台":
                        translation = "Information Desk";
                        break;
                    case "走廊":
                        translation = "The Corridor";
                        break;
                    case "娃娃机":
                        translation = "Doll Machine";
                        break;
                    case "楼梯":
                        translation = "The Stairs";
                        break;
                    case "电梯":
                        translation = "The Elevator";
                        break;
                    case "扶梯":
                        translation = "The Escalator";
                        break;
                    case "电梯厅":
                        translation = "The Elevator Hall";
                        break;
                    case "禁止区":
                        translation = "Forbidden Area";
                        break;
                    case "中空":
                        translation = "Hollow";
                        break;
                    case "男洗手间":
                        translation = "Men's Room";
                        break;
                    case "女洗手间":
                        translation = "Ladies' Room";
                        break;
                    case "残障洗手间":
                        translation = "Disabled Restroom";
                        break;
                    case "哺乳室":
                        translation = "Nursing Room";
                        break;
                    case "办公区":
                        translation = "Office Area";
                        break;
                    case "会议室":
                        translation = "Conference Room";
                        break;
                    case "库房":
                        translation = "Warehouse";
                        break;
                    case "空调机房":
                        translation = "Air Conditioning Room";
                        break;
                    case "清洁间":
                        translation = "Clean Room";
                        break;
                    case "空铺":
                        translation = "Empty Shop";
                        break;
                    default:
                        translation=BaiduTranslate.GetTranslationFromBaiduFanyi(q);
                        break;
                }

                return translation;
            }
            catch (Exception e)
            {
                return string.Empty;
            }
        }
    }
}
