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
    public class CircleToBlockCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                db.GetEntities(ACD.bRect, EN_SELECT.AC_DXF, "CIRCLE");

                ObjectIdCollection selIds = new ObjectIdCollection();
                foreach (ObjectId circleId in IR.SelectedIds)
                {
                    string st = db._getHyperlink(circleId);
                    if (!st.empty())
                    {
                        string[] ar = st.filter(";");
                        string blockname = ar.First();

                        if (!db.HasBlock(blockname).IsNull)
                        {
                            pPos pt = db._getPoint(circleId);
                            ObjectId blockId = db.Insert(blockname, pt, "LAYER=" + DE.DEF_LAYER_FURNITURE);
                            if (ar._props("FLIP") == "ON")
                                db.MirrorObject(blockId, pt, pt + new pPos(0, 100));

                            selIds.Add(circleId);
                        }
                        else
                            ACD.WR("Block {0} not exist!", blockname);
                    }
                }
                db.EraseObjects(selIds);
                ACD.Focus();
            }
        }
    }
}

