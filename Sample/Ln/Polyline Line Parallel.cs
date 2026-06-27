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

    public class PolylineLineParallelHorizontalCLS
    {
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
            
            for (int i = 0; i < pts.Length - 1; i++)
                pls.Add(_parallelSeg(pts[i], pts[i + 1], v_out));

            //foreach (pPos[] ls in pls)
                //ACD.DB.DrawPolyline(ls, false);

            //if (pls.Count > 0)
                List<pPos> res = new List<pPos>() { pls[0][0] };
                
                for (int i = 0; i < pls.Count - 1; i++)
                {
                    var __p = pls[i][0].Intersect(pls[i][1], pls[i + 1][0], pls[i + 1][1], false);
                    
                    //if (__p != null)
                    //{
                    //    if (res.Count < 2)
                            res.Add(__p);
                    //    else
                    //    {
                    //        pPos __p1 = res[res.Count - 2];
                    //        pPos __p2 = res[res.Count - 1];
                    //        __p.DistanceTo(__p1, __p2);

                    //        if (!pPos.DistanceTo_Projection.IsBetween(__p1, __p2))
                    //            res.Add(__p);
                    //    }
                    //}
                }

                res.Add(pls.Last().Last());
                return res.ToArray();
            
            //else
            //    return null;
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count >= 2)
                {
                    pPos[] pts_line = ACD.DB._getVertices(selIds[0], 16);
                    pPos[] pts_ranges = ACD.DB._getVertices(selIds[1], 16);

                    if(pts_line.IntersectPts(pts_ranges,false,false).Length > 0)
                    { 
                        double min = pts_ranges[0].DistanceTo(pts_ranges[1]) / 100;

                        foreach (pPos p in pts_ranges)
                        {
                            double v = p.DistanceToPts(pts_line, false);

                            if (v > min)
                            {
                                var new_pts_1 = ParralellLine(pts_line, v);
                                var new_pts_2 = ParralellLine(pts_line, -v);

                                var new_pts = new_pts_1;

                                if (new_pts_1.IntersectPts(pts_ranges, false, false).Length == 0)
                                    new_pts = new_pts_2;

                                ACD.DB.DrawPolyline(new_pts, false, "LAYER=" + ACD.DB._getLayer(selIds[0]));
                                //ACD.DB.DrawPolyline(new_pts_2, false, "LAYER=" + ACD.DB._getLayer(selIds[0]));
                            }
                        }

                    } else
                    {
                        ObjectId fId = selIds.First();
                        string layname = ACD.DB._getLayer(fId);

                        PosCollection pls = ACD.DB.GetIdPts(fId);

                        for (int i = 1; i < selIds.Count; i++)
                            if (ACD.DB._isPolyline(selIds[i]) || ACD.DB._isHatch(selIds[i]))
                            {
                                pPos[] ls = ACD.DB._getVertices(selIds[i]);
                                //ACD.DB.CreateText((ls.Area() / 1000000).roundNumber(0.1) + "m\\U+00B2", ls.CenterPoint(), 200);
                                foreach (pPos p in pls.AllPoints)
                                {
                                    pPos[] interps = ls.Intersect(p, new pPos(p.X + 1000, p.Y));
                                    if (interps.Length == 0)
                                        interps = ls.Intersect(p, new pPos(p.X, p.Y + 1000));

                                    if (interps.Length == 0)
                                        interps = ls.Intersect(p, new pPos(p.X + 1000, p.Y + 1000));

                                    if (interps.Length > 0)
                                        ACD.DB.DrawPolyline(new pPos[] { p, interps.First() }, false, "LAYER=" + layname);
                                }
                            }
                    }
                }

                ACD.Focus();
            }
        }
    }
}

