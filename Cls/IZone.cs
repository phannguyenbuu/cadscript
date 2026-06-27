using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AEC = Autodesk.Aec.Arch.DatabaseServices;
using Autodesk.AutoCAD.Windows.Data;


namespace AcadScript
{
    public class UserTableDataCLS
    {
        pPos current_pt, basept;
        ObjectIdCollection infoIds;
        Database db;
        public UserTableDataCLS(Database _db, pPos _basept)
        {
            db = _db;
            basept = _basept;
            current_pt = new pPos(0, 0);
            infoIds = new ObjectIdCollection();
        }
        public void AddUserTableData(string title, string[] values, pPos pt, double break_line = 0)
        {
            ObjectIdCollection res = new ObjectIdCollection();

            res.Add(db.DrawPolyline(new pPos(0, 0).Rect(20, -6), true));
            //res.Add(db.DrawPolyline((new pPos(0, -12)).Rect(20, -6), true));
            res.Add(db.CreateText("#M" + title, new pPos(10, -3), 4));

            for (int i = 0; i < values.Length; i++)
            {
                res.Add(db.DrawPolyline(new pPos(0, -(i + 1) * 6).Rect(20, -6), true));
                res.Add(db.CreateText("#M" + values[i], new pPos(10, -9 - i * 6), 4));
            }

            db.MoveObject(res, pt);

            infoIds.AddRange(res);

            if (break_line != 0)
            {
                pt.X = basept.X;
                pt.Y += break_line;
            }
            else
            {
                pt.X += 20;
            }
        }

        public void Insert(double scale)
        {
            string blockname = ACD.DB.uniqueBlockName("infor_");
            ACD.DB.NewBlock(infoIds, blockname, true, false, basept);
            ObjectId infoId = ACD.DB.Insert(blockname, basept);
            ACD.DB._setScale(infoId, scale, scale);
        }
    }

    public static class IZone
    {

        //--------------------------DRAWING ZONES--------------------------------------
        public static PosCollection AllZones;

        public static pPos[] GetZoneFromPoint(this Database db, pPos pt)
        {
            db.GetEntities(null, EN_SELECT.AC_DXF, "INSERT");
            pPos[] zone = null;

            foreach (ObjectId id in IR.SelectedIds)
                if (ACD.DB._isArray(id))
                {
                    ACD.DB.BlockEntitiesAction(id, _ids => {
                        foreach (ObjectId _id in _ids)
                        {
                            pPos[] bb = ACD.DB._getBound(_id);

                            if (pt.InsideRect(bb[0], bb[1]))
                            {
                                zone = bb;
                                break;
                            }
                        }
                    });

                    //ObjectIdCollection zIds = ACD.DB.ExplodeEntity(id);

                    //foreach (ObjectId zId in zIds)
                    //{
                    //    pPos[] bb = ACD.DB._getBound(zId);
                    //    if (pt.InsideRect(bb[0], bb[1]))
                    //    {
                    //        zone = bb;
                    //        break;
                    //    }
                    //}

                    //ACD.DB.EraseObjects(zIds);
                }

            return zone;
        }

        public static void GenerateAllRegions(this Database db)
        {
            db.GetEntities(null, EN_SELECT.AC_DXF, "INSERT");
            string[] ls = IR.SelectedIds.ToList().Where(id => db._isArray(id))
                .SelectMany(id => db.GetXNotes(id)).ToArray();

            AllZones = ls.Select(s =>
            {
                pPos[] pts = new PosCollection(s._firstProp())[0];
                pts[0].Content = s._firstPropName();
                return pts;
            }).ToCollectionSameClosed(false);
        }

        public static pPos[] GetZoneFromPoint(pPos pt)
        {
            PosCollection zone_rect = AllZones.Where(r
                => pt.InsideRect(r[0], r[1])).ToCollectionSameClosed(false);

            pPos[] res = null;

            if (zone_rect.Count > 0)
            {
                res = zone_rect.First();
                //pPos[] new_r = zone_rect.First();
            }

            return res;
        }

