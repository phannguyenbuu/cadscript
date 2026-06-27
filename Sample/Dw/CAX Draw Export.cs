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
using System.Windows.Forms;
//using SyncObject;

namespace AcadScript
{
    public static class GetAndDrawCLS
    {
        public static pPos _rPoint(this pPos p, double n)
        {
            pPos res = new pPos(0, 0);
            res.X = Math.Floor(p.X / n) * n;
            res.Y = Math.Floor(p.Y / n) * n;
            return res;
        }

        public static void _add_item(this Dictionary<string, List<pPos>> dict, pPos pt)
        {
            string[] ar = pt.Content.filter("#");
            pPos offset = new pPos(0, 0);
            
            foreach (string s in ar)
            {
                if (s.st_("x"))
                    offset.X += s.Substring(1).ToNumber();
                else if(s.st_("y"))
                    offset.Y += s.Substring(1).ToNumber();
            }

            double rot = pt.Rotation;
            pt += offset;
            pt.Rotation = rot;

            string key = ar[0];
            //ACD.WR("OK3");
            if (!dict.ContainsKey(key))
                dict.Add(key, new List<pPos>());

            //ACD.WR("OK1:{0}", pt + offset);

            if (!dict[key].Any(__p => __p.X == pt.X && __p.Y == pt.Y))
                dict[key].Add(pt);
        }

        public static ObjectIdCollection _dict_draw(this Dictionary<string, List<pPos>> dict)
        {
            ObjectIdCollection resIds = new ObjectIdCollection();

            foreach (string key in dict.Keys)
                foreach (pPos __p in dict[key])
                {
                    var obj = ACD.DB.Insert(key, __p);
                    ACD.WR("_draw_rotation {0}", __p.Rotation);
                    ACD.DB._setRotation(obj, __p.Rotation);

                    ObjectId new_obj = ObjectId.Null;
                    
                    if (__p.Content.ct_("#mirx") || __p.Content.ct_("#flipx"))
                    {
                        ACD.WR("MIRX");
                        new_obj = ACD.DB.CloneObject(obj);
                        ACD.DB.MirrorObject(new_obj, new pPos(__p.X, 0), new pPos(__p.X, 1000));

                        resIds.Add(new_obj);

                        if (__p.Content.ct_("#mirx"))
                            resIds.Add(obj);
                        else
                            ACD.DB.EraseObject(obj);
                    }

                    if (__p.Content.ct_("#miry") || __p.Content.ct_("#flipy"))
                    {
                        ACD.WR("MIRY");
                        new_obj = ACD.DB.CloneObject(obj);
                        ACD.DB.MirrorObject(new_obj, new pPos(0, __p.Y), new pPos(1000, __p.Y));
                        resIds.Add(new_obj);

                        if (__p.Content.ct_("#miry"))
                            resIds.Add(obj);
                        else
                            ACD.DB.EraseObject(obj);
                    }

                    if(new_obj.IsNull)
                        resIds.Add(obj);
                }

            return resIds;
        }

        
    }

    public class CAXDrawCLS
    {
        static pPos _to_pt(pPos pt)
        {
            pPos mv = new pPos(VIEW_MOVE_X, VIEW_MOVE_Y);

            if (VIEW_ROTATION == 0)
                mv.X = 0;
            else
                mv.Y = 0;

            return pt + mv;
        }

