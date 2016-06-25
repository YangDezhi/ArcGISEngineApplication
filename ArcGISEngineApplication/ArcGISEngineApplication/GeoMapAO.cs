using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesFile;
using System.Diagnostics;
using System.Windows.Forms;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.esriSystem;
using System.Data;

namespace ArcGISEngineApplication
{
    //定义设置控件的接口
    interface IComControl
    {
        //主视图控件
        AxMapControl AxMapControl1 { get; set; }
        //鹰眼视图控件
        AxMapControl AxMapControl2 { get; set; }
        //版面视图控件
        AxPageLayoutControl AxPageLayoutControl1 { get; set; }
        //定义设置颜色的方法
        IRgbColor GetRGB(int r, int g, int b);
    }

    //定义管理地理数据加载的接口
    interface IAddGeoData : IComControl
    {
        //存放用户选择的地理数据文件
        string[] StrFileName { get; set; }
        //加载地理数据的方法
        void AddGeoMap();

        void AddTinDataset();
    }

    //定义管理地图操作的接口
    interface IGeoDataOper : IComControl
    {
        //地图操作的类型
        string StrOperType { get; set; }
        //鼠标按下事件的参数
        IMapControlEvents2_OnMouseDownEvent E { get; set; }
        //实现地图交互操作的方法
        void OperMap();

        void OperateMapDoc();
    }

    /**
     * 鹰眼
     */
    interface IEagOpt : IComControl
    {
        //处理鼠标移动的事件参数
        IMapControlEvents2_OnMouseMoveEvent MouseMoveEvent { get; set; }
        IMapControlEvents2_OnMouseDownEvent MouseDownEvent { get; set; }
        IMapControlEvents2_OnExtentUpdatedEvent ExtentUpdatedEvent { get; set; }
        void NewGeoMap();//使主视图和鹰眼同步的方法
        void MoveEag();//移动鹰眼视图中的红色矩形框
        void MouseMov();//处理鹰眼视图中鼠标的移动
        void DrawRec();//绘制红色矩形框的过程
    }

    /**
     * PageLayout与MapControl联动
     */
    interface IMapCooper : IComControl
    {
        string StrMxdFile { get; set; }
        void CopyAndWriteMap();//拷贝并复制地图
        void repGeoMap();//替换新的地图
        void AddGeoDoc();//向PagelayoutControl控件加载地图文档文件
    }

    /**
     * TOCControl控件查看图层的属性表
     */
    interface ILaySequAttr : IComControl
    {
        AxTOCControl AxTOCControl { get; set; }
        ITOCControl MTOCControl { get; set; }
        ITOCControlEvents_OnMouseDownEvent TocMDE { get; set;}
        ITOCControlEvents_OnMouseUpEvent TocMUE { get; set;}

        ILayer PLayer { get; set; }
        string TableName { get; set; }
        string FCname { get; set; }
        void AdjLayMouseDownEvent();//处理鼠标按下时的事件
        void AdjLayMouseUpEvent();//处理鼠标弹起时的事件
        DataTable CreateDataTable();//打开属性表
    }

    class GeoMapAO : IAddGeoData, IGeoDataOper, IEagOpt, IMapCooper, ILaySequAttr
    {
        //实现设置控件的接口
        AxMapControl axMapControl1;
        public AxMapControl AxMapControl1
        {
            get
            {
                return axMapControl1;
            }
            set
            {
                axMapControl1 = value;
            }
        }

        AxMapControl axMapControl2;
        public AxMapControl AxMapControl2
        {
            get
            {
                return axMapControl2;
            }
            set
            {
                axMapControl2 = value;
            }
        }

        AxPageLayoutControl axPageLayoutControl1;
        public AxPageLayoutControl AxPageLayoutControl1
        {
            get
            {
                return axPageLayoutControl1;
            }
            set
            {
                axPageLayoutControl1 = value;
            }
        }

        public IRgbColor GetRGB(int r, int g, int b)
        {
            IRgbColor pColor = new RgbColorClass();
            pColor.Red = r;
            pColor.Green = g;
            pColor.Blue = b;
            return pColor;
        }

        //定义管理地理数据加载的接口
        string[] strFileName;
        public string[] StrFileName
        {
            get
            {
                return strFileName;
            }
            set
            {
                strFileName = value;
            }
        }

