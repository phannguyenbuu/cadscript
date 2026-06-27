using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AEC = Autodesk.Aec.Arch.DatabaseServices;
using Autodesk.AutoCAD.Windows.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;



namespace AcadScript
{
    using Chains = List<List<double>>;
    public static class ISRImage
    {
        public static string[] ExtractThumbnails(this Database db, ObjectIdCollection ids, string iconPath)
        {
            List<string> res = new List<string>();
            foreach (ObjectId id in ids)
            {
                string st = db.ExtractThumbnails(id, iconPath);
                if (!st.empty()) res.Add(st);
            }
            return res.ToArray();
        }


        public static string ExtractThumbnails(this Database db, ObjectId blockId, string iconPath)
    {
        string fname = null;
        using (var tr = db.TransactionManager.StartTransaction())
        {
            System.Windows.Media.ImageSource imgsrc = null;
            if (db._isBlock(blockId))
            {
                BlockReference block = (BlockReference)tr.GetObject(blockId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(block.BlockTableRecord, OpenMode.ForRead);
                if (!btr.IsLayout || !btr.IsAnonymous)
                    imgsrc = CMLContentSearchPreviews.GetBlockTRThumbnail(btr);
            }
            else if (blockId._isDim())
            {
                Dimension dim = (Dimension)tr.GetObject(blockId, OpenMode.ForRead);

                DimStyleTableRecord dt = (DimStyleTableRecord)tr.GetObject(dim.DimensionStyle, OpenMode.ForRead);
                //DimStyleTableRecord dtr = (DimStyleTableRecord)tr.GetObject(dt.id, OpenMode.ForRead);

                //ACD.WR("DIM_STYLE {0}", dt);
                imgsrc = CMLContentSearchPreviews.GetDimStyleThumbnail(dt);
            }

            if (imgsrc != null)
            {
                var bmp = ImageSourceToGDI(imgsrc as System.Windows.Media.Imaging.BitmapSource);
                fname = iconPath + "\\" + blockId.Handle.Value + ".bmp";

                if (File.Exists(fname)) File.Delete(fname);
                bmp.Save(fname);
            }

            tr.Commit();
        }

        return fname;
    }

        //Helper function to generate an Image from a BitmapSource

        private static System.Drawing.Image ImageSourceToGDI(System.Windows.Media.Imaging.BitmapSource src)
        {
            var ms = new MemoryStream();
            var encoder = new System.Windows.Media.Imaging.BmpBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(src));
            encoder.Save(ms);
            ms.Flush();
            return System.Drawing.Image.FromStream(ms);
        }
    }

    public static class WallInfoCLSExt
    {

        public static PosCollection _getWallOpeningPos(this Database db, ObjectId entId)
        {
            PosCollection res = new PosCollection();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (entId.ObjectClass.DxfName == "AEC_WALL")
                {
                    Autodesk.Aec.Arch.DatabaseServices.Wall wall
                        = (Autodesk.Aec.Arch.DatabaseServices.Wall)tr.GetObject(entId, OpenMode.ForRead);
                    if (wall != null)
                    {
                        ObjectIdCollection ids = wall.GetOpeningsFor();
                        double w = wall.Width;
                        foreach (ObjectId id in ids)
                        {
                            List<pPos> ls = db._getVertices(id).ToList();

                            if (ls.Count > 0)
                            {
                                //ls.AddRange(ls[0].Parallel(ls[1], w).Reverse());
                                res.Add(ls.ToArray());
                            }
                        }
                    }
                }
                tr.Commit();
            }
            return res;
        }

