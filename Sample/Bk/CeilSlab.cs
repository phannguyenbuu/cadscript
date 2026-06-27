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
    public class CeilSlabCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                PosCollection pls = new PosCollection("<REGION/>");
                
                double a = "<A/>".ToNumber();
                string patA = "<PATA/>";
                double spsA = "<SPA/>".ToNumber();
                string layerA = "<LYRA/>";

                //double b = "<A/>".ToNumber();
                string patB = "<PATB/>";
                double spsB = "<SPB/>".ToNumber();
                string layerB = "<LYRB/>";

                double c = "<C/>".ToNumber();
                string patC = "<PATC/>";
                double spsC = "<SPC/>".ToNumber();
                string layerC = "<LYRC/>";

                ObjectIdCollection ids = new ObjectIdCollection();

                foreach (pPos[] pts in pls)
                {
                    PosCollection segs = pts.Explode().OrderBy(ls => -ls.Length(false).roundNumber(10)).ThenBy(ls 
                        => ls.Boundary()[0].Y.roundNumber(10)).ThenBy(ls => ls.Boundary()[0].X).ToCollectionSameClosed();

                    pPos p1 = segs.First()[0], p2 = segs.First()[1];
                    double h = 0;
                    pPos ph = null;

                    foreach (pPos pt in pts)
                    {
                        double n = pt.DistanceTo(p1, p2);
                        if (n > 1)
                        {
                            ph = pt;
                            h = n;
                            break;
                        }
                    }

                    pPos[] r = new pPos[] { p1, p2, p1.Parallel(p2, c)[1], p1.Parallel(p2, c)[0] };

                    if(ph != null && r.CenterPoint().isLeft(p1, p2) != ph.isLeft(p1, p2))
                    {
                        ph = p1;
                        p1 = p2;
                        p2 = ph;
                    }

                    double hAngle = 0;// (p2 - p1).Angle() * 180 / Math.PI ;

                    r = new pPos[] { p1,p2, p1.Parallel(p2, c)[1], p1.Parallel(p2, c)[0] };
                    ids.Add(ACD.DB.DrawPolyline(r, true, "LAYER=" + layerC));
                    ids.AddRange(ACD.DB.DrawHatch(r, "HATCH=" + patC + "|HSCALE=" + spsC + "|HANGLE=" + hAngle));
                    
                    r = new pPos[] { p1.Parallel(p2, c)[0], p1.Parallel(p2, c)[1] ,
                        p1.Parallel(p2, h - a)[1], p1.Parallel(p2, h - a)[0] };
                    ids.Add(ACD.DB.DrawPolyline(r, true, "LAYER=" + layerB));
                    ids.AddRange(ACD.DB.DrawHatch(r, "HATCH=" + patB + "|HSCALE=" + spsB + "|HANGLE=" + hAngle));

                    r = new pPos[] { p1.Parallel(p2, h - a)[0], p1.Parallel(p2, h - a)[1] ,
                        p1.Parallel(p2, h)[1], p1.Parallel(p2, h)[0] };
                    ids.Add(ACD.DB.DrawPolyline(r, true, "LAYER=" + layerC));
                    ids.AddRange(ACD.DB.DrawHatch(r, "HATCH=" + patC + "|HSCALE=" + spsC + "|HANGLE=" + hAngle));
                }

                ACD.Focus();
            }
        }
    }
}

