using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices.Filters;

using System.Collections.Generic;
using System.Linq;
using System;

namespace AcadScript
{
    public class IBlockItem
    {
        public string Name;
        public pPos Pos;
        public double ScaleX = 1, ScaleY = 1, Rotation = 0;

        public IBlockItem(Database db, ObjectId id)
        {
            Name = db._getIdName(id);
            Pos = db._getPoint(id);
            pPos sc = db._getScale(id);
            ScaleX = sc.X;
            ScaleY = sc.Y;
            Rotation = db._getRotation(id);
        }
    }

    public class IBlockCollection
    {
        public List<IBlockItem> Items = new List<IBlockItem>();
        public ObjectIdCollection blockIds;
        public string BlockName;
        Database db;

        public int Count
        {
            get
            {
                return Items.Count;
            }
        }

        public IBlockCollection(Database _db, string blockname)
        {
            db = _db;
            BlockName = blockname;
            db.GetEntities(null, EN_SELECT.AC_DXF_AND_NAME, "INSERT", blockname);
            blockIds = IR.SelectedIds;
            Items = blockIds.Cast<ObjectId>().Select(id => new IBlockItem(db, id)).ToList();
        }

        public void PurgeBlock()
        {
            db.EraseObjects(blockIds);
            db.PurgeBlock();
        }

        public void CloneBlock()
        {
            if (!db.HasBlock(BlockName).IsNull)
                foreach (IBlockItem info in Items)
                {
                    ObjectId new_block = db.Insert(BlockName, info.Pos);
                    db._setRotation(new_block, info.Rotation);
                    db._setScale(new_block, info.ScaleX, info.ScaleY);
                }
        }
    }

    public static class IBlock
    {
        public static void BlockEntitiesAction(this Database db, ObjectId id, Action<ObjectIdCollection> act)
        {
            ObjectIdCollection res = ACD.DB.ExplodeEntity(id);

            //PosCollection pls = ACD.DB._getAllVertices(subIds, 32);//.Move(ACD.DB._getPoint(id));
            act(res);
            ACD.DB.EraseObjects(res);
        }


        public static void BlockEntitiesEdit(this Database db, ObjectId blockId, Action<ObjectIdCollection> act)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                BlockReference block = (BlockReference)tr.GetObject(blockId, OpenMode.ForWrite);

                if (block != null)
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(block.BlockTableRecord, OpenMode.ForWrite);
                    act(btr.Cast<ObjectId>().ToCollection());
                    btr.UpgradeOpen();
                }
                
