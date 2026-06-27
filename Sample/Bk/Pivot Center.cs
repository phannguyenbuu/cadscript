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
    public class PivotCenter
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                ObjectIdCollection selIds = ACD.GetSelection();

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    foreach (ObjectId blockId in selIds)
                        if (db._isBlock(blockId))
                        {
                            //db.SetBlockEntitXSTYLE(blockId, "LAYER=A-HIDDEN|LSCALE=1");
                            //db._setLayer(blockId, DE.DEF_LAYER_FURNITURE);

                            BlockReference block = (BlockReference)tr.GetObject(blockId, OpenMode.ForWrite);
                            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(block.BlockTableRecord, OpenMode.ForWrite);

                            ObjectIdCollection subIds = btr.Cast<ObjectId>().ToCollection();

                            pPos[] bb = ACD.DB._getBound(subIds);
                            ACD.DB.MoveObject(subIds, bb.CenterPoint().Invert);

                            btr.UpgradeOpen();

                            //db._setHyperLink(db.CollectBlock(blockId), "@REMOVEDIM");
                        }
                    tr.Commit();
                }
                ACD.ED.Regen();
                ACD.Focus();
            }
        }
    }
}
