using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;

namespace AcadScript
{
    public class PrgImportIText
    {
        public int ViewportIndex;
        public ObjectIdCollection ResultIds;
        public pPos BasePoint;

        public PosCollection SourceViewport;

        public PosCollection DimPts;
        public PosCollection NotePts;
        public List<string> NoteString;

        public double NoteLength = 0;

        public PrgImportIText(pPos basept = null)
        {
            BasePoint = basept == null ? new pPos(0, 0) : basept;
            //view_source_pls = axis_names.Select(s => new pPos[0]).ToCollection();
            NoteString = new List<string>();
        }

        int current_source_line_index = -1;

        int _axisIndex(string saxis)
        {
            string[] keys = { "X", "Y", "Z" };
            int index = Array.FindIndex(keys, k => saxis.Contains(k));

            return index == -1 ? 2 : index;
        }

        double space, round, min, region;
        int chains;


        PosCollection _plsFromStrings(IEnumerable<string> prams)
        {
            PosCollection pls = new PosCollection();

            foreach (string pram in prams)
                pls.AddRange(new PosCollection(pram._prop("VERTS")));

            return pls;
        }

        PosCollection _plsFromStrings(string pram)
        {
            return _plsFromStrings(new string[] { pram });
        }

        ObjectIdCollection _drawAxisGrid(PosCollection gridpls, int ax,
            double grid_start_val = 0, double grid_height_above = 0, double grid_height_below = 0)
        {
            ObjectIdCollection ids = new ObjectIdCollection();

            foreach (pPos[] ls in gridpls)
                if (ls.Length > 1)
                {
                    pPos sz = ls.Size();
                    if ((sz.X > sz.Y ? 0 : 1) != ax)
                    {
                        pPos p1 = new pPos(ls[0][ax] - grid_start_val, -grid_height_below);

                        pPos p2 = p1.Clone();
                        p2.Y = grid_height_above;

                        ids.Add(ACD.DB.DrawPolyline(new pPos[] { p1, p2 }, false, "LAYER=A-Hidden"));
                    }
                }

            return ids;
        }

        PosCollection DrawFace(string pram)
        {
            pPos[] pts = new PosCollection(pram)[0];

            //#FACE=(-393800,-15600,3100[(0,4,5,1)(1,5,6,2)(2,6,7,3)(3,7,4,0)(4,8,9,5)(5,9,10,6)(6,10,11,7)(7,11,8,4)(8,12,13,9)(9,13,14,10)(10,14,15,11)(11,15,12,8)(12,0,1,13)(13,1,2,14)(14,2,3,15)(15,3,0,12)];-393600,-15600,3100;-393600,-15600,3100;-393800,-15600,3100;-393800,-15750,3100;-393600,-15750,3100;-393600,-15750,3100;-393800,-15750,3100;-393800,-15750,5000;-393600,-15750,5000;-393600,-15750,5000;-393800,-15750,5000;-393800,-15150,5000;-393600,-15150,5000;-393600,-15150,5000;-393800,-15150,5000)
            return pts[0].Content.filter("()").Select(ss
                => ss.filter(",").Select(s
                => pts[(int)s.ToNumber()]).ToArray()).ToCollectionSameClosed();
        }

        PosCollection _itemToPosCollection(string pram)
        {

            //ObjectIdCollection ids = new ObjectIdCollection();
            PosCollection pls = new PosCollection();

            if (!pram._prop("VERTS").empty())
                pls = new PosCollection(pram._prop("VERTS"));

            string key = pram._firstPropName();
            string val = pram._firstProp();

            if (key == "#INS")
            {
                if (pls.Count == 0)
                    pls = new PosCollection(pram._prop("POS"));
            }
            else if (key == "#FACE")
            {
                pPos[] pts = new PosCollection(pram._firstProp())[0];

                //#FACE=(-393800,-15600,3100[(0,4,5,1)(1,5,6,2)(2,6,7,3)(3,7,4,0)(4,8,9,5)(5,9,10,6)(6,10,11,7)(7,11,8,4)(8,12,13,9)(9,13,14,10)(10,14,15,11)(11,15,12,8)(12,0,1,13)(13,1,2,14)(14,2,3,15)(15,3,0,12)];-393600,-15600,3100;-393600,-15600,3100;-393800,-15600,3100;-393800,-15750,3100;-393600,-15750,3100;-393600,-15750,3100;-393800,-15750,3100;-393800,-15750,5000;-393600,-15750,5000;-393600,-15750,5000;-393800,-15750,5000;-393800,-15150,5000;-393600,-15150,5000;-393600,-15150,5000;-393800,-15150,5000)
                pls = pts[0].Content.filter("()").Select(ss
                    => ss.filter(",").Select(s
                    => pts[(int)s.ToNumber()]).ToArray()).ToCollectionSameClosed();
            }

            //ACD.WR("Draw {0}, {1}, {2}", pram._prop("TXT"), pls.Count, (int)pram._getPramTextHeight() / 10);
            return pls;
        }

