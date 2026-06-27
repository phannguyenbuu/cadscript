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
    public class CreateCircleBBoxCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count == 0)
                {
                    ACD.Focus();
                    return;
                }

                pPos[] bb = db._getBound(selIds);
                if (bb == null || bb.Length < 2)
                {
                    ACD.WR("Cannot calculate bounding box from selection.");
                    ACD.Focus();
                    return;
                }

                pPos sz = bb.Size();
                double radius = Math.Min(sz.X, sz.Y) / 2.0;

                if (radius <= 0 || double.IsNaN(radius) || double.IsInfinity(radius))
                {
                    ACD.WR("Invalid bounding box size.");
                    ACD.Focus();
                    return;
                }

                db.DrawCircle(bb.CenterPoint(), radius);
                db.EraseObjects(selIds);

                ACD.WR("Created inscribed circle from selection bbox (R={0}).", radius.roundNumber(0.01));
                ACD.Focus();
            }
        }
    }
}
