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
    public class DimensionA //"A" means "ALL"
    {
        static ObjectId _createDim(pPos p1, pPos p2, double extent)
        {
            ObjectId res = ObjectId.Null;

            if (Math.Abs(p1.X - p2.X) < 10 || Math.Abs(p1.Y - p2.Y) < 10)
                res = IDimChain.CreateDimension(ACD.DB, p1, p2, extent, extent);
            else
                res = IDimChain.CreateAlignDimension(ACD.DB, p1, p2, extent, extent);

            return res;
        }

        static ObjectIdCollection _createDimList(IEnumerable<pPos> pts, double extent)
        {
            ObjectIdCollection res = new ObjectIdCollection();

            for (int i = 0; i < pts.Count() - 1; i++)
            {
                pPos p1 = pts.ElementAt(i);
                pPos p2 = pts.ElementAt(i + 1);

                res.Add(_createDim(p1, p2, extent));
            }

            return res;
        }

        static pPos[] _getWallShape(Database db, ObjectId entId)
        {
            List<pPos> res = new List<pPos>();

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
                        //List<pPos> res = new List<pPos>();
                        pPos p1 = wall.StartPoint.ToPos(), p2 = wall.EndPoint.ToPos();
                        pls = new pPos[] { p1, p2 };//.ExtentLine(extend);

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
                        //res = db.DrawPolyline(res, true);
                    }
                    else
                    {
                        res = ACD.DB._getBound(entId).ToList();
                        pPos p1 = wall.StartPoint.ToPos(), p2 = wall.MidPoint.ToPos(), p3 = wall.EndPoint.ToPos();
                        CircularArc3d carc = new CircularArc3d(wall.StartPoint, wall.MidPoint, wall.EndPoint);
                        pPos ct = carc.Center.ToPos();
                    }
                }

                tr.Commit();
            }

            //if (!resId.IsNull)
                //db._setPolylineClosed(resId, true);

            return res.ToArray();
        }

        static List<pPos> _addGridToList(int side)
        {
            List<pPos> res = new List<pPos>();
            double x1 = gridXY[0].First(), x2 = gridXY[0].Last(), y1 = gridXY[1].First(), y2 = gridXY[1].Last();

            if (side == 0)
            {
                //foreach (pPos p in allPts) p.X = x1;
                res.AddRange(gridXY[1].Select(v => new pPos(x1, v)));
            }
            else if (side == 1)
            {
                //foreach (pPos p in allPts) p.Y = y1;
                res.AddRange(gridXY[0].Select(v => new pPos(v, y1)));
            }
            else if (side == 2)
            {
                //foreach (pPos p in allPts) p.X = x2;
                res.AddRange(gridXY[1].Select(v => new pPos(x2, v)));
            }
            else
            {
                //foreach (pPos p in allPts) p.Y = y2;
                res.AddRange(gridXY[0].Select(v => new pPos(v, y2)));
            }

            return res;
        }

        static ObjectIdCollection DimGrid(int side)
        {
            //SIDE: 0 Left, 1 Top, 2 Right, 3 Bottom
            ObjectIdCollection resIds = new ObjectIdCollection();
            double x1 = gridXY[0].First(), x2 = gridXY[0].Last(), y1 = gridXY[1].First(), y2 = gridXY[1].Last();

            List<pPos> allPts = _addGridToList(side);

            allPts = allPts.Select(p
                => p.Round(50).ToString()).Distinct().Select(s => pPos.FromString(s))
                .OrderBy(p => p.X)
                .ThenBy(p => p.Y).ToList();

            if (side > 1)
                allPts.Reverse();

            double sp = dim_spacing;

            if (side > 1)
                sp = -sp;

            resIds = DE.NumericArray(0, allPts.Count - 2).Select(i =>
                    IDimChain.CreateDimension(ACD.DB, allPts[i], allPts[i + 1],
                    sp * 1.25, sp * 1.25)).ToCollection();

            resIds.Add(IDimChain.CreateDimension(ACD.DB, allPts.First(), allPts.Last(),
                    sp * 1.5, sp * 1.5));

            return resIds;
        }

        static ObjectIdCollection DimAll(PosCollection pls, double range, int side)
        {
            //SIDE: 0 Left, 1 Top, 2 Right, 3 Bottom
            ObjectIdCollection resIds = new ObjectIdCollection();

            List<pPos> allPts = pls.AllPoints.ToList();
            //allPts.AddRange(pls.SelfIntersect);

            pPos[] bb = pls.Boundary;
            
            double x1 = gridXY[0].First(), x2 = gridXY[0].Last(), y1 = gridXY[1].First(), y2 = gridXY[1].Last();
            
            pPos[] region = new pPos[] { new pPos(x1 - range, y1 - range), new pPos(x2 + range, y2 + range) };
            //ACD.DB.DrawPolyline(region);

            if (side == 0)
                region[1].X = x1 + range;
            else if (side == 1)
                region[0].Y = y2 - range;
            else if (side == 2)
                region[0].X = x2 - range;
            else
                region[1].Y = y1 + range;

            //ACD.DB.DrawPolyline(region);

            //ACD.WR("OK1");

            allPts = allPts.Where(pt => pt.InsideRect(region[0], region[1])).ToList();

            if (side == 0)
            {
                foreach (pPos p in allPts) p.X = x1;
                allPts.AddRange(gridXY[1].Select(v => new pPos(x1, v)));
            }
            else if (side == 1)
            {
                foreach (pPos p in allPts) p.Y = y1;
                allPts.AddRange(gridXY[0].Select(v => new pPos(v, y1)));
            }
            else if (side == 2)
            {
                foreach (pPos p in allPts) p.X = x2;
                allPts.AddRange(gridXY[1].Select(v => new pPos(x2, v)));
            }
            else
            {
                foreach (pPos p in allPts) p.Y = y2;
                allPts.AddRange(gridXY[0].Select(v => new pPos(v, y2)));
            }

            allPts.AddRange(_addGridToList(side));
            allPts = allPts.Select(p
                => p.Round(50).ToString()).Distinct().Select(s => pPos.FromString(s)).ToList();

            if (side % 2 == 0)
                allPts = allPts.OrderBy(p => p.Y).ToList();
            else
                allPts = allPts.OrderBy(p => p.X).ToList();

            double sp = dim_spacing;

            if (side > 1)
                sp = -sp;

            resIds = DE.NumericArray(0, allPts.Count - 2).Select(i =>
                    IDimChain.CreateDimension(ACD.DB, allPts[i], allPts[i + 1], 
                    sp, sp)).ToCollection();

            resIds.AddRange(DimGrid(side));

            return resIds;// _newBlock(resIds, new pPos(x1, y1));
        }

        static ObjectIdCollection DimColumnGrids(PosCollection pls)
        {
            ObjectIdCollection resIds = new ObjectIdCollection();

            PosCollection _pls = DE.NumericArray(0, pls.Count - 1)
                .Where(i => pls[i].Length >=4 && pls[i].Size().X <= 5000 && pls[i].Size().Y <= 5000)
                .Select(i => pls[i]).ToCollectionSameClosed(true);

            //ACD.WR("Colums {0}",_pls.Count);

            foreach(pPos[] ls in _pls)
            {
                pPos[] bb = ls.Boundary();

                resIds.Add(IDimChain.CreateDimension(ACD.DB, 
                    new pPos(bb[0].X, bb[0].Y), new pPos(bb[1].X, bb[0].Y), dim_spacing, dim_spacing));

                resIds.Add(IDimChain.CreateDimension(ACD.DB,
                    new pPos(bb[0].X, bb[0].Y), new pPos(bb[0].X, bb[1].Y), dim_spacing, dim_spacing));
            }

            return resIds;
        }

        static void DimRegions(PosCollection pls) //PHÂN LÔ PHÂN NỀN
        {
            pPos[] bb = pls.Boundary;

            ACD.DB.GetEntities(bb, EN_SELECT.AC_DXF, "AEC_WALL", "INSERT", "LWPOLYLINE");

            ObjectId blockContainerId = ObjectId.Null;

            double extent = allXNotes._props("Dim.Extent").ToNumber();
            bool ortho = allXNotes._props("Dim.Ortho").ToBool();
            int axis = -1;

            PosCollection segments = pls.GetSegment();
            List<pPos> verts = pls.AllPoints.ToList();
            verts.AddRange(pls.SelfIntersect);

            while (true)
            {
                pPos pt = ACD.GetPoint();

                if (pt == null) break;

                pPos[] near_verts = verts.Where(p => p._isVeryClosed(pt, 2)).ToArray();

                if (near_verts.Length == 0)
                {
                    PosCollection segs = segments.Where(ls
                            => pt.DistanceTo(ls[0], ls[1]) < 10 && pt.IsBetween(ls[0], ls[1]))
                            .OrderBy(ls => pt.DistanceTo(ls[0], ls[1]))
                            .ToCollectionSameClosed(false);

                    if (segs.Count > 0)
                    {
                        pPos p1 = segs[0][0], p2 = segs[0][1];

                        if (ortho)
                        {
                            if (axis == -1)
                                axis = Math.Abs(p1.X - p2.X) < 0.1 ? 1 : 0;

                            p1[(axis + 1) % 2] = p2[(axis + 1) % 2];
                        }

                        _createDim(p1, p2, extent);
                    }
                }
                else
                {
                    pPos last_pt = near_verts.First();

                    while (true)
                    {
                        pt = ACD.GetPoint();
                        if (pt == null) return;

                        if (ortho && axis != -1)
                            near_verts = verts.Where(p => Math.Abs(p[axis] - pt[axis]) < 1).ToArray();
                        else
                            near_verts = verts.Where(p => p._isVeryClosed(pt, 2)).ToArray();

                        if (near_verts.Length > 0)
                        {
                            pPos p1 = near_verts.First(), p2 = last_pt;

                            if (ortho)
                            {
                                if (axis == -1)
                                    axis = Math.Abs(p1.X - p2.X) < 0.1 ? 1 : 0;

                                p1[(axis + 1) % 2] = p2[(axis + 1) % 2];
                            }

                            _createDim(p1, p2, extent);
                            last_pt = near_verts.First();
                        }
                    }
                }
            }
        }

        static List<string> allXNotes;
        static double dim_spacing = 0;
        static PosCollection gridPls;
        static List<double[]> gridXY;

        static void _updateCurrentDimGroup(ObjectIdCollection ids, List<double[]> xys, double tole = 200)
        {
            //ACD.WR("OK1");
            ObjectIdCollection dimIds = ids.ToList().Where(id => ACD.DB._isBlock(id) && ACD.DB._getIdName(id).st_("dim")).ToCollection();
            //ACD.WR("OK2");
            foreach (ObjectId dId in dimIds)
            {
                ACD.DB.BlockEntitiesAction(dId, (_ids) =>
                {
                    foreach (ObjectId id in _ids)
                        if (ACD.DB._isDim(id))
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                pPos pt = ACD.DB._getPoint(id, i);
                                //ACD.WR("OK3");
                                for (int ax = 0; ax < 2; ax++)

                                    foreach (double v in xys[ax])
                                    {
                                        if (Math.Abs(v - pt[ax]) < tole)
                                        {
                                            pt[ax] = v;
                                        }
                                    }

                                ACD.DB._setPoint(id, pt, i);
                            }
                        }
                    //ACD.WR("OK4");
                    ACD.DB.NewBlock(_ids,ACD.DB._getIdName(dId),true,true,ACD.DB._getPoint(dId));
                });
                ACD.DB.EraseObject(dId);
            }
        }

        static ObjectIdCollection _dimDefpoints(ObjectId blockId, double extent)
        {
            ObjectIdCollection res = new ObjectIdCollection();

            ACD.DB.BlockEntitiesAction(blockId, ids =>
            {
                //ACD.WR("OK0");
                PosCollection pls = ids.ToList().Where(id => ACD.DB._getLayer(id).st_("Defpoints"))
                    .Select(id => ACD.DB._getVertices(id, 16))
                    .ToCollectionClosedList(ids.ToList().Select(id => ACD.DB._isPolylineClosed(id)));

                List<pPos> blockPts = ids.ToList().Where(id => ACD.DB._isBlock(id))
                    .Select(id => ACD.DB._getPoint(id)).ToList();
                //ACD.WR("OK1");
                for (int spl = 0; spl < pls.Count; spl++)
                {
                    pPos[] pts = pls[spl];
                    List<pPos> newSpl = new List<pPos>();

                    for (int i = 0; i < pts.Length; i++)
                    {
                        int nex = i + 1;
                        if (i >= pts.Length - 1 && pls.Closed[i])
                            nex = 0;
                        else if (i >= pts.Length - 1)
                            break;
                        //ACD.WR("OK2");
                        pPos[] points = blockPts.Where(p =>
                        {
                            pPos prj = p.Project2D(pts[i],pts[nex]);
                            return p._isVeryClosed(prj, 200) && prj.IsBetween(pts[i], pts[nex]);
                        }).ToArray();
                        //ACD.WR("OK3");

                        if (points.Length > 0)
                        {
                            //newSpl.Add(pts[i]);
                            newSpl.AddRange(points.OrderBy(p => p.DistanceTo(pts[i])));
                            //newSpl.Add(pts[nex]);
                        }
                    }
                    //ACD.WR("OK4");
                    newSpl = new PosCollection(newSpl.Select(p => p.Round(10).ToString()).Distinct().ToTextStr(";")).First().ToList();
                    res.AddRange(_createDimList(newSpl, extent));
                    //ACD.WR("OK5");
                }
            });

            return res;
        }


        static void _dimCrossLines(PosCollection pls)
        {
            pPos[] npts = pls.SelfIntersect;

            for (int i = 0; i < pls.Count; i++)
            {
                List<pPos> cross = new List<pPos>();

                for (int j = 0; j < pls[i].Length - 1; j++)
                    foreach (pPos p in npts)
                        if (p.IsBetween(pls[i][j], pls[i][j + 1]))
                            cross.Add(p);

                if (cross.Count < 2 && pls[i].Length == 2)
                    IDimChain.CreateAlignDimension(ACD.DB, pls[i][0], pls[i][1], 0, 0); 
                else if (cross.Count >= 2)
                    for (int j = 0; j < cross.Count - 1; j++)
                        IDimChain.CreateAlignDimension(ACD.DB, cross[j], cross[j + 1], 0, 0); 
            }
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    int mode = (int)ACD.ED.GetInputString("Input mode ?(0.Grid/1.Region/2.ColsPlan/3.All/4.Update/5.Line)").ToNumber();
                    if (mode == 5)
                    {
                        dim_spacing = 0;
                        //-ACD.ED.GetInputString("Enter spacing (1000)", "1000").ToNumber(1000);

                        //ObjectIdCollection ids = ACD.GetSelection();
                        PosCollection __pls = new PosCollection();
                        foreach(ObjectId _id in selIds)
                        {
                            pPos[] pts = ACD.DB._getVertices(_id, 16);
                            if(pts.Length > 0)
                                __pls.Add(pts);
                            
                        }

                        _dimCrossLines(__pls);
                    }

                    allXNotes = new List<string>();

                    PosCollection pls = new PosCollection();

                    pls.Add(new pPos[0]);
                    pls.Closed = new bool[] { true };

                    //gridPls = new PosCollection();

                    foreach (ObjectId objId in selIds)
                        allXNotes.AddRange(ACD.DB.GetXNotes(objId));

                    allXNotes.Reverse();

                    foreach (ObjectId objId in selIds)
                        if (ACD.DB._isBlock(objId))
                        {
                            if(mode == 5)
                                _dimDefpoints(objId, dim_spacing);

                            ACD.DB.BlockEntitiesAction(objId, (_ids) =>
                            {
                                foreach (ObjectId _id in _ids)
                                    if (ACD.DB._isVertice(_id))
                                    {
                                        pPos[] _ls = ACD.DB._getVertices(_id);
                                        pls.Add(_ls);
                                        pls.Closed = pls.Closed.Add(ACD.DB._isPolylineClosed(objId));

                                        //if(_isGrid(objId) && !ACD.DB._isPolylineClosed(objId) 
                                        //    && (Math.Abs(_ls[0].X - _ls[1].X) < 1000 
                                        //    || Math.Abs(_ls[0].Y - _ls[1].Y) < 1000))
                                        //{
                                        //    gridPls.Add(_ls);
                                        //    gridPls.Closed = gridPls.Closed.Add(false);
                                        //}
                                    }
                                    else if (ACD.DB._isBlock(_id))
                                    {
                                        pls.Add(ACD.DB._getBound(_id).Rect());
                                    }
                                
                            });
                        }
                        else if (ACD.DB._isWall(objId))
                        {
                            pls.Add(_getWallShape(ACD.DB, objId));
                            pls.Closed = pls.Closed.Add(true);


                        }

                    //foreach (pPos pt in pls.AllPoints)
                    //    ACD.DB.DrawCircle(pt, 0.5);

                    
                                        
                    gridXY = ACD.DB.GetGridXY(selIds);
                    //ACD.WR("Grid items X:{0} Y:{1}", gridXY[0].ToTextDouble(","), gridXY[1].ToTextDouble(","));

                    if (pls.Count > 0)
                    {
                        switch(mode)
                        {
                            case 0:
                                dim_spacing = -ACD.ED.GetInputString("Enter spacing (2000)", "2000").ToNumber(2000);
                                for (int i = 0; i <4; i++) DimGrid(i);break;
                            case 1: DimRegions(pls);break;
                            case 2:
                                dim_spacing = -ACD.ED.GetInputString("Enter spacing (500)", "500").ToNumber(500);
                                DimColumnGrids(pls); break;
                            case 3:
                                dim_spacing = -ACD.ED.GetInputString("Enter spacing (2000)", "2000").ToNumber(2000);
                                if (gridPls.Count > 0)
                                {
                                    int _dim_size = (int)ACD.ED.GetInputString("Enter side ?(0.Left/1.Bottom/2.Right/3.Top)").ToNumber();
                                    double _dim_range = ACD.ED.GetInputString("Enter range (0.2)", "0.2").ToNumber();
                                    if (_dim_range < 1)
                                        _dim_range *= Math.Max(pls.Size.X, pls.Size.Y);

                                    DimAll(pls, _dim_range, _dim_size);
                                }
                                else
                                    MessageBox.Show("No grid object!");
                                break;
                            case 4:
                                _updateCurrentDimGroup(selIds, gridXY);
                                break;
                            case 5:

                                break;
                        }

                    }
                }

                ACD.Focus();

            }
        }
    }
}
