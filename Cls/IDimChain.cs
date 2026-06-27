using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;

namespace AcadScript
{
    public enum EN_DIM_LEVEL
    {
        AC_FIN = 0,
        AC_GRID = 1,
        AC_ALL = 2,
        AC_HORIZONTAL = 3,
        AC_VERTICAL = 4
    }

    /*public class IDimSaveLoad
    {
        public ProgressBar prgLoad;

        public IDimSaveLoad(ProgressBar _prg)
        {
            prgLoad = _prg;
        }

        public void SaveDimData(string title, bool bIsNote)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    string[] pram = pString.INI_Params(pString.INI_String("CURDIM"));
                    double space = pram._prop("SPACE").ToNumber(200);
                    int round = (int)pram._prop("ROUND").ToNumber(1);
                    double min = pram._prop("MINVALUE").ToNumber();

                    List<pPos> pls = new List<pPos>();
                    ACD.Get2Points();

                    pPos p1 = ACD.FirstPoint;
                    pPos p2 = ACD.LastPoint;

                    string setting_file = ACD.CurrentDWGPath.Replace(".dwg", ".ini");
                    List<string> str = File.Exists(setting_file) ? File.ReadAllLines(setting_file).ToList() : new List<string>();
                    str.Add("-----------------------------------------------------------------------------------------------");

                    if (p1 != null && p2 != null && p1.DistanceTo(p2) >= min)
                    {
                        PosCollection regions = ACD.DB._getVertices(selIds);
                        //ACD.DB.DrawPolyline(regions);

                        if (regions.Count > 0)
                        {
                            string key = "#DIM=" + (title.empty() ? "NEW_DIM" : title.Upper());

                            if (Math.Abs(p1.X - p2.X) < 2000 || Math.Abs(p1.Y - p2.Y) < 2000)
                                str.Add(key + "|NOTE=" + (Math.Abs(p1.X - p2.X) < 2000 ? "Y" : "X")
                                    + (bIsNote ? "|TYPE=NOTE" : "")
                                    + "|P0X=" + p1.X.roundNumber(0.01)
                                    + "|P0Y=" + p1.Y.roundNumber(0.01) + "|P1X="
                                    + p2.X.roundNumber(0.01) + "|P1Y=" + p2.Y.roundNumber(0.01)
                                    + "|VERTS=" + regions.ToRelation(p1, p2));
                            else
                            {
                                pPos[] r = p1.Rect(p2);
                                string[] saxis = new string[] { "TOP", "LEFT", "BOTTOM", "RIGHT" };

                                for (int i = 0; i < r.Length; i++)
                                {
                                    int nex = (i + 1) % r.Length;

                                    p2 = r[i];
                                    p1 = r[nex];

                                    string st = key + "|NOTE=" + saxis[i]
                                        + (bIsNote ? "|TYPE=NOTE" : "|CHAINS=3")
                                        + "|P0X=" + p1.X.roundNumber(0.01)
                                        + "|P0Y=" + p1.Y.roundNumber(0.01) + "|P1X="
                                        + p2.X.roundNumber(0.01) + "|P1Y=" + p2.Y.roundNumber(0.01);

                                    PosCollection subs = regions.Where(reg =>
                                        p1.Inside(reg.Offset(100)) && p2.Inside(reg.Offset(100))).ToCollection();

                                    if (subs.Count > 0)
                                    {
                                        st += "|VERTS=" + subs.ToRelation(p1, p2) + ";;";

                                        //db.WR("{0}\r\nGP.DEF_LAYER_TEXT>{1}", st, st.ReplaceEquation()._prop("VERTS"));
                                        //ACD.DB.DrawPolyline(new PosCollection(st.ReplaceEquation()._prop("VERTS")),"LAYER=A-FIN");
                                    }

                                    str.Add(st);
                                }
                            }

                            File.WriteAllLines(setting_file, str.ToArray());
                            //btnUpdateDimSaveList_Click();
                        }
                    }
                }

                ACD.Focus();
            }
        }

        Dictionary<string, string> dimstyles = new Dictionary<string, string>();
        
        pPos[] _getObjPoints(ObjectId id)
        {
            List<pPos> res = new List<pPos>();
            if (ACD.DB._isLine(id) || ACD.DB._isPolyline(id))
                res.AddRange(ACD.DB._getVertices(id, 0));
            else if (ACD.DB._isWall(id))
            {
                res.AddRange(ACD.DB._getVertices(id, 0));
                res.AddRange(ACD.DB._getWallOpeningPos(id).AllPoints);
            }
            else if (ACD.DB._isCircle(id) || ACD.DB._isElip(id))
                res.Add(ACD.DB._getPoint(id));
            else if (ACD.DB._isBlock(id))
            {
                if (ACD.DB._getBound(id).Size().X >= min_block_explode
                  && ACD.DB._getBound(id).Size().Y >= min_block_explode)
                {
                    ObjectIdCollection subIds = ACD.DB.GetEntInBlock(id).ToList()
                        .Where(sid => not_layer_keywords.All(s =>
                        !ACD.DB._getLayer(sid).Upper().Contains(s))).ToCollection();

                    pPos mv = ACD.DB._getPoint(id);

                    foreach (ObjectId oId in subIds)
                        res.AddRange(_getObjPoints(oId).Move(mv));
                }
                else if (ACD.DB._getBound(id).Size().X >= min_block_size
                  && ACD.DB._getBound(id).Size().Y >= min_block_size)
                {
                    pPos pt = ACD.DB._getPoint(id);
                    if (pt.Inside(ACD.DB._getBound(id)))
                        res.Add(ACD.DB._getPoint(id));
                }
            }

            return res.ToArray();
        }

        List<string> not_layer_keywords = new List<string>();
        double min_block_size, min_block_explode, dim_flip_move,
            leader_lean_value, note_round, note_lscale, note_head, note_circle_radius;
        pPos text_offset;

        double _sin(pPos p1, pPos p2)
        {
            return Math.Abs(Math.Sin((p1 - p2).Angle() / 180 * Math.PI));
        }

        void _loadVariables()
        {
            min_block_size = pString.INI_String("MIN_BLOCK_DIM").ToNumber();
            min_block_explode = pString.INI_String("MIN_BLOCK_EXPLODE").ToNumber();
            dim_flip_move = pString.INI_String("DFLIPMOVE").ToNumber();
            leader_lean_value = pString.INI_String("LEADERLEAN").ToNumber(1000);
            text_offset = new pPos(pString.INI_String("TextOffset"));
            note_round = pString.INI_String("NOTEROUND").ToNumber(200);
            note_lscale = pString.INI_String("NOTELSCALE").ToNumber(10);
            note_head = pString.INI_String("NOTELHEAD").ToNumber(25);
            note_circle_radius = pString.INI_String("NOTECIRCLERADIUS").ToNumber(500);

            DE.InitDictionary();
        }

        public void LoadDimData(string key, bool bShowpoints, bool addRemoval)
        {
            using (ACD.Lock())
            {
                string setting_file = ACD.CurrentDWGPath.Replace(".dwg", ".ini");
                string type = "DIM";

                if (File.Exists(setting_file) && !key.empty())
                {
                    string[] str = File.ReadAllLines(setting_file);

                    prgLoad.Maximum = str.Length + 4;
                    prgLoad.Value = 1;

                    List<PosCollection> region_collect = new List<PosCollection>();
                    PosCollection region_points = new PosCollection();
                    List<int> chain_list = new List<int>();
                    List<string> types = new List<string>();
                    //List<string>contents = new List<string>();

                    foreach (string content in str)
                        if (content.StartsWith("#DIM=" + key + "|"))
                        {
                            region_points.Add(new pPos[] {
                                new pPos(content._prop("P0X").ToNumber(), content._prop("P0Y").ToNumber()),
                                new pPos(content._prop("P1X").ToNumber(), content._prop("P1Y").ToNumber()) });

                            region_collect.Add(new PosCollection(content.ReplaceEquation()._prop("VERTS")));
                            chain_list.Add((int)content._prop("CHAINS").ToNumber(1));
                            types.Add(content._prop("TYPE"));
                        }

                    List<pPos> bb = region_collect.Select(reg => reg.Boundary).ToCollection().Boundary.ToList();
                    bb.AddRange( region_points.Boundary);
                    pPos[] boundary = bb.Rect(1000).Boundary();

                    //db.WR("[BOUNDARY]{0}", boundary.ToText());

                    not_layer_keywords = new List<string> { "HIDDEN", "DEFPOINTS", "TEXT", "DIM" };
                    not_layer_keywords.AddRange(ACD.DB.ListLayers()
                            .Where(s => !ACD.DB._getLayerState(s)).Select(s => s.Upper()));

                    prgLoad.Value++;

                    ACD.DB.GetEntities(boundary, EN_SELECT.AC_ALL);

                    prgLoad.Value++;
                    
                    ObjectIdCollection selIds = IR.SelectedIds;
                    PosCollection grid_pls = ACD.DB.GetIdsGrids(selIds);

                    ObjectIdCollection allIds = selIds.ToList()
                        .Where(id => !ACD.DB._getHyperlink(id).StartsWith("REMOVAL")
                        && ACD.DB.ValidId(id) && not_layer_keywords.All(s
                        => !ACD.DB._getLayer(id).Upper().Contains(s))).ToCollection();
                    pPos[] all_texts = selIds.ToList()
                        .Where(id => !ACD.DB._getHyperlink(id).StartsWith("REMOVAL")
                        && ACD.DB._isText(id)).Select(id => ACD.DB._getPoint(id)).ToArray();
                    pPos[] all_points = allIds.ToList().Select(id => _getObjPoints(id)).ToCollection().AllPoints;

                    ACD.DB.EraseObjects(selIds.ToList()
                        .Where(id => ACD.DB._getHyperlink(id).StartsWith("REMOVAL")).ToCollection());

                    prgLoad.Value++;

                    ObjectIdCollection dimIds = new ObjectIdCollection();

                    dimstyles = File.ReadAllLines(DE.INI_FILE)
                        .Where(s => s.StartsWith("DIM_") || s.StartsWith("NOTE_"))
                        .ToDictionary(s => s._firstPropName(), s => pString.INI_Params(s._firstPropName()).First());

                    prgLoad.Value++;

                    _loadVariables();

                    for (int i = 0; i < region_collect.Count; i++)
                    {
                        pPos p1 = region_points[i][0];
                        pPos p2 = region_points[i][1];
                        PosCollection regions = region_collect[i];

                        List<pPos> grid_points = new List<pPos>();

                        int chains = chain_list[i];

                        if (p1 != null && p2 != null && regions != null && regions.Count > 0)
                        {
                            if (types[i] == "NOTE" || regions.AllPoints.Any(p => !p.Content.empty()))
                                foreach (pPos[] region in regions)
                                {
                                    int index = Array.FindIndex(region, p => !p.Content.empty());
                                    string s = null;
                                    if (index != -1)
                                        s = region[index].Content;

                                    if (s.empty())
                                    {
                                        pPos[] pts = all_texts.Where(p => p.Inside(region)
                                            && !p.Content.empty()).OrderBy(p => p.Y).ThenBy(p => p.X).ToArray();

                                        if (pts.Length > 0)
                                            s = pts.First().Content;
                                    }

                                    if(!s.empty())
                                        dimIds.AddRange(NoteSelectedObject(p1, p2, region, key, s));
                                }
                            else
                            {
                                PosCollection pls = new PosCollection();
                                pls.Add(new pPos[] { p1, p2 });
                                double sin = Math.Abs(p1.X - p2.X) < 2000 ? 0 : 1;

                                foreach (pPos[] region in regions)
                                {
                                    DE.flip = 0;

                                    pPos[] sub_pls = all_points.Where(p => p.Inside(region)).ToArray();

                                    foreach (pPos[] ls in grid_pls)
                                        if (ls.Length > 1 && ls.Length() > min_block_explode
                                            && Math.Abs(_sin(ls[0], ls[1]) - sin) < 0.1)
                                        {
                                            pPos[] interp = region.Offset(10).Intersect(ls[0], ls[1], true);

                                            if (interp.Length > 0)
                                                grid_points.AddRange(interp);
                                        }

                                    pls.Add(sub_pls);
                                }

                                pls.Add(grid_points.ToArray());

                                if (bShowpoints)
                                    ACD.DB._setHyperLink(ACD.DB.DrawCircle(pls.AllPoints, 50), "REMOVAL");

                                dimIds.AddRange(DimSelectedObject(p1, p2, pls.AllPoints, key));

                                if (chains > 1)
                                    dimIds.AddRange(DimSelectedObject(p1, p2, grid_points, key, 2));

                                if (chains > 2)
                                    dimIds.AddRange(DimSelectedObject(p1, p2, new pPos[] { p1, p2 }, key, 3));

                                if (prgLoad.Value < prgLoad.Maximum)
                                    prgLoad.Value++;
                            }
                        }
                    }

                    if (addRemoval)
                        ACD.DB._setHyperLink(dimIds, "REMOVAL");

                    prgLoad.Value = 0;
                }

                ACD.Focus();
            }
        }

        ObjectIdCollection DimSelectedObject(pPos p1, pPos p2, IEnumerable<pPos> pls, string key, int chain = 1)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            int index = dimstyles.Keys.ToList().FindIndex(s => key.StartsWith(s));
            string pram = null;

            if (index != -1)
                pram = dimstyles.Values.ElementAt(index);

            if (pram.empty())
                pram = dimstyles["DIM_50"];

            double space = ((chain - 1) * pram._prop("CHAINSP").ToNumber(1) + 1)
                * pram._prop("SPACE").ToNumber(200);
            int round = (int)pram._prop("ROUND").ToNumber(1);
            double min = pram._prop("MINVALUE").ToNumber();
            //int numchains = (int)key._prop("CHAINS").ToNumber();

            List<double[]> values = pls.ExtractPtsXY(round);
            pPos[] bb = pls.Boundary();

            if (Math.Abs(p1.Y - p2.Y) < 2000)
            {
                //X DIMENSION

                if (values[0].Length > 1)
                {
                    if (p1.X > p2.X)
                        space = -space;
                    double n = p1.Y;

                    for (int i = 0; i < values[0].Length - 1; i++)
                    {
                        pPos p3 = new pPos(values[0][i], n);
                        pPos p4 = new pPos(values[0][i + 1], n);
                        res.Add(IDimChain.CreateDimension(ACD.DB, p3, p4,
                            p3.Parallel(p4, space).CenterPoint(), "", dim_flip_move));
                    }
                }
            }
            else if (Math.Abs(p1.X - p2.X) < 2000)
            {
                //Y DIMENSION

                if (values[1].Length > 1)
                {
                    if (p1.Y > p2.Y)
                        space = -space;
                    double n = p1.X;

                    for (int i = 0; i < values[1].Length - 1; i++)
                    {
                        pPos p3 = new pPos(n, values[1][i]);
                        pPos p4 = new pPos(n, values[1][i + 1]);
                        res.Add(IDimChain.CreateDimension(ACD.DB, p3, p4,
                            p3.Parallel(p4, space).CenterPoint(), "", dim_flip_move));
                    }
                }
            }

            return res;
        }

        ObjectIdCollection NoteSelectedObject(pPos p1, pPos p2, IEnumerable<pPos> pls, string key, string content)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            int index = dimstyles.Keys.ToList().FindIndex(s => key.StartsWith(s));
            string pram = null;

            if (index != -1)
                pram = dimstyles.Values.ElementAt(index);

            if (pram.empty())
                pram = dimstyles["NOTE_50"];

            content = (p1.X < p2.X ? "#R" : "#L") + "CT " + content.Translate();

            //db.WR("Content {0}", content);
            double nx = (p2.X < p1.X ? -1 : 1) * leader_lean_value;
            double ny = (p2.Y < p1.Y ? -1 : 1) * leader_lean_value;

            res.Add(ACD.DB.DrawPolyline(new pPos[] { p1,
                new pPos(p1.X + nx, p1.Y + ny),
                new pPos(p2.X + note_circle_radius * (nx > 0 ? 2 : -2), p1.Y + ny) }, false, GP.DEF_LAYER_TEXT));

            res.Add(ACD.DB.CreateText(content, new pPos(p2.X + (nx > 0 ? - text_offset.X : text_offset.X),
                p1.Y + ny + text_offset.Y)));

            res.AddRange(ACD.DB.DrawCircle(p1, note_head, "LAYER=G-Text|FILL=ON"));

            res.AddRange(ACD.DB.DrawCircle(new pPos(p2.X + (nx > 0 ? note_circle_radius : - note_circle_radius), 
                p1.Y + ny), note_circle_radius, GP.DEF_LAYER_TEXT));

            res.Add(ACD.DB.DrawPolyline(pls.SortClockwise(), true,
                "LAYER=" + DE.DEF_LAYER_TEXT + "|LWIDTH=20|LTYPE=HIDDEN|LSCALE="
                + note_lscale + "|ROUND=" + note_round));

            return res;
        }

        public string[] DimSaveList
        {
            get
            {
                string setting_file = ACD.CurrentDWGPath.Replace(".dwg", ".ini");
                string[] res = new string[0];

                if (File.Exists(setting_file))
                {
                    res = File.ReadAllLines(setting_file)
                        .Where(s => s.StartsWith("#DIM=")).Select(s => s._firstProp()).Distinct().ToArray();
                }

                return res;
            }
        }
    }*/
    
