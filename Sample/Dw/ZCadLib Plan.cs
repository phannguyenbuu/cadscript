using System.Drawing;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Internal;
using System.Threading.Tasks;

using System.Windows.Forms;
namespace AcadScript
{
    public static class CadLibPlan
    {
        static ObjectIdCollection selIds;
        static string py_script_path = @"D:\html\python\myjwall\appjwall\pyscript\create_data.py";

        static void _initBasepoint()
        {
            pPos[] bb = ACD.DB._getBound(selIds);
            cReadData.html_basepoint = new pPos(bb[0].X, bb[1].Y);
            WriteHtmlCLS.VrayPyContents = new Dictionary<string, List<string>>();

            ObjectIdCollection ___blockIds = selIds.ToList().Where(_id => ACD.DB._isBlock(_id)).ToCollection();

            if (___blockIds.Count > 0)
            {
                cReadData.html_basepoint = ACD.DB._getPoint(___blockIds.ToList().OrderBy(_id =>
                {
                    var __sz = ACD.DB._getBound(_id).Size();
                    return -__sz.X * __sz.Y;
                }).First());

                cReadData.super_key = cReadData.html_basepoint.Content;
                cReadData.html_basepoint.Content = "";
            }

        }

        static string _getRasterImagePath(ObjectId entId)
        {
            string res = null;
            // Bắt đầu giao dịch
            using (Transaction tr = ACD.DB.TransactionManager.StartTransaction())
            {
                if (entId.ObjectClass.DxfName == "IMAGE")
                {
                    RasterImage img = (RasterImage)tr.GetObject(entId, OpenMode.ForRead);

                    if (img != null)
                    {
                        res = img.Path;
                    }
                }
                tr.Commit();

            }

            return res;
        }

