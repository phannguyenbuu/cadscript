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

namespace AcadScript
{
    public class PolylineRoundVertCLS
    {
        static double RoundValue(double v, int round)
        {
            return v.roundNumber(round);
        }
        static pPos RoundValue(pPos pt, int round)
        {
            return new pPos(RoundValue(pt.X,round), RoundValue(pt.Y, round));
        }

        static string _replaceT(string st)
        {
            return st.Replace("|", "/").Replace("=", "~").Replace("#", "");
        }

        static Point2d RoundValue(Point2d p,int round)
        {
            return RoundValue(p.ToPos2d(), round).ToPoint2();
        }

        static void _roundHatch(ObjectId id, int round)
        {
            using (Transaction tr = ACD.DB.TransactionManager.StartTransaction())
            {
                Hatch hat = (Hatch)tr.GetObject(id, OpenMode.ForWrite);
                HatchLoop[] ls = DE.NumericArray(0, hat.NumberOfLoops - 1).Select(i => hat.GetLoopAt(i)).ToArray();
                BlockTableRecord btr = tr.GetObject(hat.OwnerId, OpenMode.ForWrite) as BlockTableRecord;

                //ACD.WR("R0");

                if (btr == null) return;
                Plane plane = hat.GetPlane();

                var hatch_pattern = hat.PatternName;
                var hatch_scale = hat.PatternScale;
                var hatch_type = hat.PatternType;

                while (hat.NumberOfLoops > 0)
                    hat.RemoveLoopAt(0);
                //ACD.WR("R1");
                Hatch new_hatch = new Hatch();
                
                foreach (var loop in ls)
               
                    if (loop.IsPolyline)
                    {
                        for (int i = 0; i < loop.Polyline.Count; i++)
                            loop.Polyline[i].Vertex = RoundValue(loop.Polyline[i].Vertex, round);

                        hat.AppendLoop(loop);
                    } else
                    {
                        if (loop.Curves[0] is LineSegment2d)
                        {
                            List<pPos> pts = new List<pPos>();
                            ////ACD.WR("OK0");
                            foreach (Curve2d cv in loop.Curves)
                            {
                                pPos p = RoundValue(cv.StartPoint.ToPos2d(), round);
                                if(pts.Count == 0 || !p._isVeryClosed(pts.Last()))
                                    pts.Add(p);
                            }
                            ////ACD.WR("OK1");

                            Point2dCollection p2s = new Point2dCollection();
                            DoubleCollection p2d = new DoubleCollection();

                            foreach (pPos p in pts)
                            {
                                p2s.Add(p.ToPoint2());
                                p2d.Add(0);
                            }

                            p2s.Add(pts.First().ToPoint2());
                            p2d.Add(0);

                            hat.AppendLoop(loop.LoopType, p2s, p2d);
                        }
                        else if (loop.Curves[0] is  CircularArc2d)
                        {
                            CircularArc2d arc2d = loop.Curves[0] as CircularArc2d;
                            arc2d.Center = RoundValue(arc2d.Center, round);
                            arc2d.Radius = RoundValue(arc2d.Radius, round);
                        }
                        else if (loop.Curves[0] is EllipticalArc2d)
                        {
                            EllipticalArc2d ellipse2d = loop.Curves[0] as EllipticalArc2d;
                            ellipse2d.Center = RoundValue(ellipse2d.Center, round);
                            ellipse2d.MinorRadius = RoundValue(ellipse2d.MinorRadius, round);
                            ellipse2d.MajorRadius = RoundValue(ellipse2d.MajorRadius, round);
                        }
                        else if (loop.Curves[0] is NurbCurve2d)
                        {
                            NurbCurve2d spline2d = loop.Curves[0] as NurbCurve2d;

                            if (spline2d.HasFitData)
                            {
                                NurbCurve2dFitData n2fd = spline2d.FitData;
                                for (int i = 0; i < n2fd.FitPoints.Count; i++)
                                    n2fd.FitPoints[i] = RoundValue(n2fd.FitPoints[i], round);
                            }
                            else
                            {
                                NurbCurve2dData n2fd = spline2d.DefinitionData;
                                for (int i = 0; i < n2fd.ControlPoints.Count; i++)
                                    n2fd.ControlPoints[i] = RoundValue(n2fd.ControlPoints[i], round);
                            }

                            Curve2dCollection cvs = new Curve2dCollection();
                            IntegerCollection ints = new IntegerCollection();

                            cvs.Add(spline2d);
                            ints.Add((int)HatchEdgeType.Spline);
                            hat.AppendLoop(loop.LoopType, cvs, ints);
                        }
                    }

                hat.SetDatabaseDefaults();
                

                //ACD.WR("R3.1");
                hat.PatternScale = hatch_scale;
                //ACD.WR("R3.2");

                hat.HatchObjectType = HatchObjectType.HatchObject;
                hat.SetHatchPattern(hatch_type, hatch_pattern);

                try
                {
                    hat.EvaluateHatch(true);
                }catch(System.Exception ex)
                {
                    //ACD.WR("Error in evaluate hacth {0} stack {1}", id, ex.StackTrace);
                }

                tr.Commit();
            }
        }