    public class IDimChain
    {
        public EN_DIM_LEVEL Level;
        public double Range = 2000, Spacing = 500;
        public ObjectIdCollection Result, ObjectIds;//, validIds;
        Database db;
        public double dim_region_range;
        //xLine[] xlGrids;
        List<pPos> gridPoints;

        //public static void AddDimChain(IEnumerable<pPos> pts, int axis, pPos dim_point)
        //{
        //    string cmd = "_DIMADD P ";

        //    foreach (pPos p in pts)
        //        cmd += p + ",0 ";
        //    cmd += " R " + (axis == 0 ? 0 : 90) + " " + dim_point + ",0 ";
        //    //db.WR("CMD {0}", cmd);
        //    //String.Format("_DIMADD P 3,3,0 300,3,0 1300,3,0 9000,3,0  R 0 0,5000,0 ");
        //    //ACD.DOC.SendStringToExecute(cmd + "\r\n", true, false, false);
        //}


        /* bool _isDimRotationAxis( Database db, ObjectId dimId, int axis)
        {
            double angle = db._getRotation(dimId);
            double n = Math.Abs(angle % 180);
            bool res = false;

            if (45 < n && n < 135)
                res = axis == 1 || axis == 3;
            else
                res = axis == 0 || axis == 2;

            return res;
        }*/