        static void _build_plan_grid(ObjectIdCollection ids)
        {
            List<pPos> points = new List<pPos>();
            ObjectIdCollection resIds = new ObjectIdCollection();

            foreach(ObjectId id in ids)
            {
                if(ACD.DB._isHatch(id) && ACD.DB._getIdInfo(id).ct_("SOLID"))
                {
                    var pts = ACD.DB._getVertices(id);

                    if(pts.Length == 4 && pts.Area() > 0.1 * 0.1)
                        points.Add(pts.CenterPoint()._rPoint(100));
                }
            }

            var xys = points.ExtractPtsXY(100, 100);

            for (int nx = 0; nx < xys[0].Length; nx++)
                for (int ny = 0; ny < xys[1].Length; ny++)
                    for (int i = 0; i < points.Count; i++)
                        if(points[i] != null && points[i]._isVeryClosed(new pPos(xys[0][nx], xys[1][ny]), 200))
                            points[i] = new pPos(xys[0][nx], xys[1][ny]);
            

            var bb = points.Boundary();
            
            for(int nx = 0; nx < xys[0].Length; nx ++)
                resIds.Add(ACD.DB.Draw2D(new pPos[] { new pPos(xys[0][nx], bb[0].Y - ext), new pPos(xys[0][nx], bb[1].Y + ext) }));
            for (int ny = 0; ny < xys[1].Length; ny++)
                resIds.Add(ACD.DB.Draw2D(new pPos[] { new pPos(bb[0].X - ext, xys[1][ny]), new pPos(bb[1].X + ext, xys[1][ny]) }));

            var mr = new pPos(100, 100);

            foreach(pPos p in points)
                resIds.Add(ACD.DB.Draw2D((p - mr).Rect(200,200), "c"));

            ACD.DB.NewBlock(resIds, ACD.DB.uniqueBlockName("grid_plan_"), true, false, bb[0]);
            ObjectIdCollection txtIds = new ObjectIdCollection();

            foreach (ObjectId id in ids)
            {
                if (ACD.DB._isText(id))
                {
                    string content = ACD.DB._getContent(id);

                    if(content.Length >= 5)
                        txtIds.Add(ACD.DB.CreateText(content, ACD.DB._getPoint(id)._rPoint(100), 1.5));
                }
            }

            ACD.DB.NewBlock(txtIds, ACD.DB.uniqueBlockName("txt_plan_"), true, false, bb[0]);
            //ACD.DB.Insert(blockname, bb[0]);
        }

        static ObjectIdCollection _drawSlabSection(ObjectIdCollection ids)
        {
            ObjectIdCollection resIds = new ObjectIdCollection();

            var values = _read_grid_infor(ids);
            //var pls = (PosCollection)values[0];
            var bbs = (pPos[])values[1];
            //var base_pt = (pPos)values[2];

            int ax = VIEW_ROTATION == 0 ? 0 : 1;
            PosCollection pls = new PosCollection();
            List<pPos> gridls = new List<pPos>();


            foreach (ObjectId id in ids)
            {
                if (ACD.DB._isPolyline(id))
                {
                    pPos[] __ls = ACD.DB._getVertices(id);

                    if(__ls.Length == 4)
                        pls.Add(__ls);
                    else  if (__ls.Length == 2 && __ls[0][ax] == __ls[1][ax])
                        gridls.Add(__ls[0]);
                }
            }

            double b_x = pls.Boundary[0][ax];
            double f_x = pls.Boundary[1][ax];

            //pls.Add(gridls.Boundary());
            var xys = pls.ExtractPtsXY(50, 50);

            List<pPos> res = new List<pPos>();

            if(ax == 0)
                res.Add(new pPos(0, -100));
            else
                res.Add(new pPos(-100, 0));

            double bh = 300;

            double[] vals = xys[ax].Select(v => v - b_x).ToArray();

            for (int i = 0; i < vals.Length; i+=2)
            {
                var x = vals[i];

                if (ax == 0)
                {
                    res.Add(new pPos(x, -100));
                    res.Add(new pPos(x, -bh));
                    res.Add(new pPos(vals[i + 1], -bh));
                    res.Add(new pPos(vals[i + 1], -100));
                }
                else
                {
                    res.Add(new pPos( -100, x));
                    res.Add(new pPos(-bh, x));
                    res.Add(new pPos(-bh, vals[i + 1]));
                    res.Add(new pPos(-100, vals[i + 1]));
                }
            }

            if (ax == 0)
            {
                res.Add(new pPos(f_x - b_x, -100));
                res.Add(new pPos(f_x - b_x, 0));
                res.Add(new pPos(0, 0));
            }
            else
            {
                res.Add(new pPos(-100, f_x - b_x));
                res.Add(new pPos(0, f_x - b_x));
                res.Add(new pPos(0, 0));
            }

            resIds.Add(ACD.DB.Draw2D(res.ToArray(), "C"));
            ACD.WR("Slab: {0} resIds: {1}", res.ToText(), resIds[0]);

            ObjectIdCollection new_block_ids = ACD.DB.NewBlock(resIds, ACD.DB.uniqueBlockName("slab_section_"), true, false, new pPos(0, 0));
            ACD.DB.MoveObject(new_block_ids, _to_pt(basept)); // + new pPos(0, VIEW_MOVE_Y)

            return new_block_ids;
        }


