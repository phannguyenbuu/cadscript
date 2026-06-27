using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Runtime.InteropServices;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Internal;

//using SyncObject;

namespace AcadScript
{
    public class ArcLineCLS
    {
        static void FindArcFromSegments(
            pPos s1p1, pPos s1p2,
            pPos s2p1, pPos s2p2,
            out pPos[] rect,
            out double start_angle, out double sweep_angle,
            out pPos s1_far, out pPos s1_close,
            out pPos s2_far, out pPos s2_close)
    {
        // See where the segments intersect.
        pPos poi = null;
        bool lines_intersect, segments_intersect;
        pPos close1, close2;

        FindIntersection(s1p1, s1p2, s2p1, s2p2,
            out lines_intersect, out segments_intersect,
            out poi, out close1, out close2);
#if TEST
            LinesPoi = poi;
#endif

        // See if the lines intersect.
        if (!lines_intersect)
        {
            // The lines are parallel. Find the 180 degree arc.
            throw new NotImplementedException("The segments are parallel.");
        }

        // Find the point on each segment that is closest to the poi.
        double close_dist1, close_dist2, far_dist1, far_dist2;

        // Make s1_close be the closer of the points.
        if (s1p1.DistanceTo(poi) < s1p2.DistanceTo(poi))
        {
            s1_close = s1p1;
            s1_far = s1p2;
            close_dist1 = s1p1.DistanceTo(poi);
            far_dist1 = s1p2.DistanceTo(poi);
        }
        else
        {
            s1_close = s1p2;
            s1_far = s1p1;
            close_dist1 = s1p2.DistanceTo(poi);
            far_dist1 = s1p1.DistanceTo(poi);
        }

        // Make s2_close be the closer of the points.
        if (s2p1.DistanceTo(poi) < s2p2.DistanceTo(poi))
        {
            s2_close = s2p1;
            s2_far = s2p2;
            close_dist2 = s2p1.DistanceTo(poi);
            far_dist2 = s1p2.DistanceTo(poi);
        }
        else
        {
            s2_close = s2p2;
            s2_far = s2p1;
            close_dist2 = s2p2.DistanceTo(poi);
            far_dist2 = s1p1.DistanceTo(poi);
        }

        // See which of the close points is closer to the poi.
        if (close_dist1 < close_dist2)
        {
            // s1_close is closer to the poi than s2_close.
            // Find the point on seg2 that is distance
            // close_dist1 from the poi.
            s2_close = PointAtDistanceTo(poi, s2_far, close_dist1);
            close_dist2 = close_dist1;
        }
        else
        {
            // s2_close is closer to the poi than s1_close.
            // Find the point on seg1 that is distance
            // close_dist2 from the poi.
            s1_close = PointAtDistanceTo(poi, s1_far, close_dist2);
            close_dist1 = close_dist2;
        }

        // Find the arc.
        FindArcFromTangents(
            s1_close, s1_far,
            s2_close, s2_far,
            out rect, out start_angle, out sweep_angle);
    }

    // Find the point of intersection between
    // the lines p1 --> p2 and p3 --> p4.
    private static void FindIntersection(
        pPos p1, pPos p2, pPos p3, pPos p4,
        out bool lines_intersect, out bool segments_intersect,
        out pPos intersection,
        out pPos close_p1, out pPos close_p2)
    {
        // Get the segments' parameters.
        double dx12 = p2.X - p1.X;
        double dy12 = p2.Y - p1.Y;
        double dx34 = p4.X - p3.X;
        double dy34 = p4.Y - p3.Y;

        // Solve for t1 and t2
        double denominator = (dy12 * dx34 - dx12 * dy34);

        double t1 =
            ((p1.X - p3.X) * dy34 + (p3.Y - p1.Y) * dx34)
                / denominator;
        if (double.IsInfinity(t1))
        {
            // The lines are parallel (or close enough to it).
            lines_intersect = false;
            segments_intersect = false;
            intersection = new pPos(double.NaN, double.NaN);
            close_p1 = new pPos(double.NaN, double.NaN);
            close_p2 = new pPos(double.NaN, double.NaN);
            return;
        }
        lines_intersect = true;

        double t2 =
            ((p3.X - p1.X) * dy12 + (p1.Y - p3.Y) * dx12)
                / -denominator;

        // Find the point of intersection.
        intersection = new pPos(p1.X + dx12 * t1, p1.Y + dy12 * t1);

        // The segments intersect if t1 and t2 are between 0 and 1.
        segments_intersect =
            ((t1 >= 0) && (t1 <= 1) &&
             (t2 >= 0) && (t2 <= 1));

        // Find the closest points on the segments.
        if (t1 < 0)
        {
            t1 = 0;
        }
        else if (t1 > 1)
        {
            t1 = 1;
        }

        if (t2 < 0)
        {
            t2 = 0;
        }
        else if (t2 > 1)
        {
            t2 = 1;
        }

        close_p1 = new pPos(p1.X + dx12 * t1, p1.Y + dy12 * t1);
        close_p2 = new pPos(p3.X + dx34 * t2, p3.Y + dy34 * t2);
    }

