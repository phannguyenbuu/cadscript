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
    public class cReadData
    {
        public double min, round;
        public float fence_spacing, fence_min, fence_size;

        public double wall_shape_width = 0.1;

        //public float canvas_scale = 8.5f, canvas_offset = 50f;
        public static pPos  html_basepoint;
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
        public pPos size, drawing_title_offset;
        public static double __sc = 1, _dpsc = 1; //Display Scale
        public List<string> HtmlCssList;

        public static PosCollection inblock_mtl_list = new PosCollection();
        public static string img_plan_name = "asset/SVG/plan_itm.svg";
        public static string img_forest_name = "asset/SVG/plan_forest.svg";
        public static string tree_plan_name = "asset/SVG/tree_plan.svg";
        public static string lamp_plan_name = "asset/SVG/lamp_plan.svg";

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
                    if (_s.st_(k))
                        res = _s.Substring(k.Length);
                }

                return res;
            }
        }

        public static bool ___InsidePts(pPos p, IEnumerable<pPos> pts)
        {
            bool res = false;
            if (pts != null && pts.Count() > 0)
            {
                pPos[] bb = pts.Boundary();
                //ACD.WR("PTS {0} BB1 {1} BB2 {2}", pts.Count(), bb[0], bb[1]);
                if (p.InsideRect(bb[0], bb[1]))
                {
                    List<pPos> ls = pts.ToList();// : pts.Offset(offset).ToList();
                    
                    if (ls.First()._isVeryClosed(ls.Last()))
                        ls.RemoveAt(ls.Count - 1);

                    int n = ls.Count;
                    double angle = 0;

                    for (int i = 0; i < n; i++)
                        angle += DE.Angle(ls[i].X - p.X, ls[i].Y - p.Y,
                            ls[(i + 1) % n].X - p.X, ls[(i + 1) % n].Y - p.Y);

                    res = (Math.Abs(angle) >= Math.PI);
                }
            }

            return res;
        }

        public static void _generate_title_from_linears(g3DBuild g3d)
        {
            Dictionary<string, int> dicts = new Dictionary<string, int>();
            List<pPos> inblock_titles = new List<pPos>();

            inblock_titles.AddRange(TitleList);
            foreach (pPos __p in inblock_titles)
                __p.Content = "W_" + __p.Content.Replace(" ","_");

            var regions = g3d.regionList;
    
            foreach (pPos[] _ls in inblock_mtl_list)
            {
                List<pPos> res = new List<pPos>();
                string mtl = _ls[0].Content;

                foreach (pPos[] _reg in regions)
                    for (int i = 0; i < _ls.Length - 1; i++)
                    {
                        pPos _p1 = _ls[i], _p2 = _ls[i + 1], _p = null;

                        if (___InsidePts(_p1, _reg))
                            _p = _p1.Clone();
                        else if (___InsidePts(_p2, _reg))
                            _p = _p2.Clone();
                        else if (!mtl.st_("MT_") &&  _reg.Area() > 5000000)
                        {
                            pPos[] intpts = _reg.Add(_reg[0]).Intersect(_p1, _p2, true);

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

                        //ACD.WR("OK3.4");
                    }
                }
            }
            
            //string msg = "";
            
            for (int i = 0; i < regions.Count; i++)
            {
                //ACD.DB.DrawPolyline(regions[i].Select(__p => __p / dwg_scale), true);
                pPos pt_title = inblock_titles.FirstOrDefault(p => ___InsidePts(p, regions[i]));

                if (pt_title != null)
                {
                    var ___number = _getNumberFrTxt(pt_title.Content).ToNumber();

                    if (___number == 13)
                        pt_title.Content = pt_title.Content.Replace("13", "12A");
                    else if (___number == 4)
                        pt_title.Content = pt_title.Content.Replace("4", "3A");

                    g3d.MtlPoints[i] = pt_title;
                    inblock_titles.Remove(pt_title);

                    //ACD.DB.DrawCircle(pt_title / dwg_scale, 1);
                    //msg += regions[i][0].Content + ";";
                }
                //else
                    //msg += "Empty;";//ACD.WR("Empty");
            }

            //for (int i = 0; i < regions.Count; i++)
            //    if(!regions[i][0].Content.et_())
            //        ACD.DB.CreateText("[" + regions[i][0].Content._getInComma("[]") + "]", 
            //             regions[i].CenterPoint() / dwg_scale, 2);
        }

        static string _getNumberFrTxt(string key, bool inv = true)
        {
            return new string(DE.NumericArray(0, key.Length - 1)
                            .Where(__n => inv == "0123456789".ct_(key[__n].ToString()))
                            .Select(__n => key[__n]).ToArray());
        }

        public static List<pPos> AreaList = new List<pPos>(), TitleList = new List<pPos>();

        PosCollection IdsToPts(ObjectIdCollection ids, int segments = 32, pPos[] xclip = null, double __extent = 0)
        {
            PosCollection res = new PosCollection();
            res.Closed = new bool[0];

            foreach (ObjectId id in ids)
            {
                pPos[] bb = ACD.DB._getBound(id);

                if (xclip == null
                    || bb.All(__p => __p.InsideRect(xclip[0], xclip[1])))
                {

                    if (ACD.DB._isText(id))
                    {
                        pPos __p = ACD.DB._getPoint(id) - html_basepoint;
                        (__p.Content.ToNumber() >= 80 ? AreaList : TitleList).Add(__p * __sc);
                    }
                    else if (ACD.DB._isPolyline(id) || ACD.DB._isLine(id))
                    {
                        string _type = ACD.DB.GetLinetypeText(id);
                        var ls = ACD.DB._getVertices(id, segments);

                        if (_type.et_() || (ls.Length == 2 && _type == "#"))
                        {
                            res.Add(ls);
                            res.Closed = res.Closed.Add(ACD.DB._isPolylineClosed(id));
                        }
                        else
                        {
                            if (_type == "#")
                            {
                                if (ls.Length > 2)
                                    _type = "MT_GRASS";
                                else
                                {
                                    _type = "MT_ROAD";
                                    ls = new pPos[] { ls.CenterPoint() };
                                }
                            }
                            //ACD.WR("p6");
                            ls[0].Content = _type;
                            inblock_mtl_list.Add(ls);
                        }
                    }
                    else if (ACD.DB._isWall(id))
                    {
                        gWallElement _gwall = new gWallElement(id);
                        res.AddRange(_gwall.WallList);

                        for(int i = res.Closed.Length; i < res.Count; i++)
                            res.Closed = res.Closed.Add(true);
                    }
                    else if (ACD.DB._isBlock(id))
                    {
                        var __xclip = ACD.DB.GetXCLIP(id);

                        if (__xclip == null)
                            __xclip = xclip;
                                                
                        ACD.DB.BlockEntitiesAction(id, _ids =>
                        {
                            PosCollection _pls = IdsToPts(_ids, segments, __xclip);
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
                }
            }

            for (int i = 0; i < res.Count; i++)
            {
                pPos[] __pts = res[i].ExtentLine(__extent);
                res[i] = res.Closed[i] ? res[i].Add(res[i].First().Clone())
                        : (double.IsNaN(__pts.Last().X) || double.IsNaN(__pts.First().X) ? res[i] : __pts);
            }

            return res;
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

            bb = ACD.DB._getBound(selIds);
            //pPos[] bb = ACD.DB._getBound(selIds);
            cReadData.html_basepoint = new pPos(bb[0].X, bb[1].Y);
            size = bb.Size();
            //basept = bb[0];

            W = (int)size.X;
            H = (int)size.Y;

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