        static ObjectIdCollection _drawElevation(ObjectIdCollection ids, params string[] blocknames)
        {
            ObjectIdCollection resIds = new ObjectIdCollection();

            var values = _read_grid_infor(ids);
            //var pls = (PosCollection)values[0];
            var bbs = (pPos[])values[1];
            //var base_pt = (pPos)values[2];

            Dictionary<string, List<pPos>> dict = new Dictionary<string, List<pPos>>();
            //ACD.WR("I1");
            //FRONT & BACK
            foreach (ObjectId id in ids)
            {
                if (ACD.DB._isPolyline(id))
                {
                    pPos[] __ls = ACD.DB._getVertices(id);
                    
                    var bb = __ls.Boundary();

                    if (__ls.Length == 4)
                    {
                        if (blocknames.Length == 0)
                            resIds.Add(ACD.DB.Draw2D(new pPos(bb[0].X - basept.X, 0).Rect(bb[1].X - bb[0].X, COLUMN_HEIGHT), "c"));

                        foreach (string blockname in blocknames)
                        {
                            dict._add_item(new pPos((bb[0].X + bb[1].X) / 2 - basept.X, 0, 0, blockname));
                        }
                    }
                    else if (__ls.Length == 2 && __ls[0].X == __ls[1].X)
                    {
                        resIds.Add(ACD.DB.Draw2D(__ls[0].X - basept.X, - ext, __ls[0].X - basept.X, COLUMN_HEIGHT + ext, "A-Hidden"));
                    }
                    
                } else if (ACD.DB._isLeader(id))
                {
                    pPos p = ACD.DB._getPoint(id);
                    dict._add_item(new pPos(p.X - basept.X,0,0, ACD.DB._getContent(id)));
                }
            }

            resIds.AddRange(dict._dict_draw());

            foreach (var y in LEVELS)
                resIds.Add(ACD.DB.Draw2D(- ext - basept.X, y, bbs.Size().X + ext - basept.X, y, "A-Hidden"));
            
            ObjectIdCollection new_block_ids = ACD.DB.NewBlock(resIds, ACD.DB.uniqueBlockName("elev_front_"), true, false, new pPos(0,0));
            ACD.DB.MoveObject(new_block_ids, _to_pt(basept)); // + new pPos(0, VIEW_MOVE_Y)

            return resIds;
        }

        static ObjectIdCollection _drawElevation(IEnumerable<pPos> pts, params string[] blocknames)
        {
            ObjectIdCollection resIds = new ObjectIdCollection();
            Dictionary<string, List<pPos>> dict = new Dictionary<string, List<pPos>>();
            //ACD.WR("Rotation:{0}", VIEW_ROTATION);

            foreach (pPos p in pts)
                foreach (string blockname in blocknames)
                {
                    pPos pt = VIEW_ROTATION == 0 ? new pPos(p.X, VIEW_MOVE_Y + basept.Y, 0, blockname)
                        : new pPos(VIEW_MOVE_X + basept.X, p.Y, 0, blockname);
                    pt.Rotation = VIEW_ROTATION;
                    dict._add_item(pt);
                }

            foreach (var key in dict.Keys)
            {
                ACD.WR("{0}-{1}", key, dict[key].ToText());
                //foreach (pPos p in dict[key])
                //    ACD.DB.DrawCircle(p, 200);
            }

            resIds.AddRange(dict._dict_draw());

            return resIds;
        }

