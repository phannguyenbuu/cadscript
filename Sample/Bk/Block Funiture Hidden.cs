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
    public class BlockFurnitureHiddenCLS
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
                            db._setLayer(blockId, DE.DEF_LAYER_FURNITURE);

                            BlockReference block = (BlockReference)tr.GetObject(blockId, OpenMode.ForWrite);
                            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(block.BlockTableRecord, OpenMode.ForWrite);

                            db._setLayer(btr.Cast<ObjectId>().ToCollection(), "0");
                            //db._setLineworkLineScale(btr.Cast<ObjectId>().ToCollection(), 1);
                            ACD.DB.EraseObjects(btr.Cast<ObjectId>()
                                .Where(id => ACD.DB._isDim(id) || ACD.DB._isText(id)).Select(id => id).ToCollection());

                            foreach (ObjectId lwpId in btr.Cast<ObjectId>())
                                if (ACD.DB._isHatch(lwpId))
                                    ACD.DB._setLayer(lwpId, "A-HATCH");
                            btr.UpgradeOpen();

                            //db._setHyperLink(db.CollectBlock(blockId), "@REMOVEDIM");
                        }
                    tr.Commit();
                }

                ACD.Focus();
            }
        }
    }
}

