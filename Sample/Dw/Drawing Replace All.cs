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
    public class DrawingReaplaceAllCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    Database db = ACD.DB;
                    //string blockName = "";

                    string[] files = Directory.GetFiles(Path.GetDirectoryName(ACD.CurrentDWGPath), "*.dwg");
                    //string key = Path.GetFileNameWithoutExtension(ACD.CurrentDWGPath).Upper();

                    foreach (string f in files)
                        if (!ACD.IsCurrentFile(f) && !ACD.IsOpenDocument(f))
                        {
                            using (Database destdb = ACD.ReadDWG(f))
                            {
                                if (destdb != null)
                                {
                                    IdMapping mapping = new IdMapping();

                                    db.WblockCloneObjects(selIds, destdb.BlockTableId, mapping,
                                        DuplicateRecordCloning.Replace, false);

                                    ACD.WR("Update file {0}", f);
                                    destdb.SaveAs(f, DwgVersion.Current);
                                }
                            }
                        }
                }
                ACD.Focus();
            }
        }
    }
}