        public ObjectIdCollection DrawAllLine(IEnumerable<string> str)
        {
            ObjectIdCollection ids = new ObjectIdCollection();
            List<PosCollection> ePolys = new List<PosCollection>();

            for (int line = 0; line < str.Count(); line++)
            {
                string pram = str.ElementAt(line);

                if (pram.StartsWith("#") && !pram._firstProp().empty())
                {
                    PosCollection pls = _itemToPosCollection(pram);

                    string key = pram._firstPropName().Upper();
                    string val = pram._firstProp();
                    string style;

                    style = pram.Replace(key + "=" + pram._firstProp(), "LAYER=" + val);
                    //ACD.WR("OK1.3");

                    if (key == "#LW")
                    {
                        if (val.st_("WALL"))
                            ids.AddRange(ACD.DB.DrawWall(pls, val));
                        else
                            for (int i = 0; i < pls.Count; i++)
                            {
                                //ObjectIdCollection hIds = ACD.DB.HatchKey(pls[i].ToCollection(), val);
                                //ids.AddRange(ACD.DB.HatchKey(pls[i].ToCollection(), val));
                                ids.Add(ACD.DB.DrawPolyline(pls[i], pls.Closed[i], "LAYER=" + LWLayer));
                            }
                    }
                    else if (key == "#CC")
                    {
                        //ObjectIdCollection c_ids = ACD.DB.DrawCircle(pls.AllPoints,
                        //    pram._prop("RADIUS").ToNumber(200), style);
                        //ids.AddRange(c_ids);
                        //if (!pram._prop("HPATTERN").empty())
                        //    ids.AddRange(ACD.DB.DrawHatch(c_ids, pram));
                    }
                    else if (key.StartsWith("#TXT"))
                    {
                        foreach (pPos p in pls.AllPoints)
                        {
                            //pPos pt = new pPos(txt);
                            ObjectId id = ACD.DB.CreateText(p.Content, p, 0, (int)pram._getPramTextHeight() / 10, "LAYER=TXT");
                            ACD.DB._setRotation(id, p.Rotation);
                            ACD.DB._setLayer(id, pram._firstProp());
                            ids.Add(id);
                        }
                    }
                    else if (key == "#INS")
                    {
                        pPos flip = null;

                        if (pram._prop("Flip").empty())
                        {
                            int axis = (int)(pram._prop("Flip").ToNumber());
                            flip = new pPos(0, 0);
                            flip[axis] = 1;
                        }

                        ids.AddRange(ACD.DB.InsertBlock(val, pls.AllPoints, pram));
                    }

                    else if (key == "#OBJECTID")
                    {
                        if (ePolys.Count == 0)
                            ePolys.Add(new PosCollection());
                        ePolys.Last().Name = pram;
                        ACD.WR("New Face Items {0}", pram);
                    }
                    else if (key == "#FACE")
                    {
                        if (ePolys.Count == 0)
                            ePolys.Add(new PosCollection());

                        PosCollection poly = ePolys.Last();
                        poly.AddRange(pls);
                    }
                }
            }

            //ACD.WR("Face Objects {0}", ePolys.Count);

            foreach (PosCollection face in ePolys)
            {
                //ACD.WR("Face Items {0}", face.Count);
                string content = face.Name;
                string type = content._prop("TYPE");
                string val = content._prop("HATCH");
                //ACD.WR("Face Items {0} Type {1} Val {2}", face.Count, type, val);

                if (type.Upper().Contains("FLAT"))
                {
                    pPos[] bb = face.Boundary;
                    int[] axs = DE.NumericArray(0, 2).OrderBy(i => bb.Size()[i]).ToArray();
                    int ax = axs[0];

                    //ACD.WR("Flat {0}", ax);

                    PosCollection project_face = face.Select(f
                        => f.Select(p => ax == 0 ? new pPos(p.Y.roundNumber(ROUND_VALUE), p.Z.roundNumber(ROUND_VALUE)) : ax == 1
                        ? new pPos(p.X.roundNumber(ROUND_VALUE), p.Z.roundNumber(ROUND_VALUE))
                        : new pPos(p.X.roundNumber(ROUND_VALUE), p.Y.roundNumber(ROUND_VALUE))).ToArray())
                            .Where(f => f.Area() > 1).ToCollectionSameClosed();

                    project_face.Closed = project_face.Select(f => true).ToArray();
                    project_face = project_face.GetSegment().Select(seg
                        => seg.OrderBy(p => p.X).ThenBy(p => p.Y).ToText()).Distinct()
                        .Select(s => new PosCollection(s)[0]).ToCollectionSameClosed();

                    project_face = _simplyPosCollection(project_face);

                    for (int i = 0; i < project_face.Count; i++)
                    {
                        //ids.AddRange(ACD.DB.HatchKey(project_face[i].ToCollection(), val));
                        ids.Add(ACD.DB.DrawPolyline(project_face[i], false, "LAYER=" + LWLayer));
                    }

                    //ACD.WR("K5");
                }
                else
                {
                    //for (int i = 0; i < face.Count; i++)
                    //{
                    //    ids.AddRange(ACD.DB.HatchKey(face[i].ToCollection(), val));
                    //    //ids.Add(ACD.DB.DrawPolyline(face[i], true, "LAYER=" + LWLayer));
                    //}
                }
            }

            return ids;
        }