    // Find a point on the line p1 --> p2 that
    // is distance dist from point p1.
    static pPos PointAtDistanceTo(pPos p1, pPos p2, double dist)
    {
        double dx = p2.X - p1.X;
        double dy = p2.Y - p1.Y;
        double p1p2_dist = (double)Math.Sqrt(dx * dx + dy * dy);
        return new pPos(
            p1.X + dx / p1p2_dist * dist,
            p1.Y + dy / p1p2_dist * dist);
    }

    // Find the arc that connects points s1p2 and s2p2.
    static void FindArcFromTangents(
        pPos s1_close, pPos s1_far,
        pPos s2_close, pPos s2_far,
        out pPos[] rect,
        out double start_angle, out double sweep_angle)
    {
        // Find the perpendicular lines.
        pPos perp_point1, perp_point2;

        double dx1 = s1_close.X - s1_far.X;
        double dy1 = s1_close.Y - s1_far.Y;
        perp_point1 = new pPos(
            s1_close.X - dy1,
            s1_close.Y + dx1);
#if TEST
            PerpPoint1 = perp_point1;
#endif

        double dx2 = s2_close.X - s2_far.X;
        double dy2 = s2_close.Y - s2_far.Y;
        perp_point2 = new pPos(
            s2_close.X + dy2,
            s2_close.Y - dx2);
#if TEST
            PerpPoint2 = perp_point2;
#endif

        // Find the point of intersection between segments
        // s1_close --> perp_point1 and
        // s2_close --> perp_point2.
        bool lines_intersect, segments_intersect;
        pPos poi, close_p1, close_p2;
        FindIntersection(
            s1_close, perp_point1,
            s2_close, perp_point2,
            out lines_intersect, out segments_intersect,
            out poi, out close_p1, out close_p2);
#if TEST
            PerpPoi = poi;
#endif

        // Find the radius.
        double dx = s1_close.X - poi.X;
        double dy = s1_close.Y - poi.Y;
        double radius = (double)Math.Sqrt(dx * dx + dy * dy);

        // Create the rectangle.
        rect = new pPos(poi.X - radius, poi.Y - radius).Rect(2 * radius, 2 * radius);

        // Find the start, end, and sweep angles.
        start_angle = (double)(Math.Atan2(dy, dx) * 180 / Math.PI);
        dx = s2_close.X - poi.X;
        dy = s2_close.Y - poi.Y;
        double end_angle = (double)(Math.Atan2(dy, dx) * 180 / Math.PI);

        // Make the angle less than 180 degrees.
        sweep_angle = end_angle - start_angle;
        if (sweep_angle > 180)
            sweep_angle = sweep_angle - 360;
        if (sweep_angle < -180)
            sweep_angle = 360 + sweep_angle;
    }


        static pPos[] _getArc(ObjectId id, int splitarc)
        {
            List<pPos> res = new List<pPos>();

            using (Transaction tr = ACD.DB.TransactionManager.StartTransaction())
            {
                Arc arc = (Arc)tr.GetObject(id, OpenMode.ForRead);
                //List<pPos> res = new List<pPos>();
                if (splitarc == 0)
                    splitarc = 1;

                for (int i = 0; i <= splitarc; i++)
                    res.Add(arc.GetPointAtDist(i * arc.Length / splitarc).ToPos());
            }

            return res.ToArray();
        }

        static ObjectId Arc3Points(pPos p1, pPos p2, pPos p3)
        {
            Arc arc = null;
            using (Transaction tr = ACD.DB.TransactionManager.StartTransaction())
            {
                CircularArc3d carc = new CircularArc3d(p1.ToPoint3(), p2.ToPoint3(), p3.ToPoint3());
                arc = ConvertToArc(carc);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(ACD.DB.CurrentSpaceId, OpenMode.ForWrite);
                btr.AppendEntity(arc);
                tr.AddNewlyCreatedDBObject(arc, true);
                tr.Commit();
            }

            return arc.ObjectId;
        }

        static ObjectId ArcFromSegments(pPos p1, pPos p2, pPos p3, pPos p4)
        {
            ObjectId arcId = ObjectId.Null;
            using (Transaction tr = ACD.DB.TransactionManager.StartTransaction())
            {
                pPos[] rect = null;
                pPos s1_far = null, s1_close = null, s2_far = null, s2_close = null;
                double sa, ea;

                FindArcFromSegments(p1, p2, p3, p4,
                    out rect, out sa, out ea, out s1_far, out s1_close, out s2_far, out s2_close);

                var cnt = rect.CenterPoint().ToPoint3();
                var pn = new Plane(cnt, new Vector3d(0, 0, 1));
                var ang = 0; //vec.AngleOnPlane(pn);
                var rad = rect.Size().X / 2;
                //var sa = seg.StartAngle;
                //var ea = seg.EndAngle;
                Arc arc = new Arc(cnt, new Vector3d(0, 0, 1), rad, sa + ang, ea + ang);

                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(ACD.DB.CurrentSpaceId, OpenMode.ForWrite);
                arcId = btr.AppendEntity(arc);
                tr.AddNewlyCreatedDBObject(arc, true);

                tr.Commit();
            }

            return arcId;
        }

