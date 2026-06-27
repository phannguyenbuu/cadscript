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
using System.Threading.Tasks;

using System.Windows.Forms;

namespace AcadScript
{
    public class gDraw2DArch : gDraw2DDCLS
    {
        public gDraw2DArch(cReadData css, string _py_filename) : base(css, _py_filename) {}

        double _dim_range = -750;

        void _drawDimension()
        {
            PosCollection pls = CssData.PointList;
            pPos[] bb = pls.Boundary;

            List<pPos> res = bb.ToList();

            foreach (pPos[] __ls in cReadData.inblock_mtl_list)
            {
                string mtl = __ls[0].Content;

                if (mtl.st_("DIM_"))
                    res.AddRange(__ls);
            }

            var xys = res.ExtractPtsXY();

            for (int ax = 0; ax < 2; ax++)
            {
                var vals = xys[ax];
                
                for(int __n = 0; __n < vals.Length - 1; __n ++)
                {
                    DimObj.category = "a9-dimension";
                    DimObj.offset = _dim_range * 0.667;

                    if (ax == 0)
                    {
                        DimObj.y1 = vals[__n];
                        DimObj.y2 = vals[__n + 1];
                        AppendDimensionY(bb[0].Y);
                    }
                    else
                    {
                        DimObj.x1 = vals[__n];
                        DimObj.y2 = vals[__n + 1];
                        AppendDimensionX(bb[1].X);
                    }
                }
            }
        }

        void _drawMtls()
        {
            PosCollection pls = CssData.PointList;
            Dictionary<string, PosCollection> __dicts = new Dictionary<string, PosCollection>();

            foreach (pPos[] __ls in cReadData.inblock_mtl_list)
            {
                string mtl = __ls[0].Content;

                if(mtl.st_("MT_"))
                    foreach (pPos __p in __ls)
                    {
                        var __in_pls = pls.Where(__region => __region.Length > 2 && __p.Inside(__region))
                                            .OrderBy(__region => -__region.Area()).ToCollectionSameClosed(true);

                        if (!__dicts.ContainsKey(mtl))
                            __dicts.Add(mtl, new PosCollection());

                        if(!__dicts[mtl].Contains(__in_pls.Last()))
                            __dicts[mtl].Add(__in_pls.Last());
                    }
            }

            //ACD.WR("Dicts {0}", __dicts.Count);

            foreach (var _itm in __dicts)
            {
                //ACD.WR("Dict_Itm {0}:{1}",_itm.Key,_itm.Value.Count);
                foreach (var _ls in _itm.Value)
                    AppendPolylineHtml("a1-" + _itm.Key, _ls);
            }
        }

        PosCollection gridlines, gridbounds;

        PosCollection ColumnShapes
        {
            get
            {
                double w = 200, h = 300;
                var xys = gridlines.ExtractPtsXY(50, 50);
                PosCollection res = new PosCollection();

                for(int i = 0; i < xys[0].Length; i++)
                    for (int j = 0; j < xys[1].Length; j++)
                    {
                        double x = xys[0][i];
                        double y = xys[1][j];

                        pPos p = new pPos(x, y);

                        if (gridbounds != null && gridbounds.Any(_ls => p.Inside(_ls)))
                        {
                            pPos p1 = new pPos(-w / 2, -h / 2);

                            if (i == 0) p1.X = 0;
                            if (i == xys[0].Length - 1) p1.X = -w;
                            if (j == 0) p1.Y = 0;
                            if (j == xys[0].Length - 1) p1.Y = -h;

                            res.Add(p1.Rect(w, h).Move(p));
                        }
                    }

                return res;
            }
        }
        public string _getDjangoObject(string scene, string level, string mtl, pPos[] ls)
        {
            string res = "";
            string[] keywords = new string[] { "line", "column", "wall", "door", "window", "mesh" };
            
            string key = keywords.FirstOrDefault(__k => mtl.ct_(__k));
            //ACD.WR("key {0}, mtl {1}", key, mtl);

            if (!key.et_())
            {
                //ACD.WR("key {0}, mtl {1}", key, mtl);
                var h = 3000;

                var wall_width = 100;
                var style = "";

                if (mtl.st_("wall"))
                {
                    if (mtl.ct_("200"))
                        wall_width = 200;
                    else if (mtl.st_("WallD20"))
                        wall_width = 200;
                    else if (mtl.st_("WallD25"))
                        wall_width = 250;
                    else if (mtl.st_("WallD30"))
                        wall_width = 300;
                    else if (mtl.st_("WallD35"))
                        wall_width = 350;
                    else if (mtl.st_("WallD40"))
                        wall_width = 400;
                    else if (mtl.st_("WallD45"))
                        wall_width = 450;
                    else if (mtl.st_("WallD50"))
                        wall_width = 500;

                    style = wall_width.ToString();
                }
                else if (mtl.st_("column"))
                {
                    style = mtl.filter("#").Last();
                }

                res = String.Format("create_{0}('{1}','{2}', [{3},{4},{5}], [{6},{7},{8}], '{9}', {10})",
                    key, scene, level, ls[0].X.roundNumber(), ls[0].Y.roundNumber(), ls[0].Z.roundNumber(), 
                    ls[1].X.roundNumber(), ls[1].Y.roundNumber(), ls[1].Z.roundNumber(), style, h);
            }

            return res;
        }