        static object[] _read_grid_infor(ObjectIdCollection ids)
        {
            PosCollection pls = new PosCollection();
            pls = ACD.DB._getAllVertices(ids);

            var bbs = pls.Boundary;
            //var base_pt = bbs[0];

            //var base_pt = pls.Where(ls => ls.Length == 4).ToCollectionSameClosed().Boundary[0];

            return new object[] { pls, bbs};
        }


        static ObjectIdCollection _build_sweep_fad(IEnumerable<pPos> pts, ObjectIdCollection profileBlocks, double offset_x = 0)
        {
            ObjectIdCollection resIds = new ObjectIdCollection();
            Dictionary<string, List<pPos>> dict = new Dictionary<string, List<pPos>>();
            ACD.WR("Profiles - OK0.1");
            string[] all_block_names = ACD.DB.ListBlock();

            double[] heights = new double[0];
            if (heights.Length == 0) heights = new double[] { 0 };

            int ax = VIEW_ROTATION == 0 ? 0 : 1;
            int nex = VIEW_ROTATION == 0 ? 1 : 0;

            PosCollection segments = pts.GetSegment().Where(___seg => ___seg[0][nex] == ___seg[1][nex])
                .OrderBy(___seg => ___seg[0][nex]).ToCollectionSameClosed(false);

            var parallels = new List<double>();
            List<string> blocknames = new List<string>();

            if (ACD.DB.FilterIds(profileBlocks, "INSERT").Count == 0)
                profileBlocks = PROFILE_BLOCKS;

            foreach (ObjectId blockId in profileBlocks)
                if(ACD.DB._isBlock(blockId))
                {
                    blocknames.Add(ACD.DB._getIdName(blockId));
                    pPos sub_pt = ACD.DB._getPoint(blockId);
                    ACD.DB.BlockEntitiesAction(blockId, __ids => {
                        parallels.AddRange(ACD.DB._getAllVertices(__ids).Move(sub_pt.Invert).AllPoints.Select(__p => __p.Y.roundNumber()));
                    });
                }

            parallels = parallels.Distinct().ToList();
            ACD.WR("Profiles {0}, {1}", blocknames.ToTextStr(","), parallels.ToTextDouble(","));

            for (int i = 0; i < segments.Count; i++)
            {
                pPos[] seg = segments[i];
                
                if (seg[0][nex] == seg[1][nex])
                {
                    foreach (double h in parallels)
                        if(ax == 0)
                            resIds.Add(ACD.DB.Draw2D(new pPos(Math.Min(seg[0].X, seg[1].X) - basept.X + offset_x, h),
                                new pPos(Math.Max(seg[0].X,seg[1].X) - basept.X - offset_x, h)));
                        else
                            resIds.Add(ACD.DB.Draw2D(new pPos(h, Math.Min(seg[0].Y, seg[1].Y) - basept.Y + offset_x),
                                new pPos(h, Math.Max(seg[0].Y, seg[1].Y) - basept.Y - offset_x)));

                    foreach (var blockname in blocknames)
                    {
                        var __p = new pPos(0,0, 0, blockname + (ax == 0 ? "#FLIPX" : ""));
                        __p[ax] = Math.Min(seg[0][ax], seg[1][ax]) - basept[ax];
                        __p.Rotation = VIEW_ROTATION;
                        dict._add_item(__p);

                        __p = new pPos(0, 0, 0, blockname + (ax == 0 ? "" : "#FLIPY"));
                        __p[ax] = Math.Max(seg[0][ax], seg[1][ax]) - basept[ax];
                        __p.Rotation = VIEW_ROTATION;
                        dict._add_item(__p);
                    }
                }
            }

            foreach (string key in dict.Keys)
                ACD.WR("{0}:{1}", key, dict[key].ToText());

            resIds.AddRange(dict._dict_draw());
            ObjectIdCollection new_block_ids = ACD.DB.NewBlock(resIds, ACD.DB.uniqueBlockName("sweep_elev_"), true, false, new pPos(0, 0));
            ACD.DB.MoveObject(new_block_ids, _to_pt(basept)); // + new pPos(0, VIEW_MOVE_Y)

            return new_block_ids;
        }

