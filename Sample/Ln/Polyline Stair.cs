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
    public class PolylineStairCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0 && ACD.DB._isPolyline(selIds.First()))
                {
                    pPos pt = ACD.GetPoint();

                    if (pt != null)
                    {
                        ObjectIdCollection ids = new ObjectIdCollection();

                        double w = ACD.ED.GetInputString("Enter width", "1000").ToNumber(1000);
                        double step = ACD.ED.GetInputString("Enter step", "250").ToNumber(250);
                        pPos[] ls = ACD.DB._getVertices(selIds.First());

                        if (ls.First().DistanceTo(pt) > 100)
                            ls = ls.Reverse().ToArray();

                        string layname = ACD.DB._getLayer(selIds.First());
                        ids.Add(selIds.First());

                        List<pPos> pts = new List<pPos>();
                        PosCollection segs = ls.GetSegment(false);

                        for(int i = 0; i < segs.Count; i++)
                        {
                            pPos[] l1 = segs[i][0].Parallel(segs[i][1], w/2);
                            pPos[] l2 = segs[i][0].Parallel(segs[i][1], -w / 2);

                            double n = i == 0 ? 0 : (i == segs.Count - 1 ? w / 2 + (segs[i].Length() - w/2)  % step : w/2);
                            double m = i < segs.Count - 1 ? segs[i].Length() - w/2 - step: segs[i].Length() - step;
                            pts.Add(segs[i][0].Along(n, segs[i][1]));

                            ids.Add(ACD.DB.DrawPolyline(new pPos[] { pts.Last().ProjectLine(l1[0], l1[1]),
                                    pts.Last().ProjectLine(l2[0], l2[1]) }, false, "LAYER=" + layname));

                            while (n <= m + 1)
                            {
                                pts.Add(pts.Last().Along(step, segs[i][1]));

                                ids.Add(ACD.DB.DrawPolyline(new pPos[] { pts.Last().ProjectLine(l1[0], l1[1]),
                                    pts.Last().ProjectLine(l2[0], l2[1]) }, false, "LAYER=" + layname));
                                n += step;
                            }
                        }
                
                        if (ids.Count > 1)
                        {
                            ids.Add(ACD.DB.DrawArrow(ls.Last().Along(100,ls[ls.Length - 2]), ls.Last(), 200, "LAYER=" + layname));
                            ids.Add(ACD.DB.CreateText((ids.Count - 3) + "x165=" + ((ids.Count - 3) * 165), ls.First(), 100));
                            string blockname = ACD.DB.uniqueBlockName("Stair");
                            ACD.DB.NewBlock(ids, blockname, true, false, ls.First());
                            ACD.DB.Insert(blockname, ls.First());
                        }
                    }
                }
                ACD.Focus();
            }
        }
    }
}

