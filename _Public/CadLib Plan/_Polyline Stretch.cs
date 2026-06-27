using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;

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
    public class PolylineStretchCLS
    {
        static double _ptsAngle(pPos[] pts)
        {
            pPos[] line = pts.MaxSegment().OrderBy(p => p.X).ThenBy(p => p.Y).ToArray();
            return (float)(line[0] - line[1]).Angle();// / 180 * Math.PI;
        }

        static void _stretchPoints(ObjectId objId, double distance, pPos p1, pPos p2)
        {
            double angle = _ptsAngle(ACD.DB._getVertices(objId)) - 90;

            pPos[] bb = ACD.DB._getBound(objId);
            pPos basept = bb.CenterPoint();

            ACD.DB.Rotate(new ObjectIdCollection() { objId }, angle, basept);

            ObjectId newId = ACD.DB.CloneObject(objId);
            bb = ACD.DB._getBound(objId);

            pPos newp1 = p1.Rotate(angle, basept);
            pPos newp2 = p2.Rotate(angle, basept);

            ACD.DB.DrawCircle(newp1, 2);
            ACD.DB.DrawCircle(newp2, 2);

            pPos[] pts = ACD.DB._getVertices(objId);
            int ax = 0;
            for (int i = 0; i < pts.Length; i++)
                if ((p2[ax] < p1[ax] && pts[i][ax] <= p1[ax] + distance)
                    || (p2[ax] >= p1[ax] && pts[i][ax] >= p1[ax] - distance))
                {
                    ACD.DB.MoveVertex(objId, i, new pPos(0, p2.Y - p1.Y));
                    ACD.DB.MoveVertex(newId, i, new pPos(0, p2.Y - p1.Y));
                }

            ObjectId lwpId = ACD.DB.DrawPolyline(new pPos(bb[0].X, 
                p1.Y - distance).RectToPoint(new pPos(bb[1].X, p1.Y - distance)));
            //ACD.DB.Rotate(new ObjectIdCollection() { lwpId }, -angle, basept);
            ACD.DB.CloneObject(lwpId);
            ACD.DB.Rotate(new ObjectIdCollection() { objId, lwpId }, -angle, basept);

            //pPos[] bbs = pts.Rotate(-90 + angle, pts.CenterPoint()).Boundary();

            //pPos p1 = new pPos((bbs[0].X + bbs[1].X) / 2, bbs[1].Y).Rotate(-angle + 90, pts.CenterPoint());
            //pPos p2 = new pPos((bbs[0].X + bbs[1].X) / 2, bbs[0].Y).Rotate(-angle + 90, pts.CenterPoint());

            //p1 = p2.AlongRatio(p1, 1.1);
            //p2 = p1.AlongRatio(p2, 0.9);

            //pPos[] itps = pts.Intersect(p1, p2).OrderBy(p => -p.Y).ToArray();

            //return itps.Length > 0 ? itps.First().Along(distance, p2) : p1.Along(distance, p2);
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ACD.WR("Select hatch objects ...");
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    ACD.Get2Points();
                    //string st = "";

                    if (ACD.FirstPoint != null && ACD.LastPoint != null)
                    {
                        double dist = ACD.ED.GetInputString("Distance", "5").ToNumber(5);

                        foreach (ObjectId objId in selIds)
                            if (ACD.DB._isPolyline(objId))
                                _stretchPoints(objId, dist, ACD.FirstPoint, ACD.LastPoint);
                    }
                }
                //Clipboard.SetText(st);
            }
        }
    }
}