        public static PosCollection SetDrawingZones(this Database db, bool update_zone = true)
        {
            PosCollection regions = new PosCollection();
            //DrawingZoneTextIds = new ObjectIdCollection();

            if (update_zone)
            {
                ACD.WR("Update new zones... Select zones...");
                ObjectIdCollection selIds = ACD.GetSelection();
                ObjectIdCollection regionIds = db.FilterIds(selIds, "INSERT");

                regions = db.SetDrawingZonesByIds(regionIds);
            }
            else
            {
                regions = db.GetDrawingZones();
            }

            return regions;
        }


        public static PosCollection ZoneFromIds(this Database db, ObjectIdCollection ids)
        {
            PosCollection regions = new PosCollection();

            foreach (ObjectId id in ids)
            {
                ObjectIdCollection objIds = db.ExplodeEntity(id);
                foreach (ObjectId objId in objIds)
                    regions.Add(db._getBound(objId));

                db.EraseObjects(objIds);
            }

            regions.Closed = regions.Select(ls => true).ToArray();

            return regions;
        }

        public static PosCollection SetDrawingZonesByIds(this Database db, ObjectIdCollection ids)
        {
            PosCollection regions = db.ZoneFromIds(ids);

            db.GetEntities(null, EN_SELECT.AC_DXF, "MTEXT", "TEXT");

            ObjectIdCollection DrawingZoneTextIds = IR.SelectedIds.ToList().Where(id
                => db._getContent(id).StartsWith(".")
                || db._getContent(id).StartsWith(">>>")).ToCollection();

            pPos[] projectInfoTxts = IR.SelectedIds.ToList().Where(id
                => db._getContent(id).StartsWith(">>>")).Select(id => db._getPoint(id)).ToArray();

            if (projectInfoTxts.Length > 0)
            {
                string[] ar = projectInfoTxts.First().Content.Replace(">>>", "").filter("\r\n");

                if (ar.Length > 0)
                    db.SetDrawingProp("PROJECT=" + ar[0]);

                if (ar.Length > 1)
                    db.SetDrawingProp("ADDRESS=" + ar[1]);
                if (ar.Length > 2)
                    db.SetDrawingProp("OWNER=" + ar[2]);
            }

            pPos[] DrawingZoneTextPoints = DrawingZoneTextIds.ToList().Select(id => db._getPoint(id)).ToArray();

            List<string> contents = new List<string>();
            regions = regions.OrderBy(ls => ls.Boundary()[0].Y.roundNumber(100))
                .ThenBy(ls => ls.Boundary()[0].X.roundNumber(100)).ToCollectionSameClosed();

            for (int i = 0; i < regions.Count(); i++)
            {
                pPos[] txts = DrawingZoneTextPoints.Where(p => p.InsideRect(regions[i][0], regions[i][1]))
                    .OrderBy(p => p.Y.roundNumber(100)).ThenBy(p => p.X.roundNumber(100)).ToArray();

                if (txts.Length > 0)
                    regions[i].First().Content = txts.First().Content;

                contents.Add("ZONE" + (i >= 100 ? "" : "0")
                    + (i >= 10 ? "" : "0") + i.ToString()
                    + "=" + regions[i].ToText().Replace("|", "/").Replace("=", "~"));
            }

            db.SetDrawingProp(contents.ToArray());
            return regions;
        }

        public static pPos[] ViewZoneIndex
        {
            get
            {
                pPos[] view_region = ACD.bRect;
                pPos view_ct = ACD.bRect.CenterPoint();

                PosCollection zones = ACD.DB.GetDrawingZones();
                ACD.WR("Zones {0}", zones.Count);

                if (zones.Count > 0)
                {
                    PosCollection pls = zones.Where(ls => view_ct.InsideRect(ls[0], ls[1])).ToCollectionSameClosed();
                    if (pls.Count > 0)
                    {
                        view_region = pls.First();
                        ACD.WR("Zone_Index {0} Name {1}", view_region.ToText(), view_region.First().Content);
                    }
                }

                return view_region;
            }
        }