        public IDimChain(Database _db, ObjectIdCollection ids = null)
        {
            db = _db;
            ObjectIds = ids;

            Result = new ObjectIdCollection();

            //if(ObjectIds != null)
            //    _getGridValue();

            //xlGrids = new xLine[0];
            //foreach (xLine xlgrid in xlGrids)
            //    foreach (pPos pt in xlgrid.Pts)
            //        db.DrawCircle(pt, 200);
        }

        pPos[] _validRegion(pPos p1, pPos p2)
        {
            List<pPos> r = p1.Parallel(p2, Range).ToList();
            r.AddRange(p1.Parallel(p2, -Range).Reverse());
            return r.ToArray();
        }
        
        //void _getGridValue()
        //{
        //    ObjectIdCollection gridIds = db.FilterIds(ObjectIds, IR._isGrid);
        //    gridPoints = new List<pPos>();

        //    if (gridIds.Count > 0)
        //        foreach (ObjectId objId in gridIds)
        //        {
        //            db.GetEntities(objId, IR._isGridLine);

        //            pPos[] pls = IR.polylines_in_object.Intersect();
        //            pls = pls.Where(p => IR.polylines_in_object
        //                .All(ls => ls.All(tmp => tmp.DistanceTo(p) > 100))).ToArray();

        //            gridPoints.AddRange(pls);
        //            //current_dim_region = gridPoints.Rect();
        //        }
        //}

