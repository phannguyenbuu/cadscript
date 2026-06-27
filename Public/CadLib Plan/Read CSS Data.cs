using System;
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

namespace AcadScript
{

    public static class SpacingTool
    {
        static pPos _interpolate(pPos[] segment, double t)
        {
            var dx = segment[1].X - segment[0].X;
            var dy = segment[1].Y - segment[0].Y;
            var l = Math.Sqrt(dx * dx + dy * dy);

            return new pPos(segment[0].X + dx / l * t, segment[0].Y + dy / l * t);
        }

        public static pPos[] RangePoints(pPos[] pts, double pointDist, bool divison_equals = true, bool align = false)
        {
            List<pPos> res = new List<pPos>();
            double polyLineLength = pts.Length(false);
            //ACD.WR("p0");
            //pPos[] __pts = ParralellLine(pts,1 * cReadData.__sc);

            if (divison_equals)
                pointDist = polyLineLength / (Math.Floor(polyLineLength / pointDist));

            int numDist = (int)(polyLineLength / pointDist);
            //ACD.WR("p1");
            double pointPosition = 0.0;
            double prevSegmentsLength = 0.0;
            double segmentsLength = 0.0;
            int currentSegment = 0;
            PosCollection segments = pts.GetSegment(false);
            var segment = segments.First;
            //ACD.WR("p2");
            for (int i = 0; i <= numDist; i++)
            {
                while (pointPosition > segmentsLength && currentSegment < segments.Count)
                {
                    prevSegmentsLength = segmentsLength;
                    //ACD.WR("p2.1");
                    segment = segments[currentSegment]; //ACD.WR("p2.2");
                    segmentsLength += segment.Length(false);
                    currentSegment++;
                }

                var point = _interpolate(segment, pointPosition - prevSegmentsLength);

                res.Add(point);
                pointPosition += pointDist;
            }

            if (pts.Length == 2)
                for (int i = 0; i < res.Count; i++)
                    res[i].Rotation = (pts.First() - pts.Last()).Angle();
            else if (res.Count > 1)
            {
                for (int i = 0; i < res.Count - 1; i++)
                    res[i].Rotation = (res[i] - res[i + 1]).Angle();

                res[res.Count - 1].Rotation = (res[res.Count - 1] - res[res.Count - 2]).Angle();
            }

            return res.ToArray();
        }

        static pPos[] _parallelSeg(pPos p1, pPos p2, double v)
        {
            var L = p1.DistanceTo(p2);

            var x1p = p1.X + v * (p2.Y - p1.Y) / L;
            var x2p = p2.X + v * (p2.Y - p1.Y) / L;
            var y1p = p1.Y + v * (p1.X - p2.X) / L;
            var y2p = p2.Y + v * (p1.X - p2.X) / L;

            return new pPos[] { new pPos(x1p, y1p), new pPos(x2p, y2p) };
        }

        public static pPos[] ParralellLine(pPos[] pts, double v_out)//, pPos __firstpoint, pPos __lastpoint)
        {
            PosCollection pls = new PosCollection();
            //ACD.WR("PAR 0");
            for (int i = 0; i < pts.Length - 1; i++)
                pls.Add(_parallelSeg(pts[i], pts[i + 1], v_out));

            if (pls.Count > 0)
            {
                //ACD.WR("PAR 1 {0}", pls);
                List<pPos> res = new List<pPos>() { pls[0][0] };
                //ACD.WR("PAR 1.1");

                for (int i = 0; i < pls.Count - 1; i++)
                {
                    //ACD.WR("PAR 1.2");
                    var __p = pls[i][0].Intersect(pls[i][1], pls[i + 1][0], pls[i + 1][1], false);
                    //ACD.WR("PAR 2");
                    if (__p != null)
                    {
                        if (res.Count < 2)
                            res.Add(__p);
                        else
                        {
                            pPos __p1 = res[res.Count - 2];
                            pPos __p2 = res[res.Count - 1];
                            __p.DistanceTo(__p1, __p2);

                            if (!pPos.DistanceTo_Projection.IsBetween(__p1, __p2))
                                res.Add(__p);
                        }
                    }
                }

                res.Add(pls.Last().Last());
                return res.ToArray();
            }
            else
                return null;
        }
    }

