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
//using System.Windows.Forms;
//using SyncObject;

namespace AcadScript
{
    public class PolylineAreaCLS
    {
        static PosCollection _getRectPts(pPos pt)
        {
            pPos[] zone = ACD.DB.GetDrawingZone(pt);
            if (zone == null) zone = ACD.bRect;

            ACD.DB.GetEntities(zone, EN_SELECT.AC_DXF, "AEC_WALL", "LINE", "LWPOLYLINE", "ARC", "CIRCLE", "ELLIPSE");
            PosCollection res = new PosCollection();

            foreach (ObjectId id in IR.SelectedIds)
            {
                if (ACD.DB._isWall(id))
                {
                    ObjectId wallId = ACD.DB.GetWallShape(id);
                    res.Add(ACD.DB._getVertices(wallId));
                    ACD.DB.EraseObject(wallId);
                }
                else if (ACD.DB._getLayer(id).Upper() == "A-FIN")
                {
                    res.Add(ACD.DB._getVertices(id));
                }
            }

            return res;
        }


        static pPos[] GetMinDimArea(PosCollection wallpts, pPos txt_pt, int axis, pPos base_vector)
        {
            List<pPos> res = new List<pPos>();
            List<pPos> interps = new List<pPos>();

            pPos mv = new pPos(0, 0);

            if (axis == 0)
                mv = base_vector;
            else
                mv = new pPos(-base_vector.Y, base_vector.X);

            foreach (pPos[] ls in wallpts)
                interps.AddRange(ls.Intersect(txt_pt, txt_pt + mv));

            if (interps.Count > 0)
            {
                pPos p1 = null, p2 = null;
                interps = interps.OrderBy(p => p.Y).ThenBy(p => p.X).ToList();

                int index = Array.FindIndex(DE.NumericArray(0, interps.Count - 1),
                    i => interps[i][axis] <= txt_pt[axis] && (i == interps.Count - 1 || interps[i + 1][axis] >= txt_pt[axis]));

                if (index != -1)
                {
                    p1 = interps[index];
                    if (index < interps.Count - 1)
                    {
                        p2 = interps[index + 1];

                        res.Add(p1);
                        res.Add(p2);
                    }
                }
            }

            return res.ToArray();
        }

        static void _showAreaText(PosCollection wallpts, pPos txt_pt, double rotation)
        {
            pPos base_vector = new pPos(Math.Cos(rotation), Math.Sin(rotation));
            List<pPos> res = new List<pPos>();

            for (int axis = 0; axis < 2; axis++)
                res.AddRange(GetMinDimArea(wallpts, txt_pt, axis, base_vector));

            if (res.Count >= 4)
            {
                double n = res[0].DistanceTo(res[1]) * res[2].DistanceTo(res[3]);

                //ACD.DB.CreateText("#C" + (n / 1000000).roundNumber(0.1) + "m\\U+00B2", res.CenterPoint(), 2);

                if (rotation == 0)
                {
                    pPos[] bb = res.Boundary();
                    IDimChain.CreateDimension(ACD.DB, new pPos(bb[0].X, (bb[0].Y + bb[1].Y) / 2),
                        new pPos(bb[1].X, (bb[0].Y + bb[1].Y) / 2), 200, 200,
                        "<>("+ (n / 1000000).roundNumber(0.1) + "m\\U+00B2)");
                    IDimChain.CreateDimension(ACD.DB, new pPos((bb[0].X + bb[1].X) / 2, bb[0].Y),
                        new pPos((bb[0].X + bb[1].X) / 2, bb[1].Y), 200, 200);
                }
                else
                {
                    IDimChain.CreateAlignDimension(ACD.DB, res[0], res[1], 200, 200);
                    IDimChain.CreateAlignDimension(ACD.DB, res[2], res[3], 200, 200);
                }
            }
        }

        static double _hatchArea(Database db, ObjectId objId)
        {
            double res = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                if (db._isHatch(objId))
                {
                    Hatch ent = (Hatch)tr.GetObject(objId, OpenMode.ForWrite);
                    res = ent.Area;
                }
                tr.Commit();
            }