        //public void ResizeGrid(pPos pivot, int ax, string cmd)
        //{
        //    List<pPos> pts = new List<pPos>();

        //    int splitarc = (int)pString.INI_String("SPLITARC").ToNumber();

        //    foreach (ObjectId objId in ObjectIds)
        //        if (!db._isBlock(objId))
        //            pts.AddRange(db._getVertices(objId, splitarc).ToList());

        //    if (pivot == null)
        //        pivot = pts.Boundary().CenterPoint();
        //    pGrid pg = new pGrid(pts, pivot);
        //    pg.TOLERANCE = Range;

        //    if (pg.IsValid)
        //    {
        //        for (int i = 65; i < 90; i++)
        //        {
        //            char letter = (char)i;
        //            string v = cmd._prop(letter);
        //            //db.WR("Letter {0} = {1}", letter, v);

        //            if (v != null)
        //            {
        //                double val = v.ToNumber(0);
        //                if (val != 0)
        //                {
        //                    pPos[] region = pg.GetLetterRegion(ax, letter);

        //                    if (region != null)
        //                    {
        //                        //db.WR("Value {0} Region {1}", val, region.Length);
        //                        //db.WRArray("Region", region);

        //                        pPos mv = pg.GetStrechValue(ax, letter, val);

        //                        if (region.Length == 2)
        //                        {
        //                            db.Stretch(ObjectIds, region.Rect(100), mv);
        //                            //db.DrawPolyline(region.Rect(100), true, "HYPERLINK=" + letter);
        //                        }
        //                        else
        //                        {
        //                            db.Stretch(ObjectIds, new pPos[] { region[0], region[1] }.Rect(100), new pPos(0, 0, 0) - mv);
        //                            db.Stretch(ObjectIds, new pPos[] { region[2], region[3] }.Rect(100), mv);
        //                            //db.DrawPolyline(new pPos[] { region[0], region[1]}.Rect(100), true, "HYPERLINK=" + letter);
        //                            //db.DrawPolyline(new pPos[] { region[2], region[3]}.Rect(100), true, "HYPERLINK=" + letter);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        bool _filterIds(Database db, ObjectId id)
        {
            return id.ObjectClass.DxfName == "AEC_DOOR" || id.ObjectClass.DxfName == "AEC_WINDOW"
                || id.ObjectClass.DxfName == "MTEXT" || id.ObjectClass.DxfName == "TEXT"
                || id.ObjectClass.DxfName == "DIMENSION";
        }

        //public ObjectIdCollection NewGrid(pPos pivot = null)
        //{
        //    ObjectIdCollection res = new ObjectIdCollection();
        //    List<pPos> pts = new List<pPos>();

        //    ObjectIds = ACD.DB.FilterNotIds(ObjectIds, _filterIds);

        //    foreach (ObjectId objId in ObjectIds)
        //        if (!db._isBlock(objId))
        //            pts.AddRange(db._getVertices(objId, false).ToList());
        //        else
        //            pts.Add(db._getPoint(objId));

        //    if (pivot == null)
        //        pivot = pts.Boundary().CenterPoint();

        //    pGrid pg = new pGrid(pts, pivot);
        //    pg.TOLERANCE = Tole;

        //    if (pg.IsValid)
        //    {
        //        ObjectIdCollection grid_line_ids = new ObjectIdCollection();

        //        //foreach (pPos[] ls in pg.GridLines)
        //        //    grid_line_ids.Add(db.DrawPolyline(ls, false, "LAYER=" + DE.DEFPOINTS));
                
        //        for (int ax = 0; ax < 2; ax++)
        //        {
        //            double n = 180;
        //            int nex = (ax + 1) % 2;
        //            for (int i = 65; i <= 90; i++)
        //            {
        //                pPos[] ls = pg.LetterLabels(ax, (char)i);
        //                int[] indxs = pg.GetLetterIndex(ax, (char)i);

        //                if (indxs != null)
        //                {
        //                    pPos[] r = pg.Pts.Rect().Offset(200);
        //                    pPos p1 = r.First(), p2 = p1.Clone();

        //                    p1[nex] = p2[nex] += ax == 1 ? -n : n;

        //                    if (ls.Length == 1)
        //                    {
        //                        p1[ax] = pg.grids[ax][indxs.First()];
        //                        p2[ax] = pg.grids[ax].First();
        //                    }
        //                    else
        //                    {
        //                        p1[ax] = pg.grids[ax][indxs[1]];
        //                        p2[ax] = pg.grids[ax][indxs[0]];
        //                    }

        //                    res.Add(db.DrawPolyline(new pPos[] { p1, p2 }, false, 
        //                        "LAYER=" + DE.DEF_LAYER_HIDDEN + "|HYPERLINK=$REMOVAL_GRID"));
        //                    res.Add(db.CreateText("#M" + ((char)i).ToString(), (p1 + p2) / 2, 180, 0, 
        //                        "LAYER=" + DE.DEF_LAYER_HIDDEN + "|HYPERLINK=$REMOVAL_GRID"));

        //                    n += 180;
        //                }
        //            }
        //        }
        //    }

        //    return res;
        //}