    public class cReadData
    {
        public static pPos[] AxisData = new pPos[]
        {
            new pPos(0, 0, 1, "TOP"),
            new pPos(0, 1, 0, "FRONT"),
            new pPos(0, -1, 0, "BACK"),
            new pPos(1, 0, 0, "RIGHT"),
            new pPos(-1, 0, 0, "LEFT")
        };

        public static bool _isPointInView(pPos __p, pPos axis, pPos CenterPoint)
        {
            return (axis.IsEqualTo(cReadData.AxisData[0]) && __p.Z >= CenterPoint.Z)
                || (axis.IsEqualTo(cReadData.AxisData[1]) && __p.Y >= CenterPoint.Y)
                || (axis.IsEqualTo(cReadData.AxisData[2]) && __p.Y <= CenterPoint.Y)
                || (axis.IsEqualTo(cReadData.AxisData[3]) && __p.X >= CenterPoint.X)
                || (axis.IsEqualTo(cReadData.AxisData[4]) && __p.X <= CenterPoint.X);
        }


        public double min, round;
        public float fence_spacing, fence_min, fence_size;

        public double wall_shape_width = 0.1;

        //public float canvas_scale = 8.5f, canvas_offset = 50f;
        public static pPos html_basepoint, ext_basepoint_dist;
        public List<string> html_contents, html_img_svg_content, html_top_contents;
        public List<string> wall_html_contents;
        public static string super_key;
        //public List<pPos> inblock_titles;
        public PosCollection PointList;

        public int H = 1000, W = 1000;
        public string dim_block_key, showmode;
        public string script_dir = @"D:\Dropbox\VS Projects\ScriptEditor\Sample\System\";
        public string drawing_title_suffix = "", drawing_title_prefix = "";
        public double drawing_title_size = 1, drawing_number_size = 1, drawing_angle = -999;
        public static bool _dimension_per_metre = true;
        public pPos[] bb;
        //public string[] CaptionList, AllXNotes, ItemNoteList, Region3DInforList, Material3DList, Modifier3DList;
        public double[] PriceList, ThoCuList;
        public int drawing_axis = -1;

        public string project_name;
        public double price_offset;
        public CSVDataCLS csv;

        public string path_sample_tree = @"D:\Dropbox\3DLib\maps\sk\Tree plan\SampleTree.png";
        public float tree_size = 3f;

        public string exp_dir = @"D:\$exp";
        public static pPos size;
        public static double __sc = 1, _dpsc = 1; //Display Scale
        public List<string> HtmlCssList;

        public static int number_of_round_cameras = 6;
        public static double camera_height = 150000;

        public static PosCollection inblock_mtl_list = new PosCollection();
        public static string img_plan_name = "asset/SVG/plan_itm.svg";
        public static string img_forest_name = "asset/SVG/plan_forest.svg";
        public static string tree_plan_name = "asset/SVG/tree_plan.svg";
        public static string lamp_plan_name = "asset/SVG/lamp_plan.svg";
        public static string map_ref_img_file = "";

        public pPos[] GridLetters;

        public static pPos[] __to2D(IEnumerable<pPos> pts)
        {
            return pts.Select(__p => __p / cReadData.__sc).ToArray();
        }

        static pPos[] _getLetterList(ObjectIdCollection selIds)
        {
            var pts = new List<pPos>();

            foreach (ObjectId _id in selIds)
            {
                string _letter = ACD.DB.GetLinetypeText(_id);

                if (!_letter.et_())
                {
                    pPos p = ACD.DB._getVertices(_id)[0];
                    p.Content = _letter;

                    pts.Add(p);
                }
            }

            return pts.ToArray();
        }

        public static string SuperKeyValue(string k)
        {
            if (k.st_("#"))
            {
                return cReadData.super_key.ct_(k) ? "Y" : "N";
            } else
            {
                string[] __ar = cReadData.super_key.filter("_");

                string res = null;

                foreach (string _s in __ar)
                {
                    if (k.Length == 1 && _s.st_(k))
                        res = _s.Substring(k.Length);
                    else if  (k.Length > 1 && _s.st_(k + "~"))
                        res = _s.Substring(k.Length + 1);
                }

                return res;
            }
        }

        

