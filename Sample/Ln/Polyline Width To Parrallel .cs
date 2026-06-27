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
    public class PolylineWidthToParrallelCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();
                ObjectIdCollection delIds = new ObjectIdCollection();

                foreach (ObjectId lwpId in selIds)
                    if (ACD.DB._isPolyline(lwpId))
                    {
                        double n = ACD.DB._getLineworkWidth(lwpId);
                        if (n > 0)
                        {
                            pPos[] pts = ACD.DB._getVertices(lwpId);
                            pPos p1 = pts.First();
                            pPos p2 = pts.Last();
                            List<pPos> ls = p1.Parallel(p2, n / 2).ToList();
                            ls.AddRange(p2.Parallel(p1, n / 2));

                            ObjectId newId = ACD.DB.DrawPolyline(ls, true, lwpId);
                            ACD.DB._setLineworkWidth(newId, 0);
                            delIds.Add(lwpId);
                        }
                    }

                ACD.DB.EraseObjects(delIds);

                ACD.Focus();
            }
        }
    }
}

