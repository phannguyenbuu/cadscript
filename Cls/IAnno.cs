using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
//using Autodesk.AutoCAD.EditorInput;
//using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Internal;

//using System;
//using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using System.Diagnostics;
//using System.IO;
//using System.Reflection;
using AcadScript;

namespace AcadScript
{
    public static class IAnno
    {
        public static ObjectId CreateText(this Database db, string txt, pPos pt,
            double _height = 2.5, double _width = 0, object sourceId = null)
        {
            ObjectId res = ObjectId.Null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId ModelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(ModelSpaceId, OpenMode.ForWrite);

                MText mt = new MText();
                mt.Location = new Point3d(pt.X, pt.Y, 0);

                btr.AppendEntity(mt);
                tr.AddNewlyCreatedDBObject(mt, true);

                res = mt.ObjectId;
                mt.TextHeight = _height;
                db._setLayer(mt.ObjectId, DE.DEF_LAYER_TEXT);

                mt.UpgradeOpen();
                mt.TextHeight = _height / db.CurrentAnnotativeScale();
                mt.Annotative = AnnotativeStates.True;

                if (sourceId != null)
                {
                    string txtStyle = sourceId is ObjectId ?
                        db._getIdInfo((ObjectId)sourceId) : sourceId.ToString();
                    if (!txtStyle.empty())
                    {
                        db._setIdInfo(res, txtStyle);

                        if (txtStyle._prop("BACKGROUND").ToBool())
                        {
                            mt.BackgroundFill = true;
                            mt.BackgroundFillColor = Autodesk.AutoCAD.Colors.Color.FromRgb(100, 100, 100);
                        }

                        mt.TextHeight = _height / db.CurrentAnnotativeScale();
                        mt.Annotative = txtStyle._prop("ANNO").Upper() == "OFF"
                            || txtStyle._prop("ANNO").Upper() == "FALSE" ?
                            AnnotativeStates.False : AnnotativeStates.True;


                        //if (!txtStyle._prop("LAYER").empty())
                        db._setLayer(res, DE.DEF_LAYER_TEXT);
                    }
                }

                ObjectId styleId = db.Textstyle;
                if (!styleId.IsNull)
                    mt.TextStyleId = styleId;

                //* (mt.Annotative == AnnotativeStates.True ? 
                //    db.Cannoscale.Name.filter(":").Last().ToNumber() : 1);
                mt.Width = _width;

                db._setContent(mt.ObjectId, txt);
                tr.Commit();
            }

            return res;
        }


        static public string[] ListAllScale(this Database db)
        {
            List<string> res = new List<string>();
            ObjectContextManager ocm = db.ObjectContextManager;
            ObjectContextCollection occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");

            foreach (ObjectContext cc in occ)
                if(cc.Name.Contains(":")) res.Add(cc.Name);

            return res.ToArray();
        }

        static public double CurrentAnnotativeScale(this Database db)
        {
            ObjectContextCollection occ = db.ObjectContextManager.GetContextCollection("ACDB_ANNOTATIONSCALES");
            return !occ.HasContext(db.Cannoscale.Name) ? 0: db.Cannoscale.Scale;
        }