        public static void _generate_title_from_linears(cBuilder3D g3d)
        {
            //ACD.WR("OWR1");

            Dictionary<string, int> dicts = new Dictionary<string, int>();
            List<pPos> inblock_titles = new List<pPos>();

            inblock_titles.AddRange(TitleList);

            foreach (pPos __p in inblock_titles)
                __p.Content = __p.Content.filter("/").Last().Replace(" ","_");

            foreach (pPos[] _ls in inblock_mtl_list)
            {
                List<pPos> res = new List<pPos>();
                string mtl = _ls[0].Content;

                foreach (var _reg in g3d)
                    for (int i = 0; i < _ls.Length - 1; i++)
                    {
                        pPos _p1 = _ls[i], _p2 = _ls[i + 1], _p = null;

                        if (_reg.___InsidePts(_p1))
                            _p = _p1.Clone();
                        else if (_reg.___InsidePts(_p2))
                            _p = _p2.Clone();
                        else if (!mtl.st_("MT_") &&  _reg.Area() > 5000000)
                        {
                            //_reg.ToArray().Add(_reg[0]).Intersect(_p1, _p2, true);
                            pPos[] intpts = _reg.Intersect(_p1, _p2, true);

                            if (intpts.Length > 0 && intpts.CenterPoint().IsBetween(_p1, _p2))
                                _p = intpts.CenterPoint();
                        }

                        if (_p != null)
                        {
                            //ACD.DB.CreateText(mtl + "_CENTER", _p / dwg_scale, 1);

                            _p.Content = mtl;
                            res.Add(_p);

                            break;
                        }
                    
                    }

                //ACD.WR("generate_res:{0}", res.Count);

                if (res.Count > 0)
                {
                    res = res.OrderBy(p => p.openedPathParam(_ls)).ToList();

                    for (int i = 0; i < res.Count; i++)
                    {
                        string key = _ls[0].Content;

                        int n_index = (int)_getNumberFrTxt(key).ToNumber(1);
                        key = _getNumberFrTxt(key, false);

                        if (!dicts.ContainsKey(key))
                            dicts.Add(key, n_index);
                        
                        int index = dicts[key];

                        res[i].Content = key + (key.st_("MT_") ? "" : index.ToString());
                        inblock_titles.Add(res[i]);
                        //ACD.WR("OK3.3");
                        if (!key.st_("MT_")) dicts[key]++;
                    }
                }
            }

            //ACD.WR("OWR2");

            foreach (var itm in g3d)
            {
                pPos pt_title = inblock_titles.FirstOrDefault(p => itm.___InsidePts(p));

                if (pt_title != null)
                {
                    var ___number = _getNumberFrTxt(pt_title.Content).ToNumber();
                    itm.MtlPoint = pt_title;
                    itm.MtlName = pt_title.Content;

                    if (___number == 13)
                        itm.MtlName = itm.MtlName.Replace("13", "12A");
                    else if (___number == 4)
                        itm.MtlName = itm.MtlName.Replace("4", "3A");

                    inblock_titles.Remove(pt_title);
                }
            }

            //ACD.WR("OWR3");
        }

        static string _getNumberFrTxt(string key, bool inv = true)
        {
            return new string(DE.NumericArray(0, key.Length - 1)
                            .Where(__n => inv == "0123456789".ct_(key[__n].ToString()))
                            .Select(__n => key[__n]).ToArray());
        }

        public static List<pPos> AreaList = new List<pPos>(), TitleList = new List<pPos>();
        public static List<pPos[]> DimensionList = new List<pPos[]>();

        public static PosCollection[] ReadcBuilder3DInfo(ObjectIdCollection selIds)
        {
            ObjectIdCollection srcIds = new ObjectIdCollection();

            PosCollection __mtlList = new PosCollection();
            PosCollection __walkList = new PosCollection();

            foreach (ObjectId _id in selIds)
            {
                if (ACD.DB._isBlock(_id))
                    srcIds.Add(_id);

                __getSubMtlList(_id, __mtlList, __walkList);
            }

            return new PosCollection[] { __mtlList, __walkList };
        }

        static void __getSubMtlList(ObjectId _id, PosCollection mtlList, PosCollection walkList)
        {
            string st = ACD.DB.GetLinetypeText(_id);

            if (ACD.DB._isPolyline(_id) || ACD.DB._isLine(_id))
            {
                pPos[] ls = ACD.DB._getVertices(_id, 16);

                if (st.st_("MTL") || st.st_("ML") || st.st_("MT"))
                {
                    foreach (pPos _p in ls)
                        _p.Content = st;

                    mtlList.Add(ls);
                }
                else if (st.st_("#"))
                {
                    walkList.Add(cReadData.__to2D(ls));
                }
            }
            else if (ACD.DB._isBlock(_id))
            {
                ACD.DB.BlockEntitiesAction(_id, _subIds =>
                {
                    foreach (ObjectId _sid in _subIds)
                        __getSubMtlList(_sid, mtlList, walkList);
                });
            }
        }