        public void AddGeoMap()
        {
            axMapControl1.Map.ClearSelection();
            axMapControl1.ActiveView.Refresh();
            for (int i = 0; i < strFileName.Length; ++i)
            {
                string strExt = System.IO.Path.GetExtension(strFileName[i]);
                string strPath = System.IO.Path.GetDirectoryName(strFileName[i]);
                string strFile = System.IO.Path.GetFileNameWithoutExtension(strFileName[i]);
                string strFilePath = System.IO.Path.GetFileName(strFileName[i]);
                //判断文件类型，然后采用不同的方式加载文件
                switch(strExt)
                {
                    case ".shp":
                        loadGeoMapOfVector(axMapControl1, strPath, strFile);
                        axMapControl2.ClearLayers();
                        loadGeoMapOfVector(axMapControl2, strPath, strFile);
                        //axMapControl1.AddShapeFile(strPath, strFile);
                        //axMapControl2.AddShapeFile(strPath, strFile);
                        axMapControl2.Extent = axMapControl2.FullExtent;
                        break;
                    case ".mxd":
                        loadGeoMapOfMXD(axMapControl1, strPath, strFilePath);
                        break;
                    case ".bmp":
                    case ".jpg":
                    case ".img":
                    case ".tif":
                        ILayer pRasterLayer = loadGeoMapOfRaster(axMapControl1, strPath, strFilePath);
                        axMapControl2.ClearLayers();
                        axMapControl2.AddLayer(pRasterLayer, 0);
                        axMapControl2.Extent = axMapControl2.FullExtent;
                        break;
                    case ".dwg":
                        AddCADFeature(axMapControl1, axMapControl2, strPath, strFilePath);
                        break;
                    default:
                        break;
                }
                axMapControl1.Map.ClearSelection();
                axMapControl1.ActiveView.Refresh();
            }
        }

        /**
         * 加载矢量数据
         */
        private void loadGeoMapOfVector(AxMapControl axMapControl, string strPath, string strFile)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            //axMapControl.AddShapeFile(strPath, strFile);

            IWorkspaceFactory pWorkspaceFactory = new ShapefileWorkspaceFactoryClass();
            IWorkspace pWorkspace = pWorkspaceFactory.OpenFromFile(strPath, 0);
            IFeatureWorkspace pFeatureWorkspace = pWorkspace as IFeatureWorkspace;
            IFeatureClass pFeatureClass = pFeatureWorkspace.OpenFeatureClass(strFile);
            IDataset pDataset = pFeatureClass as IDataset;
            IFeatureLayer pFeatureLayer = new FeatureLayer();
            pFeatureLayer.FeatureClass = pFeatureClass;
            pFeatureLayer.Name = pDataset.Name;
            ILayer pLayer = pFeatureLayer as ILayer;
            axMapControl.Map.AddLayer(pLayer);

            stopwatch.Stop();
            Console.WriteLine(stopwatch);
        }

        /**
         * 加载MXD文档
         */
        private void loadGeoMapOfMXD(AxMapControl axMapControl, string strPath, string strFilePath)
        {
            strFilePath = strPath + "\\" + strFilePath;
            if (axMapControl.CheckMxFile(strFilePath))
            {
                //axMapControl.MousePointer = esriControlsMousePointer.esriPointerHourglass;
                //axMapControl.LoadMxFile(strFilePath, 0, Type.Missing);
                //axMapControl.MousePointer = esriControlsMousePointer.esriPointerDefault;

                IMapDocument pMapDocument = new MapDocumentClass();
                pMapDocument.Open(strFilePath, "");
                for (int i = 0; i < pMapDocument.MapCount; ++i)
                {
                    axMapControl.Map = pMapDocument.get_Map(i);
                }
                axMapControl.Refresh();
                if (pMapDocument.get_IsReadOnly(pMapDocument.DocumentFilename) == true)
                {
                    MessageBox.Show("此地图文档为只读文本", "信息提示");
                    return;
                }
                //pMapDocument.Save(pMapDocument.UsesRelativePaths, true);
                //MessageBox.Show("保存成功!", "信息提示");

                //在PageLayoutControl操作MXD文件
                axPageLayoutControl1.PageLayout = pMapDocument.PageLayout;
                axPageLayoutControl1.Refresh();
            }
            else
            {
                MessageBox.Show("所选文件不是地图文档文件" + strFilePath, "信息提示");
            }
        }

