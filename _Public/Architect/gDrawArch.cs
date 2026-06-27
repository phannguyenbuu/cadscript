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

namespace AcadScript
{
    public class gDraw2DArch : gDraw2DDCLS
    {
        public gDraw2DArch(cReadData css) : base(css) { }

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
                {
                    res.AddRange(__ls);
                }
            }

            var xys = res.ExtractPtsXY();

            for (int ax = 0; ax < 2; ax++)
            {
                var vals = xys[ax];
                //var nex = (ax + 1) % 2;

                for(int __n = 0; __n < vals.Length - 1; __n ++)
                {
                    if (ax == 0)
                        AppendDimensionX("a9-dimension", vals[__n], bb[0].Y, vals[__n + 1], _dim_range * 0.667);
                    else
                        AppendDimensionY("a9-dimension", bb[1].X, vals[__n], vals[__n + 1], _dim_range * 0.667);

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


        public void DrawArch()
        {
            PosCollection pls = CssData.PointList;
            pPos[] bb = pls.Boundary;

            foreach(pPos[] __ls in pls)
            {
                string cssname = "a0-plan-line";

                if(__ls[0].Content.ct_("wall"))
                {
                    cssname = "a0-plan-wall";
                }

                AppendPolylineHtml(cssname, __ls);
            }

            _drawMtls();
            _drawDimension();

            AppendDimensionX("a9-dimension", bb[0].X, bb[0].Y, bb[1].X, _dim_range);
            AppendDimensionY("a9-dimension", bb[1].X, bb[0].Y, bb[1].Y, _dim_range);
        }
    }
}

