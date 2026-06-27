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


//using System.Windows.Forms;
//using SyncObject;
using AcadScript;

namespace TextStylePurgeCLS
{
    class BlockClsId
    {
        public ObjectId Id, TextId;
        public string Type;
        public string Name;
        public bool Defined = false;
        
        public BlockClsId(ObjectId _id, ObjectId _txtid, string _blockname, bool _defined)
        {
            Id = _id;
            TextId = _txtid;
            Name = _blockname;
            //Type = _type;
            Defined = _defined;
        }

        public override string ToString()
        {
            return String.Format("BlockId {0} ({1}) has {2}({3}) not in current textstyle",
                Name, Defined ? "defined" : "block", TextId.ObjectClass.DxfName, TextId);
        }
    }
    public class ObjectLayerMergeCLS
    {
        static void CheckMLeaderTextStyle()
        {
            using (Transaction tr = ACD.DB.TransactionManager.StartTransaction())
            {
                DBDictionary mlStyles = (DBDictionary)tr.GetObject(ACD.DB.MLeaderStyleDictionaryId, OpenMode.ForRead);

                foreach (var entry in mlStyles)
                {
                    ACD.WR("MLeaderStyle {0}, {1}", entry.Key,(ObjectId) entry.Value);
                    try
                    {
                        MLeaderStyle currentStyle = (MLeaderStyle)tr.GetObject(entry.Value, OpenMode.ForWrite);
                        //ACD.WR("OK2 {0}", currentStyle);
                        currentStyle.TextStyleId = ACD.DB.Textstyle;
                    }catch(System.Exception ex)
                    {
                        ACD.WR("Error in MLeaderStyle {0}, {1}", entry.Key, (ObjectId)entry.Value);
                    }
                }

                tr.Commit();
            }
        }

        static List<string> blockClsList;
        static ObjectIdCollection attDefCls = new ObjectIdCollection();

        static void ScanInDrawing()
        {
            ACD.DB.GetEntities(null, EN_SELECT.AC_ALL);

            using (Transaction tr = ACD.DB.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in IR.SelectedIds)
                    if (ScanItem(id, tr))
                        UpdateItem(id, tr);
                tr.Commit();
            }
        }

        static bool ScanItem(ObjectId id, Transaction tr)
        {
            bool res = false;
            if (id.ObjectClass.DxfName == "TEXT")
            {
                DBText txt = (DBText)tr.GetObject(id, OpenMode.ForRead);
                if (txt.TextStyleId != ACD.DB.Textstyle)
                    res = true;
            }
            else if (id.ObjectClass.DxfName == "MTEXT")
            {
                MText txt = (MText)tr.GetObject(id, OpenMode.ForRead);
                if (txt.TextStyleId != ACD.DB.Textstyle)
                    res = true;
            }
            else if (id.ObjectClass.DxfName == "ATTRIB" || id.ObjectClass.DxfName == "ATTDEF")
            {
                AttributeDefinition att = (AttributeDefinition)tr.GetObject(id, OpenMode.ForRead);
                if (att.TextStyleId != ACD.DB.Textstyle)
                    res = true;
            }
            else if (id.ObjectClass.DxfName == "DIMENSION")
            {
                Dimension dim = (Dimension)tr.GetObject(id, OpenMode.ForRead);
                if (dim.DimensionStyle != ACD.DB.Dimstyle)
                    res = true;
            }
            else if (id.ObjectClass.DxfName == "INSERT")
                ScanBlockAttTextStyle(id);

            return res;
        }

        static void ScanInBlock()
        {
            

            using (Transaction tr = ACD.DB.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTble = tr.GetObject(ACD.DB.BlockTableId, OpenMode.ForRead) as BlockTable;

                foreach (ObjectId objId in acBlkTble)
                    if (!objId.IsErased)
                    {
                        BlockTableRecord btr = tr.GetObject(objId, OpenMode.ForRead) as BlockTableRecord;

                        if(btr.Name != "*Model_Space")
                        {
                            ObjectIdCollection ids = btr.Cast<ObjectId>().Select(id => id).ToCollection();

                            foreach (ObjectId id in ids)
                                if (ScanItem(id, tr))
                                    blockClsList.Add(btr.Name);
                        }
                    }

                tr.Commit();
            }

            blockClsList = blockClsList.Distinct().ToList();
            ACD.WR("Block has not in current textstyle {0}", blockClsList.ToTextStr());

            
            ACD.DB.GetEntities(null, EN_SELECT.AC_DXF, "INSERT");
            foreach (ObjectId id in IR.SelectedIds)
                ScanBlockAttTextStyle(id);

            ACD.WR("Attribute in Block has not in current textstyle: {0}", attDefCls.Count);
        }

