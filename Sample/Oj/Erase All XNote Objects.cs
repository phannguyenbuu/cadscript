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

//
using System.Windows.Forms;
//using SyncObject;
    
namespace AcadScript
{ 
    public class EraseAllXNoteObjectCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                pPos pt = ACD.GetPoint();

                if (pt != null)
                {

                    pPos[] zone = ACD.DB.GetDrawingZone(pt);

                    if (zone != null)
                    {
                        ACD.DB.GetEntities(zone, EN_SELECT.AC_ALL);

                        ObjectIdCollection oldIds = new ObjectIdCollection();

                        foreach (ObjectId id in IR.SelectedIds)
                        {
                            try
                            {
                                string[] notes = ACD.DB.GetXNotes(id);
                                if (notes.Length > 0 && notes[0]._firstPropName() == "StructLegend")
                                    oldIds.Add(id);
                            }
                            catch (System.Exception ex)
                            {

                            }
                        }

                        ACD.WR("Objs {0}", oldIds.Count);
                        if (oldIds.Count > 0)
                            ACD.DB.EraseObjects(oldIds);
                    }
                }
            }
            ACD.Focus();
        }
    }
}