        static public void RemoveAllButCurrentScale(this Database db, ObjectIdCollection ids, string anno = null)
        {
            ObjectContextCollection occ = db.ObjectContextManager.GetContextCollection("ACDB_ANNOTATIONSCALES");

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // If we can't find the current annotation scale in our dictionary, we have a problem

                if (anno.empty())
                    anno = db.Cannoscale.Name;

                if (occ.HasContext(anno))
                {
                    AnnotationScale annsc = (AnnotationScale)occ.GetContext(anno);
                    db.Cannoscale = (AnnotationScale)occ.GetContext(anno);
                }
                else
                { 
                    //db.WR("\nCannot find annotation scale {0}", anno);
                    return;
                }
                // Get the ObjectContext associated with the current annotation scale

                //db.WR("\nApply {0} objects to annotation scale {1}", ids, anno);
                ObjectContext curCtxt = occ.GetContext(anno);

                // Check each selected object

                foreach (ObjectId id in ids)
                {
                    DBObject obj = tr.GetObject(id, OpenMode.ForRead);

                    if (obj.Annotative == AnnotativeStates.True)
                    {
                        obj.UpgradeOpen();

                        if (!obj.HasContext(db.Cannoscale)) ObjectContexts.AddContext(obj, curCtxt);
                        // Loop through the various annotation scales in
                        // the drawing

                        foreach (ObjectContext oc in occ)
                            if (obj.HasContext(oc) && oc.Name != anno)
                                obj.RemoveContext(oc);
                    }
                }
                tr.Commit();
            }
        }

        public static double AddCurrentAnnotative(this Database db, ObjectId id)
        {
            // Get the manager object and the list of scales
            double res = db.Cannoscale.Scale;
            ObjectContextManager ocm = db.ObjectContextManager;
            ObjectContextCollection occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");

            // Prompt the user for objects to process (or get them from the pickfirst set)
            // Maintain counters of objects modified and scales removed

            //int objCount = 0, scaCount = 0;

            // Use a flag to check when we first modify an object

            //bool scalesRemovedForObject = false;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // If we can't find the current annotation scale in our dictionary, we have a problem

                if (!occ.HasContext(db.Cannoscale.Name))
                {
                    //db.WR("\nCannot find current annotation scale.");
                    return res;
                }
                //else
                    //db.WR("\nCurrent annotation scale: {0}\r\n", db.Cannoscale.Name);

                // Get the ObjectContext associated with the current annotation scale

                ObjectContext curCtxt = occ.GetContext(db.Cannoscale.Name);

                // Check each selected object

                DBObject obj = tr.GetObject(id, OpenMode.ForRead);

                // Check it's annotative and has the current scale

                obj.Annotative = AnnotativeStates.True;
                
                obj.UpgradeOpen();

                if (!obj.HasContext(db.Cannoscale))
                {
                    ObjectContexts.AddContext(obj, curCtxt);
                    
                }
                // Loop through the various annotation scales in
                // the drawing

                foreach (ObjectContext oc in occ)
                {
                    // If it's on the object but not current
                    // (for some reason we have to check the name
                    // rather than oc == curCtxt)

                    if (obj.HasContext(oc) && oc.Name != db.Cannoscale.Name)
                    {
                        // Remove it and increment our counter/set our flag

                        obj.RemoveContext(oc);
                        //scaCount++;
                        //scalesRemovedForObject = true;
                    }
                }

                tr.Commit();
            }

            //db.WR("Scale {0}", res);
            return res;
        }

        public static void ApplyAnno(this Database db, string scale_name)
        {
            ObjectIdCollection ids = ACD.GetSelection();

            if (ids.Count > 0)
                db.ApplyAnno(ids, scale_name);
        }

        public static void _setMLeadeBottomUnderline(this Database db, ObjectId id)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (id.ObjectClass.DxfName == "MULTILEADER")
                {
                    MLeader leader = (MLeader)tr.GetObject(id, OpenMode.ForWrite);

                    TextAlignmentType align = leader.TextAlignmentType;
                    leader.TextAlignmentType = TextAlignmentType.LeftAlignment;
                    leader.TextAttachmentType = TextAttachmentType.AttachmentBottomLine;

                    leader.TextAlignmentType = TextAlignmentType.RightAlignment;
                    leader.TextAttachmentType = TextAttachmentType.AttachmentBottomLine;

                    leader.TextAlignmentType = align;
                    tr.Commit();
                }
            }
        }
        
        /*int index = scale_list.Keys.ToList().FindIndex(itm => itm == key);

                if (index != -1)
                {
                    string display = scale_list.Values.ElementAt(index);
                    index = cbDisplay.Items.Cast<string>().ToList()
                        .FindIndex(s => s.Upper().Contains(display.Upper()));

                    if (index != -1)
                        cbDisplay.SelectedIndex = index;
                }
                ACD.Focus();
            }
        }

        private void cbDisplay_SelectedIndexChanged(object sender = null, EventArgs e = null)
        {
            using (ACD.Lock())
            {
                string display = cbDisplay.SelectedItem.ToString();
                db.WR("Display {0}", display);
                ACD.SetDisplayConfigCS(display);
                
                ACD.Focus();
                ACD.ED.Regen();
            }
        }*/
        


        
        public static void DeleteAll(Database db)
        {
            ObjectContextManager ocm = db.ObjectContextManager;

            if (ocm != null)
            {
                // Now get the Annotation Scaling context collection
                // (named ACDB_ANNOTATIONSCALES_COLLECTION)

                ObjectContextCollection occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");

                if (occ != null)
                {
                    // Create a collection to collect the IDs

                    ObjectIdCollection oic = new ObjectIdCollection();

                    foreach (ObjectContext oc in occ)
                        if (oc is AnnotationScale)
                            oic.Add(new ObjectId(oc.UniqueIdentifier));

                    // Check the object references using Purge
                    // (this does NOT purge the objects, it only
                    // filters the objects that are not purgable)

                    db.Purge(oic);

                    // Now let's erase each of the objects left


                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        // Maintain a count which we decrement for
                        // each error we receive on open/erase

                        int count = oic.Count;
                        foreach (ObjectId id in oic)
                        {
                            try
                            {
                                DBObject obj =
                                  tr.GetObject(id, OpenMode.ForWrite);
                                obj.Erase();
                            }
                            catch
                            {
                                //count--;
                            }
                        }
                        tr.Commit();

                        // InForm the user of the results

                        //db.WR("\n{0} annotation scale{1} removed.", count, count == 1 ? "" : "s");
                    }
                }
            }
        }
    }
}
 