        static ObjectIdCollection _drawRoof(pPos[] pts)
        {
            ObjectIdCollection resIds = new ObjectIdCollection();
            pPos[] bb = pts.Boundary();
            pPos sz = bb.Size() / 2;

            double w = VIEW_ROTATION == 0 ? sz.X : sz.Y;

            pPos[] h_pts = new pPos[] { new pPos(-w, 0), new pPos(-550, 0), new pPos(-w, 1100) };
            
            resIds.Add(ACD.DB.Draw2D(0, 0, - w, 1300));
            resIds.Add(ACD.DB.Draw2D(h_pts));
            resIds.AddRange(ACD.DB.DrawHatch(h_pts, "HATCH=ANSI31|HANGLE=45|HSCALE=100"));
            
            return _build_sweep_fad(pts, ACD.DB.NewBlock(resIds, ACD.DB.uniqueBlockName("_swp_roof_"), true, false, new pPos(0, 0)), w);
        }

        

        static void _init_config(ObjectIdCollection selIds)
        {
            VIEW_ROTATION = ACD.DB.GetDrawingProp("VIEW_ROTATION").ToNumber();
            double vx = ACD.DB.GetDrawingProp("VIEW_MOVE_X").ToNumber();
            double vy = ACD.DB.GetDrawingProp("VIEW_MOVE_Y").ToNumber();

            if (selIds.Count == 1 && ACD.DB._isBlock(selIds.First()))
            {
                MODE = "E";
                //VIEW_ROTATION = ACD.DB._getRotation(selIds.First())._rPointNumber(90);
            }
            else
            {
                foreach (ObjectId objId in selIds)
                {
                    string[] xnotes = ACD.DB.GetXNotes(objId);
                    if (xnotes.Length > 0)
                        MODE = xnotes.First();
                }

                if (MODE.et_())
                {
                    foreach (ObjectId objId in selIds)
                    {
                        var bb = ACD.DB._getBound(objId);
                        var sz = bb.Size();

                        if (ACD.DB._isBlock(objId) && (ACD.DB._getIdName(objId).st_("plan") || ACD.DB._getIdName(objId).st_("grid")))
                            MODE = "E";
                        else if (ACD.DB._isDoor(objId) || ACD.DB._isWall(objId))
                        {
                            //VIEW_ROTATION = sz.X > sz.Y ? 0 : 270;
                            MODE = "E";
                        }
                    }
                }
            }

            foreach (ObjectId objId in selIds)
            {
                string[] xnotes = ACD.DB.GetXNotes(objId);
                if (xnotes.Length > 0)
                {
                    CURRENT_SWEEP_HEIGHTS = xnotes.Where(s__ => !s__._prop("H").et_() && !s__._prop("B").et_()).ToArray();
                    //VIEW_ROTATION = xnotes._props("ROTATION").ToNumber(VIEW_ROTATION);
                }
            }

            if (MODE.et_())
                ACD.ED.GetInputString("Select MODE (Plan/RoofElevation/Elevation/bacK/Left/Right/SweepElevation/sectionAA/sectionAB/sectionBA/sectionBB)", "P");

            ACD.WR("Mode {0} View Rotation {1} View Move {2},{3}", MODE, VIEW_ROTATION, VIEW_MOVE_X, VIEW_MOVE_Y);

            VH = pString.INI_String("VIEW_SPACING_H").ToNumber();

            if (MODE.ct_("e"))
            {
                if(VIEW_ROTATION == 0)
                    VIEW_MOVE_Y = vy;
                else
                    VIEW_MOVE_X = vx;

                current_view = "@front";
            }
            //else if (MODE.Upper() == "K")
            //    VIEW_MOVE_Y = VH * 2;
        }

