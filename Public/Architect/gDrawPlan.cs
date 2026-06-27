using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace AcadScript
{
    public class gDraw2DPlan : gDraw2DMap
    {
        public gDraw2DPlan(cReadData css, string _py_filename) : base(css, _py_filename) {
            ACD.WR("GPLAN 01");
        }
                
        public override void DrawMap()
        {
            //for (int i = 0; i < g3D.Count; i++)
            //{
            //    var itm = g3D[i];

            //    if (itm.MtlName.st_("W_"))
            //        itm.MtlName = itm.MtlName.Substring(2);
            //}
            ACD.WR("OP1");
            base.DrawMap();
            ACD.WR("OP1.1");
            for (int i = 0; i < g3D.Count; i++) //(pPos[] pts in pls)
            {
                var itm = g3D[i];
                ACD.WR("OP2");
                if (itm is gRoadElement)
                {
                    itm.TempLines.AddRange(itm.BivectorPoints);
                    AppendPolylineHtml("i8-road", itm.ShowPoints, true);

                    foreach (var _ls in ((gRoadElement)itm).EndCaps)
                        AppendPolylineHtml("i8-tmp", _ls, true);

                    WriteHtmlCLS.AddVrayExtrude(itm.ShowPoints, -200, "i8-road");
                }
                else
                {
                    string mtl = itm.MtlName;
                    pPos pt_title = itm.MtlPoint;

                    double ____imgsc = cReadData.SuperKeyValue("sc").ToNumber(24) / 100;

                    if (!mtl.ct_("MT_") && !mtl.ct_("PARK"))
                        AppendPlanImage("i6-plan-image", itm, mtl, pt_title, -600 / 5, -400 / 5, ____imgsc, ____imgsc);

                    if (itm.MtlPoint != null && itm.AxisPoint != null)
                        AppendPolylineHtml("i8-tmp", new pPos[] { itm.MtlPoint, itm.AxisPoint }, false);

                    //AppendRegionInfor(itm, pt_title, mtl, itm.Note);
                }

                ACD.WR("OP3");
            }
            ACD.WR("OP4");
            __drawRoad();
        }

        void __drawRoad()
        {
            const double _arrow_img_sc = 0.025;
            DimObj.category = "i9-dimension";

            foreach (var itm in g3D.RoadList)
            {
                foreach (var pts in itm.Pavements)
                {
                    AppendPolylineHtml("i9-road-pavement", pts, false, "");
                    WriteHtmlCLS.AddVraySweep(pts, new pPos(0, 0).Rect(1000, 200), false, "i5-road-pavement.jpg|#z1000");
                }

                foreach (var child in itm.Children)
                {
                    foreach (var _pts in child.CenterLines)
                    {
                        AppendPolylineHtml("i9-center-road-line", _pts, false);
                        AppendPolylineHtml("i9-center-road-line-fx", _pts, false);

                        WriteHtmlCLS.AddVraySweep(_pts, new pPos(0, 0).Rect(50, 50), false, "i5-center-road-line.jpg");
                    }

                    pPos[] __lamps = child.GenerateElements(1, 25);

                    foreach (pPos _p in __lamps)
                        AppendImageHtml("i9-lamp-pavement", cReadData.lamp_plan_name,
                            _p, -60, -60, 0, 0.032, 0.032);

                    //ACD.WR("i8-lamp-pavement");
                    WriteHtmlCLS.AddVrayProxy("lamp_column", __lamps, 1);

                    var __pts = child.GenerateElements(2, 10);

                    if (__pts.Length > 0)
                    {
                        foreach (pPos _p in __pts)
                            if (!__lamps.Any(______p => _p._isVeryClosed(______p, 25)))
                            {
                                AppendImageHtml("i9-tree-pavement", cReadData.tree_plan_name,
                                    _p, -60, -60, 0, 0.032, 0.032);
                            }

                        // AM163_040
                        //ACD.WR("T2");
                        WriteHtmlCLS.AddVrayProxy("AM163_022", __pts, 2);
                    }
                }
                //ACD.WR("arrow {0}", itm.ArrowCaps.Count);

                foreach (var ls in itm.ArrowCaps)
                {
                    AppendImageHtml("i5-road-arrow", "asset/SVG/arrow.svg", ls[0],
                        0, 0, ls[0].Rotation.roundNumber(0.1) + 90, _arrow_img_sc, _arrow_img_sc);
                    AppendImageHtml("i5-road-arrow", "asset/SVG/arrow.svg", ls[0],
                        0, 0, ls[0].Rotation.roundNumber(0.1) + 90, -_arrow_img_sc, _arrow_img_sc);

                    DimObj.txt_override = "<> m";
                    AppendDimension(ls[1], ls[2]);
                }

                //ACD.WR("Temp lines:{0}", itm.TempLines.Count);
                foreach (pPos[] _ls in itm.TempLines)
                    AppendPolylineHtml("i8-tmp", _ls);
            }

            //foreach (var itm in g3D)
            //    if(itm is gRoadElement)
            //        AppendDimension("i9-dimension", pts[0], pts[1], 3 * cReadData.__sc, "<> m", false);

            PosCollection __walks = g3D.AllWalkListBars;

            foreach (var __pts in __walks)
            {
                AppendPolylineHtml("i9-cross-road", __pts, true);
                WriteHtmlCLS.AddVrayExtrude(__pts, 50, "i9-cross-road.jpg");
            }
            //ACD.WR("Titles {0}", cReadData.TitleList.ToText());
        }
    }

}

