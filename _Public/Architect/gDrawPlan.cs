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

namespace AcadScript
{
    public class gDraw2DPlan : gDraw2DDCLS
    {
        public g3DBuild g3D;
                
        public gDraw2DPlan(cReadData css) : base(css) { }

        public string __comment(string s)
        {
            return "<!-- " + s + " -->\r\n";
        }

        public string _getExtendInfoFromContent(string content)
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


        public void DrawPlan3D()
        {
            string wall_content = "";
            string content = "<head>\r\n"
                + "<link rel = \"stylesheet\" href = \"styles/cadstyle.css\">\r\n"
                + "<title>PLAN</title>\r\n"
                + "</head>\r\n"
                + "<svg width=\"1800\" height =\"900\">\r\n";

            //content += "\t" + __comment("Scale=" + cReadData.__sc);
            //content += "\t" + __comment("WallHeight=" + 3600 / cReadData.__sc);
            //content += "\t" + __comment("WallTop=");
            //content += "\t" + __comment("WallBottom=");
            //content += "\t" + __comment("WallBorder=");
            //content += "\t" + __comment("CeilBorder=" + 150 / cReadData.__sc);
            //content += "\t" + __comment("DoorTop=" + 2800 / cReadData.__sc);
            //content += "\t" + __comment("WindowTop=" + 2800 / cReadData.__sc);
            //content += "\t" + __comment("WindowBottom=" + 2800 / cReadData.__sc);
            //content += "\t" + __comment("DisplayFilter=");

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
                    pPos[] pts_2d = pts; // cReadData.__to2D(g3D.regionList[i]);
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

                    pPos pt_title = g3D.MtlPoints[i];
                    double ____imgsc = cReadData.SuperKeyValue("sc").ToNumber(24) / 100;

                    if (!pt_title.Content.ct_("MT_") && !pt_title.Content.ct_("PARK"))
                            AppendPlanImage("i6-plan-image", pts_2d,
                                pt_title.Content, pt_title, -600 / 5, -400 / 5, ____imgsc, ____imgsc);

                    string __category = pPos.FromString(pt_title.Content).Content;
                    if (__category.et_())
                        __category = pt_title.Content;

                    if (!__category.ct_("MT_ROAD"))
                    {
                        AppendPolylineHtml(!__category.st_("MT_") && !__category.st_("PARK")
                            && !__category.st_("ZERO") ? "i7-zone-" + __category[0] : "i1-" + __category, pts_2d, true);
                        AppendRegionInfor(pts_2d, pt_title);
                    }
                }
            }

            for (int i = 0; i < g3D.roadList.Count; i++) //(pPos[] pts in pls)
            {
                var __pls = g3D.roadList[i].RoadSplitToRegions;

                foreach(pPos[] __ls in __pls)
                    AppendPolylineHtml("i1-road", __ls, true);
            }

            __drawRoad();

            content += wall_content;
            content += "</svg>\r\n";

            Directory.CreateDirectory(@"D:\html\");
            File.WriteAllText(@"D:\html\wallinfo.html", content);
        }

        void __drawRoad()
        {
            const double _arrow_img_sc = 0.025;
            //ACD.WR("Road {0}", g3D.roadList);

            foreach (var __itm in g3D.roadList)
            {
                var _pts = __itm.CenterLine;
                if (_pts != null)
                {
                    AppendPolylineHtml("i5-center-road-line", _pts, false);
                    AppendPolylineHtml("i5-center-road-line-fx", _pts, false);
                }

                //ACD.WR("i5-center-road-line");

                foreach (var pts in __itm.Pavements)
                    AppendPolylineHtml("i5-road-pavement", pts, false, "");

                //ACD.WR("i5-road-pavement");

                pPos[] __lamps = __itm.GeneratePavementElements(1, 25);

                foreach (pPos _p in __lamps)
                    AppendImageHtml("i8-lamp-pavement", cReadData.lamp_plan_name,
                        _p, -60, -60, 0, 0.032, 0.032);

                //ACD.WR("i8-lamp-pavement");

                foreach (pPos _p in __itm.GeneratePavementElements(1, 10))
                    if (!__lamps.Any(______p => _p._isVeryClosed(______p, 25)))
                        AppendImageHtml("i8-tree-pavement", cReadData.tree_plan_name,
                            _p, -60, -60, 0, 0.032, 0.032);

                //ACD.WR("i8-tree-pavement");

                if (!cReadData.super_key.ct_("#noarrow"))
                    foreach (var pts in __itm.ExtendCaps)
                    {
                        pPos[] _line = pts[0].Parallel(pts[1], 5 * cReadData.__sc);
                        if (_line.CenterPoint().Inside(__itm.PointList))
                            _line = pts[0].Parallel(pts[1], -5 * cReadData.__sc);

                        pPos p = _line.CenterPoint();
                        p.Rotation = (p - pts.CenterPoint()).Angle();

                        AppendImageHtml("i5-road-arrow", "asset/SVG/arrow.svg", p,
                            0, 0, p.Rotation.roundNumber(0.1) + 90, _arrow_img_sc, _arrow_img_sc);
                        AppendImageHtml("i5-road-arrow", "asset/SVG/arrow.svg", p,
                            0, 0, p.Rotation.roundNumber(0.1) + 90, -_arrow_img_sc, _arrow_img_sc);

                        AppendDimension("i9-dimension",pts[0],pts[1],0,"<> m",false);
                    }
            }

            foreach (var pts in g3D.walkList)
                AppendDimension("i9-dimension", pts[0], pts[1], 3 * cReadData.__sc, "<> m", false);
            
            PosCollection __walks = g3D.AllWalkListBars;

            foreach (var __pts in __walks)
                AppendPolylineHtml("i9-cross-road", __pts, true);

            //ACD.WR("Titles {0}", cReadData.TitleList.ToText());
        }
    }

    public class CadLibPlanCLS
    {
        
    }
}