        public void DimRegionInBlock(ObjectId planId, IEnumerable<pPos> pts, int div)
        {
            db.BlockEntitiesAction(planId, (ids) =>
            {

                pPos mv = db._getPoint(planId);
                ids = ids.Cast<ObjectId>().Where(i => db._getBound(i).Move(mv).Inside(pts))
                    .Select(i => i).ToCollection();

                List<pPos> dim_pts = new List<pPos>();

                foreach (ObjectId i in ids)
                {
                    if (db._isPolyline(i))
                        dim_pts.AddRange(db._getVertices(i).Move(mv));
                    else if (db._isBlock(i) || db._isCircle(i))
                        dim_pts.Add(db._getPoint(i) + mv);
                }

                //db.WR("IDS {0} DIM_PTS {1}", ids.Count, dim_pts.Count);
                dim_pts.AddRange(pts);

                //IDimChain idim = new IDimChain(db);
                //idim.Range = lblTrackValue.Text.ToNumber();

                pPos[] r = pts.Rect();
            });
        }

        //public void DimPts(IEnumerable<KEY_PTS> key_pts,  int div)
        //{
        //    List<pPos> pts = new List<pPos>();
        //    foreach (KEY_PTS itm in key_pts)
        //        pts.AddRange(itm.Value);

        //    current_dim_region = pts.Rect();
        //    DimPts(pts, div);
        //}

        PosCollection _dimRegions(IEnumerable<pPos> pts, double range)
        {
            PosCollection res = new PosCollection();
            pPos[] r = pts.Rect();

            pPos ext = new pPos(range, range);
            for (int i = 0; i < 4; i++)
            {
                pPos[] bb = new pPos[] { r[i], r[(i + 1) % 4] }.Boundary();
                res.Add(new pPos[] { bb[0] - ext, bb[1] + ext });
            }

            //res.Closed = res.Select(ls => true).ToArray();

            return res;
        }

        public PosCollection _grid_pls = null;

        public void DimPts(IEnumerable<pPos> _pts, double spacing, 
            double round, double minvalue, int numchains, double dim_range = 0)
        {
            PosCollection current_dim_region = new PosCollection();

            if (dim_range == 0)
                current_dim_region = DE.NumericArray(0, 3).Select(n => _pts.Boundary()).ToCollectionSameClosed();
            else
                current_dim_region = _dimRegions(_pts, dim_range);

            PosCollection grid_pls = _grid_pls == null ?
                _pts.Rect().GetSegment() : _grid_pls;
            
            pPos[] gridpoints = grid_pls.SelfIntersect;
            pPos grid_ct = gridpoints.Boundary().CenterPoint();
            pPos[] grid_bb = gridpoints.Boundary();
            
            List<double[]> gridXY = gridpoints.ExtractPtsXY(0, 0)
                    .Select(ls => ls.Select(v => ls[0] + (v - ls[0]).roundNumber(spacing))
                    .Distinct().OrderBy(v => v).ToArray()).ToList();

            Result = new ObjectIdCollection();
            //Result.Add(ACD.DB.DrawPolyline(new pPos[] { grid_bb.First(), grid_bb.Last() }));

            for (int line = 0; line < 4; line++)
            {
                pPos[] reg = current_dim_region[line];

                //Result.Add(ACD.DB.DrawPolyline(reg[0].Rect(reg[1])));
                //db.WR("Region {0}[{1}]", line, reg.ToText());

                int dir = line == 0 || line == 2 ? 0 : 1;
                List<pPos> dim_points = new List<pPos>();

                switch (line)
                {
                    case 0:
                        dim_points.AddRange(_pts.Where(p 
                            => p.Inside(reg) && p.Y < grid_ct.Y));
                        break;
                    case 1:
                        dim_points.AddRange(_pts.Where(p 
                            => p.Inside(reg) && p.X >= grid_ct.X));
                        break;
                    case 2:
                        dim_points.AddRange(_pts.Where(p 
                            => p.Inside(reg) && p.Y >= grid_ct.Y));
                        break;
                    case 3:
                        dim_points.AddRange(_pts.Where(p 
                            => p.Inside(reg) && p.X < grid_ct.X));
                        break;
                }
                
                dim_points.AddRange(gridpoints);
                List<double[]> values = dim_points.ExtractPtsXY(round, minvalue);
                List<pPos> pts = new List<pPos>();

                for (int i = 0; i < values[dir].Length; i++)
                {
                    pPos p = new pPos(0, 0);
                    p[dir] = values[dir][i];
                    pts.Add(p);
                }

                pPos[] grid_points = null;

                //switch (line)
                //{
                //    case 0:
                //        grid_points = gridXY[0].Select(n => new pPos(n, grid_bb[0].Y)).Reverse().ToArray();
                //        break;
                //    case 1:
                //        grid_points = gridXY[1].Select(n => new pPos(grid_bb[0].X, n)).ToArray();
                //        break;
                //    case 2:
                //        grid_points = gridXY[0].Select(n => new pPos(n, grid_bb[1].Y)).Reverse().ToArray();
                //        break;
                //    case 3:
                //        grid_points = gridXY[1].Select(n => new pPos(grid_bb[1].X, n)).ToArray();
                //        break;
                //}
                                                
                ////res.Add(new ParamId("#TEXTM=" + regions.CenterPoint() + "(" + saxis[line] + ");"));

                //pts.AddRange(grid_points
                //    .Where(p => pts.All(p2 => Math.Abs(p2.X - p.X) > 5 || Math.Abs(p2.Y - p.Y) > 5)));

                //pts = pts.OrderBy(p => p.X.roundNumber(round))
                //    .ThenBy(p => p.Y.roundNumber(round)).ToList();

                for (int i = 0; i < pts.Count; i++)
                    pts[i][(dir + 1) % 2] = grid_bb[line >= 2 ? 0 : 1][(dir + 1) % 2];

                double sc = 1;
                double sX = spacing, sY = spacing;

                if(line >= 2)
                {
                    sX = -spacing;
                    sY = -spacing;
                }

                //Result.Add(db.CreateText("(" + line + ")", pts.CenterPoint()));

                for (int i = 0; i < pts.Count - 1; i++)
                    if(!pts[i]._isVeryClosed(pts[i + 1], minvalue))
                        Result.Add(CreateDimension(db, pts[i], pts[i + 1], sX, sY));

                sc = 1.5;

                if (numchains > 1) 
                {
                    if (_grid_pls != null)
                    {
                        for (int i = 0; i < grid_points.Length - 1; i++)
                            Result.Add(CreateDimension(db, grid_points[i], grid_points[i + 1], sX * sc, sY * sc));
                        sc = 2;
                    }else
                        Result.Add(CreateDimension(db, pts.First(), pts.Last(), sX * sc, sY * sc));
                }

                if (numchains > 2)
                    Result.Add(CreateDimension(db, pts.First(), pts.Last(), sX * sc, sY * sc));
            }
        }

