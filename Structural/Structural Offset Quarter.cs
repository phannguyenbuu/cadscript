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

using MessagingToolkit;
using System.Windows.Forms;
using SyncObject;

namespace AcadScript
{
    public class StructuralOffsetQuarterCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = EDSelection.GetSelection();
                ObjectIdCollection ids = new ObjectIdCollection();

                foreach (ObjectId id in selIds)
                    if (ACD.DB._isPolyline(id))
                    {
                        pPos[] pts = ACD.DB._getVertices(id);

                        pPos[] bb = pts.Boundary();
                        pPos sz = bb.Size();

                        ids.Add(ACD.DB.DrawPolyline(new pPos(bb[0].X + sz.X / 4,
                            bb[0].Y + sz.Y / 4).Rect(new pPos(bb[1].X - sz.X / 4, bb[1].Y - sz.Y / 4)), true, "LAYER=Defpoints"));
                    }
                    
                    //ACD.DB._setLayer(ids, "B-Support-Upper");
            }

            ACD.Focus();
        }
    }
}

