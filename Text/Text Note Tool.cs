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

namespace AcadScript
{
    public class NoteToolCLS
    {
        static bool _isIdIntersect(Database db, ObjectId id, IEnumerable<pPos> pts)
        {
            bool res = false;

            if (db._isPoint(id))
                res = db._getPoint(id).Inside(pts);
            else if (db._isVertice(id) || ACD.DB._isWall(id))
            {
                pPos[] ls = db._getVertices(id);
                res = ls.Any(p => p.Inside(pts));
                if (!res)
                {
                    PosCollection segs = ls.GetSegment();
                    res = segs.Any(seg => pts.Intersect(seg[0], seg[1], true).Length > 0);
                }
            }

            return res;
        }
              

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                while (true)
                {
                    ObjectIdCollection selIds = ACD.GetSelection();

                    if (selIds.Count == 0)
                        break;

                    pPos pt = ACD.GetPoint();
                    if (pt.IsNull)
                        break;

                    ObjectIdCollection nodeIds = new ObjectIdCollection();

                    foreach (ObjectId lwpId in selIds)
                        if (ACD.DB._isPolyline(lwpId))
                        {
                            pPos[] pts = ACD.DB._getVertices(lwpId).Offset(100);
                            ACD.DB.GetEntities(pts, EN_SELECT.AC_ALL);

                            nodeIds.AddRange(IR.SelectedIds.Cast<ObjectId>()
                                .Where(id => _isIdIntersect(ACD.DB, id, pts)).Select(id => id).ToCollection());
                        }
                        else if (ACD.DB._isBlock(lwpId))
                        {
                            //node
                        }

                    ObjectIdCollection ids = ACD.DB.CloneObjects(nodeIds);
                    ACD.DB.MoveObject(ids, pt - ACD.DB._getBound(selIds).CenterPoint());
                }

            }

            ACD.Focus();
        }
    }
}