        public void DimPts2(IEnumerable<pPos> _pts, int div, double range, double spacing)
        {
            Range = range;
            Spacing = spacing;

            pPos[] current_dim_region = null;

            if (current_dim_region == null)
                current_dim_region = _pts.Rect();

            List<pPos> pts = _pts.ToList();
            pts.AddRange(current_dim_region);

            pPos[] r = current_dim_region;
            PosCollection segs = r.GetSegment();

            List<int[]> indexes = current_dim_region.Length == 4 ?
                new List<int[]> { new int[] { 0, 1 }, new int[] { 1, 2 }, new int[] { 2, 3 }, new int[] { 3, 0 } }
                : new List<int[]> { new int[] { 0, 1 } };

            double spX = 100, spY = 100;
            if (indexes.Count > 1)
            {
                spX =  1.5 * Spacing;
                spY =  1.5 * Spacing;
            }

            int count = 0;

            foreach (int[] indx in indexes)
            {
                int i = indx[0];
                int nex = indx[1];

                pPos[] seg = new pPos[] { current_dim_region[i], current_dim_region[nex] };
                int axis = (current_dim_region[nex] - current_dim_region[i]).AngleAxis();

                //db.WR("Angle {0} Axis {1}", (current_dim_region[nex] - current_dim_region[i]).Angle(), axis);
                
                List<int> vals = new List<int>();

                foreach(pPos p in pts)
                    if(p.DistanceTo(seg[0],seg[1]) <= 2 * Range)
                    {
                        int d = (int)(p[axis] - seg[0][axis]).roundNumber(div);
                        if (!vals.Contains(d))
                            vals.Add(d);
                    }

                vals = vals.OrderBy(v => v).ToList();

                List<pPos> new_pts = new List<pPos>();

                nex = (axis + 1) % 2;

                foreach (int v in vals)
                {
                    pPos p = new pPos(0, 0);
                    p[axis] = v + seg[0][axis];
                    p[nex] = seg[0][nex];
                    new_pts.Add(p);
                }

                pPos neg = _negByAxis(count);
                count++;

                if (indexes.Count > 1)
                    Result.Add(CreateDimension(db, current_dim_region[i], current_dim_region[nex],
                        neg.X * 2.5 * Spacing, neg.Y * 2.5 * Spacing));

                for (int j = 0; j < new_pts.Count - 1; j++)
                    Result.Add(CreateDimension(db, new_pts[j], new_pts[j + 1],
                        neg.X * spX, neg.Y * spY));
            }
        }

        pPos _negByAxis(int axis)
        {
            double negX = 0, negY = 0;

            switch (axis)
            {
                case 0:
                    negY = 1;
                    break;
                case 1:
                    negX = 1;
                    break;
                case 2:
                    negY = -1;
                    break;
                case 3:
                    negX = -1;
                    break;
            }

            return new pPos(negX, negY);
        }
        
        double _valueByAnnotative(double v)
        {
            return v / (100 * db.Cannoscale.Scale);
        }

        public static ObjectIdCollection CreateDimension(Database db, 
            PosCollection pls, string s_over = null, double flip_move = 500)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            ObjectIdCollection[] flips = new ObjectIdCollection[] { new ObjectIdCollection(), new ObjectIdCollection(), new ObjectIdCollection() };

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                DimStyleTableRecord dst = tr.GetObject(db.Dimstyle, OpenMode.ForWrite) as DimStyleTableRecord;

                db.SetDimstyleData(dst);

                for(int i = 0; i< pls.Count; i ++)
                    if (pls[i].Length > 2)
                    {
                        pPos[] ls = pls[i];
                        pPos p1 = ls[0], p2 = ls[1], p3 = ls[2];
                        //int rot = (int)Math.Sin((p1 - p2).Angle() / 180 * Math.PI);
                        double rot = Math.Abs(p1.X - p2.X) > Math.Abs(p1.Y - p2.Y) ? 0 : Math.PI / 2;
                        
                        RotatedDimension acRotDim = new RotatedDimension(rot, p1.ToPoint3(),
                            p2.ToPoint3(), p3.ToPoint3(), !s_over.empty() ? s_over : null, db.Dimstyle);

                        btr.AppendEntity(acRotDim);
                        tr.AddNewlyCreatedDBObject(acRotDim, true);

                        res.Add(acRotDim.ObjectId);

                        db.AddCurrentAnnotative(acRotDim.ObjectId);
                        _applyDST(dst, acRotDim);

                    }
                tr.Commit();
            }

            double[] scales = new double[] { -1.5, 2.5, 4.5 };

            foreach (ObjectId dimId in res)
            {
                pPos[] line = db._getDimPoints(dimId);

                //if (line[0].DistanceTo(line[1]) < flip_move)
                //    db._setDimTextPos(dimId, db._getDimTextPos(dimId).dim_flip_over(line[0], line[1], flip_move));
                //else
                //    DE.flip = 0;
            }

