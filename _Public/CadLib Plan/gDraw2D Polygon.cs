using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace AcadScript
{
    public class gDraw2DPolygonCLS : gDraw2DDCLS
    {
        public gDraw2DPolygonCLS(cReadData css) : base(css) { }
        double txt_spacing = 8;
        bool _area_in_cirle_mode = false;
        double circle_size = 2.8;

        double _rounN(double v, params double[] base_rounds)
        {
            double res = v;

            for(int i = 0; i < base_rounds.Length - 1; i++)
                if (v < base_rounds[i + 1] && v > base_rounds[i])
                {
                    double a = base_rounds[i + 1] - v;
                    double b = v - base_rounds[i];
                    
                    if (a < b)
                        res = base_rounds[i + 1];
                    else
                        res = base_rounds[i];

                    break;
                }

            return v;// res.roundNumber(0.01);
        }

        pPos _get_txt_area_offset(double angle)
        {
            angle = _rounAngle(angle);
            //pPos sz = CSVDataCLS._getRegionSize(pts);

            return new pPos(Math.Abs(Math.Cos(angle / 180 * Math.PI)), 
                Math.Abs(Math.Sin(angle / 180 * Math.PI)))

                * txt_spacing;
        }

        double _rounAngle(double __ang)
        {
            if (__ang == 180) __ang = 0;
            if (__ang == 90) __ang = 270;

            double a = Math.Sin(__ang / 180 * Math.PI);
            double b = Math.Cos(__ang / 180 * Math.PI);

            //if (Math.Abs(a) < Math.Abs(b))
            //    __ang = 0;
            //else
            //    __ang = 270;

            return __ang;
        }

        string _t(int n)
        {
            string res = "";
            for(int i = 0; i<n; i++)
            {
                res += "\t";
            }

            return res;
        }

        public void DrawItemImg(pPos[] pts, pPos pt_title, string img_name, 
            double img_mX = 0, double img_mY = 0, double img_scX = 1, double img_scY = 1)
        {
            double tX = pX(pt_title), tY = pY(pt_title);
            double angle = CSVDataCLS._getRegionAngle(pts);

            //angle = 0;

            double area = (pts.Area()).roundNumber(0.1);

            string res = "";

            res += "<clipPath id = '" + pt_title.Content + "'>\r\n";
            res += _t(3) + "<polygon points = '";

            foreach (pPos p in pts)
                res += pX(p) + "," + pY(p) + " ";

            res += "'/>\r\n";


            //100.28,139.59 93.66,142.92 63.33,141.33 56.69,134.96 65.87,39.25 78.22,41.15 76.69,71.28 75.76,96.41 82.15,97.15 88.07,103.12 89.71,104.77 95.51,119.63 98.07,129.65 99.51,135.28' />
            //   < !-- < polygon points = '241.07,123.84 242,150.22 232.92,151.38 228.07,149 228.07,50.32 235.62,50.41 235.77,50.42 235.91,50.43 236.06,50.45 236.21,50.47 236.35,50.51 236.49,50.55 236.63,50.6 236.77,50.65 236.9,50.71 237.03,50.78 237.16,50.86 237.28,50.94 237.4,51.02 237.52,51.11 237.63,51.21 237.73,51.32 237.83,51.42 237.93,51.54 238.02,51.65 238.1,51.78 238.18,51.9 238.25,52.03 238.31,52.16 238.37,52.3 238.42,52.44 238.47,52.58 238.51,52.72 238.54,52.86 238.56,53.01 238.58,53.16 238.58,53.3 238.77,58.67' /> -->
            res += _t(2) + "</clipPath>\r\n";
            //res += _t(2) + "<g>\r\n";
            res += _t(2) + "<g style = 'clip-path: url(#" + pt_title.Content + ")'>\r\n";

            res += _t(3) + "<image x = '" + tX + "' y = '" + tY + "' href = 'asset/SVG/" + img_name 
                + "' transform = '" 
                
                //+ "translate(" + img_mX + " " + img_mY + ") "
                
                + "rotate(" + angle.roundNumber(0.1)
                + ") scale(" + img_scX + " " + img_scY + ") "
                + "translate(" + img_mX + " " + img_mY + ")"

                + "' transform-origin = '" + tX + " " + tY + "'/>\r\n";
            res += _t(2) + "</g>\r\n";

            AppendHtml(res);
        }

        public void DrawPolygonItem(pPos[] pts, pPos pt_title, int counter = 0)
        {
            double angle = CSVDataCLS._getRegionAngle(pts);
            double area = (pts.Area()).roundNumber(0.1);

            string s_caption = CssData.CaptionList._props(pt_title.Content);
            
            if (s_caption.et_())
                s_caption = pt_title.Content;

            //ACD.WR("OK1");
            string s_area = "";

            pPos __txt = cReadData.AreaList.FirstOrDefault(__p => __p.Inside(pts));

            if (__txt != null)
                s_area = __txt.Content;

            //double circle_size = (float)(CssData.AllXNotes._props("Show.Circle").ToNumber(30) * CssData.drawing_scale);
            circle_size *= CssData.drawing_scale;

            if (angle == -999)
                angle = (float)pt_title.Rotation;
                
            double v = 8.0f
                + (CssData.AllXNotes._props("Show.zigzag").ToBool()
                ? (counter % 2 == 0 ? 0 : -5) : -2);
                
            
            //ACD.WR("OK2");
            if (s_area.et_())
            {
                s_area = "" + _rounN(area, 80, 100, 120, 150, 180, 200, 220, 250, 
                    280, 300, 320, 360, 380, 400, 420, 450, 480, 500, 1000);
            }

            v += (s_caption.Length > 2 || area > 999 ? 7f : 3.5f) ;

            angle = Math.Round((angle + 180) % 360);

            if (angle == 270) angle = 90;
            if (angle == 360) angle = 0;

            string id = "txt_" 
                + new string(s_caption.Replace("3A","").Replace("2A", "").Replace("2B","")
                .Where(c => !"01234567890".Contains(c)).ToArray()) + "_" + angle;
            
            bool show_road_mode = new string[]{
                    "CÔNG VIÊN", "CAFE", "NHÀ HÀNG", "TRUNG TÂM",
                    "GRASS","WATER","PAVE", "ROAD", "WALK", "LAND"}
                .Any(__s => s_caption.ct_(__s));

            if(!show_road_mode)
            {
                AppendCircleHtml(pt_title, circle_size, "stroke:black;fill:white");

                if (_area_in_cirle_mode)
                {
                    AppendTextHtml(s_caption, pt_title + new pPos(0, 1.25 * CssData.drawing_scale), "fill='red' font-size='2'");
                    AppendPolylineHtml(new pPos[] { pt_title - new pPos(circle_size, 0), pt_title + new pPos(circle_size, 0) }, 
                        false, " style='stroke-opacity:1;fill:none'");
                }
                else
                    AppendTextHtml(s_caption, pt_title, "fill='red' font-size='2'");
            }
            
            if (show_road_mode)
            {
                if (pt_title.Content.ct_("WALK") && pts.Length > 3)
                {
                    //ACD.WR("OK4.1");
                    if (pts[0].DistanceToPoint(pts[1]) > pts[0].DistanceToPoint(pts[3]))
                        _drawDimension(pts[0], pts[1], "red");
                    else
                        _drawDimension(pts[0], pts[3], "red");
                }
            }
            else if(!pt_title.Content.st_("MT_") && pt_title.Content != "ZERO")
            {
                //ACD.WR("OK4.2");
                for (int i = 0; i < pts.Length; i++)
                    _drawDimension(pts[i], pts[i == pts.Length - 1 ? 0 : i + 1], "red", s_caption);
            }
            //ACD.WR("OK5");

            if (!show_road_mode && (CssData.AllXNotes._props("Show.Area").et_()
                || CssData.AllXNotes._props("Show.Area").ToBool()))
            {
                if (_area_in_cirle_mode)
                    AppendTextHtml(s_area, pt_title - new pPos(0, 1.25 * CssData.drawing_scale), "fill='black'");
                else
                {
                    pPos[] _r = new pPos(pt_title.X, pt_title.Y - 2).Rect(12, 2);

                    if (_r.IntersectPts(pts, true, true).Length > 0)
                        angle = _rounAngle(angle);
                    else
                        angle = 0;

                    CssData.html_contents.Add("<!-- W -->\r\n");
                    //AppendPolylineHtml(_r, true, " style='stroke-opacity:1;fill:none'");
                    AppendTextHtml(s_area, pt_title + _get_txt_area_offset(angle), "fill='black' id='" + s_caption +  "'", angle);
                }
            }
            //ACD.WR("OK6");
        }

        List<pPos> dimension_list = new List<pPos>();

        void _drawDimension(pPos p1, pPos p2, string color = "red", string id = null)
        {
            double dist = p1.DistanceTo(p2).roundNumber(1);
            pPos ct = ((p1 + p2) / 2).Round(1);

            if (!dimension_list.Any(__p => ct._isVeryClosed(__p, 1)) && dist > 2.9)
            {
                dimension_list.Add(ct);
                double angle = _rounAngle((p2 - p1).Angle());

                if (angle < 0) angle += 360;
                if (angle > 89 && angle < 181) angle += 180;

                string txt = "fill='" + color + "' font-size='2.5'";

                if (!id.et_())
                    txt += " id='" + id + "'";

                AppendTextHtml(dist + "m", ct, txt, angle);

                AppendCircleHtml(p1, 0.5, "fill:" + color);
                AppendCircleHtml(p2, 0.5, "fill:" + color);
            }
        }
    }
 
}