        PosCollection IdsToPts(ObjectIdCollection ids, int segments = 32,
            pPos[] xclip = null, double __extent = 0, string objname = "")
        {
            PosCollection res = new PosCollection();
            res.Closed = new bool[0];

            foreach (ObjectId id in ids)
            {
                pPos[] bb = ACD.DB._getBound(id);

                if (xclip == null || bb.All(__p => __p.InsideRect(xclip[0], xclip[1])))
                {
                    if (ACD.DB._isText(id))
                    {
                        pPos __p = ACD.DB._getPoint(id) - html_basepoint;

                        (__p.Content.ct_("m2") ? AreaList : TitleList).Add(__p * __sc);
                    }
                    else if (ACD.DB._isPolyline(id) || ACD.DB._isLine(id))
                    {
                        string _type = ACD.DB.GetLinetypeText(id);

                        var ls = ACD.DB._getVertices(id, segments);
                        ls[0].Content = objname;

                        if (_type.et_() || (ls.Length == 2 && _type == "#"))
                        { 


                            ls[0].Content += "_" + ACD.DB._getLayer(id);
                            res.Add(ls);
                            res.Closed = res.Closed.Add(ACD.DB._isPolylineClosed(id));
                        }
                        else
                        {
                            if (_type == "#")
                            {
                                //ACD.WR("Has # {0}", ls.Length);
                                _type = ls.Length > 2 ? "MT_GRASS" : "MT_ROAD";

                                List<pPos> __tmps = new List<pPos> { ls.Boundary().CenterPoint() };
                                __tmps.AddRange(ls);
                                ls = __tmps.ToArray();
                            }

                            ls[0].Content = _type;
                            inblock_mtl_list.Add(ls);
                        }
                    }
                    else if (ACD.DB._isHatch(id))
                    {
                        string _type = ACD.DB._getIdName(id).filter("|")[0];
                        var ls = ACD.DB._getVertices(id, segments);
                        ls[0].Content = objname + "#hatch" + "#" + _type;

                        res.Add(ls);
                        res.Closed = res.Closed.Add(ACD.DB._isPolylineClosed(id));
                    }
                    else if (ACD.DB._isWall(id))
                    {
                        //gWallElement _gwall = new gWallElement(id);
                        var __wall = ACD.DB._getVertices(id);
                        __wall[0].Content = ACD.DB._getIdName(id);

                        if (!__wall[0].Content.st_("wall"))
                            __wall[0].Content = "Wall" + __wall[0].Content;

                        res.Add(__wall);

                        for (int i = res.Closed.Length; i < res.Count; i++)
                            res.Closed = res.Closed.Add(false);
                    }
                    else if (ACD.DB._isBlock(id))
                    {
                        var __xclip = ACD.DB.GetXCLIP(id);

                        if (__xclip == null)
                            __xclip = xclip;

                        var blockname = ACD.DB._getIdName(id);

                        ACD.DB.BlockEntitiesAction(id, _ids =>
                        {
                            PosCollection _pls = IdsToPts(_ids, segments, __xclip, __extent, blockname + "_");
                            res.AddRange(_pls);

                            for (int _i = 0; _i < _pls.Count; _i++)
                                res.Closed = res.Closed.Add(_pls.Closed[_i]);
                        });
                    }
                    else if (ACD.DB._isCircle(id))
                    {
                        res.Add(_addCircle(id));
                        res.Closed = res.Closed.Add(true);
                    }
                    else if (ACD.DB._isDim(id))
                    {
                        res.Add(_addCircle(id));
                        res.Closed = res.Closed.Add(true);
                    }
                }
            }

            for (int i = 0; i < res.Count; i++)
            {
                pPos[] __pts = res[i].ExtentLine(__extent);
                res[i] = res.Closed[i] ? res[i].Add(res[i].First().Clone())
                        : (double.IsNaN(__pts.Last().X) || double.IsNaN(__pts.First().X) ? res[i] : __pts);
            }

            //ACD.WR("Mtl:{0}", inblock_mtl_list.Select(_ls => _ls[0].Content).ToTextStr());
            return res;
        }

