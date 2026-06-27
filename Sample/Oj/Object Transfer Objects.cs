using AS = Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;

using Autodesk.AutoCAD.DatabaseServices;

using Autodesk.AutoCAD.EditorInput;

using Autodesk.AutoCAD.Runtime;

using Autodesk.AutoCAD.Windows;

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Autodesk.AutoCAD.DatabaseServices;


namespace AcadScript
{
    
    public class RunNetCodeMenu
    {


        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                AS.DocumentCollection docs = AS.Application.DocumentManager;
                ObjectIdCollection selIds = ACD.GetSelection();
                
                if(selIds.Count > 0)
                    foreach (AS.Document doc in docs)
                    {
                        ACD.WR("Docs {0}", doc.Name);
                        if (Path.GetFileNameWithoutExtension(doc.Name).ct_("collect"))
                        {
                            using (AS.DocumentLock lk = doc.LockDocument())
                            {
                                ACD.WR("Docs {0}", doc.Name);
                                Database destDb = doc.Database;

                                ObjectIdCollection newIds = ACD.DB.CloneObjects(selIds, destDb);
                                pPos[] bb = destDb._getBound(newIds);

                                destDb.MoveObject(newIds, (bb[0] - bb[1]) / 2);
                                doc.Editor.Regen();
                            }
                            break;
                        }
                    }

                ACD.Focus();
            }
        }
    }
}

