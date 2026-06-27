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
    public class ScaleResetCLS
    {
        static private void resetScaleXToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;

                ObjectIdCollection selIds = ACD.GetSelection();

                foreach (ObjectId id in selIds)
                {
                    pPos pt = id._isDim() ? db._getVertices(id).CenterPoint() : db._getPoint(id);
                    db.MirrorObject(id, pt, pt + new pPos(100, 0));
                }

                ACD.Focus();
            }
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;

                ObjectIdCollection selIds = ACD.GetSelection();

                foreach (ObjectId id in selIds)
                {
                    pPos pt = db._getBound(id).CenterPoint();
                    db.MirrorObject(id, pt, pt + new pPos(0, 100));
                }

                ACD.Focus();
            }
        }
    }
}