            return res;
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count >= 2 && selIds.ToList().Any(_id => ACD.DB._isText(_id))
                    && selIds.ToList().Any(_id => ACD.DB._isPolyline(_id) || ACD.DB._isHatch(_id)))
                {
                    ObjectIdCollection hisIds = new ObjectIdCollection();

                    for (int i = 0; i < selIds.Count; i += 2)
                    {
                        ObjectId[] _arIds = new ObjectId[] {selIds[i], selIds[i+1]};
                        ObjectId lwpId = _arIds.First(_id => ACD.DB._isPolyline(_id) || ACD.DB._isHatch(_id));
                        hisIds.Add(lwpId);

                        foreach (ObjectId txtId in _arIds)
                            if (ACD.DB._isText(txtId))
                                try
                                {
                                    ACD.DB._setContent(txtId, ACD.DB._isPolyline(lwpId) ?
                                        ACD.DB._getVertices(lwpId).Area().roundNumber(1).ToString()
                                        : _hatchArea(ACD.DB, lwpId).roundNumber(1).ToString());
                                }catch(SystemException ex)
                                {
                                    ACD.WR("error in hatch {0}", txtId.Handle);
                                }
                    }

                    ACD.DB.EraseObjects(hisIds);
                }
                else
                {

                    int mode = (int)ACD.ED.GetInputString("Mode 1 - Total/ 2 - Single / 3 - Show Area / 4 - MLeader", "1").ToNumber();


                    if (mode == 1 || mode == 2)
                    {


                        if (selIds.Count > 0)
                        {

                            PosCollection pls = selIds.ToList().Where(id => ACD.DB._isHatch(id))
                                .SelectMany(id => ACD.DB._getHatch(id)).ToCollectionSameClosed(true);

                            if (mode == 2)
                                foreach (pPos[] ls in pls)
                                {
                                    //if (ACD.DB._isPolyline(lwpId) || ACD.DB._isHatch(lwpId))
                                    {
                                        // pPos[] ls = ACD.DB._getVertices(lwpId);
                                        pPos pt = ls.CenterPoint();
                                        ACD.DB.CreateText(String.Format("{0}m\\U+00B2", (ls.Area() / 1000000).roundNumber(0.1)), pt);


                                        //pts.Add(pt);
                                    }
                                }

                            double total_area = 0;

                            Dictionary<string, double> dicts = new Dictionary<string, double>();
                            foreach (ObjectId lwpId in selIds)
                            {
                                string st = null;

                                if (ACD.DB._isHatch(lwpId))
                                    st = ACD.DB._getIdName(lwpId);
                                else if (ACD.DB._isPolylineClosed(lwpId))
                                {
                                    st = ACD.DB._getLayer(lwpId);
                                    if (ACD.DB._getLineworkWidth(lwpId) > 0)
                                    {
                                        double n = ACD.DB._getVertices(lwpId).Area();
                                        if (n > total_area)
                                            total_area = n;
                                    }
                                }

                                if (!st.et_())
                                {
                                    st = st.filter("|").First();
                                    if (!dicts.ContainsKey(st))
                                        dicts.Add(st, 0);

                                    dicts[st] += ACD.DB._getVertices(lwpId).Area();
                                }
                            }

                            string msg = String.Format("<Total {0} m2>\r\n", total_area.roundNumber(0.01).ToString("N0"));

                            foreach (string k in dicts.Keys)
                                msg += String.Format("Item {0} - Area {1} m2 - {2} %\r\n",
                                    k, dicts[k].roundNumber(0.01).ToString("N0"),
                                    total_area > 0 ? (dicts[k] / total_area * 100).roundNumber(0.01) : 0);

                            ACD.WR(msg);
                            System.Windows.Forms.Clipboard.SetText(msg);
                        }
                    }
                    else if (mode == 4)
                    {
                        //selIds = ACD.GetSelection();

                        string st = String.Format("Total: {0} m2", selIds.ToList().Where(id
                            => ACD.DB._isText(id) || ACD.DB._isLeader(id)).Sum(id => ACD.DB._getContent(id).Replace("m2", "").ToNumber()));

                        ACD.WR(st);
                        System.Windows.Forms.Clipboard.SetText(st);
                    }
                    else
                    {
                        pPos[] pts = ACD.GetPickPts();

                        foreach (pPos pt in pts)
                        {
                            //PosCollection regions = _getRectPts(pt);
                            //ACD.DB.CreateText((regions.Area() / 1000000).roundNumber(0.1) + "m\\U+00B2", pt);
                            _showAreaText(_getRectPts(pt), pt, 0);
                        }
                    }
                }
                ACD.Focus();
            }
        }
    }
}