                tr.Commit();
            }
        }

        public static PosCollection SearchBlockByName(this Database db, string key, pPos[] bb, string dxf)
        {
            PosCollection res = new PosCollection();
            
            db.SearchBlockByName(key,bb,(subIds) => {
                if (dxf == "DIMENSION")
                    res.AddRange(subIds.ToList().Where(sid
                            => ACD.DB._isDim(sid)).Select(sid
                            => {
                                pPos p1 = ACD.DB._getPoint(sid, 0);
                                p1.Content = ACD.DB._getContent(sid);
                                return new pPos[] { p1, ACD.DB._getPoint(sid, 1) };
                            }));
                else
                    res.Add(subIds.ToList().Where(sid
                            => db._isText(sid)).Select(sid => db._getPoint(sid)).ToArray());
            });

            return res;
        }

        public static void SearchBlockByName(this Database db, string key, pPos[] bb, Action<ObjectIdCollection> act)
        {
            string[] blocknames = db.ListBlock().Where(s => s.st_(key.Upper())).ToArray();
            db.GetEntities(null, EN_SELECT.AC_DXF, "INSERT");

            foreach (ObjectId id in IR.SelectedIds)
                if (blocknames.Contains(db._getIdName(id)) && db._getPoint(id).InsideRect(bb[0],bb[1]))
                {
                    ACD.DB.BlockEntitiesAction(id, (subIds) =>
                    {
                        act(subIds);
                        //res.AddRange(subIds.ToList().Where(sid
                            //=> ACD.DB._isDim(sid)).Select(sid
                            //=> new pPos[] { ACD.DB._getPoint(sid, 0), ACD.DB._getPoint(sid, 1) }));
                    });
                }
        }

        public static ObjectIdCollection ExplodeBlock(this Database db, ObjectId id, bool erase = false)
        {
            var res = new ObjectIdCollection();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var br = (BlockReference)tr.GetObject(id, OpenMode.ForRead);

                ObjectEventHandler handler = (s, e) =>
                {
                    res.Add(e.DBObject.ObjectId);
                };

                db.ObjectAppended += handler;
                br.ExplodeToOwnerSpace();
                db.ObjectAppended -= handler;

                if (erase)
                {
                    br.UpgradeOpen();
                    br.Erase();
                    br.DowngradeOpen();
                }

                tr.Commit();
            }

            return res;
        }

        public static void PurgeBlock(this Database db)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);

                foreach (ObjectId oid in bt)
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(oid, OpenMode.ForWrite);

                    if (btr.GetBlockReferenceIds(false, false).Count == 0 && !btr.IsLayout)
                        btr.Erase();
                }

                tr.Commit();
            }
        }

        public static ObjectId SetBlockEntityStyle(this Database db, ObjectId objId, string prop)//, ObjectIdCollection ids, pPos pt)
        {
            ObjectId res = ObjectId.Null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                BlockReference block = (BlockReference)tr.GetObject(objId, OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(block.BlockTableRecord, OpenMode.ForWrite);

                foreach (ObjectId id in btr)
                    if(db._getLayer(id).Upper() != "DEFPOINTS")
                        db._setIdInfo(id, prop);

                btr.UpgradeOpen();
                tr.Commit();
            }

            //db._setHyperLink(db.CollectBlock(objId), prop);
            return res;
        }

        public static ObjectId CreateBlock(this Database db, string blockName)//, ObjectIdCollection ids, pPos pt)
        {
            ObjectId res = ObjectId.Null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                BlockTableRecord btr = new BlockTableRecord();
                btr.Name = blockName;
                bt.UpgradeOpen();

                res = bt.Add(btr);
                tr.AddNewlyCreatedDBObject(btr, true);
                tr.Commit();
            }

            return res;
        }

        public static ObjectIdCollection NewBlock(this Database db, ObjectIdCollection ids, string blockName,
            bool _eraseObjects = true, bool furniture_to_hidden = false, pPos default_pt = null)
        {
            if (!db.HasBlock(blockName).IsNull)
            {
                db.GetEntities(null, EN_SELECT.AC_DXF_AND_NAME, "INSERT", blockName);
                db.EraseObjects(IR.SelectedIds);
                db.PurgeBlock();
            }

            //db.WR("BLOCK04");
            ObjectIdCollection res = new ObjectIdCollection();
            ObjectId blockId = db.CreateBlock(blockName);

            pPos pt = default_pt != null ? default_pt : db._getBound(ids).Centroid();

            //db.WR("BLOCK06");
            db.MoveObject(ids.ToList().Where(id => !db._isDim(id)).ToCollection(), pt.Invert);

            foreach(ObjectId id in ids)
                if(db._isDim(id))
                    for (int i = 0; i < 3; i++)
                        db._setPoint(id, db._getPoint(id, i) - pt, i);
                
            IdMapping mapping = new IdMapping();
            db.DeepCloneObjects(ids, blockId, mapping, false);

            foreach (ObjectId id in ids)
                if (mapping[id].IsCloned)
                    ACD.DB._updateEntity(mapping[id].Value);
                
            db.EraseObjects(ids);

            res.Add(db.Insert(blockName, pt));
            //db.WR("\nCreated block named \"{0}\" containing {1} objects\n", blockName, ids.Count);
            return res;
        }

        public static string RenameBlock(this Database db, string blockName, string newName = null)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[blockName], OpenMode.ForWrite);
                if(newName.empty())
                    newName = blockName + btr.ObjectId.ToString();
                btr.Name = newName;
                tr.Commit();
            }

            return newName;
        }
        
        public static void ReplaceBlock(this Database db, string blockName, string fromBlockName)
        {
            if(!db.HasBlock(blockName).IsNull)
            {
                db.GetEntities(null, EN_SELECT.AC_DXF, "INSERT");

                foreach (ObjectId blockId in IR.SelectedIds)
                    db.ReplaceBlock(blockId, blockName, fromBlockName);
            }
            
            db.PurgeBlock();
        }

        public static ObjectIdCollection CollectBlock(this Database db, ObjectIdCollection ids, string blockName)
        {
            //ObjectIdCollection res = new ObjectIdCollection();
            //db.GetEntities(null, EN_SELECT.AC_DXF_AND_NAME, "INSERT", blockName);

            return ids.Cast<ObjectId>().Where(id => db._isBlock(id) && db._getIdName(id) == blockName).ToCollection();
        }


        public static ObjectIdCollection CollectBlock(this Database db, ObjectId blockId)
        {
            return db.CollectBlock(db._getIdName(blockId));
        }

        public static ObjectIdCollection CollectBlock(this Database db, string blockName)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            db.GetEntities(null, EN_SELECT.AC_DXF_AND_NAME, "INSERT", blockName);

            return IR.SelectedIds;
        }

        //public static pPos[] BlockPointsBounding(this Database db, ObjectId blockId)
        //{
        //    PosCollection pls = new PosCollection();

        //    db.BlockEntitiesAction(blockId, (ids) =>
        //    {
        //        pls = ids.ToList().Where(id => db._isVertice(id)).Select(id => db._getVertices(id)).ToCollectionSameClosed(true);
        //    });
        //    return pls.SelfIntersect.Boundary();
        //}

        public static void CropInBlock(this Database db, ObjectId blockId, IEnumerable<pPos> pts)
        {
            //using (Transaction tr = db.TransactionManager.StartTransaction())
            //{
            //    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
            //    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[blockName], OpenMode.ForWrite);

            //        ObjectIdCollection delIds = db.GetEntInBlock(blockId);
            //        db.EraseObjects(delIds);

            //        foreach (ObjectId id in ids)
            //        {
            //            Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
            //            btr.AppendEntity(ent);
            //        }
            
            //    tr.Commit();
            //}
        }

        public static void SetEntInBlock(this Database db, string blockName, ObjectIdCollection ids, bool append = false)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;

                if (bt.Has(blockName))
                {
                    ObjectId blockId = bt[blockName];
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[blockName], OpenMode.ForWrite);

                    //db.WR("SET1");
                    if (!append)
                    {
                        db.EraseObjects(db.GetEntInBlockByName(blockName));
                    }
                    foreach (ObjectId id in ids)
                    {
                        //db.WR("SET ITM {0} - {1}", blockName, id);

                        Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                        Entity clone = ent.Clone() as Entity;

                        btr.AppendEntity(clone);
                        tr.AddNewlyCreatedDBObject(clone, true);

                        //ent.Erase();

                        //btr.AppendEntity((Entity)tr.GetObject(id, OpenMode.ForWrite));
                    }

                    //db.WR("SET3");
                }

                tr.Commit();
            }
        }
        public static void SetEntInBlock(this Database db, ObjectId blockId, ObjectIdCollection ids, bool append = false)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[db._getIdName(blockId)], OpenMode.ForWrite);

                db.MoveObject(ids, db._getPoint(blockId).Invert);
                //db.WR("S2");
                if (!append)
                    db.EraseObjects(db.GetEntInBlockByName(db._getIdName(blockId)));

                foreach (ObjectId id in ids)
                {
                    Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                    Entity clone = ent.Clone() as Entity;

                    btr.AppendEntity(clone);
                    tr.AddNewlyCreatedDBObject(clone, true);
                }

                db.EraseObjects(ids);
                tr.Commit();
            }
        }

        public static void ReplaceBlock(this Database db, ObjectId blockId, string blockName, string fromBlockName)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;

                if (bt.Has(fromBlockName))
                {
                    BlockReference block = tr.GetObject(blockId, OpenMode.ForWrite) as BlockReference;

                    if (db._getIdName(blockId) == blockName)
                        block.BlockTableRecord = bt[fromBlockName];
                    else
                    {
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(block.BlockTableRecord, OpenMode.ForWrite);

                        foreach (ObjectId id in btr)
                            if (db._isBlock(id) && db._getIdName(id) == blockName)
                            {
                                BlockReference blockref = tr.GetObject(id, OpenMode.ForWrite) as BlockReference;
                                blockref.UpgradeOpen();
                                blockref.BlockTableRecord = bt[fromBlockName];
                            }
                    }
                }

                tr.Commit();
            }

            string st = db._getHyperlink(blockId);
            if (!st.empty())
                db._setIdInfo(blockId, st);
        }

        public static char[] ValidPrefix = new char[] { 'A', 'G', 'U', 'I', 'F', 'L' };

        static public void ReadDyncBlockTable(Database db, ObjectId blockRefId)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // open the dynamic block reference
                BlockReference blockRef = tr.GetObject(blockRefId, OpenMode.ForRead) as BlockReference;
                if (blockRef.IsDynamicBlock)
                {
                    //db.WR("DYNAMIC BLOCK");
                    // get the dynamic block table definition
                    BlockTableRecord blockDef = tr.GetObject(blockRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;

                    // open the extension dictionary
                    if (blockDef.ExtensionDictionary.IsNull)
                        return;
                    DBDictionary extDic = tr.GetObject(blockDef.ExtensionDictionary, OpenMode.ForRead) as DBDictionary;

                    // open the ENHANCEDBLOCK dictionary
                    Autodesk.AutoCAD.Internal.DatabaseServices.EvalGraph graph =
                        tr.GetObject(extDic.GetAt("ACAD_ENHANCEDBLOCK"), OpenMode.ForRead)
                        as Autodesk.AutoCAD.Internal.DatabaseServices.EvalGraph;

                    int[] nodeIds = graph.GetAllNodes();

                    //db.WR("NODE_IDS {0}", nodeIds.Length);
                    foreach (uint nodeId in nodeIds)
                    {
                        // open the node ID
                        DBObject node = graph.GetNode(nodeId, OpenMode.ForRead, tr);
                        // check is if the correct type

                        //db.WR("NODE_IS_BlockPropertiesTable {0}", node is BlockPropertiesTable);
                        if (!(node is BlockPropertiesTable))
                            continue;
                        // convert the object
                        BlockPropertiesTable table = node as BlockPropertiesTable;

                        // ok, we have the data, let's show it...

                        // get the number of columns
                        int columns = table.Columns.Count;
                        int currentRow = 0;
                        //db.WR("NODE_ROWS {0}", table.Rows.Count);
                        foreach (BlockPropertiesTableRow row in table.Rows)
                        {
                            //ed.WriteMessage("\n[{0}]:\t", currentRow);
                            for (int currentColumn = 0; currentColumn < columns; currentColumn++)
                            {
                                // get the colum value for the
                                // current row. May be more multiple
                                TypedValue[] columnValue = row[currentColumn].AsArray();
                                //foreach (TypedValue tpVal in columnValue)
                                //    db.WR("{0}; ", tpVal.Value);

                                //db.WR("|");
                            }
                            currentRow++;
                        }
                    }
                }
                tr.Commit();
            }
        }

        public static int BlockCount(this Database db, string blockname)
        {
            db.GetEntities(null, EN_SELECT.AC_DXF_AND_NAME, "INSERT", blockname);
            return IR.SelectedIds.Count;
        }

        /*public static void UpdateClassification(this Database db, ObjectIdCollection selIds = null, pPos[] region = null)
        {
            if(selIds == null || selIds.Count == 0)
                BlockNames = db.ListBlock();
            else
            {
                List<string> tmps = new List<string>();
                foreach(ObjectId id in selIds)
                {
                    string stname = db._getIdName(id);
                    if(! tmps.Contains(stname)) tmps.Add(stname);
                }
                BlockNames = tmps.ToArray();
            }
 
            BlockCounts = new int[BlockNames.Length];
            BlockScale = new PosCollection();
            BlockRotation = new List<double[]>();
            BlockPosition = new PosCollection();
 
            for (int i = 0; i < BlockNames.Length; i++)
            {
                List<pPos> ls = new List<pPos>();
                List<pPos> scales = new List<pPos>();
                List<double> rots = new List<double>();
 
                ObjectIdCollection ids = db._getBlockCount(BlockNames[i], region);
                if (ids.Count > 0)
                {
                    BlockCounts[i] = ids.Count;
                     
                    foreach (ObjectId id in ids)
                    {
                        ls.Add(db._getPoint(id));
                        rots.Add(db._getRotation(id));
                        scales.Add(db._getScale(id));
                    }
                }
 
                CenterPoint = ls.CenterPoint();
                //for (int j = 0; j < ls.Count; j++) ls[j] = ls[j] - pt_center).Round;
 
                BlockPosition.Add(ls.ToArray());
                BlockRotation.Add(rots.ToArray());
                BlockScale.Add(scales.ToArray());
            }
        }*/

        public static void StandardAllBlocks(this Database db)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTble = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                foreach (ObjectId objId in acBlkTble)
                {
                    BlockTableRecord btrec = tr.GetObject(objId, OpenMode.ForRead) as BlockTableRecord;
                    //BlockReference broc = (BlockReference)tr.GetObject(blockId, OpenMode.ForWrite);

                    //BlockTableRecord btrec = tr.GetObject(broc.BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                    // prevent work with attributes and embedded blocks
                    /*if (selent.ObjectId.ObjectClass.IsDerivedFrom(RXClass.GetClass(typeof(AttributeReference))) |
                        selent.ObjectId.ObjectClass.IsDerivedFrom(RXClass.GetClass(typeof(BlockReference))))
                        return;
                    if (selent.ObjectId.ObjectClass.IsDerivedFrom(RXClass.GetClass(typeof(DBText))))
                    {*/

                    if (!btrec.IsLayout)
                        foreach (ObjectId id in btrec)
                        {
                            if (!db._getLayer(id).StartsWith("A-"))
                                db._setLayer(id, "0");
                            db._setCurrentStyle(id);
                        }
                }
                tr.Commit();
            }
        }

        /*public static void ExplodeEntity(this Database db, ObjectIdCollection ids)
        {
            foreach (ObjectId id in ids) ExplodeEntity(db, id);
        }
 
        public static void ExplodeEntity(this Database db, ObjectId objId)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
 
                try
                {
                    Entity blockRef = (Entity)tr.GetObject(objId, OpenMode.ForWrite, true);
                    if (blockRef != null)
                    {
                        DBObjectCollection objs = new DBObjectCollection();
                        blockRef.Explode(objs);
                     
                        foreach (Entity obj in objs)
                        {
                            btr.AppendEntity(obj);
                            tr.AddNewlyCreatedDBObject(obj,true);
                        }
                        blockRef.Erase();
                    }
                }
                catch (System.Exception ex)
                {
                    db.WR("Error in id {0}, type{1}\n", objId, objId.ObjectClass.DxfName);
                }
                tr.Commit();
            }
        }*/

        public static void SetBlockAtt(this Database db, ObjectId objId, string AttName, string value)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (!value.empty())
                {
                    BlockReference blockRef = (BlockReference)tr.GetObject(objId, OpenMode.ForWrite);
                    foreach (ObjectId attId in blockRef.AttributeCollection)
                    {
                        var attDef = tr.GetObject(attId, OpenMode.ForWrite) as AttributeReference;
                        if (attDef == null)
                            continue;

                        if (AttName.Upper() == attDef.Tag.Upper())
                            if (attDef.IsMTextAttribute)
                                attDef.MTextAttribute.Contents = value;
                            else
                                attDef.TextString = value;

                        attDef.Dispose();
                    }
                }
                tr.Commit();
            }
        }

        public static void SetBlockAtt(this Database db, ObjectIdCollection ids, string AttName, string[] values)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                for (int i = 0; i < ids.Count; i++)
                {
                    ObjectId objId = ids[i];
                    BlockReference blockRef = (BlockReference)tr.GetObject(objId, OpenMode.ForWrite);
                    foreach (ObjectId attId in blockRef.AttributeCollection)
                    {
                        var attDef = tr.GetObject(attId, OpenMode.ForWrite) as AttributeReference;
                        if (attDef == null)
                            continue;

                        if (AttName.Upper() == attDef.Tag.Upper())
                            if (attDef.IsMTextAttribute)
                                attDef.MTextAttribute.Contents = values[i];
                            else
                                attDef.TextString = values[i];

                        attDef.Dispose();
                    }
                }
                tr.Commit();
            }
        }

        public static string GetAllBlockAtt(this Database db, ObjectId objId)
        {
            string res = "";
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference blockRef = (BlockReference)tr.GetObject(objId, OpenMode.ForRead);
                foreach (ObjectId attId in blockRef.AttributeCollection)
                {
                    var attDef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                    if (attDef == null)
                        continue;
                                        
                        res += "|" + attDef.Tag.Upper() + "="
                            + (attDef.IsMTextAttribute ? attDef.MTextAttribute.Text : attDef.TextString);


                    attDef.Dispose();
                }

                tr.Commit();
            }

            return res;//.UniConvert();
        }

        public static string GetBlockAtt(this Database db, ObjectId objId, string AttName = null)
        {
            string res = "";
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (objId.ObjectClass.DxfName == "INSERT")
                {
                    BlockReference blockRef = (BlockReference)tr.GetObject(objId, OpenMode.ForRead);
                    foreach (ObjectId attId in blockRef.AttributeCollection)
                    {
                        var attDef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                        if (attDef == null)
                            continue;

                        if (!AttName.empty())
                        {
                            if (AttName.Upper() == attDef.Tag.Upper())
                                res = attDef.IsMTextAttribute ? attDef.MTextAttribute.Text : attDef.TextString;
                        }
                        else
                        {
                            res += "|" + attDef.Tag.Upper() + "="
                                + (attDef.IsMTextAttribute ? attDef.MTextAttribute.Text : attDef.TextString);
                        }
                        attDef.Dispose();
                    }
                }
                else if (objId.ObjectClass.DxfName == "MULTILEADER")
                {
                    MLeader leader = (MLeader)tr.GetObject(objId, OpenMode.ForRead);

                    if (!leader.BlockContentId.IsNull)
                    {
                        BlockTableRecord br = (BlockTableRecord)tr.GetObject(leader.BlockContentId, OpenMode.ForRead);

                        foreach (ObjectId id in br)
                        {
                            AttributeDefinition adef = tr.GetObject(id, OpenMode.ForRead) as AttributeDefinition;

                            if (adef != null)
                            {
                                AttributeReference aref = leader.GetBlockAttribute(id);

                                //read attribute value
                                string val = aref.TextString;
                                //db.WR("OK2 {0}", attDef);
                                if (!AttName.empty())
                                {
                                    if (AttName.Upper() == adef.Tag.Upper())
                                        res = aref.TextString;
                                }
                                else
                                    res += adef.Tag.Upper() + "=" + aref.TextString + "|";
                            }
                        }
                    }
                }

                tr.Commit();
            }

            return res.EndsWith("|") ? res.Remove(res.Length - 1) : res;
        }

        /*static public ObjectIdCollection _getBlockCount(this Database db, ObjectId objId)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            pPos objpt = db._getPoint(objId);
            KVL kitm = db._getBestFitRegion(objpt);
             
            string blkName = db._getIdName(objId);
 
            db.GetEntities(ACD.bRect, EN_SELECT.AC_DXF, "INSERT");
            foreach (ObjectId id in IR.SelectedIds)
                if (db._getIdName(id) == blkName)
                {
                    pPos pt =  db._getPoint(id);
                    if (kitm == db._getBestFitRegion(pt)) res.Add(id);
                }
            return res;
        }
 
        static public ObjectIdCollection _getBlockCount(this Database db, string blkName, pPos[] region = null)
        {
            ObjectIdCollection res = new ObjectIdCollection();
 
            db.GetEntities(region, EN_SELECT.AC_DXF, "INSERT");
            foreach (ObjectId id in IR.SelectedIds)
                if (db._getIdName(id) == blkName)
                    res.Add(id);
 
            return res;
        }*/

        
        public static object GetDynBlockProp(this Database db, ObjectId blockID, string propname)
        {
            object res = null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference block = (BlockReference)tr.GetObject(blockID, OpenMode.ForRead);
                if ((block != null) && (block.IsDynamicBlock))
                {
                    DynamicBlockReferencePropertyCollection pc = block.DynamicBlockReferencePropertyCollection;
                    // Loop through, getting the info for each property           
                    foreach (DynamicBlockReferenceProperty prop in pc)
                        if (prop.PropertyName.Upper() == propname.Upper())
                            res = prop.Value;

                    pc.Dispose();
                }
                tr.Commit();
            }
            return res;
        }

        public static string[] ListDynBlockProp(this Database db, ObjectId blockID, string propname)
        {
            List<string> res = new List<string>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference br = (BlockReference)tr.GetObject(blockID, OpenMode.ForRead);
                if ((br != null) && (br.IsDynamicBlock))
                {
                    //ed.WriteMessage("\nDynamic properties for \"{0}\"\n", name);
                    // Get the dynamic block//s property collection           
                    DynamicBlockReferencePropertyCollection pc = br.DynamicBlockReferencePropertyCollection;
                    // Loop through, getting the info for each property           
                    foreach (DynamicBlockReferenceProperty prop in pc)
                        res.Add(prop.PropertyName);
                    pc.Dispose();
                }
                tr.Commit();
            }

            return res.ToArray();
        }

        public static void SetDynBlockProp(this Database db, ObjectId blockID, string propname, object val)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference br = (BlockReference)tr.GetObject(blockID, OpenMode.ForRead);
                if ((br != null) && (br.IsDynamicBlock))
                {
                    //ed.WriteMessage("\nDynamic properties for \"{0}\"\n", name);
                    // Get the dynamic block//s property collection           
                    DynamicBlockReferencePropertyCollection pc = br.DynamicBlockReferencePropertyCollection;
                    // Loop through, getting the info for each property           
                    foreach (DynamicBlockReferenceProperty prop in pc)
                        if (prop.PropertyName.Upper() == propname.Upper() && !prop.ReadOnly)
                        {
                            try
                            {
                                //db.WR("\nCurrent value {0}:{1} new value:{2}", prop.PropertyName, prop.Value, val);
                                prop.Value = val;
                            }
                            catch (SystemException ex)
                            {
                                //db.WR("\nException: {0}", ex.Message);
                            }
                            break;
                        }

                    pc.Dispose();
                }
                tr.Commit();
            }
        }

        public static void SetDynBlockState(this Database db, ObjectId blockID, string prams)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {

                BlockReference br = (BlockReference)tr.GetObject(blockID, OpenMode.ForRead);
                if ((br != null) && (br.IsDynamicBlock))
                {
                    //ed.WriteMessage("\nDynamic properties for \"{0}\"\n", name);
                    // Get the dynamic block//s property collection           
                    DynamicBlockReferencePropertyCollection pc = br.DynamicBlockReferencePropertyCollection;
                    // Loop through, getting the info for each property     

                    string[] propnames = prams._allPropNames();

                    foreach (DynamicBlockReferenceProperty prop in pc)
                    {

                        {
                            int index = Array.FindIndex(propnames, s => s.Upper() == prop.PropertyName.Upper());
                            if (index != -1)
                            {
                                string val = prams._prop(propnames[index]);

                                if (prop.PropertyName.Upper() == "STATE" && !prop.ReadOnly)
                                {
                                    object[] values = prop.GetAllowedValues();
                                    try
                                    {
                                        foreach (object obj in values)
                                        {
                                            string st = obj.ToString();
                                            if (st.Upper().Contains(val.Upper()))
                                            {
                                                prop.Value = st;
                                                break;
                                            }
                                        }
                                        //db.WR("\nCurrent value {0}:{1} new value:{2}", prop.PropertyName, prop.Value, val);
                                        prop.Value = val;
                                    }
                                    catch (SystemException ex)
                                    {
                                        //db.WR("\nException: {0}", ex.Message);
                                    }
                                    //break;
                                }
                                else if (prop.UnitsType == DynamicBlockReferencePropertyUnitsType.Distance)
                                    prop.Value = val.ToNumber();
                                else
                                    prop.Value = val;
                            }
                        }
                    }

                    pc.Dispose();
                }
                tr.Commit();
            }
        }

        //public static void SetDynBlockState(this Database db, ObjectId blockID, string val)
        //{
        //    using (Transaction tr = db.TransactionManager.StartTransaction())
        //    {

        //        BlockReference br = (BlockReference)tr.GetObject(blockID, OpenMode.ForRead);
        //        if ((br != null) && (br.IsDynamicBlock))
        //        {
        //            //ed.WriteMessage("\nDynamic properties for \"{0}\"\n", name);
        //            // Get the dynamic block//s property collection           
        //            DynamicBlockReferencePropertyCollection pc = br.DynamicBlockReferencePropertyCollection;
        //            // Loop through, getting the info for each property           
        //            foreach (DynamicBlockReferenceProperty prop in pc)
        //                if (prop.PropertyName.Upper() == "STATE" && !prop.ReadOnly)
        //                {
        //                    object[] values = prop.GetAllowedValues();
        //                    try
        //                    {
        //                        foreach (object obj in values)
        //                        {
        //                            string st = obj.ToString();
        //                            if (st.Upper().Contains(val.Upper()))
        //                            {
        //                                prop.Value = st;
        //                                break;
        //                            }
        //                        }
        //                        //db.WR("\nCurrent value {0}:{1} new value:{2}", prop.PropertyName, prop.Value, val);
        //                        prop.Value = val;
        //                    }
        //                    catch (SystemException ex)
        //                    {
        //                        db.WR("\nException: {0}", ex.Message);
        //                    }
        //                    break;
        //                }

        //            pc.Dispose();
        //        }
        //        tr.Commit();
        //    }
        //}

        public static string LayerInfo, Hyperlink, BlockInfo;
        public static string[] AttName, AttValue;
        public static string SourceFileName;
        public static string LayoutName;
        public static ObjectId HasLayout(this Database db, string LayoutName)
        {
            List<string> ar = new List<string>();
            ObjectId res = ObjectId.Null;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBDictionary LayoutDict = tr.GetObject(db.LayoutDictionaryId, OpenMode.ForWrite) as DBDictionary;

                foreach (DBDictionaryEntry de in LayoutDict)
                    if (de.Key.Upper() == LayoutName.Upper())
                    {
                        res = de.Value;
                        break;
                    }

                tr.Commit();
            }

            return res;
        }
        
        static string stringTab(int num)
        {
            string res = "";
            for (int i = 0; i < num; i++)
                res += " ";
            return res;
        }

        /*public static void CreateDAEBoxes(this Database db, string samplefile, string daeName, pPos[] pts, double[] rots)
        {
            if(!File.Exists(samplefile))
            {
                db.WR("Sample file {0} not exists, use default {1}!", samplefile, DE.SAMPLE_BOX_COLLADA);
                samplefile = DE.SAMPLE_BOX_COLLADA;
            }
 
            if(File.Exists(samplefile))
            {
                List<string> lines = File.ReadAllLines(DE.SAMPLE_BOX_COLLADA).ToList();
                int index = lines.FindIndex(line => line.Contains("<visual_scene"));
 
                if (index != -1)
                {
                    for (int i = 0; i < pts.Length; i++)
                    {
                        pPos pt = (new pPos(pts[i].X, -pts[i].Y, pts[i].Z) - IBlock.CenterPoint).Round;
                        Matrix3d mtx = pPosExtension.TransformMat(pt, rots[i], 1);
 
                        lines.Insert(index + 1, stringTab(4) + "<node>");
                        lines.Insert(index + 2, stringTab(8) + mtx.MatToString());
                        lines.Insert(index + 3, stringTab(8) + "<instance_geometry url=\"#Box001-lib\"/>");
                        lines.Insert(index + 4, stringTab(4) + "</node>");
                        lines.Insert(index + 5, "");
                    }
 
                    string fname = DE.COLLADA_DIR + daeName + ".dae";
                    db.WR("Successful, in line {0} <{1}> built {2} item(s)", index, fname, pts.Length);
 
                    Directory.CreateDirectory(DE.COLLADA_DIR);
                    File.WriteAllLines(fname, lines);
                    System.Windows.Forms.Clipboard.SetText(fname);
                } else
                    db.WR("Cannot find key \"<visual_scene\" in sample file {0}!", samplefile);
            } else
                db.WR("Sample file {0} not exists!", samplefile);
        }
 
        public static ObjectId CreateSolid(this Database db, pPos pt, pPos size, double rot)
        {
            ObjectId res = ObjectId.Null;
 
            using (Transaction acTrans = db.TransactionManager.StartTransaction())
            {
                // Open the Block table record for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(db.BlockTableId,
                                                OpenMode.ForRead) as BlockTable;
 
                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                OpenMode.ForWrite) as BlockTableRecord;
 
                // Create a 3D solid wedge
                using (Solid3d acSol3D = new Solid3d())
                {
                    acSol3D.CreateWedge(size.X, size.Y, size.Z);
                    //acSol3D.Ro
                    // Position the center of the 3D solid at (5,5,0) 
                    acSol3D.TransformBy(Matrix3d.Displacement(new Point3d(pt.X, pt.Y, 0) - Point3d.Origin));
 
                    // Add the new object to the block table record and the transaction
                    acBlkTblRec.AppendEntity(acSol3D);
                    acTrans.AddNewlyCreatedDBObject(acSol3D, true);
 
                    res = acSol3D.ObjectId;
                }
                acTrans.Commit();
            }
 
            return res;
        }
 
        static public ObjectIdCollection Insert(this Database db, string blockName,  string align_code = null)
        {
            ObjectIdCollection res = new ObjectIdCollection();
 
             List<pPos> pts = new List<pPos>();
 
                bool aligned = true;
                switch (align_code)
                {
                    case "MID_OPENING":
                        pts.Add(this.MidOpening);
                        break;
                    case "MID_HORIZONTAL":
                        pts = this.HorizontalSize > this.VerticalSize ?
                            new List<pPos> { (this.Rect[0] + this.Rect[1]) / 2, (this.Rect[2] + this.Rect[3]) / 2 }
                            : new List<pPos> { (this.Rect[0] + this.Rect[3]) / 2, (this.Rect[1] + this.Rect[2]) / 2 };
                        break;
                    case "MID_VERTICAL":
                        pts = this.HorizontalSize < this.VerticalSize ?
                            new List<pPos> { (this.Rect[0] + this.Rect[1]) / 2, (this.Rect[2] + this.Rect[3]) / 2 }
                            : new List<pPos> { (this.Rect[0] + this.Rect[3]) / 2, (this.Rect[1] + this.Rect[2]) / 2 };
                        break;
                    case "MID_PREV_FIRST":
                        pts.Add((this.Pts[1] + this.Pts[0]) / 2);
                        break;
                    case "MID_PREV_LAST":
                        pts.Add((this.Pts[this.Pts.Length - 2] + this.Last()) / 2);
                        break;
                    case "MID_RECT01":
                        pts.Add((this.Rect[0] + this.Rect[1]) / 2);
                        break;
                    case "MID_RECT12":
                        pts.Add((this.Rect[1] + this.Rect[2]) / 2);
                        break;
                    case "MID_RECT23":
                        pts.Add((this.Rect[2] + this.Rect[3]) / 2);
                        break;
                    case "MID_RECT34":
                        pts.Add((this.Rect[3] + this.Rect[0]) / 2);
                        break;
                    case "OFFSET_A_RECT0":
                        pts.Add(this.Rect.Offset(-300)[0]);
                        break;
                    case "OFFSET_B_RECT0":
                        pts.Add(this.Rect.Offset(-700)[0]);
                        break;
                    case "OFFSET_A_RECT1":
                        pts.Add(this.Rect.Offset(-300)[1]);
                        break;
                    case "OFFSET_A_RECT2":
                        pts.Add(this.Rect.Offset(-300)[2]);
                        break;
                    case "OFFSET_A_RECT3":
                        pts.Add(this.Rect.Offset(-300)[3]);
                        break;
                    default:
                        pts.Add(this.Rect.CenterPoint());
                        aligned = false;
                        break;
                }
 
                for (int i = 0; i < pts.Count; i++)
                {
                    ObjectIdCollection ids = new ObjectIdCollection();
                    if (aligned)
                    {
                        pts[i].DistanceTo(this.Pts);
 
                        pPos pt = pPos.DistanceTo_Projection;
                        ids = db.Insert(blockName, pt);
 
                        if (ids.Count > 0)
                        {
                            ObjectId blockId = ids.Last();
 
                            int index = pPos.DistanceTo_Index;
                            double angle = new pPos(1, 0).Angle(this.Pts[index] - this.Pts[(index + 1) % this.Pts.Length]);
 
                            pPos new_pt = (pt + new pPos(0, 150)).TransForm(new pPos(0, 0), angle, 1, pt);
                            if (!new_pt.Inside(this.Pts))
                                angle += 180;
 
                            db._setRotation(blockId, angle);
                        }
                    }
                    else
                        ids = db.Insert(blockName, pts[i]);
 
                    res.AddRange(ids);
                }
             
            return res;
        }*/

        public static string[] blocknames = null, template_blocknames = null;

        static public ObjectIdCollection InsertBlock(this Database db, string blockname, 
            IEnumerable<pPos> points, string pram)
        {
            if(template_blocknames == null)
                template_blocknames = IR.ListBlockInFile(DE.CAD_TEMPLATE_FILE);
            if (blocknames == null)
                blocknames = db.ListBlock();

            //double width = prams._prop("WIDTH").ToNumber();
            //double height = prams._prop("HEIGHT").ToNumber();

            ObjectIdCollection blockIds = new ObjectIdCollection();
            int index = Array.FindIndex(blocknames, s => s.Upper().Contains(blockname.Upper()));

            if (index != -1)
                blockIds = db.Insert(blocknames[index], points, "LAYER=" + DE.DEF_LAYER_FURNITURE);
            else
            {
                index = Array.FindIndex(template_blocknames, si => si.Upper().Contains(blockname.Upper()));

                if (index != -1)
                    blockIds = db.Insert(DE.CAD_TEMPLATE_FILE, template_blocknames[index], points);
                else
                    blockIds.AddRange(points.Select(p => db.CreateText(blockname, 
                        p, 500, 0, "LAYER=" + DE.DEF_LAYER_TEXT)).ToCollection());
            }

            if (blockIds.Count > 0)
            {
                double angle = pram._prop("Rotation").ToNumber();
                pPos flip = null;

                if (!pram._prop("Flip").empty())
                {
                    flip = pPos.FromString(pram._prop("Flip"));
                }

                foreach (ObjectId id in blockIds)
                    if (db._isBlock(id))
                    {
                        pPos pt = db._getPoint(id);
                        string st = db.GetAllDynBlockProp(id);

                        //db.WR("Block Props {0}", st);

                        foreach (string propname in st._allPropNames())
                        {
                            double v = pram._prop(propname).ToNumber();
                            //db.WR("propname {0}:{1}", propname, v);
                            if (v>0)
                                db.SetDynBlockProp(id, propname, v);
                        }

                        if (flip != null)
                            db.MirrorObject(id, pt, pt + flip);
                        if (angle > 0)
                            db._setRotation(id, angle);
                    }
            }

            return blockIds;
        }

        static public ObjectIdCollection Insert(this Database db, string _blockName, 
            IEnumerable<pPos> pts, object sourceId = null)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            foreach (pPos pt in pts)
                res.Add(db.Insert(_blockName, pt, sourceId));
            return res;
        }
        
        static public ObjectId Insert(this Database db, string blockName, pPos pt, object sourceId = null)
        {
            ObjectId res = ObjectId.Null;
            //string[] ar = _blockName.filter("#");
            //string blockName = ar.First();
            ObjectIdCollection ids = db.CollectBlock(blockName);

            if (ids.Count > 0)
            {
                res = db.CloneObject(ids.First());
                db._setPoint(res, pt);
            }else
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
                    if (bt.Has(blockName))
                    {
                        BlockTableRecord blockDef = bt[blockName].GetObject(OpenMode.ForRead) as BlockTableRecord;
                        BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                        BlockReference blockRef = new BlockReference(pt.ToPoint3(), blockDef.ObjectId);
                        blockRef.ScaleFactors = new Scale3d(1, 1, 1);

                        ms.AppendEntity(blockRef);
                        tr.AddNewlyCreatedDBObject(blockRef, true);
                        _applyInfo(db, blockRef.ObjectId);
                        res = blockRef.ObjectId;
                    }
                    //else
                    //    db.WR("Cannot find block {0}", blockName);

                    tr.Commit();
                }

            if (!res.IsNull && sourceId != null)
            {
                string txtStyle = sourceId is ObjectId ? db._getIdInfo((ObjectId)sourceId) : sourceId.ToString();
                if (!txtStyle.empty())
                    db._setIdInfo(res, txtStyle);
            }

            //if (ar.Length > 1)
            //db.SetDynBlockState(res, ar.Last());
            return res;
        }

        public static bool object_annotative;

        public static pPos[] region_check_not_duplicate;

        static pPos[] _clearDuplicate(this Database db, string blockName)
        {
            pPos[] pts = new pPos[0];
            if (region_check_not_duplicate != null && region_check_not_duplicate.Length > 0)
            {
                db.GetEntities(region_check_not_duplicate, EN_SELECT.AC_DXF_AND_NAME, "INSERT", blockName);
                if (IR.SelectedIds.Count > 0)
                {
                    pts = new pPos[IR.SelectedIds.Count];
                    for (int i = 0; i < IR.SelectedIds.Count; i++)
                        pts[i] = db._getPoint(IR.SelectedIds[i]);
                    db.EraseObjects(IR.SelectedIds);
                }
            }

            return pts;
        }

        static public ObjectIdCollection Insert(this Database db, string filename, string blockName, IEnumerable<pPos> pts)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            SourceFileName = filename;
            ObjectIdCollection srcIds = new ObjectIdCollection();

            string _cur_layout = IBlock.LayoutName;

            if (IR.HasBlockInFile(SourceFileName, blockName) != null)
            {
                using (Database libDb = ACD.ReadDWG(SourceFileName))
                {
                    ObjectIdCollection ids = new ObjectIdCollection();

                    using (Transaction tr = libDb.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = (BlockTable)tr.GetObject(libDb.BlockTableId, OpenMode.ForRead);
                        
                        if (bt.Has(blockName))
                        {
                            ids.Add(bt[blockName]);
                        }

                        tr.Commit();
                    }

                    if (ids.Count != 0)
                    {
                        IdMapping iMap = new IdMapping();
                        db.WblockCloneObjects(ids, db.BlockTableId, iMap, DuplicateRecordCloning.Ignore, false);
                    
                        IBlock.LayoutName = _cur_layout;
                        if(!db.HasBlock(blockName).IsNull)
                            res = db.Insert(blockName, pts);
                    }
                }
            }

            return res;
        }

        static int[] lsDirection;
        public static pPos ObjectSizes;

        //static bool _filterSWP(Database db, ObjectId id)
        //{
        //    bool res = false;
        //    if (db._isPolylineClosed(id))
        //    {
        //        pPos[] pts = db._getVertices(id);

        //        if (pts.Length >= 5)
        //        {
        //            SWP_CLS src = new SWP_CLS(pts);
        //            res = src.Valid;
        //            if (res)
        //                lsDirection[(int)src.DirectionType]++;
        //        }
        //    }
        //    return res;
        //}

        /*static bool _filterRoom(Database db, ObjectId id)
        {
            bool res = false;
            if (db._isPolylineClosed(id))
            {
                pPos[] pts = db._getVertices(id);
                ROOM_CLS this = new ROOM_CLS(pts);
                res = this.Valid;
            }
            return res;
        }
 
        static pPos[] _resizeRegionByRoom(this  int index)
        {
            pPos[] res = null;
            pPos pt;
            switch (index)
            {
                case 0:
                    pt = this.Pts[0].Project(this.Rect[2], this.Rect[3]);
                    res = new pPos[] { this.Rect[1], this.Rect[0], pt, this.Rect[3] };
                    break;
                case 1:
                    pt = this.Pts[0].Project(this.Rect[2], this.Rect[3]);
                    res = new pPos[] {pt, this.Rect[2], this.Rect[1], this.Last() };
                    break;
            }
            return res;
        }*/

        //public static ObjectIdCollection ResizeObjects(this Database db, ObjectIdCollection ids)
        //{
        //    ObjectIdCollection res = new ObjectIdCollection();
        //    lsDirection = new int[3] { 0, 0, 0 };
        //    ObjectIdCollection swpIds = db.FilterIds(ids, _filterSWP);
        //    //PosCollection pls = new PosCollection();
        //    List<SWP_CLS> lsSWP = new List<SWP_CLS>();
        //    for (int i = 0; i < swpIds.Count; i++)
        //    {
        //        pPos[] pts = db._getVertices(swpIds[i]);
        //        ids.Remove(swpIds[i]);
        //        lsSWP.Add(new SWP_CLS(pts));
        //    }
        //    db.EraseObjects(swpIds);
        //    lsSWP = lsSWP.OrderBy(swp => swp.DirectionType).ToList();
        //    foreach (SWP_CLS src in lsSWP)
        //    {
        //        int index = (int)src.DirectionType;
        //        if (ObjectSizes[index] > 0)
        //        {
        //            double stretch_value = (ObjectSizes[index] - src.Value) / lsDirection[index];
        //            //db.WR("Stretch Value {0}, Pts {1} Direction {2}",
        //            //    stretch_value, src.Pts.Length, src.Direction * stretch_value);
        //            db.Stretch(ids, src.Pts, src.Direction * stretch_value);
        //            src.Stretch(stretch_value * lsDirection[index]);
        //            //pls.Add(src.Pts);
        //        }
        //    }

        //    foreach (ObjectId id in ids)
        //        if (db.ValidId(id))
        //            res.Add(id);

        //    return res;
        //}

        static void _applyInfo(Database db, ObjectId objId)
        {
            string layer = LayerInfo != "" && LayerInfo != null ?
                LayerInfo : db._getIdName(objId).StartsWith("G") ? "G-Text" : "I-Furniture";

            db._setLayer(objId, layer);

            if (AttName != null && AttName.Length > 0)
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockReference blockRef = (BlockReference)tr.GetObject(objId, OpenMode.ForWrite);
                    BlockTableRecord blockDef = (BlockTableRecord)tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead);

                    foreach (ObjectId id in blockDef)
                    {
                        DBObject obj = id.GetObject(OpenMode.ForRead);
                        AttributeDefinition attDef = obj as AttributeDefinition;
                        if ((attDef != null) && (!attDef.Constant))
                        {
                            int index = Array.FindIndex(AttName, st => st.Upper() == attDef.Tag.Upper());

                            if (index != -1)
                                using (AttributeReference attRef = new AttributeReference())
                                {
                                    attRef.SetAttributeFromBlock(attDef, blockRef.BlockTransform);
                                    if (AttValue[index] != "")
                                        attRef.TextString = AttValue[index];

                                    blockRef.AttributeCollection.AppendAttribute(attRef);
                                    tr.AddNewlyCreatedDBObject(attRef, true);

                                    attRef.Dispose();
                                }
                        }
                    }
                    tr.Commit();
                }
        }

        /*public static ObjectId AttachXREF(this Database acCurDb, string PathName, pPos pt, bool bind = false)
        {
            ObjectId res = ObjectId.Null;
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Create a reference to a DWG file
 
                ObjectId acXrefId = acCurDb.AttachXref(PathName, Path.GetFileNameWithoutExtension(PathName));
 
                // If a valid reference is created then continue
                if (!acXrefId.IsNull)
                {
                    res = acXrefId;
                    // Attach the DWG reference to the current space
                    Point3d insPt = pt.ToPoint3();
                    using (BlockReference acBlkRef = new BlockReference(insPt, acXrefId))
                    {
                        BlockTableRecord acBlkTblRec;
                        acBlkTblRec = acTrans.GetObject(acCurDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
 
                        acBlkTblRec.AppendEntity(acBlkRef);
                        acTrans.AddNewlyCreatedDBObject(acBlkRef, true);
 
                        if (bind)
                        {
                            ObjectIdCollection ids = new ObjectIdCollection(new ObjectId[] { acXrefId });
                            acCurDb.BindXrefs(ids, false);
                        }
                    }
                }
 
                // Save the new objects to the database
                acTrans.Commit();
 
                // Dispose of the transaction
            }
            return res;
        }
 
        public static string[] CollectXREFs(this Database db)
        {
            List<string> res = new List<string>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                db.ResolveXrefs(true, false);
                XrefGraph xg = db.GetHostDwgXrefGraph(true);
                GraphNode root = xg.RootNode;
 
                for (int o = 0; o < root.NumOut; o++)
                {
                    XrefGraphNode child = root.Out(o) as XrefGraphNode;
 
                    if (child.XrefStatus == XrefStatus.Resolved)
                    {
                        BlockTableRecord bl = tr.GetObject(child.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
                        res.Add(bl.Name);
                    }
                }
 
                tr.Commit();
            }
            return res.ToArray();
        }*/

        public static void XClipBlock(this Database db, ObjectId blockId, pPos[] region)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference acBlkRef = (BlockReference)tr.GetObject(blockId, OpenMode.ForWrite);
                BlockTableRecord bt = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                Point2dCollection ptCol = db.ToPoint2dCollection(blockId, region);

                Vector3d nothisal;
                double elev = 0;

                if (db.TileMode == true)
                {
                    nothisal = db.Ucsxdir.CrossProduct(db.Ucsydir);
                    elev = db.Elevation;
                }
                else
                {
                    nothisal = db.Pucsxdir.CrossProduct(db.Pucsydir);
                    elev = db.Pelevation;
                }

                // Set the clipping boundary and enable it
                using (SpatialFilter filter = new SpatialFilter())
                {
                    SpatialFilterDefinition filterDef = new SpatialFilterDefinition(ptCol, nothisal, elev, 0, 0, true);
                    var pts = filterDef.GetPoints();
                    filter.Definition = filterDef;

                    // Define the name of the extension dictionary and entry name
                    //string dictName = "ACAD_FILTER";
                    //string spName = "SPATIAL";

                    // Check to see if the Extension Dictionary exists, if not create it
                    if (acBlkRef.ExtensionDictionary.IsNull)
                    {
                        acBlkRef.UpgradeOpen();
                        acBlkRef.CreateExtensionDictionary();
                        acBlkRef.DowngradeOpen();
                    }

                    // Open the Extension Dictionary for write
                    DBDictionary extDict = tr.GetObject(acBlkRef.ExtensionDictionary, OpenMode.ForWrite) as DBDictionary;

                    // Check to see if the dictionary for clipped boundaries exists, 
                    // and add the spatial filter to the dictionary
                    if (extDict.Contains(filterDictName))
                    {
                        DBDictionary filterDict = tr.GetObject(extDict.GetAt(filterDictName), OpenMode.ForWrite) as DBDictionary;

                        if (filterDict.Contains(spatialName))
                        {
                            filterDict.Remove(spatialName);
                        }

                        filterDict.SetAt(spatialName, filter);
                    }
                    else
                    {
                        using (DBDictionary filterDict = new DBDictionary())
                        {
                            extDict.SetAt(filterDictName, filterDict);

                            tr.AddNewlyCreatedDBObject(filterDict, true);
                            filterDict.SetAt(spatialName, filter);
                        }
                    }

                    // Append the spatial filter to the drawing
                    tr.AddNewlyCreatedDBObject(filter, true);
                }

                // Save the new objects to the database
                tr.Commit();

                // Dispose of the transaction
            }
            db.UpdateExt(true);
        }

        static string filterDictName = "ACAD_FILTER";
        static string spatialName = "SPATIAL";

        public static pPos[] GetXCLIP(this Database db, ObjectId blockId)
        {
            pPos[] res = null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Open the selected BlockReference

                BlockReference br = tr.GetObject(blockId, OpenMode.ForRead) as BlockReference;

                bool found = false;
                //db.WR("OK1");
                // It should always be a block reference, but it might
                // not have an extension dictionary

                if (br != null && br.ExtensionDictionary != ObjectId.Null)
                {
                    // The extension dictionary needs to contain a nested
                    // dictionary called ACAD_FILTER

                    var extdict = tr.GetObject(br.ExtensionDictionary, OpenMode.ForRead) as DBDictionary;
                    if (extdict != null && extdict.Contains(filterDictName))
                    {
                        var fildict = tr.GetObject(extdict.GetAt(filterDictName), OpenMode.ForRead) as DBDictionary;
                        if (fildict != null)
                        {
                            //db.WR("found.");
                            // The nested dictionary should contain a
                            // SpatialFilter object called SPATIAL

                            if (fildict.Contains(spatialName))
                            {
                                var fil = tr.GetObject(fildict.GetAt(spatialName), OpenMode.ForRead) as SpatialFilter;
                                if (fil != null)
                                {
                                    // We have a SpatialFilter: print its bounds

                                    var ext = fil.GetQueryBounds();
                                    res = new pPos[] { ext.MinPoint.ToPos(), ext.MaxPoint.ToPos()};
                                    
                                    found = true;
                                }
                            }
                        }
                    }
                }
                if (!found)
                {
                    //db.WR("No clipping inFormation found.");
                }
                tr.Commit();
            }

            return res;
        }

        public static string uniqueBlockName(this Database db, string blockName)
        {
            string new_name = blockName;
            int counter = 0;
            while (!db.HasBlock(new_name).IsNull)
            {
                counter++;
                new_name = blockName + "(" + counter.ToString() + ")";
            }
            return new_name;
        }


        public static ObjectId ToObjectId(this Database db, string st)
        {
            ObjectId res = ObjectId.Null;
            
            db.GetEntities(null, EN_SELECT.AC_ALL);
            int index = IR.SelectedIds.FindIndex(id => id.Handle.Value.ToString() == st);
            if (index != -1)
                res = IR.SelectedIds[index];
            return res;
        }

        public static ObjectIdCollection ToObjectIdCollection(this Database db, string txt)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            string[] ar = txt._prop("IDS").filter(",");

            foreach (string st in ar)
            {
                ObjectId new_id = db.ToObjectId(st);
                if (db.ValidId(new_id))
                    res.Add(new_id);
            }
            return res;
        }

        public static ObjectIdCollection TagElement(this Database db, string content)
        {
            ObjectIdCollection ids = new ObjectIdCollection();

            ids = db.ToObjectIdCollection(content);
            //db.WR("Index {0} Content {1} Ref_Content {2} Dxf {3}",
            //    i, contents[i]._prop("NA"), contents[i], ids.First().ObjectClass.DxfName);

            string val = content._prop("NA");

            if (!val.empty())
            {
                IBlock.AttName = new string[] { "NO" };
                IBlock.AttValue = new string[] { val };

                foreach(ObjectId id in db.Insert(DE.CAD_TEMPLATE_FILE, DE.DEF_DOORTAG_LEGEND,
                    ids.Cast<ObjectId>().Where(id => db.ValidId(id))
                    .Select(id => db._getBound(id).CenterPoint())))
                        db.SetXNotes(id, DE.DEF_LAYER_TEXT);
            }

            return ids;
        }
    }
}