        public static void SetDrawingProp(this Database db, params string[] prams)
        {
            DatabaseSummaryInfoBuilder builder = new DatabaseSummaryInfoBuilder();

            List<string> curprams = db.GetAllDrawingProp()
                .Where(s => !s._firstPropName().StartsWith("ZONE")).ToList();

            foreach (string st in prams)
            {
                string key = st._firstPropName();
                string val = st._firstProp();

                curprams = curprams._setprops(key, val).ToList();
            }

            foreach (string st in curprams)
            {
                string key = st._firstPropName();
                string val = st._firstProp();

                if (!key.empty() && !val.empty())
                {
                    if (builder.CustomPropertyTable.Contains(key))
                        builder.CustomPropertyTable[key] = val;
                    else
                        builder.CustomPropertyTable.Add(key, val);
                }
            }

            db.SummaryInfo = builder.ToDatabaseSummaryInfo();
        }

        public static string[] GetAllDrawingProp(this Database db)
        {
            DatabaseSummaryInfoBuilder builder = new DatabaseSummaryInfoBuilder();
            System.Collections.IDictionaryEnumerator info = db.SummaryInfo.CustomProperties;

            List<string> res = new List<string>();

            while (info.MoveNext())
                res.Add(info.Key + "=" + info.Value);

            return res.OrderBy(s => s).ToArray();
        }

        public static string GetDrawingProp(this Database db, string key)
        {
            string[] prams = db.GetAllDrawingProp();
            return prams._props(key);
        }

        static int GetDrawingZoneIndex(this Database db, pPos pt)
        {
            int res = -1;
            PosCollection regions = db.GetDrawingZones();

            if (regions.Count > 0)
            {
                pPos[] cts = regions.Select(reg => reg.CenterPoint()).ToArray();
                res = DE.NumericArray(0, regions.Count - 1).OrderBy(n => cts[n].DistanceTo(pt)).First();
            }

            return res;
        }

        public static pPos[] GetDrawingZone(this Database db, pPos pt)
        {
            int index = db.GetDrawingZoneIndex(pt);

            pPos[] res = index != -1 ? db.GetDrawingZones()[index] : null;
            if (res != null)
                res[0].Content = "ZONE" + index;

            return res;
        }

        public static pPos[] GetNextDrawingZoneIndex(this Database db, int index)
        {
            pPos[] res = null;
            PosCollection regions = db.GetDrawingZones();

            if (regions.Count > 0)
            {
                //pPos[] cts = regions.Select(reg => reg.CenterPoint()).ElementAt(index);
                pPos ct = regions[index].CenterPoint();

                regions = regions.OrderBy(ls
                    => ls.Boundary()[0].Y.roundNumber(100)).ThenBy(ls
                    => ls.Boundary()[0].X.roundNumber(100)).ToCollectionSameClosed();

                //int[] tmps = DE.NumericArray(0, regions.Count - 1)
                //    .Where(n => cts[n][axis] > ct[axis] && (cts[n][nex].roundNumber(100) == ct[nex].roundNumber(100))).ToArray();

                int[] tmps = DE.NumericArray(0, regions.Count - 1)
                    .Where(n => ct.Inside(regions[n])).ToArray();

                if (tmps.Length > 0 && tmps.First() < regions.Count)
                    res = regions[tmps.First() + 1];
            }

            return res;
        }