        /**
         * 加载栅格数据
         */
        private ILayer loadGeoMapOfRaster(AxMapControl axMapControl, string strPath, string strFile)
        {
            IWorkspaceFactory pWSF = new RasterWorkspaceFactoryClass();
            IWorkspace pWS = pWSF.OpenFromFile(strPath, 0);
            IRasterWorkspace pRWS = pWS as IRasterWorkspace;
            IRasterDataset pRD = pRWS.OpenRasterDataset(strFile);
            IRasterPyramid pRP = pRD as IRasterPyramid;
            if (pRP != null && ! pRP.Present)
            {
                pRP.Create();
            }
            IRaster pRaster = pRD.CreateDefaultRaster();
            IRasterLayer pRasterLayer = new RasterLayerClass();
            pRasterLayer.CreateFromRaster(pRaster);
            ILayer pLayer = pRasterLayer as ILayer;
            axMapControl.AddLayer(pLayer, 0);
            return pLayer;
        }

        /**
         * 添加CAD数据
         */
        private void AddCADFeature(AxMapControl axMapControl1, AxMapControl axMapControl2, string strPath, string strCAD) 
        {
            IWorkspaceFactory pWSF = new CadWorkspaceFactoryClass();
            IWorkspace pWS = pWSF.OpenFromFile(strPath, 0);
            IFeatureWorkspace pFWS = pWS as IFeatureWorkspace;
            IFeatureDataset pFD = pFWS.OpenFeatureDataset(strCAD);
            IFeatureClassContainer pFCC = pFD as IFeatureClassContainer;
            IFeatureClass pFC = null;
            IFeatureLayer pFL = null;
            for (int i = 0; i < pFCC.ClassCount - 1; ++i)
            {
                pFC = pFCC.get_Class(i);
                if (pFC.FeatureType == esriFeatureType.esriFTCoverageAnnotation)
                {
                    pFL = new CadAnnotationLayerClass();
                }
                else 
                {
                    pFL = new FeatureLayerClass();
                }
                pFL.Name = pFC.AliasName;
                pFL.FeatureClass = pFC;
                axMapControl1.AddLayer(pFL, 0);
                axMapControl2.ClearLayers();
                axMapControl2.AddLayer(pFL, 0);
                axMapControl2.Extent = axMapControl2.FullExtent;
            }
        }

        string strOperType;
        public string StrOperType
        {
            get
            {
                return strOperType;
            }
            set
            {
                strOperType = value;
            }
        }

        IMapControlEvents2_OnMouseDownEvent e;
        public IMapControlEvents2_OnMouseDownEvent E
        {
            get
            {
                return e;
            }
            set
            {
                e = value;
            }
        }

        /**
         * 操作地图
         */
        public void OperMap()
        {
            IMap pMap = axMapControl1.Map;
            IActiveView pActiveView = pMap as IActiveView;
            IEnvelope pEnvelope;
            switch (strOperType)
            {
                case "LKZoomIn":
                case "ZoomInLK"://拉框缩放
                    //axMapControl1.Extent = axMapControl1.TrackRectangle();
                    //axMapControl1.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
                    pEnvelope = axMapControl1.TrackRectangle();
                    pActiveView.Extent = pEnvelope;
                    pActiveView.Refresh();
                    break;
                case "GeoMapLkShow"://拉框显示
                    axMapControl1.MousePointer = esriControlsMousePointer.esriPointerCrosshair;
                    axMapControl1.Extent = axMapControl1.TrackRectangle();
                    axMapControl1.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
                    break;
                case "MoveMap"://移动地图
                    axMapControl1.Pan();
                    break;
                case "DrawPoint"://绘制点
                    break;
                case "DrawLine"://绘制线
                    break;
                case "DrawPolygon"://绘制面
                    break;
                case "LabelMap"://地图标注
                    break;
                case "SelectMap"://数据选择
                    break;
                default:
                    break;
            }
        }


        public void AddTinDataset()
        {
            
        }


        public void OperateMapDoc()
        {
            
        }

        IMapControlEvents2_OnMouseMoveEvent mouseMoveEvent;
        public IMapControlEvents2_OnMouseMoveEvent MouseMoveEvent
        {
            get
            {
                return mouseMoveEvent;
            }
            set
            {
                mouseMoveEvent = value;
            }
        }

