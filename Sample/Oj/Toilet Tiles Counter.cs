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

namespace AcadScript
{
    public class ToiletCounterCLS
    {
        
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ACD.Get2Points();
                if(ACD.FirstPoint != null && ACD.LastPoint != null)
                {
                    pPos p1 = ACD.FirstPoint;
                    pPos p2 = ACD.LastPoint;

                    string content = "";
                    pPos sz = p1 - p2;
                    content = Math.Ceiling(sz.X) + "x" + Math.Ceiling(sz.Y);
                }

                ACD.Focus();
            }
        }
    }
}