        public static void DrawMap()
        {
            //Tìm basepoint trong nhóm blockIds
            //ACD.WR("AP1");
            _initBasepoint();

            //if (cReadData.super_key.st_("plan"))
            //ACD.WR("AP2");
            cReadData.__sc = 1000;
            cReadData._dpsc = 1;
            gDraw2DDCLS._dimension_round = 0.1;
            //}
            //ACD.WR("AP3");
            var CssData = new cReadData(selIds, 0.2);
            var csv = new CSVDataCLS(CssData);

            //Ghi file 3D
            var gMap = new gDraw2DMap(CssData, @"D:\html\alibaba\myalibaba\appalibaba\pyscript\create_data.py");
            //var prams = cReadData.ReadcBuilder3DInfo(selIds);
            gMap.g3D = new cBuilder3D(CssData.PointList);//, prams[0], prams[1]);
            //ACD.WR("AP3");
            ACD.DB.GetEntities(null, EN_SELECT.AC_DXF, "IMAGE");
            imgIds = IR.SelectedIds;

            foreach (ObjectId __id in imgIds)
            {
                pPos[] __bb = ACD.DB._getBound(__id);

                if (ACD.DB._getBound(selIds[0]).Any(__p => __p.InsideRect(__bb[0], __bb[1])))
                {
                    cReadData.map_ref_img_file = _getRasterImagePath(__id);

                    var fname = cReadData.map_ref_img_file.Replace(@"D:\html\", "").Replace("\\", "/");

                    gMap.IMGBasepoint = cReadData.__sc * (new pPos(__bb[0].X, __bb[1].Y) - cReadData.html_basepoint);
                    gMap.IMGPath = Path.GetFileName(cReadData.map_ref_img_file);
                    gMap.AppendImageHtml("i0-xref-image", fname, gMap.IMGBasepoint);//, 0, 0, 0, __sc, __sc);
                }
            }
            ACD.WR("AP4");
            gMap.DrawMap();

            //Ghi file Csv *_csv.csv, lấy dữ liệu đầu vào table_data cho hàm WriteHtml
            string table_data = csv.SaveCSV(gMap.g3D, @"D:\html\"
                + Path.GetFileNameWithoutExtension(ACD.CurrentDWGFileName) + "_" + cReadData.super_key + ".csv");

            if(csv.yaw_pitch_clipboard.Count > 0)
                Clipboard.SetText(csv.yaw_pitch_clipboard.ToTextStr(",\r\n"));

            //Ghi file Html plan_html.html
            WriteHtmlCLS.WriteHtml("map_html", "mapstyle.css", "MAP", CssData, 5000, 5000, table_data);
        }

        public static void DrawPlan()
        {
            ACD.WR("Plan 1");
            //Tìm basepoint trong nhóm blockIds
            _initBasepoint();
                        
            //if (cReadData.super_key.st_("plan"))
            {
                cReadData.__sc = 1000;
                cReadData._dpsc = 1;
                gDraw2DDCLS._dimension_round = 0.1;
            }

            var CssData = new cReadData(selIds, 0.2);
            var csv = new CSVDataCLS(CssData);

            ACD.WR("Plan 1.1 {0}", CssData);

            try
            {
                //Ghi file 3D
                var gArch = new gDraw2DPlan(CssData, py_script_path);
                ACD.WR("Plan 1.2");
                //var prams = cReadData.ReadcBuilder3DInfo(selIds);
                gArch.g3D = new cBuilder3D(CssData.PointList, true); //, prams[0], prams[1]);
                ACD.WR("Plan 1.3");
                gArch.DrawMap();

                ACD.WR("Plan 2");

                //Ghi file Csv *_csv.csv, lấy dữ liệu đầu vào table_data cho hàm WriteHtml
                string table_data = csv.SaveCSV(gArch.g3D, @"D:\html\"
                    + Path.GetFileNameWithoutExtension(ACD.CurrentDWGFileName) + "_cls.csv");

                //Ghi file Html plan_html.html
                WriteHtmlCLS.WriteHtml("plan_html", "planstyle.css", "PLAN", CssData, 4000, 4000, table_data);
            }catch(SystemException ex)
            {
                ACD.WR("{0},{1}", ex.Message, ex.StackTrace);
            }
        }

        static void DrawArch()
        {
            _initBasepoint();

            //cReadData.super_key = "arch_" + cReadData.super_key;
            cReadData.__sc = 1;
            cReadData._dpsc = 0.01;

            
            //gDraw2DDCLS._dimension_txt_offset = 50;
            gDraw2DDCLS._dimension_round = 50;
            cReadData._dimension_per_metre = false;

            var CssData = new cReadData(selIds);

            foreach (ObjectId _id in selIds._filterDXF("MTEXT", "TEXT"))
            {
                string __stname = ACD.DB._getContent(_id).Replace("%%U","");

                if (__stname.st_("MẶT BẰNG") || __stname.st_("MB "))
                {
                    string __type = "arch_";

                    if (__stname.ct_("KẾT CẤU"))
                        __type =  "struct";
                    else if (__stname.ct_("ĐIỆN"))
                        __type = "power";
                    else if (__stname.ct_("NƯỚC"))
                        __type = "water";

                    cReadData.super_key = __type  + "_" + cReadData.super_key + "_#" + __stname;
                }
            }

            var gArch = new gDraw2DArch(CssData, @"D:\html\python\myjwall\appjwall\pyscript\create_data.py");
            gArch.DimObj.dimension_architecture_tick = true;
            gArch.DrawArch();

            pPos sz = CssData.PointList.Boundary.Size();

            //Ghi file Html plan_html.html
            WriteHtmlCLS.WriteHtml("arch_html", "archstyle.css", "ARCH", CssData, (int)sz.X, (int)sz.Y);
        }

        static void DrawPlate()
        {
            _initBasepoint();

            cReadData.super_key = "plate";
            cReadData.__sc = 1;
            cReadData._dpsc = 0.01;

            
            //gDraw2DDCLS._dimension_txt_offset = 50;
            gDraw2DDCLS._dimension_round = 50;
            cReadData._dimension_per_metre = false;

            foreach (ObjectId _id in selIds)
                if (ACD.DB._isBlock(_id))
                {
                    cReadData.html_basepoint = ACD.DB._getPoint(_id);
                    //cReadData.ext_basepoint_dist = ACD.DB._getBound(selIds)[0] - cReadData.html_basepoint;
                    break;
                }

            var CssData = new cReadData(selIds);
            
            var gTekla = new gDraw2DTekla(CssData, py_script_path);
            gTekla.DimObj.dimension_architecture_tick = true;
            gTekla.DrawPlate();

            pPos sz = 1.5 * CssData.PointList.Boundary.Size();

            //Ghi file Html plan_html.html
            WriteHtmlCLS.WriteHtml("tekla_html", "teklastyle.css", "PLATE", CssData, (int)sz.X, (int)sz.Y);
            WriteHtmlCLS.WriteMXS("tekla_mxs");
        }

        static void DrawNormalLine(pPos basept)
        {
            cReadData.super_key = "arch";
            cReadData.__sc = 1;
            cReadData._dpsc = 0.01;

            
            //gDraw2DDCLS._dimension_txt_offset = 50;
            gDraw2DDCLS._dimension_round = 50;
            cReadData._dimension_per_metre = false;

            var CssData = new cReadData(selIds);
            cReadData.html_basepoint = basept;

            //ACD.WR("Base {0}", cReadData.html_basepoint);

            var gDr = new gDraw2DDCLS(CssData, null);
            gDr.DimObj.dimension_architecture_tick = true;

            foreach (ObjectId id in selIds)
            {
                if (ACD.DB._isLine(id) || ACD.DB._isPolyline(id))
                    gDr.AppendPolylineHtml("a0-line", 
                        ACD.DB._getVertices(id).Move(cReadData.html_basepoint.Invert),
                        !ACD.DB._isLine(id) && ACD.DB._isPolylineClosed(id));
                else if (ACD.DB._isCircle(id))
                    gDr.AppendCircleHtml("a0-circle", 
                        ACD.DB._getPoint(id) - cReadData.html_basepoint, 
                        ACD.DB._getRadius(id).roundNumber(0.01));
            }

            WriteHtmlCLS.WriteHtml("line_html", "linestyle.css", "LINE", CssData, 40000, 40000);
        }
        static bool _hasKey(string key)
        {
            return selIds.ToList().Any(__id => ACD.DB._isBlock(__id) && ACD.DB._getIdName(__id).st_(key));
        }

        static ObjectIdCollection imgIds;

        public static async Task Main(string[] args)
        {
            using (ACD.Lock())
            {
                imgIds = new ObjectIdCollection();
                selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    ACD.WR("<<Note>>Red line is street");
                    ACD.WR("360 hotkey: x = <x>, y = <y>, j = <yaw>, p = <pitch>, f = <fov>");
                    ACD.WR("$ = height, # = cote, +- = level !!!");

                    pPos[] bb = ACD.DB._getBound(selIds);
                    cReadData.html_basepoint = new pPos(bb[0].X, bb[1].Y);
                    //pPos sz = bb.Size();
                    cReadData.size = bb.Size();

                    var lsIds = selIds.ToList();

                    if (lsIds.Any(__id => ACD.DB._isWall(__id)) || _hasKey("grid") || _hasKey("arch"))
                        // KIẾN TRÚC
                        DrawArch();
                    else if(_hasKey("plate"))
                    {
                        DrawPlate();
                    }
                    else if (_hasKey("plan"))
                    {
                        ACD.WR("QUY HOẠCH PHÂN LÔ");
                        DrawPlan();
                    }
                    else if (_hasKey("map"))
                    {
                        DrawMap();
                    }
                    else if (_hasKey("beam_"))
                    {
                        //
                    }
                    else
                    {
                        pPos basept = ACD.GetPoint();

                        if (basept != null)
                            DrawNormalLine(basept);
                    }

                    WriteHtmlCLS.WriteVrayPython(@"D:\html\vray\user_run.py", false); 
                        //(new pPos(sz.X / 2, sz.Y, 2000)).Round(100), (new pPos(sz.X / 2, sz.Y / 2, 500)).Round(100));
                }
                
                ACD.Focus();
            }
        }
    }
}