        IMapControlEvents2_OnMouseDownEvent mouseDownEvent;
        public IMapControlEvents2_OnMouseDownEvent MouseDownEvent
        {
            get
            {
                return mouseDownEvent;
            }
            set
            {
                mouseDownEvent = value;
            }
        }

        IMapControlEvents2_OnExtentUpdatedEvent extentUpdatedEvent;
        public IMapControlEvents2_OnExtentUpdatedEvent ExtentUpdatedEvent
        {
            get
            {
                return extentUpdatedEvent;
            }
            set
            {
                extentUpdatedEvent = value;
            }
        }

        public void NewGeoMap()
        {
            IMap pMap = axMapControl1.Map;
            for (int i = 0; i < pMap.LayerCount; ++i)
            {
                axMapControl2.Map.AddLayer(pMap.get_Layer(i));
            }
            axMapControl2.Extent = axMapControl2.FullExtent;
        }

        public void MoveEag()
        {
            IEnvelope pEnvelope = null;
            switch (mouseDownEvent.button)
            {
                case 1://鼠标左键
                    IPoint pPt = new PointClass();
                    pPt.X = mouseDownEvent.mapX;
                    pPt.Y = mouseDownEvent.mapY;
                    pEnvelope = axMapControl1.Extent as IEnvelope;
                    pEnvelope.CenterAt(pPt);
                    break;
                case 2://鼠标右键
                    pEnvelope = axMapControl1.TrackRectangle();
                    break;
            }
            if(pEnvelope != null)
            {
                axMapControl1.Extent = pEnvelope;
                //刷新所有图层
                axMapControl1.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
            }
        }

        public void MouseMov()
        {
            if (mouseMoveEvent.button != 1)//鼠标左键
            {
                return;
            }
            IPoint pPt = new PointClass();
            pPt.X = mouseMoveEvent.mapX;
            pPt.Y = mouseMoveEvent.mapY;
            axMapControl1.CenterAt(pPt);
            axMapControl2.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
        }

        public void DrawRec()
        {
            IGraphicsContainer pGC = axMapControl2.Map as IGraphicsContainer;
            IActiveView pAV = pGC as IActiveView;
            pGC.DeleteAllElements();
            IRectangleElement pRE = new RectangleElementClass();
            IElement pEle = pRE as IElement;
            IEnvelope pEnv = extentUpdatedEvent.newEnvelope as IEnvelope;
            pEle.Geometry = pEnv;

            IRgbColor pColor = new RgbColorClass();
            pColor = GetRGB(200, 0, 0);
            pColor.Transparency = 255;
            ILineSymbol pLS = new SimpleLineSymbolClass();
            pLS.Width = 2;
            pLS.Color = pColor;
            IFillSymbol pFS = new SimpleFillSymbolClass();
            pColor.Transparency = 0;
            pFS.Color = pColor;
            pFS.Outline = pLS;
            IFillShapeElement pFSE = pRE as IFillShapeElement;
            pFSE.Symbol = pFS;
            pGC.AddElement(pEle, 0);
            axMapControl2.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
        }

        string strMxdFile;
        public string StrMxdFile
        {
            get
            {
                return strMxdFile;
            }
            set
            {
                strMxdFile =  value;
            }
        }

        public void CopyAndWriteMap()
        {
            IObjectCopy objectCopy = new ObjectCopyClass();
            object toCopyMap = axMapControl1.Map;
            object copiedMap = objectCopy.Copy(toCopyMap);

            //获取视图控件的焦点地图
            object toOverwiteMap = axPageLayoutControl1.ActiveView.FocusMap;
            //复制地图
            objectCopy.Overwrite(copiedMap, ref toOverwiteMap);
        }

        public void repGeoMap()
        {
            IActiveView pActiveView = axPageLayoutControl1.ActiveView.FocusMap as IActiveView;
            IDisplayTransformation displayTransformation = pActiveView.ScreenDisplay.DisplayTransformation;
            //设置焦点地图的可视范围
            displayTransformation.VisibleBounds = axMapControl1.Extent;
            axPageLayoutControl1.ActiveView.Refresh();
            CopyAndWriteMap();
        }