        public static PosCollection GetDrawingZones(this Database db)
        {
            PosCollection zones = new PosCollection();
            string[] prams = db.GetAllDrawingProp().Where(s => s.StartsWith("ZONE")).ToArray();

            foreach (string pram in prams)
            {
                pPos[] ls = new PosCollection(pram._firstProp()).First();
                //ACD.WR("LS {0}", ls.ToText());

                if (ls.Length > 1)
                {
                    if (ls.Length > 2)
                        ls = new pPos[] { ls[0], ls[1] };

                    ls.First().Content = pram._firstPropName() + ls.First().Content;
                    ls.First().Content = ls.First().Content.Replace("/", "|");
                    ls.First().Content = ls.First().Content.Replace("~", "=");
                    //ACD.WR("Content {0},{1}", ls.First().Content, ls.ToText());
                    zones.Add(ls);
                }
            }

            if (zones.Count == 0)
                zones = db.SetDrawingZones();

            return zones;
        }

        public static PosCollection GetDrawingZoneList(this Database db, int start_index, int count)
        {
            PosCollection zones = db.GetDrawingZones();
            zones = zones.OrderBy(ls
                    => ls.Boundary()[0].Y.roundNumber(100)).ThenBy(ls
                    => ls.Boundary()[0].X.roundNumber(100)).ToCollectionSameClosed();

            PosCollection res = new PosCollection();
            for (int i = start_index; i < start_index + count; i++)

                if (i < zones.Count)
                    res.Add(zones[i]);

            return res;
        }

        public static PosCollection GetDrawingZoneListByPoint(this Database db, pPos pt, int count)
        {
            PosCollection zones = db.GetDrawingZones();
            zones = zones.OrderBy(ls
                    => ls.Boundary()[0].Y.roundNumber(100)).ThenBy(ls
                    => ls.Boundary()[0].X.roundNumber(100)).ToCollectionSameClosed();

            PosCollection res = new PosCollection();

            int start_index = -1;

            int[] indx = DE.NumericArray(0, zones.Count - 1).Where(n => pt.Inside(zones[n])).ToArray();

            if (indx.Length > 0)
            {
                start_index = indx.First();
                for (int i = start_index; i < start_index + count; i++)
                    if (i < zones.Count)
                        res.Add(zones[i]);
            }

            return res;
        }

        public static ObjectIdCollection DrawingZoneTextIds;
        public static pPos[] DrawingZoneTextPoints;


        public static string[] ListDisplayConfig(this Database db)
        {
            List<string> res = new List<string>();

            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Autodesk.Aec.DatabaseServices.DisplayRepresentationManager disRepMngr
                        = new Autodesk.Aec.DatabaseServices.DisplayRepresentationManager(db);

                    Autodesk.Aec.DatabaseServices.DisplayConfiguration currentDisplayConfig
                        = disRepMngr.DisplayConfigurationIdForCurrentViewport.GetObject(OpenMode.ForRead)
                        as Autodesk.Aec.DatabaseServices.DisplayConfiguration;

                    Autodesk.Aec.DatabaseServices.DictionaryDisplayConfiguration dictDisplConfigs
                        = new Autodesk.Aec.DatabaseServices.DictionaryDisplayConfiguration(db);

                    foreach (ObjectId dcId in dictDisplConfigs.Records)
                    {
                        Autodesk.Aec.DatabaseServices.DisplayConfiguration dc = tr.GetObject(dcId, OpenMode.ForWrite)
                            as Autodesk.Aec.DatabaseServices.DisplayConfiguration;
                        res.Add(dc.Name);
                        //Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("Name : " + dc.Name + "\n" +
                        //"Description :  " + dc.Description + "\n");
                    }
                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ACD.WR("Error in list display configuration!");
            }
            return res.ToArray();
        }

        public static void SetDisplayConfigCS(this Database db, string key)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                //ACD.DB.TransactionManager.EnableGraphicsFlush(true);
                Autodesk.Aec.DatabaseServices.DisplayRepresentationManager disRepMngr
                    = new Autodesk.Aec.DatabaseServices.DisplayRepresentationManager(db);

                Autodesk.Aec.DatabaseServices.DisplayConfiguration currentDisplayConfig
                    = disRepMngr.DisplayConfigurationIdForCurrentViewport.GetObject(OpenMode.ForRead)
                    as Autodesk.Aec.DatabaseServices.DisplayConfiguration;

