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
    public class PolylineExtentCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                ObjectIdCollection selIds = ACD.GetSelection();
                double extent_value = ACD.ED.GetInputString("Extent value:", "1").ToNumber(1);

                foreach(ObjectId id in selIds)
                {
                    pPos[] pts = ACD.DB._getVertices(id,32);

                    if (pts.Length == 2)
                    {
                        pts[0] = pts[0].Along(-extent_value, pts[1]);
                        pts[1] = pts[1].Along(-extent_value, pts[0]);
                        ACD.DB._setPolylineVerts(id, pts);
                    }
                }

                ACD.Focus();
            }
        }
    }
}