        public void AddGeoDoc()
        {
            IMapDocument pMapDocument = new MapDocumentClass();
            pMapDocument.Open(strMxdFile, "");
            axPageLayoutControl1.PageLayout = pMapDocument.PageLayout;
            axPageLayoutControl1.Refresh();
        }

        /**
         * ***************TOCControl控件查看图层的属性表***************
         */
        ILayer pMovelayer;
        int toIndex;
        AxTOCControl axTOCControl1;
        public AxTOCControl AxTOCControl
        {
            get
            {
                return axTOCControl1;
            }
            set
            {
                axTOCControl1 = value;
            }
        }

        ITOCControl mTOCControl;
        public ITOCControl MTOCControl
        {
            get
            {
                return mTOCControl;
            }
            set
            {
                mTOCControl = value;
            }
        }

        ITOCControlEvents_OnMouseDownEvent tocMDE;
        public ITOCControlEvents_OnMouseDownEvent TocMDE
        {
            get
            {
                return tocMDE;
            }
            set
            {
                tocMDE = value;
            }
        }

        ITOCControlEvents_OnMouseUpEvent tocMUE;
        public ITOCControlEvents_OnMouseUpEvent TocMUE
        {
            get
            {
                return tocMUE;
            }
            set
            {
                tocMUE = value;
            }
        }

        ILayer pLayer;
        public ILayer PLayer
        {
            get
            {
                return pLayer;
            }
            set
            {
                pLayer = value;
            }
        }

        string tableName;
        public string TableName
        {
            get
            {
                return tableName;
            }
            set
            {
                tableName = value;
            }
        }

        string fCname;
        public string FCname
        {
            get
            {
                return fCname;
            }
            set
            {
                fCname = value;
            }
        }

        public void AdjLayMouseDownEvent()
        {
            esriTOCControlItem item; 
            IBasicMap map = null;
            ILayer layer = null;
            object other = null;
            object index = null;
            if (tocMDE.button == 1)
            {
                item = esriTOCControlItem.esriTOCControlItemNone;
                mTOCControl.HitTest(tocMDE.x, tocMDE.y, ref item, ref map, ref layer, ref other, ref index);
                if (item == esriTOCControlItem.esriTOCControlItemLayer)
                {
                    if(layer is IAnnotationSublayer)
                    {
                        return;
                    } 
                    else
                    {
                        pMovelayer = layer;
                    }
                }
            }
            else if (tocMDE.button == 2)
            {
                if (axMapControl1.LayerCount > 0)//主视图中有地理数据
                {
                    item = new esriTOCControlItem();
                    map = new MapClass();
                    layer = new FeatureLayerClass();
                    other = new object();
                    index = new object();
                    axTOCControl1.HitTest(tocMDE.x, tocMDE.y, ref item, ref map, ref layer, ref other, ref index);
                    //显示所选图层的属性表
                    ILaySequAttr GeoDataTable = new GeoMapAO();
                    GeoDataTable.PLayer = layer;
                    GeoDataTable.FCname = layer.Name;
                    DataTable pTable = new DataTable();
                    pTable = GeoDataTable.CreateDataTable();
                    GeoMapAttributeForm frmTable = new GeoMapAttributeForm();//显示属性的窗体
                    frmTable.Show();
                    frmTable.dataGridView1.DataSource = pTable;
                }
            }
        }

        public void AdjLayMouseUpEvent()
        {
            if (mTOCControl == null)
            {
                return;
            }
            if (tocMUE.button == 1)
            {
                esriTOCControlItem item = esriTOCControlItem.esriTOCControlItemNone;
                IBasicMap map = null;
                ILayer layer = null;
                object other = null;
                object index = null;
                mTOCControl.HitTest(tocMUE.x, tocMUE.y, ref item, ref map, ref layer, ref other, ref index);
                IMap pMap = axMapControl1.ActiveView.FocusMap;
                if (item == esriTOCControlItem.esriTOCControlItemLayer || layer != null)
                {
                    if (pMovelayer != layer)
                    {
                        ILayer pTempLayer;
                        for (int i = 0; i < pMap.LayerCount; ++i)
                        {
                            pTempLayer = pMap.get_Layer(i);
                            if (pTempLayer == layer)//获取鼠标点击位置的图层索引号
                            {
                                toIndex = i;
                            }
                        }
                        pMap.MoveLayer(pMovelayer, toIndex);//移动源图层到目标图层位置
                        axMapControl1.ActiveView.Refresh();
                        mTOCControl.Update();
                    }
                }
            }
        }