        pPos[] _setPValue(int ax, double v_ax, double v1, double v2)
        {
            int nex = (ax + 1) % 2;

            pPos p1 = new pPos(0, 0);
            p1[ax] = v_ax;
            p1[nex] = v1;

            pPos p2 = new pPos(0, 0);
            p2[ax] = v_ax;
            p2[nex] = v2;

            return new pPos[] { p1, p2 };
        }

        pPos _setPValue(int ax, double v_ax, double v_nex)
        {
            int nex = (ax + 1) % 2;

            pPos p = new pPos(0, 0);
            p[ax] = v_ax;
            p[nex] = v_nex;

            return p;
        }

        PosCollection _simplyPosCollection(PosCollection pls)
        {
            PosCollection segments = pls.GetSegment().Select(seg
                => seg.OrderBy(p => p.X).ThenBy(p => p.Y).ToText()).Distinct().Select(s
                => new PosCollection(s)[0]).ToCollectionSameClosed();

            PosCollection new_pls = segments.Where(seg
                => seg[0].X == seg[1].X || seg[0].Y == seg[1].Y).ToCollectionSameClosed();

            PosCollection res = segments.Where(seg
                => seg[0].X != seg[1].X && seg[0].Y != seg[1].Y).ToCollectionSameClosed();

            var lsXY = new_pls.ExtractPtsXY(ROUND_VALUE, ROUND_VALUE);

            for (int ax = 0; ax < 2; ax++)
            {
                int nex = (ax + 1) % 2;
                foreach (double v in lsXY[ax])
                {
                    PosCollection pts = new_pls.Where(ls => ls[0][ax] == v && ls[1][ax] == v).ToCollectionSameClosed();
                    double[] vals = pts.AllPoints.Select(p => p[nex]).OrderBy(n => n).ToArray();

                    if (vals.Length == 2)
                        res.Add(_setPValue(ax, v, vals.First(), vals.Last()));
                    else if (pts.Count > 0)
                    {
                        double[] mids = DE.NumericArray(0, vals.Length - 2).Select(i
                            => (vals[i] + vals[i + 1]) / 2).ToArray();

                        int[] not_indexes = DE.NumericArray(0, mids.Length - 1).Where(i => pts.All(ls
                            => mids[i] < ls[0][nex] || mids[i] > ls[1][nex])).ToArray();

                        if (not_indexes.Length == 0)
                            res.Add(_setPValue(ax, v, vals.First(), vals.Last()));
                        else
                        {
                            int last_i = 0;

                            for (int i = 0; i < mids.Length; i++)
                                if (not_indexes.Contains(i) || i == mids.Length - 1)
                                {
                                    if (last_i == 0 && i == 0)
                                        last_i = 1;
                                    else if (last_i - i == 1)
                                        last_i = i;
                                    else
                                    {
                                        res.Add(_setPValue(ax, v, vals[last_i], vals[i]));
                                        last_i = i + 1;
                                    }
                                }
                        }
                    }
                }
            }

            return res;
        }

        double[] _getNoteX(string saxis)
        {
            double[] res = null;
            PosCollection pls = SourceViewport.Where(ls
                => ls.First().Content.Contains("[" + saxis + "]")).ToCollectionSameClosed();

            if (pls.Count > 0)
            {
                pPos[] region = pls.First();
                pPos sz = region.Size();
                res = new double[] { region.Boundary()[0].X + sz.X / 6,
                    region.Boundary()[1].X - sz.X / 6 };
            }

            return res;
        }