        static void _roundInBlock(ObjectId blockId, int round)
        {
            using (Transaction tr = ACD.DB.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(ACD.DB.BlockTableId, OpenMode.ForWrite) as BlockTable;
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                BlockReference block = (BlockReference)tr.GetObject(blockId, OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(block.BlockTableRecord, OpenMode.ForWrite);

                ObjectIdCollection subIds = btr.Cast<ObjectId>().ToCollection();

                _roundIds(ACD.DB, subIds, round);
                btr.UpgradeOpen();

                tr.Commit();
            }
        }
        
        static void _runHtmlCommand(string[] contents)
        {
            foreach(string s in contents)
                if(s.st_("<div"))
                {
                    ObjectIdCollection ids = ACD.DB.strToObjectId(s._gValue("ids"));

                    foreach(ObjectId id in ids)
                        if(ACD.DB._isDoor(id))
                        {
                            ACD.DB._setDoorWidth(id, s._gValue("width").ToNumber());
                            ACD.DB._setDoorHeight(id, s._gValue("height").ToNumber());
                            ACD.DB._setDoorStyle(id, s._gValue("name"));
                        }
                }
        }

        //static bool _isMLine(Database db, ObjectId id)
        //{
        //    return id.ObjectClass.DxfName == "MLINE";
        //}

        static void _roundMLine(Database db, ObjectId id, int r)
        {
            using(Transaction tr = db.TransactionManager.StartTransaction())
            {
                Mline ml = (Mline)tr.GetObject(id, OpenMode.ForWrite);

                for(int i = 0; i < ml.NumberOfVertices; i ++)
                    ml.MoveVertexAt(i, ml.VertexAt(i).ToPos().Round(r).ToPoint3());

                tr.Commit();
            }
        }

        static string[] _roundIds(Database db, ObjectIdCollection selIds, int round, pPos basept = null)
        {
            List<string> ls = new List<string>();
            if (basept == null) basept = new pPos(0, 0);

            foreach (ObjectId id in selIds)
                try
                {
                    if (db._isMLine(id))
                    {
                        _roundMLine(db, id, round);
                    }
                    else if (db._isLine(id) || db._isPolyline(id))
                    {
                        string s = db._getLayer(id);
                        //if (!db.GetLinetypeText(id).empty())
                        //    s += ";" + db.GetLinetypeText(id).empty();

                        pPos[] pts = db._getPolylineVerts(id).Select(p => RoundValue(p, round)).ToArray();
                        db._setPolylineVerts(id, pts);

                        ls.Add("#LW=" + pts.Move(basept.Invert).ToText(db._isPolylineClosed(id)) + "[" + s + "]");
                    }
                    else if (db._isPoint(id))
                    {
                        pPos pt = RoundValue(db._getPoint(id), round);
                        db._setPoint(id, pt);

                        if (db._isCircle(id))
                        {
                            var new_radius = RoundValue(db._getRadius(id), round);

                            if(new_radius > 0)
                                db._setRadius(id,new_radius);
                        }
                        else if (db._isBlock(id))
                        {
                            string sname = db._getIdName(id);
                            if (db._isArray(id))
                            {
                                db._setPoint(id, RoundValue(db._getPoint(id), round));
                            }
                            else if (db._isGrid(id) || sname.st_("P_") || sname.st_("R_"))
                            {
                                //ObjectIdCollection subIds = db.GetEntInBlock(id);
                                ////ACD.WR("Ids {0}", subIds.Count);
                                _roundInBlock(id, round);
                            }
                            else
                            {
                                db._setPoint(id, RoundValue(db._getPoint(id), round));
                                ls.Add(String.Format("#INS={0}|Pos={1}|Rot={2}|Scale={3}",
                                    db._getIdName(id), pt - basept,
                                    db._getRotation(id).roundNumber(), db._getScale(id)));
                            }

                            db._setRotation(id, db._getRotation(id).roundNumber(1));
                        }

                        else if (db._isText(id))
                            ls.Add("#TXT=" + (pt - basept) + "[" + _replaceT(db._getContent(id)) + "]");
                    }
                    else if (db._isWall(id))
                    {
                        pPos p1 = RoundValue(db._getPoint(id, 0), round);
                        pPos p2 = RoundValue(db._getPoint(id, 1), round);

                        //ACD.WR("Wall {0},{1}", p1, p2);

                        db._setWallPoint(id, p1, p2);
                        ls.Add("#WALL=" + (p1 - basept) + ";" + (p2 - basept)
                            + "[" + _replaceT(db._getIdInfo(id)) + "]"
                            + "|Verts=" + (db._getBound(id).Move(basept.Invert).Rect().ToText()));
                    }
                    else if (db._isDim(id))
                    {
                        db._setPoint(id, RoundValue(db._getPoint(id, 0), round), 0);
                        //ACD.WR("Dim01");
                        db._setPoint(id, RoundValue(db._getPoint(id, 1), round), 1);
                        //ACD.WR("Dim02");
                        db._setPoint(id, RoundValue(db._getPoint(id, 2), round), 2);
                        //ACD.WR("Dim03");
                    }
                    else if (db._isHatch(id))
                    {
                        ////ACD.WR("Hatch");
                        _roundHatch(id, round);
                    }
                } catch(System.Exception ex)
                {
                    ACD.WR("Error in id {0} - {1}", id, id.ObjectClass.DxfName);
                    ACD.DB.CreateMLeader("ERROR",ACD.DB._getBound(id).CenterPoint(),
                        ACD.DB._getBound(id).CenterPoint() + new pPos(1000,1000));
                }

            return ls.ToArray();
        }

        static void _roundObjs(ObjectIdCollection ids, int round, int level = 0)
        {
            pPos basept = new pPos(0, 0);

            _roundIds(ACD.DB, ids, round, basept);

            foreach (ObjectId id in ACD.DB.FilterIds(ids, "AEC_DOOR", "AEC_WINDOW"))
            {
                ACD.DB._setDoorWidth(id, RoundValue(ACD.DB._getDoorWidth(id), round));
                ACD.DB._setDoorHeight(id, RoundValue(ACD.DB._getDoorHeight(id), round));

                pPos p = RoundValue(ACD.DB._getPoint(id), round);
                ACD.DB._setPoint(id, p);

                contents.Add((id.ObjectClass.DxfName == "AEC_DOOR" ? "#DR=" : "#DW=") + (p - basept)
                    + "[" + _replaceT(ACD.DB._getIdInfo(id)) + "]"
                    + "|Verts=" + (ACD.DB._getBound(id).Move(basept.Invert).Rect().ToText()));
            }

            foreach (ObjectId id in ACD.DB.FilterIds(ids, "INSERT"))
            {
                string s = ACD.DB._getIdName(id);

                if (s.st_("fur") || s.st_("grid") || s.ct_("u_"))
                    ACD.DB.BlockEntitiesEdit(id, _ids =>
                    {
                        _roundObjs(_ids, round, level + 1);
                    });
            }
        }

        static List<string> contents = new List<string>();

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                
                ObjectIdCollection selIds = ACD.GetSelection();
               
                if (selIds.Count > 0)
                {
                    int round = (int)ACD.ED.GetInputString("Enter round value", "50").ToNumber(50);

                    if (selIds.ToList().Any(id => ACD.DB._isDim(id)))
                    {
                        bool round_dim = ACD.ED.GetInputString("Only dimensions ?", "Y/N").ToBool();
                        if (round_dim)
                        {
                            selIds = selIds.ToList().Where(id => ACD.DB._isDim(id)).ToCollection();
                            ACD.DB._setLayer(selIds, DE.DEF_LAYER_DIM);
                        }
                    }

                    if (selIds.Count > 0)
                    {
                        _roundObjs(selIds, round);

                        if (contents.Count > 0)
                            System.Windows.Forms.Clipboard.SetText(contents.OrderBy(s => s._firstPropName()).ToTextStr("\r\n"));

                        if (File.Exists(@"D:\html\cmd.html"))
                            _runHtmlCommand(File.ReadAllLines(@"D:\html\cmd.html"));
                    }
                }

                ACD.ED.Regen();
                ACD.Focus();
            }
        }
    }
}

