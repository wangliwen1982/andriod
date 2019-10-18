using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ESRI.ArcGIS.Geometry;
using NLog;
using Npgsql;
using NpgsqlTypes;
using Tencent_trans.ExportData;
using Path = ESRI.ArcGIS.Geometry.Path;

namespace Tencent_trans
{
    public partial class Form1 : Form
    {
        private static readonly string currentVersion =
           ConfigurationManager.AppSettings["Version"];
        //private Product p;
        private string MDBPath;
        private string FoldPath;
        private string SdePath;
        private string GDBPath;
        private string _hwPath;
        public Form1()
        {
            //SdePath = @"C:\Users\songb\AppData\Roaming\ESRI\Desktop10.4\ArcCatalog\palmap.sde";
            SdePath = System.IO.Path.Combine(Application.StartupPath, "Template", "palmap.sde");
            GDBPath = System.IO.Path.Combine(Application.StartupPath, "Template", "Tencent.gdb");

            _hwPath = System.IO.Path.Combine(Application.StartupPath, "Template", "hwshp");
            //_hwPath = @"D:\shptest";
            //GDBPath = @"E:\Tencent\Tencent.gdb";
            //string path = @"C:\Users\songb\Desktop\Tencent\Floor.mdb";
            //string sdepath = @"C:\Users\songb\AppData\Roaming\ESRI\Desktop10.4\ArcCatalog\palmap.sde";
            //string sdepath = @"E:\Tencent\Palmap.gdb";
            //string exportpath = @"E:\Tencent\Tencent.gdb";

            //p = new Product(path, sdepath,exportpath);
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //p.Export();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //p.InPut();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择文件路径";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string foldPath = dialog.SelectedPath;
                textBox1.Text = foldPath;
                FoldPath = foldPath;

                listView1.Items.Clear();
                string[] files = Directory.GetFiles(FoldPath, "*.mdb", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    ListViewItem item = new ListViewItem(file,0);
                    item.SubItems.Add("");
                    item.SubItems.Add("准备导入...");
                    item.Checked = true;
                    listView1.Items.Add(item);
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //FolderBrowserDialog dialog = new FolderBrowserDialog();
            //dialog.Description = "请选择文件路径";
            //if (dialog.ShowDialog() == DialogResult.OK)
            //{
            //    string foldPath = dialog.SelectedPath;
            //    textBox2.Text = foldPath;
            //    FoldPath = foldPath;
            //}
        }
        
        private async void button5_Click(object sender, EventArgs e)
        {
            if (FoldPath != null)
            {
                //string[] files = Directory.GetFiles(FoldPath, "*.mdb", SearchOption.AllDirectories);
                
                string info = string.Empty;
                int i = 0;
                int suc = 0;
                int fail = 0;
                //listBox1.Items.Add($"开始导入...");
                
                //List<string> lst = new List<string>();
                foreach (ListViewItem item in listView1.Items)
                {
                    if (!item.Checked)
                    {
                        item.SubItems[2].Text = "跳过";
                        continue;
                    }
                    i++;
                    item.SubItems[2].Text = "导入中...";
                    var res = Task.Run(() =>
                    {
                        Product product = new Product(item.SubItems[0].Text, SdePath);
                        bool result = product.InPut();
                        return result;
                    });

                    if (await res)
                    {
                        suc++;
                        item.SubItems[2].Text = "成功";
                        //listBox1.Items.Add($"导入成功:{file}");
                    }
                    else
                    {
                        fail++;
                        item.SubItems[2].Text = "失败";
                        //listBox1.Items.Add($"导入失败:{file}");
                    }
                }
                //listBox1.Items.Add($"导入完成：共导入{i}个文件，成功{suc}个，失败{fail}个。");

                GetMallInfo();
                GetTreeInfo();
                GetTimeInfoNull();
                MessageBox.Show($"导入完成：共导入{i}个文件，成功{suc}个，失败{fail}个。");
            }
            else
            {
                MessageBox.Show("请选择转换路径");
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                string workpath = System.IO.Path.Combine(Application.StartupPath, "Work");
                if (Directory.Exists(workpath))
                {
                    Directory.Delete(workpath,true);
                }
                Directory.CreateDirectory(workpath);

                string mapid = comboBox1.SelectedValue.ToString();
                Product product = new Product(SdePath, GDBPath, 0);
                product.CleanFileGDB();
                product.Export(mapid);
                product.ExportWithFME(mapid);
                if (product.CheckData(mapid))
                {
                    MessageBox.Show("完成");
                }
                else
                {
                    MessageBox.Show("检查存在错误，请查看日志！");
                }
                System.Diagnostics.Process.Start(workpath);
            }
            catch (Exception exception)
            {
                Program.logger.Debug(exception);
                MessageBox.Show("导出失败");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            GetMallInfo();
            GetTreeInfo();
            GetTimeInfo();
            GetVersion();
            //指定行列单元格只读
            dataGridView1.Columns[0].ReadOnly = true;
            dataGridView1.Columns[1].ReadOnly = true;
            dataGridView1.Columns[2].ReadOnly = true;
            dataGridView1.Columns[3].ReadOnly = true;
            dataGridView1.Columns[4].ReadOnly = true;
        }

        private void GetVersion()
        {
            NpgsqlHelper PalmapDB =
                new NpgsqlHelper(ConfigurationManager.ConnectionStrings["Palmap"].ConnectionString);
            //var cc = ConfigurationManager.AppSettings["Version"];
            var latestVersion = Convert
                .ToDateTime(PalmapDB.ExecuteScalar("select datetime from dic_version order by objectid desc"))
                .ToString("yyyy/MM/dd");
            this.Text = this.Text + String.Format(" Version:{0} {1}", currentVersion,
                            currentVersion == latestVersion ? "" : "该软件不是最新版本");
        }

        private void GetMallInfo()
        {
            NpgsqlHelper PalmapDB =
                new NpgsqlHelper(ConfigurationManager.ConnectionStrings["Palmap"].ConnectionString);
            var MallInfos = PalmapDB.ExecuteDataTable("select namecn,mapid,city,udid from mall").AsEnumerable().Select(_ =>
                new
                {
                    MapID = _.Field<string>("mapid"),
                    ShowInfo = $"{_["udid"]}|{_["mapid"]}|{_["namecn"]}|{_["city"]}",
                }).ToList();
            MallInfos.Reverse();
            comboBox1.ResetText();
            comboBox1.DataSource = MallInfos;
            comboBox1.ValueMember = "MapID";
            comboBox1.DisplayMember = "ShowInfo";
        }

        private void GetTreeInfo()
        {
            NpgsqlHelper PalmapDB =
                new NpgsqlHelper(ConfigurationManager.ConnectionStrings["Palmap"].ConnectionString);
            var MallInfos = PalmapDB.ExecuteDataTable("select namecn,mapid,city,province,udid from mall").AsEnumerable()
                .Select(_ =>
                    new
                    {
                        MapID = _.Field<string>("mapid"),
                        ShowInfo = $"{_["udid"]}|{_["mapid"]}|{_["namecn"]}",
                        City = _.Field<string>("city"),
                        Province = _.Field<string>("province"),
                        UDID = _["udid"]
                    }).ToList();
            MallInfos.Reverse();
            var Tree = MallInfos.GroupBy(_ => _.Province, _ => _);

            treeView1.Nodes.Clear();
            TreeNode parNode = new TreeNode();
            parNode.Text = "PalMap";

            foreach (var t in MallInfos)
            {
                TreeNode node = new TreeNode();
                node.Name = t.MapID;
                node.Tag = t.UDID;
                node.Text = t.ShowInfo;
                parNode.Nodes.Add(node);
            }


            //foreach (var tr in Tree)
            //{
            //    TreeNode node=new TreeNode();
            //    node.Text = tr.Key;
            //    parNode.Nodes.Add(node);
            //    foreach (var tt in tr.GroupBy(_=>_.City,_=>_))
            //    {
            //        TreeNode node2 = new TreeNode();
            //        node2.Text = tt.Key;
            //        node.Nodes.Add(node2);
            //        foreach (var t in tt)
            //        {
            //            TreeNode node3 = new TreeNode();
            //            node3.Name = t.MapID;
            //            node3.Tag = t.UDID;
            //            node3.Text = t.ShowInfo;
            //            node2.Nodes.Add(node3);
            //        }
            //    }
            //}

            treeView1.Nodes.Add(parNode);
            treeView1.ExpandAll();
        }

        private void GetTimeInfo()
        {
            NpgsqlHelper PalmapDB =
                new NpgsqlHelper(ConfigurationManager.ConnectionStrings["Palmap"].ConnectionString);
            var MallInfos = PalmapDB.ExecuteDataTable("select namecn,mapid,city,openingtime,tencenttime,'' as 写入时间 from mall");

            dataGridView1.DataSource = MallInfos;
            //foreach (DataRow row in MallInfos.Rows)
            //{
            //    string openingtime = row["openingtime"].ToString();
            //    string tencenttime = Transopeningtime(openingtime);
            //    row["写入时间"] = tencenttime;
            //}
        }

        private void GetTimeInfoNull()
        {
            NpgsqlHelper PalmapDB =
                new NpgsqlHelper(ConfigurationManager.ConnectionStrings["Palmap"].ConnectionString);
            var MallInfos = PalmapDB.ExecuteDataTable("select namecn,mapid,city,openingtime,tencenttime,'' as 写入时间 from mall where tencenttime is null");
            
            dataGridView1.DataSource = MallInfos;
            foreach (DataRow row in MallInfos.Rows)
            {
                string openingtime = row["写入时间"].ToString();
                string tencenttime = "bbb";
                row["写入时间"] = tencenttime;
            }
        }

        private string Transopeningtime(string openingtime)
        {
            try
            {
                string[] split = openingtime.Split(' ');
                string result=String.Empty;
                foreach (var str in split)
                {
                    if (str.Trim()!=String.Empty)
                    {
                        string[] split2 = str.Split('：');
                        if (split2.Length == 2)
                        {
                            string day=String.Empty;
                            if (split2[0].Contains("每天"))
                            {
                                day=String.Empty;
                            }
                            else
                            {
                                string[] split3 = split2[0].Split('-');

                                day = $"({ConvertNum(split3[0])})({ConvertNum(split3[1])})";

                            }
                            string[] split4 = split2[1].Split('-');

                            string hour = "H" + DateTime.Parse(split4[0]).Hour;
                            string minute = DateTime.Parse(split4[0]).Minute==0?String.Empty : "M" + DateTime.Parse(split4[0]).Minute;
                            string hour2 = "H" + DateTime.Parse(split4[1]).Hour;
                            string minute2 = DateTime.Parse(split4[1]).Minute == 0 ? String.Empty : "M" + DateTime.Parse(split4[1]).Minute;
                            string path = $"({hour}{minute})({hour2}{minute2})";

                            string re = day == String.Empty ? $"[{path}]" : $"[{path}]*[{day}]";
                            result += re + "+";
                        }
                    }
                }
                return result.TrimEnd('+');
            }
            catch (Exception e)
            {
                return string.Empty;
            }
        }

        private string ConvertNum(string str)
        {
            switch (str.Substring(str.Length - 1))
            {
                case "日":
                    return "D7";
                case "六":
                    return "D6";
                case "五":
                    return "D5";
                case "四":
                    return "D4";
                case "三":
                    return "D3";
                case "二":
                    return "D2";
                case "一":
                    return "D1";
                default:
                    return String.Empty;
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Product product = new Product(SdePath, GDBPath, 0);
            product.CleanFileGDB();
            GetMallInfo();
            MessageBox.Show("完成");
        }

        private async void button1_Click_1(object sender, EventArgs e)
        {
            try
            {
                string workpath = System.IO.Path.Combine(Application.StartupPath, "Work");
                if (Directory.Exists(workpath))
                {
                    Directory.Delete(workpath,true);
                }
                Directory.CreateDirectory(workpath);

                listView1.Items.Clear();
                tabControl1.SelectTab(tabPage1);

                //List<TreeNode> lstNodes = getCheckedNode(treeView1.Nodes[0],new List<TreeNode>());

                foreach (var checkedNode in listBox1.Items)
                {
                    //string mapid = checkedNode.Name;
                    string mapid = checkedNode.ToString().Split('|')[1];

                    ListViewItem item = new ListViewItem(checkedNode.ToString(), 0);
                    item.SubItems.Add(mapid);
                    item.SubItems.Add("导出中...");
                    item.Checked = true;
                    listView1.Items.Add(item);

                    var res = Task.Run(() =>
                    {
                        try
                        {
                            Product product = new Product(SdePath, GDBPath, 0);
                            //Thread.Sleep(5000);
                            product.CleanFileGDB();
                            product.Export(mapid);
                            product.ExportWithFME(mapid);
                            if (product.CheckData(mapid))
                            {
                                return 0;
                            }
                            else
                            {
                                return 1;
                            }
                        }
                        catch (Exception exception)
                        {
                            Program.logger.Debug(exception.Message, mapid);
                            Console.WriteLine(exception);
                            return 2;
                        }
                    });

                    switch (await res)
                    {
                        case 0:
                            item.SubItems[2].Text = "成功";
                            break;
                        case 1:
                            item.SubItems[2].Text = "存在问题";
                            break;
                        case 2:
                            item.SubItems[2].Text = "失败";
                            break;
                    }
                }

                //foreach (TreeNode node1 in treeView1.Nodes)
                //{
                //    foreach (TreeNode node2 in node1.Nodes)
                //    {
                //        foreach (TreeNode node3 in node2.Nodes)
                //        {
                //            foreach (TreeNode node4 in node3.Nodes)
                //            {
                //                if (node4.Checked)
                //                {
                //                    string mapid = node4.Name;

                //                    ListViewItem item = new ListViewItem(node4.FullPath, 0);
                //                    item.SubItems.Add(node4.Name);
                //                    item.SubItems.Add("导出中...");
                //                    item.Checked = true;
                //                    listView1.Items.Add(item);

                //                    var res = Task.Run(() =>
                //                    {
                //                        try
                //                        {
                //                            Product product = new Product(SdePath, GDBPath, 0);
                //                            //Thread.Sleep(5000);
                //                            product.CleanFileGDB();
                //                            product.Export(mapid);
                //                            product.ExportWithFME(mapid);
                //                            if (product.CheckData(mapid))
                //                            {
                //                                return 0;
                //                            }
                //                            else
                //                            {
                //                                return 1;
                //                            }
                //                        }
                //                        catch (Exception exception)
                //                        {
                //                            Program.logger.Debug(exception, mapid);
                //                            Console.WriteLine(exception);
                //                            return 2;
                //                        }
                //                    });

                //                    switch (await res)
                //                    {
                //                        case 0:
                //                            item.SubItems[2].Text = "成功";
                //                            break;
                //                        case 1:
                //                            item.SubItems[2].Text = "存在问题";
                //                            break;
                //                        case 2:
                //                            item.SubItems[2].Text = "失败";
                //                            break;
                //                    }
                //                }
                //            }
                //        }
                //    }
                //}

                System.Diagnostics.Process.Start(workpath);
                MessageBox.Show("完成");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                MessageBox.Show("导出失败");
            }
        }

        private List<TreeNode> getCheckedNode(TreeNode node,List<TreeNode> checkedNodes)
        {
            foreach (TreeNode tn in node.Nodes)
            {
                if (tn.Checked && tn.Name != null)
                {
                    checkedNodes.Add(tn);
                }
                getCheckedNode(tn, checkedNodes);
            }
            return checkedNodes;
        }

        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Action == TreeViewAction.ByMouse)
            {
                if (e.Node.Checked == true)
                {
                    //选中节点之后，选中该节点所有的子节点
                    setChildNodeCheckedState(e.Node, true);

                    if (e.Node.Text=="PalMap")
                    {
                        foreach (TreeNode node in e.Node.Nodes)
                        {
                            listBox1.Items.Add(node.Text);
                        }
                    }
                    else
                    {
                        if (!listBox1.Items.Contains(e.Node.Text))
                        {
                            listBox1.Items.Add(e.Node.Text);
                        }
                    }
                }
                else if (e.Node.Checked == false)
                {
                    if (e.Node.Text == "PalMap")
                    {
                        listBox1.Items.Clear();
                    }
                    else
                    {
                        if (listBox1.Items.Contains(e.Node.Text))
                        {
                            listBox1.Items.Remove(e.Node.Text);
                        }
                    }

                    //取消节点选中状态之后，取消该节点所有子节点选中状态
                    setChildNodeCheckedState(e.Node, false);
                    //如果节点存在父节点，取消父节点的选中状态
                    if (e.Node.Parent != null)
                    {
                        setParentNodeCheckedState(e.Node, false);
                    }
                }
            }
        }
        //取消节点选中状态之后，取消所有父节点的选中状态
        private void setParentNodeCheckedState(TreeNode currNode, bool state)
        {
            TreeNode parentNode = currNode.Parent;
            parentNode.Checked = state;
            if (currNode.Parent.Parent != null)
            {
                setParentNodeCheckedState(currNode.Parent, state);
            }
        }
        //选中节点之后，选中节点的所有子节点
        private void setChildNodeCheckedState(TreeNode currNode, bool state)
        {
            TreeNodeCollection nodes = currNode.Nodes;
            if (nodes.Count > 0)
            {
                foreach (TreeNode tn in nodes)
                {
                    tn.Checked = state;
                    setChildNodeCheckedState(tn, state);
                }
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            NpgsqlHelper PalmapDB =
                new NpgsqlHelper(ConfigurationManager.ConnectionStrings["Palmap"].ConnectionString);
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                string mapid = row.Cells["mapid"].Value.ToString();
                string time= row.Cells["写入时间"].Value.ToString();
                if (time.Trim() != "")
                {
                    row.Cells["tencenttime"].Value = time;
                    string sql = $"update mall set tencenttime='{time}' where mapid = '{mapid}'";
                    PalmapDB.ExecuteNonQuery(sql);
                }
            }
            MessageBox.Show("更新成功");
        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            GetTimeInfo();
        }

        private void button8_Click_1(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                string time = row.Cells["openingtime"].Value.ToString();

                    row.Cells["写入时间"].Value = Transopeningtime(time);
                
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("确认清理", "", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                Product product = new Product(SdePath, GDBPath, 0);
                product.Clean();
                GetMallInfo();
                GetTreeInfo();
                GetTimeInfo();
                MessageBox.Show("完成");
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            string mapid = comboBox1.SelectedValue.ToString();

            DialogResult dr = MessageBox.Show($"确认清理MapID={mapid}的数据？", "", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                Product product = new Product(SdePath, GDBPath, 0);
                product.Clean($"mapid = '{mapid}'");
                GetMallInfo();
                GetTreeInfo();
                GetTimeInfo();
                MessageBox.Show("完成");
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            GetTreeInfo();

            string text = textBox2.Text.Trim();

            List<TreeNode> lstNodes=new List<TreeNode>();
            foreach (TreeNode selNode in treeView1.Nodes[0].Nodes)
            {
                if (selNode != null)
                {
                    bool flag = false;

                    string name = selNode.Name ?? string.Empty;
                    string tag = selNode.Tag.ToString();

                    if (name.Contains(text) || tag.Contains(text))
                    {

                    }
                    else
                    {
                       lstNodes.Add(selNode);
                    }
                }
            }
            foreach (TreeNode selNode in lstNodes)
            {
                selNode.Remove();
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            string mapid = comboBox1.SelectedValue.ToString();

            //NpgsqlHelper PalmapDB =
            //    new NpgsqlHelper(ConfigurationManager.ConnectionStrings["Palmap"].ConnectionString);

            NpgsqlHelper PostGIS =
                new NpgsqlHelper(ConfigurationManager.ConnectionStrings["PostGIS"].ConnectionString);

            //DataTable Frame = PalmapDB.ExecuteDataTable(
            //   $"select objid,bytea(st_asbinary(shape)) as binary,text(st_astext(shape)) as text from frame_polygon where mapid = '{mapid}'");

            string polygon =
                "LINESTRING(11586593.1242 3557636.3912,11586590.8041 3557634.4383,11586588.2238 3557637.5031,11586589.3833 3557638.4795,11586590.5428 3557639.4557,11586593.1242 3557636.3912)";
            PostGIS.ExecuteNonQuery($"INSERT INTO postgres.angle_line (shape) VALUES (st_geomfromtext('{polygon}', 3785))");

            NpgsqlParameter p=new NpgsqlParameter("p",DbType.String);
            p.Value = polygon;
            PostGIS.ExecuteNonQuery($"INSERT INTO postgres.angle_line (shape) VALUES (st_geomfromtext(@p, 3785))",
                new[] {p});

            var Tables=new (string table,string field)[]
            {
                ("aa",""),
                ("aa",""),
                ("aa",""),
                ("aa",""),
                ("aa",""),
                ("aa",""),
                ("aa",""),
                ("aa",""),
                ("aa",""),
                ("aa","")
            };

            //DataTable dt = PostGIS.ExecuteDataTable("select shape from postgres.frame_polygon");

            //DataTable Frame2 = PostGIS.ExecuteDataTable(
            //    $"SELECT row_to_json(fc)::json AS frame FROM (SELECT 'FeatureCollection' AS type, array_to_json(array_agg(f)) AS features FROM (SELECT 'Feature' AS type,row_to_json((SELECT l FROM(SELECT 'test' as planar_graph) AS l)) AS properties,ST_AsGeoJSON(ST_Transform(lg.shape, 4326))::json AS geometry FROM (select ST_ConvexHull(ST_Collect(ST_Transform(shape, 4326))) as shape from postgres.door_point group by mapid) AS lg) AS f) AS fc");

            //DataTable Frame3 = PostGIS.ExecuteDataTable($"select * from spatial_ref_sys");
            //PostgisGeometry pf;
            //NpgsqlPolygon g;

        }

        private void button11_Click_1(object sender, EventArgs e)
        {
            NpgsqlHelper PalmapDB =
                new NpgsqlHelper(ConfigurationManager.ConnectionStrings["Palmap"].ConnectionString);
            DateTime dt=DateTime.Now;
            PalmapDB.ExecuteNonQuery($"update dic_version set datetime='{dt.ToString("yyyy/MM/dd")}'");
        }

        private async void btnHuawei_Click(object sender, EventArgs e)
        {
            var workpath = string.Empty;

            if (FoldPath != null)
            {
                workpath = System.IO.Path.Combine(Application.StartupPath, "Work");
                if (Directory.Exists(workpath))
                {
                    Directory.Delete(workpath, true);
                }
                Directory.CreateDirectory(workpath);
                foreach (ListViewItem item in listView1.Items)
                {
                    try
                    {
                        if (!item.Checked)
                        {
                            item.SubItems[2].Text = @"跳过";
                            continue;
                        }
                        item.SubItems[2].Text = @"导出中...";
                        var res = Task.Run(() =>
                        {
                            PalmapMdbEx product = new PalmapMdbEx(item.SubItems[0].Text, _hwPath, workpath);
                            return product.Export(item.SubItems[0].Text);
                        });
                        if (await res)
                            item.SubItems[2].Text = @"导出成功";
                    }
                    catch (Exception exception)
                    {
                        Program.logger.Debug(exception);
                        item.SubItems[2].Text = @"导出失败";
                    }
                }
                //MessageBox.Show($@"导出完成：共导出{i}个文件，成功{suc}个，失败{fail}个。");
                System.Diagnostics.Process.Start(workpath);
            }
            else
            {
                MessageBox.Show(@"请选择转换路径");
            }
        }
    }
}
