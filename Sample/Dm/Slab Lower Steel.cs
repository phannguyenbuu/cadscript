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
    public class SlabLowerSteelCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                double r = 100;
                PosCollection pls = new PosCollection("<REGION/>");

                if (pls.Count > 0)
                {
                    pls = pls.Select(ls => ls.OrderBy(p => p.X).ThenBy(p => p.Y).Weld(10).ToArray()).ToCollectionSameClosed();
                    foreach (pPos[] ls in pls)
                        if (ls.Length > 1)
                        {
                            pPos[] bb = ls.Boundary();
                                                        
                            ACD.DB.DrawStylePolyline("LSTEEL", ls, "LAYER=B-Steel");
                            ACD.DB.CreateText("#C%%C10a100",
                                new pPos(bb.CenterPoint().X, bb[1].Y + 100), 2, 0, "LAYER=B-Steel");
                        }
                }

                ACD.Focus();
            }
        }
    }
}