        public static void InitBasepoint(ObjectIdCollection selIds)
        {
            pPos[] bb = ACD.DB._getBound(selIds);
            html_basepoint = new pPos(bb[0].X, bb[1].Y);
            //WriteHtmlCLS.VrayPyContents = new Dictionary<string, List<string>>();

            ObjectIdCollection ___blockIds = selIds.ToList().Where(_id => ACD.DB._isBlock(_id)).ToCollection();

            if (___blockIds.Count > 0)
            {
                html_basepoint = ACD.DB._getPoint(___blockIds.ToList().OrderBy(_id =>
                {
                    var __sz = ACD.DB._getBound(_id).Size();
                    return -__sz.X * __sz.Y;
                }).First());

                super_key = cReadData.html_basepoint.Content;
                html_basepoint.Content = "";
            }

        }

        public static void RunPython(string script)
        {
            string python = @"C:\Python312\python.exe";  // Hoặc chỉ cần "python" nếu đã cài PATH

            // Đường dẫn tới file script Python của bạn
            //string script = @"C:\path\to\your_script.py";

            // Tạo đối tượng ProcessStartInfo để cấu hình việc chạy script
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
            psi.FileName = python;
            psi.Arguments = "\"" + script + "\""; // Gọi file script Python
            psi.UseShellExecute = false; // Không sử dụng giao diện shell
            psi.RedirectStandardOutput = true; // Chuyển hướng đầu ra
            psi.RedirectStandardError = true;  // Chuyển hướng đầu ra lỗi

            // Tạo tiến trình và bắt đầu thực hiện
            using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi))
            {
                // Đọc đầu ra của tiến trình
                string output = process.StandardOutput.ReadToEnd();
                string errors = process.StandardError.ReadToEnd();

                // Chờ quá trình hoàn tất
                process.WaitForExit();

                // Hiển thị kết quả
                ACD.WR("Output: " + output);
                ACD.WR("Errors: " + errors);
            }
        }

        static pPos[] _addCircle(ObjectId id)
        {
            pPos ct = ACD.DB._getPoint(id);
            double r = ACD.DB._getRadius(id);

            List<pPos> pts = new List<pPos>();

            var cX = ct.X;
            var cY = ct.Y;
            int numSegment = 32;

            double angleOffset = Math.PI * 2 / numSegment;
            double currentAngle = 0;

            pts.Add(new pPos(r * Math.Cos(currentAngle) + cX, r * Math.Sin(currentAngle) + cY));

            for (int i = 0; i < numSegment; i++)
            {
                double startAngle = currentAngle;
                double endAngle = currentAngle + angleOffset ;

                pts.Add(new pPos(r * Math.Cos(endAngle) + cX, r * Math.Sin(endAngle) + cY));

                currentAngle += angleOffset;
            }

            return pts.ToArray().Add(pts.First());
        }

        public static string _getCrossRoadMtlAxis(pPos[] ls)
        {
            double __ang = CSVDataCLS._getRegionAngle(ls);

            string res = "";
            double a = Math.Sin(__ang / 180 * Math.PI);
            double b = Math.Cos(__ang / 180 * Math.PI);

            if (Math.Abs(a) < Math.Abs(b))
                res = "MT_WALK_A_$20";
            else
                res = "MT_WALK_B_$20";

            return ls.CenterPoint() + "[" +  res + "]";
        }

        public static pPos[] _getMLineVertices(ObjectId _id)
        {
            ObjectIdCollection _newIds = ACD.DB.ExplodeEntity(_id);

            List<pPos> _lls = new List<pPos>();
            //int i = 0;
            for(int i = 0; i < _newIds.Count - 1; i += 2)
            {
                _lls.AddRange(ACD.DB._getVertices(_newIds[i], 0, false));
                _lls.AddRange(ACD.DB._getVertices(_newIds[i + 1], 0, false).Reverse());
            }

            _lls.Add(_lls[0]);

            ACD.DB.EraseObjects(_newIds);

            return _lls.ToArray();
        }

        public char[] AllNameLetters
        {
            get
            {
                return "ABCDEFGHIJK".Select(s => s).ToArray();
            }
        }

        public pPos GetRegionTitle(pPos[] pts, bool closed, out string st)
        {
            pPos p = pts.FirstOrDefault(p1 => !p1.Content.empty());
            pPos res = TitleList.FirstOrDefault(p1 => p1.Inside(pts));

            //ACD.WR("REGION {0} PTS {1}", res, pts.ToText());

            string title = res == null ? "" : res.Content;
            
            st = p == null ? "" : (p.Content
                + (title.empty() ? "" : "(" + title + ")") + "=" + pts.ToText(closed));
            //ACD.WR("oK5");
            if (res == null)
                res = pts.Centroid();

            res.Content = title;

            //ACD.WR("<Polygon title> {0} TitleList {1} Content {2}", title, TitleList.Length, res);

            return res;
        }

        pPos[] _searchBlockByName(string key)
        {
            string[] blocknames = ACD.DB.ListBlock().Where(s => s.ct_(key)).ToArray();

            List<pPos> res = new List<pPos>();

            ACD.DB.GetEntities(null, EN_SELECT.AC_DXF, "INSERT");

            foreach (ObjectId id in IR.SelectedIds)
                if (blocknames.Contains(ACD.DB._getIdName(id)))
                    ACD.DB.BlockEntitiesAction(id, (subIds) =>
                    {
                        res.AddRange(subIds.ToList()
                                .Where(sid => ACD.DB._isText(sid))
                                .Select(sid =>
                                {
                                    pPos p = ACD.DB._getPoint(sid);
                                    p.Rotation = ACD.DB._getRotation(sid);
                                    return p;
                                }
                            )
                        );
                    });

            ACD.WR("<TitleList> Blocks {0} result {1}", blocknames.Length, res.Count);
            return res.ToArray();
        }

        
        public string AddHtmlCss(string cssname, bool add_transform, params string[] cssvalues)
        {
            string css = "\t" + cssname + "\r\n\t{\r\n\t\t" + cssvalues.ToTextStr(";\r\n\t\t") 
                + (add_transform ? "\t\ttransform: translate(0,0);" : "") + "}\r\n";

            if (!HtmlCssList.Contains(css))
                HtmlCssList.Add(css);

            return cssname;
        }

        public static string DefaultXNotes
        {
            get
            {
                return File.ReadAllText(Path.Combine(DE.CADLIB_CONSTRUCT, "DefautCSSData.txt"));
            }
        }

        public static pPos basepoint_to_3d_python
        {
            get
            {
                return new pPos(html_basepoint.X, -html_basepoint.Y) * __sc;
            }
        }

        public string[] StyleList;

        public cReadData(ObjectIdCollection selIds, double _get_pointlist_extend = 0)
        {
            HtmlCssList = new List<string>();
            html_contents = new List<string>();
            html_top_contents = new List<string>();
            html_img_svg_content = new List<string>();
            wall_html_contents = new List<string>();
            
            Directory.CreateDirectory(exp_dir);

            foreach (string f in Directory.GetFiles(exp_dir, "*.*"))
                File.Delete(f);

            //bb = ACD.DB._getBound(selIds);
            //pPos[] bb = ACD.DB._getBound(selIds);
            //cReadData.html_basepoint = new pPos(bb[0].X, bb[1].Y);
            
            //basept = bb[0];

            W = (int)cReadData.size.X;
            H = (int)cReadData.size.Y;

            cReadData.size *= cReadData.__sc;

            GridLetters = _getLetterList(selIds);

            PointList = IdsToPts(selIds, 32, null, _get_pointlist_extend)
                .Select(_ls => _ls.Move(html_basepoint.Invert).Select(_p  => _p * __sc).ToArray())
                .ToCollectionSameClosed(false);

            inblock_mtl_list = inblock_mtl_list
                .Select(_ls => _ls.Move(html_basepoint.Invert).Select(_p => _p * __sc).ToArray())
                .OrderBy(_ls => _ls[0].Content)
                .ToCollectionSameClosed(false);

            //ACD.WR("AreaList:{0} TitleList:{1}", AreaList.Count, TitleList.Count);
        }

        public void AddPointList(PosCollection pls)
        {
            PointList.AddRange(pls
                .Select(_ls => _ls.Move(html_basepoint.Invert).Select(_p => _p * __sc).ToArray())
                .ToCollectionSameClosed(false));
        }
    }
}

