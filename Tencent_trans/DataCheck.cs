using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geodatabase;

namespace Tencent_trans
{
    class DataCheck
    {
        private IWorkspace workspace;
        private IFeatureWorkspace featureWorkspace;

        public DataCheck(string mdbpath)
        {
            IWorkspaceFactory workspaceFactory = new AccessWorkspaceFactoryClass();
            workspace = workspaceFactory.OpenFromFile(mdbpath, 0);
            featureWorkspace = workspace as IFeatureWorkspace;
        }

        private ICursor Def(string tables, string subfields, string whereClause)
        {
            IQueryDef def = featureWorkspace.CreateQueryDef();
            def.Tables = tables;
            def.SubFields = subfields;
            def.WhereClause = whereClause;
            ICursor cursor = def.Evaluate();
            return def.Evaluate();
        }

        public void StartCheck()
        {
            var FeatureidCheck = new(string FeatureClass, string Field)[]
            {
                ( "Mall","MapID"),
                ( "Building","BDID"),
                ( "Floors","FLID"),
                ( "Lane_Line","LinkID"),
                ( "LanePoint_Point","NodeID"),
                ( "",""),
                ( "",""),
                ( "",""),
                ( "","")
            };
        }
    }
}
