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
    public class StructuralDivideUSteelCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                while (true)
                {
                    
                    {
                        ACD.Get2Points();

                        if (ACD.FirstPoint != null && ACD.LastPoint != null)
                        {
                            pPos p1 = ACD.MinPoint;
                            pPos p2 = ACD.MaxPoint;

                            pPos p1a = new pPos(p1.X + (p2.X - p1.X) * 0.25, p1.Y + (p2.Y - p1.Y) * 0.25);
                            pPos p1b = new pPos(p2.X - (p2.X - p1.X) * 0.25, p2.Y - (p2.Y - p1.Y) * 0.25);

                            //pPos v = p1 - p2;
                            //pPos dv = new pPos(-v.Y, v.X).Normalize;

                            ACD.DB.Draw2D(p1a.RectToPoint(p1b), "c");
                            //ACD.DB.Draw2D(p1b + 5000 * dv,  - 5000 * dv + p1b);
                        }
                        else
                            break;
                    }
                }
            }
            ACD.Focus();
        }
    }
}

