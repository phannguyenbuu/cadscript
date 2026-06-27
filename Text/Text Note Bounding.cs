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
    public class NoteBoundingCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                //ObjectIdCollection selIds = ACD.GetSelection();
                PosCollection pls = new PosCollection("<REGION/>");
                
                double val = ACD.ED.GetInputString("Round radius","200").ToNumber(200);

                foreach (pPos[] pts in pls)
                    //if (ACD.DB._isPolyline(id))
                    {
                        //pPos[] pts = ACD.DB._getVertices(id);
                        ACD.DB.DrawPolyline(pts.SortClockwise(), true,
                            "LAYER=A-Anno-Dims|LWIDTH=20|LTYPE=HIDDEN|ROUND=" + val);

                        //ACD.DB.EraseObjects(id);
                    }
                ACD.Focus();
            }
        }
    }
}

