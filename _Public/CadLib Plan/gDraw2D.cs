using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;

namespace AcadScript
{
    public class gDraw2DDCLS
    {
        public static bool _dimension_architecture_tick = false;
        public static double _dimension_round = 1;
        public static double _dimension_txt_offset = 100;
        public static double _dimension_breakline = 100;
        public cReadData CssData { get; set; }

        public gDraw2DDCLS(cReadData css)
        {
            CssData = css;
        }

        public void AppendHtml(string st)
        {
            CssData.html_contents.Add(st);
        }

        public void AppendTopHtml(string st)
        {
            CssData.html_top_contents.Add(st);
        }

        public void AppendSvgGroup(string st)
        {
            CssData.html_img_svg_content.Add(st);
        }

        public double pX(pPos pt)
        {
            return (pt.X / cReadData.__sc).roundNumber(0.01);
        }

        public double pY(pPos pt)
        {
            return (- pt.Y / cReadData.__sc).roundNumber(0.01);
        }

        public string __transStrList(IEnumerable<pPos> pts, string _extent_info = "")
        {
            return "<polyline points =\"" + pts.Select(p 
                => new pPos(pX(p),pY(p))).ToText(false, ' ') + "\"" + _extent_info + "/>\r\n";
        }


        pPos[] _sortRectanglePoints(pPos[] pts) //Hình chữ nhật đứng
        {
            pPos p1, p2, p3, p4;

            p1 = pts[0];

            if (pts[0].DistanceToPoint(pts[1]) > pts[0].DistanceToPoint(pts[3]))
            {
                p2 = pts[1];
                p3 = pts[3];
                p4 = pts[2];
            }
            else
            {
                p2 = pts[3];
                p3 = pts[1];
                p4 = pts[2];
            }

            return new pPos[] { p1, p2, p3, p4 };
        }

        public void AppendPlanImage(string category, pPos[] clip_path_pts, string clip_path_id,
            pPos img_pt, double img_move_x, double img_move_y,
            double img_scale_x = 1, double img_scale_y = 1)
        {
            double img_rotation = CSVDataCLS._getRegionAngle(clip_path_pts).roundNumber(0.1);

            WriteHtmlCLS.AddItem(category,
                    "<clipPath id = '" + clip_path_id + "'>\r\n"
                    + "\t\t\t\t<polygon points = '" + clip_path_pts.Select(__p => pX(__p) + "," + pY(__p)).ToTextStr(" ") + "'/>\r\n"
                    + "\t\t\t</clipPath>");

            string f_name = cReadData.img_plan_name;
            if (clip_path_id.st_("W_"))
            {
                f_name = cReadData.img_forest_name;
                img_scale_x *= 2.5;
                img_scale_y *= 2.5;
            }

            WriteHtmlCLS.AddItem(category,
                    "<g style = 'clip-path: url(#" + clip_path_id + ")'>\r\n"
                    + "\t\t\t\t<image x = '" + pX(img_pt) + "' y = '" + pY(img_pt)
                    + "' href = '" + f_name + "' transform = '"
                    + "rotate(" + img_rotation
                    + ") scale(" + img_scale_x + " " + img_scale_y + ") "
                    + "translate(" + img_move_x + " " + img_move_y + ")"
                    + "' transform-origin = '" + pX(img_pt) + " " + pY(img_pt) + "'/>\r\n\t\t\t</g>");
        }

        public void AppendCrossRoad(pPos[] pts)
        {
            pPos[] r = _sortRectanglePoints(pts);
            pPos p1 = r[0], p2 = r[1], p3 = r[2], p4 = r[3];

            double space = 1;
            double total = p1.DistanceTo(p2) / 0.6;

            p1 = p1.Along(space, p4);
            p4 = p4.Along(space, p1);
            p2 = p2.Along(space, p3);
            p3 = p3.Along(space, p2);

            for (int i = 0; i < total - 1; i += 2)
                AppendPolylineHtml("i7-cross-road", new pPos[]{

                        p1.Along(0.6 * i, p2),
                        p1.Along(0.6 * (i + 1), p2),
                        p3.Along(0.6 * (i + 1), p4),
                        p3.Along(0.6 * i, p4)

                    }, true);
        }