            db._setLayer(res, pString.INI_String("DIM_LAYER"));
            return res;
        }

        public static ObjectId CreateDimension(Database db, pPos p1, pPos p2, 
            pPos p3, string text_override = "<>", double flip_move = 500)
        {
            return CreateDimension(db, new PosCollection { new pPos[] { p1, p2, p3 } },  text_override, flip_move ).First();
        }

        public static ObjectId CreateAlignDimension(Database db, pPos p1, pPos p2,
            double spacingX, double spacingY, string text_override = "<>")
        {
            ObjectId res = ObjectId.Null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                DimStyleTableRecord dst = tr.GetObject(db.Dimstyle, OpenMode.ForWrite) as DimStyleTableRecord;

                db.SetDimstyleData(dst);

                //double rot = Math.Abs(p1.X - p2.X) > Math.Abs(p1.Y - p2.Y) ? 0 : Math.PI / 2;

                pPos p3 = p1 + new pPos(spacingX, spacingY);
                AlignedDimension acRotDim = new AlignedDimension(p1.ToPoint3(),
                    p2.ToPoint3(), p3.ToPoint3(), text_override, db.Dimstyle);

                btr.AppendEntity(acRotDim);
                tr.AddNewlyCreatedDBObject(acRotDim, true);

                res = acRotDim.ObjectId;

                db.AddCurrentAnnotative(acRotDim.ObjectId);
                _applyDST(dst, acRotDim);

                db._setLayer(acRotDim.ObjectId, pString.INI_String("DIM_LAYER"));

                tr.Commit();
            }
            return res;
        }

        public static ObjectId CreateDimension(Database db, pPos p1, pPos p2,  
            double spacingX, double spacingY, string text_override = "<>")
        {
            ObjectId res = ObjectId.Null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                DimStyleTableRecord dst = tr.GetObject(db.Dimstyle, OpenMode.ForWrite) as DimStyleTableRecord;

                db.SetDimstyleData(dst);

                double rot = Math.Abs(p1.X - p2.X) > Math.Abs(p1.Y - p2.Y) ? 0 : Math.PI / 2;
                
                pPos p3 = p1 + new pPos(spacingX, spacingY);
                RotatedDimension acRotDim = new RotatedDimension(rot, p1.ToPoint3(), 
                    p2.ToPoint3(), p3.ToPoint3(), text_override, db.Dimstyle);

                btr.AppendEntity(acRotDim);
                tr.AddNewlyCreatedDBObject(acRotDim, true);

                res = acRotDim.ObjectId;

                db.AddCurrentAnnotative(acRotDim.ObjectId);
                _applyDST(dst, acRotDim);
                                
                db._setLayer(acRotDim.ObjectId, pString.INI_String("DIM_LAYER"));

                tr.Commit();
            }
            return res;
        }

        //public static void CreateDimension( Database db, pPos p1, pPos p2, pPos p3, pPos p4)
        //{
        //    using (Transaction tr = db.TransactionManager.StartTransaction())
        //    {
        //        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
        //        BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
        //        DimStyleTableRecord dst = tr.GetObject(db.Dimstyle, OpenMode.ForWrite) as DimStyleTableRecord;
                
        //        // Create an angular dimension
        //        using (LineAngularDimension2 acLinAngDim = new LineAngularDimension2())
        //        {
        //            acLinAngDim.XLine1Start = p1.ToPoint3();
        //            acLinAngDim.XLine1End = p2.ToPoint3();
        //            acLinAngDim.XLine2Start = p3.ToPoint3();
        //            acLinAngDim.XLine2End = p4.ToPoint3();
        //            acLinAngDim.ArcPoint = (new pPos[] { p1, p2, p3, p4 }).CenterPoint().ToPoint3();
        //            acLinAngDim.DimensionStyle = db.Dimstyle;

        //            // Add the new object to Model space and the transaction
        //            btr.AppendEntity(acLinAngDim);
        //            tr.AddNewlyCreatedDBObject(acLinAngDim, true);

        //            db.AddCurrentAnnotative(acLinAngDim.ObjectId);
        //            _applyDST(dst, acLinAngDim);
        //            db._setLayer(acLinAngDim.ObjectId, pString.INI_String("DIM_LAYER"));
        //        }

        //        tr.Commit();
        //    }
        //}

        public static void CreateDimension( Database db, pPos pt, double radius)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                DimStyleTableRecord dst = tr.GetObject(db.Dimstyle, OpenMode.ForWrite) as DimStyleTableRecord;

                // Create an angular dimension
                using (RadialDimension acRadDim = new RadialDimension())
                {
                    acRadDim.Center = pt.ToPoint3();
                    acRadDim.ChordPoint = new Point3d(pt.X, pt.Y + radius, 0);
                    acRadDim.LeaderLength = 5;
                    acRadDim.DimensionStyle = db.Dimstyle;

                    // Add the new object to Model space and the transaction
                    btr.AppendEntity(acRadDim);
                    tr.AddNewlyCreatedDBObject(acRadDim, true);

                    db.AddCurrentAnnotative(acRadDim.ObjectId);
                    _applyDST(dst, acRadDim);
                    db._setLayer(acRadDim.ObjectId, pString.INI_String("DIM_LAYER"));
                }

                // Commit the changes and dispose of the transaction
                tr.Commit();
            }
        }

        static int _getDimIndex(Database db, ObjectId dimId)
        {
            int a = (int)Math.Round(Math.Sin(db._getRotation(dimId)));
            pPos txtpt = db._getPoint(dimId, 2);
            pPos pt = db._getPoint(dimId, 0);

            int b = 0;

            if ((a == 0 && txtpt.Y >= pt.Y)
                || (a == 1 && txtpt.X >= pt.X))
                b = 1;

            return b * 2 + a;
        }

        static void _alignDim(Database db, ObjectIdCollection ids, double tole)
        {
            int dir = _getDimIndex(db, ids.First());
            int axis = dir % 2;
            int nex = (axis + 1) % 2;
            PosCollection pls = new PosCollection();

            if (ids.Count > 0)
            {
                List<double> tmp_nex = new List<double>();
                List<double> tmp_axis = new List<double>();

                foreach (ObjectId dimId in ids)
                {
                    pPos[] ls = db._getVertices(dimId);
                    pls.Add(ls);
                    tmp_nex.AddRange(ls.Select(p => p[nex]));
                    tmp_axis.AddRange(ls.Select(p => p[axis]));
                }

                double v_nex = dir > 1 ? tmp_nex.Max() : tmp_nex.Min();
                double v_axis = dir > 1 ? tmp_axis.Max() : tmp_axis.Min();

                foreach (ObjectId dimId in ids)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        pPos pt = db._getPoint(dimId, k);
                        pt[nex] = v_nex;
                        pt[axis] = v_axis + (pt[axis] - v_axis).roundNumber(tole);

                        db._setPoint(dimId, pt, k);
                    }
                }
            }
        }

        public static void SetDimensionXLineLength(Database db, ObjectId dimId, double dimrange)
        {
            pPos[] ls = db._getDimPoints(dimId);
            pPos[] pts = new pPos[] { db._getPoint(dimId, 0), db._getPoint(dimId, 1) };

            for (int i = 0; i < 2; i++)
                db._setPoint(dimId, ls[i].Along(dimrange, pts[i]), i);
        }

        public static void AlignDimensions(Database db, ObjectIdCollection ids, double tole = 50)
        {
            ids = ids.Cast<ObjectId>().Where(id => db._isRotateDim(id)).Select(id => id).ToCollection();
            bool[] T = ids.Cast<ObjectId>().Select(id => false).ToArray();
            int[] Indexes = ids.Cast<ObjectId>().Select(id => _getDimIndex(db, id)).ToArray();

            //db.WR("Dims {0}", ids.Count);
            //db.WRArray("Dim_Index", Indexes);

            while (T.Sum(b => !b ? 1 : 0) > 0)
            {
                ObjectIdCollection selIds = new ObjectIdCollection();

                for (int i = 0; i < ids.Count; i++)
                    if (!T[i])
                    {
                        T[i] = true;
                        selIds.Add(ids[i]);

                        for (int j = 0; j < ids.Count; j++)
                            if (i != j && !T[j] && Indexes[i] == Indexes[j])
                            {
                                pPos txt1 = db._getPoint(ids[i], 0);
                                pPos txt2 = db._getPoint(ids[j], 0);

                                int axis = Indexes[i] % 2 == 1 ? 0 : 1;
                                if (Math.Abs(txt1[axis] - txt2[axis]) < 100)
                                {
                                    T[j] = true;
                                    selIds.Add(ids[j]);
                                }
                            }

                        //db.WR("<{1}> SEL_IDS {0}", selIds.Count, i);

                        if(selIds.Count > 1)
                        {
                            _alignDim(db, selIds, tole);
                            selIds = new ObjectIdCollection();
                        }
                    }
            }
        }

        static void _applyDST(DimStyleTableRecord dst, Dimension dim)
        {

            dim.Dimadec = dst.Dimadec;
            dim.Dimalt = dst.Dimalt;
            dim.Dimaltd = dst.Dimaltd;
            dim.Dimaltf = dst.Dimaltf;
            //dim.Dimaltmzf = dst.Dimaltmzf;
            //dim.Dimaltmzs = dst.Dimaltmzs;
            dim.Dimaltrnd = dst.Dimaltrnd;
            dim.Dimalttd = dst.Dimalttd;
            dim.Dimalttz = dst.Dimalttz;
            dim.Dimaltu = dst.Dimaltu;
            dim.Dimaltz = dst.Dimaltz;
            dim.Dimapost = dst.Dimapost;
            dim.Dimarcsym = dst.Dimarcsym;
            dim.Dimasz = dst.Dimasz;
            dim.Dimatfit = dst.Dimatfit;
            dim.Dimaunit = dst.Dimaunit;
            dim.Dimazin = dst.Dimazin;
            dim.Dimblk = dst.Dimblk;
            dim.Dimblk1 = dst.Dimblk1;
            dim.Dimblk2 = dst.Dimblk2;
            dim.Dimcen = dst.Dimcen;
            dim.Dimclrd = dst.Dimclrd;
            dim.Dimclre = dst.Dimclre;
            dim.Dimclrt = dst.Dimclrt;
            dim.Dimdec = dst.Dimdec;
            dim.Dimdle = dst.Dimdle;
            dim.Dimdli = dst.Dimdli;
            dim.Dimdsep = dst.Dimdsep;
            dim.Dimexe = dst.Dimexe;
            dim.Dimexo = dst.Dimexo;
            dim.Dimfrac = dst.Dimfrac;
            dim.Dimfxlen = dst.Dimfxlen;
            dim.DimfxlenOn = dst.DimfxlenOn;
            dim.Dimgap = dst.Dimgap;
            dim.Dimjogang = dst.Dimjogang;
            dim.Dimjust = dst.Dimjust;
            dim.Dimldrblk = dst.Dimldrblk;
            //dim.Dimlfac = dst.Dimlfac;
            dim.Dimlim = dst.Dimlim;
            dim.Dimltex1 = dst.Dimltex1;
            dim.Dimltex2 = dst.Dimltex2;
            dim.Dimltype = dst.Dimltype;
            dim.Dimlunit = dst.Dimlunit;
            dim.Dimlwd = dst.Dimlwd;
            dim.Dimlwe = dst.Dimlwe;
            //dim.Dimmzf = dst.Dimmzf;
            //dim.Dimmzs = dst.Dimmzs;
            dim.Dimpost = dst.Dimpost;
            dim.Dimrnd = dst.Dimrnd;
            dim.Dimsah = dst.Dimsah;
            
            //dim.Dimscale = dst.Dimscale;
            dim.Dimsd1 = dst.Dimsd1;
            dim.Dimsd2 = dst.Dimsd2;
            dim.Dimse1 = dst.Dimse1;
            dim.Dimse2 = dst.Dimse2;
            dim.Dimsoxd = dst.Dimsoxd;
            dim.Dimtad = dst.Dimtad;
            dim.Dimtdec = dst.Dimtdec;
            //dim.Dimtfac = dst.Dimtfac;
            dim.Dimtfill = dst.Dimtfill;
            dim.Dimtfillclr = dst.Dimtfillclr;
            dim.Dimtih = dst.Dimtih;
            dim.Dimtix = dst.Dimtix;
            dim.Dimtm = dst.Dimtm;
            dim.Dimtmove = dst.Dimtmove;
            dim.Dimtofl = dst.Dimtofl;
            dim.Dimtoh = dst.Dimtoh;
            dim.Dimtol = dst.Dimtol;
            dim.Dimtolj = dst.Dimtolj;
            dim.Dimtp = dst.Dimtp;
            dim.Dimtsz = dst.Dimtsz;
            dim.Dimtvp = dst.Dimtvp;
            dim.Dimtxt = dst.Dimtxt;
            dim.Dimtxtdirection = dst.Dimtxtdirection;
            dim.Dimtzin = dst.Dimtzin;
            dim.Dimupt = dst.Dimupt;
            dim.Dimzin = dst.Dimzin;
        }

        public static void _applyMLeaderDST(MLeaderStyle dst, MLeader leader)
        {
            leader.ArrowSymbolId = dst.ArrowSymbolId;
            leader.ArrowSize = dst.ArrowSize;
            leader.ContentType = dst.ContentType;
            leader.MText = dst.DefaultMText;
            //leader.Dimaltmzf = dst.Dimaltmzf;
            //leader.Dimaltmzs = dst.Dimaltmzs;
            leader.LandingGap = dst.LandingGap;
            //leader.BlockRotation = dst.EnableBlockRotation;
            //leader.MaxLeaderSegmentsPoints = dst.MaxLeaderSegmentsPoints;
        }
    }
}