        pPos _getAxisMove(string saxis, IEnumerable<pPos> bb)
        {
            pPos res = null;

            PosCollection pls = SourceViewport.Where(ls
                => ls.First().Content.Contains("[" + saxis + "]")).ToCollectionSameClosed();

            if (pls.Count > 0)
            {
                pPos[] region = pls.First();
                pPos sz = region.Size();

                string vert = region.First().Content._firstProp();

                if (!vert.empty())
                {
                    string[] ar = vert.Replace(";;", "\r\n").filter("\r\n");
                    int index = Array.FindIndex(ar, s => s.Contains("[" + saxis + "]"));

                    //ACD.WR("AR_INDEX={0}", index);

                    if (index != -1)
                    {
                        ar = ar[index].filter(";");
                        //ACD.WR("AR_INDEX={0} AR={1}", index, ar.ToText(";"));

                        if (ar.Length > 1)
                        {
                            //ACD.WR("VALUE=" + ar[0] + "|" + ACD.DB._getBound(ids).ToInfo());
                            pPos p1 = pPos.FromString(("VALUE=" + ar[0] + "|"
                                + bb.ToInfo()).ReplaceEquation()._firstProp());

                            //ACD.WR("VALUE=" + ar[1] + "|" + SourceViewport[index].ToInfo());
                            pPos p2 = pPos.FromString(("VALUE=" + ar[1] + "|"
                                + region.ToInfo()).ReplaceEquation()._firstProp());

                            res = p2 - p1;
                        }
                    }
                }
            }

            return res;
        }

        string[] contents;
        string dim_setting = null;
        string LWLayer = "0";
        double ROUND_VALUE = 50;

        public void DrawData(IEnumerable<string> str, double _ROUND_VALUE)
        {
            ROUND_VALUE = _ROUND_VALUE;

            contents = str.ToArray();
            ResultIds = new ObjectIdCollection();
            DimPts = new PosCollection();
            NotePts = new PosCollection();

            space = pString.INI_String("DIM_SPACE").ToNumber(1000);
            round = pString.INI_String("DIM_ROUND").ToNumber(ROUND_VALUE);
            min = pString.INI_String("DIM_MIN").ToNumber(1000);
            region = pString.INI_String("DIM_REGION").ToNumber(0);
            chains = (int)pString.INI_String("DIM_CHAINS").ToNumber(1000);
            NoteLength = pString.INI_String("DIM_NOTE_LENGTH").ToNumber(1000);
            LWLayer = pString.INI_String("LAYER_LW");

            string[] firstprop_keys = str.Select(s => s._firstProp()).Distinct().ToArray();
            PosCollection viewports = new PosCollection();

            viewports = ACD.DB.GetDrawingZoneListByPoint(BasePoint, firstprop_keys.Length);

            ResultIds = DrawAllLine(str);
        }

        double Spacing
        {
            get
            {
                return 500000 * ACD.DB.CurrentAnnotativeScale();
            }
        }

        pPos[] _getBS(ObjectIdCollection ids)
        {
            ObjectIdCollection res = ids.ToList().Where(id
                => !ACD.DB._getLayer(id).Upper().Contains("TEXT")
                && !ACD.DB._isText(id) && !ACD.DB._isDim(id)).ToCollection();
            return ACD.DB._getBound(res);
        }

        public PosCollection MasterPlan_Region(IEnumerable<string> str)
        {
            ObjectIdCollection ids = new ObjectIdCollection();
            List<PosCollection> ePolys = new List<PosCollection>();

            PosCollection regions = new PosCollection();

            for (int line = 0; line < str.Count(); line++)
            {
                string pram = str.ElementAt(line);

                if (pram.StartsWith("#") && !pram._firstProp().empty())
                {
                    PosCollection pls = _itemToPosCollection(pram);

                    string key = pram._firstPropName().Upper();
                    string val = pram._firstProp();
                    string style;

                    style = pram.Replace(key + "=" + pram._firstProp(), "LAYER=" + val);
                    //ACD.WR("OK1.3");

                    if (key == "#LW")
                    {
                        regions.AddRange(pls);
                    }
                }
            }

            regions = regions.Select(ls => ls.Select(p => p.Round(50)).ToArray()).ToCollectionClosedList(regions.Closed);
            GraphConsole.Compute(regions);

            return GraphConsole.ResultPts;
        }
    }
}
