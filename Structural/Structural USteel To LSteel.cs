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
    public class StructuralUSteelToLSteelCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                foreach (ObjectId lwpId in selIds)
                    if (ACD.DB._isPolyline(lwpId))
                    {
                        PosCollection segs = ACD.DB._getVertices(lwpId).GetSegment(false)
                            .OrderBy(seg => -seg.Length()).ToCollectionSameClosed();
                        pPos[] ls = segs.First;

                        ACD.DB.DrawStylePolyline(segs.Count > 3 ? "USTEEL" : "LSTEEL", ls, "LAYER=B-Steel");
                        ACD.DB.EraseObject(lwpId);
                    }

            }
            ACD.Focus();
        }
    }
}