                Autodesk.Aec.DatabaseServices.DictionaryDisplayConfiguration dictDisplConfigs
                    = new Autodesk.Aec.DatabaseServices.DictionaryDisplayConfiguration(db);

                string[] displaynames = db.ListDisplayConfig();
                int index = Array.FindIndex(displaynames, s => s.st_(key.Upper()));

                if (index != -1)
                {
                    ObjectId newDisplyConfigId = dictDisplConfigs.Records[index];
                    //ACD.WR("Display {0} {1} {2}", displaynames[index], 
                    //    dictDisplConfigs.GetAt(displaynames[index]), dictDisplConfigs.Records[index]);
                    try
                    {
                        disRepMngr.SetDisplayConfigurationId(db.GetEditor().ActiveViewportId, newDisplyConfigId);
                    }
                    catch (System.Exception)
                    {
                        disRepMngr.DefaultDisplayConfigurationId = newDisplyConfigId;
                    }

                    //currentDisplayConfig = disRepMngr.DisplayConfigurationIdForCurrentViewport.GetObject(OpenMode.ForRead) as DisplayConfiguration;
                    //ed.WriteMessage("\nThe current displayConfig is " + currentDisplayConfig.Name);
                }

                db.TransactionManager.QueueForGraphicsFlush();
                //ACD.ED.Regen();

                //ACD.DOC.SendStringToExecute("_regenall\r\n", true, false, false);

