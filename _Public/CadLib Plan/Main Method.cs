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

using System.Windows.Forms;
//using SyncObject;
//using Photoshop;

namespace AcadScript
{
    public class CadLibPlanCLS_2
    {
        static g3DBuild g3D;
        static gDraw2DPolygonCLS draw2d;
        static gDraw2DRegionCLS region2d;

        static int H = 2000, W = 2000;
        static CSVDataCLS csv;
        static cReadData CssData;
        
        static string _getStylename(string key, string _default = null)
        {
            string stylename = null;

            if (!key.et_())
                stylename = CssData.StyleList.FirstOrDefault(s => s.st_("style_" + key));

            //ACD.WR("StyleItem {0},{1}", key, stylename);

            return stylename.et_() ? 
                (_default.et_() ? null : " style='" + _default + "'")
                : " style='" + stylename._firstProp() + "'";
        }

        static string __comment(string s)
        {
            return "<!-- " + s + " -->\r\n";
        }

        static string _getExtendInfoFromContent(string content)
        {
            string ext_info = null;
            double cote = 0, h = 0;

            string[] ar = content.filter("_");

            h = ar.FirstOrDefault(_s => _s.st_("$")).ToNumber();
            double nlevel = ar.FirstOrDefault(_s => _s.st_("+") || _s.st_("-")).ToNumber();
            cote = ar.FirstOrDefault(_s => _s.st_("#")).ToNumber();

            if (content.et_())
                content = "DEF";

            if (content.ct_("[") && content.ct_("]"))
            {
                content = content._getInComma("[]");

                foreach (char __s in "0123456789")
                    content = content.Replace(__s.ToString(), "");
            }


            ext_info = " mtl=\"" + content + "\"";

            if (h != 0)
                ext_info += " h=\"" + h + "\"";

            if (cote != 0)
                ext_info += " z=\"" + cote + "\"";

            return ext_info;
        }

        static string __transStrList(IEnumerable<pPos> pts, string _extent_info = "")
        {
            return "<polyline points =\""
                + pts.Select(p => new pPos(p.X - CssData.html_basepoint.X * cReadData.__sc, 
                    p.Y - CssData.html_basepoint.Y * cReadData.__sc)).ToText(false, ' ') 
                + "\"" + _extent_info + "/>\r\n";
        }


