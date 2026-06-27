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
    public class NoteAutoGCoteCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                ObjectIdCollection selIds = ACD.GetSelection();

                foreach (ObjectId gridId in selIds)
                    if (db._isBlock(gridId))
                    {
                        db.GetEntities(db._getBound(gridId), EN_SELECT.AC_DXF, "INSERT");
                        ObjectIdCollection coteIds = IR.SelectedIds.Cast<ObjectId>()
                            .Where(id => !db.GetBlockAtt(id, "COTE").empty()).Select(id => id).ToCollection();

                        if (coteIds.Count > 0)
                        {
                            int index = coteIds.FindIndex(id => db.GetBlockAtt(id, "COTE").Contains("0.000"));
                            ACD.WR("COTE_IDS {0} INDEX {1}", coteIds, index);

                            if (index != -1)
                            {
                                pPos basept = db._getPoint(coteIds[index]);
                                foreach (ObjectId id in coteIds)
                                {
                                    pPos pt = db._getPoint(id);
                                    int v = (int)(((pt.X - basept.X) / 100).roundNumber() * 100);
                                    int v_ext = Math.Abs(v) % 1000;

                                    db.SetBlockAtt(id, "COTE", v == 0 ? "%%P0.000"
                                        : (v < 0 ? "-" : "+") + (((int)(Math.Abs(v) / 1000))).ToString() + "."
                                        + (v_ext < 10 ? "0" : "") + (v_ext < 100 ? "0" : "") + v_ext.ToString());
                                }
                            }
                        }
                    }

                ACD.Focus();
            }
        }
    }
}

