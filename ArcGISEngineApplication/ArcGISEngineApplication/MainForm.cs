using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.GlobeCore;
using ESRI.ArcGIS.Output;
using ESRI.ArcGIS.SystemUI;

namespace ArcGISEngineApplication
{
    public partial class MainForm : Form
    {
        bool strUnion = true;//PageLayoutControl与MapControl是否联动
        public MainForm()
        {
            ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.EngineOrDesktop);
            InitializeComponent();
        }

        string GeoOpType = string.Empty;

        private void Form1_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
            axTOCControl1.SetBuddyControl(axMapControl1);
            axToolbarControl1.SetBuddyControl(axMapControl1);

            //axToolbarControl1.AddItem(new ClearCurrentActiveToolCmd(), -1, -1, false, 0, esriCommandStyles.esriCommandStyleIconOnly);
        }

        private void 加载矢量ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //OpenFileDialog OpenDig = new OpenFileDialog();
            //OpenDig.Title = "请选择地理数据文件";
            //OpenDig.Filter = "矢量数据文件(*.shp)|*.shp";
            //OpenDig.Multiselect = true;
            //OpenDig.ShowDialog();
            //string[] strFileName = OpenDig.FileNames;
            //if (strFileName.Length > 0)
            //{
            //    IAddGeoData pAddGeoData = new GeoMapAO();
            //    pAddGeoData.StrFileName = strFileName;
            //    //设置伙伴控件关系
            //    pAddGeoData.AxMapControl1 = axMapControl1;
            //    pAddGeoData.AxMapControl2 = axMapControl2;
            //    pAddGeoData.AddGeoMap();
            //}
            string title = "请选择矢量数据文件";
            string filter = "矢量数据文件(*.shp)|*.shp";
            OpenSelectFileDialog(title, filter);
        }

        private void axMapControl1_OnMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            if (GeoOpType == string.Empty)
            {
                return;
            }
            IGeoDataOper pGeoMapOp = new GeoMapAO();
            pGeoMapOp.StrOperType = GeoOpType;
            pGeoMapOp.AxMapControl1 = axMapControl1;
            pGeoMapOp.AxMapControl2 = axMapControl2;
            pGeoMapOp.E = e;
            pGeoMapOp.OperMap();

            IEagOpt pEagOpt = new GeoMapAO();
            pEagOpt.AxMapControl1 = axMapControl1;
            pEagOpt.AxMapControl2 = axMapControl2;
            pEagOpt.MouseDownEvent = e;
            pEagOpt.DrawRec();
        }

        private void axMapControl1_OnExtentUpdated(object sender, IMapControlEvents2_OnExtentUpdatedEvent e)
        {
            IEagOpt pEagOpt = new GeoMapAO();
            pEagOpt.AxMapControl1 = axMapControl1;
            pEagOpt.AxMapControl2 = axMapControl2;
            pEagOpt.ExtentUpdatedEvent = e;
            pEagOpt.DrawRec();
        }

        private void axMapControl1_OnMapReplaced(object sender, IMapControlEvents2_OnMapReplacedEvent e)
        {
            IEagOpt pEagOpt = new GeoMapAO();
            pEagOpt.AxMapControl1 = axMapControl1;
            pEagOpt.AxMapControl2 = axMapControl2;
            pEagOpt.NewGeoMap();
        }

        private void 加载栅格ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string title = "请选择栅格数据文件";
            string filter = "栅格数据文件(*.tif)|*.tif";
            OpenSelectFileDialog(title, filter);
        }

        private void 打开文档ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string title = "请选择MXD文档";
            string filter = "ArcGIS文档(*.mxd)|*.mxd";
            OpenSelectFileDialog(title, filter);
        }

        /**
         *  打开选择文件的对话框
         */
        private void OpenSelectFileDialog(string title, string filter)
        {
            OpenFileDialog OpenDig = new OpenFileDialog();
            OpenDig.Title = title;
            OpenDig.Filter = filter;
            OpenDig.Multiselect = true;
            OpenDig.ShowDialog();
            string[] strFileName = OpenDig.FileNames;
            if (strFileName.Length > 0)
            {
                IAddGeoData pAddGeoData = new GeoMapAO();
                pAddGeoData.StrFileName = strFileName;
                //设置伙伴控件关系
                pAddGeoData.AxMapControl1 = axMapControl1;
                pAddGeoData.AxMapControl2 = axMapControl2;
                pAddGeoData.AxPageLayoutControl1 = axPageLayoutControl1;
                pAddGeoData.AddGeoMap();//向PageLayoutControl控件加载地图文档文件
            }
        }

        private void 关闭ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void axMapControl1_OnAfterScreenDraw(object sender, IMapControlEvents2_OnAfterScreenDrawEvent e)
        {
            if(strUnion ==  false)
            {
                return;
            }
            IMapCooper pMapCooper = new GeoMapAO();
            pMapCooper.AxPageLayoutControl1 = axPageLayoutControl1;
            pMapCooper.AxMapControl1 = axMapControl1;
            pMapCooper.repGeoMap();//替换新的地图
        }

        private void axMapControl1_OnViewRefreshed(object sender, IMapControlEvents2_OnViewRefreshedEvent e)
        {
            if(strUnion == false)
            {
                return;
            }
            axTOCControl1.Update();
            IMapCooper pMapCooper = new GeoMapAO();
            pMapCooper.AxPageLayoutControl1 = axPageLayoutControl1;
            pMapCooper.AxMapControl1 = axMapControl1;
            pMapCooper.CopyAndWriteMap();//拷贝并复制地图
        }

        ILaySequAttr AdjLay = new GeoMapAO();
        private void axTOCControl1_OnMouseDown(object sender, ITOCControlEvents_OnMouseDownEvent e)
        {
            AdjLay.AxMapControl1 = axMapControl1;
            AdjLay.AxTOCControl = axTOCControl1;
            AdjLay.TocMDE = e;
            AdjLay.MTOCControl = axTOCControl1.Object as ITOCControl;
            AdjLay.AdjLayMouseDownEvent();
        }

        private void axTOCControl1_OnMouseUp(object sender, ITOCControlEvents_OnMouseUpEvent e)
        {
            AdjLay.TocMUE = e;
            AdjLay.AdjLayMouseUpEvent();
        }
    }
}
