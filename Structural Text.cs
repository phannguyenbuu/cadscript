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
    public static class IExtensionCLS
    {
        static double SinVector(pPos p11, pPos p12, pPos p21, pPos p22)
        {
            // Find the vectors.
            pPos v1 = new pPos(p12.X - p11.X, p12.Y - p11.Y);
            pPos v2 = new pPos(p22.X - p21.X, p22.Y - p21.Y);

            // Calculate the vector lengths.
            double len1 = Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y);
            double len2 = Math.Sqrt(v2.X * v2.X + v2.Y * v2.Y);

            // Use the dot product to get the cosine.
            double dot_product = v1.X * v2.X + v1.Y * v2.Y;
            double cos = dot_product / len1 / len2;

            // Use the cross product to get the sine.
            double cross_product = v1.X * v2.Y - v1.Y * v2.X;
            return len1 == 0 || len2 == 0 ? 0 : Math.Abs( cross_product / (len1 * len2));

            // Find the angle.
            //double angle = Math.Acos(cos);
            //if (sin < 0) angle = -angle;
            //return angle;
        }

        //public static double AngleVector(pPos a, pPos b)
        //{
        //    return Math.Atan((a.Y - b.Y) / (b.X - a.X));
        //}

        public static PosCollection GetSegmentsByStraightent(this PosCollection pls)
        {
            PosCollection segments = pls.GetSegment();
            PosCollection res = new PosCollection() { segments[0] };

            for(int i = 0; i < segments.Count; i++)
            {
                double sin = SinVector(segments[i][0] , segments[i][1], 
                    res.Last()[res.Last().Length - 2] , res.Last()[res.Last().Length - 1]);

                //ACD.WR("Angle value {0}", sin);

                if (sin >= 0.1 && sin <= 0.9)
                {
                    res[res.Count - 1] = res[res.Count - 1].Add(segments[i]);
                }
                else
                {
                    res.Add(segments[i]);
                }
            }

            res = res.Select(ls => ls.ToText()).Distinct()
                .Select(s => new PosCollection(s).First()).ToCollectionSameClosed(false);
            
            //res = res.Select(ls => new pPos[] { ls.First(), ls.Last() }).ToCollectionSameClosed(false);

            foreach(pPos[] ls in res)
                ACD.DB.DrawPolyline(
                ls.Move(new pPos(100, 100)), false, "LAYER=0");

            return res;
        }
    }

    public class StructuralTextFromBeamCLS
    {
        static double range = 500;

        static void analysBlockPolylines(ObjectIdCollection ids)
        {
            PosCollection pls = ids.ToList().Where(id => ACD.DB._isPolyline(id))
                .Select(id => ACD.DB._getVertices(id)).ToCollectionSameClosed(true);

            pls.Closed = ids.ToList().Where(id => ACD.DB._isPolyline(id))
                .Select(id => ACD.DB._isPolylineClosed(id)).ToArray();

            pPos[] txts = ids.ToList().Where(id => ACD.DB._isText(id))
                .Select(id => ACD.DB._getPoint(id)).ToArray();

            var xys = pls.ExtractPtsXY(0, 0);

            pPos[] bb = pls.Boundary;

            PosCollection segments = pls.GetSegmentsByStraightent();

            ACD.DB.DrawPolyline(segments
    //GraphConsole.ResultBorders
    .Move(new pPos(100, 100)), "LAYER=0");
            ACD.WR("Sum {0}", segments.Select(seg => seg.Length).ToTextInt());
            index = 1;

            List<PosCollection> xlines = new List<PosCollection>();

            for (int axis = 0; axis < 2; axis++)
            {
                int nex = (axis + 1) % 2;
                xlines.Add(new PosCollection());

                for (int i = 0; i < xys[axis].Length; i++)
                {
                    pPos p1 = bb[0].Clone();
                    pPos p2 = bb[1].Clone();

                    p1[axis] = p2[axis] = xys[axis][i];

                    xlines[axis].Add(new pPos[] { p1, p2 });
                }
            }

            //PosCollection angleLines = segments.Where(seg => Math.Abs(seg.Last().X - seg.First().X) > 1
            //    && Math.Abs(seg.Last().Y - seg.First().Y) > 1)
            //    .Select(seg =>
            //    {
            //        pPos[] ls = seg.Select(p => p.Clone()).ToArray();
            //        ls[0] = ls[0].Along(-100, ls[1]);
            //        ls[1] = ls[1].Along(-100, ls[0]);
            //        return ls;
            //    }).ToCollectionSameClosed(false);


            //ACD.WR("OK2");
            for (int axis = 1; axis < 2; axis++)
            {
                int nex = (axis + 1) % 2;

                List<double> ls = new List<double> { xys[axis][0] };

                PosCollection new_pls;

                for (int i = 1; i < xys[axis].Length; i++)
                {
                    //ACD.WR("OK3 {0},{1},{2}", axis, segments, ls.ToArray());
                    new_pls = _computeRegion(_extendSeg(axis, segments, ls.ToArray()),
                            _connectSeg(segments, ls.ToArray(), axis),
                            _trimLine(xlines[nex], axis, ls.Min(), ls.Max()));
                    //ACD.WR("OK4");
                    if (Math.Abs(xys[axis][i] - ls.Last()) < range  
                        || ls.Count <= 1 || new_pls.Count == 0 || new_pls.AreaPls() < 200000)
                            ls.Add(xys[axis][i]);
                    else
                    {
                        //_drawSeg(new_pls, axis);
                        ls = new List<double> { xys[axis][i] };
                    }
                    //ACD.WR("OK5");
                }

                //_drawSeg(_computeRegion(_extendSeg(axis, segments, ls.ToArray()),
                //            _connectSeg(segments, ls.ToArray(), axis),
                //            _trimLine(xlines[nex], axis, ls.Min(), ls.Max())), axis);
            }
            ACD.WR("OK6");
        }

        static PosCollection _trimLine(PosCollection pls, int axis, double minvalue, double maxvalue)
        {
            return pls.Select(ls => ls.Select(p =>
            {
                pPos np = p.Clone();
                if (np[axis] < minvalue) np[axis] = minvalue;
                if (np[axis] > maxvalue) np[axis] = maxvalue;
                return np;
            }).ToArray()).ToCollectionSameClosed(false);
        }

        static PosCollection _computeRegion(params PosCollection[] extents)
        {
            PosCollection res = new PosCollection();
            //ACD.WR("OK3.0");
            foreach (PosCollection pls in extents)
                res.AddRange(pls);
            //ACD.WR("OK3.1");
            res.Closed = res.Select(l => false).ToArray();

            //ObjectIdCollection lwpIds = ACD.DB.DrawPolyline(
            //   res.Move(new pPos(100, 100)), "LAYER=0");

            //foreach (ObjectId lwpId in lwpIds)
            //    ACD.DB.SetXNotes(lwpId, "Index=L" + index);

            index++;
            //ACD.WR("OK3.2");
            //GraphConsole.treshold = 10;
            GraphConsole.Compute(res);
            
            return GraphConsole.ResultBorders.Select(ls => ls.Straighten(true,1)).Where(ls => ls.Length > 0).ToCollectionSameClosed(true);
        }

        static void _drawSeg(PosCollection selSegments, int axis)
        {
            ObjectIdCollection lwpIds = ACD.DB.DrawPolyline(
                selSegments
                //GraphConsole.ResultBorders
                .Move(new pPos(100, 100)), axis == 0 ? "LAYER=0" : "LAYER=Defpoints");

            foreach (ObjectId lwpId in lwpIds)
                ACD.DB.SetXNotes(lwpId, "Index=" + (axis == 0 ? "X" : "Y") + index);
        }

        static PosCollection _extendSeg(int axis, PosCollection segments, double[] vals)
        {
            int nex = (axis + 1) % 2;
            //double[] vals = xys[axis];
            //ACD.WR("OK E1 {0}", segments);
            var res = segments.Where(seg => seg.Length > 1 && vals.Any(v => Math.Abs(v - seg.First()[axis]) < 1
                && Math.Abs(v - seg.Last()[axis]) < 1))
                .Select(seg =>
                    {
                        //ACD.WR("OK E1.1");
                        pPos[] ls = seg.Select(p => p.Clone()).ToArray();
                        //ACD.WR("OK E1.2");
                        ls[0] = ls[0].Along(-range, ls[1]);
                        //ACD.WR("OK E1.3");
                        ls[ls.Length - 1] = ls[ls.Length - 1].Along(-range, ls[ls.Length - 2]);
                        //ACD.WR("OK E1.4 {0}", ls.ToText());
                        return ls;
                    });
            return res.ToCollectionSameClosed(false);
        }

        static int index = 0;

        static PosCollection _connectSeg(PosCollection segments, double[] vals, int axis)
        {
            PosCollection res = new PosCollection();

            int nex = (axis + 1) % 2;

            PosCollection pls = segments.Where(seg => vals.Any(v => Math.Abs(v - seg[0][axis]) < 1
                && Math.Abs(v - seg[1][axis]) < 1)).ToCollectionSameClosed(false);

            int total = pls.Count; //MUST BE

            for (int i = 0; i < total - 1; i++)
                for (int j = i + 1; j < total; j++)
                    res.AddRange(validDist(pls[i][0], pls[i][1],
                        pls[j][0], pls[j][1], range));

            res.Closed = res.Select(l => false).ToArray();

            //selSegments.AddRange(pls);

            return res;
        }
               
        static bool _dist(pPos a, pPos b)
        {
            return Math.Abs(a.X - b.X) >= 0.1 &&  Math.Abs(a.X - b.X) <= range 
                && Math.Abs(a.Y - b.Y) >= 0.1 && Math.Abs(a.Y - b.Y)  <= range;
        }

        static PosCollection validDist(pPos a, pPos b, pPos c, pPos d, double _range)
        {
            PosCollection res = new PosCollection();
            PosCollection pts = new PosCollection();
            double ran = _range * _range;
            //a = _a.X * _a.X + _a.Y * _a.Y, 
            //b = _b.X * _b.X + _b.Y * _b.Y, 
            //c = _c.X * _c.X + _c.Y + _c.Y, 
            //d = _d.X * _d.X + _d.Y + _d.Y;
            //pPos[] res = null;
            if (_dist(a, c))
                pts.Add( new pPos[] { a, c });
            if (_dist(b, d))
                pts.Add(new pPos[] { b, d });
            if (_dist(a, d))
                pts.Add(new pPos[] { a, d });
            if(_dist(b, c))
                pts.Add(new pPos[] { b, c });

            foreach (pPos[] ls in pts)
                if (ls[0].X != ls[1].X && ls[0].Y != ls[1].Y)
                    res.AddRange(ls[0].RectToPoint(ls[1]).GetSegment(true));
                else
                    res.Add(ls);

            return res;
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                //ObjectIdCollection cirIds = selIds.Cast<ObjectId>()
                //    .Where(id => ACD.DB._isCircle(id) && ACD.DB._getRadius(id) <= 20).ToCollection();

                //Dictionary<string, string> dict = new Dictionary<string, string>();
                //dict.Add(" ɸ ", "ɸ");
                //dict.Add("ɸ ", "ɸ");
                //dict.Add(" ɸ", "ɸ");
                
                //string txt = ACD.ED.GetInputString("Enter text:");
                //double txt_h = ACD.ED.GetInputString("Enter text height (200):").ToNumber(200);

                foreach (ObjectId id in selIds)
                    if(ACD.DB._isBlock(id))
                    {
                        ACD.DB.BlockEntitiesAction(id, analysBlockPolylines);
                    }

            }
            ACD.Focus();
        }
    }
}

