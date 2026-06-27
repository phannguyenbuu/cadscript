using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Internal;
//using SyncObject;

namespace AcadScript
{
    public class AnnotativeCLS
    {
       
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ACD.DB.GetEntities(ACD.bRect, EN_SELECT.AC_DXF, "MTEXT", "TEXT", "AEC_DIMENSION_GROUP", "DIMENSION", "MULTILEADER");
                ObjectIdCollection ids = IR.SelectedIds;

                ACD.WR("IDS {0}", ids.Count);

                if (ids.Count > 0)
                {
                    PosCollection regions = ACD.DB.GetDrawingZones();
                    double page_w = pString.INI_String("PAGEWIDTH").ToNumber();
                    double page_h = pString.INI_String("PAGEHEIGHT").ToNumber();

                    using (Transaction tr = ACD.DB.TransactionManager.StartTransaction())
                    {
                        foreach (ObjectId objId in ids)
                        {
                            ObjectContextManager ocm = ACD.DB.ObjectContextManager;
                            ObjectContextCollection occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");

                            //pPos pt = ACD.DB._getPoint(objId);
                            pPos[] bb = ACD.DB._getBound(objId);
                            pPos pt = bb.CenterPoint();

                            if(objId.ObjectClass.DxfName != "AEC_DIMENSION_GROUP")
                                pt = ACD.DB._getPoint(objId);

                            pPos[] zone = ACD.DB.GetDrawingZone(pt);
                            //ACD.DB.DrawPolyline(zone.Rect(), true);
                            string scale_name = zone.BoundScale(page_w, page_h);

                            ACD.WR("SCALE {0}", scale_name);

                            if (!scale_name.empty() && occ.HasContext(scale_name))
                            {
                                ObjectContext curCtxt = occ.GetContext(scale_name);

                                foreach (ObjectId id in ids)
                                {
                                    //ACD.WR("ID {0}", id.ObjectClass.DxfName);
                                    DBObject obj = tr.GetObject(id, OpenMode.ForWrite);

                                    if (obj != null)
                                    {
                                        obj.Annotative = AnnotativeStates.True;
                                        obj.UpgradeOpen();

                                        if (!obj.HasContext(curCtxt))
                                            ObjectContexts.AddContext(obj, curCtxt);

                                        foreach (ObjectContext oc in occ)
                                            if (obj.HasContext(oc) && oc.Name != scale_name)
                                                obj.RemoveContext(oc);
                                    }
                                }
                            }
                            else
                            {
                                ACD.WR("\nCannot find current annotation scale.");
                            }
                        }

                        tr.Commit();
                    }
                    ACD.Focus();
                }
            }
        }
    }
}

