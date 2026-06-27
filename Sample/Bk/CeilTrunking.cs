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
    public class CeilTrunkingCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                PosCollection pls = new PosCollection("<REGION/>");
                
                double a = "<A/>".ToNumber();
                double b = "<B/>".ToNumber();
                double c = "<C/>".ToNumber();

                ObjectIdCollection ids = new ObjectIdCollection();

                foreach (pPos[] pts in pls)
                {
                    pPos[] bb = pts.Boundary();

                    List<pPos> ls = new List<pPos> {
                        new pPos(bb[0].X + b, bb[0].Y+a), new pPos(bb[0].X, bb[0].Y+a),
                        new pPos(bb[0].X, bb[0].Y), new pPos(bb[1].X, bb[0].Y),
                        new pPos(bb[1].X, bb[0].Y+a), new pPos(bb[1].X - b, bb[0].Y+a)};

                    pPos[] inner = ls.OffsetOpen(-c);
                    if(inner.Area() > ls.Area())
                        inner = ls.OffsetOpen(c);

                    ls.AddRange(inner.Reverse());

                    ids.Add(ACD.DB.DrawPolyline(ls, true));
                }

                ACD.Focus();
            }
        }
    }
}

