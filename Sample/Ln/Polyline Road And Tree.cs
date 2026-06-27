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
    public class PolylineRoadTreeCLS
    {
        static double OFFSET_SIDE = 500;
        static double DIST_RANGE = 10000;

        static pPos[] divideRoad(pPos[] ln)
        {
            List<pPos> res = new List<pPos>();
            double d = ln[0].DistanceTo(ln[1]);

            int total = (int)((d  / DIST_RANGE).roundNumber());
            double x = (d - total * DIST_RANGE) / 2;

            while(x <= d + DIST_RANGE)
            {
                res.Add(ln[0].Along(x, ln[1]));
                x += DIST_RANGE;
            }

            return res.ToArray();
        }

        static pPos[] offsetRoads()
        {
            List<pPos> res = new List<pPos>();

            for (int i = 0; i < pls.Count; i++)
            {
                ACD.WR("OK1");
                pPos p1 = pls[i][0];
                pPos p2 = pls[i][1];

                double w = widths[i];
                ACD.WR("OK2");
                //ACD.WR("Objs {0},{1},{2}",p1,p2, w);
                List<pPos> intersections = new List<pPos>();

                for (int j = 0; j < pls.Count; j++)
                    if (i != j && pls[j].Any(p => p.DistanceToPts(new pPos[] { p1, p2 }) < 100))
                    {
                        pPos p = pls[j][0].Intersect(pls[j][1], p1, p2, false);
                        if(p != null && p.DistanceTo(p1) > DIST_RANGE && p.DistanceTo(p2) > DIST_RANGE)
                            intersections.Add(p);
                    }
                ACD.WR("OK3");
                intersections = intersections.OrderBy(p => p.DistanceTo(p1)).ToList();

                List<pPos> next_pls = new List<pPos>() { p1.Along(w * 2, p2) };

                foreach (pPos p in intersections)
                {
                    if (p.Along(w * 2, p1).IsBetween(p1, p2))
                        next_pls.Add(p.Along(w * 2, p1));

                    if (p.Along(w * 2, p2).IsBetween(p1, p2))
                        next_pls.Add(p.Along(w * 2, p2));
                }
                ACD.WR("OK4");
                next_pls.Add(p2.Along(w * 2, p1));

                for (int j = 0; j < next_pls.Count; j += 2)
                {
                    //ACD.DB.CreateText("P" + j, next_pls[j], 10);
                    res.AddRange(divideRoad(next_pls[j].Parallel(next_pls[j + 1], w / 2 + OFFSET_SIDE)));
                    res.AddRange(divideRoad(next_pls[j].Parallel(next_pls[j + 1], - w / 2 - OFFSET_SIDE)));
                };
                ACD.WR("OK5");
                //res.AddRange(next_pls);
            }
            
            return res.ToArray();
        }

        static PosCollection pls = new PosCollection();
        static List<double> widths = new List<double>();

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                
                ObjectIdCollection selIds = ACD.GetSelection();

                pls = new PosCollection();
                widths = new List<double>();

                foreach (ObjectId id in selIds)
                {
                    if (ACD.DB._isPolyline(id))
                    {
                        PosCollection segs = ACD.DB._getVertices(id).GetSegment(ACD.DB._isPolylineClosed(id));
                        pls.AddRange(segs);

                        foreach(pPos[] seg in segs)
                            widths.Add(ACD.DB._getLineworkWidth(id) * 10);
                    }
                }


                foreach(pPos pt in offsetRoads())
                {
                    ObjectIdCollection cirId = ACD.DB.DrawCircle(pt, 1000);
                    ACD.DB.SetXNotes(cirId[0], "ObjectType=<Build>");
                }

                ACD.Focus();
            }
        }
    }
}

