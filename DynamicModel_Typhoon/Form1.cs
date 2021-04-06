using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ESRI.ArcGIS.esriSystem;

using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.DataSourcesOleDB;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Geodatabase;
using System.IO;

namespace DynamicModel_Typhoon
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            nhd = new Neo4jHandler("bolt://localhost:7687", "neo4j", "P@ssw0rd");
            textBox1.Text = System.AppDomain.CurrentDomain.BaseDirectory+ "lima.csv";
            textBox2.Text = System.AppDomain.CurrentDomain.BaseDirectory + "typhoonProcess.csv";
            textBox3.Text = System.AppDomain.CurrentDomain.BaseDirectory + "WarnStates.csv";



        }
        Neo4jHandler nhd;

        private GeoProcess getPartProcess(string statev, Dictionary<string, string> statesofprocess, List<GeoProcess> list)
        {
            foreach (string item in statesofprocess.Keys)
            {
                int minid = Convert.ToInt32(statesofprocess[item].Split(new char[2] { '-', '-' })[0]);
                int maxid = Convert.ToInt32(statesofprocess[item].Split(new char[2] { '-', '-' })[2]);
                int cur_id = Convert.ToInt32(statev);
                if (cur_id >= minid && cur_id <= maxid)
                {
                    foreach (GeoProcess gp in list)
                    {
                        if (gp.Name == item)
                        {
                            return gp;
                        }
                    }
                }
            }
            return null;
        }
        private GeoState getStateByName(string v, List<GeoState> allstate)
        {
            foreach (GeoState gs in allstate)
            {
                if (gs.Name == v)
                    return gs;
            }
            return null;
        }
        public ITable OpenCSVFile(string csvFullPath)
        {
            string csvPath = System.IO.Path.GetDirectoryName(csvFullPath); //csv文件的文件夹位置
            string csvName = System.IO.Path.GetFileName(csvFullPath);//csv文件的文件名
            IWorkspaceFactory pWorkspaceFactory = new OLEDBWorkspaceFactory();
            IPropertySet pPropSet = new PropertySet();
            //注意如果csv文件的字符编码是utf-8
            //pPropSet.SetProperty("CONNECTSTRING", "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + tablePath + ";Extended Properties='Text;HDR=Yes;IMEX=1;CharacterSet=65001;'");
            pPropSet.SetProperty("CONNECTSTRING", "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + csvPath + ";Extended Properties='Text;HDR=Yes;IMEX=1;'");
            IWorkspace pWorkspace = pWorkspaceFactory.Open(pPropSet, 0);
            IFeatureWorkspace pFeatureWorkspace2 = (IFeatureWorkspace)pWorkspace;
            ITable pTable = pFeatureWorkspace2.OpenTable(csvName);
            return pTable;
        }
        private void CreateTyphoonGraphDb()
        {
            //创建台风事件
            GeoEvent ge = new GeoEvent();
            ge.Name = "Lecima Typhoon Event";
            GeoProcess pLekimaTyphoonProcess = new GeoProcess("Lekima Typhoon Process");
            pLekimaTyphoonProcess.Name = "Lekima Typhoon Process";
            string sql0 = $"Merge (n:GeoProcess {{ Name:'" + pLekimaTyphoonProcess.Name + $"'}})";
            nhd.CreateBySQL(sql0);
            string sql01 = $"Merge (n:GeoEvent {{ Name:'" + ge.Name + $"'}})";
            string sql11 = $" MATCH(a: GeoEvent),(b: GeoProcess) WHERE a.Name = '" + ge.Name + "' AND b.Name = '" + pLekimaTyphoonProcess.Name + "' Merge(a) -[r: detailedby]-> (b) RETURN r";
            nhd.CreateBySQL(sql01);
            nhd.CreateBySQL(sql11);
            ge.DetialProcess = pLekimaTyphoonProcess;
            List<GeoProcess> childrenProcess = null;

            ITable pLekimaProcessTb = OpenCSVFile(textBox2.Text);//"D:\\地理动态模拟\\地理场景时空动态模型与空间推理\\利马台风建库\\typhoonProcess.csv"
            Dictionary<string, string> statesofprocess = new Dictionary<string, string>();
            try
            {
                childrenProcess = new List<GeoProcess>();
                IQueryFilter que = new QueryFilterClass();
                ICursor pCursor = pLekimaProcessTb.Search(que, true);
                IRow pRow = pCursor.NextRow();
                GeoProcess gpbefor = null;
                if (pRow != null)
                {
                    while (pRow != null)
                    {
                        string sql = $"Merge (n:GeoProcess{{";
                        string sql2 = "";
                        string sql3 = "";
                        GeoProcess gp = new GeoProcess();

                        //for (int j = 0; j < pCursor.Fields.FieldCount; j++)
                        //    stateDic.Add(keys[j], pRow.get_Value(j).ToString());
                        statesofprocess.Add(pRow.get_Value(1).ToString(), pRow.get_Value(6).ToString());
                        gp.Name = pRow.get_Value(1).ToString();
                        sql = sql + "Name:'" + pRow.get_Value(1).ToString() + "',";
                        gp.StartTime = pRow.get_Value(2).ToString();
                        sql = sql + "StartTime:'" + pRow.get_Value(2).ToString() + "',";
                        gp.EndTime = pRow.get_Value(3).ToString();
                        sql = sql + "EndTime:'" + pRow.get_Value(3).ToString() + "',";
                        if (pRow.get_Value(4).ToString() != "")
                            gp.Precede = gpbefor;
                        sql = sql.Substring(0, sql.Length - 1);
                        sql = sql + $"}})";
                        if (gpbefor != null)
                        {
                            sql2 = $" MATCH(a: GeoProcess),(b: GeoProcess) WHERE a.Name = '" + gp.Name + "' AND b.Name = '" + gpbefor.Name + "' Merge(a) -[r: Precede]-> (b) RETURN r";
                        }
                        gpbefor = gp;
                        childrenProcess.Add(gp);
                        sql3 = " MATCH(a: GeoProcess),(b: GeoProcess) WHERE a.Name = '" + pLekimaTyphoonProcess.Name + "' AND b.Name = '" + gp.Name + "' Merge(a)<-[r: IncludeBy]- (b) RETURN r";
                        pRow = pCursor.NextRow();
                        if (sql != "" && gp.Name != "Lecima Typhoon Invade Taizhou")
                            nhd.CreateBySQL(sql);
                        nhd.CreateBySQL(sql3);
                        if (sql2 != "" && gp.Name != "Lecima Typhoon Invade Taizhou")
                            nhd.CreateBySQL(sql2);
                    }

                }
                pLekimaTyphoonProcess.SubGeoprocess = childrenProcess;
            }
            catch (System.Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }
            // 创建状态
            List<GeoState> allstate = new List<GeoState>();
            ITable pTb = OpenCSVFile(textBox1.Text);//"D:\\地理动态模拟\\地理场景时空动态模型与空间推理\\利马台风建库\\lima.csv"
            int fieldcount = pTb.Fields.FieldCount;
            string[] keys = new string[] { "ID", "TIME", "WID", "TSD", "X", "Y", " Pressure", "Speed", "Direct", "KTS", "KT1", "KT2", "KT3", "KT4", "KT5", "ID2" };

            //try
            {

                IQueryFilter que = new QueryFilterClass();
                ICursor pCursor = pTb.Search(que, true);
                IRow pRow = pCursor.NextRow();
                GeoState gsbefor = null;
                int i = 0;
                if (pRow != null)
                {
                    while (pRow != null)
                    {
                        string sql = $"Merge (n:GeoState{{ Name:'";
                        string sql2 = "";
                        string sql22 = "";
                        string sql3 = "";
                        GeoState gs = new GeoState();
                        Dictionary<string, string> stateDic = new Dictionary<string, string>();
                        sql = sql + "State" + pRow.get_Value(0).ToString() + "',";
                        for (int j = 0; j < pCursor.Fields.FieldCount; j++)
                        {
                            stateDic.Add(keys[j], pRow.get_Value(j).ToString());
                            sql = sql + keys[j] + ":'" + pRow.get_Value(j).ToString() + "',";
                        }
                        sql = sql.Substring(0, sql.Length - 1);
                        sql = sql + "}) return n";


                        gs.Properties = stateDic;
                        gs.Name = "State" + pRow.get_Value(0).ToString();
                        gs.Precede = gsbefor;

                        allstate.Add(gs);
                        if (gsbefor != null)
                        {
                            sql2 = " MATCH(a: GeoState),(b: GeoState) WHERE a.Name = '" + gs.Name + "' AND b.Name = '" + gsbefor.Name + "' Merge(a) -[r: Precede]-> (b) RETURN r";
                            sql22 = " MATCH(a: GeoState),(b: GeoState) WHERE a.Name = '" + gs.Name + "' AND b.Name = '" + gsbefor.Name + "' Merge(a) <-[r: Next]-> (b) RETURN r";
                        }

                        gsbefor = gs;
                        GeoProcess newgp = getPartProcess(pRow.get_Value(0).ToString(), statesofprocess, pLekimaTyphoonProcess.SubGeoprocess);
                        gs.ParentProcess = newgp;
                        if (newgp.Geostates == null)
                            newgp.Geostates = new List<GeoState>();
                        newgp.Geostates.Add(gs);

                        if (newgp.Geostates.Count > 0)
                        {
                            sql3 = " MATCH(a: GeoProcess),(b: GeoState) WHERE a.Name = '" + newgp.Name + "' AND b.Name = '" + gs.Name + "' Merge(a) <-[r: IncludeBy]- (b) RETURN r";   //误打误

                        }

                        pRow = pCursor.NextRow();
                        i = i + 1;
                        nhd.CreateBySQL(sql);
                        if (sql2 != "")
                        {
                            nhd.CreateBySQL(sql2);
                            nhd.CreateBySQL(sql22);
                        }
                        if (sql3 != "")
                            nhd.CreateBySQL(sql3);
                    }

                }
            }
            //catch (System.Exception ex)
            //{
            //    MessageBox.Show(ex.Message);
            //}
            // 创建台风预警过程

            GeoProcess gpw = new GeoProcess();
            gpw.Name = "Lecima Typhoon Warning";
            ITable pWarningTb = OpenCSVFile(textBox3.Text);//"D:\\地理动态模拟\\地理场景时空动态模型与空间推理\\利马台风建库\\WarnStates.csv"

            string[] ks = new string[] { "ID", "Time", "ZF", "Wind", "Level", " Rain_6", "Rain_3", "Mutual" };

            try
            {

                IQueryFilter que = new QueryFilterClass();
                ICursor pCursor = pWarningTb.Search(que, true);
                IRow pRow = pCursor.NextRow();
                GeoState gsbefor = null;
                int i = 0;
                if (pRow != null)
                {
                    while (pRow != null)
                    {
                        string sql = $"Merge (n:GeoState{{ Name:'";
                        string sql2 = "";
                        string sql22 = "";
                        string sql3 = "";
                        string sql33 = "";
                        string sql4 = "";

                        GeoState gs = new GeoState();
                        gs.Name = "Warning_State" + pRow.get_Value(0).ToString();
                        Dictionary<string, string> stateDic = new Dictionary<string, string>();
                        sql = sql + gs.Name + "',";
                        for (int j = 0; j < pCursor.Fields.FieldCount; j++)
                        {
                            stateDic.Add(ks[j], pRow.get_Value(j).ToString());
                            sql = sql + ks[j] + ":'" + pRow.get_Value(j).ToString() + "',";
                        }
                        sql = sql.Substring(0, sql.Length - 1);
                        sql = sql + "})";

                        if (gsbefor != null)
                        {
                            sql2 = " MATCH(a: GeoState),(b: GeoState) WHERE a.Name = '" + gs.Name + "' AND b.Name = '" + gsbefor.Name + "' Merge (a) -[r: Precede]-> (b) RETURN r";
                            sql22 = " MATCH(a: GeoState),(b: GeoState) WHERE a.Name = '" + gs.Name + "' AND b.Name = '" + gsbefor.Name + "' Merge (a) <-[r: Next]- (b) RETURN r";

                        }
                        gs.Precede = gsbefor;
                        gs.Properties = stateDic;


                        gs.ParentProcess = gpw;
                        GeoState pgs = getStateByName(pRow.get_Value(7).ToString(), allstate);
                        gs.MutualState = pgs;
                        gs.ParentProcess = gpw;
                        if (gpw.Geostates == null)
                            gpw.Geostates = new List<GeoState>();
                        gpw.Geostates.Add(gs);

                        sql3 = " MATCH(a: GeoState),(b: GeoState) WHERE a.Name = '" + gs.Name + "' AND b.Name = '" + pgs.Name + "' Merge(a) <-[r: Mutual]- (b) RETURN r";
                        //sql33 = " MATCH(a: GeoState),(b: GeoState) WHERE a.Name = '" + gs.Name + "' AND b.Name = '" + pgs.Name + "' Merge(a) -[r: Mutual]-> (b) RETURN r";

                        sql4 = " MATCH(a: GeoProcess),(b: GeoState) WHERE a.Name = '" + gpw.Name + "' AND b.Name = '" + gs.Name + "' Merge(a) -[r: IncludeBy]-> (b) RETURN r";

                        gsbefor = gs;
                        pRow = pCursor.NextRow();
                        i = i + 1;
                        nhd.CreateBySQL(sql);
                        if (sql2 != "")
                        {
                            nhd.CreateBySQL(sql2);
                            nhd.CreateBySQL(sql22);
                        }
                        if (pgs != null)
                        {
                            nhd.CreateBySQL(sql3);
                            //nhd.CreateBySQL(sql33);
                        }

                        nhd.CreateBySQL(sql4);
                    }

                }

            }
            catch (System.Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }

            //构建预警相互影响
            List<string> sqllist = new List<string>();

            sqllist.Add("MATCH(a: GeoState),(b: GeoState) WHERE a.Name = 'Warning_State1' AND b.Name = 'State34' Merge(a) <-[r: Mutual]- (b) RETURN r");
            sqllist.Add("MATCH(a: GeoState),(b: GeoState) WHERE a.Name = 'Warning_State1' AND b.Name = 'State37' Merge(a) <-[r: Mutual]- (b) RETURN r");
            sqllist.Add("MATCH(a: GeoState),(b: GeoState) WHERE a.Name = 'Warning_State1' AND b.Name = 'State36' Merge(a) <-[r: Mutual]- (b) RETURN r");
            sqllist.Add("MATCH(a: GeoState),(b: GeoState) WHERE a.Name = 'Warning_State1' AND b.Name = 'State35' Merge(a) <-[r: Mutual]- (b) RETURN r");
            sqllist.Add("MATCH(a: GeoState),(b: GeoState) WHERE a.Name = 'Warning_State3' AND b.Name = 'State64' Merge(a) <-[r: Mutual]- (b) RETURN r");
            sqllist.Add("MATCH(a: GeoState),(b: GeoState) WHERE a.Name = 'Warning_State3' AND b.Name = 'State63' Merge(a) <-[r: Mutual]- (b) RETURN r");
            sqllist.Add("MATCH(a: GeoState),(b: GeoState) WHERE a.Name = 'Warning_State3' AND b.Name = 'State64' Merge(a) <-[r: Mutual]- (b) RETURN r");
            sqllist.Add("MATCH(a: GeoState),(b: GeoState) WHERE a.Name = 'Warning_State3' AND b.Name = 'State61' Merge(a) <-[r: Mutual]- (b) RETURN r");
            sqllist.Add("MATCH(a: GeoState),(b: GeoState) WHERE a.Name = 'Warning_State2' AND b.Name = 'State51' Merge(a) <-[r: Mutual]- (b) RETURN r");
            sqllist.Add("MATCH(a: GeoState),(b: GeoState) WHERE a.Name = 'Warning_State2' AND b.Name = 'State50' Merge(a) <-[r: Mutual]- (b) RETURN r");
            sqllist.Add("MATCH(a: GeoState),(b: GeoState) WHERE a.Name = 'Warning_State2' AND b.Name = 'State49' Merge(a) <-[r: Mutual]- (b) RETURN r");
            sqllist.Add("MATCH(a: GeoState),(b: GeoState) WHERE a.Name = 'Warning_State2' AND b.Name = 'State48' Merge(a) <-[r: Mutual]- (b) RETURN r");
            sqllist.Add("MATCH(a: GeoState),(b: GeoState) WHERE a.Name = 'Warning_State2' AND b.Name = 'State47' Merge(a) <-[r: Mutual]- (b) RETURN r");


            foreach (string a in sqllist)
            {
                nhd.CreateBySQL(a);
            }

            //构建台风登陆台州事件，台风登陆台州过程
            GeoEvent geTZ = new GeoEvent();
            geTZ.Name = "Lecima Typhoon Loadfall Taizhou Event";
            string b = $"Merge (n:GeoEvent {{ Name:'Lecima Typhoon Loadfall Taizhou Event',Speed:'52 m/s',Level:'16',Time:'2019-8-10 1：45',Location:'Chengnan Town, Wenling City'}})";
            nhd.CreateBySQL(b);
            string c = $" MATCH(a:GeoState ),(b: GeoEvent) WHERE a.Name = '" + "State80" + "' AND b.Name = '" + geTZ.Name + "' Merge(a) <-[r: registerby] - (b) RETURN r";// $" MATCH(a: GeoEvent),(b: GeoState) WHERE a.Name = '" + geTZ.Name + "' AND b.Name = '" +"State80" + "' Merge(a) -[r: register]<- (b) RETURN r";
            nhd.CreateBySQL(c);

            GeoProcess tzgp = new GeoProcess();
            tzgp.Name = "Lecima Typhoon Invade Taizhou";
            string d = $"Merge (n:GeoProcess {{ Name:'" + tzgp.Name + $"',StartTime:'2019-8-10 3:00',EndTime:'2019-8-10 15:00'}})";
            nhd.CreateBySQL(d);

            GeoEvent geTZTQ = new GeoEvent();
            geTZTQ.Name = "Lecima Typhoon Invade Taizhou Event";
            string ee = $"Merge (n:GeoEvent {{ Name:'" + geTZTQ.Name + $"'}})";
            nhd.CreateBySQL(ee);

            List<string> sqllist2 = new List<string>();
            sqllist2.Add("MATCH(a: GeoProcess),(b: GeoState) WHERE a.Name = '" + tzgp.Name + $"' AND b.Name = 'State80' Merge(a) <-[r:IncludeBy]- (b) RETURN r");
            sqllist2.Add("MATCH(a: GeoProcess),(b: GeoState) WHERE a.Name = '" + tzgp.Name + $"' AND b.Name = 'State81' Merge(a) <-[r: IncludeBy]- (b) RETURN r");
            sqllist2.Add("MATCH(a: GeoProcess),(b: GeoState) WHERE a.Name = '" + tzgp.Name + $"' AND b.Name = 'State82' Merge(a) <-[r: IncludeBy]- (b) RETURN r");
            sqllist2.Add("MATCH(a: GeoProcess),(b: GeoState) WHERE a.Name = '" + tzgp.Name + $"' AND b.Name = 'State83' Merge(a) <-[r: IncludeBy]- (b) RETURN r");
            sqllist2.Add("MATCH(a: GeoProcess),(b: GeoState) WHERE a.Name = '" + tzgp.Name + $"' AND b.Name = 'State84' Merge(a) <-[r: IncludeBy]- (b) RETURN r");
            sqllist2.Add("MATCH(a: GeoProcess),(b: GeoState) WHERE a.Name = '" + tzgp.Name + $"' AND b.Name = 'State85' Merge(a) <-[r: IncludeBy]- (b) RETURN r");
            sqllist2.Add("MATCH(a: GeoProcess),(b: GeoState) WHERE a.Name = '" + tzgp.Name + $"' AND b.Name = 'State86' Merge(a) <-[r: IncludeBy]- (b) RETURN r");
            sqllist2.Add("MATCH(a: GeoProcess),(b: GeoState) WHERE a.Name = '" + tzgp.Name + $"' AND b.Name = 'State87' Merge(a) <-[r: IncludeBy]- (b) RETURN r");

            sqllist2.Add("MATCH(a: GeoEvent),(b: GeoProcess) WHERE a.Name = '" + geTZTQ.Name + "' AND b.Name = '" + tzgp.Name + $"' Merge(a) -[r: detailedby]-> (b) RETURN r");

            foreach (string a in sqllist2)
            {
                nhd.CreateBySQL(a);
            }
            //台风第二次登陆事件

            GeoEvent geTZ2 = new GeoEvent();
            geTZ2.Name = "Lecima Typhoon Loadfall DaLian Event";
            string b2 = $"Merge (n:GeoEvent {{ Name:'Lecima Typhoon Loadfall DaLian Event',Speed:'23 m/s',Level:'9',Time:'2019-8-10 1：45',Location:'Huangdao District, Qingdao City'}})";
            nhd.CreateBySQL(b2);
            string c2 = $" MATCH(a:GeoState ),(b: GeoEvent) WHERE a.Name = '" + "State122" + "' AND b.Name = '" + geTZ2.Name + "' Merge(a) <-[r: registerby] - (b) RETURN r";
            nhd.CreateBySQL(c2);

            //地质灾害模型
            //台风入侵，导致了地质灾害  
            string dzzh = $"Merge (n:Disaster {{ Name:'Typhoon_Disaster',StartTime:'2019-8-10',Duration:'3'}})";
            nhd.CreateBySQL(dzzh);
            string cause = $" MATCH(a:Disaster ),(b: GeoEvent) WHERE a.Name = '" + "Typhoon_Disaster" + "' AND b.Name = '" + geTZTQ.Name + "' Merge(b) -[r: cause] -> (a) RETURN r";
            nhd.CreateBySQL(cause);

            //地质灾害分为许多部分
            List<string> sqllist3 = new List<string>();

            sqllist3.Add($"Merge(n: Disaster {{ Name: 'Agriculture_Loss',The_Area_Affected: '111000 hectare',No_Harvest_Area: '21300 hectare'}})");
            sqllist3.Add($"Merge(n: Disaster {{ Name: 'Business_Loss',Direct_Loss: '￥37.5 billion',The_number_of_enterprises_affected: 1870000}})");
            sqllist3.Add($"Merge(n: Disaster {{ Name: 'Geological_Disaster',The_number_of_debris_flow:5,The_number_of_landslide:25,The_number_pf_collapse:26}})");
            sqllist3.Add($"Merge(n: Disaster {{ Name: 'People',Affected_Population:3723000,Death:32,Number_of_Lost:16}})");
            sqllist3.Add($"Merge(n: Disaster {{ Name: 'Power_Facilities',The_number_of_10kV_line:1684,The_number_of_110kV_substation:27,The_number_of_220kV_substation:3,The_number_of_power_outages:1730000}})");
            sqllist3.Add($"Merge(n: Disaster {{ Name: 'Rainstorm',Place:'TAIZHOU City',precipitation:210}})");
            sqllist3.Add($"Merge(n: Disaster {{ Name: 'Waterlogging',waterlogging_area_WenLin:'20%',Deepest_water_Wenlin:'2 meter',The_deepest_water_Linhai:10, The_deepeset_water_Linhai:2.5}})");
            sqllist3.Add($"Merge(n: Disaster {{ Name: 'Social_Activities',Collapsed_House:4107,Seriously_Damage_the_House:9154}})");
            sqllist3.Add($"Merge(n: Disaster {{ Name: 'Road_damage',Affecting_traffic_projects:53,Damage_Highway:11,Rural_road:670,Rural_Bridge:7}})");
            sqllist3.Add($"Merge(n: Disaster {{ Name: 'Water_Conservancy_Project',Damaged_embankment:524,Damaged_embankment_range:'189km',Damaged_reservoir:29,Dike_breach:15,Dike_breach_range:'1.2km',Direct_economic_loss:1.368800000}})");

            foreach (string a in sqllist3)
            {
                nhd.CreateBySQL(a);
            }
            //之间的关系
            // include

            List<string> sqllist4 = new List<string>();
            sqllist4.Add($" MATCH(a:Disaster ),(b: Disaster) WHERE a.Name = '" + "Typhoon_Disaster" + "' AND b.Name = '" + "Agriculture_Loss" + "' Merge(a) -[r: is_part_of] -> (b) RETURN r");
            sqllist4.Add($" MATCH(a:Disaster ),(b: Disaster) WHERE a.Name = '" + "Typhoon_Disaster" + "' AND b.Name = '" + "Business_Loss" + "' Merge(a) -[r: is_part_of] -> (b) RETURN r");
            sqllist4.Add($" MATCH(a:Disaster ),(b: Disaster) WHERE a.Name = '" + "Typhoon_Disaster" + "' AND b.Name = '" + "Geological_Disaster" + "' Merge(a) -[r: is_part_of] -> (b) RETURN r");
            sqllist4.Add($" MATCH(a:Disaster ),(b: Disaster) WHERE a.Name = '" + "Typhoon_Disaster" + "' AND b.Name = '" + "People" + "' Merge(a) -[r: is_part_of] -> (b) RETURN r");
            sqllist4.Add($" MATCH(a:Disaster ),(b: Disaster) WHERE a.Name = '" + "Typhoon_Disaster" + "' AND b.Name = '" + "Power_Facilities" + "' Merge(a) -[r: is_part_of] -> (b) RETURN r");
            sqllist4.Add($" MATCH(a:Disaster ),(b: Disaster) WHERE a.Name = '" + "Typhoon_Disaster" + "' AND b.Name = '" + "Rainstorm" + "' Merge(a) -[r: is_part_of] -> (b) RETURN r");
            sqllist4.Add($" MATCH(a:Disaster ),(b: Disaster) WHERE a.Name = '" + "Typhoon_Disaster" + "' AND b.Name = '" + "Waterlogging" + "' Merge(a) -[r: is_part_of] -> (b) RETURN r");
            sqllist4.Add($" MATCH(a:Disaster ),(b: Disaster) WHERE a.Name = '" + "Typhoon_Disaster" + "' AND b.Name = '" + "Social_Activities" + "' Merge(a) -[r: is_part_of] -> (b) RETURN r");
            sqllist4.Add($" MATCH(a:Disaster ),(b: Disaster) WHERE a.Name = '" + "Typhoon_Disaster" + "' AND b.Name = '" + "Road_damage" + "' Merge(a) -[r: is_part_of] -> (b) RETURN r");
            sqllist4.Add($" MATCH(a:Disaster ),(b: Disaster) WHERE a.Name = '" + "Typhoon_Disaster" + "' AND b.Name = '" + "Water_Conservancy_Project" + "' Merge(a) -[r: is_part_of] -> (b) RETURN r");
            sqllist4.Add($" MATCH(a:Disaster ),(b: Disaster) WHERE a.Name = '" + "Rainstorm" + "' AND b.Name = '" + "Waterlogging" + "' Merge(a) -[r: cause] -> (b) RETURN r");

            foreach (string a in sqllist4)
            {
                nhd.CreateBySQL(a);
            }
            string sql_e = "MATCH (Big:GeoProcess { Name: 'Lekima Typhoon Process' }),(Sml:GeoProcess { Name: 'Lecima Typhoon Invade Taizhou' }) MERGE(Big) < -[r: IncludeBy] - (Sml)";
            nhd.CreateBySQL(sql_e);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Stream mystream;
            OpenFileDialog openfiledialog1 = new OpenFileDialog();
            openfiledialog1.Multiselect = false;//允许同时选择多个文件 
            openfiledialog1.InitialDirectory = "c:\\";
            openfiledialog1.Filter = "txt files(*.csv)|*.*";
            openfiledialog1.FilterIndex = 2;
            openfiledialog1.RestoreDirectory = true;
            if (openfiledialog1.ShowDialog() == DialogResult.OK)
            {
                if ((mystream = openfiledialog1.OpenFile()) != null)
                {
                    this.textBox1.Text = "";
                    this.textBox1.Text += openfiledialog1.FileName.ToString();
                    mystream.Close();
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Stream mystream;
            OpenFileDialog openfiledialog1 = new OpenFileDialog();
            openfiledialog1.Multiselect = false;//允许同时选择多个文件 
            openfiledialog1.InitialDirectory = "c:\\";
            openfiledialog1.Filter = "txt files(*.csv)|*.*";

            openfiledialog1.FilterIndex = 2;
            openfiledialog1.RestoreDirectory = true;
            if (openfiledialog1.ShowDialog() == DialogResult.OK)
            {
                if ((mystream = openfiledialog1.OpenFile()) != null)
                {
                    this.textBox2.Text = "";
                    this.textBox2.Text += openfiledialog1.FileName.ToString();
                    mystream.Close();
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Stream mystream;
            OpenFileDialog openfiledialog1 = new OpenFileDialog();
            openfiledialog1.Multiselect = false;//允许同时选择多个文件 
            openfiledialog1.InitialDirectory = "c:\\";
            openfiledialog1.Filter = "txt files(*.csv)|*.*";
            openfiledialog1.FilterIndex = 2;
            openfiledialog1.RestoreDirectory = true;
            if (openfiledialog1.ShowDialog() == DialogResult.OK)
            {
                if ((mystream = openfiledialog1.OpenFile()) != null)
                {
                    this.textBox3.Text = "";
                    this.textBox3.Text += openfiledialog1.FileName.ToString();
                    mystream.Close();
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            CreateTyphoonGraphDb();
        }
    }
}
