using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Internal;
using AEC = Autodesk.Aec.Arch.DatabaseServices;

using System;
using System.Linq;
using System.Collections.Generic;

namespace AcadScript
{
    public class note_struct
    {
        public string contents;
        public pPos note_point, start_point;
        
        public note_struct(string _s, pPos _p, pPos _startp)
        {
            contents = _s;
            note_point = _p;
            start_point = _startp;
        }
    }

    public class INote
    {
        public double MinY, MaxY, MinX, MaxX, TextSize;
        Database db;

        public List<note_struct> note_contents;

        public INote(Database _db, pPos p1, pPos p2, double _textsize = 100)
        {
            db = _db;
            TextSize = _textsize;

            note_contents = new List<note_struct>();

            MinY = p1.Y;
            MaxY = p2.Y;
            MinX = p1.X;
            MaxX = p2.X;
        }

        public void AddNote(string st, pPos p)
        {
            if (!st.empty())
            {
                note_contents.Add(new note_struct(st, p, new pPos(MinX,
                    p != null ? (Math.Abs(p.Y - MinY) < Math.Abs(p.Y - MaxY) ? MinY : MaxY) : MinY)));
            }
        }

        double _segDistanceTo(pPos[] segs)
        {
            double res = 0;
            pPos p = segs.CenterPoint();

            if (Math.Abs(p.Y - MinY) < Math.Abs(p.Y - MaxY))
                res = Math.Abs(p.Y - MinY);
            else
                res = Math.Abs(p.Y - MaxY);

            return res;
        }

        public void AddNote(string key, IEnumerable<pPos> value)
        {
            string[] excludes = new string[] { "SHAPE", "HATCH" };
            string st = key._firstProp();

            bool isSpline = key.st_("#SPLINE");

            pPos ct = new pPos((MinX + MaxX) / 2, (MinY + MaxY) / 2);

            if (!excludes.Any(s => st.ToUpper().StartsWith(s)))
            {
                double ny = 0;
                pPos p = null;
                if (!isSpline)
                {
                    p = value.CenterPoint();
                    ny = Math.Abs(p.Y - MinY) < Math.Abs(p.Y - MaxY) ? MinY : MaxY;
                }
                else
                {
                    PosCollection segs = value.OrderSegments().OrderBy(ls => _segDistanceTo(ls)).ToCollectionSameClosed();
                    int index = segs.FindIndex(ls => !ls[0].IsParrallel(ls[1], new pPos(0, 0), new pPos(0, 1)));

                    if (index != -1)
                    {
                        p = segs[index].CenterPoint();
                        ny = Math.Abs(p.Y - MinY) < Math.Abs(p.Y - MaxY) ? MinY : MaxY;
                    }
                    else
                    {
                        p = value.CenterPoint();
                    }
                }

                if (!st.empty())
                {
                    if (key._firstPropName() == "#SPLINE")
                        st += "(" + Math.Round(value.Length(false) / 1000, 2).ToString() + "m)";
                    else if (key._firstPropName() == "#BLOCK")
                        st = value.Count().ToString() + "x" + st;
                }

                double offset = DE.DEF_TRACK_VALUE;
                p.Y = p.Y < ct.Y ? value.Boundary()[0].Y - offset : value.Boundary()[1].Y + offset;

                note_contents.Add(new note_struct(st, p, new pPos(MinX, ny)));
            }
        }

        public ObjectIdCollection DrawTextNote()
        {
            ObjectIdCollection res = new ObjectIdCollection();

            if (note_contents.Count > 0)
            {
                note_contents = note_contents.OrderBy(itm => itm.note_point == null
                    ? double.NegativeInfinity : -itm.note_point.X).ToList();

                List<note_struct[]> ar = new List<note_struct[]>
                    {   note_contents.Where(itm => itm.start_point.Y == MinY).Select(itm => itm).ToArray(),
                        note_contents.Where(itm => itm.start_point.Y != MinY).Select(itm => itm).ToArray()  };
                                
                int null_count = note_contents.Count(itm => itm.note_point == null);
                int min_y_count = note_contents.Count(itm => itm.note_point != null && itm.start_point.Y == MinY);
                int max_y_count = note_contents.Count - min_y_count - null_count;
                
                double cur_x = double.PositiveInfinity;

                for (int index = 0; index < 2; index++)
                {
                    double ny = index == 0 ? MinY - min_y_count * TextSize * 10
                                : MaxY + max_y_count * TextSize * 2;

                    for (int i = 0; i < ar[index].Length; i++)
                    {
                        note_struct itm = ar[index][i];
                        double nx = i > ar[index].Length / 2 ? MinX : MaxX;
                        int ax = i > ar[index].Length / 2 ? 1 : -1;
                        
                        ny += (index == 0 ? 1 : -1) * ax * TextSize * 10;

                        if (itm.note_point != null)
                        {
                            if (cur_x - itm.note_point.X < 10)
                                itm.note_point.X = cur_x - 10;

                            res.Add(db.DrawPolyline(new pPos[] { new pPos(nx, ny),
                            new pPos(itm.note_point.X, ny),
                            itm.note_point + (ny < itm.start_point.Y ? -1 : 1)
                            * new pPos(0, TextSize * 2), itm.note_point }, false,
                                "LAYER=" + DE.DEF_LAYER_TEXT, new double[] { 0, 0, TextSize, 0 }));

                            cur_x = itm.note_point.X;
                        }

                        res.Add(db.CreateText((ax == 1 ? "#L" : "#R") + itm.contents,
                            new pPos(nx, ny + 10), 1.8, 0, "LAYER=" + DE.DEF_LAYER_TEXT));
                    }
                }
            }
            return res;
        }
    }

    public class StrecthInfo
    {
        public string Key;
        public double CurrentValue, NewValue;
        //public List<pPos> StrecthPoints, DimPoints;
        public int Scale;
        public pPos Direction;
        pPos[] Rect;
        //pPos[] forward, back;
        public pPos[] Region;

        public StrecthInfo(string _key, double _curval, pPos[] _r)
        {
            Key = _key;
            CurrentValue = _curval;
            Scale = 1;
            //NewValue = _newval;
            //StrecthPoints = new List<pPos>();
            //DimPoints = new List<pPos>();
            Rect = _r;
        }
        
        //public pPos[] ForwardRegion
        //{
        //    get
        //    {
        //        //return StrecthPoints.Count > 2 ? 
        //        //    _getRegion(StrecthPoints.Last(), StrecthPoints[StrecthPoints.Count - 2])
        //        //    : _getRegion(StrecthPoints[0], StrecthPoints[1]);
        //        return forward;
        //    }
        //    set
        //    {
        //        forward = value;
        //    }
        //}

        //public pPos[] BackRegion
        //{
        //    get
        //    {
        //        //return StrecthPoints.Count > 2 ? _getRegion(StrecthPoints[0], StrecthPoints[1])
        //        //    : null;

        //        return back;
        //    }
        //    set
        //    {
        //        back = value;
        //    }
        //}
        
        public void DrawRegion(Database db)
        {
            //db.WR("Letter {0} StrecthPoints {1} Val {2} Forward {3} Backward {4}",
            //    Key, StrecthPoints.Count, CurrentValue, ForwardRegion, BackRegion);

            pPos[] reg = Region;

            if (reg != null)
                db.DrawPolyline(reg, true, String.Format("LAYER=DEFPOINTS|HYPERLINK={0},DIR {1}",Key,Direction));
            //else
            //    db.WR("No forward rect!");

            //reg = BackRegion;
            //if (reg != null)
            //    db.DrawPolyline(reg, true, String.Format("LAYER=DEFPOINTS|HYPERLINK={0}",Key));
            //else
            //    db.WR("No back rect!");
        }
    }

    public class StrecthDwgCLS
    {
        List<StrecthInfo> lsStrecth;
        Database db;
        public ObjectIdCollection ids;//, dimIds;//, rangedimIds;
        public bool IsValid;

        public string[] Params;
        pPos[] r;
        string letter = "XYH";

        public StrecthDwgCLS(Database _db, ObjectIdCollection _ids, pPos[] _r)//, string _prop)
        {
            db = _db;
            ids = _ids.ToList().Select(id => id).ToCollection();
            r = _r.Offset(1000);
            Params = new string[0];

            lsStrecth = new List<StrecthInfo>();
            //db.WR("DIMS {0} in IDS {1}", dimIds.Count, ids.Count);

            for(int i = 0; i < letter.Length; i++)
            {
                string key = letter[i].ToString();
                ObjectIdCollection dimIds = _filterDimResize(db, ids, key);
                    //.OrderBy(id => db._getVertices(id).CenterPoint()[i]).ToCollection();

                foreach (ObjectId dimId in dimIds)
                {
                    /// dimIds.ToList().Sum(id => db._getLength(id));
                    pPos[] pts = db._getVertices(dimId);

                    StrecthInfo itm = new StrecthInfo(key, pts.Length(false), r);
                    //itm.StrecthPoints = _getStrecthPoints(dimIds);

                    itm.Scale = 1;
                                        
                    itm.Direction = (pts[1] - pts[0]).Normalize.Abs;
                    itm.Region = _getRegion(pts, itm.Direction);

                    //if (dimIds.Count > 1)
                    //    itm.BackRegion = _getRegion(db._getVertices(dimIds.First()), itm.Direction);

                    //itm.DrawRegion(db);
                    lsStrecth.Add(itm);
                }
            }
        }

        pPos[] _getRegion(pPos[] pts, pPos dir)
        {
            pts = pts.OrderBy(p => p.X.roundNumber(100)).ThenBy(p => p.Y).ToArray();

            List<pPos> res = new List<pPos>();
            pPos d = new pPos(-dir.Y, dir.X);

            res.AddRange(r.Intersect(pts[0], pts[0] + d, false)
                .OrderBy(p => p.X).ThenBy(p => p.Y));
            res.AddRange(r.Intersect(pts[1], pts[1] + d, false)
                .OrderBy(p => p.X).ThenBy(p => p.Y).Reverse());

            if (res.Any(p => p == null))
                res = null;
            return res == null ? null : res.Rect();
        }
        
        public void DrawStrecthRegion()
        {
            foreach (StrecthInfo itm in lsStrecth)
                itm.DrawRegion(db);
        }
        
        public static ObjectIdCollection _filterDimResize(Database db, ObjectIdCollection ids, string capchar_letter_upper)
        {
            return db.FilterIds(ids, "DIMENSION").Cast<ObjectId>()
                .Where(id => db.ValidId(id) && db._getContent(id).Upper().EndsWith(capchar_letter_upper))
                .Select(id => id).ToCollection();
        }
        
        public void Strecth(string key, double newval)
        {
            StrecthInfo[] ls = lsStrecth.Where(itm => itm.Key.Upper() == key.Upper()).Select(itm => itm).ToArray();
            //db.WR("Strecth_Index {0}", index);

            foreach(StrecthInfo itm in ls)
                db.Stretchs(ids, itm.Region, itm.Scale * itm.Direction * (newval - itm.CurrentValue));

            ObjectIdCollection dimIds = _filterDimResize(db, ids, key.ToString());

            if (dimIds.Count > 0)
            {
                foreach (ObjectId id in dimIds)
                    if (ids.Contains(id))
                        ids.Remove(id);
                db.EraseObjects(dimIds);
            }
        }
    }

    public static class ME
    {
        public static double _getElevation(this Database db, ObjectId objId)
        {
            double res = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity obj = (Entity)tr.GetObject(objId, OpenMode.ForWrite);
                res = obj.GeometricExtents.MinPoint.Z;

                tr.Commit();
            }

            return res;
        }

        public static void _setHatchElevation(this Database db, ObjectId hId, double z)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Hatch hat = (Hatch)tr.GetObject(hId, OpenMode.ForWrite);
                hat.Elevation = z;

                //hat.EvaluateHatch(true);
                hat.UpgradeOpen();