        public static double _getWallWidth(this Database db, ObjectId entId)
        {
            double res = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (entId.ObjectClass.DxfName == "AEC_WALL")
                {
                    Autodesk.Aec.Arch.DatabaseServices.Wall wall
                        = (Autodesk.Aec.Arch.DatabaseServices.Wall)tr.GetObject(entId, OpenMode.ForRead);
                    if (wall != null) res = wall.Width;
                }
                tr.Commit();
            }
            return res;
        }

        public static double _getWallHeight(this Database db, ObjectId entId)
        {
            double res = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (entId.ObjectClass.DxfName == "AEC_WALL")
                {
                    Autodesk.Aec.Arch.DatabaseServices.Wall wall
                        = (Autodesk.Aec.Arch.DatabaseServices.Wall)tr.GetObject(entId, OpenMode.ForRead);
                    if (wall != null) res = wall.BaseHeight;
                }
                tr.Commit();
            }
            return res;
        }
    }

    public static class IR //Read objects in region - Main method: GetEntities
    {
        public static EN_SELECT SelectMode;
        public static ObjectIdCollection _selectedIds;
        public static Database curDB;

        public static bool GridFromPoint_Inside;

        public static List<pPos> gridTxts;

        public static pPos[] GetBlockContents(this Database db, ObjectIdCollection selIds)
        {
            List<pPos> res = new List<pPos>();

            foreach (ObjectId objId in selIds)
                if (ACD.DB._isBlock(objId))
                {
                    ACD.DB.BlockEntitiesAction(objId, (_ids) =>
                    {
                        foreach (ObjectId _id in _ids)
                            if (ACD.DB._isText(_id))
                            {
                                res.Add(ACD.DB._getPoint(_id));
                            }
                    });
                }

            res = res.OrderBy(p => p.X.roundNumber(50)).ThenBy(p => p.Y).ToList();

            return res.ToArray();
        }

        static void _readGetGridXY(this Database db, ObjectId objId, pPos[] limit_distance_zone = null)
        {
            if (db._isVertice(objId))
            {
                pPos[] _ls = db._getVertices(objId);

                if (limit_distance_zone != null)
                    _ls = _ls.Where(p => p.Inside(limit_distance_zone)).ToArray();

                GetGridXY_result.Add(_ls);
                GetGridXY_result.Closed = GetGridXY_result.Closed.Add(false);

            }
            else if (ACD.DB._isText(objId))
            {
                pPos p = ACD.DB._getPoint(objId);
                if (limit_distance_zone == null || p.Inside(limit_distance_zone))
                    gridTxts.Add(p);
            }
            else if (ACD.DB._isBlock(objId))
                ACD.DB.BlockEntitiesAction(objId, _ids => { foreach (ObjectId _id in _ids) db._readGetGridXY(_id); });
 
        }

        static PosCollection GetGridXY_result;

        public static List<double[]> GetGridXY(this Database db, ObjectIdCollection selIds, 
            double round = 50, double range = 1000, pPos[] limit_distance_zone = null)
        {
            GetGridXY_result = new PosCollection();
            gridTxts = new List<pPos>();
            
            foreach (ObjectId id in selIds)
            {
                //ACD.WR("Id {0}", id.ObjectClass.DxfName);
                db._readGetGridXY(id);
            }

            gridTxts = gridTxts.OrderBy(p => p.X.roundNumber(50)).ThenBy(p => p.Y).ToList();

            return GetGridXY_result.Count > 0 ? GetGridXY_result.ExtractPtsXY(round, range) : new List<double[]>();
        }
        
        public static string GetXYName(int axis, double v, double tole = 100)
        {
            string res = null;

            foreach(pPos p in gridTxts)
            {
                if (Math.Abs(p[axis] - v) <= tole)
                {
                    res = p.Content;
                    break;
                }
            }

            return res;
        }


        public static void GetEntities(this Database db, IEnumerable<pPos> _region,
                    EN_SELECT mode, params string[] lscodes)
        {
            curDB = db;

            string[] codes = lscodes.Select(st => st.ToUpper()).ToArray();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                _selectedIds = new ObjectIdCollection();

                _selectedIds = btr.Cast<ObjectId>().Where(id => (db.ValidId(id)
                    && (codes == null || _isNodeSelection(db, id, mode, codes))
                    && (_region == null || db.Inside(id, _region)))).ToCollection();

                tr.Commit();
            }
        }
        
        public static ObjectIdCollection _getAllGrids(this Database db)
        {
            db.GetEntities(null, EN_SELECT.AC_DXF, "INSERT");
            return IR.SelectedIds.Cast<ObjectId>().Where(id => db._isGrid(id)).Select(id => id)
                .OrderBy(id => db._getPoint(id).Y).ThenBy(id => db._getPoint(id).X).ToCollection();
        }
        public static ObjectIdCollection _getAllGridsByIds(this Database db, ObjectIdCollection ids)
        {
            return ids.Cast<ObjectId>().Where(id => db._isGrid(id)).Select(id => id)
                .OrderBy(id => db._getPoint(id).Y).ThenBy(id => db._getPoint(id).X).ToCollection();
        }

        public static ObjectId GridFromPoint(this Database db, pPos pt)
        {
            ObjectId res = ObjectId.Null;
            ObjectIdCollection gridIds = db._getAllGrids();

            if (gridIds.Count == 0)
            {
                //ACD.WR("GridFromPoint >> Don't find any grids!");
                return res;
            }

            int index = gridIds.FindIndex(id => pt.Inside(db._getBound(id).Rect()));

            if (index != -1)
            {
                GridFromPoint_Inside = true;
                res = gridIds[index];
            }
            else
            {
                GridFromPoint_Inside = false;
                gridIds = gridIds.ToList().OrderBy(id =>
                    pt.DistanceTo(db._getBound(id).CenterPoint())).ToCollection();
                res = gridIds.First();
            }
            return res;
        }
        public static string[] SelectedTitles
        {
            get
            {
                List<KeyValuePair<string, pPos>> res = new List<KeyValuePair<string, pPos>>();

                using (Transaction tr = curDB.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId objId in _selectedIds)
                    {
                        if (objId.ObjectClass.DxfName == "MTEXT")
                        {
                            MText mtxt = (MText)tr.GetObject(objId, OpenMode.ForWrite);
                            res.Add(new KeyValuePair<string, pPos>(mtxt.Contents, curDB._getPoint(objId)));
                        }
                        else if (objId.ObjectClass.DxfName == "TEXT")
                        {
                            DBText txt = (DBText)tr.GetObject(objId, OpenMode.ForWrite);
                            res.Add(new KeyValuePair<string, pPos>(txt.TextString, curDB._getPoint(objId)));
                        }
                    }
                }

                res = res.OrderBy(itm => itm.Value.Y).ThenBy(itm => itm.Value.X).ToList();

                return (from itm in res select itm.Key).ToArray();
            }
        }

        public static ObjectIdCollection FilterIds(this Database db, Func<Database, ObjectId, bool> fn)
        {
            return db.FilterIds(_selectedIds, fn);
        }



        public static ObjectIdCollection SelectedIds
        {
            get
            {
                ObjectIdCollection res = new ObjectIdCollection();
                if (_selectedIds != null)
                    res = _selectedIds.Cast<ObjectId>().Where(id => curDB.ValidId(id)).Select(id => id).ToCollection();
                return res;
            }
        }
        static bool _isNodeSelection(this Database db, ObjectId id, EN_SELECT mode, string[] codes)
        {
            if (!db._isVisible(id))
                return false;

            if (mode == EN_SELECT.AC_ALL)
                return true;

            if (codes == null || codes.Length == 0)
                return true;

            if (mode < EN_SELECT.AC_NAME && !codes.Contains(id.ObjectClass.DxfName))
                return false;

            //if (mode == EN_SELECT.AC_HYPERLINK || mode == EN_SELECT.AC_DXF_AND_HYPERLINK)
            //    return codes.Contains(db._getHyperlink(id).Upper());

            if (mode == EN_SELECT.AC_DXF_AND_NAME || mode == EN_SELECT.AC_NAME)
            {
                string stname = db._getIdName(id);
                if (stname == null)
                    return false;
                else
                    return codes.Contains(stname.ToUpper());
            }

            return true;
        }

        public static bool PolylineClosed;

        public static bool Inside(this Database db, ObjectId id, IEnumerable<pPos> region)
        {
            bool res = false;
            pPos[] bb1 = db._getBound(id);

            if (db._isText(id) || db._isCircle(id) || db._isElip(id))
            {
                pPos pt = db._getPoint(id);
                res = region.Count() == 2 ?
                    pt.InsideRect(region.ElementAt(0), region.ElementAt(1)) : pt.Inside(region);

                if (!res)
                    res = pt.DistanceToPts(region) <= 5;
            }
            else if (bb1 != null)
            {
                List<pPos> bb = db._getBound(id).ToList();

                if (db._isBlock(id) && bb.IntersectBounding(region))
                {
                    //pPos[] new_reg = region.Move(db._getPoint(id).Invert);
                    res = true;// db.GetEntInBlock(id).ToList().Any(sid => db.Inside(sid, new_reg));
                }
                else
                {
                    if (db._isDim(id))
                    {
                        bb = db._getVertices(id).ToList();
                        pPos[] pts = db._getDimPoints(id);
                        if (pts != null)
                            bb.AddRange(bb);
                    }
                    else if (db._isPolyline(id) || db._isWall(id) || db._isDoor(id) || db._isLine(id))
                    {
                        bb = db._getVertices(id).ToList();
                    }

                    pPos[] reg_bb = region.Boundary();

                    int n = 1000;
                    if (bb.Select(p => (p / n).Round(1)).Any(p
                        => p.InsideRect((reg_bb[0] / n).Round(1), (reg_bb[1] / n).Round(1))))
                            res = bb.Any(p => p.Inside(region));
                }
            }

            return res;
        }




















        //---------------------------------------------------------------------------------------

        public static string REMOVE_KEY = "$Removal";
        public static string[] AC_REGION_KEYS = new string[] { "BDEF", "LEVEL", "REGION" };
        static List<string> DxfPolyline = new List<string> { "LWPOLYLINE", "CIRCLE", "ELLIPSE" };
        public static List<string> DxfDoor = new List<string> { "AEC_DOOR", "AEC_WINDOW" };

        

        
        public static string[] titles_in_object;


        public static ObjectIdCollection _getAllTitlesByBound(this Database db, pPos[] bb)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            db.GetEntities(bb, EN_SELECT.AC_DXF, "MTEXT", "TEXT");

            int index = IR.SelectedIds.FindIndex(id => db._getContent(id).Contains("#TITLE="));

            if (index != -1)
            {
                res = new ObjectIdCollection { IR.SelectedIds[index] };
            }
            else
            {
                res = IR.SelectedIds.Cast<ObjectId>()
                    .Select(id => id).OrderBy(id => db._getPoint(id).Y)
                    .ThenBy(id => db._getPoint(id).X).ToCollection();
            }
            return res;
        }

        public static ObjectIdCollection _getAllTitles(this Database db)
        {
            db.GetEntities(null, EN_SELECT.AC_DXF, "MTEXT", "TEXT");

            ObjectIdCollection res = IR.SelectedIds.Cast<ObjectId>()
                .Where(id => db._getContent(id) == db._getContent(id))
                .Select(id => id)
                .OrderBy(id => db._getPoint(id).Y).ThenBy(id => db._getPoint(id).X).ToCollection();

            return res;
        }

        public static ObjectId object_current_Id;

        public static pPos[] _findInternalRect(this Database db, IEnumerable<pPos> region)
        {
            pPos[] res = null;
            //double region_area = Pts.Area();

            db.GetEntities(region.Offset(-10), EN_SELECT.AC_DXF, "LWPOLYLINE", "LINE");
            ObjectIdCollection lwpIds = IR.SelectedIds.Cast<ObjectId>()
                .Where(id => db._isPolylineClosed(id)
                    && Math.Abs(db._getVertices(id).First()._getOffsetValue(region)) >= 300)
                .OrderBy(id => -db._getVertices(id).Area()).ToCollection();

            //ACD.WR("IDS {0} SEL_IDS {1}", SelectedIds.Count, lwpIds.Count);

            if (lwpIds.Count > 0)
            {
                object_current_Id = lwpIds.First();
                res = db._getVertices(object_current_Id);
            }
            return res;
        }

        public static pPos _findBasepoint(this Database db, IEnumerable<pPos> region)
        {
            pPos res = null;
            pPos ct = region.CenterPoint();
            db.GetEntities(region, EN_SELECT.AC_DXF_AND_NAME, "INSERT", "BASEPOINT");

            ObjectIdCollection blockIds = IR.SelectedIds.Cast<ObjectId>()
                .OrderBy(id => db._getPoint(id).DistanceTo(ct)).ToCollection();

            if (blockIds.Count > 0)
            {
                object_current_Id = blockIds.First();
                res = db._getPoint(object_current_Id);
            }
            return res;
        }


        /*static ObjectId _findIdInside(this Database db, ObjectIdCollection ids, IEnumerable<pPos> region)
        {
            ObjectId res = ObjectId.Null;
            //ObjectIdCollection selIds = ids.Cast<ObjectId>()
                    //.OrderBy(id => db._getPoint(id).Y.roundNumber(10))
                    //.ThenBy(id => db._getPoint(id).X).ToCollection();
            int index = ids.FindIndex(id => db._getPoint(id).Inside(region));
            if (index != -1)
                res = ids[index];
            return res;
        }*/

        public static pPos[] _getPointParams(this Database db, ObjectId id, double dist)
        {
            List<pPos> res = new List<pPos>();
            pPos[] pts = db._getVertices(id);
            if (pts.Length > 0)
            {
                res.Add(pts.First());
                pPos[] tmp = pts.Divide(dist, false);
                for (int i = 1; i < tmp.Length - 1; i++)
                    if (!tmp[i].IsOnLine(tmp[i - 1], tmp[i + 1])) res.Add(tmp[i]);
                res.Add(pts.Last());
            }

            return res.ToArray();
        }

        //public static int _keyCounter(this Database db, string key)
        //{
        //    db.GetEntities(null, EN_SELECT.AC_DXF, "TEXT", "MTEXT", "MLEADER");
        //    ObjectIdCollection selIds = SelectedIds;
        //    int res = 0;

        //    foreach (ObjectId id in SelectedIds)
        //    {
        //        string st = db._getContent(id);
        //        if (st._hasContent() && st.StartsWith(key))
        //        {
        //            int num = (int)st.Substring(key.Length + 1).Trim().ToNumber(int.MinValue);
        //            if (num > res) res = num;
        //        }
        //    }
        //    return res;
        //}

        public static pPos[] _isLayout(this Database db, ObjectId id)
        {
            pPos[] res = null;

            if (db._isPolyline(id) && !db._isPolylineClosed(id))
            {
                pPos[] pts = db._getVertices(id);

                if (pts.Length == 5)
                {
                    bool b = true;
                    for (int i = 1; i < 4; i++)
                        if (Math.Abs(Math.Cos(pts[i].AngleVector(pts[i - 1], pts[i + 1]) * Math.PI / 180)) > 0.1)
                        {
                            b = false;
                            break;
                        }

                    if (b)
                    {
                        pPos pt = pts[0].Intersect(pts[1], pts[3], pts[4]);
                        if (pt != null && pt.DistanceTo(pts.First()) > 90 && pt.DistanceTo(pts.Last()) > 90)
                            res = Math.Abs(pt.Y - pts[1].Y) > 1 ? new pPos[] { pt, pts[1], pts[2], pts[3] }
                            : new pPos[] { pt, pts[3], pts[2], pts[1] };
                    }
                }
            }

            return res;
        }


        public static pPos[] GetPlineAroundPoint(this Database db, pPos pt, double search_range = 10000)
        {
            db.GetEntities((pt - new pPos(search_range, search_range)).RectToPoint(pt + new pPos(search_range, search_range)),
                EN_SELECT.AC_DXF, "LWPOLYLINE");

            ObjectIdCollection srcIds = IR.SelectedIds;

            //ACD.WR("Selection {0}", srcIds.Count);

            ObjectId selId = ObjectId.Null;
            double min_value = double.PositiveInfinity;

            foreach (ObjectId id in srcIds)
                if (db._isPolylineClosed(id))
                {
                    pPos[] pts = db._getVertices(id);
                    double n = pt.DistanceToPts(pts);
                    if (min_value > n)
                    {
                        min_value = n;
                        selId = id;
                    }
                }

            pPos[] res = new pPos[0];

            if (!selId.IsNull)
            {
                pPos[] pts = db._getVertices(selId);
                if (pt.Inside(pts)) res = pts;
            }

            return res;
        }

        //public static void GetEntities(string fname, pPos[] region, EN_SELECT mode, params string[] lscodes)
        //{
        //    curDB = ACD.ReadDWG(fname);
        //    _selectedIds = new ObjectIdCollection();
        //    blocks_in_object = new List<pPosWH>();
        //    polylines_in_object = new PosCollection();

        //    titles_in_object = new string[0];

        //    if (curDB != null)
        //    {
        //        string[] codes = (from st in lscodes select st.Upper()).ToArray();

        //        using (Transaction tr = curDB.TransactionManager.StartTransaction())
        //        {
        //            BlockTable bt = (BlockTable)tr.GetObject(curDB.BlockTableId, OpenMode.ForRead);
        //            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
        //            _selectedIds = new ObjectIdCollection();

        //            ObjectIdCollection visId = btr.Cast<ObjectId>()
        //                                .Where(id => codes != null ? _isNodeSelection(curDB, id, mode, codes) : true)
        //                                .Select(id => id).ToCollection();

        //            foreach (ObjectId id in visId)
        //                if (curDB.ValidId(id) && curDB.Inside(id, region))
        //                    _selectedIds.Add(id);
        //            tr.Commit();
        //        }
        //    }
        //}

        //public static pPos BlockToWH(this Database db, ObjectId id, ObjectId parentId)
        //{
        //    pPos[] itm = new pPos[2];
        //    string stname = db._getIdName(id);
        //    if (!stname.empty())
        //    {
        //        if (db._isBlock(id))
        //        {
        //            pPos pt = db._getPoint(id);
        //            pPos[] bb = db._getBound(id);
        //            itm = new pPos(stname, bb[0].X, bb[0].Y, db._getRotation(id));

        //            itm.Width = bb[1].X - bb[0].X;
        //            itm.Height = bb[1].Y - bb[0].Y;
        //            itm.ObjectId = parentId.ToText();
        //        }
        //        else if (db._isDoor(id))
        //        {
        //            pPos[] bb = db._getVertices(id);
        //            pPos pt = (bb[0] + bb[1]) / 2;
        //            itm = new pPosWH(stname, pt.X, pt.Y, bb[0].Angle(bb[1]));
        //            itm.Width = bb[0].DistanceTo(bb[1]);
        //            itm.Height = db._getDoorHeight(id);
        //            itm.ObjectId = parentId.ToText();
        //        }
        //    }
        //    return itm;
        //}

        //public static void GetEntities(this Database db, ObjectId objId,
        //    Func<Database, ObjectId, bool> fn = null, bool _get_center_point = false)
        ////Inside block
        //{
        //    ObjectIdCollection ids = db.ExplodeEntity(objId);

        //    blocks_in_object = new List<pPosWH>();
        //    polylines_in_object = new PosCollection();
        //    polylines_leader_in_object = new PosCollection();
        //    polylines_in_object_closed = new List<bool>();
        //    titles_in_object = new string[0];

        //    ACD.WR("IDS {0}", IR.SelectedIds.Count);

        //    if (ids.Count > 0)
        //    {
        //        ObjectIdCollection lwpIds = ids.Cast<ObjectId>()
        //            .Where(id => (fn == null || (fn(db, id)) && db._isLine(id) && !db._isGObject(id))).Select(id => id).ToCollection();

        //        polylines_in_object = lwpIds.Cast<ObjectId>().Select(id => db._getVertices(id)).ToCollection();
        //        polylines_in_object_closed = lwpIds.Cast<ObjectId>().Select(id => db._isPolylineClosed(id)).ToList();
        //        polylines_in_layer = lwpIds.Cast<ObjectId>().Select(id => db._getLayer(id)).ToList();

        //        lwpIds = ids.Cast<ObjectId>()
        //            .Where(id => (fn == null || fn(db, id)) && db._isWall(id)).Select(id => id).ToCollection();

        //        foreach (ObjectId lwpId in lwpIds)
        //        {
        //            pPos[] tmp_ls = db._getVertices(lwpId);

        //            polylines_in_object.Add(new pPos[] { tmp_ls[0], tmp_ls[1] });
        //            polylines_in_object_closed.Add(false);
        //            polylines_in_layer.Add(db._getLayer(lwpId));

        //            polylines_in_object.Add(new pPos[] { tmp_ls[2], tmp_ls[3] });
        //            polylines_in_object_closed.Add(false);
        //            polylines_in_layer.Add(db._getLayer(lwpId));
        //        }

        //        polylines_leader_in_object = ids.Cast<ObjectId>()
        //            .Where(id => (fn == null || fn(db, id)) && db._isLeader(id))
        //            .Select(id => db._getVertices(id)).OrderBy(pts => -pts.Length).ToCollection();

        //        titles_in_object = ids.Cast<ObjectId>()
        //            .Where(id => (fn == null || fn(db, id)) && db._isText(id)).OrderBy(id => db._getPoint(id).Y)
        //            .ThenBy(id => db._getPoint(id).X).Select(id => db._getContent(id)).ToArray();

        //        blocks_in_object = ids.Cast<ObjectId>()
        //            .Where(id => (fn == null || fn(db, id)) && (db._isBlock(id) || db._isDoor(id)))
        //            .Select(id => db.BlockToWH(id, objId)).ToList();

        //        //if (erased)
        //        db.EraseObjects(ids);
        //    }
        //}

        
        

        public static string[] GetAllTitles(this Database db)
        {
            db.GetEntities(null, EN_SELECT.AC_DXF, "INSERT");
            List<string> str = new List<string>();

            foreach (ObjectId id in IR.SelectedIds)
            {
                string stname = db._getIdName(id);

                if (stname == "REGION")
                {
                    stname = db._getContent(id);

                    if (stname != null && stname != "" && !stname.StartsWith("*") && !str.Contains(stname))
                        str.Add(stname);
                }
            }

            str.Sort();
            return str.ToArray();
        }

        public static string[] GetAllTitlesInFile(string fname, string key)
        {
            string[] res = null;
            using (Database db = ACD.ReadDWG(fname))
            {
                db.GetEntities(null, EN_SELECT.AC_DXF, "MTEXT", "TEXT");
                res = IR.SelectedIds.Cast<ObjectId>()
                    .Where(id => db._getContent(id).StartsWith(key))
                    .Select(id => db._getContent(id)).ToArray();
            }
            return res;
        }

        //public static PosCollection GetIdsGrids(this Database db, ObjectIdCollection ids)
        //{
        //    PosCollection res = new PosCollection();
        //    //ObjectIdCollection expIds = new ObjectIdCollection();
        //    double minsize = pString.INI_String("MIN_BLOCK_EXPLODE").ToNumber(5000);

        //    foreach (ObjectId id in ids)
        //        if (db._isBlock(id))
        //        {
        //            pPos[] bb = db._getBound(id);

        //            if (bb != null && db._getBound(id).Size().X > minsize
        //            && db._getBound(id).Size().Y > minsize)
        //            {
        //                //pPos[] bb = db._getBound(id);

        //                pPos basept = db._getPoint(id);
        //                double len = Math.Min(bb.Size().X, bb.Size().Y);

        //                ObjectIdCollection expIds = new ObjectIdCollection();
        //                ObjectIdCollection subIds = db.GetEntInBlock(id).ToList()
        //                    .Where(i => (db._isLine(i) || (db._isPolyline(i)
        //                    && !db._isPolylineClosed(i))))
        //                    .Select(i => i).ToCollection();

        //                res.AddRange(subIds.ToList().Select(i => db._getVertices(i).Move(basept)).ToCollection());
        //            }
        //        }
        //    return res;
        //}

        public static void _hideObjects(this Database db, pPos[] region, string key)
        {
            db.GetEntities(region, EN_SELECT.AC_DXF_AND_HYPERLINK, "LWPOLYLINE", "INSERT", "MTEXT", key);
            //ME.EraseObjects(db, IR._selectedIds);
            
        }

        public static ObjectId currentIdName;

        //public static double _getIdSize(this Database db, ObjectId objId, int axis = 0)
        //{
        //    double res = 0;

        //    if (db._isBlock(objId))
        //    {
        //        object state = null;
        //        string[] state_names = new string[] { "Width", "Size" };

        //        if (axis == 0)
        //        {
        //            foreach (string st in state_names)
        //            {
        //                state = db.GetDynBlockProp(objId, st);
        //                if (state != null)
        //                    break;
        //            }

        //            if (state != null)
        //                res = Convert.ToDouble(state);
        //        }

        //        if (res == 0)
        //        {
        //            ObjectId cloneId = db.CloneObjects(objId);
        //            db._setRotation(cloneId, 0);
        //            pPos[] bb = db._getBound(cloneId);

        //            res = bb[1][axis] - bb[0][axis];
        //            db.EraseObjects(cloneId);
        //        }
        //    } else if (db._isDoor(objId) || db._isWall(objId))
        //    {
        //        pPos[] pts = db._getVertices(objId);
        //        res = pts.First().DistanceTo(pts.Last());
        //    }
        //    return res;
        //}

        //public static pPosWH[] _insideBlock(this Database db, ObjectId objId)
        //{
        //    List<pPosWH> res = new List<pPosWH>();
        //    using (Transaction tr = db.TransactionManager.StartTransaction())
        //    {
        //        if (db._isBlock(objId))
        //        {
        //            BlockReference blockRef = (BlockReference)tr.GetObject(objId, OpenMode.ForRead, true);
        //            if (blockRef != null)
        //            {
        //                BlockTableRecord br = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;

        //                foreach (ObjectId id in br)
        //                    if (db._isBlock(id))
        //                    {
        //                        string blockName = db._getIdName(id);

        //                        if (!blockName.StartsWith("*"))
        //                        {
        //                            pPos[] bb = db._getBound(id).Move(db._getPoint(objId));
        //                            pPosWH itm = new pPosWH(blockName, bb[0].X, bb[0].Y, db._getRotation(id));

        //                            itm.Width = bb[1].X - bb[0].X;
        //                            itm.Height = bb[1].Y - bb[0].Y;
        //                            res.Add(itm);
        //                        }
        //                    }
        //            }
        //        }
        //        tr.Commit();
        //    }
        //    return res.ToArray();
        //}

        public static bool _isDynamicBlock(this Database db, ObjectId objId)
        {
            bool res = false;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference blockRef = (BlockReference)tr.GetObject(objId, OpenMode.ForRead, true);
                if (blockRef != null)
                res = blockRef.IsDynamicBlock;

                tr.Commit();
            }

            return res;
        }
        public static string GetAllDynBlockProp(this Database db, ObjectId blockID)
        {
            string res = "";
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference block = (BlockReference)tr.GetObject(blockID, OpenMode.ForRead);
                if ((block != null) && (block.IsDynamicBlock))
                {
                    DynamicBlockReferencePropertyCollection pc = block.DynamicBlockReferencePropertyCollection;
                    // Loop through, getting the info for each property           
                    foreach (DynamicBlockReferenceProperty prop in pc)
                        if (prop.PropertyName != "Origin" && !prop.PropertyName.StartsWith("Flip"))
                        {
                            string val = prop.Value.ToString();
                            double n = prop.Value.ToString().ToNumber();
                            if (n != 0)
                                val = ((int)n).ToString();
                            res += String.Format("{0}={1}|", prop.PropertyName, val);
                        }

                    pc.Dispose();
                }
                tr.Commit();
            }
            return res;
        }



        //public static List<KVL> SelectedKVLs //Get region in selected
        //{
        //    get
        //    {
        //        List<KVL> lsRes = new List<KVL>();

        //        /*foreach (ObjectId id in _selectedIds)
        //        {
        //            string st = curDB._getIdName(id);
        //            if (st != null)
        //                foreach (string key in AC_REGION_KEYS)
        //                    if (st.Upper().Contains(key))
        //                    {
        //                        if (id.ObjectClass.DxfName == "LWPOLYLINE")
        //                        {
        //                            lsRes.Add(new KVL(st, curDB._getVertices(id), EN_KVLTYPE.AC_REGION, id));
        //                            break;
        //                        }
        //                        else
        //                        {
        //                            var bb = _getBound(curDB, id);
        //                            if (bb != null)
        //                            {
        //                                lsRes.Add(new KVL(st, bb[0].Rect(bb[1]), EN_KVLTYPE.AC_REGION, id));
        //                                break;
        //                            }
        //                        }
        //                    }
        //        }

        //        if (lsRes.Count > 0)
        //            lsRes = lsRes.OrderBy(itm => itm.Key.ToNumber()).ToList();*/
        //        return lsRes;
        //    }
        //}

        /*public static double _Area(this Database db, ObjectId id)
        {
            double res = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (id.ObjectClass.DxfName == "LWPOLYLINE")
                {
                    Polyline lwp = (Polyline)tr.GetObject(id, OpenMode.ForRead);
                    res = lwp.Area;
                }
                else if (id.ObjectClass.DxfName == "HATCH")
                {
                    Hatch hat = (Hatch)tr.GetObject(id, OpenMode.ForRead);

                    for (int i = 0; i < hat.NumberOfLoops; i++)
                    {
                        HatchLoop hatloop = hat.GetLoopAt(i);
                        BulgeVertexCollection cls = hatloop.Polyline;
                        
                        //ACD.WR("Curve {0}", cls);
                        List<pPos> pls = new List<pPos>();
                        for (int ii = 0; ii < cls.Count; ii++)
                            pls.Add(cls[ii].Vertex.ToPos());

                        res += pls.Area();
                    }
                }
                tr.Commit();
            }

            return res;
        }*/

        //public static double _getHeight(this Database db, ObjectId id)
        //{
        //    double res = 0;
        //    using (Transaction tr = db.TransactionManager.StartTransaction())
        //    {
        //        switch (id.ObjectClass.DxfName)
        //        {
        //            case "INSERT":
        //                string[] title_keys = new string[] { "HEIGHT", "LEVEL", "COTE" };
        //                foreach (string key in title_keys)
        //                {
        //                    string st = db.GetBlockAtt(id, key);
        //                    if (st != null && st != "")
        //                    {
        //                        res = st.ToNumber();
        //                        break;
        //                    }
        //                }
        //                break;
        //            case "MTEXT":
        //                MText mtxt = (MText)tr.GetObject(id, OpenMode.ForRead);
        //                res = mtxt.TextHeight;
        //                break;
        //            case "TEXT":
        //                DBText txt = (DBText)tr.GetObject(id, OpenMode.ForRead);
        //                res = txt.Height;
        //                break;
        //        }

        //        tr.Commit();
        //    }
        //    return res;
        //}

        public static bool _isDoorType(this Database db, ObjectId id)
        {
            string[] keywords = new string[] { "Ga", "Gb" };
            return db._isDoor(id) || (db._isWall(id)
                    && keywords.Any(key => db._getIdName(id).st_(key.Upper())));
        }


        public static string _getField(this Database db, ObjectId objId)
        {
            string res = "";
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                MText mtext = tr.GetObject(objId, OpenMode.ForRead) as MText;
                
                if (mtext != null && mtext.HasFields)
                {
                    ObjectId id = mtext.GetField("TEXT");
                    Field field = tr.GetObject(id, OpenMode.ForRead) as Field;
                    
                    res = field.GetFieldCode(FieldCodeFlags.AddMarkers| FieldCodeFlags.FieldCode);
                }

                tr.Commit();
            }

            return res;
        }


        
        

        public static void _setPoint(this Database db, ObjectId objId, pPos pt, int index = 0)
        {
            Point3d p3t = pt.ToPoint3();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                switch (objId.ObjectClass.DxfName)
                {
                    case "AEC_WALL":
                        Autodesk.Aec.Arch.DatabaseServices.Wall wall = (Autodesk.Aec.Arch.DatabaseServices.Wall)tr.GetObject(objId, OpenMode.ForWrite);
                        if (index == 1)
                            wall.Set(p3t, wall.EndPoint, Vector3d.ZAxis);
                        else
                            wall.Set(wall.StartPoint, p3t, Vector3d.ZAxis);
                        break;

                    case "AEC_DOOR":
                        Autodesk.Aec.Arch.DatabaseServices.Door door = (Autodesk.Aec.Arch.DatabaseServices.Door)tr.GetObject(objId, OpenMode.ForWrite);
                        door.Location = p3t;
                        break;

                    case "AEC_WINDOW":
                        Autodesk.Aec.Arch.DatabaseServices.Window window = (Autodesk.Aec.Arch.DatabaseServices.Window)tr.GetObject(objId, OpenMode.ForWrite);
                        window.Location = p3t;
                        break;
                    case "CIRCLE":
                        Circle cir = (Circle)tr.GetObject(objId, OpenMode.ForWrite);
                        cir.Center = p3t;
                        break;
                    case "ELLIPSE":
                        Ellipse elp = (Ellipse)tr.GetObject(objId, OpenMode.ForWrite);
                        elp.Center = p3t;
                        break;
                    case "POINT":
                        DBPoint point = (DBPoint)tr.GetObject(objId, OpenMode.ForWrite);
                        point.Position = p3t;
                        break;
                    case "MTEXT":
                        MText mtxt = (MText)tr.GetObject(objId, OpenMode.ForWrite);
                        mtxt.Location = p3t;
                        break;
                    case "TEXT":
                        DBText txt = (DBText)tr.GetObject(objId, OpenMode.ForWrite);
                        txt.Position = p3t;
                        break;
                    case "HATCH":
                        Hatch hatch = (Hatch)tr.GetObject(objId, OpenMode.ForWrite);
                        hatch.Origin = pt.ToPoint2();
                        break;
                    case "INSERT":
                        BlockReference block = (BlockReference)tr.GetObject(objId, OpenMode.ForWrite);
                        block.Position = p3t;
                        break;
                    case "DIMENSION":
                        Entity dimObj = (Entity)tr.GetObject(objId, OpenMode.ForWrite);

                        if (dimObj is RotatedDimension)
                        {
                            RotatedDimension dim = (RotatedDimension)tr.GetObject(objId, OpenMode.ForWrite);
                            if (dim != null)
                            {
                                if (index == 0)
                                    dim.XLine1Point = p3t;
                                else if (index == 1)
                                    dim.XLine2Point = p3t;
                                if (index == 2)
                                    dim.DimLinePoint = p3t;
                            }
                        }else if(dimObj is AlignedDimension)
                        {
                            AlignedDimension adim = (AlignedDimension)tr.GetObject(objId, OpenMode.ForWrite);
                            if(adim != null)
                            {
                                if (index == 0)
                                    adim.XLine1Point = p3t;
                                else if (index == 1)
                                    adim.XLine2Point = p3t;
                                if (index == 2)
                                    adim.DimLinePoint = p3t;
                            }
                        }

                        //if (Math.Abs(dim.Measurement - 100) <= 1)
                        //    dim.DimensionText = "t";
                        //else if (Math.Abs(dim.Measurement - 200) <= 1)
                        //    dim.DimensionText = "2t";

                        break;
                }
                tr.Commit();
            }
        }


        public static string[] _getContent(string filename, string table_name)
        {
            string[] res = null;
            using (Database db = ACD.ReadDWG(filename))
            {
                db.GetEntities(null, EN_SELECT.AC_DXF, "ACAD_TABLE");

                foreach (ObjectId tabId in IR.SelectedIds)
                    if (db._getIdName(tabId).Upper() == table_name.Upper())
                    {
                        //res = db._getTabContent(tabId);
                        break;
                    }
            }
            return res;
        }

        public static bool _isVertice(this Database db, ObjectId objId)
        {
            return !db._isText(objId) && !db._isBlock(objId) && !db._isCircle(objId) && !db._isElip(objId);// && !objId._isGLayer(db);
        }

        public static double _maxSegmentVertice(this Database db, ObjectId lwpId)
        {
            pPos[] pts = db._getVertices(lwpId);
            double res = 0;

            for (int i = 0; i < pts.Length - 1; i++)
            {
                double n = pts[i].DistanceTo(pts[i + 1]);
                if (res < n) res = n;
            }

            if (db._isPolylineClosed(lwpId))
            {
                double n = pts.Last().DistanceTo(pts.First());
                if (res < n) res = n;
            }
            return res;
        }

        public static pPos[] _selectedRoom(this Database db, ObjectIdCollection ids)
        {
            ObjectIdCollection selIds = db.FilterIds(ids, _isVertice);
            pPos[] ls;
            List<pPos> pts = new List<pPos>();
            if (selIds.Count > 0)
            {
                ls = db._getVertices(selIds.ToArray().OrderBy(id => -db._maxSegmentVertice(id)).First());
                //ACD.WR("LS {0}", ls.Length);
                foreach (ObjectId id in ids)
                    pts.AddRange(db._isVertice(id) ? db._getVertices(id) : db._getBound(id).Rect());
            }
            else
            {
                pts = db._getBound(ids).ToList();
                ls = pts.Rect();
            }

            if (pts.All(pt => pt.DistanceTo(ls[0], ls[1]) < 1))
            {
                pts.AddRange(ls[0].Parallel(ls[1], 250));
                pts.AddRange(ls[0].Parallel(ls[1], -250));
            }

            pPos[] r = pts.RectByDirection(ls[1] - ls[0]);

            pPos[] res = r[0].IsParrallel(r[1], ls[0], ls[1]) ?
                new pPos[] { r[0].Along(100, r[1]), r[0], r[3], r[2], r[1], r[1].Along(100, r[0]) } :
                new pPos[] { r[2].Along(100, r[1]), r[2], r[3], r[0], r[1], r[1].Along(100, r[2]) };

            //ACD.WR("Parallel {0}", r[0].IsParrallel(r[1], ls[0], ls[1]));
            //db.DrawPolyline(r, false, "LAYER=DEFPOINTS", true);
            //db.DrawPolyline(res, false);

            return res;
        }

        //public static List<pPosWH> blocks_in_object;
        public static PosCollection polylines_in_object;
        public static List<string> polylines_in_layer;
        public static List<bool> polylines_in_object_closed;
        public static PosCollection polylines_leader_in_object;

        public static ObjectIdCollection _setWallPoint(this Database db, ObjectId objId, pPos pt1, pPos pt2)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                Autodesk.Aec.Arch.DatabaseServices.Wall wall = (Autodesk.Aec.Arch.DatabaseServices.Wall)tr.GetObject(objId, OpenMode.ForWrite);
                wall.Set(pt1.ToPoint3(), pt2.ToPoint3(), Vector3d.ZAxis);
                //wall.StartPoint = pt.ToPoint3();

                tr.Commit();
            }
            return res;
        }

        //public static ObjectIdCollection _setWallPoint(this Database db, ObjectId objId, pPos pt)
        //{
        //    ObjectIdCollection res = new ObjectIdCollection();
        //    using (Transaction tr = db.TransactionManager.StartTransaction())
        //    {
        //        BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForWrite);
        //        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

        //        Autodesk.Aec.Arch.DatabaseServices.Wall wall = (Autodesk.Aec.Arch.DatabaseServices.Wall)tr.GetObject(objId, OpenMode.ForWrite);
        //        wall.Set(pt.ToPoint3(), wall.EndPoint, Vector3d.ZAxis);
        //        //wall.StartPoint = pt.ToPoint3();

        //        tr.Commit();
        //    }
        //    return res;
        //}

        static pPos _nearestPoint(pPos[] ls, pPos pt)
        {
            double v = double.PositiveInfinity;
            pPos res = null;

            foreach (pPos tmp in ls)
            {
                double n = pt.DistanceTo(tmp);
                if (n < v)
                {
                    v = n;
                    res = tmp;
                }
            }
            return res;
        }

        public static ObjectIdCollection ClipVertices(this Database db, ObjectId objId, pPos[] region)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                pPos[] src_pls = db._getVertices(objId);
                pPos[] int_pls = new pPos[0];

                if (src_pls.Length > 0)
                    int_pls = src_pls.IntersectPts(region,
                        src_pls.First().Content._prop("Closed").ToBool());

                if (int_pls.Length == 0)
                    db.EraseObject(objId);
                else
                {
                    switch (objId.ObjectClass.DxfName)
                    {
                        case "DIMENSION":
                            RotatedDimension dim = (RotatedDimension)tr.GetObject(objId, OpenMode.ForWrite);

                            if (!dim.XLine1Point.ToPos().Inside(region))
                                dim.XLine1Point = _nearestPoint(int_pls, dim.XLine1Point.ToPos()).ToPoint3();

                            if (!dim.XLine2Point.ToPos().Inside(region))
                                dim.XLine2Point = _nearestPoint(int_pls, dim.XLine2Point.ToPos()).ToPoint3();

                            res.Add(objId);
                            break;

                        case "LINE":
                            Line ln = (Line)tr.GetObject(objId, OpenMode.ForWrite);

                            if (!ln.StartPoint.ToPos().Inside(region))
                                ln.StartPoint = _nearestPoint(int_pls, ln.StartPoint.ToPos()).ToPoint3();

                            if (!ln.EndPoint.ToPos().Inside(region))
                                ln.EndPoint = _nearestPoint(int_pls, ln.EndPoint.ToPos()).ToPoint3();

                            res.Add(objId);
                            break;

                        case "AEC_WALL":
                            Autodesk.Aec.Arch.DatabaseServices.Wall wall = (Autodesk.Aec.Arch.DatabaseServices.Wall)tr.GetObject(objId, OpenMode.ForWrite);

                            if (wall != null && wall.WallGeometryType == AEC.WallType.Linear)
                            {
                                if (!wall.StartPoint.ToPos().Inside(region))
                                    wall.Set(_nearestPoint(int_pls, wall.StartPoint.ToPos()).ToPoint3(), wall.EndPoint, Vector3d.ZAxis);
                                else
                                    wall.Set(wall.StartPoint, _nearestPoint(int_pls, wall.EndPoint.ToPos()).ToPoint3(), Vector3d.ZAxis);
                            }
                            res.Add(objId);
                            break;

                        default:
                            //res.Add(db.DrawPolyline(src_pls));

                            //if (IR.PolylineClosed) src_pls = src_pls.Add(src_pls.First());
                            //GraphConsole.Compute(new pPos[][] { src_pls, region.Add(region.First()) });

                            ////db.DrawPolyline(region, false);
                            ////db.DrawPolyline(tmp, false);
                            ////ACD.WR("R_SULT {0}", GraphConsole.ResultPts.Count);

                            //if (GraphConsole.ResultPts.Count > 0)
                            //{
                            //    pPos[] inner = region.Offset(-100);
                            //    pPos[] outer = region.Offset(100);
                            //    //pPos[] outer_pls = pPos.Offset(src_pls, 100);

                            //    //db._setLayer(db.DrawPolyline(outer_pls), DE.DEFPOINTS);
                            //    //db._setLayer(db.DrawPolyline(outer), "0");

                            //    foreach (pPos[] ls in GraphConsole.ResultPts)
                            //    {
                            //        if (ls.Inside(outer))
                            //        {
                            //            if (db._isHatch(objId))
                            //            {
                            //                if (ls.Centroid().Inside(src_pls)) res.AddRange(db.DrawHatch(ls));
                            //            }
                            //            else
                            //            {
                            //                List<pPos> tmp_ls = new List<pPos>();
                            //                for (int i = 0; i < ls.Length; i++)
                            //                    if (((ls[i] + ls[(i + 1) % ls.Length]) / 2).Inside(inner))
                            //                    {
                            //                        tmp_ls.Add(ls[i]);
                            //                        tmp_ls.Add(ls[(i + 1) % ls.Length]);
                            //                    }
                            //                    else if (tmp_ls.Count > 0)
                            //                    {
                            //                        ObjectId lwpId = db.DrawPolyline(tmp_ls, false);
                            //                        res.Add(lwpId);
                            //                        tmp_ls = new List<pPos>();
                            //                    }

                            //                if (tmp_ls.Count > 0)
                            //                {
                            //                    ObjectId lwpId = db.DrawPolyline(tmp_ls, false);
                            //                    res.Add(lwpId);
                            //                    tmp_ls = new List<pPos>();
                            //                }
                            //            }
                            //        }
                            //    }

                            //    if (res.Count == 0)
                            //    {
                            //        List<pPos> tmp = new List<pPos>();
                            //        pPos pt = int_pls.First();
                            //        for (int i = 0; i < src_pls.Length - 1; i++)
                            //            if (pt.IsBetween(src_pls[i], src_pls[(i + 1) % src_pls.Length]))
                            //            {
                            //                if (src_pls[i].Inside(region))
                            //                {
                            //                    for (int j = 0; j <= i; j++)
                            //                        tmp.Add(src_pls[j]);
                            //                    tmp.Add(pt);
                            //                }
                            //                else
                            //                {
                            //                    tmp.Add(pt);
                            //                    for (int j = i + 1; j < src_pls.Length; j++)
                            //                        tmp.Add(src_pls[j]);
                            //                }
                            //            }

                            //        //db.DrawCircle(pt, 200);
                            //        //ACD.WR("OK_INT_2 INT_PLS {0}", tmp.Count);

                            //        if (tmp.Count > 1)
                            //        {
                            //            //res.Add(db.DrawPolyline(tmp, IR.PolylineClosed));
                            //            //db._setLayer(res[res.Count - 1], DE.DEF_LAYER_FIN);
                            //        }
                            //    }

                            //    db.EraseObjects(objId);
                            //}
                            break;
                    }
                }
                tr.Commit();
            }
            return res;
        }



        //public static ObjectIdCollection GetEntInBlock(this Database db, ObjectId blockId)
        //{
        //    return db.GetEntInBlockByName(db._getIdName(blockId));
        //}

        public static string[] ListBlock(this Database db)
        {
            List<string> res = new List<string>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTble = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                foreach (ObjectId objId in acBlkTble)
                    if (!objId.IsErased)
                    {
                        BlockTableRecord btr = tr.GetObject(objId, OpenMode.ForRead) as BlockTableRecord;
                        //if (ValidPrefix.Any(ch => btr.Name.StartsWith(ch.ToString())) && !res.Contains(btr.Name))
                        res.Add(btr.Name);
                    }
                tr.Commit();
            }
            return res.ToArray();
        }

        public static string[] ListBlockInFile(string filename)
        {
            List<string> res = new List<string>();
            using (Database db = ACD.ReadDWG(filename))
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable acBlkTble = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    foreach (ObjectId objId in acBlkTble)
                    {
                        BlockTableRecord btr = tr.GetObject(objId, OpenMode.ForRead) as BlockTableRecord;
                        if (!res.Contains(btr.Name))
                            res.Add(btr.Name);
                    }
                    tr.Commit();
                }
            }
            return res.ToArray();
        }


        public static string HasBlockInFile(string sourcefilename, string blockname)
        {
            string[] list = ListBlockInFile(sourcefilename);
            //db.WR("Blockname: {0}", blockname);
            //db.WRArray("List:", list);
            string res = null;
            int index = Array.FindIndex(list, st => st.Upper().Contains(blockname.Upper()));
            if (index != -1)
                res = list[index];
            return res;
        }

        public static ObjectId HasBlock(this Database db, string blockname)
        {
            ObjectId res = ObjectId.Null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
                if (bt.Has(blockname))
                    res = bt[blockname];
                tr.Commit();
            }
            return res;
        }

        public static ObjectId HasBlockToObjectId(string sourcefilename, string blockname, bool toObjectId)
        {
            ObjectId res = ObjectId.Null;

            using (Database db = ACD.ReadDWG(sourcefilename))
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
                    if (bt.Has(blockname))
                        res = bt[blockname];
                    tr.Commit();
                }
            }

            return res;
        }


        public static ObjectIdCollection GetEntInBlockByName(this Database db, string blockName)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                ObjectId blockId = db.HasBlock(blockName);

                if (!blockId.IsNull)
                {
                    //BlockReference block = tr.GetObject(blockId, OpenMode.ForWrite) as BlockReference;

                    //db.WR("BlockId {0} DXF {1}", blockId, blockId.ObjectClass.DxfName);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(blockId, OpenMode.ForWrite);

                    res = btr.Cast<ObjectId>().Select(id => id).ToCollection();
                }

                tr.Commit();
            }

            return res;
        }




        

        //public static bool PolylineClosed;
        //public static WallOpeningDataCls WallVerticesResult;

        static PosCollection pls;
        
        static void appendSelId(this Database db, ObjectId lwpId)
        {
            pPos[] ls = db._getVertices(lwpId);
            if (ls.Length() > 1)
            {
                pls.Add(ls.OrderBy(p => p.X).ThenBy(p => p.Y).ToArray());
                pls.Closed = pls.Closed.Add(db._isHatch(lwpId) || db._isPolylineClosed(lwpId));
            }
        }


        public static ObjectIdCollection ExplodeEntity(this Database db, ObjectIdCollection ids)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            foreach (ObjectId id in ids)
                res.AddRange(db.ExplodeEntity(id));
            return res;
        }

        public static ObjectIdCollection ExplodeEntity(this Database db, ObjectId objId)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            ObjectIdCollection del_ids = new ObjectIdCollection();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                Entity obj = (Entity)tr.GetObject(objId, OpenMode.ForRead);

                Entity clone_obj = (Entity)obj.Clone();
                DBObjectCollection ents = new DBObjectCollection();

                try
                {
                    clone_obj.Explode(ents);
                }
                catch (System.Exception ex)
                {
                    //ACD.WR("Cannot explode {0}", db._getIdName(objId));
                }

                foreach (Entity ent in ents)
                {
                    btr.AppendEntity(ent);
                    tr.AddNewlyCreatedDBObject(ent, true);

                    if (db._isArray(objId))// || db._isWallExploded(ent.ObjectId))
                    {
                        res.AddRange(db.ExplodeEntity(ent.ObjectId));
                        del_ids.Add(ent.ObjectId);
                    }
                    else
                        res.Add(ent.ObjectId);
                }

                db.EraseObjects(del_ids);
                tr.Commit();
            }

            return res;
        }


        public static PosCollection GetIdPts(this Database db, ObjectId id)
        {
            string[] dxfs = new string[] { "LINE", "LWPOLYLINE", "HATCH" };
            string dxf = id.ObjectClass.DxfName;
            ObjectIdCollection delIds = new ObjectIdCollection();
            pls = new PosCollection();

            if (dxf == "AEC_2D_SECTION")
            {
                ObjectIdCollection subIds = db.ExplodeEntity(id);

                delIds.AddRange(subIds);
                id = subIds.First();

                //ACD.WR("SelIds {0}", subIds.Count);
            }

            dxf = id.ObjectClass.DxfName;

            if (dxf == "INSERT")
            {
                ObjectIdCollection subIds = db.ExplodeEntity(id);
                delIds.AddRange(subIds);

                foreach (ObjectId subId in subIds)
                    appendSelId(db, subId);
            }
            else if (dxfs.Contains(dxf))
            {
                appendSelId(db, id);
            }

            db.EraseObjects(delIds);
            return pls;
        }

        public static pPos[] _getPolylineVerts(this Database db, ObjectId id)
        {
            List<pPos> pts = new List<pPos>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                if (db._isPolyline(id))
                {
                    Polyline pl = (Polyline)tr.GetObject(id, OpenMode.ForRead);

                    for (int i = 0; i < pl.NumberOfVertices; i++)
                    {
                        pPos pt = pl.GetPoint2dAt(i).ToPos2d();
                        pt.Content = pl.GetBulgeAt(i).ToString();
                        pts.Add(pt);
                    }
                }
                else if (db._isLine(id))
                {
                    Line ln = (Line)tr.GetObject(id, OpenMode.ForWrite);
                    pts.Add(ln.StartPoint.ToPos());
                    pts.Add(ln.EndPoint.ToPos());
                }

                tr.Commit();
            }

            return pts.ToArray();
        }

        public static ObjectId _setPolylineVerts(this Database db, ObjectId id, IEnumerable<pPos> pts)
        {
            ObjectId res = ObjectId.Null;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                if (db._isPolyline(id))
                {
                    Polyline pl = (Polyline)tr.GetObject(id, OpenMode.ForWrite);

                    for (int i = 0; i < pts.Count(); i++)
                        //{
                        //ACD.WR("Index {0}:{1}", i, pts.ElementAt(i));
                        pl.SetPointAt(i, pts.ElementAt(i).ToPoint2());
                    //ACD.WR("Done {0}", i);
                    //}
                    //pl.Closed = false;

                    //res = btr.AppendEntity(pl);
                    //tr.AddNewlyCreatedDBObject(pl, true);
                }else if(db._isLine(id))
                {
                    Line ln = (Line)tr.GetObject(id, OpenMode.ForWrite);
                    ln.StartPoint = pts.ElementAt(0).ToPoint3();
                    ln.EndPoint = pts.ElementAt(1).ToPoint3();
                }
                tr.Commit();
            }

            return res;
        }

        public static pPos[] TrimPolyline(this Database db, ObjectId id, IEnumerable<pPos> trim_region)
        {
            List<pPos> res = new List<pPos>();
            pPos[] pts = _getPolylineVerts(db, id);
            pPos[] tr_region = trim_region.Offset(1);

            for (int i = 0; i < pts.Length - 1; i++)
            {
                pPos[] ln = new pPos[] { pts[i], pts[i + 1] };
                pPos[] intps = tr_region.Intersect(pts[i], pts[i + 1], true);

                if (intps.Length > 0)
                {
                    if (pts[i + 1].Inside(tr_region))
                    {
                        res.AddRange(DE.NumericArray(0, i).Select(n => pts[n]));
                        res.AddRange(intps);
                    }
                    else if (pts[i].Inside(tr_region))
                    {
                        res.AddRange(intps);
                        res.AddRange(DE.NumericArray(i + 1, pts.Length - 1).Select(n => pts[n]));
                    }
                }
            }

            return res.ToArray();
        }


        public static Matrix3d _getIdTransform(this Database db, ObjectId objId)
        {
            Matrix3d res = Matrix3d.Identity;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity ent = (Entity)tr.GetObject(objId, OpenMode.ForRead);
                res = ent.CompoundObjectTransform;

                string st = "";
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                        st += res[i, j] + ";";

                    st += "\r\n";
                }
                //ACD.WR("Matrix {0}", st);

                tr.Commit();
            }
            return res;
        }


        public static void _setRadius(this Database db, ObjectId objId, double val)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                switch (objId.ObjectClass.DxfName)
                {
                    case "CIRCLE":
                        Circle cir = (Circle)tr.GetObject(objId, OpenMode.ForWrite);
                        cir.Radius = val;
                        break;
                    case "ARC":
                        Arc arc = (Arc)tr.GetObject(objId, OpenMode.ForWrite);
                        arc.Radius = val;
                        break;
                }
                tr.Commit();
            }
        }

        //public static ObjectId _getGridCoteId(this Database db, ObjectId gridId)
        //{
        //    ObjectId res = ObjectId.Null;
        //    db.GetEntities(db._getBound(gridId), EN_SELECT.AC_DXF, "INSERT");
        //    foreach(ObjectId id in IR.SelectedIds)
        //    {
        //        string cote = db.GetBlockAtt(id, "COTE");
        //        if (!cote.empty())
        //        {
        //            res = id;
        //            break;
        //        }
        //    }
        //    return res;
        //}

        static pPos[] _getInfoPoints(string txt)
        {
            pPos[] res = new pPos[0];
            string pos_str = txt._prop("POS");
            if (!pos_str.empty())
                res = pos_str.filter("[]").Cast<string>().Select(st => pPos.FromString(st)).ToArray();

            return res;
        }

        static pPos _getInfoCenterPoint(string txt)
        {
            pPos[] ls = _getInfoPoints(txt);
            return ls.Length > 0 ? ls.CenterPoint() : null;
        }
        
        //public static string[] _groupWallIds(this Database db, ObjectIdCollection wallIds)
        //{
        //    List<string> res = new List<string>();
        //    List<int[]> indexes = new List<int[]>();

        //    for (int i = 0; i < wallIds.Count - 1; i++)
        //        if (indexes.All(ls => !ls.Contains(i)))
        //        {
        //            List<int> ar = new List<int> { i };
        //            pPos[] ls1 = db._getVertices(wallIds[i]).ExtentLine(200);

        //            for (int j = i + 1; j < wallIds.Count; j++)
        //                if (db._getIdName(wallIds[i]) == db._getIdName(wallIds[j]) && indexes.All(ls => !ls.Contains(j)))
        //                {
        //                    pPos[] ls2 = db._getVertices(wallIds[j]);
                            
        //                    if (new PosCollection() { ls1, ls2 }.Intersect().Length > 0)
        //                        ar.Add(j);
        //                }

        //            if (ar.Count > 0)
        //            {
        //                indexes.Add(ar.ToArray());

        //                if (ar.Count > 1)
        //                {
        //                    string str_ids = "";
        //                    for (int j = 0; j < ar.Count; j++)
        //                        str_ids += wallIds[ar[j]].ToText() + (j < ar.Count - 1 ? "," : "");

        //                    pPos[] r = db._selectedRoom(ar.Cast<int>().Select(n => wallIds[n]).ToCollection());

        //                    if (r != null)
        //                    {
        //                        //ROOM_CLS rm = new ROOM_CLS(r);
        //                        //string st = rm.ToText(db, 3600, false);
        //                        //rm.Content = String.Format("OBJ={0}|KEY={1}", "WALL_GROUP", db._getIdName(wallIds[i]));
        //                        //st = st._setprop("KEY", db._getIdName(wallIds[i]));
        //                        //st = st._setprop("IDS", str_ids);
        //                        //res.Add(st);
        //                    }
        //                }
        //                else
        //                {
        //                    ObjectId wId = wallIds[ar[0]];
        //                    string st = String.Format("POS={0}|OBJ={1}|KEY={2}|WIDTH={3}|HEIGHT={4}",
        //                        db._getBound(wId).CenterPoint(), "AEC_WALL", db._getIdName(wId), db._getLength(wId).roundNumber(), 3600);
        //                    //ACD.WR("Line {0}", st);
        //                    res.Add(st);
        //                }
        //            }
        //        }

        //    return res.ToArray();
        //}

        static string[] _collectKey(IEnumerable<string> str, string key)
        {
            List<string> res = new List<string>();
            foreach (string s in str)
            {
                string val = s._prop(key);
                if (!res.Contains(val))
                    res.Add(val);
            }
            return res.OrderBy(s => s).ToArray();
        }

        static double _getWallStyleHoles(this Database db, ObjectIdCollection objIds, string style)
        {
            //db.GetEntities(null, EN_SELECT.AC_DXF, "AEC_WALL");

            double res = 0;

            foreach (ObjectId id in objIds)
                if (db._getIdName(id) == style)
                {
                    pPos[] sizes = db._getWallOpeningSize(id);

                    foreach(pPos sz in sizes)
                        res += sz[0] * sz[1];
                }

            return res;
        }

        public static string[] ListWall(this Database db, ObjectIdCollection objIds)
        {
            //db._loadTableLibDatabase();

            //ObjectIdCollection ids = objIds.Cast<ObjectId>()
            //    .Where(id => id.ObjectClass.DxfName == "AEC_WALL").ToCollection();
            //ObjectIdCollection excludeIds = new ObjectIdCollection();
                        
            //List<string> str = db.FilterIds(ids, IR._isWall).Cast<ObjectId>()
            //                //.Where(id => db._isDoorType(id))
            //                .Select(id => db._getIdInfo(id)).ToList(); //._collectItemIndexes());
                        
            //if (str.Count > 0)
            //    str = str.CollectItemIndexes("LENGTH", true).ToList();

            //str = str.OrderBy(st => st._prop("KEY"))
            //    .ThenBy(st => st._prop("OBJ"))
            //    .ThenBy(st => -st._prop("LENGTH").ToNumber())
            //    .ThenBy(st => -st._prop("WIDTH").ToNumber())
            //    .ThenBy(st => -st._prop("HEIGHT").ToNumber())
            //    .ToList();
            
            List<string> res = new List<string>();
            //string s_ref = ACD.RefFileList(ACD.CADLIB_SAMPLES_WALL);
            //foreach (string st in str)
            //{
            //    res.Add(String.Format("{0}=(Quatity){1}(Obj){2}(Width){3}(Height){4}(HOLE){5}(POS){6}",
            //    st._prop("KEY"), Math.Round(st._prop("LENGTH").ToNumber() /1000, 2), 
            //    st._prop("OBJ"),st._prop("WIDTH"), st._prop("HEIGHT"), 
            //    Math.Round(db._getWallStyleHoles(objIds, st._prop("KEY")) / 1000000,2), st._prop("POS")));
            //    res.Add("[Ref]" + st._prop("WIDTH") + "x" + st._prop("HEIGHT") + "=" + s_ref);
            //}
            
            return res.ToArray();
        }

        public static string[] ListDoor(this Database db, ObjectIdCollection objIds)
        {
            //ObjectIdCollection ids = objIds.Cast<ObjectId>()
            //    .Where(id => db._isDoorType(id)).ToCollection();
            //ObjectIdCollection excludeIds = new ObjectIdCollection();

            //foreach (ObjectId id in ids)
            //    if (db._isWall(id))
            //        excludeIds.AddRange(db._getWallOpeningIds(id));

            //ids = ids.Cast<ObjectId>().Where(id => !excludeIds.Contains(id)).ToCollection();

            //List<string> str = ids.Cast<ObjectId>()
            //                .Where(id => db._isDoorType(id))
            //                .Select(id => db._getIdInfo(id)).ToList(); //._collectItemIndexes());

            ////db.GetEntities(null, EN_SELECT.AC_DXF, "INSERT");
            ////ObjectIdCollection blockIds = IR.SelectedIds;
            ////foreach (ObjectId id in blockIds)
            ////    if (db._isBlock(id))
            ////    {
            ////        db.GetEntities(id, IR._isDoor);
            ////        //ACD.WR("block_in {0}", IR.blocks_in_object);
            ////        if (IR.blocks_in_object != null && IR.blocks_in_object.Count > 0)
            ////            str.AddRange(IR.blocks_in_object.Select(pwh => pwh.ToString() + ",OBJ=Aec_Door"));
            ////    }

            //if(str.Count > 0)
            //    str = str.CollectItemIndexes("POS", false).ToList();

            //str = str.OrderBy(st => st._prop("KEY"))
            //    //.ThenBy(st => ITable._findLAValue(st._prop("KEY")).ChainNumber())
            //    .ThenBy(st => st._prop("OBJ"))
            //    .ThenBy(st => -st._prop("WIDTH").ToNumber())
            //    .ThenBy(st => -st._prop("HEIGHT").ToNumber())
            //    .ToList();

            List<string> res = new List<string>();
            //string s_ref = ACD.RefFileList(ACD.CADLIB_SAMPLES_DOOR);

            ////str = str.OrderBy(s => s._prop("OBJ")).ThenBy(s => s._prop("KEY"))
            //    //.ThenBy(s => s._prop("WIDTH")).ThenBy(s => s._prop("HEIGHT")).ToList();

            //foreach (string st in str)
            //{
            //    res.Add(String.Format("{0}=(Quatity){1}(Obj){2}(Width){3}(Height){4}(POS){5}",
            //        st._prop("KEY"), st._prop("POS").filter(";").Length, st._prop("OBJ"),
            //        st._prop("WIDTH"), st._prop("HEIGHT"), st._prop("POS")));
            //    res.Add("[Ref]" + st._prop("WIDTH") + "x" + st._prop("HEIGHT") + "=" + s_ref);
            //}
            
            return res.ToArray();
        }

        public static ObjectId BlockAroundPoint(this Database db, params pPos[] pts)
        {
            ObjectId res = ObjectId.Null;
            db.GetEntities(null, EN_SELECT.AC_ALL);

            int index = IR.SelectedIds.ToList()
                .FindIndex(id => db._isBlock(id)
                    && !db._getIdName(id).StartsWith("ref")
                    && pts.Any(p => p.Inside(db._getBound(id))));

            if (index != -1)
                res = IR.SelectedIds[index];
            return res;
        }

        public static bool _isOutterBound(this Database db, ObjectId id)
        {
            return ACD.DB._isPolyline(id) && ACD.DB.GetLinetypeText(id) == "OBD";
        }

        public static bool _isInnerBound(this Database db, ObjectId id)
        {
            return ACD.DB._isPolyline(id) && ACD.DB.GetLinetypeText(id) == "IBD";
        }

        public static pPos _getDimTextPos(this Database db, ObjectId id)
        {
            pPos res = null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity ent = (Entity)tr.GetObject(id, OpenMode.ForRead);

                if (ent is RotatedDimension)
                {
                    RotatedDimension dim = (RotatedDimension)ent;
                    res = dim.TextPosition.ToPos();
                }
                else if (ent is AlignedDimension)
                {
                    AlignedDimension dim = (AlignedDimension)ent;
                    res = dim.TextPosition.ToPos();
                }
                else if (ent is RadialDimension)
                {
                    RadialDimension dim = (RadialDimension)ent;
                    res = dim.TextPosition.ToPos();
                }
                if (ent is OrdinateDimension)
                {
                    OrdinateDimension dim = (OrdinateDimension)ent;
                    res = dim.TextPosition.ToPos();
                }

                tr.Commit();
            }

            return res;
        }

        public static pPos[] GetCurvePoints(this Database db, ObjectId id, 
            int splitacrc = 0, bool with_first_point = true)
        {
            pPos[] res = null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(id, OpenMode.ForRead) as Curve;
                
                double len = curve.GetDistanceAtParameter(curve.EndParam)
                - curve.GetDistanceAtParameter(curve.StartParam);

                Point3dCollection pts = new Point3dCollection();
                curve.GetStretchPoints(pts);
                
                if (splitacrc != 0)
                {
                    double d = 0;

                    if (!with_first_point)
                        d = curve.GetDistAtPoint(pts[1]);

                    res = DE.NumericArray((int)Math.Floor(d * splitacrc / len), splitacrc)
                        .Select(i => curve.GetPointAtDist(i * len / splitacrc).ToPos()).ToArray();
                }
                else
                    res = DE.NumericArray(0, pts.Count - 1)
                        .Where(i => i == 0 || pts[i].DistanceTo(pts[i - 1]) >= 500)
                        .Select(i => pts[i].ToPos()).ToArray();

                tr.Commit();

            }
            return res.ToArray();
        }

        public static pPos[] GetTextNearPoint(this Database db, IEnumerable<pPos> pts)
        {
            db.GetEntities(null, EN_SELECT.AC_DXF, "MTEXT", "TEXT");
            pPos[] txts = IR.SelectedIds.ToList().Select(id => db._getPoint(id)).ToArray(); ;

            List<pPos> res = new List<pPos>();
            foreach (pPos pt in pts)
            {
                int index = Array.FindIndex(txts, txt => pt._isVeryClosed(txt, 3000));
                res.Add(index != -1 ? txts[index] : null);
            }

            return res.ToArray();
        }

        public static string[] AllXNotes(this Database db,ObjectIdCollection ids)
        {
            return ids.ToList().SelectMany(id => db.GetXNotes(id)).ToArray();
        }

        public static pPos _getScale(this Database db, ObjectId blockID)
        {
            pPos res = new pPos(1,1,1);
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference block = (BlockReference)tr.GetObject(blockID, OpenMode.ForRead);
                //if (block.ScaleFactors.X != 1 || block.ScaleFactors.Y != 1)
                    res = new pPos(block.ScaleFactors.X, block.ScaleFactors.Y, block.ScaleFactors.Z);
                tr.Commit();
            }
            return res;
        }

        public static void _setScale(this Database db, ObjectId blockID, double scaleX, double scaleY)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference block = (BlockReference)tr.GetObject(blockID, OpenMode.ForWrite);
                block.ScaleFactors = new Scale3d(scaleX, scaleY, 1);
                tr.Commit();
            }
        }

        public static double _getRotation(this Database db, ObjectId objId)
        {
            double res = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (db._isBlock(objId))
                {
                    BlockReference block = (BlockReference)tr.GetObject(objId, OpenMode.ForRead);
                    res = block.Rotation * 180 / Math.PI;
                }
                else if (objId.ObjectClass.DxfName == "ELLIPSE")
                {
                    Ellipse elp = (Ellipse)tr.GetObject(objId, OpenMode.ForRead);
                    res = elp.StartPoint.ToPos().AngleVector(elp.Center.ToPos(), elp.StartPoint.ToPos() + new pPos(1, 0));
                }
                else if (objId.ObjectClass.DxfName == "DIMENSION")
                {
                    RotatedDimension rot_dim = (RotatedDimension)tr.GetObject(objId, OpenMode.ForRead);
                    if (rot_dim != null)
                        res = rot_dim.Rotation * 180 / Math.PI;
                }
                else if (objId.ObjectClass.DxfName == "MTEXT")
                {
                    MText mtxt = (MText)tr.GetObject(objId, OpenMode.ForRead);
                    res = mtxt.Rotation / Math.PI * 180;
                }
                else if (objId.ObjectClass.DxfName == "TEXT")
                {
                    DBText txt = (DBText)tr.GetObject(objId, OpenMode.ForRead);
                    res = txt.Rotation / Math.PI * 180;
                }
                tr.Commit();
            }
            
            return res;
        }

        public static void _setRotations(this Database db, ObjectIdCollection ids, double angle)
        {
            foreach (ObjectId objId in ids)
                db._setRotation(objId, angle);
        }

        
        public static void _setRotation(this Database db, ObjectId objId, double angle)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                double ang = Math.PI * angle / 180;
                switch (objId.ObjectClass.DxfName)
                {
                    case "MTEXT":
                        MText mtxt = (MText)tr.GetObject(objId, OpenMode.ForWrite);
                        mtxt.Rotation = ang;
                        break;
                    case "TEXT":
                        DBText txt = (DBText)tr.GetObject(objId, OpenMode.ForWrite);
                        txt.Rotation = ang;
                        break;
                    
                    case "INSERT":
                        BlockReference block = (BlockReference)tr.GetObject(objId, OpenMode.ForWrite);
                        block.Rotation = ang;
                        break;

                }

                tr.Commit();
            }
        }

        public static double _getArea(this Database db, ObjectId id)
        {
            double res = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (db._isPolyline(id))
                {
                    Polyline ent = (Polyline)tr.GetObject(id, OpenMode.ForWrite);
                    res = ent.Area;
                }
                else if (db._isHatch(id))
                {
                    Hatch ent = (Hatch)tr.GetObject(id, OpenMode.ForWrite);
                    double area = 0;
                    int total = ent.NumberOfLoops;

                    for (int i = 0; i < total; i++)
                    {
                        HatchLoop loop = ent.GetLoopAt(i);
                        List<pPos> ls = new List<pPos>();

                        if (loop.IsPolyline)
                        {
                            BulgeVertexCollection bvc = loop.Polyline;
                            foreach (BulgeVertex bv in bvc)
                            { 
                                ls.Add(new pPos(bv.Vertex.X, bv.Vertex.Y));
                                res += ls.Area();
                            }
                        }
                        else
                        {
                            Curve2dCollection c2c = loop.Curves;
                            foreach (Curve2d c2 in c2c) res += c2.GetArea(0, 1);
                            //ACD.WR("CURVE2D = {0}", res);
                        }
                    }
                }
                tr.Commit();
            }

            //ACD.WR("RESULT_AREA = {0}", res);
            return res;
        }

        /*public static string _getLineworkInfo(this Database db, ObjectId id)
        {
            string res = "";
            LinetypeTableRecord ltype;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (db._isPolyline(id))
                {
                    Polyline lwp = (Polyline)tr.GetObject(id, OpenMode.ForWrite);
                    
                    if (lwp != null)
                        res = String.Format("LWIDTH={0}", lwp.ConstantWidth);
                }
                
                Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                if (ent != null)
                {
                    ltype = (LinetypeTableRecord)tr.GetObject(ent.LinetypeId, OpenMode.ForRead);
                     res += String.Format("LAYER={0}|LTYPE={1}|LSCALE={2}|LCOLOR={3}|HYPERLINK={4}",
                        ent.Layer, ltype.Name, ent.LinetypeScale, ent.ColorIndex, db._getHyperlink(id));
                }
                
                tr.Commit();
            }
            return res;
        }*/

        public static double _getLineweight(this Database db, ObjectId objId)
        {
            double res = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (db._isPolyline(objId))
                {
                    Polyline poly = (Polyline)tr.GetObject(objId, OpenMode.ForRead);
                    if(poly.HasWidth) res = poly.ConstantWidth;
                }
                tr.Commit();
            }

            return res;
        }
        
        public static ObjectIdCollection _seperateHatches(this Database db, ObjectId objId)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                if (db._isHatch(objId))
                {
                    Hatch ent = (Hatch)tr.GetObject(objId, OpenMode.ForWrite);
                    int total = ent.NumberOfLoops;

                    if (total == 1)
                        res.Add(objId);
                    else
                    {
                        for (int i = 0; i < total; i++)
                        {
                            HatchLoop loop = ent.GetLoopAt(i);
                            Hatch new_ent = new Hatch();
                            btr.AppendEntity(new_ent);
                            tr.AddNewlyCreatedDBObject(new_ent, true);

                            try
                            {
                                new_ent.PatternScale = ent.PatternScale;
                                new_ent.SetHatchPattern(ent.PatternType, ent.PatternName);
                            }
                            catch (System.Exception ex)
                            {
                                new_ent.PatternScale = DE.DEF_HATCH_SCALE;
                                new_ent.SetHatchPattern(HatchPatternType.PreDefined, DE.DEF_HATCH_PATTERN);
                            }

                            new_ent.PatternAngle = ent.PatternAngle;
                            new_ent.PatternDouble = ent.PatternDouble;

                            try
                            {
                                new_ent.AppendLoop(loop);

                                new_ent.Associative = ent.Associative;
                                new_ent.EvaluateHatch(true);
                            }
                            catch (System.Exception ex)
                            {
                                //ACD.WR("Error {0},{1}", ex.StackTrace, ex.Message);
                            }

                            //db._setLayer(new_ent.ObjectId, db._getLayer(objId));
                            res.Add(new_ent.ObjectId);
                        }
                        //db.EraseObjects(objId);
                    }
                }
                tr.Commit();
            }

            return res;
        }
        
        static bool _allDistance(pPos[] pts)
        {
            bool res = true;

            for (int i = 0; i < pts.Length - 1; i++)
                for (int j = i + 1; j < pts.Length; j++)
                {
                    if (pts[i].DistanceTo(pts[j]) < 1)
                    {
                        res = false;
                        break;
                    }
                }
            return res;
        }

        public static ObjectId _getLeaderStyle(this Database db, string styleName)
        {
            ObjectId res = ObjectId.Null;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBDictionary mlStyles = (DBDictionary)tr.GetObject(db.MLeaderStyleDictionaryId, OpenMode.ForRead);
                //ACD.WR("LEADER_ID {0},{1}",styleName, mlStyles.Contains(styleName));
                if (mlStyles.Contains(styleName))
                    res = mlStyles.GetAt(styleName);
                tr.Commit();
            }

            return res;
        }

        public static ObjectId _getTextStyle(this Database db, string styleName)
        {
            ObjectId res = ObjectId.Null;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                TextStyleTable mlStyles = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                if (mlStyles.Has(styleName))
                    res = mlStyles[styleName];
                tr.Commit();
            }

            return res;
        }

        public static ObjectId _getTableStyle(this Database db, string styleName)
        {
            ObjectId res = ObjectId.Null;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBDictionary tabStyles = (DBDictionary)tr.GetObject(db.TableStyleDictionaryId, OpenMode.ForRead);
                if (tabStyles.Contains(styleName))
                    res = tabStyles.GetAt(styleName);
                tr.Commit();
            }

            return res;
        }

        public static string _hasLeader(this Database db, IEnumerable<pPos> pts, ObjectIdCollection selIds = null)
        {
            string res = null;
            pPos p1 = pts.First(), p2 = pts.Last();
            ObjectIdCollection leaderIds = new ObjectIdCollection();
            ObjectIdCollection txtIds = new ObjectIdCollection();

            if (selIds == null)
            {
                db.GetEntities(pts.Rect().Offset(1000), EN_SELECT.AC_DXF, "LINE", "LWPOLYLINE");
                leaderIds.AddRange(IR.SelectedIds);

                db.GetEntities(pts.Rect().Offset(1000), EN_SELECT.AC_DXF, "TEXT", "MTEXT");
                txtIds.AddRange(IR.SelectedIds);
            }
            else
            {
                leaderIds = db.FilterIds(selIds, "LINE", "LWPOLYLINE");
                txtIds = db.FilterIds(selIds, "TEXT", "MTEXT");
            }

            if (leaderIds.Count > 0)
            {
                leaderIds = leaderIds.Cast<ObjectId>().Where(id => !db._isLeaders(db._getVertices(id), txtIds).empty())
                    .Select(id => id).ToCollection();

                foreach (ObjectId id in leaderIds)
                {
                    pPos[] r = db._getBound(id).Rect();
                    if (p1.DistanceToPts(r) < 100 || p2.DistanceToPts(r) < 100)
                    {
                        res = null;
                        break;
                    }
                }
            }
            //ACD.WR("TXT_IDS {0} RESULT {1}", txtIds.Count, res);
            return res;
        }

        public static string _isLeaders(this Database db, IEnumerable<pPos> pts, ObjectIdCollection selIds = null)
        {
            string res = null;
            pPos p1 = pts.First(), p2 = pts.Last();
            ObjectIdCollection txtIds = new ObjectIdCollection();

            if (selIds == null)
            {
                db.GetEntities(pts.Rect().Offset(1000), EN_SELECT.AC_DXF, "CIRCLE", "MTEXT", "TEXT");
                txtIds.AddRange(IR.SelectedIds);
            }
            else
                txtIds = db.FilterIds(selIds, "CIRCLE", "MTEXT", "TEXT");
            
            foreach(ObjectId id in txtIds)
            {
                pPos[] r = db._getBound(id).Rect();
                if (p1.DistanceToPts(r) < 100 || p2.DistanceToPts(r) < 100)
                {
                    if(db._isText(id))
                        res = db._getContent(id);
                    else if (db._isCircle(id))
                    {

                    }
                    break;
                }
            }
            
            //ACD.WR("TXT_IDS {0} RESULT {1}", txtIds.Count, res);
            return res;
        }

        public static bool _isLeader(this Database db, ObjectId id)
        {
            return id._isLeader();
        }

        public static bool _isLeader(this ObjectId id)
        {
            return id.ObjectClass.DxfName == "LEADER" || id.ObjectClass.DxfName == "MULTILEADER";
        }

        public static bool _isFINObject(this Database db, ObjectId id)
        {
            return id.IsValid && !db._isGrid(id)
                && !id._isDim() && !db._isGObject(id) && !db._isFurniture(id);
        }

        public static void _setLayerColor(this Database db, string sLayerName, short colorindex)
        {
            using (Transaction acTrans = db.TransactionManager.StartTransaction())
            {
                LayerTable acLyrTbl = acTrans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                Autodesk.AutoCAD.Colors.Color color = Autodesk.AutoCAD.Colors.Color.FromColorIndex
                    (Autodesk.AutoCAD.Colors.ColorMethod.ByAci, colorindex);

                if (acLyrTbl.Has(sLayerName))
                {
                    // Open the layer if it already exists for write
                    LayerTableRecord acLyrTblRec = acTrans.GetObject(acLyrTbl[sLayerName], OpenMode.ForWrite) as LayerTableRecord;

                    // Set the color of the layer
                    acLyrTblRec.Color = color;
                }

                acTrans.Commit();
            }
        }

        public static void _setLayerLineType(this Database db, string sLayerName, string typename)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable acLyrTbl = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                if (acLyrTbl.Has(sLayerName))
                {
                    var linetypelib = tr.GetObject(db.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;

                    foreach (ObjectId oid in linetypelib)
                    {
                        var linetypeRecord = tr.GetObject(oid, OpenMode.ForRead) as LinetypeTableRecord;

                        if (linetypeRecord.Name.ct_(typename))
                        {
                            LayerTableRecord acLyrTblRec = tr.GetObject(acLyrTbl[sLayerName], OpenMode.ForWrite) as LayerTableRecord;
                            acLyrTblRec.LinetypeObjectId = linetypeRecord.ObjectId;

                            break;
                        }
                    }
                }

                tr.Commit();
            }
        }


        public static short _getLayerColor(this Database db, string sLayerName)
        {
            short res = 0;
            using (Transaction acTrans = db.TransactionManager.StartTransaction())
            {
                LayerTable acLyrTbl = acTrans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                
                if (acLyrTbl.Has(sLayerName))
                {
                    LayerTableRecord acLyrTblRec = acTrans.GetObject(acLyrTbl[sLayerName], OpenMode.ForWrite) as LayerTableRecord;
                    res = acLyrTblRec.Color.ColorIndex;
                }
                acTrans.Commit();
            }

            return res;
        }

        //public static bool _isStair(this Database db, ObjectIdCollection ids)
        //    //In result list <Fisrt> Leader <Next-To-End> Steps Regions
        //{
        //    bool res = false;
        //    int splitarc = (int)pString.INI_String("SPLITARC").ToNumber();

        //    if (ids.Cast<ObjectId>().All(id => db._isBlock(id)))
        //    {
        //        foreach (ObjectId id in ids)
        //        {
        //            db.GetEntities(id);
        //            res = polylines_leader_in_object.Count > 0;
        //        }
        //    }else { 
        //        PosCollection leaders =  db.FilterIds(ids, IR._isLeader)
        //            .Cast<ObjectId>().Select(id => db._getVertices(id, splitarc))
        //            .OrderBy(pts => - pts.Length).ToCollection();

        //        res = leaders.Count > 0;
        //    }

        //    return res;
        //}

        //public static PosCollection GetStair(this Database db, ObjectIdCollection ids)
        ////In result list <Fisrt> Leader <Next-To-End> Steps Regions
        //{
        //    PosCollection res = new PosCollection();
        //    pPos[] leaderPts = null;
        //    PosCollection regions = new PosCollection();
        //    int splitarc = (int)pString.INI_String("SPLITARC").ToNumber();

        //    if (ids.Cast<ObjectId>().All(id => db._isBlock(id)))
        //    {
        //        db.GetEntities(ids.First());
        //        if (polylines_leader_in_object.Count > 0)
        //        {
        //            //regions = polylines_in_object.RoomCycleResult();
        //            leaderPts = polylines_leader_in_object.First.ExtentLine(200);
        //        }
        //    }
        //    else
        //    {
        //        PosCollection leaders = db.FilterIds(ids, IR._isLeader)
        //            .Cast<ObjectId>().Select(id => db._getVertices(id, splitarc))
        //            .OrderBy(pts => -pts.Length).ToCollection();

        //        if (leaders.Count > 0)
        //        {
        //            ObjectIdCollection lwpIds = db.FilterIds(ids, IR._isPolyline);
        //            leaderPts = leaders.First.ExtentLine(200);
        //            //regions = db.RoomCycleResult(lwpIds);
        //        }
        //    }

        //    if (leaderPts != null && regions.Count > 0)
        //    {
        //        regions = regions.Where(ls => 
        //            new PosCollection() { ls, leaderPts }.Intersect().Length > 0).ToCollection();

        //        if (regions.Count > 0)
        //        {
        //            res.Add(leaderPts);
        //            res.AddRange(regions);
        //        }
        //    }
        //    return res;
        //}

        //public static void _updateEntity(this Database db, ObjectIdCollection ids)
        //{
        //    foreach (ObjectId id in ids)
        //        db._updateEntity(id);
        //}

        public static void _updateEntity(this Database db, ObjectId id)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                ent.UpgradeOpen();
                tr.Commit();
            }
        }

        public static int _getColorIndex(this Database db, ObjectId objId)
        {
            int res = -1;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity obj = (Entity)tr.GetObject(objId, OpenMode.ForRead);
                res = obj.ColorIndex;
                tr.Commit();
            }
            return res;
        }

        public static string _getHyperlink(this Database db, ObjectId objId)
        {
            string res = "";
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity obj = (Entity)tr.GetObject(objId, OpenMode.ForRead);
                if(obj.Hyperlinks.Count > 0) res = obj.Hyperlinks[0].Name;
                tr.Commit();
            }
            return res;
        }
        
        public static ObjectIdCollection _getWallOpeningIds(this Database db, ObjectId entId)
        {
            ObjectIdCollection res = null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (entId.ObjectClass.DxfName == "AEC_WALL")
                {
                    Autodesk.Aec.Arch.DatabaseServices.Wall wall
                        = (Autodesk.Aec.Arch.DatabaseServices.Wall)tr.GetObject(entId, OpenMode.ForRead);
                    if (wall != null) res = wall.GetOpeningsFor();
                }
                tr.Commit();
            }
            return res;
        }

        public static pPos[] _getWallOpeningSize(this Database db, ObjectId entId)
        {
            ObjectIdCollection openIds = db._getWallOpeningIds(entId);
            return openIds.Cast<ObjectId>().Select(id => db._getObjectSize(id)).ToArray();
        }

        //public static pPos[] _getPolylineAround(this Database db, pPos pt)
        //{
        //    pPos[] res = null;
            
        //    GetEntities(db, null, EN_SELECT.AC_DXF, "LWPOLYLINE");

        //    using (Transaction tr = db.TransactionManager.StartTransaction())
        //    {
        //        foreach (ObjectId id in _selectedIds)
        //        {
        //            pPos[] pls = _getVertices(db, id);
        //            if (pt.Inside(pls))
        //            {
        //                res = pls;
        //                break;
        //            }
        //        }
        //        tr.Commit();
        //    }
        //    return res;
        //}



        //public static double _getBoundArea(this Database db, ObjectId id, double offset = 0)
        //{
        //    pPos[] bb = _getBound(db, id, offset);
        //    return (bb[1].X - bb[0].X) * (bb[1].Y - bb[0].Y);
        //}



        public static pPos _getObjectSize(this Database db, ObjectId objId)
        {
            pPos res = null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (objId.ObjectClass.DxfName == "AEC_DOOR")
                {
                    AEC.Door obj = (AEC.Door)tr.GetObject(objId, OpenMode.ForRead);
                    res = new pPos(obj.Width, obj.Height);
                }
                else if (objId.ObjectClass.DxfName == "AEC_WINDOW")
                {
                    AEC.Window obj = (AEC.Window)tr.GetObject(objId, OpenMode.ForRead);
                    res = new pPos(obj.Width, obj.Height);
                }

                //ACD.WR("Object {0} Size {1}x{2}", objId.ObjectClass.DxfName, res[0], res[1]);
                        
                tr.Commit();
            }
            return res;

        }

        public static void _setIdBound(this Database db, ObjectId objId, params pPos[] new_bb)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                pPos[] bb = db._getBound(objId);
                if(bb != null)
                {
                    BlockReference obj = (BlockReference)tr.GetObject(objId, OpenMode.ForWrite);

                    double w = (new_bb[1].X - new_bb[0].X) / (bb[1].X - bb[0].X);
                    double h = (new_bb[1].Y - new_bb[0].Y) / (bb[1].Y - bb[0].Y);

                    /*double[] dMatrix = new double[16]{w,0,0,0,
                                                    0,h,0,0,
                                                    0,0,1,0,
                                                    bb[0].X,bb[0].Y,bb[0].Z,1};

                    Matrix3d acMat3d = new Matrix3d(dMatrix);
                    obj.TransformBy(acMat3d);*/

                    obj.ScaleFactors = new Scale3d(w,h,1);
                }
                
                tr.Commit();
            }
        }

        
        public static pPos _getDirection(this Database db, ObjectId id)
        {
            double rot = db._getRotation(id)  / 180 * Math.PI ;

            pPos res = new pPos(  Math.Sin(rot), - Math.Cos(rot)).Normalize;
           
            return res;
        }

        public static string[] _readTableData(this Database db, string _title)
        {
            List<string> res = new List<string>();
            db.GetEntities(null, EN_SELECT.AC_DXF, "ACAD_TABLE");

            //ACD.WR("TABLE 01");
            if (IR.SelectedIds.Count > 0)
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Table itm_tb = (Table)tr.GetObject(IR.SelectedIds[0], OpenMode.ForWrite);
                    string title = itm_tb.GetTextString(0, 0, 0);

                    if (title.Upper() == _title.Upper())
                        for (int j = 1; j < itm_tb.NumRows; j++)
                        {
                            string st = "";
                            bool has_value = false;
                            for (int i = 0; i < itm_tb.NumColumns; i++)
                            {
                                string stmp = itm_tb.GetTextString(j, i, 0);
                                if (stmp == null || stmp == "")
                                    stmp = "#";
                                else if (i > 1)
                                    has_value = true;
                                st += stmp + "|";
                            }
                            if(has_value) res.Add(st);
                        }
                    
                    tr.Commit();
                }
            //ACD.WR("TABLE 03{0}",res.Count);
            return res.ToArray();
        }

        public static string[] _readTableData(string filename, string _title)
        {
            string[] res = null;
            using (Database db = ACD.ReadDWG(filename))
            {
                res = _readTableData(db, _title);
            }
            return res;
        }




        //---------------------------------------------------------------------------------------------------------------------
        
    }

    public class WallOpeningDataCls
    {
        public PosCollection WallPoints, HeadOpeningPoints, SillOpeningPoints;
        List<pPos> wdPoints, drPoints;
        public pPos[] WallBoundPoints;
        public double w;
        bool _aligncenter;
        pPos _wallp1, _wallp2;
        AEC.Wall wall;

        public WallOpeningDataCls(Database db, ObjectId _wallId)
        {
            WallPoints = new PosCollection();
            HeadOpeningPoints = new PosCollection();
            SillOpeningPoints = new PosCollection();
            wdPoints = new List<pPos>();
            drPoints = new List<pPos>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                wall = (AEC.Wall)tr.GetObject(_wallId, OpenMode.ForWrite);
                if (wall != null)
                {
                    ObjectIdCollection doorIds = new ObjectIdCollection();// db._getWallOpeningIds(wall.ObjectId);
                    _wallp1 = wall.StartPoint.ToPos();
                    _wallp2 = wall.EndPoint.ToPos();
                    _aligncenter = wall.JustificationType == AEC.WallJustificationType.Center;
                    w = wall.Width;

                    foreach (ObjectId drId in doorIds)
                    {
                        pPos[] pts = db._getVertices(drId);
                        if (pts.Length >= 2)
                            for (int i = 0; i < 2; i++)
                            {
                                drPoints.Add(pts[i].ProjectLine(_wallp1, _wallp2));
                                if (drId.ObjectClass.DxfName == "AEC_WINDOW")
                                    wdPoints.Add(pts.CenterPoint());
                            }
                    }

                    drPoints = drPoints.OrderBy(pt => pt.DistanceTo(_wallp1)).ToList();
                    drPoints.Insert(0, _wallp1);
                    drPoints.Add(_wallp2);

                    _updateWallW(db);
                }

                _updateWallBoundPoints(db);
                _updateAllPoints(db);
                tr.Commit();
            }
        }

        void _updateWallW(Database db)
        {
            if (wall.WallGeometryType == AEC.WallType.Linear)
            {
                if (!_aligncenter)
                {
                    pPos midpt = wall.MidPoint.ToPos();

                    if (_wallp2.Y != _wallp1.Y)
                        wall.Set(_wallp1.ToPoint3(), new Point3d(_wallp2.X, _wallp1.Y, 0), Vector3d.ZAxis);

                    pPos[] bb = db._getBound(wall.ObjectId);
                    pPos[] new_line = _wallp1.Parallel(_wallp2, w / 2);

                    if (!new_line.CenterPoint().InsideRect(bb[0], bb[1]))
                        w = -w;

                    wall.Set(_wallp1.ToPoint3(), _wallp2.ToPoint3(), Vector3d.ZAxis);
                }
            }
        }

        void _updateWallBoundPoints(Database db)
        {
            pPos[] bb = db._getBound(wall.ObjectId);
            if (wall.WallGeometryType == Autodesk.Aec.Arch.DatabaseServices.WallType.Linear)
            {
                pPos[] new_line1, new_line2;
                if (_aligncenter)
                {
                    new_line1 = _wallp1.Parallel(_wallp2, w / 2);
                    new_line2 = _wallp1.Parallel(_wallp2, -w / 2);
                }
                else
                {
                    new_line1 = new pPos[] { _wallp1, _wallp2 };
                    new_line2 = _wallp1.Parallel(_wallp2, w);

                    if (!new pPos[] { new_line1[0], new_line1[1], new_line2[1], new_line2[0] }.CenterPoint().Inside(bb))
                    {
                        new_line2 = _wallp1.Parallel(_wallp2, -w);
                    }
                }
                WallBoundPoints = new pPos[] { new_line1[0], new_line1[1], new_line2[1], new_line2[0] };
            }
            else
            {
                pPos mid = wall.MidPoint.ToPos();


                //using (Transaction tr = db.TransactionManager.StartTransaction())
                //{
                //    ObjectIdCollection cloneIds = db.ExplodeAEC(new ObjectIdCollection { db.CloneObjects(wall.ObjectId) });
                //    ObjectIdCollection newIds = db.ExplodeEntity(cloneIds);

                //    ObjectIdCollection ids = newIds.Cast<ObjectId>()
                //        .Where(id => id.ObjectClass.DxfName == "ARC")
                //        .Select(id => id).OrderBy(id => db._getRadius(id)).ToCollection();

                //    if (ids.Count > 0)
                //    {
                //        List<pPos> pts = db._getVertices(ids.First()).ToList();
                //        pts.AddRange(db._getVertices(ids.Last()).Reverse());
                //        WallBoundPoints = pts.ToArray();
                //        //db.DrawPolyline(pts);

                //        db.EraseObjects(cloneIds);
                //        db.EraseObjects(newIds);
                //    }
                //    tr.Commit();
                //}
            }
        }

        void _updateAllPoints(Database db)
        {
            pPos[] new_line1, new_line2;
            pPos[] bb = db._getBound(wall.ObjectId);

            for (int i = 0; i < drPoints.Count - 1; i++)
            {
                if (_aligncenter)
                {
                    new_line1 = drPoints[i].Parallel(_wallp2, w / 2);
                    new_line2 = drPoints[i + 1].Parallel(_wallp2, -w / 2);
                }
                else
                {
                    new_line1 = new pPos[] { drPoints[i], drPoints[i + 1] };
                    new_line2 = drPoints[i].Parallel(drPoints[i + 1], w);

                    if (!new pPos[] { new_line1[0], new_line1[1], new_line2[1], new_line2[0] }.CenterPoint().Inside(bb))
                        new_line2 = drPoints[i].Parallel(drPoints[i + 1], -w);
                }

                pPos[] pts = new pPos[] { new_line1[0], new_line1[1], new_line2[1], new_line2[0] };

                if (i % 2 == 0)
                    WallPoints.Add(pts);
                else
                {
                    HeadOpeningPoints.Add(pts);
                    if (wdPoints.Any(p => p.Inside(pts)))
                        SillOpeningPoints.Add(pts);
                }
            }
        }
    }

    public static class MoreIR
    {

        public static string Snapshot(this ObjectIdCollection ids, string fname = null,
            pPos[] bb = null, int width = 2000,int height = 2000, double zoom_scale = 0)
        {
            Autodesk.AutoCAD.GraphicsSystem.Manager gsm = ACD.DOC.GraphicsManager;
            Autodesk.AutoCAD.GraphicsSystem.View gsv = ACD.DOC.GraphicsManager.GetCurrentAcGsView(0);

            using (var view = new Autodesk.AutoCAD.GraphicsSystem.View())
            {
                var cvport = (short)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("CVPORT");
                ACD.DOC.GraphicsManager.SetViewFromViewport(view, cvport);
                view.SetView(new Point3d(0, 0, 1), Point3d.Origin, Vector3d.XAxis, DE.AC_VIEWSIZE.X, DE.AC_VIEWSIZE.Y);
                var descriptor = new Autodesk.AutoCAD.GraphicsSystem.KernelDescriptor();
                descriptor.addRequirement(Autodesk.AutoCAD.UniqueString.Intern("3D Drawing"));
                Autodesk.AutoCAD.GraphicsSystem.GraphicsKernel kernel = Autodesk.AutoCAD.GraphicsSystem.Manager.AcquireGraphicsKernel(descriptor);
                Autodesk.AutoCAD.GraphicsSystem.Device dev = gsm.CreateAutoCADOffScreenDevice(kernel);

                using (dev)
                {
                    dev.OnSize(new System.Drawing.Size(width, height));// (int)DE.AC_VIEWSIZE.X, (int)DE.AC_VIEWSIZE.X));
                    dev.DeviceRenderType = Autodesk.AutoCAD.GraphicsSystem.RendererType.Default;
                    dev.BackgroundColor = System.Drawing.Color.White;
                    dev.Add(view);
                    dev.Update();

                    Point2d screen = (Point2d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("SCREENSIZE");
                    int screen_w = (int)screen.X;
                    int screen_h = (int)screen.Y;

                    using (Autodesk.AutoCAD.GraphicsSystem.Model model = gsm.CreateAutoCADModel(kernel))
                    {
                        using (Transaction tr = ACD.DB.TransactionManager.StartTransaction())
                        {
                            BlockTable bt = tr.GetObject(ACD.DB.BlockTableId, OpenMode.ForRead) as BlockTable;
                            BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                            if (bb == null) bb = ACD.DB._getBound(ids);
                            //Line ln = new Line(bb[0].ToPoint3(), bb[1].ToPoint3());

                            foreach (ObjectId id in ids)
                                if (ACD.DB.ValidId(id))
                                {
                                    Entity ent = (Entity)tr.GetObject(id, OpenMode.ForRead);
                                    view.Add(ent, model);
                                }

                            view.ZoomExtents(bb[0].ToPoint3(), bb[1].ToPoint3());
                            
                            if(zoom_scale != 0)
                                view.Zoom(zoom_scale);


                            ACD.WR("View[{0},{1},{2},{3}]", view.FieldWidth, view.FieldHeight, 
                                view.FieldWidth / screen_h, view.FieldHeight / screen_w);
                            
                            

                            tr.Commit();
                        }
                    }



                    if (fname == null)
                        fname = ACD.TempImgName;

                    System.Drawing.Bitmap bmp = view.GetSnapshot(new System.Drawing.Rectangle(0, 0, (int)width, (int)height));//.MakeGrayScale();


                    
                    //bmp.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipNone);
                    bmp.Save(fname);

                    dev.Dispose();
                }
                view.Dispose();
            }

            return fname;
        }

        public static double GetViewportZoom(this Database db, ObjectId vpId)
        {
            double res = 0;
            using(Transaction tr = db.TransactionManager.StartTransaction())
            {
                Viewport view = (Viewport)tr.GetObject(vpId, OpenMode.ForRead);
                res = view.CustomScale;
            }

            return res;
        }

        public static bool _isGrid(this Database db, ObjectId objId)
        {
            return db._isBlock(objId) && db._getIdName(objId).st_("GRID");
        }
    }


    public static class MethodEx
    {


        public static pPos[] GetMinDimArea(this PosCollection wallpts, pPos txt_pt, int axis, pPos base_vector)
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

        //public static void ShowAreaText(this PosCollection wallpts, pPos txt_pt, double rotation)
        //{
        //    pPos base_vector = new pPos(Math.Cos(rotation), Math.Sin(rotation));
        //    List<pPos> res = new List<pPos>();

        //    for (int axis = 0; axis < 2; axis++)
        //        res.AddRange(GetMinDimArea(wallpts, txt_pt, axis, base_vector));

        //    if (res.Count >= 4)
        //    {
        //        double n = res[0].DistanceTo(res[1]) * res[2].DistanceTo(res[3]);

        //        ACD.DB.CreateText("#C" + (n / 1000000).roundNumber(0.1) + "m\\U+00B2", res.CenterPoint(), 2);

        //        if (rotation == 0)
        //        {
        //            pPos[] bb = res.Boundary();
        //            IDimChain.CreateDimension(ACD.DB, new pPos(bb[0].X, (bb[0].Y + bb[1].Y) / 2),
        //                new pPos(bb[1].X, (bb[0].Y + bb[1].Y) / 2), 200, 200);
        //            IDimChain.CreateDimension(ACD.DB, new pPos((bb[0].X + bb[1].X) / 2, bb[0].Y),
        //                new pPos((bb[0].X + bb[1].X) / 2, bb[1].Y), 200, 200);
        //        }
        //        else
        //        {
        //            IDimChain.CreateAlignDimension(ACD.DB, res[0], res[1], 200, 200);
        //            IDimChain.CreateAlignDimension(ACD.DB, res[2], res[3], 200, 200);
        //        }
        //    }
        //}

        public static double _getTextRotation(this Database db, ObjectId objId)
        {
            double res = 0;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (objId.ObjectClass.DxfName == "TEXT")
                {
                    DBText txt = (DBText)tr.GetObject(objId, OpenMode.ForRead);
                    res = txt.Rotation;
                }
                else if (objId.ObjectClass.DxfName == "MTEXT")
                {
                    MText txt = (MText)tr.GetObject(objId, OpenMode.ForRead);
                    res = txt.Rotation;
                }
                tr.Commit();
            }
            return res;
        }

        //public static void AddSaveDimPoint(this Database db, ObjectId blockId, pPos pt)
        //{
        //    pPos[] ls = new pPos[] {new pPos(pt.X - 100, pt.Y + 100, pt.Z),
        //        pt, new pPos(pt.X + 100, pt.Y + 100, pt.Z) };

        //    ObjectId lwpId = ACD.DB.DrawPolyline(ls, false, "LAYER=0");
        //    db.SetEntInBlock(blockId, new ObjectIdCollection() { lwpId }, true);
        //}

        //public static ObjectId BuildDefaultBlock(this Database db, pPos ct)
        //{
        //    string blockName = db.uniqueBlockName("D_Block");
        //    ObjectIdCollection ids = new ObjectIdCollection();

        //    ids.Add(db.DrawPolyline(new pPos[] {new pPos(ct.X - 100, ct.Y),
        //            new pPos(ct.X + 100, ct.Y) }, false, "LAYER=0"));
        //    ids.Add(db.DrawPolyline(new pPos[] {new pPos(ct.X, ct.Y - 100),
        //            new pPos(ct.X, ct.Y + 100) }, false, "LAYER=0"));

        //    db.NewBlock(ids, blockName, true, false, ct);
        //    return db.Insert(blockName, ct, "LAYER=Defpoints");
        //}

        public static pPos[] GetDimGroupVertices(this Database db, ObjectId id, out pPos txt_pos)
        {
            List<pPos> pls = new List<pPos>();
            txt_pos = null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Autodesk.Aec.ArchDACH.DatabaseServices.DimensionGroup dimgroup
                    = (Autodesk.Aec.ArchDACH.DatabaseServices.DimensionGroup)tr.GetObject(id, OpenMode.ForWrite);

                Autodesk.Aec.ArchDACH.DatabaseServices.DimensionCollection ddims = dimgroup.Dimensions;
                PosCollection points = new PosCollection();

                for (int i = 0; i < ddims.Count; i++)
                    if (ddims[i] is RotatedDimension)
                    {
                        RotatedDimension dim = (RotatedDimension)ddims[i];
                        if (dim != null)
                        {
                            pPos p1 = dim.XLine1Point.ToPos();
                            if (txt_pos == null)
                                p1.Content = dim.DimLinePoint.ToPos().ToString();

                            pls.Add(p1);

                            pPos p2 = dim.XLine2Point.ToPos();
                            if (i == ddims.Count - 1)
                                pls.Add(p2);
                        }
                    }

                tr.Commit();
            }

            return pls.ToArray();
        }

        //public static ObjectIdCollection BuildLayout2DGrid(this Database db, IEnumerable<pPos> pts,
        //    double round = 1, string layer = "Defpoints", string description = null)
        //{
        //    ObjectIdCollection res = new ObjectIdCollection();
        //    pPos[] bb = pts.Boundary();
        //    List<double[]> xys = pts.ExtractPtsXY(round, round);

        //    ACD.WR("B01");
        //    //ACD.WR("XYS {0},{1}", xys[0].Length, xys[1].Length);

        //    using (Transaction tr = db.TransactionManager.StartTransaction())
        //    {
        //        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
        //        BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

        //        Autodesk.Aec.DatabaseServices.LayoutGrid2d grid2d = new Autodesk.Aec.DatabaseServices.LayoutGrid2d();
        //        grid2d.SetToStandard(db);
        //        grid2d.SetDatabaseDefaults(db);
        //        //ACD.WR("B02");
        //        grid2d.Location = bb[0].ToPoint3();
        //        //ACD.WR("B03");
        //        grid2d.GridType = Autodesk.Aec.DatabaseServices.GridType.UVRectangular;
        //        grid2d.BaseGrid.UAxisLength = bb.Size().X;
        //        grid2d.BaseGrid.ULayoutMode = Autodesk.Aec.DatabaseServices.UVLayoutModeType.Manual;
        //        grid2d.BaseGrid.UStartOffset = 0;
        //        //ACD.WR("Grid00 {0}", xys[0].Length);
        //        grid2d.BaseGrid.AppendGridRow(0);
        //        for (int i = 1; i < xys[1].Length; i++)
        //            grid2d.BaseGrid.AppendGridRow(xys[1][i] - xys[1][0]);
        //        //ACD.WR("Grid01 {0}", xys[1].Length);
        //        grid2d.BaseGrid.UEndOffset = 0;
        //        grid2d.BaseGrid.VAxisLength = bb.Size().Y;
        //        grid2d.BaseGrid.VLayoutMode = Autodesk.Aec.DatabaseServices.UVLayoutModeType.Manual;
        //        grid2d.BaseGrid.VStartOffset = 0;
        //        grid2d.BaseGrid.VEndOffset = 0;
        //        //ACD.WR("B03");
        //        grid2d.BaseGrid.AppendGridColumn(0);
        //        for (int i = 1; i < xys[0].Length; i++)
        //            grid2d.BaseGrid.AppendGridColumn(xys[0][i] - xys[0][0]);

        //        grid2d.BaseGrid.UpdateParameters();

        //        grid2d.Description = description;
        //        res.Add(ms.AppendEntity(grid2d));
        //        tr.AddNewlyCreatedDBObject(grid2d, true);

        //        db._setLayer(res, layer);
        //        //ACD.WR("Grid03");
        //        tr.Commit();
        //    }
        //    //ACD.WR("B04");
        //    return res;
        //}

        public static string _getGrid2DDescription(this Database db, ObjectId id)
        {
            string res = "";
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Autodesk.Aec.DatabaseServices.LayoutGrid2d grid2d = (Autodesk.Aec.DatabaseServices.LayoutGrid2d)
                    tr.GetObject(id, OpenMode.ForRead);
                res = grid2d.Description;
                tr.Commit();
            }
            return res;
        }

        public static void SetDyncBlockProp(this Database db, ObjectId blockID, string propname, string val)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference br = (BlockReference)tr.GetObject(blockID, OpenMode.ForRead);

                //ACD.WR("Dync {0}", br.IsDynamicBlock);
                if ((br != null) && (br.IsDynamicBlock))
                {
                    //ed.WriteMessage("\nDynamic properties for \"{0}\"\n", name);
                    // Get the dynamic block//s property collection           
                    DynamicBlockReferencePropertyCollection pc = br.DynamicBlockReferencePropertyCollection;
                    // Loop through, getting the info for each property           
                    foreach (DynamicBlockReferenceProperty prop in pc)
                        if (prop.PropertyName.Upper() == propname.Upper() && !prop.ReadOnly)
                        {
                            try
                            {
                                object value = null;

                                switch (prop.UnitsType)
                                {
                                    case DynamicBlockReferencePropertyUnitsType.Angular:
                                        value = val.ToNumber();
                                        break;
                                    case DynamicBlockReferencePropertyUnitsType.Area:
                                        value = val.ToNumber();
                                        break;
                                    case DynamicBlockReferencePropertyUnitsType.Distance:
                                        value = val.ToNumber();
                                        break;
                                    case DynamicBlockReferencePropertyUnitsType.NoUnits:
                                        value = val.ToString();
                                        break;
                                }

                                //ACD.WR("\nCurrent value {0}:{1}", prop.PropertyName, prop.UnitsType);
                                prop.Value = value;
                            }
                            catch (SystemException ex)
                            {
                                ACD.WR("\nException: {0}", ex.Message);
                            }
                            break;
                        }

                    pc.Dispose();
                }
                tr.Commit();
            }
        }

    }
}
