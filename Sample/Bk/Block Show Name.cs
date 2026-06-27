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
    public class BlockShowNameCLS
    {
        static public void SetDynamicBlkProperty(Database db, ObjectId id)
        {
            using (Transaction Tx = db.TransactionManager.StartTransaction())
            {
                BlockReference bref = Tx.GetObject(id, OpenMode.ForWrite) as BlockReference;

                if (bref.IsDynamicBlock)
                {
                    DynamicBlockReferencePropertyCollection props =
                        bref.DynamicBlockReferencePropertyCollection;

                    foreach (DynamicBlockReferenceProperty prop in props)
                    {
                        object[] values = prop.GetAllowedValues();
                        //Switch Property

                        if (prop.PropertyName == "Visibility" && !prop.ReadOnly)
                        {
                            if (prop.Value.ToString() == values[0].ToString())
                                prop.Value = values[1];
                            else
                                prop.Value = values[0];
                        }
                    }
                }
                Tx.Commit();
            }
        }


        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {

                string txt = ACD.ED.GetInputString("Input show name:");

                ObjectIdCollection selIds = ACD.GetSelection();


                if (txt.empty())
                {
                    foreach (ObjectId blockId in selIds)
                    {
                        if (ACD.DB._isBlock(blockId))
                        {

                            string st = ACD.DB._getIdName(blockId);
                            pPos[] bb = ACD.DB._getBound(blockId);
                            pPos sz = bb.Size();
                            ACD.DB.CreateText(st.ToUpper() + " "
                                + sz.X.roundNumber(1) + " x " + sz.Y.roundNumber(1), bb[0], 250);

                            ACD.WR(ACD.DB.GetAllDynBlockProp(blockId));
                            ACD.DB.SetDyncBlockProp(blockId, "State", "Closet");
                            ACD.DB.SetDyncBlockProp(blockId, "Length", "1800");

                        }
                    }
                }
                else
                {
                    ACD.DB.GetEntities(null, EN_SELECT.AC_DXF, "INSERT");

                    string[] blocknames = selIds.ToList().Where(id 
                        => ACD.DB._isBlock(id)).Select(id => ACD.DB._getIdName(id)).ToArray();
                    ObjectIdCollection blockIds = IR.SelectedIds.ToList().Where(id 
                        => blocknames.Contains( ACD.DB._getIdName(id))).ToCollection();

                    foreach(ObjectId blockId in blockIds)
                        ACD.DB.CreateText(txt, ACD.DB._getPoint(blockId) + new pPos(0, 200), 2, 0, "ANNNO=TRUE");
                }
            }
            ACD.Focus();
        }
    }
}