        public void AppendBreakLine(string category, double p1X, double p1Y, double p2X, double p2Y, double extend)
        {
            pPos p1 = new pPos(p1X, p1Y), p2 = new pPos(p2X, p2Y);
            pPos mid = (p1 + p2) / 2;
            pPos[] line1 = p1.Parallel(p2, extend / 2);
            pPos[] line2 = p1.Parallel(p2, -extend / 2);

            AppendPolylineHtml (category, p1.Along(-extend,p2), mid.Along(extend/4, p1),
                line1.CenterPoint(),line2.CenterPoint(), mid.Along(extend/4, p2), p2.Along(-extend, p1));
        }

        public void AppendCircleHtml(string category, pPos pt, double r)
        {
            WriteHtmlCLS.AddItem(category, String.Format("<circle cx='{0}' cy='{1}' r='{2}'/>", pX(pt), pY(pt), r));
        }

        public void AppendImageHtml(string category, string path, pPos pt,
            double moveX = double.NegativeInfinity, double moveY = 0, 
            double rot = 0, double scaleX = 1, double scaleY = 1)
        {
            if (!double.IsInfinity(moveX))
                WriteHtmlCLS.AddItem(category, String.Format("<image x='{1}' y='{2}'"
                    + " href='{0}' transform ='rotate({5}) scale({6} {7})"
                    + " translate({3} {4})' transform-origin='{1} {2}'/>",
                    path, pX(pt), pY(pt), moveX, moveY, rot, scaleX, scaleY));
            else
                WriteHtmlCLS.AddItem(category, String.Format("<image x='{1}' y='{2}' href='{0}'/>", path, pX(pt), pY(pt)));
        }

        public void AppendNoteLeader(string category, string txt1, string txt2, pPos pt, int type, bool has_pointed = true)
        {
            var len = 500;
            pPos p1 = new pPos(-25, 25), p2 = new pPos(100, 250), p3 = new pPos(len, 250),
                p4 = new pPos(len + 125, 250), p5 = new pPos(len - 400, 260);

            if (type == 1)
            {
                p2 = p2.Invert;
                p3 = p3.Invert;
                p4 = p4.Invert;
                p5 = new pPos(-len + 50, -260);
            }
            else if (type == 2)
            {
                p2 = p2.Invert;
                p3 = p3.Invert;
                p4 = new pPos(475, 100);
                p5 = new pPos(0, 110);
            }
            else if (type == 3)
            {
                p2.Y = p3.Y = p4.Y = p5.Y = 0;
                p2.X -= -len;
                p3.X -= -len;
                p4.X -= -len - 125;
                p5.X -= -len;

                //ACD.WR("Note {0},{1},{2},{3},{4}", p1, p2, p3, p4, p5);
            }
            else if (type == 4)
            {
                p2.Y = p3.Y = p4.Y = p5.Y = 0;
                p2.X = -len;
                p3.X = -len;
                p4.X = -len - 125;
                p5.X = -len;
            }

            if (has_pointed)
                //AppendPolylineHtml("b9-beam-note", pt.X - p1.X, pt.Y - p1.Y, pt.X + p1.X, pt.Y + p1.Y);
                AppendCircleHtml("point", pt, 0.2);

            AppendPolylineHtml(category, pt.X, pt.Y, pt.X + p2.X, pt.Y + p2.Y, pt.X + p3.X, pt.Y + p3.Y);
            AppendCircleHtml(category, pt + p4, 1.2);
            AppendTextHtml("b9-beam-note-txt1", txt1, pt + p4);
            AppendTextHtml("b9-beam-note-txt2", txt2, pt + p5);
        }

        public void AppendNoteCutX(string category, string txt, double x, double y, double h)
        {
            AppendPolylineHtml(category, x, y, x, y + 120);
            AppendTextHtml(category, txt, new pPos(x - 100, y));

            y += h;
            AppendPolylineHtml(category, x, y, x, y + 120);
            AppendTextHtml(category, txt, new pPos(x - 100, y));
        }