                tr.Commit();
            }
        }

        public static void StretchHatch(this Database db, ObjectId hId, pPos[] region, pPos mv)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Hatch hat = (Hatch)tr.GetObject(hId, OpenMode.ForWrite);
                HatchLoop[] ls = DE.NumericArray(0, hat.NumberOfLoops - 1).Select(i => hat.GetLoopAt(i)).ToArray();
                

                while (hat.NumberOfLoops > 0)
                    hat.RemoveLoopAt(0);

                foreach (var loop in ls)
                {
                    if (loop.IsPolyline)
                        for (int j = 0; j < loop.Polyline.Count; j++)
                        {
                            pPos pt = loop.Polyline[j].Vertex.ToPos2d();
                            if (pt.Inside(region))
                                loop.Polyline[j].Vertex = (pt + mv).ToPoint2();
                        }

                    hat.AppendLoop(loop);
                }

                hat.EvaluateHatch(true);
                hat.UpgradeOpen();

                tr.Commit();
            }
        }

        static void _moveHatch(this Database db, ObjectId hId, int[] indexes, pPos mv)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Hatch hat = (Hatch)tr.GetObject(hId, OpenMode.ForWrite);
                HatchLoop[] ls = DE.NumericArray(0, hat.NumberOfLoops - 1).Select(i => hat.GetLoopAt(i)).ToArray();
                
                while (hat.NumberOfLoops > 0)
                    hat.RemoveLoopAt(0);

                foreach (int i in indexes)
                {
                    //db.WR("Hatch loop Value {2}: {0} vert {1}", i / 100, i % 100, i);
                    var loop = ls[i/100];
                    int index = i % 100;

                    if (loop.IsPolyline)
                        loop.Polyline[index].Vertex = (loop.Polyline[index].Vertex.ToPos2d() + mv).ToPoint2();
                    else
                    {
                        var cv = loop.Curves[0];

                        if (cv is LineSegment2d)
                        {
                            int curve_index = index / 2;
                            int curve_vertex_index = index % 2;

                            //db.WR("------------------Loop {0} curve {1} vertex {2}", i / 100, curve_index, curve_vertex_index);

                            if(curve_vertex_index == 0)
                                loop.Curves[curve_index] = new LineSegment2d((loop.Curves[curve_index].StartPoint.ToPos2d() 
                                    + mv).ToPoint2(), loop.Curves[curve_index].EndPoint);
                            else if (curve_vertex_index == 1)
                                loop.Curves[curve_index] = new LineSegment2d(loop.Curves[curve_index].StartPoint,
                                    (loop.Curves[curve_index].EndPoint.ToPos2d() + mv).ToPoint2());
                        }
                        else if (cv is CircularArc2d)
                        {
                            CircularArc2d arc2d = cv as CircularArc2d;
                            arc2d.Center = (arc2d.Center.ToPos2d() + mv).ToPoint2();
                        }
                        else if (cv is EllipticalArc2d)
                        {
                            EllipticalArc2d ellipse2d = cv as EllipticalArc2d;
                            ellipse2d.Center = (ellipse2d.Center.ToPos2d() + mv).ToPoint2();
                        }
                        else if (cv is NurbCurve2d)
                        {
                            NurbCurve2d spline2d = cv as NurbCurve2d;

                            if (spline2d.HasFitData)
                            {
                                NurbCurve2dFitData n2fd = spline2d.FitData;
                                n2fd.FitPoints[index] = (n2fd.FitPoints[index].ToPos2d() + mv).ToPoint2();
                            }
                            else
                            {
                                NurbCurve2dData n2fd = spline2d.DefinitionData;
                                n2fd.ControlPoints[index] = (n2fd.ControlPoints[index].ToPos2d() + mv).ToPoint2();
                            }
                        }
                    }
                }
            
                foreach(var loop in ls)
                    hat.AppendLoop(loop);

                hat.EvaluateHatch(true);
                hat.UpgradeOpen();

                tr.Commit();
            }
        }

        public static void Stretchs(this Database db, ObjectIdCollection ids, pPos[] region, pPos mv)
        {
            for (int i = 0; i < ids.Count; i++)
                db.Stretch(ids[i], region, mv);
        }

        public static void Move(this Database db, ObjectId id, int[] indexes, pPos mv)
        {
            if (indexes.Length > 0)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    pPos[] bb;
                    bb = db._getBound(id);
                    if (bb != null)
                    {
                        if (indexes.Length == 1 && indexes.First() == -1)
                        {
                            db.MoveObject(id, mv);
                        }
                        else
                        {
                            switch (id.ObjectClass.DxfName)
                            {
                                case "MULTILEADER":
                                    MLeader lead = (MLeader)tr.GetObject(id, OpenMode.ForWrite);

                                    foreach (int i in indexes)
                                        lead.SetVertex(i / 100, i % 100, (lead.GetVertex(i / 100, i % 100).ToPos() + mv).ToPoint3());
                                    
                                    break;
                                case "LWPOLYLINE":
                                    Polyline lwp = (Polyline)tr.GetObject(id, OpenMode.ForWrite);

                                    foreach (int i in indexes)
                                        lwp.SetPointAt(i, (lwp.GetPoint2dAt(i).ToPos2d() + mv).ToPoint2());

                                    break;
                                case "LINE":
                                    Line line = (Line)tr.GetObject(id, OpenMode.ForWrite);

                                    if (indexes.Contains(0))
                                        line.StartPoint = (line.StartPoint.ToPos() + mv).ToPoint3();
                                    if (indexes.Contains(1))
                                        line.EndPoint = (line.EndPoint.ToPos() + mv).ToPoint3();
                                    break;
                                case "DIMENSION":
                                    foreach (int i in indexes)
                                        db._setPoint(id, db._getPoint(id,i) + mv, i);
                                    
                                    break;
                                case "AEC_WALL":
                                    pPos p1 = db._getPoint(id, 0);
                                    if (indexes.Contains(0))
                                        p1 += mv;

                                    pPos p2 = db._getPoint(id, 1);
                                    if (indexes.Contains(1))
                                            p2 += mv;

                                    db._setWallPoint(id, p1, p2);

                                    break;
                                case "HATCH":
                                    //db.WR("Indexes {0}", indexes.ToText());
                                    db._moveHatch(id, indexes, mv);

                                    break;
                            }
                        }
                    }

                    tr.Commit();
                }
            }
        }

        public static void Stretch(this Database db, ObjectId id, pPos[] region, pPos mv)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                pPos[] bb;
                bb = db._getBound(id);
                if (bb != null)
                {
                    if (db._isPoint(id))
                    {
                        pPos pt = db._getPoint(id);
                        //db.WR("PT {0}", pt);
                        //db.WRArray("REGION", region);

                        if (pt != null && pt.Inside(region))
                            db.MoveObject(id, mv);
                            
                    } else
                    {
                        if (bb.Inside(region))
                            db.MoveObject(id, mv);
                        else
                            switch (id.ObjectClass.DxfName)
                            {
                                case "MULTILEADER":
                                    MLeader lead = (MLeader)tr.GetObject(id, OpenMode.ForWrite);

                                    for(int i = 0; i < lead.LeaderLineCount; i++)
                                        for (int j = 0; j < lead.VerticesCount(i); j++)
                                        {
                                            pPos pt = lead.GetVertex(i, j).ToPos();
                                            if (pt.Inside(region))
                                                lead.SetVertex(i, j, (pt + mv).ToPoint3());
                                        }
                                    
                                    break;
                                case "LWPOLYLINE":
                                    Polyline lwp = (Polyline)tr.GetObject(id, OpenMode.ForWrite);
                                    
                                    for (int i = 0; i < lwp.NumberOfVertices; i++)
                                    {
                                        pPos pt = lwp.GetPoint2dAt(i).ToPos2d();
                                        if (pt.Inside(region))
                                            lwp.SetPointAt(i, (pt + mv).ToPoint2());
                                    }
                                    break;
                                case "LINE":
                                    Line line = (Line)tr.GetObject(id, OpenMode.ForWrite);

                                    if (line.StartPoint.ToPos().Inside(region))
                                        line.StartPoint = (line.StartPoint.ToPos() + mv).ToPoint3();
                                    if (line.EndPoint.ToPos().Inside(region))
                                        line.EndPoint = (line.EndPoint.ToPos() + mv).ToPoint3();
                                    break;
                                case "DIMENSION":
                                    //Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);

                                    //if (ent is RotatedDimension)
                                    //{
                                        pPos p = db._getPoint(id, 0);
                                        if(p.Inside(region))
                                            db._setPoint(id, p + mv, 0);

                                        p = db._getPoint(id, 1);
                                        if (p.Inside(region))
                                            db._setPoint(id, p + mv, 1);
                                    //}
                                    break;

                                case "AEC_WALL":
                                    pPos p1 = db._getPoint(id, 0);

                                    if(p1.Inside(region))
                                        p1 += mv;

                                    pPos p2 = db._getPoint(id, 1);
                                    if (p2.Inside(region))
                                        p2 += mv;

                                    db._setWallPoint(id, p1, p2);

                                    break;
                                case "HATCH":
                                    db.StretchHatch(id, region, mv);

                                    break;
                            }
                        }
                    }
                
                tr.Commit();
            }
        }
        
        public static Database current_assocarray_database;
        public static ObjectId current_assocarray_objectId;
        public static pPos[] current_assocarray_bound;
        public static int current_assocarray_numX, current_assocarray_numY;
        
        public static void _setObjectVisible(this Database db, ObjectIdCollection ids, bool visible)
        {
            foreach (ObjectId id in ids)
                db._setObjectVisible(id, visible);
        }

        public static void _setObjectVisible(this Database db, ObjectId id, bool visible)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                ent.Visible = visible;
                tr.Commit();
            }
        }

        public static ObjectId CreateAssocArray(this Database db, ObjectId sourceId, pPos[] bound)
        {
            if (current_assocarray_database == null)
                current_assocarray_database = db;
            //db.WR("AR01");
            current_assocarray_bound = null;
            ObjectId res = ObjectId.Null;
            AssocArrayRectangularParameters par = null;
            ObjectIdCollection entIds = null;
            //db.WR("AR02 {0}", current_assocarray_database);

            AssocArray ar = AssocArray.GetAssociativeArray(sourceId);
            if (ar != null)
            {
                //db.WR("AR03, is same database {0}", current_assocarray_database == db);
                entIds = current_assocarray_database != db ? current_assocarray_database.CloneObjects(ar.SourceEntities, db) : ar.SourceEntities;
                par = (AssocArrayRectangularParameters)ar.GetParameters();
                //db.WR("AR03.1");
                if (par != null)
                {
                    //db.WR("AR03.2");
                    //db.WR("Number of items {0},{1},{2},{3} X = {4}", par.RowCount, 
                        //par.RowSpacing, par.ColumnCount, par.ColumnSpacing, bound[1].X - bound[0].X);
                    par.ColumnCount = current_assocarray_numX = (int)Math.Ceiling((bound[1].X - bound[0].X) / par.ColumnSpacing);
                    par.RowCount = current_assocarray_numY = (int)Math.Ceiling((bound[1].Y - bound[0].Y) / par.RowSpacing);
                    //db.WR("AR04");
                    AssocArray itm = AssocArray.CreateArray(entIds, new VertexRef(new Point3d(0, 0, 0)), par);
                    res = itm.EntityId;

                    //db.WR("New values {0},{1},{2},{3},Id = ({4})", par.RowCount, bound[1].Y - bound[0].Y, par.ColumnCount, bound[1].X - bound[0].X, res);
                    current_assocarray_bound = new pPos[]{bound[0],
                        new pPos( bound[0].X + par.ColumnCount * par.ColumnSpacing, bound[0].Y + par.RowCount * par.RowSpacing )};
                    //db.WR("AR05");
                    //ACD.DB.DrawPolyline(current_assocarray_bound.Rect());

                    current_assocarray_database = db;
                    current_assocarray_objectId = res;
                    //db.WR("AR06");
                    db.MoveObject(res, new pPos(bound[0].X + par.ColumnSpacing / 2, bound[0].Y + par.RowSpacing / 2));
                    db._setLayer(res, DE.DEF_LAYER_HATCH);
                    //db.WR("AR07");
                }
            }
            return res;
        }

        public static void DrawNote(this Database db, IEnumerable<pPos> pts, double radius = 200)
        {
            db.GetEntities(null, EN_SELECT.AC_DXF, "INSERT");
            ObjectIdCollection blockIds = IR.SelectedIds;

            pPos[] bb = pts.Boundary();
            pPos[] outter = bb.Rect().Offset(radius);
            ObjectId lwpId = db.DrawPolyline(outter, true,
                "LAYER=G-Text|LINEWIDTH=20|LTYPE=HIDDEN2|ROUND=" + radius);

            pPos ct = bb.CenterPoint();

            PosCollection ls = blockIds.Cast<ObjectId>()
                .Where(id => db._getBound(id) != null && bb.Inside(db._getBound(id).Rect()))
                .Select(id => db._getBound(id).Rect()).OrderBy(r => r.Area()).ToCollectionSameClosed();

            if (ls.Count == 0)
                ls = pts.ToCollection();

            pPos[] rect = ls.Last.Offset(-2000);
            ct.DistanceToPts(rect);
            pPos prj = pPos.DistanceTo_Projection;

            db.Insert(DE.CAD_TEMPLATE_FILE, DE.DEF_BLOCK_NOTE, new pPos[] { prj });

            pPos[] intrs = outter.Intersect(ct, prj, true);

            if (intrs.Length > 0)
                db._setLayer(db.CreateLeader(prj.Along(450, intrs.First()), intrs.First()), DE.DEF_LAYER_TEXT);
        }

        public static void MoveVertex(this Database db, ObjectId id, int index, pPos mv)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                switch (id.ObjectClass.DxfName)
                {
                    case "LWPOLYLINE":
                        Polyline lwp = (Polyline)tr.GetObject(id, OpenMode.ForWrite);
                        pPos pt = lwp.GetPoint2dAt(index).ToPos2d() + mv;
                        lwp.SetPointAt(index, pt.ToPoint2());

                        break;
                    case "LINE":
                        Line line = (Line)tr.GetObject(id, OpenMode.ForWrite);
                        if (index == 0)
                            line.StartPoint = (line.StartPoint.ToPos() + mv).ToPoint3();
                        else
                            line.EndPoint = (line.EndPoint.ToPos() + mv).ToPoint3();
                        break;
                    case "DIMENSION":
                        RotatedDimension dim = (RotatedDimension)tr.GetObject(id, OpenMode.ForWrite);
                        if (dim != null)
                        {
                            pPos _moveDim = new pPos(0, 0, 0);
                            if (dim.Rotation == 0)
                            {
                                _moveDim.Y = mv.Y;
                                mv.Y = 0;
                            }
                            else
                            {
                                _moveDim.X = mv.X;
                                mv.X = 0;
                            }
                            //db.WR("Id = {0}, Value = {3}, mv = {1}, _moveDim = {2}",id, mv, _moveDim, dim.Measurement);

                            if (!mv.IsZero)
                            {
                                ME.MoveObject(db, id, _moveDim);

                                if (index == 0)
                                    dim.XLine1Point = (dim.XLine1Point.ToPos() + mv).ToPoint3();
                                else
                                    dim.XLine2Point = (dim.XLine2Point.ToPos() + mv).ToPoint3();
                            }
                        }
                        break;
                }

                tr.Commit();
            }
        }

        public static void _setDefaultDimTextPosition(this Database db, ObjectId id)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Dimension dim = (Dimension)tr.GetObject(id, OpenMode.ForWrite);
                dim.UsingDefaultTextPosition = true;

                pPos[] bb = db._getBound(id);

                if (dim.HorizontalRotation != 0)
                    dim.TextPosition = new Point3d(dim.TextPosition.X, (bb[0] + bb[1]).Y / 2, 0);
                else
                    dim.TextPosition = new Point3d((bb[0] + bb[1]).X / 2, dim.TextPosition.Y, 0);

                tr.Commit();
            }
        }

        // Evaluates if the points are clockwise.
        private static bool Clockwise(Point2d p1, Point2d p2, Point2d p3)
        {
            return ((p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X)) < 1e-8;
        }

        // Adds an arc (fillet) at each vertex, if able.
        public static void Fillet(this Database db, ObjectId id, double radius)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Polyline pline = (Polyline)tr.GetObject(id, OpenMode.ForWrite);
                int i = pline.Closed ? 0 : 1;
                for (int j = 0; j < pline.NumberOfVertices - i; j += 1 + db.FilletByIndex(id, j, radius))
                { }
                tr.Commit();
            }
        }

        // Adds an arc (fillet) at the specified vertex. Returns 1 if the operation succeeded, 0 if it failed.
        public static int FilletByIndex(this Database db, ObjectId id, int index, double radius)
        {
            int res = -1;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Polyline pline = (Polyline)tr.GetObject(id, OpenMode.ForWrite);
                int prev = index == 0 && pline.Closed ? pline.NumberOfVertices - 1 : index - 1;
                if (pline.GetSegmentType(prev) != SegmentType.Line ||
                    pline.GetSegmentType(index) != SegmentType.Line)
                    res = 0;
                else
                {
                    LineSegment2d seg1 = pline.GetLineSegment2dAt(prev);
                    LineSegment2d seg2 = pline.GetLineSegment2dAt(index);
                    Vector2d vec1 = seg1.StartPoint - seg1.EndPoint;
                    Vector2d vec2 = seg2.EndPoint - seg2.StartPoint;
                    double angle = (Math.PI - vec1.GetAngleTo(vec2)) / 2.0;
                    double dist = radius * Math.Tan(angle);
                    if (dist > seg1.Length || dist > seg2.Length)
                        res = 0;
                    else
                    {
                        Point2d pt1 = seg1.EndPoint + vec1.GetNormal() * dist;
                        Point2d pt2 = seg2.StartPoint + vec2.GetNormal() * dist;
                        double bulge = Math.Tan(angle / 2.0);
                        if (Clockwise(seg1.StartPoint, seg1.EndPoint, seg2.EndPoint))
                            bulge = -bulge;
                        pline.AddVertexAt(index, pt1, bulge, 0.0, 0.0);
                        pline.SetPointAt(index + 1, pt2);
                        res = 1;
                    }
                }
                tr.Commit();
            }

            return res;
        }

        public static ObjectIdCollection DrawOpeningCross(this Database db, pPos[] bb)
        {
            ObjectIdCollection ids = new ObjectIdCollection();
            pPos[] r = bb.Rect();
            pPos[] ls = new pPos[] { r[0], r[2] };
            ids.Add(db.DrawPolyline(ls));
            ls = new pPos[] { r[1], r[3] };
            ids.Add(db.DrawPolyline(ls));

            foreach (ObjectId id in ids)
                db._setLayer(id, DE.DEF_LAYER_HIDDEN);

            return ids;
        }

        public static ObjectIdCollection DrawCross(this Database db, pPos pt, double size)
        {
            ObjectIdCollection ids = new ObjectIdCollection();
            pPos[] ls = new pPos[] { new pPos(pt.X - size, pt.Y), new pPos(pt.X + size, pt.Y) };
            ids.Add(db.DrawPolyline(ls));
            ls = new pPos[] { new pPos(pt.X, pt.Y - size), new pPos(pt.X, pt.Y + size) };
            ids.Add(db.DrawPolyline(ls));

            foreach (ObjectId id in ids)
                db._setLayer(id, DE.DEF_LAYER_HIDDEN);

            return ids;
        }
        
        public static Vector3d MOVE_TITLE_VECTOR = new Vector3d(0, -300, 0);

        public static void SaveDatabase(this Database db, string dwgname)
        {
            string tmpname = System.IO.Path.GetTempFileName();

            db.SaveAs(tmpname, DwgVersion.Current);
            db.Dispose();

            System.IO.File.Delete(dwgname);
            System.IO.File.Move(tmpname, dwgname);
        }

        public static void CreateComplexLinetype(this Database db, string prams,
            string dashstring = "----- ", double pattern_length = 0.9, 
            int num_dashes = 3,  double dash_length_1 = 0.5, 
            double dash_length_2 = -0.2, double dash_offset_x = -0.1, 
            double dash_offset_y = -0.05)
        {
            string linetypename = prams._prop("Name");
            string linetypestring = prams._prop("Value");

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LinetypeTable acLinTbl = tr.GetObject(db.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;
                //if (!acLinTbl.Has(_lineType))  db.LoadLineTypeFile(_lineType, "acadiso.lin");

                if (!acLinTbl.Has(linetypename))
                {
                    TextStyleTable tt = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                    LinetypeTable lt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForWrite);
                    LinetypeTableRecord ltr = new LinetypeTableRecord();

                    ltr.Name = linetypename; // "COLD_WATER_SUPPLY";
                    ltr.AsciiDescription = linetypename + dashstring + linetypestring + dashstring
                        + linetypestring + dashstring + linetypestring + dashstring + linetypestring;
                    //"Cold water supply ---- CW ---- CW ---- CW ----";

                    ltr.PatternLength = pattern_length;
                    ltr.NumDashes = num_dashes;

                    // Dash #1
                    ltr.SetDashLengthAt(0, dash_length_1);


                    // Dash #2

                    ltr.SetDashLengthAt(1, dash_length_2);

                    ltr.SetShapeStyleAt(1, tt["Standard"]);

                    ltr.SetShapeNumberAt(1, 0);

                    ltr.SetShapeOffsetAt(1, new Vector2d(dash_offset_x, dash_offset_y));

                    ltr.SetShapeScaleAt(1, 0.1);

                    ltr.SetShapeIsUcsOrientedAt(1, false);

                    ltr.SetShapeRotationAt(1, 0);

                    ltr.SetTextAt(1, linetypestring);


                    // Dash #3


                    ltr.SetDashLengthAt(2, -0.2);


                    // Add the new linetype to the linetype table


                    ObjectId ltId = lt.Add(ltr);

                    tr.AddNewlyCreatedDBObject(ltr, true);


                    // Create a test line with this linetype
                    //BlockTable bt =(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead);
                    //BlockTableRecord btr =(BlockTableRecord)tr.GetObject( bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    //Line ln =new Line(new Point3d(0, 0, 0), new Point3d(10, 10, 0) );
                    //ln.SetDatabaseDefaults(db);
                    //ln.LinetypeId = ltId;

                    //btr.AppendEntity(ln);
                    //tr.AddNewlyCreatedDBObject(ln, true);
                }

                tr.Commit();
            }
        }

        //public static void _setHyperLink(this Database db, ObjectId entId, string sHyperlink, bool append = false)
        //{
        //    HyperLink h = new HyperLink();
        //    string st = db._getHyperlink(entId);

        //    h.Name = append ?  st + sHyperlink : sHyperlink;

        //    using (Transaction tr = db.TransactionManager.StartTransaction())
        //    {
        //        Entity ent = (Entity)tr.GetObject(entId, OpenMode.ForWrite);

        //        while (ent.Hyperlinks != null && ent.Hyperlinks.Count > 0)
        //            ent.Hyperlinks.RemoveAt(0);
                                
        //        ent.Hyperlinks.Add(h);

        //        tr.Commit();
        //    }
        //    h.Dispose();
        //}

        //public static void _setHyperLinks(this Database db, ObjectIdCollection ids, string sHyperlink, bool append = false)
        //{
        //    ProgressMeter progress = new ProgressMeter();
        //    progress.Start("Set hyperlink");
        //    progress.SetLimit(ids.Count);
            
        //    foreach (ObjectId objId in ids)
        //    {
        //        db._setHyperLink(objId, sHyperlink, append);
        //        progress.MeterProgress();
        //    }
        //    progress.Stop();
        //}

        public static AnnotationScale AddAnnotativeScale(this Database db, string scaleName)
        {
            AnnotationScale asc = null;

            double unit1 = 1, unit2 = 10;
            string[] ar = scaleName.filter(":");

            if (ar.Length == 1) return asc;

            unit1 = Convert.ToDouble(ar[0]);
            unit2 = Convert.ToDouble(ar[1]);

            try
            {
                ObjectContextManager cm = db.ObjectContextManager;
                if (cm != null)
                {
                    ObjectContextCollection occ = cm.GetContextCollection("ACDB_ANNOTATIONSCALES");
                    if (occ != null)
                    {
                        ObjectContext itm = occ.GetContext(scaleName);
                        asc = new AnnotationScale();

                        if (itm == null)
                        {
                            asc.Name = scaleName;
                            asc.PaperUnits = unit1;
                            asc.DrawingUnits = unit2;
                            occ.AddContext(asc);
                        }
                        else
                            asc = (AnnotationScale)itm;

                    }
                }
            }
            catch (System.Exception ex)
            {
                //db.WR(ex.ToString());
            }

            return asc;
        }

        public static double AddAnnotative(this Database db, Entity ent, string scaleName)
        {
            double unit1 = 1, unit2 = 10;
            string[] ar = scaleName.filter(":");

            if (ar.Length == 1) return -1;

            unit1 = Convert.ToDouble(ar[0]);
            unit2 = Convert.ToDouble(ar[1]);

            try
            {
                ObjectContextManager cm = db.ObjectContextManager;
                if (cm != null)
                {
                    ObjectContextCollection occ = cm.GetContextCollection("ACDB_ANNOTATIONSCALES");
                    if (occ != null)
                    {
                        ObjectContext itm = occ.GetContext(scaleName);
                        AnnotationScale asc = new AnnotationScale();

                        if (itm == null)
                        {
                            asc.Name = scaleName;
                            asc.PaperUnits = unit1;
                            asc.DrawingUnits = unit2;
                            occ.AddContext(asc);
                        }
                        else
                            asc = (AnnotationScale)itm;

                        if (ent != null)
                        {
                            ObjectContexts.AddContext(ent, asc);
                            ent.Annotative = AnnotativeStates.True;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                //db.WR(ex.ToString());
            }

            return unit1 / unit2;
        }
        
        public static ObjectIdCollection DrawGrid(this Database db, IEnumerable<pPos[]> ceils)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            ObjectIdCollection ceils_gridIds = ceils.Cast<pPos[]>()
                .Select(ls => db.DrawPolyline(ls, ls.Length > 2)).ToCollection();

            if (ceils_gridIds.Count > 0)
            {
                string new_name = db.uniqueBlockName("grid_" + ceils_gridIds[0].Handle.Value);
                db._setLayer(ceils_gridIds, DE.DEF_LAYER_HIDDEN);
                pPos pt = db._getBound(ceils_gridIds).Boundary()[0];
                ObjectIdCollection blockIds = db.NewBlock(ceils_gridIds, new_name, true, false, pt);
                db.Insert(new_name, pt);
            }
            return res;
        }
        
        public static bool CloneObjectsByBlockName(this Database db, string blockName, Database destDb)
        {
            bool res = false;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);

                string tmp_block_name = null;
                if (bt.Has(blockName))
                {
                    if (!destDb.HasBlock(blockName).IsNull)
                        tmp_block_name = destDb.RenameBlock(blockName);

                    ObjectIdCollection ids = new ObjectIdCollection { bt[blockName] };

                    var mapping = new IdMapping();
                    db.WblockCloneObjects(ids, destDb.BlockTableId, mapping, DuplicateRecordCloning.Replace, false);
                    res = true;

                    if (!tmp_block_name.empty())
                        destDb.ReplaceBlock(tmp_block_name, blockName);
                }
                else
                    //db.WR("Clone block dont have {0}", blockName);
                tr.Commit();
            }
            return res;
        }

        public static void _setCurrentStyles(this Database db, ObjectIdCollection ids)
        {
            foreach (ObjectId id in ids)
                db._setCurrentStyle(id);
        }
        
        public static void _setCurrentStyle(this Database db, ObjectId id)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                TextStyleTableRecord ttr = (TextStyleTableRecord)db.Textstyle.GetObject(OpenMode.ForRead);
                if (id.ObjectClass.DxfName == "MTEXT")
                {
                    MText mtxt = (MText)tr.GetObject(id, OpenMode.ForWrite);
                    mtxt.TextStyleId = db.Textstyle;

                    if(db._getLayer(id).Upper() != DE.DEFPOINTS.Upper())
                        db._setLayer(id, DE.DEF_LAYER_TEXT);
                }
                else if (id.ObjectClass.DxfName == "TEXT")
                {
                    DBText mtxt = (DBText)tr.GetObject(id, OpenMode.ForWrite);
                    mtxt.TextStyleId = db.Textstyle;
                    if (db._getLayer(id).Upper() != DE.DEFPOINTS.Upper())
                        db._setLayer(id, DE.DEF_LAYER_TEXT);
                }
                else if (id.ObjectClass.DxfName == "DIMENSION")
                {
                    Dimension dim = (Dimension)tr.GetObject(id, OpenMode.ForWrite);
                    dim.DimensionStyle = db.Dimstyle;
                    db._setLayer(id, pString.INI_String("DIM_LAYER"));
                }
                else if (id.ObjectClass.DxfName == "ACAD_TABLE")
                {
                    Table tab = (Table)tr.GetObject(id, OpenMode.ForWrite);
                    tab.TableStyle = db.Tablestyle;
                    db._setLayer(id, DE.DEF_LAYER_TEXT);
                }
                else if (db._isBlock(id))
                {
                    if (db._isGObject(id))
                        db._setLayer(id, DE.DEF_LAYER_TEXT);

                    double sc = Math.Abs(db._getScale(id).X);
                    BlockReference blockRef = (BlockReference)tr.GetObject(id, OpenMode.ForWrite);
                    foreach (ObjectId attId in blockRef.AttributeCollection)
                    {
                        var attDef = tr.GetObject(attId, OpenMode.ForWrite) as AttributeReference;

                        if (attDef != null)
                        {
                            attDef.TextStyleId = db.Textstyle;
                            attDef.Height = (attDef.Tag.Upper().Contains("SCALE") ? 3 : 4) * sc;
                            attDef.Dispose();
                        }
                        //res = attDef.IsMTextAttribute ? attDef.MTextAttribute.Text : attDef.TextString;
                        
                    }
                }else if(db._isHatch(id))
                {
                    db._setLayer(id, DE.DEF_LAYER_HATCH);
                }
                tr.Commit();
            }
        }

        public static ObjectIdCollection DrawLayout(this Database db, pPos[] pts, string title = null)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            //db.WR("PTS {0},{1}", pts[0], pts[1]);
            res.Add(db.DrawPolyline(pts.RectToLayout(), false, "LAYER=DEFPOINTS|LINEWIDTH=10"));

            if (!title.empty())
                res.Add(db.CreateText(title, pts.Boundary()[0] + new pPos(200, 200), 100,0, "LAYER=" + DE.DEFPOINTS));
            return res;
        }

        public static ObjectIdCollection CloneObjectsFromFile(this Database db, string fname, params string[] excludes)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            using (Database new_db = ACD.ReadDWG(fname))
            {
                new_db.GetEntities(null, EN_SELECT.AC_ALL);

                ObjectIdCollection ids = IR.SelectedIds.Cast<ObjectId>()
                    .Where(id => !new_db._isBlock(id) || (!excludes.Any(s => new_db._getIdName(id).StartsWith(s))))
                    .Select(id => id).ToCollection();

                res = new_db.CloneObjects(ids, db);
            }
            return res;
        }


        public static ObjectId IGetWallShape(this Database db, ObjectId entId, double extend = 200)
        {
            ObjectId resId = ObjectId.Null;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Autodesk.Aec.Arch.DatabaseServices.Wall wall
                    = (Autodesk.Aec.Arch.DatabaseServices.Wall)tr.GetObject(entId, OpenMode.ForRead);

                if (wall != null)
                {
                    pPos[] bb = db._getBound(entId);
                    double w = wall.Width;
                    pPos[] pls = null;

                    if (wall.WallGeometryType == Autodesk.Aec.Arch.DatabaseServices.WallType.Linear)
                    {
                        List<pPos> res = new List<pPos>();
                        pPos p1 = wall.StartPoint.ToPos(), p2 = wall.EndPoint.ToPos();
                        pls = new pPos[] { p1, p2 }.ExtentLine(extend);

                        if (wall.JustificationType == Autodesk.Aec.Arch.DatabaseServices.WallJustificationType.Center)
                        {
                            List<pPos> tmps = pls[0].Parallel(pls[1], w / 2).ToList();
                            tmps.AddRange(pls[0].Parallel(pls[1], -w / 2).Reverse());
                            res = tmps.ToList();
                        }
                        else
                        {
                            res = pls.ToList();
                            res.AddRange(pls[0].Parallel(pls[1], w).Reverse());

                            if (wall.JustificationType == Autodesk.Aec.Arch.DatabaseServices.WallJustificationType.Left
                                || wall.JustificationType == Autodesk.Aec.Arch.DatabaseServices.WallJustificationType.Baseline)
                            {
                                res = pls.ToList();
                                res.AddRange(pls[0].Parallel(pls[1], -w).Reverse());
                            }
                        }
                        resId = ACD.DB.DrawPolyline(res, true);
                    }
                    else
                    {
                        pPos p1 = wall.StartPoint.ToPos(), p2 = wall.MidPoint.ToPos(), p3 = wall.EndPoint.ToPos();
                        CircularArc3d carc = new CircularArc3d(wall.StartPoint, wall.MidPoint, wall.EndPoint);
                        pPos ct = carc.Center.ToPos();

                        Polyline lwp = db._arcPoints(p1, p2, p3, ct, 0);

                        if (wall.JustificationType == Autodesk.Aec.Arch.DatabaseServices.WallJustificationType.Center)
                        {
                            Polyline lwp1 = db._arcPoints(p1, p2, p3, ct, w / 2);
                            pPos[] ls1 = lwp1._getLWPVertices();

                            Polyline lwp2 = db._arcPoints(p1, p2, p3, ct, -w / 2);
                            pPos[] ls2 = lwp2._getLWPVertices();

                            resId = 
                                db.DrawPolyline(new pPos[] { ls1.Last(), ls2.First() }, false);
                        }
                        else
                        {
                            Polyline lwp1 = wall.JustificationType == Autodesk.Aec.Arch.DatabaseServices.WallJustificationType.Left
                                || wall.JustificationType == Autodesk.Aec.Arch.DatabaseServices.WallJustificationType.Baseline ?
                                db._arcPoints(p1, p2, p3, ct, -w) : db._arcPoints(p1, p2, p3, ct, w);
                            resId = 
                                db.DrawPolyline(new pPos[] { p3, lwp1._getLWPVertices().First() }, false);
                        }
                    }
                }

                tr.Commit();
            }

            if (!resId.IsNull)
                db._setPolylineClosed(resId, true);

            return resId;
        }


        public static System.Drawing.Bitmap Snapshot(bool save_image = false)
        {
            var acadapp = Autodesk.AutoCAD.ApplicationServices.Application.AcadApplication;
            acadapp.GetType().InvokeMember("ZoomExtents", System.Reflection.BindingFlags.InvokeMethod, null, acadapp, null);

            pPos pMin = ACD.DB.Extmin.ToPos();
            pPos pMax = ACD.DB.Extmax.ToPos();

            pPos sz = pMin.RectToPoint(pMax).Size();
            ObjectId lwpId = ACD.DB.DrawPolyline(pMin.RectToPoint(pMax), true, "LAYER=0");

            double scale = Math.Min(3200 / sz.X, 1362 / sz.X);

            System.Drawing.Bitmap img = (System.Drawing.Bitmap)System.Windows.Forms.ToolStripRenderer.CreateDisabledImage(
                ACD.DOC.CapturePreviewImage((uint)(sz.X * scale), (uint)(sz.X * scale)));//.InvertingImage());

            if (save_image)
                img.Save(System.IO.Path.Combine(DE.CADLIB_SAMPLES, ACD.TempFileName, ".jpg"));

            ACD.DB.EraseObject(lwpId);

            return img;
        }


        public static void ApplyAnno(this Database db, ObjectIdCollection srcIds, string scale_name)
        {
            //using (ACD.Lock())
            {
                try
                {
                    ObjectIdCollection ids = db.FilterIds(srcIds, "MTEXT", "TEXT",
                        "MULTILEADER", "LEADER", "AEC_DIMENSION_GROUP", "DIMENSION", "LEADER");
                    //srIds = IR.SelectedIds;

                    foreach (ObjectId id in ids)
                        if (!(db._isText(id)
                            && db._getContent(id).StartsWith(".")
                            && db._getLayer(id) != "Defpoints"))
                            db._setLayer(ids, "A-Anno-Dims");

                    double sc = scale_name.filter(":").Length > 1 ? scale_name.filter(":").Last().ToNumber() : 0;

                    if (sc > 0)
                    {
                        ObjectContextManager ocm = db.ObjectContextManager;
                        ObjectContextCollection occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");

                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            if (!occ.HasContext(scale_name))
                            {
                                //db.WR("\nCannot find current annotation scale.");
                                return;
                            }

                            ObjectContext curCtxt = occ.GetContext(scale_name);

                            foreach (ObjectId id in ids)
                            {
                                //db.WR("ID {0}", id.ObjectClass.DxfName);
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

                                if (db._isDim(id))
                                    db._setDimValueRound(id, sc < 50 ? 0 : 50);

                                //if (ACD.DB._isLeader(id))
                                //ACD.DB._setMLeadeBottomUnderline(id);
                            }
                            tr.Commit();
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    //db.WR("Error {0},{1}", ex.Message, ex.StackTrace);
                }
            }

            //ACD.Focus();
        }


        public static ObjectId CloneDimensionObject(this Database db, ObjectId srcId, Database destdb)
        {
            ObjectId dimId = IDimChain.CreateDimension(destdb, db._getPoint(srcId, 0),
                            db._getPoint(srcId, 1), db._getPoint(srcId, 2), db._getContent(srcId));

            double sc = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Dimension dimObj = (Dimension)tr.GetObject(srcId, OpenMode.ForRead);
                if(dimObj is RotatedDimension)
                    sc = dimObj.Dimlfac;
                
                tr.Commit();
            }

            using (Transaction tr = destdb.TransactionManager.StartTransaction())
            {
                Dimension dimObj = (Dimension)tr.GetObject(dimId, OpenMode.ForWrite);
                //if (dimObj is RotatedDimension)
                    dimObj.Dimlfac = sc;

                tr.Commit();
            }

            return dimId;
        }

        public static ObjectIdCollection CloneObjects(this Database db, ObjectIdCollection ids, Database destdb = null)
        {
            IdMapping iMap = new IdMapping();
            ObjectId destDbMsId = SymbolUtilityServices.GetBlockModelSpaceId(destdb == null ? db : destdb);
            ObjectIdCollection res = new ObjectIdCollection();
            //db.WR("Model {0} Ids {1}", destDbMsId, ids.Count);

            //foreach (ObjectId id in ids)
            //    if(id.IsErased || id.IsEffectivelyErased)
            //    {
            //        db.WR("Invalid {0}", id.ObjectClass.DxfName);
            //    }

            if (destdb != null)
            {
                db.WblockCloneObjects(ids, destDbMsId, iMap, DuplicateRecordCloning.Ignore, false);
                res = ids.Cast<ObjectId>().Select(id => iMap[id].Value).ToCollection();

                //ObjectIdCollection dimIds = ids.ToList().Where(id => db._isDim(id)).ToCollection();
                //foreach (ObjectId id in dimIds)
                //    if (db._isRotateDim(id))
                //        res.Add(db.CloneDimensionObject(id, destdb));
            }
            else
            {
                //db.WR("Model {0} Ids {1}", destDbMsId, ids);
                db.DeepCloneObjects(ids, destDbMsId, iMap, false);
                res = ids.Cast<ObjectId>().Select(id => iMap[id].Value).ToCollection();
            }

            return res;
        }

        public static ObjectId CloneObject(this Database db, ObjectId id)
        {
            return db.CloneObjects(new ObjectIdCollection { id }).First();
        }

        public static void MoveObject(this Database db, ObjectId id, pPos pt)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (db.ValidId(id))
                {
                    Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                    Point3d acPt3d = new Point3d(0, 0, 0);
                    Vector3d acVec3d = acPt3d.GetVectorTo(pt.ToPoint3());

                    ent.TransformBy(Matrix3d.Displacement(acVec3d));
                }
                tr.Commit();
            }
        }

        public static void MoveObject(this Database db, ObjectIdCollection ids, pPos pt)
        {
            foreach (ObjectId id in ids)
                if (id != null && !id.IsNull && db.ValidId(id))
                    MoveObject(db, id, pt);
        }

        public static void MirrorObject(this Database db, ObjectId id, pPos pt1, pPos pt2)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                ent.TransformBy(pt1.MirrorMat(pt2));

                tr.Commit();
            }
        }

        public static void MirrorObjects(this Database db, ObjectIdCollection ids, pPos pt1, pPos pt2)
        {
            foreach (ObjectId id in ids)
                db.MirrorObject(id, pt1, pt2);
        }

        /*public static void MirrorObject(this Database db, ObjectIdCollection ids, pPos pt1, pPos pt2)
        {
            foreach (ObjectId id in ids) MirrorObject(db, id, pt1, pt2);
        }*/
  
        public static void ReloadXREFs(this Database db)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                    List<ObjectId> visLwp = btr
                               .Cast<ObjectId>()
                               .Where(id => id.ObjectClass.DxfName == "INSERT")
                               .Select(id => id)
                               .ToList();

                    ObjectIdCollection objIds = new ObjectIdCollection();
                    foreach (ObjectId id in visLwp)
                    {
                        BlockReference blockRef = (BlockReference)tr.GetObject(id, OpenMode.ForRead);
                        BlockTableRecord blockDef = (BlockTableRecord)tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead);

                        if (blockDef.IsFromExternalReference)
                            objIds.Add(blockRef.BlockTableRecord);
                    }

                    db.ReloadXrefs(objIds);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    //db.WR("Error in {0},{1}", ex.StackTrace, ex.Message);
                }

                tr.Commit();
            }
        }


        public static bool _getLayerLocked(this Database db, string sLayerName)
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
                    res = acLyrTblRec.IsLocked;

                }
                tr.Commit();
            }
            return res;
        }


        public static void _setLayerLock(this Database db, string sLayerName, bool locked)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable acLyrTbl = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                string layer = sLayerName.filter("|").First();
                
                LayerTableRecord acLyrTblRec;
                if (acLyrTbl.Has(layer))
                {
                    acLyrTblRec = tr.GetObject(acLyrTbl[layer], OpenMode.ForWrite) as LayerTableRecord;

                    // Turn the layer off 
                    acLyrTblRec.IsLocked = locked;

                }
                tr.Commit();
            }
        }

        public static void _setLayerState(this Database db, string sLayerName, bool state)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Open the Layer table for read 
                LayerTable acLyrTbl = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                LayerTableRecord acLyrTblRec;
                if (acLyrTbl.Has(sLayerName))
                {
                    acLyrTblRec = tr.GetObject(acLyrTbl[sLayerName], OpenMode.ForWrite) as LayerTableRecord;

                    // Turn the layer off 
                    acLyrTblRec.IsOff = !state;

                }
                tr.Commit();
            }
        }
        
        public static void DrawSlab(this Database db, IEnumerable<pPos> pls)
        {
            db._setLayer(db.DrawPolyline(pls.ToArray()), DE.DEF_LAYER_HATCH);
            pPos[] offset = pls.Offset(-10);
            ObjectId lwpId = db.DrawPolyline(offset);
            db._setLayer(lwpId, DE.DEF_LAYER_WALL);
            //db._setIdInfo(lwpId, null, 20);

            DE.DEF_HATCH_PATTERN = "AR-CONC";
            DE.DEF_HATCH_SCALE = 0.2;
            DE.DEF_HATCH_ANGLE = 0;
            db.DrawHatch(offset);
        }
        
        public static void TransformByMatrix(this Database db, ObjectId id, Matrix3d mat)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                ent.TransformBy(mat);
                tr.Commit();
            }
        }
        
        public static void Transform(this Database db, ObjectId id, pPos movept, double rot, double scale, pPos basept = null)
        {
            Matrix3d mat = pPosExtension.TransformMat(movept, rot, scale, basept);
            //db.WR("TRANSFORM_SCALE {0}", scale);
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                ent.TransformBy(mat);
                tr.Commit();
            }
        }

        public static void Rotate(this Database db, ObjectIdCollection ids, double rot, pPos basept)
        {
            //Matrix3d mat = pPosExtension.TransformMat(movept, rot, scale, basept);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                    ent.TransformBy(Matrix3d.Rotation(Math.PI * rot / 180, Vector3d.ZAxis, basept.ToPoint3()));
                }
                tr.Commit();
            }
        }

        public static void Transforms(this Database db, ObjectIdCollection ids, pPos movept, double rot, double scale, pPos basept = null)
        {
            Matrix3d mat = pPosExtension.TransformMat(movept, rot, scale, basept);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                    ent.TransformBy(mat);
                }
                tr.Commit();
            }
        }

        public static void TransformsByMat(this Database db, ObjectIdCollection ids, Matrix3d mat)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                if(db.ValidId(id))
                    {
                    Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                    ent.TransformBy(mat);
                }
                tr.Commit();
            }
        }

        public static void _setTextFactor(this Database db, ObjectId id, double value)
        {
            if (id.ObjectClass.DxfName == "TEXT")
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                
                    DBText ent = (DBText)tr.GetObject(id, OpenMode.ForWrite);
                    //LayerTableRecord acLyrTblRec = tr.GetObject(acLyrTbl[sLayerName], OpenMode.ForWrite) as LayerTableRecord;
                    if (ent != null)
                        ent.WidthFactor = value;
                    tr.Commit();
                }
        }


        public static void _setLayer(this Database db, ObjectIdCollection ids, string LayerInfo)
        {
            foreach (ObjectId id in ids)
                db._setLayer(id, LayerInfo);
        }

        public static void _setDimTextPos(this Database db, ObjectId id, pPos pt)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);

                if (ent is RotatedDimension)
                {
                    RotatedDimension dim = (RotatedDimension)ent;
                    dim.TextPosition = pt.ToPoint3();
                }
                else if (ent is RotatedDimension)
                {
                    AlignedDimension dim = (AlignedDimension)ent;
                    dim.TextPosition = pt.ToPoint3();
                }
                else if (ent is RadialDimension)
                {
                    RadialDimension dim = (RadialDimension)ent;
                    dim.TextPosition = pt.ToPoint3();
                }
                if (ent is OrdinateDimension)
                {
                    OrdinateDimension dim = (OrdinateDimension)ent;
                    dim.TextPosition = pt.ToPoint3();
                }

                tr.Commit();
            }
        }
        
        public static void _setContent(this Database db, ObjectId id, string _value, Dictionary<string, string> dict = null)
        {
            if (_value.empty())
                return;
            if (dict == null) dict = new Dictionary<string, string>();
            if (!dict.ContainsKey("<n>")) dict.Add("<n>", "\r\n");

            string value = _value.Replace(dict)._replaceShortkeys();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (id.ObjectClass.DxfName == "TEXT")
                {
                    DBText txt = (DBText)tr.GetObject(id, OpenMode.ForWrite);
                    //txt.Justify = AttachmentPoint.BottomLeft;

                    if (value.st_("#T"))
                    {
                        txt.TextString = value.Substring(2);
                        txt.Justify = AttachmentPoint.TopLeft;
                    }
                    else if (value.st_("#K"))
                    {
                        txt.TextString = value.Substring(2);
                        txt.Justify = AttachmentPoint.TopCenter;
                    }
                    else if (value.st_("#J"))
                    {
                        txt.TextString = value.Substring(2);
                        txt.Justify = AttachmentPoint.TopRight;
                    }
                    else if (value.st_("#M"))
                    {
                        txt.TextString = value.Substring(2);
                        txt.Justify = AttachmentPoint.MiddleCenter;
                    }
                    else if (value.st_("#L"))
                    {
                        txt.TextString = value.Substring(2);
                        txt.Justify = AttachmentPoint.BottomLeft;
                    }
                    else if (value.st_("#C"))
                    {
                        txt.TextString = value.Substring(2);
                        txt.Justify = AttachmentPoint.BottomCenter;
                    }
                    else if (value.st_("#R"))
                    {
                        txt.TextString = value.Substring(2);
                        txt.Justify = AttachmentPoint.BottomRight;
                    }
                    else
                        txt.TextString = value;
                }
                else if (id.ObjectClass.DxfName == "MTEXT")
                {
                    MText txt = (MText)tr.GetObject(id, OpenMode.ForWrite);
                    txt.Attachment = AttachmentPoint.BottomLeft;

                    if (value.st_("#T"))
                    {
                        txt.Contents = value.Substring(2);
                        txt.Attachment = AttachmentPoint.TopLeft;
                    }
                    else if (value.st_("#K"))
                    {
                        txt.Contents = value.Substring(3);
                        txt.Attachment = AttachmentPoint.TopCenter;
                    }
                    else if (value.st_("#J"))
                    {
                        txt.Contents = value.Substring(3);
                        txt.Attachment = AttachmentPoint.TopRight;
                    }
                    else if (value.st_("#M"))
                    {
                        txt.Contents = value.Substring(2);
                        txt.Attachment = AttachmentPoint.MiddleCenter;
                    }
                    else if (value.st_("#L"))
                    {
                        txt.Contents = value.Substring(2);
                        txt.Attachment = AttachmentPoint.BottomLeft;
                    }
                    else if (value.st_("#C"))
                    {
                        txt.Contents = value.Substring(2);
                        txt.Attachment = AttachmentPoint.BottomCenter;
                    }
                    else if (value.st_("#R"))
                    {
                        txt.Contents = value.Substring(2);
                        txt.Attachment = AttachmentPoint.BottomRight;
                    }
                    else
                        txt.Contents = value;
                }
                else if (db._isBlock(id))
                {
                    db.SetBlockAtt(id, "TITLE", value);
                }
                else if (id._isDim())
                {
                    RotatedDimension rotDim = (RotatedDimension)tr.GetObject(id, OpenMode.ForWrite);
                    if (rotDim != null)
                    {
                        rotDim.DimensionText = value;
                    }
                }
                else if (id.ObjectClass.DxfName == "MULTILEADER")
                {
                    //db.WR("MULTI {0}", value);
                    MLeader txt = (MLeader)tr.GetObject(id, OpenMode.ForWrite);
                    txt.MText.SetContentsRtf(value);

                    MText annomtx = new MText();
                    annomtx.Location = txt.MText.Location;
                    double h = txt.MText.TextHeight;

                    annomtx.Contents = value;
                    txt.MText = annomtx;
                    txt.TextHeight = h * db.CurrentAnnotativeScale();

                    //db.WR("Txt_height {0} / {1} = {2}", h, db.CurrentAnnotativeScale(), txt.TextHeight);
                }
                tr.Commit();
            }
        }

        public static void _setTextSize(this Database db, ObjectId id, double size)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (id.ObjectClass.DxfName == "TEXT")
                {
                    DBText txt = (DBText)tr.GetObject(id, OpenMode.ForWrite);
                    txt.Height = size;
                }
                else if (id.ObjectClass.DxfName == "MTEXT")
                {
                    MText txt = (MText)tr.GetObject(id, OpenMode.ForWrite);
                    txt.TextHeight = size;
                }
                else if (db._isBlock(id))
                {
                    //db.SetBlockAtt(id, "TITLE", value);
                }
                else if (id._isDim())
                {
                    RotatedDimension rotDim = (RotatedDimension)tr.GetObject(id, OpenMode.ForWrite);
                    if (rotDim != null)
                    {
                        rotDim.Dimtxt = size;
                    }
                }
                else if (id.ObjectClass.DxfName == "MULTILEADER")
                {
                    MLeader txt = (MLeader)tr.GetObject(id, OpenMode.ForWrite);
                    txt.TextHeight = size;
                }
                tr.Commit();
            }
        }

        public static void _setTextAlignment(this Database db, ObjectId id, AttachmentPoint justify)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (id.ObjectClass.DxfName == "TEXT")
                {
                    DBText txt = (DBText)tr.GetObject(id, OpenMode.ForWrite);
                    txt.Justify = justify;
                }
                else if (id.ObjectClass.DxfName == "MTEXT")
                {
                    MText txt = (MText)tr.GetObject(id, OpenMode.ForWrite);
                    txt.Attachment = justify;
                }
                
                tr.Commit();
            }
        }
        public static void _replaceContent(this Database db, ObjectId id, string key, string new_value)
        {
            string st = db._getContent(id);
            if (st.Contains(key))
                db._setContent(id,st._replaceLineValue(key, new_value));
        }
        
        public static ObjectIdCollection CreateMLeader(this Database db, string txt, pPos destPos, pPos textPos, 
                string LayerInfo = null, string AnnoInfo = null)
        {
            ObjectIdCollection res = new ObjectIdCollection();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace],OpenMode.ForWrite)as BlockTableRecord;
                MLeaderStyle mlStyle = (MLeaderStyle)tr.GetObject(db.MLeaderstyle, OpenMode.ForWrite);
                MLeader leader = new MLeader();

                btr.AppendEntity(leader);
                tr.AddNewlyCreatedDBObject(leader, true);

                double sc = IAnno.AddCurrentAnnotative(db, leader.ObjectId);
                //ObjectId styleId = ACD._getLeaderStyle(db, DE.DEF_MLEADERSTYLE);

                //if (!styleId.IsNull) leader.MLeaderStyle = styleId;
                //leader.SetDatabaseDefaults();
                leader.MLeaderStyle = db.MLeaderstyle;
                leader.ContentType = ContentType.MTextContent;

                MText mText = new MText();
                mText.TextStyleId = mlStyle.TextStyleId;

                //styleId = ACD._getTextStyle(db, DE.DEF_TSTYLE);
                //db.WR("Textstyle {0}", styleIds.Count);
                //if (styleId != ObjectId.Null) mText.TextStyleId = styleId;
                mText.Width = 0;
                //mText.Height = DE.DEF_TEXT_HEIGHT / ACD.DB.CurrentAnnotativeScale();
                mText.Attachment = AttachmentPoint.BottomLeft;
                mText.SetContentsRtf(txt);
                double nx = textPos.X; //+mText.ActualWidth / sc;
                //db.WR("Text width {0}", mText.ActualWidth / sc);
                mText.Location = new Point3d(nx - mText.Width, textPos.Y, 0);

                if (LayerInfo != null) leader.Layer = db.CreateLayer(LayerInfo);
                if (AnnoInfo != null) AddAnnotative(db, leader, AnnoInfo);
 
                leader.MText = mText;
                leader.LandingGap = 0;
                leader.TextHeight = DE.DEF_TEXT_HEIGHT / db.CurrentAnnotativeScale();
                //db.WR("TxtHeight {0}", leader.TextHeight);

                int idx = leader.AddLeaderLine(new Point3d((destPos.X + nx) / 2, textPos.Y - mText.Height, 0));
                leader.AddFirstVertex(idx, destPos.ToPoint3());
                leader.SetVertex(idx, 2, new Point3d(textPos.X, textPos.Y - mText.Height, 0));
                //leader.SetTextAttachmentType(TextAttachmentType.AttachmentBottomLine, LeaderDirectionType.BottomLeader);
                
                res.Add(leader.ObjectId);

                tr.Commit();
            }

            return res;
        }

        public static string _getLeaderInfo(this Database db, ObjectId leaderId)
        {
            string res = "";
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                MLeader leader = (MLeader)tr.GetObject(leaderId, OpenMode.ForWrite);
                TextAttachmentType top_type = leader.GetTextAttachmentType(LeaderDirectionType.TopLeader);
                TextAttachmentType bottom_type = leader.GetTextAttachmentType(LeaderDirectionType.BottomLeader);
                res += "Top:" + top_type.ToString();
                res += "Bottom:" + bottom_type.ToString();
                tr.Commit();
            }
            return res;
        }

        public static void _setLeaderStandard(this Database db, ObjectId leaderId)
        {
            if(leaderId.ObjectClass.DxfName == "MULTILEADER")
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                MLeader leader = (MLeader)tr.GetObject(leaderId, OpenMode.ForWrite);
                leader.SetTextAttachmentType(TextAttachmentType.AttachmentBottomLine, LeaderDirectionType.LeftLeader);
                leader.SetTextAttachmentType(TextAttachmentType.AttachmentBottomLine, LeaderDirectionType.RightLeader);
                db._setLayer(leaderId, DE.DEF_LAYER_TEXT);
                tr.Commit();
            }
        }

        public static void UpdateAllTextFont(this Database db, string fontname = "Tahoma")
        {
            // Start a transaction
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                SymbolTable symTable = (SymbolTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                foreach (ObjectId textstyleId in symTable)
                {
                    TextStyleTableRecord acTextStyleTblRec = tr.GetObject(textstyleId, OpenMode.ForWrite) as TextStyleTableRecord;

                    // Get the current font settings
                    Autodesk.AutoCAD.GraphicsInterface.FontDescriptor acFont = acTextStyleTblRec.Font;

                    // Update the text style's typeface with "PlayBill"
                    Autodesk.AutoCAD.GraphicsInterface.FontDescriptor acNewFont;
                    acNewFont = new Autodesk.AutoCAD.GraphicsInterface.FontDescriptor(fontname,
                                                                        acFont.Bold,
                                                                        acFont.Italic,
                                                                        acFont.CharacterSet,
                                                                        acFont.PitchAndFamily);

                    acTextStyleTblRec.Font = acNewFont;
                }
                tr.Commit();
            }
        }

        public static ObjectId GetArrowObjectId(this Database db, string newArrName)
        {
            ObjectId arrObjId = ObjectId.Null;

            // Get the current value of DIMBLK
            string oldArrName = Application.GetSystemVariable("DIMBLK") as string;

            // Set DIMBLK to the new style
            // (this action may create a new block)
            Application.SetSystemVariable("DIMBLK", newArrName);

            // Reset the previous value of DIMBLK
            if (oldArrName.Length != 0)
                Application.SetSystemVariable("DIMBLK", oldArrName);

            // Now get the objectId of the block
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead);
                arrObjId = bt[newArrName];
                tr.Commit();
            }
            return arrObjId;
        }
        
        public static ObjectId CreateLeaderFromSource(this Database db, pPos basePos, string content, ObjectId sourceId)
        {
            ObjectId res = db.CloneObject(sourceId);
            db.MoveObject(res, basePos - db._getPoint(sourceId));
            db._setContent(res, content);
            return res;
        }

        public static ObjectIdCollection AddSampleDimension(this Database db, IEnumerable<pPos> pts)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                Autodesk.AEC.Interop.ArchBase.AecDimension dim = new Autodesk.AEC.Interop.ArchBase.AecDimension();
                
                Autodesk.AEC.Interop.Base.AecAnchor anchor = new Autodesk.AEC.Interop.Base.AecAnchor();

                
                //AecAnchorEntToWall anchor = new AecAnchorEntToWall();
                //anchor.AttachEntity()
                //AnchorPointToEntity anc = new AnchorPointToEntity();
                //dim.
                //dim.AttachAnchor(anchor);
                //dim.AttachAnchor()

                //btr.AppendEntity((Entity)dim);
                //tr.AddNewlyCreatedDBObject((Entity)dim, true);
                //db.WR("ObjectId:{0}", dim.ObjectID);

                tr.Commit();
            }
            return res;
        }

        public static ObjectId CreateLeader(this Database db, pPos basePos, pPos destPos)
        {
            ObjectId res = ObjectId.Null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Leader leader = new Leader();
                leader.AppendVertex(destPos.ToPoint3());
                leader.AppendVertex(basePos.ToPoint3());
                leader.HasArrowHead = true;
                
                leader.Dimldrblk = db.GetArrowObjectId("_CLOSEDBLANK");
                leader.Dimasz = 200;

                btr.AppendEntity(leader);
                tr.AddNewlyCreatedDBObject(leader, true);
                leader.Annotative = AnnotativeStates.False;

                res = leader.ObjectId;

                tr.Commit();
            }
            return res;
        }

        //public static void LibUpdateContent(this Database db, ObjectIdCollection ids) //FROM LIB
        //{
        //    if (File.Exists(DE.OBJECT_STYLE_DATA))
        //    {
        //        string[] str = File.ReadAllLines(DE.OBJECT_STYLE_DATA).Where(s => !s.empty()).Select(s => s).ToArray();

        //        foreach (ObjectId id in ids)
        //        {
        //            string key = db._getContent(id);

        //            if(!key.empty())
        //                foreach (string s in str)
        //                {
        //                    if (key._firstProp().st_(s._firstProp().Upper()))
        //                    {
        //                        switch (s._firstPropName())
        //                        {
        //                            case "NOTE":
        //                                key = key.Replace(s._firstProp(), s._prop("VALUE"));
        //                                db._setContent(id, key);
        //                                break;
        //                        }

        //                    }
        //                }
        //        }
        //    }
        //}

        //public static ObjectIdCollection LibKeyAction(this Database db, string key, pPos[] value) //FROM LIB
        //{
        //    ObjectIdCollection res = new ObjectIdCollection();

        //    if (File.Exists(DE.OBJECT_STYLE_DATA))
        //    {
        //        string[] str = File.ReadAllLines(DE.OBJECT_STYLE_DATA);
        //        foreach (string s in str)
        //        if(!s.empty()){
        //            if (key._firstProp().Upper().Contains(s._firstProp().Upper()))
        //            {
        //                switch (s._firstPropName())
        //                {
        //                    case "HATCH":
        //                        res.AddRange(db.DrawHatch(value, s));
        //                        break;
                            
        //                }

        //            }
        //        }
        //    }

        //    return res;
        //}

        static ObjectId _definedHatch(this Database db, object sourceId = null)
        {
            ObjectId res = ObjectId.Null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                ObjectIdCollection saveIds = new ObjectIdCollection();

                string hatch_info = null;
                if (sourceId is ObjectId)
                {
                    ObjectId sId = (ObjectId)sourceId;
                    hatch_info = db._getIdInfo(sId);
                    //sthyperlink = db._getHyperlink(sId);
                }else if(sourceId is string)
                {
                    hatch_info = sourceId.ToString();
                }

                using (Hatch hat = new Hatch())
                {
                    try
                    {
                        string pattern = hatch_info._prop("HPATTERN");
                        
                        if(pattern.empty())
                            pattern = hatch_info._prop("HATCH");

                        if (!pattern.empty())
                            hat.SetHatchPattern(HatchPatternType.PreDefined, pattern);
                    }
                    catch (System.Exception ex)
                    {
                        hat.PatternScale = 100;
                        hat.SetHatchPattern(HatchPatternType.PreDefined, "ANSI31");
                    }

                    btr.AppendEntity(hat);
                    tr.AddNewlyCreatedDBObject(hat, true);

                    res = hat.ObjectId;
                    db._setLayer(hat.ObjectId, DE.DEF_LAYER_HATCH);
                    hat.Associative = false;

                    double scale = hatch_info._prop("HSCALE").ToNumber();
                    if (scale != 0)
                        hat.PatternScale = scale;
                    string st = hatch_info._prop("HANGLE");

                    if(!st.empty())
                        hat.PatternAngle = st.ToNumber()/180 * Math.PI;

                    //db.WR("Scale {0} = {1}", hatch_info, scale);

                    hat.HatchStyle = HatchStyle.Normal;

                    //db._setHyperLink(res, hatch_info._prop("HYPERLINK"));
                    //if (sthyperlink != "") db._setHyperLink(res, sthyperlink);
                }
                tr.Commit();
            }
            return res;
        }

        public static ObjectIdCollection DrawHatchFromIds(this Database db, ObjectIdCollection ids, object sourceId = null)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            ObjectId hId = db._definedHatch(sourceId);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    Hatch hat = (Hatch)tr.GetObject(hId, OpenMode.ForWrite);

                    foreach(ObjectId id in ids)
                        hat.AppendLoop(HatchLoopTypes.External, new ObjectIdCollection { id });

                    hat.EvaluateHatch(true);
                    res.Add(hat.ObjectId);
                }
                catch (System.Exception ex)
                {
                    //db.WR("Hatch error!");
                }
                tr.Commit();
            }

            return res;
        }

        public static ObjectIdCollection DrawHatch(this Database db, IEnumerable<pPos> pts, 
            object sourceId = null)
        {
            //db.WR("Draw_HATCH {0}", pts.Count());
            ObjectIdCollection res = new ObjectIdCollection();

            //db.WRArray("Hatch_Pts {0}", pts);
            ObjectIdCollection lwpIds = new ObjectIdCollection(new ObjectId[] { db.DrawPolyline(pts) });
            ObjectId hId = db._definedHatch(sourceId);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    Hatch hat = (Hatch)tr.GetObject(hId, OpenMode.ForWrite);
                    
                    hat.AppendLoop(HatchLoopTypes.External, lwpIds);
                    hat.EvaluateHatch(true);
                    res.Add(hat.ObjectId);

                    bool _showArea = false;

                    if (sourceId is string)
                        _showArea = sourceId.ToString()._prop("SHOWAREA") == "ON" || sourceId.ToString()._prop("SHOWAREA") == "TRUE";

                    if (_showArea)
                        res.Add(db.CreateText("#M" + (pts.Area() / 1000000).roundNumber(2).ToString() + "m\\U+00B2", 
                            db._getPoint(hat.ObjectId), 500));
                }
                catch (System.Exception ex)
                {
                    //db.WR("Hatch error in {0}!", sourceId.ToString());
                }
                tr.Commit();
            }

            db.EraseObjects(lwpIds);
            return res;
        }

        //static bool HatchByParams_Closed = false;


        static PosCollection _rectFormList(IEnumerable<pPos[]> pls, double w, double h)
        {
            PosCollection res = new PosCollection();

            foreach (pPos[] ls in pls)
            {
                pPos p = ls.First();
                res.Add( new pPos[] {p, new pPos(p.X + w, p.Y), p + new pPos(w,h), new pPos(p.X, p.Y + h) } );
            }

            return res;
        }
        
        public static ObjectIdCollection DrawStylePolyline(this Database db, string style,
            IEnumerable<pPos> ls, object sourceId = null)
        {
            ObjectIdCollection res = new ObjectIdCollection();

            if (ls != null && ls.Count() > 0)
            {
                string txtStyle = null;
                if (sourceId != null)
                    txtStyle = sourceId is ObjectId ?
                                    db._getIdInfo((ObjectId)sourceId) : sourceId.ToString();

                string prefix = "#C";

                string content = txtStyle._propContent().Key;
                double sz = txtStyle._propContent().Value;

                pPos txt_pt = ls.First().Parallel(ls.Last(), DE.DEF_TEXT_SPACING * sz).CenterPoint();
                double txt_rot = 0;

                pPos p1 = ls.First();
                pPos p2 = ls.ElementAt(1);
                
                List<pPos> lines = ls.ToList();

                switch (style)
                {
                    case "LSTEEL":
                        res.Add(db._drawLowerSteel(ls, DE.DEF_LOWER_STEEL_SIZE));
                        txt_rot = (ls.Last() - ls.First()).Angle();
                        break;
                    case "USTEEL":
                        pPos[] ln1 = p1.Parallel(p2, -DE.DEF_LOWER_STEEL_SIZE);
                        pPos[] ln2 = ls.ElementAt(ls.Count() - 2).Parallel(ls.Last(), -DE.DEF_LOWER_STEEL_SIZE);

                        lines.Insert(0, ln1.First());
                        lines.Add(ln2.Last());

                        res.Add(db.DrawPolyline(lines, false));
                        txt_rot = (ls.Last() - p1).Angle();
                        break;
                    case "LEADER":
                        lines.Add(lines.Last().Clone());
                        lines[lines.Count - 2] = lines.Last().Along(DE.DEF_LEADER_SIZE * sz, lines[lines.Count - 3]);

                        double[] ws2 = lines.Select(p => 0.0).ToArray();

                        double[] ws1 = ws2.ToArray();
                        ws1[ws1.Length - 2] = DE.DEF_LEADER_SIZE * sz / 3;

                        res.Add(db.DrawPolyline(lines, false,null, ws1, ws2));
                        //res.Add(db.DrawCircle(ls.Last(), 20));
                        prefix = p1.X < p2.X ? "#L" : "#R";
                        txt_pt = ls.First() + new pPos(0, DE.DEF_TEXT_SPACING * sz);
                        break;
                    case "BREAK":
                        res.Add(db.DrawBreakLine(ls.First(),ls.ElementAt(1), DE.DEF_BREAK_LINE_SIZE));
                        break;
                    case "RECT":
                        //db.WR("RECT_OFFSET {0}", txtStyle._prop("OFFSET").ToNumber());
                        res.Add(db.DrawPolyline(ls.First().RectToPoint(ls.Last(), 
                            txtStyle._prop("OFFSET").ToNumber()), true, txtStyle));
                        break;

                    case "CROSS":
                        //db.WR("CROSS_OFFSET {0}", txtStyle._prop("OFFSET").ToNumber());
                        pPos[] line = ls.First().RectToPoint(ls.Last(), txtStyle._prop("OFFSET").ToNumber());
                        res.Add(db.DrawPolyline(new pPos[] { line[0], line[2] }, false, txtStyle));
                        res.Add(db.DrawPolyline(new pPos[] { line[1], line[3] }, false, txtStyle));
                        break;
                }

                if (!content.empty())
                {
                    res.Add(db.CreateText(prefix + content, txt_pt, sz));
                    db._setRotation(res.Last(), Math.Abs(txt_rot));
                }

                if (!txtStyle.empty())
                    db._setIdInfo(res, txtStyle);
            }

            return res;
        }

        
        public static ObjectId DrawBreakLine(this Database db, pPos p1, pPos p2, double extend)
        {
            return db.DrawPolyline(p1.BreakLine(p2, extend), false, "LAYER=A-Hatch");
        }

        
        public static void _setHatchPoint(this Database db, ObjectId objId, pPos pt)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Hatch nHatch = (Hatch)tr.GetObject(objId, OpenMode.ForWrite);

                string hatchName = nHatch.PatternName;
                nHatch.Origin = pt.ToPoint2();
                nHatch.SetHatchPattern(HatchPatternType.PreDefined, hatchName);
                nHatch.EvaluateHatch(true);
                nHatch.Draw();

                tr.Commit();
            }
        }

        public static void ResetDefaultDimText(this Database db, ObjectId objId, bool val)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                RotatedDimension dimObj = (RotatedDimension)tr.GetObject(objId, OpenMode.ForWrite);

                dimObj.UsingDefaultTextPosition = val;
                dimObj.UpgradeOpen();
                tr.Commit();
            }
        }

        public static ObjectIdCollection DrawHatchs(this Database db, PosCollection pls, object sourceId = null)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            ObjectIdCollection lwpIds = db.DrawPolyline(pls);
                        
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    if (pls.Count > 1 && pls[1].Inside(pls[0]))
                    {
                        ObjectId hId = db._definedHatch(sourceId);

                        Hatch hat = (Hatch)tr.GetObject(hId, OpenMode.ForWrite);
                        hat.AppendLoop(HatchLoopTypes.External, new ObjectIdCollection { lwpIds[0] });

                        for (int i = 1; i < lwpIds.Count; i++)
                            hat.AppendLoop(HatchLoopTypes.Default, new ObjectIdCollection(new[] { lwpIds[i] }));

                        hat.RemoveAssociatedObjectIds();
                        hat.EvaluateHatch(true);
                        hat.RecordGraphicsModified(true);

                        res.Add(hId);
                    }
                    else
                    {
                        for (int i = 0; i < lwpIds.Count; i++)
                        {
                            ObjectId hId = db._definedHatch(sourceId);
                            Hatch hat = (Hatch)tr.GetObject(hId, OpenMode.ForWrite);

                            hat.AppendLoop(HatchLoopTypes.Default, new ObjectIdCollection(new[] { lwpIds[i] }));

                            hat.RemoveAssociatedObjectIds();
                            hat.EvaluateHatch(true);
                            hat.RecordGraphicsModified(true);

                            res.Add(hId);
                        }
                    }
                        
                }
                catch (System.Exception ex)
                {

                }
                tr.Commit();
            }

            db.EraseObjects(lwpIds);
            return res;
        }
        
        public static ObjectIdCollection DrawPolyline(this Database db, IEnumerable<pPos> region, string info)
        {
            ObjectIdCollection res = new ObjectIdCollection();

            if (info.Upper() == "EMPTY")
            {
                pPos[] rect = region.Boundary().Rect();
                res.Add(db.DrawPolyline(new pPos[] { rect[0], rect[2] }, false, "LAYER=" + DE.DEF_LAYER_HIDDEN));
                res.Add(db.DrawPolyline(new pPos[] { rect[1], rect[3] }, false, "LAYER=" + DE.DEF_LAYER_HIDDEN));
            }
            else
            {
                string obj_str = info._prop("OBJ");
                double v_offset = info._prop("OFFSET").ToNumber();

                pPos[] pts = region.Offset(v_offset);

                if (pts != null) //&& ((v_offset < 0 && pts.Inside(region)) 
                    //|| (v_offset >= 0 && region.Inside(pts))))
                {
                    if (obj_str == "HATCH")
                        res.AddRange(db.DrawHatch(pts, info));
                    else if (obj_str == "LWPOLYLINE")
                        res.Add(db.DrawPolyline(pts, true, info));
                }
            }
            return res;
        }

        static int extend_current_index;

        static pPos _findIntersect(pPos[] pts, pPos[] to_region)
        {
            pPos[] intersects = to_region.Intersect(pts[0], pts[1]);

            //db.WR("Intersect {0}", intersects.Length);
            extend_current_index = -1;
            pPos res = null;
            double nmin = double.PositiveInfinity;
            for (int i = 0; i < pts.Length; i++)
                for (int j = 0; j < intersects.Length; j++)
                {
                    double n = pts[i].DistanceTo(intersects[j]);
                    if (nmin > n)
                    {
                        nmin = n;
                        res = intersects[j];
                        extend_current_index = i;
                    }
                }

            return res;
        }

        public static ObjectIdCollection TrimObject(this Database db, ObjectId objId, 
            pPos[] region, bool inside = true)
        {
            ObjectIdCollection res = new ObjectIdCollection();

            if (!inside && db._getBound(objId).Rect().Inside(region))
            {
                db.EraseObject(objId);
                return res;
            }

            if (db._isBlock(objId) && db._getPoint(objId).Inside(region))
            {
                res.Add(objId);
                return res;
            }

            int splitarc = (int)pString.INI_String("SPLITARC").ToNumber();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                PosCollection pls = null;
                pPos[] pts = null;

                switch (objId.ObjectClass.DxfName)
                {
                    case "LINE":
                        Line ln = (Line)tr.GetObject(objId, OpenMode.ForWrite);
                        pts = new pPos[] { ln.StartPoint.ToPos(), ln.EndPoint.ToPos()};

                        if (pts.Inside(region))
                        {
                            if(inside)
                                res.Add(objId);
                            else
                                db.EraseObject(objId);
                        }
                        else if(region.Intersect(pts[0],pts[1], true).Length > 0)
                        {
                            //db.WR("LineINtersect {0}", region.Intersect(pts[0], pts[1], true).Length);
                            pls = pts.Trim(region, false, inside);

                            for (int i = 0; i < pls.Count; i++)
                                //if (inside && pls[i].CenterPoint().Inside(region))
                                res.Add(db.DrawPolyline(pls[i], false, objId));
                            db.EraseObject(objId);
                        }
                        
                        break;
                    case "LWPOLYLINE":
                        //Polyline lwp = (Polyline)tr.GetObject(objId, OpenMode.ForWrite);
                        pts = db._getVertices(objId, splitarc, false);

                        if (!region.Inside(pts))
                        {
                            if (pts.Inside(region))
                            {
                                if (inside)
                                    res.Add(objId);
                                else
                                    db.EraseObject(objId);
                            }
                            else
                            {
                                pls = pts.Trim(region, db._isPolylineClosed(objId), inside);
                                foreach (pPos[] ls in pls)
                                {
                                    res.Add(db.DrawPolyline(ls, false, objId));
                                }

                                db.EraseObject(objId);
                            }
                        }
                                               
                        break;

                    case "HATCH":
                        pls = db._getHatch(objId);

                        //if (!pls.Any(ls => region.All(p => p.Inside(ls))))
                        //{
                            if (pls.All(ls => ls.Inside(region)))
                            {
                                if (inside)
                                    res.Add(objId);
                                else
                                    db.EraseObject(objId);
                            }
                            else
                            {
                                PosCollection tmps = new PosCollection();

                                for (int i = 0; i < pls.Count; i++)
                                {
                                    pts = pls[i];

                                    if (pts.Inside(region))
                                    {
                                        if(inside) tmps.Add(pts);
                                    }
                                    else
                                        tmps.AddRange(pts.Trim(region, true, inside));
                                }

                                for (int i = 0; i < tmps.Count; i++)
                                    if (tmps[i].Length > 2)// && tmps[i].CenterPoint().Inside(region))
                                    {
                                        //tmps[i] = tmps[i].Add(tmps[i].First());
                                        res.AddRange(db.DrawHatch(tmps[i], objId));
                                        //res.Add(db.DrawPolyline(tmps[i], false, "LAYER=DEFPOINTS"));
                                    }
                                //if (res.Count > 0)
                                db.EraseObject(objId);
                            }
                        //}
                        break;

                    case "AEC_WALL":
                        pts = db._getVertices(objId, splitarc, false);

                        if (pts.Inside(region))
                        {
                            if (inside)
                                res.Add(objId);
                            else
                                db.EraseObject(objId);
                        }
                        else
                        {
                            Autodesk.Aec.Arch.DatabaseServices.Wall wall = (Autodesk.Aec.Arch.DatabaseServices.Wall)tr.GetObject(objId, OpenMode.ForWrite);
                            pls = (new pPos[] { wall.StartPoint.ToPos(), wall.EndPoint.ToPos() }).Trim(region, false, inside);

                            for (int i = 0; i < pls.Count; i++)
                            {
                                ObjectId new_id = db.CloneObject(objId);
                                db._setPoint(new_id, pls[i][0], 0);
                                db._setPoint(new_id, pls[i][1], 1);
                                res.Add(new_id);
                            }

                            db.EraseObject(objId);
                        }
                        
                        break;
                    case "DIMENSION":
                        //db.WR("RotatedDIm TRIM");
                        if(db._isRotateDim(objId))
                        {
                            pts = db._getVertices(objId);
                            pPos[] npts = db._getDimPoints(objId);

                            pPos[] ls1 = region.Intersect(pts[0], npts[0], true);
                            if (ls1.Length > 0)
                                db._setPoint(objId, ls1[0], 0);

                            pPos[] ls2 = region.Intersect(pts[1], npts[1], true);
                            if (ls2.Length > 0)
                                db._setPoint(objId, ls2[0], 1);
                        }
                        break;
                    case "INSERT":
                        if (db._getPoint(objId).Inside(region) && !inside)
                            db.EraseObject(objId);
                        break;
                    default:
                        pPos[] bb = db._getBound(objId);

                        if (bb != null && bb.Rect().Inside(region))
                        {
                            if (inside)
                                res.Add(objId);
                            else
                                db.EraseObject(objId);
                        }
                        break;
                }
                
                tr.Commit();
            }

            return res;
        }

        public static void ExtendObject(this Database db, ObjectId objId, pPos[] to_region)
        {
            pPos p3d = null;
            extend_current_index = -1;
            int splitarc = (int)pString.INI_String("SPLITARC").ToNumber();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                //BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
                //BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                switch (objId.ObjectClass.DxfName)
                {
                    case "LINE":
                        Line ln = (Line)tr.GetObject(objId, OpenMode.ForWrite);
                        p3d = _findIntersect(new pPos[] { ln.StartPoint.ToPos(), ln.EndPoint.ToPos() }, to_region);
                        if (p3d != null)
                        {
                            if (extend_current_index == 0)
                                ln.StartPoint = p3d.ToPoint3();
                            else
                                ln.EndPoint = p3d.ToPoint3();
                        }
                        break;
                    case "LWPOLYLINE":
                        Polyline lwp = (Polyline)tr.GetObject(objId, OpenMode.ForWrite);
                        pPos[] pts = db._getVertices(objId, splitarc, false);

                        if (pts.Length == 2)
                        {
                            p3d = _findIntersect(new pPos[] { pts[0], pts[1] }, to_region);
                            if (p3d != null)
                            {
                                lwp.SetPointAt(extend_current_index, p3d.ToPoint2());
                            }
                        }
                        /*else
                        {
                            p3d = _findIntersect(new pPos[] { lwp.GetPoint3dAt(1).ToPos(), lwp.GetPoint3dAt(0).ToPos() }, to_region);
                            lwp.StartPoint = p3d.ToPoint3();

                            p3d = _findIntersect(new pPos[] { lwp.GetPoint3dAt(lwp.NumberOfVertices - 2).ToPos()
                                , lwp.GetPoint3dAt(lwp.NumberOfVertices - 1).ToPos() }, to_region);
                            lwp.EndPoint = p3d.ToPoint3();
                        }*/

                        break;

                    case "AEC_WALL":
                        //db.WR("WALL");
                        Autodesk.Aec.Arch.DatabaseServices.Wall wall = (Autodesk.Aec.Arch.DatabaseServices.Wall)tr.GetObject(objId, OpenMode.ForWrite);
                        p3d = _findIntersect(new pPos[] { wall.StartPoint.ToPos(), wall.EndPoint.ToPos() }, to_region);
                        //ACD.DB.DrawCircle(p3d, 200);

                        if (p3d != null)
                        {
                            if (extend_current_index == 0)
                                wall.Set(p3d.ToPoint3(), wall.EndPoint, Vector3d.ZAxis);
                            else
                                wall.Set(wall.StartPoint, p3d.ToPoint3(), Vector3d.ZAxis);
                        }
                        break;
                }

                if(extend_current_index == -1)
                { 
                    pPos pt = db._getPoint(objId);
                    if(pt != null)
                    {
                        double n = pt.DistanceToPts(to_region);
                        if (pPos.DistanceTo_Projection != null)
                        {
                            db._setPoint(objId, pPos.DistanceTo_Projection);
                            //db.DrawCircle(pPos.DistanceTo_Projection, 200);
                        }
                    }
                }

                tr.Commit();
            }
        }
    }
}