        static Arc ConvertToArc(CircularArc3d seg)
        { 
            var cnt = seg.Center;
            var norm = seg.Normal;
            var vec = seg.ReferenceVector;
            var pn = new Plane(cnt, norm);
            var ang = vec.AngleOnPlane(pn);

            ACD.WR("Ang {0} normal [{1},{2},{3}] vec [{4},{5},{6}]", ang / Math.PI * 180, 
                norm.X,norm.Y,norm.Z, vec.X,vec.Y,vec.Z);

            var rad = seg.Radius;
            var sa = seg.StartAngle;
            var ea = seg.EndAngle;
            return new Arc(cnt, norm, rad, sa + ang, ea + ang);
        }

        static pPos[] _trimPtsByDistanceTo(pPos[] pts, double dist)
        {
            double d = 0;
            bool found = false;
            List<pPos> res = new List<pPos>();

            for(int i = 0; i < pts.Length - 1; i++)
            {
                double v = pts[i].DistanceTo(pts[i + 1]);
                if (d + v >= dist)
                {
                    res.Add(pts[i].Along(dist - d, pts[i + 1]));
                    res.AddRange(DE.NumericArray(i + 1, pts.Length - 1).Select(j => pts[j]));
                    found = true;
                    break;
                }
                else
                {
                    //res.Add(pts[i]);
                    d += v;
                }

                ACD.WR("D={0}", d);
            }

            if (!found)
            {
                res.Add(pts.Last());
                res.Add(pts[pts.Length - 2].Along(dist - d, pts[pts.Length - 1]));
            }

            return res.ToArray();
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();
                double rad = ACD.ED.GetInputString("Fillet", "5").ToNumber(5);
                pPos pt = ACD.GetPoint();
                if (rad < 100)
                {
                    pt *= 100;
                }
                //string st = "";
                PosCollection pls = new PosCollection();
                pls.Closed = new bool[0];

                foreach (ObjectId objId in selIds)
                {
                    if (ACD.DB._isArc(objId))
                    {
                        pPos[] pts = _getArc(objId, 32);
                        if (rad < 100) pts = pts.Select(p => p * 100).ToArray();

                        pls.Add(pts);
                        pts[0].Content = "Arc";

                        pls.Closed = pls.Closed.Add(false);
                    }
                    else
                    {
                        pPos[] pts = ACD.DB._getVertices(objId);

                        if (rad < 100) pts = pts.Select(p => p * 100).ToArray();

                        pls.Add(pts);

                        pls.Closed = pls.Closed.Add(ACD.DB._isPolylineClosed(objId));
                        //pts[0].Content = "Polyline";
                    }
                }

                //ACD.WR("{0}-{1}-{2}", pls.ToString(), pt, rad);

                foreach (pPos[] ls in pls)
                {
                    //ACD.WR("[{0}]", ls[0].Content);
                    //ACD.DB.DrawPolyline(ls.Select(p => p / 100));
                    bool is_first = ls.First().DistanceTo(pt) < ls.Last().DistanceTo(pt);
                    //pPos p = is_first ? ls.First() : ls.Last();

                    pPos[] seg = is_first ? new pPos[] { ls[0], ls[1] } : new pPos[] { ls.Last(), ls[ls.Length - 2] };
                    pPos prj = pt.ProjectLine(seg[0], seg[1]);

                    //pPos np = null;
                    double v = rad < 100 ? rad * 100 : rad;

                    pPos[] new_ls = _trimPtsByDistanceTo(is_first ? ls : ls.Reverse(), v);
                    
                    //pPos np = prj.Along(, seg[1]);
                    //pPos[] new_ls = ls.Select(t => t).ToArray();

                    //if (ls[0].Content == "Arc")
                    //{
                    //    ACD.DB.DrawCircle(np / 100, 2);
                    //    ACD.DB.DrawCircle(prj / 100, 5);
                    //}

                    //if (is_first) 
                    //    new_ls[0] = np;
                    //else
                    //    new_ls[new_ls.Length - 1] = np;

                    if (rad < 100)
                        new_ls = new_ls.Select(p => p / 100).ToArray();
                        
                    if (ls[0].Content == "Arc")
                        Arc3Points(new_ls[0], new_ls[1], new_ls.Last());
                    else
                        ACD.DB.DrawPolyline(new_ls, false);
                }

                pls = pls.Select(ls => ls.Select(p => p / 100).ToArray()).ToCollectionSameClosed();
                ArcFromSegments(pls[0][0], pls[0][1], pls[1][0], pls[1][1]);

                //Clipboard.SetText(st);
            }
        }
    }
}

