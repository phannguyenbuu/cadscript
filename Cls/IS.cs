using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AEC = Autodesk.Aec.Arch.DatabaseServices;

using System;
using System.Collections.Generic;
using System.Linq;
using AcadScript;
using System.Runtime.Serialization;

namespace AcadScript
{

    public enum EN_SELECT
    {
        AC_DXF = 0,
        AC_DXF_AND_NAME = 1,
        AC_DXF_AND_HYPERLINK = 2,
        AC_NAME = 3,
        AC_HYPERLINK = 4,
        AC_ALL = 5
    }

    public class IGetVerticesCLS
    {
        public bool PolylineClosed;
        Database db;
        int splitarc;
        public bool sort, addIfClosed;
        
        public IGetVerticesCLS(Database _db, int _splitarc, bool _sort, bool _addIfClosed = false)
        {
            db = _db;
            splitarc = _splitarc;

            //ACD.WR("SPLITARC: {0}", splitarc);

            sort = _sort;
            addIfClosed = _addIfClosed;
        }

        public PosCollection GetVertices(ObjectIdCollection ids)
        {
            PosCollection res = new PosCollection();
            //IGetVerticesCLS itm = new IGetVerticesCLS(db, _splitarc, _sort, _addIfClosed);

            foreach (ObjectId id in ids)
                if (db._isPolyline(id))
                {
                    List<pPos> ls = GetVertices(id).ToList();

                    if (ls.Count > 0)
                    {
                        //if (PolylineClosed)
                        //ls.Add(ls.First());
                        res.Add(ls.ToArray());
                        res.Closed = res.Closed.Add(PolylineClosed);
                    }
                }
            //else if (db._isBlock(id))
            //    res.AddRange(db._getVertices(db.GetEntInBlock(id)).Move(db._getPoint(id)));

            return res;
        }

        public pPos[] GetVertices(ObjectId objId)
        {
            List<pPos> res = new List<pPos>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                //LWP_CLS itm;
                PolylineClosed = false;

                switch (objId.ObjectClass.DxfName)
                {
                    case "DIMENSION":
                        Dimension dimNode = (Dimension)tr.GetObject(objId, OpenMode.ForRead);

                        if (dimNode is RotatedDimension)
                        {
                            RotatedDimension dim = (RotatedDimension)dimNode;
                            res = new List<pPos> { dim.XLine1Point.ToPos(), dim.XLine2Point.ToPos() };
                        }
                        break;

                    case "LINE":
                        Line ln = (Line)tr.GetObject(objId, OpenMode.ForWrite);
                        res = new List<pPos> { ln.StartPoint.ToPos(), ln.EndPoint.ToPos() };
                        break;

                    case "MULTILEADER":
                        {
                            MLeader ldr = (MLeader)tr.GetObject(objId, OpenMode.ForWrite);
                            res = new List<pPos>();
                            for (int i = 0; i < ldr.VerticesCount(0); i++)
                                res.Add(ldr.GetVertex(0, i).ToPos());
                            if (ldr.GetFirstVertex(0).ToPos().DistanceTo(res.First()) < 100)
                                res.Reverse();
                        }
                        break;

                    case "LEADER":
                        {
                            Leader ldr = (Leader)tr.GetObject(objId, OpenMode.ForWrite);
                            if (!ldr.IsSplined)
                            {
                                res = new List<pPos>();
                                for (int i = 0; i < ldr.NumVertices; i++)
                                    res.Add(ldr.VertexAt(i).ToPos());
                                if (ldr.StartPoint.ToPos().DistanceTo(res.First()) > 100)
                                    res.Reverse();
                            }
                            else
                                res = new List<pPos> { ldr.StartPoint.ToPos(), ldr.EndPoint.ToPos() };
                        }
                        break;

                    case "ARC":
                        //ACD.WR("SPLITARC_ARC: {0}", splitarc);
                        ObjectId tlwp = db.ArcToPolyline(objId, splitarc, false);
                        
                        res = GetVertices(tlwp).ToList();
                        ACD.DB.EraseObject(tlwp);

                        break;

                    case "LWPOLYLINE":
                        //ACD.WR("SPLITARC_LWP: {0}", splitarc);
                        Polyline lwp = (Polyline)tr.GetObject(objId, OpenMode.ForRead);
                        res = lwp._getLWPVertices(splitarc).ToList();
                        PolylineClosed = lwp.Closed;
                        break;
                    case "MLINE":
                        //ACD.WR("SPLITARC_LWP: {0}", splitarc);
                        Mline ml = (Mline)tr.GetObject(objId, OpenMode.ForRead);
                        res = DE.NumericArray(0, ml.NumberOfVertices - 1).Select(i => ml.VertexAt(i).ToPos()).ToList();
                        
                        break;
                    case "CIRCLE":
                        //itm = new LWP_CLS(db, objId);

                        //if (splitarc > 0)
                        //{
                        //    for (int i = 0; i < splitarc; i++)
                        //    {
                        //        double angle = i * (Math.PI * 2 / splitarc);
                        //        res.Add(new pPos(itm.Points[0].X + itm.Points[1].X * Math.Cos(angle),
                        //            itm.Points[0].Y + itm.Points[1].X * Math.Sin(angle)));
                        //    }
                        //} else
                        //{
                        //    //ACD.WR("Circle {0}", db._getPoint(objId));
                        //    res.Add(db._getPoint(objId));
                        //}

                        //PolylineClosed = true;
                        break;

                    case "ELLIPSE":
                        //itm = new LWP_CLS(db, objId);

                        //for (int i = 0; i < splitarc; i++)
                        //{
                        //    double angle = i * (Math.PI * 2 / splitarc);
                        //    res.Add(new pPos(itm.Points[0].X + itm.Points[1].X * Math.Cos(angle),
                        //        itm.Points[0].Y + itm.Points[1].Y * Math.Sin(angle)));
                        //}

                        //PolylineClosed = true;
                        break;

                    case "HATCH":
                        //Hatch ent = (Hatch)tr.GetObject(objId, OpenMode.ForWrite);
                        PosCollection tmps = db._getHatch(objId);

                        if (tmps.Count > 0)
                            res = tmps.First.ToList();

                        PolylineClosed = true;
                        break;

                    case "AEC_WALL":
                        sort = false;
                        Autodesk.Aec.Arch.DatabaseServices.Wall wall = (Autodesk.Aec.Arch.DatabaseServices.Wall)tr.GetObject(objId, OpenMode.ForWrite);
                        if (wall != null)
                        {
                            //WallOpeningDataCls walldata = new WallOpeningDataCls(db, wall.ObjectId);

                            //db.DrawPolyline(walldata.WallPoints, "LAYER=A-Wall");

                            //if (walldata.WallBoundPoints != null)
                            //{
                            //ObjectId tmp = db.GetWallShape(objId, 0);
                            res.Add(wall.StartPoint.ToPos());
                            res.Add(wall.EndPoint.ToPos());
                            //res.AddRange(db._getVertices(tmp));
                            //db.EraseObjects(tmp);
                            //}
                        }
                        break;

                    case "AEC_DOOR":
                        sort = false;
                        Autodesk.Aec.Arch.DatabaseServices.Door door = (Autodesk.Aec.Arch.DatabaseServices.Door)tr.GetObject(objId, OpenMode.ForRead);
                        res = new List<pPos> { door.StartPoint.ToPos(), door.EndPoint.ToPos() };
                        break;

                    case "AEC_WINDOW":
                        sort = false;
                        Autodesk.Aec.Arch.DatabaseServices.Window win = (Autodesk.Aec.Arch.DatabaseServices.Window)tr.GetObject(objId, OpenMode.ForRead);
                        res = new List<pPos> { win.StartPoint.ToPos(), win.EndPoint.ToPos() };
                        break;
                }

                tr.Commit();
            }

            if (res.Count > 1)
            {
                bool found = false;
                pPos[] bb = db._getBound(objId);

                if (bb != null)
                {
                    pPos centerpt = bb.CenterPoint();

                    for (int i = 0; i < 2; i++)
                    {
                        for (int j = 0; j < 2; j++)
                        {
                            pPos[] tmps = res.Select(pt => new pPos((i == 0 ? 1 : -1) * pt.X,
                                (j == 0 ? 1 : -1) * pt.Y)).ToArray();

                            if (tmps.Boundary().CenterPoint().DistanceTo(centerpt) < 10)
                            {
                                res = tmps.ToList();
                                found = true;
                                break;
                            }
                        }
                        if (found)
                            break;
                    }
                }

                if (PolylineClosed && addIfClosed)
                    res.Add(res.First());
            }

            //string notes = db.GetXNotes(objId);
            res = sort ? res.SortClockwise().ToList() : res.ToList();

            //if (!notes.empty())
            //res.First().Content = notes;
            //res.First().Content += "|Closed=True";
            return res.ToArray();
        }

        pPos[] _threePointOffset(pPos p1, pPos p2, pPos p3, double w)
        {
            pPos[] l1 = p1.Parallel(p2, w);
            pPos[] l2 = p2.Parallel(p3, w);
            pPos p4 = l1[0].Intersect(l1[1], l2[0], l2[1], false);

            return new pPos[] { l1.First(), p4, l2.Last() };
        }



