using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace AcadScript
{
    
    public class gDraw2DDCLS
    {
        
        public static double _dimension_round = 1;
        public static double _dimension_txt_offset = 100;
        public static double _dimension_breakline = 100;
        public string[] py_contents;
        public string py_script_path;

        public gDimensionCLS DimObj;
        public cReadData CssData { get; set; }

        public gDraw2DDCLS(cReadData css, string _py_filename)
        {
            CssData = css;
            DimObj = new gDimensionCLS();
            
            py_script_path = _py_filename;

            if (File.Exists(py_script_path))
                py_contents = File.ReadAllLines(py_script_path);
            else
                py_contents = new string[0];
        }

        public string[] ReplaceLinesInList(string startKeyword, string endKeyword, IEnumerable<string> newList)
        {
            List<string> result = new List<string>();

            bool insideBlock = false;

            foreach (string line in py_contents)
            {
                if (line.ct_(startKeyword))
                {
                    // Khi tìm thấy từ khóa bắt đầu, thêm nó vào kết quả
                    result.Add(line);
                    // Thêm các dòng mới ngay sau từ khóa bắt đầu
                    result.AddRange(newList);
                    // Bắt đầu bỏ qua các dòng nằm giữa từ khóa bắt đầu và kết thúc
                    insideBlock = true;
                }
                else if (line.ct_(endKeyword))
                {
                    // Khi tìm thấy từ khóa kết thúc, thêm nó vào kết quả
                    result.Add(line);
                    // Kết thúc bỏ qua các dòng
                    insideBlock = false;
                }
                else if (!insideBlock)
                {
                    // Chỉ thêm các dòng không nằm trong khối cần bỏ qua
                    result.Add(line);
                }
            }

            return result.ToArray();
        }

        public void AppendDimensionX(double x)
        {
            DimObj.x1 = DimObj.x2 = x;
            AppendDimension(new pPos(DimObj.x1, DimObj.y1), new pPos(DimObj.x2, DimObj.y1));
        }

        public void AppendDimensionY(double y)
        {
            DimObj.y1 = DimObj.y2 = y;
            AppendDimension(new pPos(DimObj.x1, DimObj.y1), new pPos(DimObj.x1, DimObj.y2));
        }

        public void AppendDimension(pPos _p1, pPos _p2)
        {
            pPos[] _dpts = _p1.Parallel(_p2, DimObj.offset).OrderBy(_p => _p.X).ThenBy(_p => _p.Y).ToArray();
            pPos p1 = _dpts[0], p2 = _dpts[1];

            double dist = p1.DistanceTo(p2);

            if (cReadData._dimension_per_metre)
                dist /= cReadData.__sc;

            dist = dist.roundNumber(_dimension_round);
            pPos ct = ((p1 + p2) / 2).Round(1);

            //ACD.WR("Dim_1 {0},{1},{2}", dist, _p1, _p2);

            if (! DimObj.dimension_list.Any(__p => ct._isVeryClosed(__p, 1)) && dist > 2.9)
            {
                //ACD.WR("Dim_2");

                DimObj.dimension_list.Add(ct);
                double angle = _rounAngle((p2 - p1).Angle());

                //if (angle < 0) angle += 360;
                //if (angle > 89 && angle < 181) angle += 180;

                string txt_over = dist.ToString();

                if (DimObj.values_override != null && DimObj.values_override.Length > 0)
                {
                    var ar = DimObj.values_override.OrderBy(___d => Math.Abs(dist - ___d));
                    if (Math.Abs(ar.First() - dist) < 2)
                        txt_over = ar.First().ToString();
                }

                if (!DimObj.txt_override.et_())
                    txt_over = DimObj.txt_override.Replace("<>", txt_over);

                var _vt = _dimension_txt_offset * new pPos(-_p1.Y + _p2.Y, _p1.X - _p2.X).Normalize;

                if (DimObj.offset != 0)
                    ct -= _vt / 2;

                AppendTextHtml(DimObj.category, txt_over, ct, "", angle);

                if (DimObj.show_full)
                    if (DimObj.dimension_architecture_tick)
                    {
                        var __sz = 20;
                        AppendPolylineHtml(DimObj.category + "-archtick", p1 - new pPos(__sz, __sz), p1 + new pPos(__sz, __sz));
                        AppendPolylineHtml(DimObj.category + "-archtick", p2 - new pPos(__sz, __sz), p2 + new pPos(__sz, __sz));

                        AppendPolylineHtml(DimObj.category, p1 + _vt, _p1 + _vt);
                        AppendPolylineHtml(DimObj.category, p2 + _vt, _p2 + _vt);

                        var _ext = _dimension_txt_offset * (p2 - p1).Normalize;
                        AppendPolylineHtml(DimObj.category, p1 - _ext, p2 + _ext);
                    }
                    else
                    {
                        AppendCircleHtml(DimObj.category, p1, 0.5);
                        AppendCircleHtml(DimObj.category, p2, 0.5);
                        AppendPolylineHtml(DimObj.category, p1, p2);
                    }
            }
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

        public void AppendPlanImage(string category, IEnumerable<pPos> clip_path_pts, string clip_path_id,
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
        string RemoveTransformOrigin(string svgContent)
        {
            // Biểu thức chính quy để tìm và xóa thuộc tính transform-origin
            string pattern = @"\s+transform-origin=['""][^'""]*['""]";
            return System.Text.RegularExpressions.Regex.Replace(svgContent, pattern, string.Empty);
        }
        public void AppendImageHtml(string category, string image_path, pPos pt,
            double moveX = 0, double moveY = 0, 
            double rot = 0, double scaleX = 1, double scaleY = 1)
        {
            if (!double.IsInfinity(moveX))
            {
                string transform_str = String.Format("<image x='{1}' y='{2}'"
                    + " href='{0}' transform ='rotate({5}) scale({6} {7})"
                    + " translate({3} {4})' transform-origin='{1} {2}'/>",
                    image_path, pX(pt), pY(pt), moveX, moveY, rot, scaleX, scaleY);

                transform_str = transform_str.Replace("translate(0 0)", "")
                                               .Replace("rotate(0)", "")
                                               .Replace("rotate(0)", "")
                                               .Replace("scale(1 1)", "")
                                               .Replace("'   '", "''")
                                               .Replace("'  '", "''")
                                               .Replace("transform =''", "");

                if (!transform_str.ct_("transform ="))
                {
                    //ACD.WR("Not constain :{0}", transform_str);
                    transform_str = RemoveTransformOrigin(transform_str);
                }
                
                WriteHtmlCLS.AddItem(category, transform_str);
            }
            else
                WriteHtmlCLS.AddItem(category, String.Format("<image x='{1}' y='{2}' href='{0}'/>", image_path, pX(pt), pY(pt)));
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

            //vrs.AddMesh(p3d.SweepShape(src_3d, section, True),[[0, 0, 0]], 
            //    mtl = 'Red_#dMERALIG3.jpg_#z1000',
            //    tex_displacementmod = 'grass-disp.jpg_#h500')

            //vrs.AddMesh(p3d.ExtrudeShape(src_2d, 20000),
            //    position =[[10000, 11000, 20000]], 
            //    mtl = 'Green_#d054-Pudi.jpg_#z1000')
            
            foreach (pPos[] seg in pts.GetSegment(closed))
            {
                List<pPos> r = seg[0].Parallel(seg[1], CssData.wall_shape_width / 2).ToList();
                r.AddRange(seg[0].Parallel(seg[1], - CssData.wall_shape_width / 2).Reverse());

                CssData.wall_html_contents.Add("<" + (closed ? "polygon" : "polyline") + " points='"
                    + r.Select(pt => pX(pt) + "," + pY(pt)).ToTextStr(" ") + "'/>");
            }
        }

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
   
        //public void AppendRegionInfor(IEnumerable<pPos> pts, pPos pt_title, string mtl)
        //{
        //    RegionInfor itm = new RegionInfor(CssData);
        //    itm.Points = pts.ToArray();
        //    itm.Title = pt_title;
        //    itm.Mtl = mtl;
        //    itm.DrawHtml();
        //}
    }
}
