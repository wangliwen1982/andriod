using ESRI.ArcGIS;
using System;
using System.Windows.Forms;
using ESRI.ArcGIS.esriSystem;

namespace Tencent_trans
{
    static class Program
    {
        public static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (!RuntimeManager.Bind(ProductCode.Engine))
            {
                if (!RuntimeManager.Bind(ProductCode.Desktop))
                {
                    MessageBox.Show(@"Unable to bind to ArcGIS runtime. Application will be shut down.");
                    return;
                }
            }

            ESRI.ArcGIS.esriSystem.IAoInitialize ao = new ESRI.ArcGIS.esriSystem.AoInitialize();
            ao.Initialize(ESRI.ArcGIS.esriSystem.esriLicenseProductCode.esriLicenseProductCodeAdvanced);
            ao.Initialize(esriLicenseProductCode.esriLicenseProductCodeArcServer);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
