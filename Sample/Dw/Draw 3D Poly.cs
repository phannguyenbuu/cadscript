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

//
using System.Windows.Forms;
//using SyncObject;

namespace AcadScript
{
    public class Draw3DPolyCLS
    {
        static double angle = 25 * Math.PI / 180;
        static double dist = 1000;
        static double _dist(pPos p1, pPos p2)
        {
            return p1.DistanceTo(p2);
        }


        static void _drawBox()
        {
            using (ACD.Lock())
            {
                bool ctrlmode = ACD.ControlHold;
                string str = System.Windows.Forms.Clipboard.GetText();
                pPos pt = ACD.GetPoint();

                if (pt != null)
                {
                    string s_size = ACD.ED.GetInputString("Input size:");

                    if (!s_size.empty() && s_size.filter(",;").Length > 2)
                    {
                        string[] ar = s_size.filter(",;");
                        double[] sizes = ar.Select(s => s.ToNumber()).ToArray();

                        double a = sizes[0] * Math.Cos(angle);
                        double b = sizes[0] * Math.Sin(angle);
                        double m = sizes[1] * Math.Cos(angle);
                        double n = sizes[1] * Math.Sin(angle);

                        ACD.DB.DrawPolyline(new pPos[] { new pPos(0,0), new pPos(m, n),
                            new pPos(m - a, b + n), new pPos(-a, b)}.Move(pt), true, "LAYER=" + DE.DEF_LAYER_FIN);
                        ACD.DB.DrawPolyline(new pPos[] { new pPos(-a, b) , new pPos(-a,-sizes[2] + b),
                            new pPos(0, - sizes[2]), new pPos(m, -sizes[2] + n),
                            new pPos(m, n)}.Move(pt), false, "LAYER=" + DE.DEF_LAYER_FIN);
                        ACD.DB.DrawPolyline(new pPos[] { new pPos(0, 0),
                            new pPos(0, -sizes[2])}.Move(pt), false, "LAYER=" + DE.DEF_LAYER_FIN);

                    }
                }
                ACD.Focus();
            }

        }
        static pPos[] _planPoly(IEnumerable<pPos> pts, pPos[] bb)
        {
            //pPos[] bb = pts.Boundary();
            pPos basept = new pPos( bb[0].X, bb[1].Y);
            pPos newbasept = bb[1].Rotate(25, basept);
            
            List<pPos> res = new List<pPos>();
            int i = 0;

            foreach (pPos pt in pts)
            {
                //ACD.DB.CreateText(i.ToString(), pt, 20);
                i++;

                pPos p1 = basept.Along(pt.X - basept.X, newbasept);
                if (Math.Abs(basept.X - pt.X) < 0.1)
                    p1 = basept;

                if (p1._isVeryClosed(newbasept))
                    res.Add(newbasept.Along(basept.Y - pt.Y, 
                        (newbasept + new pPos(100,0)).Rotate(-25, newbasept)));
                else
                    res.Add(p1.Along(basept.Y - pt.Y, newbasept.Rotate(-50, p1)));
            }
            //ACD.WR(res.ToText());

            return res.ToArray();
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection ids = ACD.GetSelection();

                string all_axis = "XYZ";

                if (ids.Count > 0)
                {
                    pPos pt = ACD.GetPoint();
                    
                    if (pt != null)
                    {
                        string s_axis = ACD.ED.GetInputString("Enter Axis (X/Y/Z)");
                        string s_dist = ACD.ED.GetInputString("Extrude Amount", "");

                        dist = s_dist.ToNumber();
                        pPos[] bb = ACD.DB._getBound(ids);

                        int index = all_axis.IndexOf(s_axis.Upper());
                        
                        foreach (ObjectId id in ids)
                            if (index == 0 || index == 1)
                            {                                 
                                pPos[] pts = ACD.DB._getVertices(id, 16).Select(p => new pPos(bb[0].X
                                    + (p.X - bb[0].X) * Math.Cos(angle),
                                        p.Y - (p.X - bb[0].X) * Math.Sin(angle))).ToArray();

                                ACD.DB.DrawPolyline(pts.Move(pt - bb[0]), 
                                    ACD.DB._isPolylineClosed(id), "LAYER=" + DE.DEF_LAYER_FIN);

                                if (dist != 0)
                                    foreach (pPos p in pts)
                                        ACD.DB.DrawPolyline(new pPos[] { p, p + new pPos(dist * Math.Cos(angle),
                                    dist * Math.Sin(angle)) }.Move(pt - bb[0]), false, "LAYER=" + DE.DEF_LAYER_FIN);
                            }else
                            {
                                pPos[] pts = _planPoly(ACD.DB._getVertices(id, 16),bb);
                                ACD.DB.DrawPolyline(pts.Move(pt - bb[0]), 
                                    ACD.DB._isPolylineClosed(id), "LAYER=" + DE.DEF_LAYER_FIN);

                                int i = 0;
                                foreach (pPos p in pts)
                                {
                                    if (dist != 0)
                                        ACD.DB.DrawPolyline(new pPos[] { p, p - new pPos(0, dist) }.Move(pt - bb[0]),
                                            false, "LAYER=" + DE.DEF_LAYER_FIN);
                                    //ACD.DB.CreateText(i.ToString(), p + pt - bb[0], 20);
                                    i++;
                                }

                            }
                    }
                }

                ACD.Focus();
            }
        }
    }
}