        static void ScanBlockAttTextStyle(ObjectId objId)
        {
            using (Transaction tr = ACD.DB.TransactionManager.StartTransaction())
            {
                BlockReference blockRef = (BlockReference)tr.GetObject(objId, OpenMode.ForRead);
                foreach (ObjectId attId in blockRef.AttributeCollection)
                {
                    var attDef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                    if (attDef == null)
                        continue;

                    if (attDef.TextStyleId != ACD.DB.Textstyle)
                        attDefCls.Add(attId);

                    attDef.Dispose();
                }
                
                tr.Commit();
            }
        }

        static void UpdateItem(ObjectId id, Transaction tr)
        {
            if (id.ObjectClass.DxfName == "TEXT")
            {
                DBText txt = (DBText)tr.GetObject(id, OpenMode.ForWrite);

                if (txt != null)
                    txt.TextStyleId = ACD.DB.Textstyle;
            }
            else if (id.ObjectClass.DxfName == "MTEXT")
            {
                MText txt = (MText)tr.GetObject(id, OpenMode.ForWrite);
                if (txt != null)
                    txt.TextStyleId = ACD.DB.Textstyle;
            }
            //else if (id.ObjectClass.DxfName == "ATTRIB" || id.ObjectClass.DxfName == "ATTDEF")
            //{
            //    AttributeDefinition attDef = (AttributeDefinition)tr.GetObject(id, OpenMode.ForWrite);

            //    if (attDef != null)
            //    {
            //        attDef.TextStyleId = ACD.DB.Textstyle;
            //        attDef.Dispose();
            //    }
            //}
            else if (id.ObjectClass.DxfName == "DIMENSION")
            {
                Dimension dim = (Dimension)tr.GetObject(id, OpenMode.ForWrite);
                dim.DimensionStyle = ACD.DB.Dimstyle;
            }
        }

        static void UpdateAllBlockTextStyle()
        {
            using (Transaction tr = ACD.DB.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(ACD.DB.BlockTableId, OpenMode.ForWrite);

                foreach (string blockname in blockClsList)
                {
                    ObjectId blockId = bt[blockname];
                    
                    //ACD.WR("OK1 {0}", blockname);

                    if (!blockId.IsNull)
                    {
                        //ACD.WR("OK1.1 {0}", blockId.ObjectClass.DxfName);

                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(blockId, OpenMode.ForWrite);

                        //ACD.WR("OK2");
                        ObjectIdCollection ids = btr.Cast<ObjectId>().Select(id => id).ToCollection();
                        //ACD.WR("OK2.1 {0}", ids.Count);

                        foreach (ObjectId id in ids)
                            UpdateItem(id, tr);
                    }
                }
                
                foreach(ObjectId attId in attDefCls)
                {
                    var attDef = tr.GetObject(attId, OpenMode.ForWrite) as AttributeReference;
                    if (attDef != null)
                    {
                        attDef.TextStyleId = ACD.DB.Textstyle;
                        attDef.Dispose();
                    }
                }

                bt.UpgradeOpen();
                tr.Commit();
            }
        }
        
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                CheckMLeaderTextStyle();

                blockClsList = new List<string>();
                attDefCls = new ObjectIdCollection();

                ScanInBlock();
                ScanInDrawing();

                if (ACD.ED.GetInputString("Do you want change all text to current textstyle?(Y/N)").Upper() != "N")
                    UpdateAllBlockTextStyle();

                ACD.WR("Update textstyle done!");
                ACD.Focus();
            }
        }
    }
}