        static ObjectIdCollection _txt_to_ids(string str)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            string[] profiles = str.filter(",;").ToArray();

            if (profiles.Length > 0)
            {
                ACD.DB.GetEntities(null, EN_SELECT.AC_ALL);

                res = IR.SelectedIds.ToList().Where(id__ => ACD.DB._isBlock(id__)
                    && profiles.Any(s__ => s__ == ACD.DB._getIdName(id__))).Distinct().ToCollection();
            }

            return res;
        }

        static string MODE = null;
        static ObjectIdCollection PROFILE_BLOCKS = null;
        static double[] LEVELS = new double[] { -750, 0, 3600, 3600 + 3300, 3600 + 3300 + 2000 };
        static string[] CURRENT_SWEEP_HEIGHTS = new string[0];
        static int ext = 1000, _rPoint_UNIT = 50000;
        static pPos basept = null;
        static double VH = 0, VIEW_MOVE_X = 0, VIEW_MOVE_Y = 0; //, VIEW_BACK_Y = 0, VIEW_LEFT_Y = 0, VIEW_RIGHT_Y = 0;
        static double VIEW_ROTATION = 0;
        static double COLUMN_HEIGHT = 7200;
        static string[] lib_block_names, all_block_names;
        //static string[] obj_keywords = new string[] { "obj_", "main_", "col_", "beam_", "wall_" };
        static string current_view = "";
        
        static void _parsePolyline(ObjectId objId, ObjectIdCollection selIds)
        {
            var pts = ACD.DB._getVertices(objId);

            if (MODE.Upper() == "RE")
            {
                //ACD.WR("R0.2");
                _drawRoof(pts);
            }
            else if (MODE.Upper() == "SE")
            {
                //SE
                //H = 7800 | B = _swp_profile_001
                //H = 3600 | B = _swp_profile_002
                //H = 0 | B = _swp_profile_003

                foreach (var s in CURRENT_SWEEP_HEIGHTS)
                {
                    PROFILE_BLOCKS = _txt_to_ids(s._prop("B"));

                    double[] heights = s._prop("H").filter(",;").Select(s__ => s__.ToNumber()).ToArray();

                    foreach (var h in heights)
                    {
                        ACD.DB.MoveObject(_build_sweep_fad(pts, selIds),VIEW_ROTATION == 0 ? new pPos(0, h) : new pPos(h, 0));
                    }
                }
            }
        }

        static void _parseDoor(ObjectId objId)
        {

            var bb = ACD.DB._getBound(objId);
            var sz = bb.Size();


            if (MODE.Upper() == "E")
            {
                string name__ = ACD.DB._getIdName(objId);
                string[] ls_sel_block_names = lib_block_names.Where(s__ => s__.ct_(name__)).ToArray();

                if (ls_sel_block_names.Length == 0)
                    ls_sel_block_names = all_block_names.Where(s__ => s__.ct_(name__)).ToArray();

                string sel_key = null;

                ACD.WR("ls_sel_block_names:{0}, {1}", name__, ls_sel_block_names.Length);

                if (ls_sel_block_names.Length > 0)
                {
                    sel_key = ls_sel_block_names[0];

                    if (ls_sel_block_names.Length > 1)
                    {
                        string[] tmps__ = ls_sel_block_names.Where(s__ => s__.ct_(ACD.DB._getDoorWidth(objId).ToString()
                            + "x" + ACD.DB._getDoorHeight(objId))).ToArray();

                        if (tmps__.Length > 0)
                            sel_key = tmps__[0];
                    }

                    ACD.WR("sel_key {0}", sel_key);

                    //ACD.DB.DrawCircle(bb.CenterPoint(), 500);
                    _drawElevation(new pPos[] { bb.CenterPoint() }, new string[] { sel_key });
                }
            }
        }

        

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    _init_config(selIds);
                    