        public DataTable CreateDataTable()
        {
            DataTable pDataTable = CreateDataTableByLayer();
            pDataTable.TableName = pLayer.Name;
            string shapeType = getShapeType();
            DataRow pDataRow = null;
            ITable pTable = pLayer as ITable;
            ICursor pCursor = pTable.Search(null, false);
            IRow pRow = pCursor.NextRow();
            for(int n = 0; pRow != null; ++n)
            {
                pDataRow = pDataTable.NewRow();
                for (int i = 0; i < pRow.Fields.FieldCount; ++i)
                {
                    switch(pRow.Fields.get_Field(i).Type)
                    {
                        case esriFieldType.esriFieldTypeGeometry:
                            pDataRow[i] = shapeType;
                            break;
                        case esriFieldType.esriFieldTypeBlob:
                            pDataRow[i] = "Element";
                            break;
                        default:
                            pDataRow[i] = pRow.get_Value(i);
                            break;
                    }
                }
                pDataTable.Rows.Add(pDataRow);
                pDataRow = null;
                pRow = pCursor.NextRow();
            }
            return pDataTable;
        }

        private DataTable CreateDataTableByLayer()
        {
            DataTable pDataTable = new DataTable(tableName);
            ITable pTable = pLayer as ITable;
            IField pField = null;
            DataColumn pDataColumn = null;
            for (int i = 0; i < pTable.Fields.FieldCount; ++i)
            {
                pField = pTable.Fields.get_Field(i);
                pDataColumn = new DataColumn(pField.Name);
                if(pField.Name == pTable.OIDFieldName)
                {
                    pDataColumn.Unique = true;
                }
                pDataColumn.AllowDBNull = pField.IsNullable;
                pDataColumn.Caption = pField.AliasName;
                pDataColumn.DataType = System.Type.GetType(ParseFieldType(pField.Type));
                pDataColumn.DefaultValue = pField.DefaultValue;
                if(pField.VarType == 8)
                {
                    pDataColumn.MaxLength = pField.Length;
                }
                pDataTable.Columns.Add(pDataColumn);
                pField = null;
                pDataColumn = null;
            }
            return pDataTable;
        }

        //获得矢量的类型
        private string getShapeType()
        {
            IFeatureLayer pFtuLayer = pLayer as IFeatureLayer;
            switch(pFtuLayer.FeatureClass.ShapeType)
            {
                case esriGeometryType.esriGeometryPoint:
                    return "Point";
                case esriGeometryType.esriGeometryPolyline:
                    return "Polyline";
                case esriGeometryType.esriGeometryPolygon:
                    return "Polygon";
                default:
                    return "";
            }
        }

        private string ParseFieldType(esriFieldType fieldType)
        {
            switch(fieldType)
            {
                case esriFieldType.esriFieldTypeBlob:
                    return "System.String";
                case esriFieldType.esriFieldTypeDate:
                    return "System.DataTime";
                case esriFieldType.esriFieldTypeDouble:
                    return "System.Double";
                case esriFieldType.esriFieldTypeGeometry:
                    return "System.String";
                case esriFieldType.esriFieldTypeGlobalID:
                    return "System.String";
                case esriFieldType.esriFieldTypeGUID:
                    return "System.String";
                case esriFieldType.esriFieldTypeInteger:
                    return "System.Int32";
                case esriFieldType.esriFieldTypeOID:
                    return "System.String";
                case esriFieldType.esriFieldTypeRaster:
                    return "System.String";
                case esriFieldType.esriFieldTypeSingle:
                    return "System.Single";
                case esriFieldType.esriFieldTypeSmallInteger:
                    return "System.Int32";
                case esriFieldType.esriFieldTypeString:
                    return "System.String";
                default:
                    return "System.String";
            }
        }

        private string getValidFeatureClassName()
        {
            int dot = fCname.IndexOf(".");
            if (dot != -1)
            {
                return fCname.Replace(".", "_");
            }
            return fCname;
        }

        private DataTable CreateAttributeTable()
        {
            DataTable attributeTable = new DataTable();
            string tableName = getValidFeatureClassName();
            attributeTable = CreateDataTable();
            return attributeTable;
        }
    }
}
