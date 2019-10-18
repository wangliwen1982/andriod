using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Tencent_trans.Common
{
    public class BaiduTranslate
    {
        const string appId = "20181217000249499";
        const string password = "xe_wp_juuv_TCc0Z8oSK";

        public static string GetTranslationFromBaiduFanyi(string q, Language from=Language.zh, Language to= Language.en)
        {
            string jsonResult = String.Empty;
            //源语言
            string languageFrom = from.ToString().ToLower();
            //目标语言
            string languageTo = to.ToString().ToLower();
            //随机数
            string randomNum = System.DateTime.Now.Millisecond.ToString();
            //md5加密
            string md5Sign = getMd5(appId + q + randomNum + password);

            var client = new RestClient("http://api.fanyi.baidu.com");
            var request = new RestRequest("/api/trans/vip/translate", Method.GET);
            request.AddParameter("q", q);
            request.AddParameter("from", from);
            request.AddParameter("to", to);
            request.AddParameter("appid", appId);
            request.AddParameter("salt", randomNum);
            request.AddParameter("sign", md5Sign);
            IRestResponse response = client.Execute(request);

            //解析json
            TranslationResult result=JsonConvert.DeserializeObject<TranslationResult>(response.Content);
            return processing(result.Trans_result[0].Dst);
        }

        static string getMd5(string str)
        {
            var md5 = new MD5CryptoServiceProvider();
            var result = Encoding.UTF8.GetBytes(str);
            var output = md5.ComputeHash(result);
            return BitConverter.ToString(output).Replace("-", "").ToLower();
        }

        public enum Language
        {
            //百度翻译API官网提供了多种语言，这里只列了几种
            auto = 0,
            zh = 1,
            en = 2,
            cht = 3,
        }
        
        /// <summary>
        /// 将首字母大写
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string processing(string str)//处理这段英文的方法
        {
            string[] strArray = str.Split(" ".ToCharArray());
            string result = string.Empty;//定义一个空字符串

            foreach (string s in strArray)//循环处理数组里面每一个字符串
            {
                result += System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(s) + " ";
                //result += s.Substring(0, 1).ToUpper() + s.Substring(1) + " ";//.Substring(0, 1).ToUpper()把循环到的字符串第一个字母截取并转换为大写，并用s.Substring(1)得到循环到的字符串除第一个字符后的所有字符拼装到首字母后面。
            }
            //Console.ReadKey();
            return result.Substring(0, result.Length - 1);
        }
    }

    public class Translation
    {
        public string Src { get; set; }
        public string Dst { get; set; }
    }

    public class TranslationResult
    {
        //错误码，翻译结果无法正常返回
        public string Error_code { get; set; }
        public string Error_msg { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Query { get; set; }
        //翻译正确，返回的结果
        //这里是数组的原因是百度翻译支持多个单词或多段文本的翻译，在发送的字段q中用换行符（\n）分隔
        public Translation[] Trans_result { get; set; }
    }

}