                    all_block_names = ACD.DB.ListBlock();
                    lib_block_names = ACD.DB.FilterIds(selIds, "INSERT").ToList().Select(id => ACD.DB._getIdName(id)).ToArray();
                    ACD.WR("IDS:{0} ROTATION {1}", lib_block_names.ToTextStr(","), VIEW_ROTATION);

                    foreach (ObjectId objId in selIds)
                        if (ACD.DB._isBlock(objId) && (ACD.DB._getIdName(objId).st_("plan") || ACD.DB._getIdName(objId).st_("grid")))
                        {
                            string blockname = ACD.DB._getIdName(objId);
                            string[] ar = blockname.filter("@");

                            basept = ACD.DB._getPoint(objId)._rPoint(_rPoint_UNIT);

                            ACD.DB.BlockEntitiesAction(objId, ids =>
                            {
                                if (MODE.Upper() == "P")
                                {
                                    _build_plan_grid(ids);
                                }//ACD.WR("OKA");
                                else if (MODE.Upper() == "E")
                                {
                                    ACD.WR("Draw slab");
                                    //_drawElevation(ids, lib_block_names.Where(s__ => s__.ct_("col_")).ToArray());
                                    _drawSlabSection(ids);
                                }
                            });
                        }
                        else if (ACD.DB._isDoor(objId))
                        {
                            basept = ACD.DB._getBound(objId).CenterPoint()._rPoint(_rPoint_UNIT);
                            //ACD.DB.DrawCircle(basept, 200);
                            _parseDoor(objId);
                        }
                        else if (ACD.DB._isWall(objId))
                        {
                            basept = ACD.DB._getBound(objId).CenterPoint()._rPoint(_rPoint_UNIT);
                            ObjectIdCollection __door_ids = ACD.DB._getWallOpeningIds(objId);
                            ACD.WR("Doors {0}", __door_ids.Count);
                            foreach(ObjectId ___id in __door_ids)
                                _parseDoor(___id);
                        }
                        else if (ACD.DB._isPolyline(objId))
                        {
                            basept = ACD.DB._getBound(objId).CenterPoint()._rPoint(_rPoint_UNIT);
                            //ACD.DB.DrawCircle(basept, 200);
                            _parsePolyline(objId, selIds);
                        }
                        else if (ACD.DB._isBlock(objId))
                        {
                            //ACD.WR("B1");
                            basept = ACD.DB._getPoint(objId)._rPoint(_rPoint_UNIT);
                            //basept.Y = basept.Y._rPointNumber(_rPoint_UNIT);

                            if (MODE.Upper() == "E")
                            {
                                
                                {
                                    string name__ = ACD.DB._getIdName(objId);
                                    string[] ls_sel_block_names = lib_block_names.Where(s__ => s__ != name__ && s__.ct_(name__) && s__.ct_(current_view)).ToArray();

                                    if (ls_sel_block_names.Length == 0)
                                        ls_sel_block_names = all_block_names.Where(s__ => s__ != name__ && s__.ct_(name__) && s__.ct_(current_view)).ToArray();

                                    //string sel_key = null;

                                    ACD.WR("ls_sel_block_names:{0}, {1}", name__, ls_sel_block_names.Length);

                                    if (ls_sel_block_names.Length > 0)
                                    {
                                        _drawElevation(new pPos[] { ACD.DB._getPoint(objId) }, new string[] { ls_sel_block_names[0] });
                                    }
                                }
                            }
                        }
                }

                ACD.Focus();
            }
        }
    }
}