        static void Draw3D()
        {
            string wall_content = "";
            string content = "<head>\r\n"
                + "<link rel = \"stylesheet\" href = \"styles/cadstyle.css\">\r\n"
                + "</head>\r\n"
                + "<svg width=\"" + W + "\""
                + " height =\"" + H + "\">\r\n";

            content += "\t" + __comment("Scale=" + cReadData.__sc);
            content += "\t" + __comment("WallHeight=" + 3600 / cReadData.__sc);
            content += "\t" + __comment("WallTop=");
            content += "\t" + __comment("WallBottom=");
            content += "\t" + __comment("WallBorder=");
            content += "\t" + __comment("CeilBorder=" + 150 / cReadData.__sc);
            content += "\t" + __comment("DoorTop=" + 2800 / cReadData.__sc);
            content += "\t" + __comment("WindowTop=" + 2800 / cReadData.__sc);
            content += "\t" + __comment("WindowBottom=" + 2800 / cReadData.__sc);
            content += "\t" + __comment("DisplayFilter=");

            //ACD.WR("To Html {0} - {1} - {2}", srcIds.ToList().Select(id => id.ObjectClass.DxfName).ToTextStr(","), 
            //ACD.DB.FilterIds(srcIds, "AEC_WALL").Count, ACD.DB.FilterIds(srcIds, "INSERT").Count);

            //List<string> contents_2d = new List<string>();
            //ACD.WR("OK1");

            for (int i = 0; i < g3D.regionList.Count; i++) //(pPos[] pts in pls)
            {
                //ACD.WR("MTL: {0}", g3D.regionList[i][0].Content);
                
                string mtl = g3D[i];

                if (!mtl.et_())
                {
                    //ACD.WR("OK2");
                    pPos[] pts = g3D.regionList[i];
                    pPos[] pts_2d = g3D.regionList[i].Select(__p => __p / cReadData.__sc).ToArray();
                    //ACD.DB.DrawPolyline(pts_2d, true);

                    content += __comment("Mesh");
                    content += "\t" + __transStrList(pts, _getExtendInfoFromContent(mtl)) + "\r\n";

                    PosCollection _segs = pts.GetSegment(true).OrderBy(_l
                        => -_l.Length(false)).ToCollectionSameClosed(false);

                    for (int j = 0; j < 2; j++)
                        if (!mtl.ct_("MT_") && !mtl.st_("Z") && _segs[j].Length(false) >= 15)
                        {
                            wall_content += "\t" + __comment("WallFence");
                            wall_content += "\t" + __transStrList(_segs[j][0].ParallelOffset(_segs[j][1], 50));
                            wall_content += "\r\n";
                        }

                    pPos pt_title = g3D.MtlPoints[i] / cReadData.__sc;
                    //ACD.WR("OK3");

                    //ACD.DB.DrawPolyline(pts_2d.Select(_p => _p / cReadData.__sc), false);

                    region2d.AppendPolylineHtml(pts_2d, false, _getStylename(null, "fill:none"));
                    draw2d.DrawPolygonItem(pts_2d, pt_title, 0);

                    if(!pt_title.Content.ct_("MT_"))
                        draw2d.DrawItemImg(pts_2d, pt_title, "plan_itm.svg", -600/5, -400/5, 0.32, 0.32);

                    //200 x 600: /8
                    //600 x 200: /4
                    //600 x 400: /5


                    region2d.DrawColor(pts_2d, pt_title.Content, _getStylename(pt_title.Content),
                        Array.IndexOf(CssData.AllNameLetters, pt_title.Content[0]));
                }
            }

            ACD.WR("Center Lines {0}", g3D.roadListObj.CenterLines.Count);

            if (g3D.roadListObj != null)
            {
                foreach (pPos[] __pts in g3D.roadListObj.SegmentPavementList)
                {
                    pPos[] __ls = __pts[0].ParallelOffset(__pts[1], 100);

                    wall_content += "\t" + __comment("WallPaveBorder");
                    wall_content += "\t" + __transStrList(__ls);
                    wall_content += "\r\n";

                    //region2d.AppendPolylineHtml(__ls.Select(__p => __p / cReadData.__sc),
                    //true, _getStylename(null, "fill:orange"));
                }

                _addGroupPolyline("center-road-line");
                _addGroupPolyline("center-road-line-fx");
            }

            //ACD.WR("OK5");

            content += wall_content;
            content += "</svg>\r\n";

            Directory.CreateDirectory(@"D:\html\");
            File.WriteAllText(@"D:\html\wallinfo.html", content);
        }

        static void _addGroupPolyline(string classname)
        {
            region2d.AppendTopHtml("<g class='" + classname + "'>");
            foreach (pPos[] __line_2d in g3D.roadListObj.CenterLines)
                region2d.AppendTopPolylineHtml(__line_2d.Select(__p => __p / cReadData.__sc), false, "");

            region2d.AppendTopHtml("</g>");
        }

        public static void StartMethod()
        {
            using (ACD.Lock())
            {
                ACD.WR("<<Note>>Red line is street");
                ACD.WR("!!! For ceiling object, put circle for low ceil plane !!!");
                ACD.WR("!!! Char $ = height, # = cote, +- = level !!!");

                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    CssData = new cReadData(selIds);
                    pPos[] bb = ACD.DB._getBound(selIds);

                    CssData.html_basepoint = new pPos(bb[0].X, bb[1].Y);

                    //Tìm basepoint trong nhóm blockIds
                    ObjectIdCollection ___blockIds = selIds.ToList().Where(_id => ACD.DB._isBlock(_id)).ToCollection();

                    if (___blockIds.Count > 0)
                    {
                        CssData.html_basepoint = ACD.DB._getPoint(___blockIds.ToList().OrderBy(_id =>
                            {
                                var __sz = ACD.DB._getBound(_id).Size();
                                return -__sz.X * __sz.Y;
                            }).First());

                        CssData.super_key = CssData.html_basepoint.Content;
                    }

                    csv = new CSVDataCLS(CssData);

                    W = (int)(1.5 * bb.Size().X * CssData.drawing_scale).roundNumber(100);
                    H = (int)(1.5 * bb.Size().Y * CssData.drawing_scale).roundNumber(100);

                    //Ghi file 3D
                    g3D = new g3DBuild(selIds, CssData);
                    
                    draw2d = new gDraw2DPolygonCLS(CssData);
                    region2d = new gDraw2DRegionCLS(CssData);

                    Draw3D();
                    //g3D.Print();

                    //Draw2D();

                    //Ghi file Csv *_csv.csv, lấy dữ liệu đầu vào table_data cho hàm WriteHtml
                    string table_data = csv.SaveCSV(g3D, @"D:\html\" 
                        + Path.GetFileNameWithoutExtension(ACD.CurrentDWGFileName) + "_cls.csv");


                    //Ghi file Html plan_html.html
                    WriteHtmlCLS.WriteHtml(CssData, W, H, table_data);
                }

                ACD.Focus();
            }
        }
    }
}

