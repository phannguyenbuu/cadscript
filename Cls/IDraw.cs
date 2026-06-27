using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Internal;
using AEC = Autodesk.Aec.Arch.DatabaseServices;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcadScript
{
    public static class IDraw
    {
        static int FilletAt(Polyline pline, int index, double radius)
        {
            int prev = index == 0 && pline.Closed ? pline.NumberOfVertices - 1 : index - 1;
            if (pline.GetSegmentType(prev) != SegmentType.Line ||
                pline.GetSegmentType(index) != SegmentType.Line)
            {
                return 0;
            }
            LineSegment2d seg1 = pline.GetLineSegment2dAt(prev);
            LineSegment2d seg2 = pline.GetLineSegment2dAt(index);
            Vector2d vec1 = seg1.StartPoint - seg1.EndPoint;
            Vector2d vec2 = seg2.EndPoint - seg2.StartPoint;
            double angle = (Math.PI - vec1.GetAngleTo(vec2)) / 2.0;
            double dist = radius * Math.Tan(angle);
            if (dist == 0.0 || dist > seg1.Length || dist > seg2.Length)
            {
                return 0;
            }
            Point2d pt1 = seg1.EndPoint + vec1.GetNormal() * dist;
            Point2d pt2 = seg2.StartPoint + vec2.GetNormal() * dist;
            double bulge = Math.Tan(angle / 2.0);
            if (Clockwise(seg1.StartPoint, seg1.EndPoint, seg2.EndPoint))
            {
                bulge = -bulge;
            }
            pline.AddVertexAt(index, pt1, bulge, 0.0, 0.0);
            pline.SetPointAt(index + 1, pt2);
            return 1;
        }
        static bool Clockwise(Point2d p1, Point2d p2, Point2d p3)
        {
            return ((p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X)) < 1e-8;
        }

        public static void FilletAll(this Database db, ObjectId lwpId, double radius)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Polyline pline = (Polyline)tr.GetObject(lwpId, OpenMode.ForWrite);

                int n = pline.Closed ? 0 : 1;
                for (int i = n; i < pline.NumberOfVertices - n; i += 1 + FilletAt(pline, i, radius))
                {
                    
                }

                tr.Commit();
            }

        }


        public static void FilletAt(this Database db, ObjectId lwpId, int index, double radius)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Polyline pline = (Polyline)tr.GetObject(lwpId, OpenMode.ForWrite);

                int n = pline.Closed ? 0 : 1;

                double lw = pline.ConstantWidth;

                FilletAt(pline, index, radius);

                pline.ConstantWidth = lw;

                tr.Commit();
            }

        }

        public static string CreateLayer(this Database db, string layInfo)
        {
            if (layInfo == null) return null;

            string layName = null, _lineType = null;
            short _colorIndex = -1;

            string[] ar = layInfo.filter();
            if (ar.Length > 0) layName = ar[0];

            if (ar.Length > 1)
            {
                //db.WR("CHARACTER = {0}\r\n", ar[1]);
                _colorIndex = (short)ar[1].ToNumber();
            }
            if (ar.Length > 2) _lineType = ar[2];

            if (layName != null)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);
                    LayerTableRecord ltr = null;

                    if (!lt.Has(layName))
                    {
                        ltr = new LayerTableRecord();
                        ltr.Name = layName;
                        ObjectId ltId = lt.Add(ltr);
                        //db.Clayer = ltId;
                        tr.AddNewlyCreatedDBObject(ltr, true);
                    }
                    else
                        ltr = (LayerTableRecord)tr.GetObject(lt[layName], OpenMode.ForWrite);

                    if (_colorIndex != -1) ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, _colorIndex);
                    if (_lineType != null)
                    {
                        LinetypeTable acLinTbl = tr.GetObject(db.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;
                        //if (!acLinTbl.Has(_lineType))  db.LoadLineTypeFile(_lineType, "acadiso.lin");

                        if (acLinTbl.Has(_lineType))
                        {
                            lt.UpgradeOpen();
                            ltr.LinetypeObjectId = acLinTbl[_lineType];
                        }
                    }

                    lt.UpgradeOpen();
                    tr.Commit();
                }
                return layName;
            }
            return null;
        }

        public static void CreateEllipse(this Database db, pPos pt, double radius1, double radius2, string LayerInfo = "G-Region")
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                string sLayer = db.CreateLayer(LayerInfo);

                if (radius1 == radius2)
                {
                    var circle = new Circle();
                    circle.Radius = radius1;
                    circle.Center = new Point3d(pt.X, pt.Y, 0);

                    btr.AppendEntity(circle);
                    tr.AddNewlyCreatedDBObject(circle, true);
                    if (sLayer != null) circle.Layer = sLayer;
                }
                else
                {
                    Vector3d majorAxis;
                    double ratio;
                    if (radius1 > radius2)
                    {
                        ratio = radius2 / radius1;
                        majorAxis = radius1 * Vector3d.XAxis;
                    }
                    else
                    {
                        ratio = radius1 / radius2;
                        majorAxis = radius2 * Vector3d.YAxis;
                    }
                    Vector3d normal = Vector3d.ZAxis;

                    var elip = new Ellipse(new Point3d(pt.X, pt.Y, 0), normal,
                        majorAxis, ratio, 0, 360 * Math.Atan(1.0) / 45.0);

                    btr.AppendEntity(elip);
                    tr.AddNewlyCreatedDBObject(elip, true);
                    if (sLayer != null) elip.Layer = sLayer;
                }
                tr.Commit();
            }
        }

        public static ObjectId DrawZigZag(this Database db, pPos[] ls, double size, bool closed = true)
        {
            ObjectId res = ObjectId.Null;

            List<pPos> pls = new List<pPos>();
            bool up = true;

            for (int i = 0; i < ls.Length; i++)
            {
                pPos pt = ls[i];

                if (i < ls.Length - 1)
                {
                    int nex = (i + 1) % ls.Length;
                    bool xaxis = Math.Abs(pt.Y - ls[nex].Y) < 1;

                    if (i == 0) pls.Add(pt);
                    double n = up ? size : -size;

                    if (xaxis)
                    {
                        pls.Add(new pPos(pt.X, pt.Y + n));
                        pls.Add(new pPos(ls[nex].X, pt.Y + n));
                    }
                    else
                    {
                        pls.Add(new pPos(pt.X + n, pt.Y));
                        pls.Add(new pPos(pt.X + n, ls[nex].Y));
                    }
                    up = !up;
                }
                else
                    pls.Add(pt);
            }

            if (pls.Count > 0)
                res = DrawPolyline(db, pls.ToArray(), closed);

            return res;
        }

        /*public static ObjectId DrawRoundRect(this Database db, pPos pt1, pPos pt2, double radius)
        {
            ObjectId res = ObjectId.Null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                pPos[] bb = new pPos[] { pt1, pt2 }.Boundary();
                var pl = new Polyline(8);

                List<pPos> ls = new List<pPos> { new pPos(bb[0].X + radius, bb[0].Y), new pPos(bb[1].X - radius, bb[0].Y),
                                                new pPos(bb[1].X, bb[0].Y + radius), new pPos(bb[1].X, bb[1].Y - radius),
                                                new pPos(bb[1].X - radius, bb[1].Y), new pPos(bb[0].X + radius, bb[1].Y),
                                                new pPos(bb[0].X, bb[1].Y - radius), new pPos(bb[0].X, bb[0].Y + radius)};

                for (int i = 0; i < ls.Count(); i++)
                {
                    pl.AddVertexAt(i, ls.ElementAt(i).ToPoint2(),i % 2 == 0 ? 0 : 0.4, 0, 0);
                    
                }

                pl.Closed = true;

                res = btr.AppendEntity(pl);
                tr.AddNewlyCreatedDBObject(pl, true);
                tr.Commit();
            }
            return res;
        }*/

        public static ObjectId DrawArc(this Database db, pPos ct, double radius,
            double start_angle, double end_angle, object sourceId = null)
        {
            ObjectId res = ObjectId.Null;
            using (Transaction acTrans = db.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                OpenMode.ForWrite) as BlockTableRecord;

                // Create an arc that is at 6.25,9.125 with a radius of 6, and
                // starts at 64 degrees and ends at 204 degrees
                using (Arc acArc = new Arc(ct.ToPoint3(), radius,
                    start_angle / 180 * Math.PI, end_angle / 180 * Math.PI))
                {
                    res = acBlkTblRec.AppendEntity(acArc);
                    acTrans.AddNewlyCreatedDBObject(acArc, true);
                }

                // Save the new line to the database
                acTrans.Commit();
            }

            return res;
        }

        public static ObjectId DrawArrow(this Database db, pPos p1, pPos p2,
            double arrow_width = 50, object sourceId = null)
        {
            ObjectId res = ObjectId.Null;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Polyline pl = new Polyline(2);
                pl.AddVertexAt(0, p1.ToPoint2(), 0, 0, 0);
                pl.AddVertexAt(0, p2.ToPoint2(), 0, 0, arrow_width);

                pl.Closed = false;

                res = btr.AppendEntity(pl);
                tr.AddNewlyCreatedDBObject(pl, true);

                if (sourceId != null)
                {
                    string sStyle = sourceId is ObjectId ?
                        db._getIdInfo((ObjectId)sourceId) : sourceId.ToString();
                    if (!sStyle.empty())
                        db._setIdInfo(res, sStyle);
                }

                tr.Commit();
            }
            return res;
        }

        public static void _setIdInfo(this Database db, ObjectIdCollection ids, object sourceId)
        {
            foreach (ObjectId id in ids)
                db._setIdInfo(id, sourceId);
        }

        public static void _setLineworkLineScale(this Database db, ObjectIdCollection ids, double value)
        {
            foreach (ObjectId id in ids)
                db._setLineworkLineScale(id, value);
        }

        public static void _setLineworkLineScale(this Database db, ObjectId id, double value)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                //LayerTableRecord acLyrTblRec = tr.GetObject(acLyrTbl[sLayerName], OpenMode.ForWrite) as LayerTableRecord;
                if (ent != null) ent.LinetypeScale = value;
                tr.Commit();
            }
        }

        public static void _setLineworkLineColor(this Database db, ObjectId id, int color)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                //LayerTableRecord acLyrTblRec = tr.GetObject(acLyrTbl[sLayerName], OpenMode.ForWrite) as LayerTableRecord;
                if (ent != null)
                    ent.ColorIndex = color;
                tr.Commit();
            }
        }

        static void _loadLinetype(this Database acCurDb, string sLineTypName)
        {
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                try
                {
                    LinetypeTable acLineTypTbl;
                    acLineTypTbl = acTrans.GetObject(acCurDb.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;

                    if (acLineTypTbl.Has(sLineTypName) == false)
                        acCurDb.LoadLineTypeFile(sLineTypName, "acadiso.lin");
                }
                catch (System.Exception er)
                {
                    //acCurDb.WR("Cannot find linestyle {0}", sLineTypName);
                }
                acTrans.Commit();
            }
        }

        public static void _setLineworkLineType(this Database db, ObjectId id, string value)
        {
            if (id.ObjectClass.DxfName == "LWPOLYLINE")
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                    //LayerTableRecord acLyrTblRec = tr.GetObject(acLyrTbl[sLayerName], OpenMode.ForWrite) as LayerTableRecord;
                    if (ent != null)
                    {
                        LinetypeTable acLinTbl = tr.GetObject(db.LinetypeTableId, OpenMode.ForWrite) as LinetypeTable;
                        if (!acLinTbl.Has(value)) _loadLinetype(db, value);

                        if (acLinTbl.Has(value))
                            ent.Linetype = value;
                    }
                    tr.Commit();
                }
        }

        public static void _setLineworkWidth(this Database db, ObjectId id, double value = 0)
        {
            if (id.ObjectClass.DxfName == "LWPOLYLINE")
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Polyline ent = (Polyline)tr.GetObject(id, OpenMode.ForWrite);
                    if (ent != null) ent.ConstantWidth = value;
                    tr.Commit();
                }
        }

        public static string[] ListLayers(this Database db)
        {
            List<string> res = new List<string>();

            LayerTableRecord layer;
            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                foreach (ObjectId layerId in lt)
                {
                    layer = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;
                    res.Add(layer.Name);
                }
                tr.Commit();
            }
            return res.OrderBy(l => l).ToArray();
        }


        public static bool isLockLayer(this Database db, string sLayerName)
        {
            bool res = false;
            // Start a transaction
            using (Transaction acTrans = db.TransactionManager.StartTransaction())
            {
                // Open the Layer table for read
                LayerTable acLyrTbl = acTrans.GetObject(db.LayerTableId,
                                                OpenMode.ForRead) as LayerTable;

                //string sLayerName = "ABC";

                if (acLyrTbl.Has(sLayerName))
                {
                    using (LayerTableRecord acLyrTblRec = new LayerTableRecord())
                    {
                        res = acLyrTblRec.IsLocked;
                    }
                }

                acTrans.Commit();
            }

            return res;
        }
        public static void _setLayer(this Database db, ObjectId id, string LayerInfo)
        {
            string layName = null;
            string[] ar = LayerInfo.filter();
            if (ar.Length > 0)
                layName = ar[0];

            if (!layName.empty() && !LayerInfo.empty() && !db.isLockLayer(layName))
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                    string[] layers = db.ListLayers();
                    int index = layers.ToList().FindIndex(s => s.Upper() == layName);

                    if (index == -1)
                    {
                        db.CreateLayer(LayerInfo);
                        //Random random = new Random();
                        //db._setLayerColor(layName, (short)random.Next(20, 200));
                    }
                    else
                        layName = layers[index];

                    ent.Layer = layName;
                    tr.Commit();
                }
        }

        public static void _setIdInfo(this Database db, ObjectId id, object sourceId)
        {
            //db.WR("LAYER {0}", info._prop("LAYER"));
            string info = null;
            if (sourceId is string)
                info = sourceId.ToString();
            else if (sourceId is ObjectId)
                info = db._getIdInfo((ObjectId)sourceId);

            if (!info.empty())
            {
                if (!info._prop("LAYER").empty())
                    db._setLayer(id, info._prop("LAYER"));

                double w = info._prop("LWIDTH").ToNumber();
                if (db._isPolyline(id) && w > 0)
                    db._setLineworkWidth(id, w);

                string st = info._prop("LTYPE");
                if (!st.empty())
                    db._setLineworkLineType(id, st);

                st = info._prop("LCOLOR");
                if (!st.empty())
                    db._setLineworkLineColor(id, (int)st.ToNumber());

                w = info._prop("LSCALE").ToNumber();
                if (w > 0)
                    db._setLineworkLineScale(id, w);

                //st = info._prop("HYPERLINK");
                //if (!st.empty())
                //    db._setHyperLink(id, st);
            }
        }

        static Polyline _bulgePolyline(IEnumerable<pPos> ls,
            double round, double _bulgevalue, bool closed = true)
        {
            Polyline pl = new Polyline(ls.Count() * 2);

            int start = 0, end = ls.Count();
            if (!closed)
            {
                //pl = new Polyline((ls.Count() ) * 2);
                start++;
                end--;
            }

            for (int i = 0; i < ls.Count(); i++)
            {
                int prev = i > 0 ? i - 1 : ls.Count() - 1;
                int nex = (i + 1) % ls.Count();
                pPos pt1 = ls.ElementAt(i).Along(round, ls.ElementAt(prev));

                if (i == 0 && !pt1.IsBetween(ls.ElementAt(i), ls.ElementAt(prev)))
                {
                    round = -round;
                    pt1 = ls.ElementAt(i).Along(round, ls.ElementAt(prev));
                }

                pPos pt2 = ls.ElementAt(i).Along(round, ls.ElementAt(nex));

                if (!closed)
                {
                    if (i == 0)
                        pl.AddVertexAt(i * 2, ls.First().ToPoint2(), 0, 0, 0);
                    else if (i == ls.Count() - 1)
                        pl.AddVertexAt(i * 2, pt1.ToPoint2(), 0, 0, 0);
                    else
                        pl.AddVertexAt(i * 2, pt1.ToPoint2(), _bulgevalue, 0, 0);
                }
                else
                    pl.AddVertexAt(i * 2, pt1.ToPoint2(), _bulgevalue, 0, 0);

                if (!closed && i == ls.Count() - 1)
                    pl.AddVertexAt(i * 2 + 1, ls.Last().ToPoint2(), 0, 0, 0);
                else
                    pl.AddVertexAt(i * 2 + 1, pt2.ToPoint2(), 0, 0, 0);
            }

            return pl;
        }

        public static ObjectIdCollection DrawPolyline(this Database db, PosCollection pls, object sourceId = null)
        {
            ObjectIdCollection res = new ObjectIdCollection();

            for (int i = 0; i < pls.Count; i++)
                res.Add(db.DrawPolyline(pls[i], pls.Closed.Length > i ? pls.Closed[i] : false, sourceId));

            return res;
        }

        public static ObjectId _drawLowerSteel(this Database db, IEnumerable<pPos> ls, double r)
        {
            ObjectId res = ObjectId.Null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                int total = ls.Count() + 4;
                Polyline pl = new Polyline(total);
                pl.Closed = false;

                pPos[] ln = ls.ElementAt(0).Parallel(ls.ElementAt(1), -r);
                pl.AddVertexAt(0, ln[0].Along(r * 2, ln[1]).ToPoint2(), 0, 0, 0);
                pl.AddVertexAt(1, ln[0].Along(r, ln[1]).ToPoint2(), -1, 0, 0);

                pl.AddVertexAt(2, ls.First().Along(r, ls.ElementAt(1)).ToPoint2(), 0, 0, 0);

                for (int i = 1; i < ls.Count() - 1; i++)
                    pl.AddVertexAt(i + 2, ls.ElementAt(i).ToPoint2(), 0, 0, 0);

                pl.AddVertexAt(total - 3, ls.Last().Along(r, ls.ElementAt(ls.Count() - 2)).ToPoint2(), -1, 0, 0);
                ln = ls.ElementAt(ls.Count() - 2).Parallel(ls.Last(), -r);

                //db.WR("LS {0}", ls.ToText());
                //db.WR("LN {0}",ln);

                //for (int i = 0; i < ln.Length; i++)
                //ACD.DB.CreateText(i + "*", ln[i]);

                pl.AddVertexAt(total - 2, ln[1].Along(r, ln[0]).ToPoint2(), 0, 0, 0);
                pl.AddVertexAt(total - 1, ln[1].Along(r * 2, ln[0]).ToPoint2(), 0, 0, 0);

                //for (int i = 0; i < total; i++)
                //ACD.DB.CreateText(i + "/" + total, pl.GetPoint2dAt(i).ToPos());

                res = btr.AppendEntity(pl);
                tr.AddNewlyCreatedDBObject(pl, true);
                tr.Commit();
            }

            return res;
        }

        //public static ObjectId DrawPolyline(this Database db, pPos[] ls,
        //    bool closed = true, object sourceId = null, double[] start_widths = null, double[] end_widths = null)
        //{
        //    return db.DrawPolyline(ls.ToList(), closed, sourceId, start_widths, end_widths);
        //}

        public static ObjectId DrawLine(this Database db, pPos p1, pPos p2)
        {
            ObjectId res = ObjectId.Null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Polyline pl = new Polyline();
                    
                pl = new Polyline(2);

                pl.AddVertexAt(0, p1.ToPoint2(), 0, 0, 0);
                pl.AddVertexAt(1, p2.ToPoint2(), 0, 0, 0);
                pl.Closed = false;

                res = btr.AppendEntity(pl);
                tr.AddNewlyCreatedDBObject(pl, true);

                tr.Commit();
            }

            return res;
        }

        public static void DrawIndexCircle(this Database db, pPos pt, int color, string xdata)
        {
            ObjectIdCollection cirIds = db.DrawCircle(pt, 200);
            db._setLineworkLineColor(cirIds.First(), color);
            db.SetXNotes(cirIds.First(), xdata);
        }

        public static pPos[] AnyParamsToPts(params object[] ls)
        {
            List<pPos> pts = new List<pPos>();
            
            pPos pt = new pPos(0, 0);
            pPos basept = new pPos(0, 0);
            int number_index = 0;

            foreach (var obj in ls)
                if (obj is string)
                {
                    string st = obj.ToString();
                    if (st.st_("$"))
                         basept = pPos.FromString(st.Substring(1).Replace(" ",","));
                }

            //string s_number = "0123456789";

            foreach (var obj in ls)
                if (obj is pPos)
                {
                    pts.Add((pPos)obj);
                }
                else if (obj.GetType() == typeof(pPos[]))
                    pts.AddRange((pPos[])obj);
                else if (!(obj is string))
                {
                    double n = obj.ToNumber(double.NaN);

                    if (!double.IsNaN(n))
                    {
                        pt[number_index % 2] = n;

                        if (number_index % 2 == 1)
                            pts.Add(pt.Clone());

                        number_index++;
                    }
                }

            return pts.Move(basept).ToArray();
        }

        public static ObjectIdCollection Dim2D(this Database db, params object[] objs)
        {
            string txt_override = "<>";
            double spacing = 0;

            foreach (var obj in objs)
                if (obj is string)
                {
                    string st = obj.ToString();

                    if (st.st_("S"))
                        spacing = st.Substring(1).ToNumber();
                    else if (st.st_("T"))
                        txt_override = st.Substring(1);
                }

            pPos[] pts = AnyParamsToPts(objs);

            return pts.Length > 1 ? DE.NumericArray(0, pts.Length - 2).Select(i 
                => IDimChain.CreateDimension(ACD.DB, pts[i], pts[i + 1], 

                pts[i].X.roundNumber(100) == pts[i + 1].X.roundNumber(100) ?
                    new pPos(pts[i].X + spacing, pts[i].Y)
                    : new pPos(pts[i].X, pts[i].Y + spacing),

                txt_override)).ToCollection() 
                : new ObjectIdCollection();
        }

        public static ObjectId Draw2D(this Database db, params object[] objs)
        {
            bool closed = false;
            string info = "";
            Dictionary<int,double> fillets = new Dictionary<int, double>();

            foreach (var obj in objs)
                if (obj is string)
                {
                    string st = obj.ToString();

                    if (st.Upper() == "C")
                        closed = true;
                    
                    else if (st.st_("F"))
                        fillets.Add((int)st.Substring(1).filter(" ,").First().ToNumber(), 
                            st.Substring(1).filter(" ,").Last().ToNumber());
                    else if (st.st_("LAYER") || st.st_("LWIDTH") || st.st_("LTYPE") || st.st_("LSCALE"))
                        info += "|" + st + "|";
                    else if (!st.ct_("=") && !st.ct_("$"))
                        info += "LAYER=" + st + "|";
                }

            pPos[] pts = AnyParamsToPts(objs);
            ObjectId lwpId = ObjectId.Null;

            if (pts.Length > 0)
            {
                lwpId = db.DrawPolyline(pts, closed, info);

                foreach(var itm in fillets)
                    db.FilletAt(lwpId, itm.Key, itm.Value);
            }

            return lwpId;
        }

        public static ObjectId DrawPolyline(this Database db, IEnumerable<pPos> ls,
            bool closed = true, object sourceId = null, double[] start_widths = null, double[] end_widths = null)
        {
            //ACD.WR("DrawPolyline.01");
            ObjectId res = ObjectId.Null;
            string txtStyle = null;

            if (sourceId != null)
                txtStyle = sourceId is ObjectId ?
                                db._getIdInfo((ObjectId)sourceId) : sourceId.ToString();
            //ACD.WR("DrawPolyline.02");
            double round = txtStyle._prop("round").ToNumber();
            bool ShowCharacter = txtStyle._prop("character") == "TRUE" || txtStyle._prop("character") == "ON";
            //ACD.WR("DrawPolyline.03");
            if (ls != null && ls.Count() > 0)
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                    //ACD.WR("DrawPolyline.04");
                    Polyline pl = new Polyline();
                    //pPos[] ls = _ls.SortClockwise();
                    //ACD.WR("DrawPolyline.05");
                    if ((ls.First().DistanceToPoint(ls.Last()) < 1))
                    {
                        closed = true;
                        List<pPos> tmp = ls.ToList();
                        tmp.RemoveAt(tmp.Count - 1);
                        ls = tmp.ToArray();
                    }
                    //ACD.WR("DrawPolyline.06");
                    if (round == 0)
                    {
                        if (start_widths == null)
                            start_widths = ls.Select(i => 0.0).ToArray();
                        if (end_widths == null)
                            end_widths = ls.Select(i => 0.0).ToArray();
                        //ACD.WR("DrawPolyline.07");
                        pl = new Polyline(ls.Count());

                        for (int i = 0; i < ls.Count(); i++)
                        {
                            pl.AddVertexAt(i, ls.ElementAt(i).ToPoint2(), 0, start_widths[i], end_widths[i]);

                            //if (ShowCharacter)
                                //db.CreateText("#C" + i.ToString(), ls.ElementAt(i), 200); //(char)(65 + i)
                        }
                    }
                    else
                    {
                        pl = new double[] { -0.4, 0.4 }.Select(d => _bulgePolyline(ls, round, d, closed)).OrderBy(obj => -obj.Area).First();
                        //db.DrawCircle(pl.StartPoint.ToPos(), 100);
                    }
                    //ACD.WR("DrawPolyline.08");
                    pl.Closed = closed;

                    res = btr.AppendEntity(pl);
                    tr.AddNewlyCreatedDBObject(pl, true);

                    if (!txtStyle.empty())
                    {
                        //db.WR("Info {0}", txtStyle);
                        db._setIdInfo(res, txtStyle.Replace(txtStyle._firstPropName(), "LAYER"));
                    }
                    //ACD.WR("DrawPolyline.09");
                    tr.Commit();
                }
            return res;
        }
                
        public static ObjectIdCollection DrawCircle(this Database db, IEnumerable<pPos> pts,
            double radius, object sourceId = null)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                foreach (pPos pt in pts)
                {
                    var cc = new Circle();
                    cc.Center = pt.ToPoint3();
                    cc.Radius = radius;

                    res.Add(btr.AppendEntity(cc));
                    tr.AddNewlyCreatedDBObject(cc, true);
                }
                tr.Commit();
            }
            string txtStyle = null;
            if (sourceId != null)
            {
                txtStyle = sourceId is ObjectId ?
                    db._getIdInfo((ObjectId)sourceId) : sourceId.ToString();

                //if (txtStyle._prop("FILL").ToBool())
                //    res.AddRange(db.DrawHatch(res, "HPATTERN=SOLID"));
            }

            db._setIdInfo(res, sourceId);
            //}

            return res;
        }

        public static ObjectIdCollection DrawCircle(this Database db, pPos center,
            double radius, object sourceId = null)
        {
            return db.DrawCircle(new pPos[] { center }, radius, sourceId);
        }

        public static ObjectId DrawEllipse(this Database db, pPos center,
            double radius1, double radius2, string LayerInfo = "A-Wall|4", string sHyperlink = null)
        {
            ObjectId res = ObjectId.Null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Vector3d axis = new Vector3d(radius1, 0, 0);
                if (radius2 > radius1)
                {
                    double tmp = radius2;
                    radius2 = radius1;
                    radius1 = tmp;
                    axis = new Vector3d(0, radius1, 0);
                }

                var cc = new Ellipse(new Point3d(center.X, center.Y, 0),
                    new Vector3d(0, 0, 1), axis,
                    (double)(radius2 / radius1), 0, 360 * Math.Atan(1.0) / 45.0);

                res = btr.AppendEntity(cc);
                tr.AddNewlyCreatedDBObject(cc, true);

                //db._setEntityInfo(cc.ObjectId, LayerInfo, sHyperlink);

                tr.Commit();
            }
            return res;
        }

        public static ObjectIdCollection DrawWall(this Database db, pPos[] ls, string _wallStyleName,
            string LayerInfo = "A-Wall|4", string sHyperlink = null, bool explode = false)
        {
            PosCollection pls = ls.ToCollection();
            pls.Closed = pls.Select(s => false).ToArray();
            return db.DrawWall(pls, _wallStyleName, LayerInfo, sHyperlink, explode);
        }

        public static ObjectIdCollection DrawWall(this Database db, pPos p1, pPos p2, string _wallStyleName,
            string LayerInfo = "A-Wall|4", string sHyperlink = null, bool explode = false)
        {
            PosCollection pls = new pPos[] { p1, p2 }.ToCollection();
            pls.Closed = pls.Select(ls => false).ToArray();
            return db.DrawWall(pls, _wallStyleName, LayerInfo, sHyperlink, explode);
        }

        public static ObjectIdCollection DrawWall(this Database db, PosCollection pls, string _wallStyleName,
            string LayerInfo = "A-Wall|4", string sHyperlink = null, bool explode = false)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            //if (ls != null && ls.Count > 0)
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var wallStyles = new Autodesk.Aec.Arch.DatabaseServices.DictionaryWallStyle(db);
                ObjectId wallStyleId = ObjectId.Null;

                if (wallStyles.Has(_wallStyleName, tr))
                    wallStyleId = wallStyles.GetAt(_wallStyleName);

                for (int i = 0; i < pls.Count; i++)
                    for (int j = 0; j < pls[i].Length; j++)
                        if (pls.Closed[i] || j < pls[i].Length - 1)
                        {
                            var p1 = new Point3d(pls[i][j].X, pls[i][j].Y, 0);
                            var p2 = new Point3d(pls[i][(j + 1) % pls[i].Length].X, pls[i][(j + 1) % pls[i].Length].Y, 0);
                            var wall = new AEC.Wall();
                            wall.SetDatabaseDefaults(db);
                            wall.SetToStandard(db);
                            wall.Set(p1, p2, new Vector3d(0, 0, 1));

                            res.Add(btr.AppendEntity(wall));
                            tr.AddNewlyCreatedDBObject(wall, true);

                            if (!wallStyleId.IsNull)
                            {
                                AEC.WallStyle ws = (AEC.WallStyle)tr.GetObject(wallStyleId, OpenMode.ForRead);
                                wall.StyleId = wallStyleId;
                                wall.UpgradeOpen();
                            }

                            if (explode)
                            {
                                DBObjectCollection ents = new DBObjectCollection();
                                wall.Explode(ents);

                                wall.UpgradeOpen();
                                wall.Erase();

                                foreach (Entity ent in ents)
                                {
                                    btr.AppendEntity(ent);
                                    tr.AddNewlyCreatedDBObject(ent, true);
                                }
                            }
                        }

                tr.Commit();
            }

            return res;
        }

        public static void _setPolylineClosed(this Database db, ObjectId objId, bool closed)
        //public static pPos[] _getVertices(Database db, ObjectId objId, bool splitarc = true)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (objId.ObjectClass.DxfName == "LWPOLYLINE")
                {
                    Polyline lwp = (Polyline)tr.GetObject(objId, OpenMode.ForWrite);
                    lwp.Closed = closed;
                }
                tr.Commit();
            }
        }

        public static bool _isWallArc(this Database db, ObjectId entId)
        {
            bool res = false;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Autodesk.Aec.Arch.DatabaseServices.Wall wall
                    = (Autodesk.Aec.Arch.DatabaseServices.Wall)tr.GetObject(entId, OpenMode.ForRead);

                res = wall != null && wall.WallGeometryType == Autodesk.Aec.Arch.DatabaseServices.WallType.Arc;
                
                tr.Commit();
            }

            return res;
        }

        public static PosCollection _getWallOpenings(this Database db, ObjectId entId)
        {
            PosCollection res = new PosCollection();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Autodesk.Aec.Arch.DatabaseServices.Wall wall
                    = (Autodesk.Aec.Arch.DatabaseServices.Wall)tr.GetObject(entId, OpenMode.ForRead);

                ObjectIdCollection openIds = wall.GetOpeningsFor();

                foreach(ObjectId _id in openIds)
                {
                    pPos[] pts = db._getVertices(_id);
                    pts[0].Content = db._getIdName(_id) + "{";

                    if(db._isDoor(_id))
                    {
                        pts[0].Content += "width:" + db._getDoorWidth(_id) + ";";
                    }

                    pts[0].Content += "}";

                    res.Add(pts);
                }

                tr.Commit();
            }

            return res;
           
        }

        public static int _getWallJustify(this Database db, ObjectId entId)
        {
            //-1:Left, 0:Center, 1:Right
            int res = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Autodesk.Aec.Arch.DatabaseServices.Wall wall
                    = (Autodesk.Aec.Arch.DatabaseServices.Wall)tr.GetObject(entId, OpenMode.ForRead);

                

                if (wall.JustificationType == Autodesk.Aec.Arch.DatabaseServices.WallJustificationType.Left)
                    res = -1;
                else if (wall.JustificationType == Autodesk.Aec.Arch.DatabaseServices.WallJustificationType.Right)
                    res = 1;

                tr.Commit();
            }

            return res;
        }

        public static pPos[] _getWallShape(this Database db, ObjectId entId)
        {
            ObjectId wshapeId = db.GetWallShape(entId, 0);

            pPos[] res = db._getVertices(wshapeId);
            db.EraseObject(wshapeId);
            
            return res;
        }

        public static ObjectId GetWallShape(this Database db, ObjectId entId, double extend = 200)
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

                            if (wall.JustificationType == Autodesk.Aec.Arch.DatabaseServices.WallJustificationType.Left)
                            {
                                res = pls.ToList();
                                res.AddRange(pls[0].Parallel(pls[1], -w).Reverse());
                            }
                        }
                        resId = db.DrawPolyline(res, true);
                    }
                    else
                    {
                        pPos p1 = wall.StartPoint.ToPos(), p2 = wall.MidPoint.ToPos(), p3 = wall.EndPoint.ToPos();
                        CircularArc3d carc = new CircularArc3d(wall.StartPoint, wall.MidPoint, wall.EndPoint);
                        pPos ct = carc.Center.ToPos();
                    }
                }

                tr.Commit();
            }

            if (!resId.IsNull)
                db._setPolylineClosed(resId, true);

            return resId;
        }

    }
}