                tr.Commit();
            }
        }


        public static void SetAnnotationScale(this Database db, string scale_name)
        {
            //using (ACD.Lock())
            {
                try
                {
                    //db.GetEntities(ACD.bRect, EN_SELECT.AC_DXF, "MTEXT", "TEXT", "AEC_DIMENSION_GROUP", "DIMENSION");
                    //ObjectIdCollection ids = IR.SelectedIds;

                    Autodesk.AutoCAD.DatabaseServices.ObjectContextManager ocm = db.ObjectContextManager;
                    Autodesk.AutoCAD.DatabaseServices.ObjectContextCollection occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");

                    using (Autodesk.AutoCAD.DatabaseServices.Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        if (!occ.HasContext(scale_name))
                        {
                            //db.WR("\nCannot find current annotation scale.");
                            return;
                        }

                        db.Cannoscale = (Autodesk.AutoCAD.DatabaseServices.AnnotationScale)occ.GetContext(scale_name);

                        Dictionary<string, string> scale_list = pString.INI_String("SCALE_LIST").filter(";")
                            .ToDictionary(s => s.filter("(").First(), s => s._getInComma());

                        string[] display_list = db.ListDisplayConfig();
                        int index = scale_list.Keys.ToList().FindIndex(itm => itm == scale_name);

                        if (index != -1)
                        {
                            string display = scale_list.Values.ElementAt(index);
                            index = display_list.Cast<string>().ToList()
                                .FindIndex(s => s.Upper().Contains(display.Upper()));

                            if (index != -1)
                            {
                                db.SetDisplayConfigCS(display);
                            }
                        }

                        tr.Commit();
                    }

                    //ACD.ED.Regen();
                }
                catch (System.Exception ex)
                {
                    ACD.WR("Error {0},{1}", ex.Message, ex.StackTrace);
                }
            }

            //ACD.Focus();
        }

        //public static string SnapshotLayout(this Database db, string LayoutName, string savename)
        //{
        //    db.GetEntities(LayoutName);
        //    IR.SelectedIds.Snapshot(savename);

        //    return savename;
        //}



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
        static void _addPointToList(List<pPos> pls, pPos pt)
        {
            if (pls.Count == 0 || (pt.DistanceTo(pls.Last()) > 1 && pt.DistanceTo(pls.First()) > 1))
                pls.Add(pt);
        }

        static double _getArcBulge(Arc arc)
        {
            double deltaAng = arc.EndAngle - arc.StartAngle;
            if (deltaAng < 0)
                deltaAng += 2 * Math.PI;
            return Math.Tan(deltaAng * 0.25);
        }

        static int splitarc = 16;

        static pPos[] _getVertices(Polyline lwp)
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

        static Polyline arc2poly(this Database db, Arc arc)
        {
            Polyline poly = null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                int splitarc = (int)pString.INI_String("SPLITARC").ToNumber();

                poly = new Polyline();

                Point2d p1 = new Point2d(arc.StartPoint.X, arc.StartPoint.Y);
                Point2d p2 = new Point2d(arc.EndPoint.X, arc.EndPoint.Y);

                poly.AddVertexAt(0, p2, _getArcBulge(arc), 0, 0);
                poly.AddVertexAt(1, p1, 0, 0, 0);
                poly.Closed = false;

                pPos[] pls = _getVertices(poly);
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
                arc.Erase();

                tr.Commit();
            }
            return poly;
        }

        static Polyline _arcPoints(this Database db, pPos p1, pPos p2, pPos p3, pPos ct, double w)
        {
            pPos ps1 = p1.Along(-w, ct);
            pPos ps2 = p2.Along(-w, ct);
            pPos ps3 = p3.Along(-w, ct);

            return db.arc2poly(db._createArcByThreePoints(ps1.ToPoint3(), ps2.ToPoint3(), ps3.ToPoint3()));
        }

        public static void ZoomBounds(this Database db, params pPos[] bound)
        {
            db.TileMode = true;
            Point2d scrSize = (Point2d)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("screensize");
            double ratio = scrSize.X / scrSize.X;

            using (Transaction acTrans = db.TransactionManager.StartTransaction())
            {
                // Get the current view
                using (ViewTableRecord acView = ACD.ED.GetCurrentView())
                {
                    Extents3d eExtents;

                    // Translate WCS coordinates to DCS
                    Matrix3d matWCS2DCS;
                    matWCS2DCS = Matrix3d.PlaneToWorld(acView.ViewDirection);
                    matWCS2DCS = Matrix3d.Displacement(acView.Target - Point3d.Origin) * matWCS2DCS;
                    matWCS2DCS = Matrix3d.Rotation(-acView.ViewTwist,
                                                    acView.ViewDirection,
                                                    acView.Target) * matWCS2DCS;

                    // If a center point is specified, define the min and max 
                    // point of the extents
                    // for Center and Scale modes
                    pPos pCenter = bound.CenterPoint();

                    eExtents = new Extents3d(bound[0].ToPoint3(), bound[1].ToPoint3());

                    // Calculate the ratio between the width and height of the current view
                    double dViewRatio;
                    dViewRatio = (acView.Width / acView.Height);

                    // Tranform the extents of the view
                    matWCS2DCS = matWCS2DCS.Inverse();
                    eExtents.TransformBy(matWCS2DCS);

                    double dWidth;
                    double dHeight;
                    Point2d pNewCentPt;

                    // Check to see if a center point was provided (Center and Scale modes)

                    // Calculate the new width and height of the current view
                    dWidth = eExtents.MaxPoint.X - eExtents.MinPoint.X;
                    dHeight = eExtents.MaxPoint.X - eExtents.MinPoint.X;

                    // Get the center of the view
                    pNewCentPt = new Point2d(((eExtents.MaxPoint.X + eExtents.MinPoint.X) * 0.5),
                                                ((eExtents.MaxPoint.X + eExtents.MinPoint.X) * 0.5));


                    // Check to see if the new width fits in current window
                    if (dWidth > (dHeight * ratio))
                        dHeight = dWidth / ratio;

                    double dFactor = scrSize.X / dWidth;
                    // Resize and scale the view
                    if (dFactor != 0)
                    {
                        acView.Height = dHeight * dFactor;
                        acView.Width = dWidth * dFactor;
                    }

                    // Set the center of the view
                    acView.CenterPoint = pNewCentPt;

                    // Set the current view
                    ACD.ED.SetCurrentView(acView);
                }

                // Commit the changes
                acTrans.Commit();
            }
        }

    }

}