        public pPos[] _getArc(Arc arc)
        {
            List<pPos> res = new List<pPos>();
            if (splitarc == 0)
                splitarc = 1;

            for (int i = 0; i <= splitarc; i++)
                res.Add(arc.GetPointAtDist(i * arc.Length / splitarc).ToPos());

            return res.ToArray();
        }

        
    }

    public static class ISR
    {

        static string ByDXF(ObjectId id)
        {
            string res = "";
            switch (id.ObjectClass.DxfName)
            {
                case "LWPOLYLINE": res = "LW"; break;
                case "CIRCLE": res = "CC"; break;
                case "TEXT": res = "TXT"; break;
                case "MTEXT": res = "TXT"; break;
            }
            return res;
        }

        public static double _getLength(this Database db, ObjectId objId, int splitarc = 0)
        {
            double res = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                switch (objId.ObjectClass.DxfName)
                {
                    case "DIMENSION":
                        Dimension dimNode = (Dimension)tr.GetObject(objId, OpenMode.ForRead);

                        if (dimNode is RotatedDimension)
                        {
                            RotatedDimension dim = (RotatedDimension)dimNode;
                            res = dim.XLine1Point.ToPos().DistanceTo(dim.XLine2Point.ToPos());
                        }
                        break;

                    case "LINE":
                        Line ln = (Line)tr.GetObject(objId, OpenMode.ForWrite);
                        res = ln.StartPoint.ToPos().DistanceTo(ln.EndPoint.ToPos());
                        break;

                    case "MULTILEADER":
                        {
                            MLeader ldr = (MLeader)tr.GetObject(objId, OpenMode.ForWrite);
                            for (int i = 1; i < ldr.VerticesCount(0); i++)
                                res += ldr.GetVertex(0, i).ToPos().DistanceTo(ldr.GetVertex(0, i - 1).ToPos());
                        }
                        break;

                    case "LEADER":
                        {
                            Leader ldr = (Leader)tr.GetObject(objId, OpenMode.ForWrite);
                            if (!ldr.IsSplined)
                                for (int i = 1; i < ldr.NumVertices; i++)
                                    res += ldr.VertexAt(i).ToPos().DistanceTo(ldr.VertexAt(i - 1).ToPos());
                            else
                                res = ldr.StartPoint.ToPos().DistanceTo(ldr.EndPoint.ToPos());
                        }
                        break;

                    case "ARC":
                        Arc arcObj = (Arc)tr.GetObject(objId, OpenMode.ForRead);
                        res = arcObj.Length;
                        break;

                    case "LWPOLYLINE":
                        Polyline lwp = (Polyline)tr.GetObject(objId, OpenMode.ForRead);
                        res = lwp.Length;
                        break;

                    case "CIRCLE":
                        Circle cir = (Circle)tr.GetObject(objId, OpenMode.ForRead);
                        res = Math.PI * cir.Diameter;
                        break;

                    case "ELLIPSE":
                        //LWP_CLS itm = new LWP_CLS(db, objId);
                        //List<pPos> pls = new List<pPos>();
                        //for (int i = 0; i < splitarc; i++)
                        //{
                        //    double angle = i * (Math.PI * 2 / splitarc);
                        //    pls.Add(new pPos(itm.Points[0].X + itm.Points[1].X * Math.Cos(angle),
                        //        itm.Points[0].Y + itm.Points[1].Y * Math.Sin(angle)));
                        //}

                        //res = pls.Length();
                        break;

                    case "HATCH":
                        pPos[] r = db._getHatch(objId).First;
                        if (r != null) res = r.Length();
                        //PolylineClosed = true;
                        break;

                    case "AEC_WALL":
                        Autodesk.Aec.Arch.DatabaseServices.Wall wall = (Autodesk.Aec.Arch.DatabaseServices.Wall)tr.GetObject(objId, OpenMode.ForWrite);
                        if (wall != null)
                            res = wall.Length;
                        break;

                    case "AEC_DOOR":
                        Autodesk.Aec.Arch.DatabaseServices.Door door = (Autodesk.Aec.Arch.DatabaseServices.Door)tr.GetObject(objId, OpenMode.ForRead);
                        res = door.StartPoint.ToPos().DistanceTo(door.EndPoint.ToPos());
                        break;

                    case "AEC_WINDOW":
                        Autodesk.Aec.Arch.DatabaseServices.Window win = (Autodesk.Aec.Arch.DatabaseServices.Window)tr.GetObject(objId, OpenMode.ForRead);
                        res = win.StartPoint.ToPos().DistanceTo(win.EndPoint.ToPos());
                        break;
                }

                tr.Commit();
            }
            return res;
        }

        public static ObjectIdCollection FilterIds(this Database db, ObjectIdCollection ids, Func<Database, ObjectId, bool> fn)
        {
            ObjectIdCollection res = new ObjectIdCollection();

            //ACD.WR("IR_IDS {0}", ids.Count);
            foreach (ObjectId id in ids)
                if (db.ValidId(id) && fn(db, id)) res.Add(id);
            return res;
        }

        public static ObjectIdCollection FilterIds(this Database db, ObjectIdCollection ids, params string[] keys)
        {
            return ids.Cast<ObjectId>().Where(id => keys.Contains(id.ObjectClass.DxfName)).Select(id => id).ToCollection();
        }


        public static double _getRadius(this Database db, ObjectId objId)
        {
            double res = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                switch (objId.ObjectClass.DxfName)
                {
                    case "CIRCLE":
                        Circle cir = (Circle)tr.GetObject(objId, OpenMode.ForRead);
                        res = cir.Radius;
                        break;
                    case "ARC":
                        Arc arc = (Arc)tr.GetObject(objId, OpenMode.ForRead);
                        res = arc.Radius;
                        break;
                }
                tr.Commit();
            }
            return res;
        }
        public static string _getIdInfo(this Database db, ObjectId id)
        {
            string res = "#" + ByDXF(id) + "=" + db._getLayer(id);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (db._isText(id))
                {
                    TextStyleTableRecord txtStyle = null;
                    if (id.ObjectClass.DxfName == "MTEXT")
                    {
                        MText txt = (MText)tr.GetObject(id, OpenMode.ForWrite);
                        txtStyle = (TextStyleTableRecord)tr.GetObject(txt.TextStyleId, OpenMode.ForRead);
                    }
                    else
                    {
                        DBText txt = (DBText)tr.GetObject(id, OpenMode.ForWrite);
                        txtStyle = (TextStyleTableRecord)tr.GetObject(txt.TextStyleId, OpenMode.ForRead);
                    }

                    if (txtStyle != null)
                        res = String.Format("#MTEXT={0}", txtStyle.Name);
                }
                else if (db._isDim(id))
                {
                    Dimension dim = (Dimension)tr.GetObject(id, OpenMode.ForWrite);
                    res = String.Format("#DIM={0}", dim.DimensionStyleName);
                }
                else if (db._isLine(id))
                {
                    res = "#LW=" + db._getLayer(id);
                }
                else if (db._isDoor(id))
                {
                    if (id.ObjectClass.DxfName == "AEC_DOOR")
                    {
                        AEC.Door door = (AEC.Door)tr.GetObject(id, OpenMode.ForRead);

                        if (door != null)
                            res = String.Format("#Door={0}|Width={1}|Height={2}|Sill=0",
                                db._getIdName(id), door.Width.roundNumber(10), door.Height.roundNumber(10));
                    }
                    else
                    {
                        AEC.Window door = (AEC.Window)tr.GetObject(id, OpenMode.ForRead);

                        if (door != null)
                            res = String.Format("#Door={0}|Width={1}|Height={2}|Sill={3}",
                                db._getIdName(id), door.Width.roundNumber(10),
                                door.Height.roundNumber(10), door.StartPoint.Z);
                    }
                }
                else if (db._isWall(id))
                {
                    AEC.Wall wall = (AEC.Wall)tr.GetObject(id, OpenMode.ForRead);

                    if (wall != null)
                    {
                        res = String.Format("#Element={0}|WIDTH={1}",
                            db._getIdName(id), wall.Width.roundNumber());
                    }
                }
                else if (db._isBlock(id))
                {
                    pPos[] bb = db._getBound(id);
                    res = String.Format("#INS={0}|", db._getIdName(id));
                    //res += db.GetBlockAtt(id);
                }
                else if (db._isHatch(id))
                {
                    Hatch hatch = (Hatch)tr.GetObject(id, OpenMode.ForRead);
                    res = "#HATCH=" + hatch.PatternName
                        + "|HSCALE=" + hatch.PatternScale;

                    if (hatch.PatternAngle > 0)
                        res += "|HANGLE=" + (hatch.PatternAngle / Math.PI * 180).roundNumber();
                }
                else if (db._isViewport(id))
                {
                    Viewport vw = (Viewport)tr.GetObject(id, OpenMode.ForRead);
                    res += String.Format("ViewCenter {0} Target {1} ViewHeight {2} Width {3} Height {4} Scale {5}",
                        vw.ViewCenter, vw.ViewTarget.ToPos(), vw.ViewHeight, vw.Width, vw.Height, vw.CustomScale);
                }
                else if (db._isCircle(id))
                {
                    res += String.Format("|RADIUS={0}", db._getRadius(id));
                }
                else if (db._isElip(id))
                {
                    Ellipse elip = (Ellipse)tr.GetObject(id, OpenMode.ForRead);
                    var v = elip.MajorAxis;
                    res += String.Format("MAJOR={0}|MINOR={1}|ANGLE={2}",
                        elip.MajorRadius, elip.MinorRadius, new pPos(v.X, v.Y).Angle());
                }

                if (db._isLine(id) || db._isPolyline(id) || db._isCircle(id) || db._isElip(id))
                {
                    Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                    if (ent != null)
                    {
                        LinetypeTableRecord ltype = (LinetypeTableRecord)tr.GetObject(ent.LinetypeId, OpenMode.ForRead);
                        //res += String.Format("|LTYPE={0}|LSCALE={1}|LCOLOR={2}",
                        //   ltype.Name, ent.LinetypeScale, ent.ColorIndex);

                        if (ltype.Name.Upper() != "BYLAYER")
                            res += "|LTYPE=" + ltype.Name;
                        if (ent.LinetypeScale != 1)
                            res += "|LSCALE=" + ent.LinetypeScale;
                        if (ent.ColorIndex != 256)
                            res += "|LCOLOR=" + ent.ColorIndex;

                        if (db._isPolyline(id) && db._getLineworkWidth(id) > 0)
                            res += "|LWIDTH=" + db._getLineworkWidth(id);
                    }
                }

                //pPos pt = db._isPoint(id) ? db._getPoint(id) : db._getBound(id).CenterPoint();
                //res += "|POS=" + pt.X.roundNumber(2).ToString() + "," + pt.Y.roundNumber(2).ToString();

                tr.Commit();
            }
            return res;
        }

        public static bool PolylineClosed;

        public static pPos[] _getVertices(this Database db, ObjectId objId,
            int _splitarc = 0, bool _sort = true, bool _addIfClosed = false)
        {
            //ACD.WR("SPLITARC: {0}", _splitarc);
            IGetVerticesCLS itm = new IGetVerticesCLS(db, _splitarc, _sort, _addIfClosed);
            pPos[] res = itm.GetVertices(objId);
            PolylineClosed = itm.PolylineClosed;
            return res;
        }


        public static PosCollection _getAllVertices(this Database db, ObjectIdCollection ids,
             int _splitarc = 0, bool _sort = true, bool _addIfClosed = false)
        {
            IGetVerticesCLS itm = new IGetVerticesCLS(db, _splitarc, _sort, _addIfClosed);
            return itm.GetVertices(ids);
        }
        static double _getArcBulge(Arc arc)
        {
            double deltaAng = arc.EndAngle - arc.StartAngle;
            if (deltaAng < 0)
                deltaAng += 2 * Math.PI;
            return Math.Tan(deltaAng * 0.25);
        }


        static void _addPointToList(List<pPos> pls, pPos pt)
        {
            //if (pls.Count == 0 || (pt.DistanceTo(pls.Last()) > 1 && pt.DistanceTo(pls.First()) > 1))
                pls.Add(pt);
        }


        public static pPos[] _getLWPVertices(this Polyline lwp, int splitarc = 16)
        {
            List<pPos> pls = new List<pPos>();

            int vn = lwp.NumberOfVertices;
            //PolylineClosed = lwp.Closed;

            for (int i = 0; i < vn; i++)
            {
                var type = lwp.GetSegmentType(i);
                if (splitarc > 0 && type.Equals(SegmentType.Arc))
                {
                    var arc2d = lwp.GetArcSegment2dAt(i);

                    var lsP = arc2d.GetSamplePoints(splitarc);
                    foreach (Point2d pt in lsP)
                        //pls.Add(pt.ToPos());
                        _addPointToList(pls, pt.ToPos2d());
                }
                else
                {
                    Point2d pt = lwp.GetPoint2dAt(i);
                    //pls.Add(pt.ToPos());
                    _addPointToList(pls, pt.ToPos2d());
                }
            }
            return pls.ToArray();
        }


        static Arc _createArcByThreePoints(this Database db, Point3d p1, Point3d p2, Point3d p3)
        {
            Arc arc = null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                CircularArc3d carc = new CircularArc3d(p1, p2, p3);

                Point3d cpt = carc.Center;
                Vector3d normal = carc.Normal;
                Vector3d refVec = carc.ReferenceVector;
                Plane plan = new Plane(cpt, normal);
                double ang = refVec.AngleOnPlane(plan);

                arc = new Arc(cpt, normal, carc.Radius, carc.StartAngle + ang, carc.EndAngle + ang);

                btr.AppendEntity(arc);
                tr.AddNewlyCreatedDBObject(arc, true);

                tr.Commit();
            }
            return arc;
        }

        public static ObjectId ArcToPolyline(this Database db, ObjectId objId, 
            int splitarc = 0, bool erase_source = true)
        {
            ObjectId res = ObjectId.Null;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                //ACD.WR("A_SPLITARC: {0}", splitarc);
                res = db._arc2poly((Arc)tr.GetObject(objId, OpenMode.ForRead), splitarc).ObjectId;

                if(erase_source)
                    ACD.DB.EraseObject(objId);

                tr.Commit();
            }

            return res;
        }

        public static Polyline _arc2poly(this Database db, Arc arc, int splitarc = 0)
        {
            Polyline poly = null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                //int splitarc = (int)pString.INI_String("SPLITARC").ToNumber(16);

                poly = new Polyline();

                Point2d p1 = new Point2d(arc.StartPoint.X, arc.StartPoint.Y);
                Point2d p2 = new Point2d(arc.EndPoint.X, arc.EndPoint.Y);

                poly.AddVertexAt(0, p2, _getArcBulge(arc), 0, 0);
                poly.AddVertexAt(1, p1, 0, 0, 0);
                poly.Closed = false;

                pPos[] pls = _getLWPVertices(poly);
                pPos pt = poly.GetPointAtDist(poly.Length / 2).ToPos(); //pls[Convert.ToInt32(pls.Length / 2)];
                pPos pj = arc.GetPointAtDist(arc.Length / 2).ToPos();
                if (pt.DistanceTo(pj) > 100)
                {
                    poly = new Polyline();
                    poly.AddVertexAt(0, p1, _getArcBulge(arc), 0, 0);
                    poly.AddVertexAt(1, p2, 0, 0, 0);
                }
                btr.AppendEntity(poly);
                tr.AddNewlyCreatedDBObject(poly, true);

                arc.UpgradeOpen();
                //arc.Erase();

                tr.Commit();
            }
            return poly;
        }

        public static Polyline _arcPoints(this Database db, pPos p1, pPos p2, pPos p3, pPos ct, double w)
        {
            pPos ps1 = p1.Along(-w, ct);
            pPos ps2 = p2.Along(-w, ct);
            pPos ps3 = p3.Along(-w, ct);

            return db._arc2poly(db._createArcByThreePoints(ps1.ToPoint3(), ps2.ToPoint3(), ps3.ToPoint3()));
        }

        public static string GetLinetypeText(this Database db, ObjectId lwpId)
        {
            string res = null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity ent = (Entity)tr.GetObject(lwpId, OpenMode.ForRead);
                LinetypeTableRecord lt = (LinetypeTableRecord)tr.GetObject(ent.LinetypeId, OpenMode.ForWrite);

                if (lt.NumDashes > 1 && lt.ShapeStyleAt(1) != ObjectId.Null)
                {
                    TextStyleTableRecord tb = (TextStyleTableRecord)tr.GetObject(lt.ShapeStyleAt(1), OpenMode.ForRead);

                    if (tb != null)
                        res = lt.TextAt(1);
                }
                tr.Commit();
            }

            return res;
        }



        public static bool _isPolylineClosed(this Database db, ObjectId objId)
        //public static pPos[] _getVertices(Database db, ObjectId objId, bool splitarc = true)
        {
            bool res = false;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (db._isPolyline(objId))
                {
                    Polyline lwp = (Polyline)tr.GetObject(objId, OpenMode.ForRead);
                    res = lwp.Closed;
                }else if(db._isMLine(objId))
                {
                    Mline ml = (Mline)tr.GetObject(objId, OpenMode.ForRead);
                    res = ml.IsClosed;
                }
                else if (db._isHatch(objId))
                    res = true;

                tr.Commit();
            }
            return res;
        }

        
        public static bool _getLayerState(this Database db, string sLayerName)
        {
            bool res = false;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Open the Layer table for read 
                LayerTable acLyrTbl = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                LayerTableRecord acLyrTblRec;
                if (acLyrTbl.Has(sLayerName))
                {
                    acLyrTblRec = tr.GetObject(acLyrTbl[sLayerName], OpenMode.ForWrite) as LayerTableRecord;

                    // Turn the layer off 
                    res = !acLyrTblRec.IsOff;

                }
                tr.Commit();
            }
            return res;
        }

        public static ObjectIdCollection FilterNotIds(this Database db, ObjectIdCollection ids, Func<Database, ObjectId, bool> fn)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            foreach (ObjectId id in ids)
                if (!fn(db, id)) res.Add(id);
            return res;
        }


        static bool _isWallExploded(this Database db, ObjectId id)
        {
            return db._isBlock(id) && db._getIdName(id).StartsWith("*") && db._getLayer(id) == "A-Wall";

        }



        public static double _getViewportScale(this Database db, ObjectId objId)
        {
            double res = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Viewport ent = (Viewport)tr.GetObject(objId, OpenMode.ForRead);
                res = ent.CustomScale;

                tr.Commit();
            }
            return res;
        }

        public static bool _isViewport(this Database db, ObjectId id)
        {
            return id.ObjectClass.DxfName == "VIEWPORT" && db._getViewportScale(id) != 1;
        }

        public static bool _isLevel(this ObjectId id, Database db)
        {
            if (id.ObjectClass.DxfName != "INSERT") return false;
            if (db._getIdName(id).Contains("LEVEL")) return true;
            return false;
        }

        public static bool _isRegion(this Database db, ObjectId objId)
        {
            return db._isBlock(objId) && (db._getIdName(objId).Upper().Contains("REGION"));
        }

        public static bool _isText(this Database db, ObjectId id)
        {
            return (id.ObjectClass.DxfName == "TEXT" || id.ObjectClass.DxfName == "MTEXT");
        }

        public static bool _isFurniture(this Database db, ObjectId id)
        {
            bool res = false;
            if (db._isBlock(id))
            {
                string stname = db._getIdName(id);
                res = stname != null && stname != "" && "UFI".Contains(stname[0]);
            }
            else if (db._getLayer(id).Upper().Contains("FUR"))
                res = true;

            return res;
        }






        public static bool isCurve(this ObjectId id)
        {
            return id.ObjectClass.DxfName == "SPLINE";
        }

        public static pPos[] _getDimPoints(this Database db, ObjectId objId)
        {
            pPos[] res = null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (objId.ObjectClass.DxfName == "DIMENSION")
                {
                    Entity ent = (Entity)tr.GetObject(objId, OpenMode.ForWrite);

                    if (ent is RotatedDimension)
                    {
                        RotatedDimension dim = (RotatedDimension)ent;
                        if (dim != null)
                        {
                            Point3dCollection p3ts = new Point3dCollection();
                            dim.GetStretchPoints(p3ts);

                            res = new pPos[] { p3ts[2].ToPos(), p3ts[3].ToPos() };
                        }
                    }
                }
                tr.Commit();
            }
            return res;
        }


        public static void _setDimValueRound(this Database db, ObjectId id, double round_value = 0)
        {
            //string res = "";
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                switch (id.ObjectClass.DxfName)
                {

                    case "DIMENSION":
                        Entity ent = (Entity)tr.GetObject(id, OpenMode.ForRead);

                        if (ent is RotatedDimension)
                        {
                            RotatedDimension dim = (RotatedDimension)ent;
                            dim.Dimrnd = round_value;
                        }

                        break;
                }

                tr.Commit();
            }

        }



        public static double _getLineworkWidth(this Database db, ObjectId id, double value = 0)
        {
            double res = 0;
            if (id.ObjectClass.DxfName == "LWPOLYLINE")
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        Polyline ent = (Polyline)tr.GetObject(id, OpenMode.ForWrite);
                        if (ent != null)
                            res = ent.ConstantWidth;
                    }
                    catch (System.Exception ex)
                    {
                        //ACD.WR("Error in get linework width of {0}", id.Handle.Value);
                    }
                    tr.Commit();
                }
            return res;
        }

        public static bool _isGLayer(this ObjectId objId, Database db)
        {
            bool res = false;
            string st = db._getLayer(objId);

            if (st.st_("G")) res = true;

            return res;
        }

        public static bool _isFurnLayer(this ObjectId objId, Database db)
        {
            bool res = false;
            string st = db._getLayer(objId);

            if (st.Upper().Contains("FURN"))
                res = true;

            return res;
        }

        public static string _getLayer(this Database db, ObjectId objId)
        {
            string res = null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity obj = (Entity)tr.GetObject(objId, OpenMode.ForRead);
                res = obj.Layer;
                tr.Commit();
            }
            return res;
        }
        public static bool _isCanDimObjId(this Database db, ObjectId id)
        {
            string[] DXFs = new string[] { "CIRCLE", "ELLIPSE", "ARC", "DIMENSION", "TEXT", "MTEXT" };
            return !db._isGObject(id) && !DXFs.Contains(id.ObjectClass.DxfName);
        }

        public static bool _isHiddenLayer(this ObjectId objId, Database db)
        {
            bool res = false;
            string st = db._getLayer(objId);

            if (st.Upper().Contains("HIDDEN"))
                res = true;

            return res;
        }

        public static bool _isGObject(this Database db, ObjectId objId)
        {
            return (db._isBlock(objId) && db._getIdName(objId).StartsWith("G")) || db._getLayer(objId).StartsWith("G");
        }

        public static bool _isEObject(this Database db, ObjectId objId)
        {
            return db._isBlock(objId) && db._getIdName(objId).StartsWith("E");
        }

        public static bool _isALayer(this ObjectId objId, Database db)
        {
            bool res = false;
            string st = db._getLayer(objId);

            if (st != null && st != "" && st.st_("A")) res = true;

            return res;
        }

        public static bool _isDefpointsLayer(this ObjectId objId, Database db)
        {
            return db._getLayer(objId).Upper() == "Defpoints";
        }

        public static bool _isAEC(this ObjectId id)
        {
            string[] aec = new string[] { "WALL", "WINDOW", "DOOR" };
            return aec.Any(st => id.ObjectClass.DxfName.Contains(st));
        }

        public static bool _isDim(this Database db, ObjectId id)
        {
            return id.ObjectClass.DxfName == "DIMENSION";
        }

        public static bool _isDim(this ObjectId id)
        {
            return id.ObjectClass.DxfName == "DIMENSION";
        }

        public static bool _isMLine(this Database db, ObjectId id)
        {
            return id.ObjectClass.DxfName == "MLINE";
        }

        public static bool _updateDim(this Database db, ObjectId id)
        {
            bool res = false;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity ent = (Entity)tr.GetObject(id, OpenMode.ForRead);
                    ent.UpgradeOpen();

                    tr.Commit();
                }

            return res;
        }


        public static bool _isRotateDim(this Database db, ObjectId id)
        {
            bool res = false;
            if (id.ObjectClass.DxfName == "DIMENSION")
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity ent = (Entity)tr.GetObject(id, OpenMode.ForRead);
                    res = ent is RotatedDimension;

                    RotatedDimension dim = (RotatedDimension)ent;
                    
                    tr.Commit();
                }

            return res;
        }

        public static bool _isAlignDim(this Database db, ObjectId id)
        {
            bool res = false;
            if (id.ObjectClass.DxfName == "DIMENSION")
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity ent = (Entity)tr.GetObject(id, OpenMode.ForRead);
                    res = ent is AlignedDimension;
                    tr.Commit();
                }

            return id.ObjectClass.DxfName == "DIMENSION";
        }

        public static bool _isDoor(this Database db, ObjectId id)
        {
            return id.ObjectClass.DxfName == "AEC_DOOR" || id.ObjectClass.DxfName == "AEC_WINDOW";
        }
        public static bool _isArc(this Database db, ObjectId id)
        {
            return id.ObjectClass.DxfName == "ARC";
        }
        public static bool _isCircle(this Database db, ObjectId id)
        {
            return id.ObjectClass.DxfName == "CIRCLE";
        }

        public static bool _isElip(this Database db, ObjectId id)
        {
            return id.ObjectClass.DxfName == "ELLIPSE";
        }

        public static bool _isHatch(this Database db, ObjectId id)
        {
            return id.ObjectClass.DxfName == "HATCH";
        }

        public static bool _isArray(this Database db, ObjectId id)
        {
            return db.ValidId(id) && AssocArray.IsAssociativeArray(id);
        }

        public static bool _isTable(this Database db, ObjectId id)
        {
            return id.ObjectClass.DxfName == "ACAD_TABLE";
        }

        public static bool _isWall(this Database db, ObjectId id)
        {
            return id.ObjectClass.DxfName == "AEC_WALL";
        }

        public static bool _isDXF(this Database db, ObjectId id, params string[] dxfs)
        {
            return dxfs.Any(s => s.Upper() == id.ObjectClass.DxfName.Upper());
        }

        public static bool _isLine(this Database db, ObjectId id)
        {
            return id.ObjectClass.DxfName == "LINE";
        }

        public static bool _isPoint(this Database db, ObjectId id)
        {
            return new string[] { "TEXT", "MTEXT", "CIRCLE", "INSERT", "ELLIPSE" }
                .Any(s => s.Upper() == id.ObjectClass.DxfName.Upper());
        }

        public static bool _isPolyline(this Database db, ObjectId id)
        {
            return id.ObjectClass.DxfName == "LWPOLYLINE";
        }

        public static bool _isBlock(this Database db, ObjectId id)
        {
            return id.ObjectClass.DxfName == "INSERT";// && !db._isArray(id);
        }

        public static void _setDoorSize(this Database db, ObjectId entId, double w, double h)
        {
            db._setDoorWidth(entId, w);
            db._setDoorWidth(entId, h);
        }

        public static void _setDoorSize(this Database db, ObjectId entId, pPos sz)
        {
            db._setDoorSize(entId, sz.X, sz.Y);
        }

        public static void _setDoorWidth(this Database db, ObjectId entId, double w)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (entId.ObjectClass.DxfName == "AEC_DOOR")
                {
                    AEC.Door door = (AEC.Door)tr.GetObject(entId, OpenMode.ForWrite);

                    if (door != null)
                    {
                        door.Width = w;
                    }
                }
                else
                {
                    AEC.Window window = (AEC.Window)tr.GetObject(entId, OpenMode.ForWrite);

                    if (window != null)
                    {
                        window.Width = w;
                    }
                }
                tr.Commit();
            }
        }

        public static void _setDoorHeight(this Database db, ObjectId entId, double w)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (entId.ObjectClass.DxfName == "AEC_DOOR")
                {
                    AEC.Door door = (AEC.Door)tr.GetObject(entId, OpenMode.ForWrite);

                    if (door != null)
                    {
                        door.Height = w;
                    }
                }
                else
                {
                    AEC.Window window = (AEC.Window)tr.GetObject(entId, OpenMode.ForWrite);

                    if (window != null)
                    {
                        window.Height = w;
                    }
                }
                tr.Commit();
            }
        }

        public static void AlignDoor(this Database db, ObjectId entId, pPos p1, pPos p2)
        {
            double w = p1.DistanceTo(p2).roundNumber(50);
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (entId.ObjectClass.DxfName == "AEC_DOOR")
                {
                    AEC.Door door = (AEC.Door)tr.GetObject(entId, OpenMode.ForWrite);

                    if (door != null)
                    {
                        door.Width = w;
                        door.Location = ((p1 + p2) / 2).ToPoint3();
                        //door.EndPoint = p2.ToPoint3();
                    }
                }
                else
                {
                    AEC.Window window = (AEC.Window)tr.GetObject(entId, OpenMode.ForWrite);

                    if (window != null)
                    {
                        window.Width = w;
                        window.Location = ((p1 + p2) / 2).ToPoint3();
                        //window.EndPoint = p2.ToPoint3();
                    }
                }
                tr.Commit();
            }
        }


        public static double _getDoorSill(this Database db, ObjectId entId)
        {
            double res = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (entId.ObjectClass.DxfName == "AEC_DOOR")
                {
                    AEC.Door door = (AEC.Door)tr.GetObject(entId, OpenMode.ForRead);

                    if (door != null)
                    {
                        res = door.StartPoint.Z;
                    }
                }
                else
                {
                    AEC.Window window = (AEC.Window)tr.GetObject(entId, OpenMode.ForRead);

                    if (window != null)
                    {
                        res = window.StartPoint.Z;
                    }
                }
                tr.Commit();
            }
            return res.roundNumber();
        }

        public static double _getDoorWidth(this Database db, ObjectId entId)
        {
            double res = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (entId.ObjectClass.DxfName == "AEC_DOOR")
                {
                    AEC.Door door = (AEC.Door)tr.GetObject(entId, OpenMode.ForRead);

                    if (door != null)
                    {
                        res = door.Width;
                    }
                }
                else
                {
                    AEC.Window window = (AEC.Window)tr.GetObject(entId, OpenMode.ForRead);

                    if (window != null)
                    {
                        res = window.Width;
                    }
                }
                tr.Commit();
            }
            return res.roundNumber();
        }

        public static double _getDoorHeight(this Database db, ObjectId entId)
        {
            double res = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (entId.ObjectClass.DxfName == "AEC_DOOR")
                {
                    AEC.Door door = (AEC.Door)tr.GetObject(entId, OpenMode.ForRead);

                    if (door != null)
                    {
                        res = door.Height;
                    }
                }
                else
                {
                    AEC.Window window = (AEC.Window)tr.GetObject(entId, OpenMode.ForRead);

                    if (window != null)
                    {
                        res = window.Height;
                    }
                }
                tr.Commit();
            }
            return res.roundNumber();
        }

        public static pPos _getDoorDirection(this Database db, ObjectId entId)
        {
            pPos res = null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (entId.ObjectClass.DxfName == "AEC_DOOR")
                {
                    AEC.Door door = (AEC.Door)tr.GetObject(entId, OpenMode.ForRead);

                    if (door != null)
                    {
                        res = new pPos(door.Direction.X, door.Direction.Y);
                    }
                }
                else
                {
                    AEC.Window window = (AEC.Window)tr.GetObject(entId, OpenMode.ForRead);

                    if (window != null)
                    {
                        res = new pPos(window.Direction.X, window.Direction.Y);
                    }
                }
                tr.Commit();
            }
            return res;
        }

        public static pPos[] _getDoor(this Database db, ObjectId entId)
        {
            List<pPos> res = new List<pPos>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (entId.ObjectClass.DxfName == "AEC_DOOR")
                {
                    AEC.Door door = (AEC.Door)tr.GetObject(entId, OpenMode.ForRead);

                    if (door != null)
                    {
                        res.Add(door.StartPoint.ToPos());
                        res.Add(door.EndPoint.ToPos());
                    }
                }
                else
                {
                    AEC.Window window = (AEC.Window)tr.GetObject(entId, OpenMode.ForRead);

                    if (window != null)
                    {
                        res.Add(window.StartPoint.ToPos());
                        res.Add(window.EndPoint.ToPos());
                    }
                }
                tr.Commit();
            }
            return res.ToArray();
        }

        public static void _setWallStyle(this Database db, ObjectId objId, string wallStyleName)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                AEC.Wall wall = (AEC.Wall)tr.GetObject(objId, OpenMode.ForWrite);
                if (wall != null)
                {
                    AEC.DictionaryWallStyle wallStyleDictionary = new AEC.DictionaryWallStyle(db);

                    if (wallStyleDictionary.Has(wallStyleName, tr))
                        wall.StyleId = wallStyleDictionary.GetAt(wallStyleName);
                }
                tr.Commit();
            }
        }

        public static void _setDoorStyle(this Database db, ObjectId objId, string doorStyleName)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (objId.ObjectClass.DxfName == "AEC_DOOR")
                {
                    AEC.Door door = (AEC.Door)tr.GetObject(objId, OpenMode.ForWrite);
                    if (door != null)
                    {
                        AEC.DictionaryDoorStyle doorStyleDictionary = new AEC.DictionaryDoorStyle(db);

                        if (doorStyleDictionary.Has(doorStyleName, tr))
                            door.StyleId = doorStyleDictionary.GetAt(doorStyleName);
                    }
                }
                else
                {
                    AEC.Window window = (AEC.Window)tr.GetObject(objId, OpenMode.ForRead);

                    if (window != null)
                    {
                        AEC.DictionaryWindowStyle doorStyleDictionary = new AEC.DictionaryWindowStyle(db);

                        if (doorStyleDictionary.Has(doorStyleName, tr))
                            window.StyleId = doorStyleDictionary.GetAt(doorStyleName);
                    }
                }
                tr.Commit();
            }
        }

        public static ObjectId _addBlock(this string blockName, pPos pt, string key, string val)

        {
            ObjectId res = ObjectId.Null;
            Database db = ACD.DB;

            using (Transaction myT = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;

                BlockTableRecord blockDef = bt[blockName].GetObject(OpenMode.ForRead) as BlockTableRecord;
                //Also open modelspace - we'll be adding our BlockReference to it

                BlockTableRecord ms = bt[BlockTableRecord.ModelSpace].GetObject(OpenMode.ForWrite) as BlockTableRecord;
                //Create new BlockReference, and link it to our block definition

                using (BlockReference blockRef = new BlockReference(pt.ToPoint3(), blockDef.ObjectId))
                {
                    //Add the block reference to modelspace
                    ms.AppendEntity(blockRef);
                    myT.AddNewlyCreatedDBObject(blockRef, true);

                    //Iterate block definition to find all non-constant

                    // AttributeDefinitions
                    if (!key.empty())
                        foreach (ObjectId id in blockDef)
                        {
                            DBObject obj = id.GetObject(OpenMode.ForRead);
                            AttributeDefinition attDef = obj as AttributeDefinition;

                            if ((attDef != null) && (!attDef.Constant) && key.Upper() == attDef.Tag.Upper())
                            {
                                using (AttributeReference attRef = new AttributeReference())
                                {
                                    attRef.SetAttributeFromBlock(attDef, blockRef.BlockTransform);
                                    attRef.TextString = val;
                                    //Add the AttributeReference to the BlockReference
                                    blockRef.AttributeCollection.AppendAttribute(attRef);
                                    myT.AddNewlyCreatedDBObject(attRef, true);
                                }
                            }
                        }
                    res = blockRef.ObjectId;
                    db._setLayer(blockRef.ObjectId, "A-Anno-Dims");
                }

                myT.Commit();
            }
            return res;
        }

        public static ObjectId _replaceTextByBlock(this string param, string val, pPos pt)
        {
            ObjectId res = ObjectId.Null;
            string key = param._firstPropName();
            string in_comma = key._getInComma();
            string r_key = key._getBeforeComma();

            if (r_key.empty()) r_key = key;

            //ACD.WR("r_key {0} in_comma {1} key {2}", r_key, in_comma, key);

            string[] ls_keys = param._firstProp().filter(";");
            //ACD.WR("ls_keys {0}", ls_keys.ToText());

            if (ls_keys.Any(s => val.st_(s.StartsWith(">") ? s.Upper() : (">" + s.Upper()))))
            {
                if (ACD.DB.HasBlock(r_key).IsNull)
                    ACD.DB.Insert(DE.CAD_TEMPLATE_FILE, r_key, new pPos[] { new pPos(0, 0, 0) });

                res = _addBlock(r_key, pt, in_comma, val.Substring(1));

                //ACD.WR("HAS");
            }

            return res;
        }

        public static void _replaceTextByBlock(this ObjectId id)
        {
            string val = ACD.DB._getContent(id);
            pPos pt = ACD.DB._getPoint(id);
            var chapters = ACD.LoadChapter(DE.INI_FILE, "INSERT KEY");

            if (val.st_(">"))
                foreach (string param in chapters)
                {
                    ObjectId new_id = _replaceTextByBlock(param, val, pt);

                    if (!new_id.IsNull)
                    {
                        ACD.DB.EraseObject(id);
                        break;
                    }
                }
        }


        //----------------------------------------------------------------------------------------
        public static string EffectiveName(this BlockReference block)
        {
            if (block == null)
                throw new ArgumentNullException(nameof(block));

            if (block.DynamicBlockTableRecord.Database.TransactionManager.TopTransaction == null)
                throw new Autodesk.AutoCAD.Runtime.Exception(Autodesk.AutoCAD.Runtime.ErrorStatus.NoActiveTransactions);

            return ((BlockTableRecord)block.DynamicBlockTableRecord.GetObject(OpenMode.ForRead)).Name;
        }

        public static string _getIdName(this Database db, ObjectId objId, bool upper = false)
        {
            string res = null;
            //db._insideBlock(objId);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                switch (objId.ObjectClass.DxfName)
                {
                    case "INSERT":
                        BlockReference blockRef = (BlockReference)tr.GetObject(objId, OpenMode.ForRead, true);
                        if (blockRef != null)
                        {
                            //ACD.WR("BlockREF = {0}\n", blockRef.IsDynamicBlock);
                            ObjectId id = blockRef.IsDynamicBlock ? blockRef.DynamicBlockTableRecord : blockRef.BlockTableRecord;

                            if (!id.IsNull && id.IsValid)
                            {
                                BlockTableRecord block = tr.GetObject(id, OpenMode.ForRead) as BlockTableRecord;
                                res = block.Name;

                                if (blockRef.IsDynamicBlock)
                                    res = ((BlockTableRecord)blockRef.DynamicBlockTableRecord.GetObject(OpenMode.ForRead)).Name;
                                //+ "|" + db.GetAllDynBlockProp(objId);

                                //ACD.WR("block_res = {0}\n", res);
                            }
                        }
                        break;
                    case "HATCH":
                        Hatch ent = (Hatch)tr.GetObject(objId, OpenMode.ForRead, true);
                        res = ent.PatternName + "|" + ent.PatternScale + "|" + ent.PatternAngle;
                        break;
                    case "MLINE":
                        Mline mline = (Mline)tr.GetObject(objId, OpenMode.ForRead, true);
                        MlineStyle ms = (MlineStyle)tr.GetObject(mline.Style, OpenMode.ForRead, true); 
                        res = ms.Name;
                        break;
                    case "MTEXT":
                        res = db._getContent(objId);
                        break;
                    case "TEXT":
                        res = db._getContent(objId);
                        break;
                    case "AEC_WALL":
                        AEC.Wall wall = (AEC.Wall)tr.GetObject(objId, OpenMode.ForRead);
                        if (wall != null)
                        {
                            AEC.WallStyle wstyle = (AEC.WallStyle)tr.GetObject(wall.StyleId, OpenMode.ForRead);
                            if (wstyle != null) res = wstyle.Name;
                        }
                        break;
                    case "AEC_DOOR":
                        AEC.Door door = (AEC.Door)tr.GetObject(objId, OpenMode.ForRead);

                        if (door != null)
                        {
                            AEC.DoorStyle dstyle = (AEC.DoorStyle)tr.GetObject(door.StyleId, OpenMode.ForRead);
                            if (dstyle != null) res = dstyle.Name;
                            //res += String.Format("#W{0}#H{1}", door.Width.roundNumber(), door.Height.roundNumber());
                        }
                        break;
                    case "AEC_WINDOW":
                        AEC.Window window = (AEC.Window)tr.GetObject(objId, OpenMode.ForRead);

                        if (window != null)
                        {
                            AEC.WindowStyle winstyle = (AEC.WindowStyle)tr.GetObject(window.StyleId, OpenMode.ForRead);
                            if (winstyle != null) res = winstyle.Name;
                            //res += String.Format("#W{0}#H{1}", window.Width.roundNumber(), window.Height.roundNumber());
                        }
                        break;
                    case "ACAD_TABLE":
                        Table tb = (Table)tr.GetObject(objId, OpenMode.ForRead);
                        if (tb != null)
                            res = tb.GetTextString(0, 0, 0);
                        break;
                }
                tr.Commit();
            }

            if (res != null && upper) res = res.Upper();
            return res;
        }

        public static string[] _getTabContent(this Database db, ObjectId tabId)
        {
            List<string> res = new List<string>();
            //bool _isKeyTab = db._getIdName(tabId).StartsWith("#");

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Table tb = (Table)tr.GetObject(tabId, OpenMode.ForRead);
                string[] headers = new string[tb.Columns.Count];

                for (int j = 0; j < tb.Columns.Count; j++)
                {
                    string content = tb.Cells[1, j].GetTextString(FormatOption.IgnoreMtextFormat);

                    if (!content.empty())
                    {
                        //int index = ITable.TEMPLATE_HEADER == null ? -1
                        //: Array.FindIndex(ITable.TEMPLATE_HEADER, st => st._prop("NAME") == content);
                        headers[j] = content;
                    }
                }

                //db.WRArray("HEADERS", headers);

                for (int i = 2; i < tb.Rows.Count; i++)
                {
                    string line = "";
                    if (!tb.Cells[i, 1].GetTextString(FormatOption.IgnoreMtextFormat).empty())
                    {
                        for (int j = 0; j < tb.Columns.Count; j++)
                        {
                            string st = tb.Cells[i, j].GetTextString(FormatOption.IgnoreMtextFormat);
                            if (st.empty()) st = "";
                            line += headers[j] + "=" + st + "|";
                        }
                        res.Add(line);
                    }
                }

                tr.Commit();
            }

            //if (!db._getIdName(tabId).EndsWith("_PREF"))
            //{

            //    ObjectId refTabId = db._getRefTable(tabId);
            //    if (!refTabId.IsNull)
            //    {
            //        string[] ref_contents = db._getTabContent(refTabId);

            //        if (ref_contents != null)
            //            for (int i = 0; i < res.Count; i++)
            //                if (i < ref_contents.Length && i < res.Count)
            //                    res[i] += "|" + ref_contents[i];
            //    }
            //}

            //db.WRArray("REFS", res);

            return res.ToArray();
        }

        public static double _getTextHeight(this Database db, ObjectId id)
        {
            double res = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                switch (id.ObjectClass.DxfName)
                {
                    case "TEXT":
                        DBText txt = (DBText)tr.GetObject(id, OpenMode.ForRead);
                        res = txt.Height;
                        break;
                    case "MTEXT":
                        MText stxt = (MText)tr.GetObject(id, OpenMode.ForRead);
                        res = stxt.Height;
                        break;
                }
            }
            return res;
        }

        public static string _getContent(this Database db, ObjectId id, bool upper = false, bool with_rtf = false)
        {
            string res = "";
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                switch (id.ObjectClass.DxfName)
                {
                    case "TEXT":
                        DBText txt = (DBText)tr.GetObject(id, OpenMode.ForRead);
                        res = txt.TextString;
                        break;
                    case "MTEXT":
                        MText stxt = (MText)tr.GetObject(id, OpenMode.ForRead);
                        res = with_rtf ? stxt.Contents : stxt.Text;
                        break;
                    case "MULTILEADER":
                        MLeader mtxt = (MLeader)tr.GetObject(id, OpenMode.ForRead);
                        if (mtxt.HasContent())
                            res = with_rtf ? mtxt.MText.Contents : mtxt.MText.Text;
                        break;
                    case "ACAD_TABLE":
                        //string[] contents = db._getTabContent(id);

                        //if (contents.Length > 0)
                        //    foreach (string content in contents)
                        //        res += content + "\r\n";
                        break;
                    case "DIMENSION":
                        Entity ent = (Entity)tr.GetObject(id, OpenMode.ForRead);

                        if (ent is RotatedDimension)
                        {
                            RotatedDimension dim = (RotatedDimension)ent;
                            res = dim.DimensionText;
                        }
                        else if (ent is AlignedDimension)
                        {
                            AlignedDimension dim = (AlignedDimension)ent;
                            res = dim.DimensionText;
                        }
                        else if (ent is RadialDimension)
                        {
                            RadialDimension dim = (RadialDimension)ent;
                            res = dim.DimensionText;
                        }
                        if (ent is OrdinateDimension)
                        {
                            OrdinateDimension dim = (OrdinateDimension)ent;
                            res = dim.DimensionText;
                        }
                        break;
                }

                tr.Commit();
            }
            if (res != null && upper)
                res = res.ToUpper();

            //ACD.WR("VAL_STR {0},{1}", res, res.UniConvert());

            return res;
        }


        //--------------------------------------------------------------------------------------------------
        public static pPos _getPoint(this Database db, ObjectId objId, int index = 0)
        {
            pPos res = null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                switch (objId.ObjectClass.DxfName)
                {
                    case "CIRCLE":
                        Circle cir = (Circle)tr.GetObject(objId, OpenMode.ForRead);
                        res = cir.Center.ToPos();
                        break;
                    case "ELLIPSE":
                        Ellipse elp = (Ellipse)tr.GetObject(objId, OpenMode.ForRead);
                        res = elp.Center.ToPos();
                        break;
                    case "MULTILEADER":
                        {
                            MLeader ldr = (MLeader)tr.GetObject(objId, OpenMode.ForWrite);
                            pPos pt = ldr.GetVertex(0, 0).ToPos();
                            res = ldr.GetFirstVertex(0).ToPos();

                            //db.DrawCircle(res, 20, "LAYER=DEFPOINTS");
                        }
                        break;

                    case "LEADER":
                        {
                            Leader ldr = (Leader)tr.GetObject(objId, OpenMode.ForWrite);
                            res = ldr.StartPoint.ToPos();
                        }
                        break;
                    case "POINT":
                        DBPoint point = (DBPoint)tr.GetObject(objId, OpenMode.ForRead);
                        res = point.Position.ToPos();
                        break;
                    case "MTEXT":
                        MText mtxt = (MText)tr.GetObject(objId, OpenMode.ForRead);
                        res = mtxt.Location.ToPos();
                        res.Content = db._getContent(objId);
                        break;
                    case "TEXT":
                        DBText txt = (DBText)tr.GetObject(objId, OpenMode.ForRead);
                        res = txt.Position.ToPos();
                        res.Content = db._getContent(objId);
                        break;
                    case "HATCH":
                        Hatch hatch = (Hatch)tr.GetObject(objId, OpenMode.ForRead);
                        Point3dCollection p3ds = new Point3dCollection();
                        IntegerCollection snaps = new IntegerCollection();
                        IntegerCollection idss = new IntegerCollection();
                        hatch.GetGripPoints(p3ds, snaps, idss);

                        //foreach (Point3d tmp in p3ds)
                        //    db.DrawCircle(tmp.ToPos(), 100);
                        res = p3ds[0].ToPos();
                        break;
                    case "INSERT":
                        BlockReference block = (BlockReference)tr.GetObject(objId, OpenMode.ForRead);
                        res = block.Position.ToPos();
                        res.Content = db._getIdName(objId);
                        break;
                    case "DIMENSION":
                        Entity ent = (Entity)tr.GetObject(objId, OpenMode.ForWrite);

                        if (ent is RotatedDimension)
                        {
                            RotatedDimension dim = (RotatedDimension)ent;
                            if (dim != null)
                            {
                                if (index == 0)
                                    res = dim.XLine1Point.ToPos();
                                else if (index == 1)
                                    res = dim.XLine2Point.ToPos();
                                if (index == 2)
                                    res = dim.DimLinePoint.ToPos();


                                //ACD.WR("Pt:{0}", dim.DimLinePoint.ToPos());
                                //db.DrawCircle(dim.DimLinePoint.ToPos(), 100);
                                //res = (index == 0 ? dim.XLine1Point : dim.XLine2Point).ToPos();
                            }
                        }
                        else if (ent is OrdinateDimension)
                        {
                            OrdinateDimension dim = (OrdinateDimension)ent;
                            if (dim != null)
                            {
                                if (index == 0)
                                    res = dim.LeaderEndPoint.ToPos();
                                else if (index == 1)
                                    res = dim.DefiningPoint.ToPos();
                                else if (index == 2)
                                    res = dim.TextPosition.ToPos();
                            }
                        }
                        else if (ent is AlignedDimension)
                        {
                            AlignedDimension dim = (AlignedDimension)ent;
                            if (dim != null)
                            {
                                if (index == 0)
                                    res = dim.XLine1Point.ToPos();
                                else if (index == 1)
                                    res = dim.XLine2Point.ToPos();
                                else if (index == 2)
                                    res = dim.DimLinePoint.ToPos();
                            }
                        }
                        break;
                    case "ACAD_TABLE":
                        Table tb = (Table)tr.GetObject(objId, OpenMode.ForRead);
                        if (tb != null)
                        {
                            res = tb.Position.ToPos();
                        }
                        break;
                    case "AEC_DOOR":
                        Autodesk.Aec.Arch.DatabaseServices.Door door
                            = (Autodesk.Aec.Arch.DatabaseServices.Door)tr.GetObject(objId, OpenMode.ForRead);
                        res = door.Location.ToPos();
                        break;

                    case "AEC_WINDOW":
                        Autodesk.Aec.Arch.DatabaseServices.Window window
                            = (Autodesk.Aec.Arch.DatabaseServices.Window)tr.GetObject(objId, OpenMode.ForRead);
                        res = window.Location.ToPos();
                        break;

                    case "AEC_WALL":
                        Autodesk.Aec.Arch.DatabaseServices.Wall wall
                            = (Autodesk.Aec.Arch.DatabaseServices.Wall)tr.GetObject(objId, OpenMode.ForRead);
                        res = index == 0 ? wall.StartPoint.ToPos() : wall.EndPoint.ToPos();
                        break;

                        //default:
                        //    pPos[] pts = db._getVertices(objId);
                        //    if (pts != null && pts.Length > 0)
                        //        res = pts[index];
                        //    break;
                }
                tr.Commit();
            }
            return res;
        }


        

        public static pPos[] _getBound(this Database db, ObjectId objId, double offset = 0, bool _isArray = false)
        {
            pPos[] res = null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    Entity obj = (Entity)tr.GetObject(objId, OpenMode.ForRead);
                    var ext = obj.GeometricExtents;

                    if (ext.MinPoint.X._isValidNumber() && ext.MinPoint.Y._isValidNumber()
                        && ext.MaxPoint.X._isValidNumber() && ext.MaxPoint.Y._isValidNumber())
                        res = new pPos[] { new pPos(ext.MinPoint.X - offset, ext.MinPoint.Y - offset),
                                new pPos(ext.MaxPoint.X + offset, ext.MaxPoint.Y + offset) }.Boundary();
                }
                catch (System.Exception ex)
                {
                }

                tr.Commit();
            }
            return res;
        }
        public static pPos[] _getBound(this Database db, ObjectIdCollection ids, double offset = 0, params string[] strExcludeDxfs)
        {
            PosCollection pls = ids.Cast<ObjectId>()
                .Where(id => !strExcludeDxfs.Contains(id.ObjectClass.DxfName)
                && db._getBound(id) != null && !db._getBound(id)[0].IsNull && !db._getBound(id)[1].IsNull)
                .Select(id => db._getBound(id, offset)).ToCollectionSameClosed();
            pPos[] res = null;
            if (pls.Count > 0) res = pls.Boundary;
            return res;
        }

        public static ObjectIdCollection _getHatchBoundary(this Database db, ObjectId objId)
        {
            //Document doc = Application.DocumentManager.MdiActiveDocument;
            //Editor ed = doc.Editor;
            //PromptEntityOptions prOps = new PromptEntityOptions("\nSelect Hatch: ");
            //prOps.SetRejectMessage("\nNot a Hatch");
            //prOps.AddAllowedClass(typeof(Hatch), false);
            //PromptEntityResult prRes = ed.GetEntity(prOps);

            //if (prRes.Status != PromptStatus.OK) return;

            ObjectIdCollection res = new ObjectIdCollection();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Hatch hatch = tr.GetObject(objId, OpenMode.ForRead) as Hatch;
                if (hatch != null)
                {
                    BlockTableRecord btr = tr.GetObject(hatch.OwnerId, OpenMode.ForWrite) as BlockTableRecord;
                    if (btr == null) return null;

                    Plane plane = hatch.GetPlane();
                    int nLoops = hatch.NumberOfLoops;

                    for (int i = 0; i < nLoops; i++)
                    {
                        HatchLoop loop = hatch.GetLoopAt(i);
                        if (loop.IsPolyline)
                        {
                            using (Polyline poly = new Polyline())
                            {
                                int iVertex = 0;
                                foreach (BulgeVertex bv in loop.Polyline)
                                {
                                    poly.AddVertexAt(iVertex++, bv.Vertex, bv.Bulge, 0.0, 0.0);
                                }

                                btr.AppendEntity(poly);
                                tr.AddNewlyCreatedDBObject(poly, true);

                                res.Add(poly.ObjectId);
                            }
                        }
                        else
                        {
                            foreach (Curve2d cv in loop.Curves)
                            {
                                LineSegment2d line2d = cv as LineSegment2d;
                                CircularArc2d arc2d = cv as CircularArc2d;
                                EllipticalArc2d ellipse2d = cv as EllipticalArc2d;
                                NurbCurve2d spline2d = cv as NurbCurve2d;
                                if (line2d != null)
                                {
                                    using (Line ent = new Line())
                                    {
                                        ent.StartPoint = new Point3d(plane, line2d.StartPoint);
                                        ent.EndPoint = new Point3d(plane, line2d.EndPoint);
                                        btr.AppendEntity(ent);
                                        tr.AddNewlyCreatedDBObject(ent, true);

                                        res.Add(ent.ObjectId);
                                    }
                                }
                                else if (arc2d != null)
                                {
                                    if (Math.Abs(arc2d.StartAngle - arc2d.EndAngle) < 1e-5)
                                    {
                                        using (Circle ent = new Circle(new Point3d(plane, arc2d.Center), plane.Normal, arc2d.Radius))
                                        {
                                            btr.AppendEntity(ent);
                                            tr.AddNewlyCreatedDBObject(ent, true);

                                            res.Add(ent.ObjectId);
                                        }
                                    }
                                    else
                                    {
                                        double angle = new Vector3d(plane, arc2d.ReferenceVector).AngleOnPlane(plane);
                                        using (Arc ent = new Arc(new Point3d(plane, arc2d.Center), arc2d.Radius, arc2d.StartAngle + angle, arc2d.EndAngle + angle))
                                        {
                                            btr.AppendEntity(ent);
                                            tr.AddNewlyCreatedDBObject(ent, true);

                                            res.Add(ent.ObjectId);
                                        }
                                    }
                                }
                                else if (ellipse2d != null)
                                {
                                    //-------------------------------------------------------------------------------------------
                                    // &#1054;&#1096;&#1080;&#1073;&#1082;&#1072;: &#1053;&#1077;&#1083;&#1100;&#1079;&#1103; &#1087;&#1088;&#1080;&#1089;&#1074;&#1086;&#1080;&#1090;&#1100; StartParam &#1080; EndParam &#1087;&#1088;&#1080;&#1084;&#1080;&#1090;&#1080;&#1074;&#1091;  &#1101;&#1083;&#1083;&#1080;&#1087;&#1089;:
                                    // Ellipse ent = new Ellipse(new Point3d(plane, e2d.Center), plane.Normal,
                                    //      new Vector3d(plane,e2d.MajorAxis) * e2d.MajorRadius,
                                    //      e2d.MinorRadius / e2d.MajorRadius, e2d.StartAngle, e2d.EndAngle);
                                    // ent.StartParam = e2d.StartAngle;
                                    // ent.EndParam = e2d.EndAngle;
                                    // error CS0200: Property or indexer 'Autodesk.AutoCAD.DatabaseServices.Curve.StartParam' cannot be assigned to -- it is read only
                                    // error CS0200: Property or indexer 'Autodesk.AutoCAD.DatabaseServices.Curve.EndParam' cannot be assigned to -- it is read only
                                    //---------------------------------------------------------------------------------------------
                                    // &#1054;&#1073;&#1093;&#1086;&#1076;&#1080;&#1084; &#1101;&#1090;&#1091; &#1086;&#1096;&#1080;&#1073;&#1082;&#1091; &#1080;&#1089;&#1087;&#1086;&#1083;&#1100;&#1079;&#1091;&#1103; «&#1086;&#1090;&#1088;&#1072;&#1078;&#1077;&#1085;&#1080;&#1077;» (Reflection)
                                    //
                                    using (Ellipse ent = new Ellipse(new Point3d(plane, ellipse2d.Center), plane.Normal,
                                         new Vector3d(plane, ellipse2d.MajorAxis) * ellipse2d.MajorRadius,
                                         ellipse2d.MinorRadius / ellipse2d.MajorRadius, ellipse2d.StartAngle, ellipse2d.EndAngle))
                                    {
                                        ent.GetType().InvokeMember("StartParam", System.Reflection.BindingFlags.SetProperty, null,
                                          ent, new object[] { ellipse2d.StartAngle });
                                        ent.GetType().InvokeMember("EndParam", System.Reflection.BindingFlags.SetProperty, null,
                                          ent, new object[] { ellipse2d.EndAngle });
                                        btr.AppendEntity(ent);
                                        tr.AddNewlyCreatedDBObject(ent, true);

                                        res.Add(ent.ObjectId);
                                    }

                                }
                                else if (spline2d != null)
                                {
                                    if (spline2d.HasFitData)
                                    {
                                        NurbCurve2dFitData n2fd = spline2d.FitData;
                                        using (Point3dCollection p3ds = new Point3dCollection())
                                        {
                                            foreach (Point2d p in n2fd.FitPoints) p3ds.Add(new Point3d(plane, p));
                                            using (Spline ent = new Spline(p3ds, new Vector3d(plane, n2fd.StartTangent), new Vector3d(plane, n2fd.EndTangent),
                                              /* n2fd.KnotParam, */  n2fd.Degree, n2fd.FitTolerance.EqualPoint))
                                            {
                                                btr.AppendEntity(ent);
                                                tr.AddNewlyCreatedDBObject(ent, true);

                                                res.Add(ent.ObjectId);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        NurbCurve2dData n2fd = spline2d.DefinitionData;
                                        using (Point3dCollection p3ds = new Point3dCollection())
                                        {
                                            DoubleCollection knots = new DoubleCollection(n2fd.Knots.Count);
                                            foreach (Point2d p in n2fd.ControlPoints) p3ds.Add(new Point3d(plane, p));
                                            foreach (double k in n2fd.Knots) knots.Add(k);
                                            double period = 0;
                                            using (Spline ent = new Spline(n2fd.Degree, n2fd.Rational,
                                                     spline2d.IsClosed(), spline2d.IsPeriodic(out period),
                                                     p3ds, knots, n2fd.Weights, n2fd.Knots.Tolerance, n2fd.Knots.Tolerance))
                                            {
                                                btr.AppendEntity(ent);
                                                tr.AddNewlyCreatedDBObject(ent, true);

                                                res.Add(ent.ObjectId);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                tr.Commit();
            }

            return res;
        }

        public static PosCollection _getHatch(this Database db, ObjectId objId)
        {
            PosCollection pls = new PosCollection();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Hatch ent = (Hatch)tr.GetObject(objId, OpenMode.ForRead);

                if (ent.NumberOfLoops > 0)
                {
                    for (int index = 0; index < ent.NumberOfLoops; index++)
                    {
                        HatchLoop loop = ent.GetLoopAt(index);

                        if (loop.IsPolyline)
                        {
                            //ACD.WR("MoD_1");
                            pls.Add(loop.Polyline.Cast<BulgeVertex>()
                                .Select(bv => new pPos(bv.Vertex.X, bv.Vertex.Y)).ToArray());
                        }
                        else
                        {
                            //ACD.WR("MoD_2");
                            Curve2dCollection c2c = loop.Curves;
                            List<pPos> ls = new List<pPos>();

                            foreach (Curve2d c2 in c2c)
                            {
                                pPos pt = c2.StartPoint.ToPos2d();
                                if (!pt.IsNull && ls.All(p => p.DistanceTo(pt) > 1))
                                    ls.Add(pt);

                                pt = c2.EndPoint.ToPos2d();
                                if (!pt.IsNull && ls.All(p => p.DistanceTo(pt) > 1))
                                    ls.Add(pt);
                            }

                            pls.Add(ls.ToArray());
                        }
                    }
                    tr.Commit();
                }
            }

            pls = pls.OrderBy(ls => -ls.Area()).ToCollectionSameClosed();
            return pls;
        }



        //--------------------------------
        

    }

    public class HatchAnalysCLS
    {
        string curxnote, prefix;
        string[] hatch_keys;

        int hatch_index;
        //double z_depth = 0;

        Action<ObjectIdCollection> act;
        //Func<string, string> convertnote;
        ObjectIdCollection hatchIds;
        ObjectIdCollection drawingHatchIds;

        public EventHandler AllRaiseEventCompleted = new EventHandler((e, a) => { });
        Dictionary<string, string> XNoteKeysDict;

        public HatchAnalysCLS(ObjectIdCollection _hIds, Action<ObjectIdCollection> _act)
        {
            ACD.DB.GetEntities(null, EN_SELECT.AC_ALL);
            allIds = IR.SelectedIds;
            drawingHatchIds = allIds.ToList().Where(id => ACD.DB._isHatch(id)).ToCollection();

            hatchIds = _hIds;
            act = _act;

            XNoteKeysDict = new Dictionary<string, string>();

            hatch_keys = hatchIds.ToList().Select(id
                => ACD.DB._getIdName(id).Upper()).Distinct().ToArray();

            ACD.WR("hatch_keys {0}", hatch_keys.Length);

            foreach (string key in hatch_keys)
            {
                XNoteKeysDict.Add(key, _getXNoteByKey(key));
                ACD.WR("keys {0} [{1}]", key, _getXNoteByKey(key));
            }

            foreach (ObjectId hId in hatchIds)
                ACD.DB._setHatchElevation(hId, ACD.DB.GetXNotes(hId)._props("H").ToNumber());

            hatch_index = 0;

            ACD.DOC.CommandEnded += CommandEnd;
            ACD.DOC.CommandWillStart += (o, e) =>
            {
                acadBusy = true;
            };

            _analysItem();
        }

        string _getXNoteByKey(string key)
        {
            string res = "";
            ObjectIdCollection dIds = drawingHatchIds.ToList()
                .Where(id => ACD.DB._getIdName(id).Upper() == key).ToCollection();

            if (dIds.Count > 0)
            {
                string xnote = dIds.ToList()
                    //.Where(id => ACD.DB.GetXNotes(id) != null)
                    .SelectMany(id => ACD.DB.GetXNotes(id)).ToTextStr("|");
                //ACD.WR("OK3");
                res = xnote._allPropNames().Select(s => s + "=" + xnote._prop(s)).ToTextStr("|");

            }

            return res;
        }

        bool acadBusy = false;

        void _analysItem()
        {
            string key = hatch_keys[hatch_index];

            ObjectIdCollection nIds = hatchIds.ToList().Where(id
                => ACD.DB._getIdName(id).Upper() == key.Upper()).ToCollection();

            curxnote = XNoteKeysDict[key];

            ACD.WR("Key {0} CurXNote {1}", key, curxnote);
            prefix = hatch_keys[hatch_index].Replace("|", "$").Replace("=", "~");

            string cmd = nIds.ToList().Where(id => ACD.DB.ValidId(id))
                .Select(id => "(handent \"" + id.Handle.ToString() + "\") ").ToTextStr("");

            if (cmd.EndsWith(" "))
                cmd = cmd.Substring(0, cmd.Length - 1);

            cmd = "_.HATCHGENERATEBOUNDARY " + cmd + "\n\n";
            ACD.WR("CMD {0}", cmd);
            ACD.DOC.SendStringToExecute(cmd, true, false, true);

            while (acadBusy) ;
        }

        ObjectIdCollection allIds;

        void CommandEnd(object o, EventArgs e)
        {
            acadBusy = false;
            //ACD.WR("Command End");

            if (hatch_index != -1)
            {
                ACD.DB.GetEntities(null, EN_SELECT.AC_ALL);
                ObjectIdCollection newIds = IR.SelectedIds.ToList().Where(id => !allIds.Contains(id)).ToCollection();

                ACD.WR("Index {0}/{1}, {2}, Objs {3} ------------------\n CurXNote {4}", hatch_index,
                    hatch_keys.Length, hatch_keys[hatch_index], newIds.Count, curxnote);

                if (newIds.Count > 0)
                {
                    act(newIds);
                    ACD.DB.EraseObjects(newIds);
                }

                hatch_index++;

                if (hatch_index < hatch_keys.Length)
                    _analysItem();
                else
                {
                    hatch_index = -1;
                    ACD.DOC.CommandEnded -= CommandEnd;
                    //RemoveCmdEndEvent();
                    foreach (ObjectId hId in hatchIds)
                    {
                        //ACD.WR("Hatch {0}:{1}", hId.Handle.ToString(), ACD.DB._getElevation(hId));
                        ACD.DB._setHatchElevation(hId, 0);
                    }

                    AllRaiseEventCompleted(null, EventArgs.Empty);
                }
            }
        }
    }

    public class iGridObjectCLS
    {
        List<List<double[]>> gridXYs;
        List<string[]> infors;

        public iGridObjectCLS(ObjectIdCollection ids)
        {
            gridXYs = ids.ToList().Where(id => ACD.DB._isGrid(id))
                .Select(id => ACD.DB.GetGridXY(new ObjectIdCollection { id }))
                .OrderBy(ls => ls[0].First()).ThenBy(ls => ls[1].Last()).ToList();

            infors = ids.ToList().Where(id => ACD.DB._isGrid(id))
                .Select(id => ACD.DB.GetXNotes(id))
                .ToList();
        }

        public int FindGridIndexFromPoint(pPos pt)
        {
            int[] ar = DE.NumericArray(0, gridXYs.Count - 1).Where(n
                => gridXYs[n][0].First() <= pt.X && pt.X <= gridXYs[n][0].Last()
                && gridXYs[n][1].First() <= pt.Y && pt.Y <= gridXYs[n][1].Last()).ToArray();

            return ar.Length > 0 ? ar.First() : -1;
        }

        int _pointGridByAxis(pPos pt, int axis)
        {
            int index = FindGridIndexFromPoint(pt);
            int res = -1;

            if (index != -1)
            {
                var ls = gridXYs[index];

                if (pt.X < ls[axis].First()) res = -1;
                else if (pt.X > ls[axis].Last()) res = ls.Count;

                int[] ar = DE.NumericArray(0, ls.Count - 2).Where(n
                => ls[axis][n] <= pt[axis] && pt[axis] <= ls[axis][n]).ToArray();

                res = ar.Length > 0 ? ar.First() : -1;
            }

            return res;
        }

        public int PointGridXIndex(pPos pt)
        {
            return _pointGridByAxis(pt, 0);
        }

        public int PointGridYIndex(pPos pt)
        {
            return _pointGridByAxis(pt, 0);
        }

        public string[] this[int index]
        {
            get { return infors[index]; }
            set { infors[index] = value; }
        }

        public override string ToString()
        {
            string res = "Grid " + gridXYs.Count + " items:";

            for (int i = 0; i < gridXYs.Count; i++)
            {
                res += "\r\nX:" + gridXYs[i][0].ToTextDouble(",");
                res += "\r\nY:" + gridXYs[i][1].ToTextDouble(",");
                res += "\r\nInfo:" + infors[i].ToTextStr(",") + "\r\n";
            }

            return res;
        }


    }
}