        public void AppendTextHtml(string category, string content, pPos pt, string ext = "", double rotation = 0)
        {
            if (pt == null)
            {
                if (rotation == 0)
                    WriteHtmlCLS.AddItem(category, String.Format("<text {3}>{2}</text>",
                        0, 0, content, ext));
                else
                    WriteHtmlCLS.AddItem(category, String.Format("<text {3} transform='rotate({4})'>{2}</text>",
                        0, 0, content, ext, rotation.roundNumber(0.1)));
            }
            else
            {
                double x = pX(pt);
                double y = pY(pt);

                if (rotation == 0)
                    WriteHtmlCLS.AddItem(category, String.Format("<text x='{0}' y='{1}' {3}>{2}</text>",
                        x, y, content, ext));
                else
                    WriteHtmlCLS.AddItem(category, String.Format("<text x='{0}' y='{1}' {3}transform='rotate({4},{0},{1})'>{2}</text>",
                        x, y, content, ext, rotation.roundNumber(0.1)));
            }
        }

        public void AppendPolylineHtml(string category, params pPos[] pts)
        {
            AppendPolylineHtml(category, pts, pts[0]._isVeryClosed(pts.Last()));
        }

        public void AppendPolylineHtml(string category, params double[] dts)
        {
            pPos[] pts = DE.NumericArray(0, (int)(dts.Length /2) - 1)
                .Select(_n => new pPos(dts[_n * 2], dts[_n * 2 + 1])).ToArray();
            AppendPolylineHtml(category, pts, pts[0]._isVeryClosed(pts.Last()));
        }


        public void AppendPolylineHtml(string category, IEnumerable<pPos> pts, bool closed = true, string style = "")
        {
            WriteHtmlCLS.AddItem(category, "<" + (closed ? "polygon" : "polyline") + " points='"
                + pts.Select(pt => pX(pt) + "," + pY(pt)).ToTextStr(" ") + "'"
                + style + "/>");

            foreach (pPos[] seg in pts.GetSegment(closed))
            {
                List<pPos> r = seg[0].Parallel(seg[1], CssData.wall_shape_width / 2).ToList();
                r.AddRange(seg[0].Parallel(seg[1], - CssData.wall_shape_width / 2).Reverse());

                CssData.wall_html_contents.Add("<" + (closed ? "polygon" : "polyline") + " points='"
                    + r.Select(pt => pX(pt) + "," + pY(pt)).ToTextStr(" ") + "'/>");
            }
        }

        public void AppendDimensionX(string category, double x1, double y, double x2, double offset = 0, string txt_override = "")
        {
            AppendDimension(category, new pPos(x1, y), new pPos(x2, y), offset, txt_override);
        }

        public void AppendDimensionY(string category, double x, double y1, double y2, double offset = 0, string txt_override = "")
        {
            AppendDimension(category, new pPos(x, y1), new pPos(x, y2), offset,  txt_override);
        }

        public void AppendDimension(string category, double x1, double y1, double x2, double y2, double offset = 0, string txt_override = "")
        {
            AppendDimension(category, new pPos(x1, y1), new pPos(x2, y2), offset, txt_override);
        }

        public static double _dimension_offset = 0;

        public void AppendDimension(string category, pPos _p1, pPos _p2,
            double offset = 0, string txt_override = "", bool show_full = true)
        {
            if (double.IsNaN(offset))
                offset = _dimension_offset;

            _dimension_offset = offset;

            pPos[] _dpts = _p1.Parallel(_p2, offset);
            pPos p1 = _dpts[0], p2 = _dpts[1];

            double dist = p1.DistanceTo(p2);

            if (cReadData._dimension_per_metre)
                 dist /= cReadData.__sc;
                
            dist = dist.roundNumber(_dimension_round);
            pPos ct = ((p1 + p2) / 2).Round(1);

            //ACD.WR("Dim_1 {0},{1},{2}", dist, _p1, _p2);

            if (!dimension_list.Any(__p => ct._isVeryClosed(__p, 1)) && dist > 2.9)
            {
                //ACD.WR("Dim_2");

                dimension_list.Add(ct);
                double angle = _rounAngle((p2 - p1).Angle());

                if (angle < 0) angle += 360;
                if (angle > 89 && angle < 181) angle += 180;

                txt_override = txt_override.et_() ? dist.ToString() : txt_override.Replace("<>", dist.ToString());

                var _vt = _dimension_txt_offset * new pPos(-_p1.Y + _p2.Y, _p1.X - _p2.X).Normalize;


                if (offset != 0)
                    ct -=  _vt / 2;
                
                AppendTextHtml(category, txt_override, ct, "", angle);

                if (show_full)
                {
                    if (gDraw2DDCLS._dimension_architecture_tick)
                    {
                        var __sz = 20;
                        AppendPolylineHtml(category + "-archtick", p1 - new pPos(__sz, __sz), p1 + new pPos(__sz, __sz));
                        AppendPolylineHtml(category + "-archtick", p2 - new pPos(__sz, __sz), p2 + new pPos(__sz, __sz));

                        AppendPolylineHtml(category, p1 + _vt, _p1 + _vt);
                        AppendPolylineHtml(category, p2 + _vt, _p2 + _vt);

                        var _ext = _dimension_txt_offset * (p2 - p1).Normalize;
                        AppendPolylineHtml(category, p1 - _ext, p2 + _ext);
                    }
                    else
                    {
                        AppendCircleHtml(category, p1, 0.5);
                        AppendCircleHtml(category, p2, 0.5);
                        AppendPolylineHtml(category, p1, p2);
                    }
                }
                
            }
        }

