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
    public class DimensionTXTScheduleCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;

                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    pPos[] bb = ACD.DB._getBound(selIds);

                    List<pPos> txts = new List<pPos>();

                    UserTableDataCLS tab = new UserTableDataCLS(ACD.DB, bb[0]);

                    foreach (ObjectId id in selIds)
                        if (ACD.DB._isBlock(id))
                        {
                            ACD.DB.BlockEntitiesAction(id, (subIds) =>
                            {
                                txts.AddRange(subIds.ToList().Where(txtId => ACD.DB._isText(txtId))
                                    .Select(txtId => ACD.DB._getPoint(txtId)).ToArray());
                            });

                        }

                    txts = txts.OrderBy(p => p.Content).ToList();

                    pPos pt = bb[0].Clone();

                    foreach (pPos txt_pt in txts)
                        tab.AddUserTableData(txt_pt.Content, new string[] { "",""}, pt, pt.X > bb[1].X - 20 ? -20 : 0);

                    tab.Insert(0.5);
                }
                ACD.Focus();
            }
        }
    }
}