        int level_from_text(string __level) {
            var level = -1;

            if (__level.ct_("MẶT BẰNG") || __level.ct_("MB"))
            {
                if (__level.ct_("TRỆT"))
                    level = 0;
                if (__level.ct_("LẦU 1"))
                    level = 1;
                else if (__level.ct_("LẦU 2"))
                    level = 2;
                else if (__level.ct_("LẦU 3"))
                    level = 3;
                else if (__level.ct_("LẦU 4"))
                    level = 4;
                else if (__level.ct_("LẦU 5"))
                    level = 5;
                else if (__level.ct_("LẦU 6"))
                    level = 6;
                else if (__level.ct_("LẦU 7"))
                    level = 7;
                else if (__level.ct_("LẦU 8"))
                    level = 8;
                else if (__level.ct_("LẦU 9"))
                    level = 9;
                else if (__level.ct_("LẦU 10"))
                    level = 10;
            }
            return level;
        }

        string __create_polyline_django(string __scene_name, string __type, IEnumerable<pPos> __ls)
        {
            return String.Format("create_polyline('{0}', '{1}', [{2}])",
                        __scene_name, __type, __ls.Select(___p => "(" + ___p.X.roundNumber() + ","
                        + ___p.Y.roundNumber() + "," + ___p.Z.roundNumber()+")").ToTextStr(","));
        }

        public void DrawArch()
        {
            PosCollection pls = CssData.PointList;
            pPos[] bb = pls.Boundary;

            gridlines = new PosCollection();
            gridbounds = new PosCollection();

            List<string> __new_py_contents = new List<string>();

            string __scene = cReadData.super_key.filter("#").First();

            
            string __scenename = cReadData.super_key.filter("_")[0] + "_" + cReadData.SuperKeyValue("name");
            if (!cReadData.super_key.ct_("#"))
                __scenename = cReadData.super_key;

            int __level = level_from_text(cReadData.super_key.filter("#").Last());
            
            for (int i = 0; i < pls.Count; i++)
            {
                pPos[] __ls = pls[i];
                string mtl = __ls[0].Content;
                string cssname = "a0-arch-line" + mtl;

                if (mtl.ct_("wall"))
                {
                    cssname = "a0-arch-wall-" + mtl;
                    __new_py_contents.Add(_getDjangoObject(__scenename, __level.ToString(), mtl, __ls));
                }
                else if (mtl.st_("grid"))
                {
                    //ACD.WR("Hatch:{0}", __ls.Length);

                    if (!mtl.ct_("hatch") && __ls.Length <= 10 && __ls.Length >= 4 && __ls.Size().X <= 600 && __ls.Size().Y <= 600)
                        __new_py_contents.Add(_getDjangoObject(__scenename,
                            __level.ToString(), "column#" + __ls.Size().X.roundNumber() + "x" + __ls.Size().Y.roundNumber(), __ls.Boundary()));

                    if (__ls[0]._isVeryClosed(__ls.Last())) //Nhận ra cột 
                    {
                        gridbounds.Add(__ls);
                        cssname = "a0-arch-gridbound";
                        WriteHtmlCLS.AddVrayExtrude(__ls, -300, "#Wall");

                        
                    }
                    else
                    {
                        gridlines.Add(__ls);
                        cssname = "a0-arch-gridline";
                    }
                }
                else if (mtl.ct_("hidden"))
                {
                    //mtl = "opening";
                    //ACD.WR("Hidden");

                    __new_py_contents.Add(__create_polyline_django(__scenename, "opening", __ls));
                }
                else if (mtl.ct_("angle"))
                {
                    //mtl = "slab_toilet";
                    //ACD.WR("Hatch");
                    __new_py_contents.Add(__create_polyline_django(__scenename, "slab_toilet", __ls));
                }
                else if (mtl.ct_("ansi"))
                {
                    //mtl = "slab_low";
                    __new_py_contents.Add(__create_polyline_django(__scenename, "slab_low", __ls));
                }
                else
                    __new_py_contents.Add(__create_polyline_django(__scenename, "line", __ls));

                AppendPolylineHtml(cssname, __ls);
            }

            _drawMtls();
            _drawDimension();
                        
            DimObj.category = "a9-dimension";
            DimObj.offset = _dim_range;

            DimObj.x1 = bb[0].X;
            DimObj.x2 = bb[1].X;
            AppendDimensionY(bb[0].Y);

            DimObj.y1 = bb[0].Y;
            DimObj.y2 = bb[1].Y;
            AppendDimensionX(bb[1].X);

            foreach(pPos[] r in ColumnShapes)
            {
                AppendPolylineHtml("a1-struct-column", r, true);
                WriteHtmlCLS.AddVrayExtrude(r, 3600, "#Wall");
            }

            //ACD.WR("Lines {0}", __new_py_contents.Count);
            __new_py_contents = __new_py_contents.OrderBy(___s => ___s).Select(__s => "        " + __s).ToList();

            __new_py_contents.Insert(0, "    if (scene):");
            __new_py_contents.Insert(0, 
               String.Format("    scene = create_or_replace_scene(name='{0}', level_id='{1}', gx='{2}', gy='{3}', gz='{4}')",
               __scenename, __level, cReadData.SuperKeyValue("gx"),
               cReadData.SuperKeyValue("gy"), cReadData.SuperKeyValue("gz")));

            File.WriteAllLines(this.py_script_path, 
                this.ReplaceLinesInList("# SAVE DATABASE", "# END DATABASE", __new_py_contents));

            cReadData.RunPython(py_script_path);
        }
    }
}