        public List<pPos> dimension_list = new List<pPos>();

        public double _rounAngle(double __ang)
        {
            if (__ang == 180) __ang = 0;
            if (__ang == 90) __ang = 270;

            double a = Math.Sin(__ang / 180 * Math.PI);
            double b = Math.Cos(__ang / 180 * Math.PI);

            return __ang;
        }

            

        double _rounN(double v, params double[] base_rounds)
        {
            double res = v;

            for (int i = 0; i < base_rounds.Length - 1; i++)
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

           
        public void AppendRegionInfor(pPos[] pts, pPos pt_title)
        {
            double angle = 0;
            //Vẽ hình chữ nhật bao quanh text, nếu không chạm đường bao thì ko cần xoay text
            if ((pt_title - new pPos(5 * cReadData.__sc, -0.25 * cReadData.__sc))
                .Rect(5 * cReadData.__sc, 0.25 * cReadData.__sc).Any(__p => !__p.Inside(pts)))
                    angle = CSVDataCLS._getRegionAngle(pts);

            double area = (pts.Area() / 1000000).roundNumber(0.1); // 

            string s_caption = "";// CssData.CaptionList._props(pt_title.Content);

            if (s_caption.et_())
                s_caption = pt_title.Content;

            //ACD.WR("OK1");
            string s_area = "";

            pPos __txt = cReadData.AreaList.FirstOrDefault(__p => __p.Inside(pts));

            if (__txt != null)
                s_area = __txt.Content;

            //double circle_size = (float)(CssData.AllXNotes._props("Show.Circle").ToNumber(30) * CssData.drawing_scale);
            double circle_size = 2.8;
            circle_size *= cReadData._dpsc;

            if (angle == -999)
                angle = (float)pt_title.Rotation;

            //double v = 8.0f;

            if (s_area.et_())
            {
                s_area = "" + _rounN(area, 80, 100, 120, 150, 180, 200, 220, 250,
                    280, 300, 320, 360, 380, 400, 420, 450, 480, 500, 1000);
            }

            string id = "txt_"
                + new string(s_caption.Replace("3A", "").Replace("2A", "").Replace("2B", "")
                .Where(c => !"01234567890".Contains(c)).ToArray()) + "_" + angle;

            if (!s_caption.ct_("MT_") && !s_caption.ct_("ZERO"))
            {
                AppendCircleHtml("i8-caption-txt", pt_title, circle_size);
                AppendTextHtml("i8-caption-txt", s_caption, pt_title + new pPos(0, 1.25)); //, "fill='red' font-size='2'"
                AppendTextHtml("i9-area-txt", s_area, null, // pt_title + _get_txt_area_offset(angle),
                    "id ='" + s_caption + "'", angle);
            }

            if (pt_title.Content.ct_("WALK") && pts.Length > 3 * cReadData.__sc)
            {
                if (pts[0].DistanceToPoint(pts[1]) > pts[0].DistanceToPoint(pts[3]))
                    AppendDimension("i9-dimension", pts[0], pts[1]);
                else
                    AppendDimension("i9-dimension", pts[0], pts[3]);
            }
            
            if (!pt_title.Content.st_("MT_") && pt_title.Content != "ZERO")
                for (int i = 0; i < pts.Length; i++)
                    AppendDimension("i9-dimension", pts[i], pts[i == pts.Length - 1 ? 0 : i + 1]);
        }
    }
}
