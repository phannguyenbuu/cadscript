using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;

using System;

namespace AcadScript
{
    public static class IGeneralCLS
    {
        public static pPos p0;

        public static void AddClosedVertex(this PosCollection pls)
        {
            for (int i = 0; i < pls.Count; i++)
                if (pls.Closed[i])
                    pls[i] = pls[i].Add(pls[i].First());
        }

        public static KeyValuePair<string, int> StartPrefix(this string sindex)
        {
            string prefix = sindex;

            if (!sindex.empty())
                prefix = sindex;

            string[] ar = prefix.Select(s => s.ToString()).ToArray();

            prefix = ar.Where(s => !"0123456789".Contains(s)).ToTextStr().Replace(",", "");
            int start = (int)ar.Where(s => "0123456789".Contains(s)).ToTextStr().ToNumber(1);

            return new KeyValuePair<string, int>(prefix, start);
        }

        public static pPos[] findClosedPath(this IEnumerable<pPos> _points)
        {
            pPos[] points = _points.ToArray();
            double y_min = points[0].Y;
            int min = 0;

            for (int i = 1; i < points.Length; i++)
            {
                double y = points[i].Y;
                if ((y < y_min) || (y_min == y && points[i].X < points[min].X))
                {
                    y_min = points[i].Y;
                    min = i;
                }
            }

            pPos temp = points[0];
            points[0] = points[min];
            points[min] = temp;

            IGeneralCLS.p0 = points[0];

            //List<pPos> ls = new List<pPos> { points[0] };
            //ls.AddRange(DE.NumericArray(1, points.Length - 1).Select(i => points[i]).OrderBy(p => p, new YourComparer()));

            Quick_Sort(points, 0, points.Length - 1);

            return points;
        }

        private static void Quick_Sort(pPos[] arr, int left, int right)
        {
            if (left < right)
            {
                int pivot = Partition(arr, left, right);

                if (pivot > 1)
                    Quick_Sort(arr, left, pivot - 1);

                if (pivot + 1 < right)
                    Quick_Sort(arr, pivot + 1, right);
            }
        }
        static double euclid_dist(pPos p1, pPos p2)
        {
            return (p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y);
        }
        static int orientation(pPos p1, pPos p2, pPos p3)
        {
            double val = (p2.Y - p1.Y) * (p3.X - p2.X) - (p2.X - p1.X) * (p3.Y - p2.Y);
            if (val == 0) return 0; // colinear
            return (val > 0) ? 1 : 2; // clockwise or counterclock wise
        }

        static int Compare(pPos p1, pPos p2)
        {
            int res = 1;

            int o = orientation(IGeneralCLS.p0, p1, p2);
            if (o == 0)
                res = euclid_dist(IGeneralCLS.p0, p2) >= euclid_dist(IGeneralCLS.p0, p1) ? -1 : 1;
            else
                res = o == 2 ? -1 : 1;

            return res;
        }
        private static int Partition(pPos[] arr, int left, int right)
        {
            pPos pivot = arr[left];

            while (true)
            {
                //ACD.WR("PLS {0},{1},{2},{3}", arr.Length, left, right, pivot);

                while (Compare(arr[left], pivot) == -1)
                    left++;
                //ACD.WR("OK02 {0};{1}", arr[right],pivot);
                while (Compare(arr[right], pivot) == 1)
                    right--;
                //ACD.WR("OK03");
                if (left < right)
                {
                    if (arr[left] == arr[right]) return right;
                    //ACD.WR("OK03");
                    pPos temp = arr[left];
                    arr[left] = arr[right];
                    arr[right] = temp;
                    //ACD.WR("OK04");
                }
                else
                    return right;
            }
        }

    }

    public class HatchRegionCLS
    {
        PosCollection sources;
        pPos[] ptsIntersects;
        int round_value, width_index_count, height_index_count, grid_cell_value;
        Dictionary<string, List<pPos>> grids;
        List<pPos> allpoints;

        pPos basept;

        public HatchRegionCLS(PosCollection _sources, int _round_value)
        {
            round_value = _round_value;
            grid_cell_value = round_value * 2;

            sources = _sources;
            ptsIntersects = sources.Intersect(true);

            pPos[] bb = sources.Boundary.Select(p => p.Round(_round_value)).ToArray();
            pPos sz = bb.Size();
            basept = bb.First();

            width_index_count = (int)(sz.X / grid_cell_value);
            height_index_count = (int)(sz.Y / grid_cell_value);

            allpoints = sources.AllPoints.ToList();
            allpoints.AddRange(ptsIntersects);

            grids = new Dictionary<string, List<pPos>>();

            foreach (pPos p in allpoints)
            {
                int nx = (int)((p.X - basept.X) / grid_cell_value);
                int ny = (int)((p.Y - basept.Y) / grid_cell_value);

                string key = nx + "_" + ny;

                if (!grids.ContainsKey(key))
                    grids.Add(key, new List<pPos>());

                grids[key].Add(p);
            }

            PosCollection calc_pls = sources.Select(pts
                => _roundPts(pts.Move(basept.Invert))).ToCollectionSameClosed();

            _modifyPts(calc_pls, o => o /= round_value);

            //for (int i = 0; i < calc_pls.Count; i++)
            //{
            //    if (calc_pls.Closed[i])
            //        calc_pls[i] = calc_pls[i].Add(calc_pls[i].First());

            //    for (int j = 0; j < calc_pls[i].Length; j++)
            //        calc_pls[i][j] /= round_value;
            //}

            GraphConsole.Compute(calc_pls);
            calc_pls = GraphConsole.ResultPts;

            _modifyPts(calc_pls, o => o * round_value); //

            _drawRegion(calc_pls.Move(basept), "HPATTERN=ANSI31|HSCALE=10");

            _modifyPts(calc_pls, o => _findNearestPoint(o)); //

            _drawRegion(calc_pls, "HPATTERN=ANSI37|HSCALE=10");
        }

        void _drawRegion(PosCollection pls, string hatch_info)
        {
            foreach (pPos[] pts in pls)
            {
                ObjectIdCollection hIds = ACD.DB.DrawHatch(pts, hatch_info);

                //foreach (ObjectId id in hIds)
                    //ACD.DB.SetXNotes(id, "Type=Erased");
            }
        }

        void _modifyPts(PosCollection pls, Func<pPos, pPos> fn)
        {
            for (int i = 0; i < pls.Count; i++)
                for (int j = 0; j < pls[i].Length; j++)
                    pls[i][j] = fn(pls[i][j]);
        }

        pPos[] _roundPts(pPos[] pts)
        {
            return pts.Select(p => p.Round(round_value).ToString())
               .Distinct().Select(s => pPos.FromString(s)).ToArray();
        }

        pPos _findNearestPoint(pPos pt)
        {
            pPos res = pt;

            int nx = (int)(pt.X / grid_cell_value);
            int ny = (int)(pt.Y / grid_cell_value);

            List<pPos> pts = new List<pPos>();

            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    string key = (nx + i) + "_" + (ny + j);
                    if (grids.ContainsKey(key) && grids[key].Count > 0)
                        pts.AddRange(grids[key]);
                }

            if (pts.Count > 0)
                res = pts.OrderBy(p => pt.DistanceTo(p)).First(); //Math.Abs(pt.X - p.X) + Math.Abs(pt.Y - p.Y)

            return res;
        }
    }

    public class BasepointHatchCLS
    {
        static string HatchInfo(ObjectIdCollection selIds)
        {

            ACD.WR("Select hatch objects ...");
            //ObjectIdCollection selIds = ACD.GetSelection();

            string st = "";

            foreach (ObjectId objId in selIds)
            {
                if (ACD.DB._isHatch(objId))
                {
                    PosCollection pls = ACD.DB._getHatch(objId);
                    st += pls.Select(ls => ls.Size().X + " x " + ls.Size().Y).ToTextStr("\r\n");
                }
            }

            return st;
            //Clipboard.SetText(st);

        }

        static void HatchBasepoint(ObjectIdCollection selIds)
        {
            //ObjectIdCollection selIds = ACD.GetSelection()._filterDXF("HATCH");
            Database db = ACD.DB;
            string[] blocknames = ACD.DB.ListBlock().Where(s => s.Upper().Contains("BASEPOINT")).Select(s => s).ToArray();
            List<pPos> pts = new List<pPos>();

            while (true)
            {
                pPos pt = ACD.GetPoint();

                if (pt == null)
                {
                    break;
                }
                else
                {
                    int index = selIds.FindIndex(id => pt.Inside(ACD.DB._getVertices(id)));
                    if (index != -1)
                    {
                        ACD.DB._setHatchPoint(selIds[index], pt);

                        if (blocknames.Length > 0)
                            ACD.DB.Insert(blocknames.First(), pt, GP.DEF_LAYER_TEXT);
                    }
                }
            }
        }

        static pPos _topPoint(pPos[] pts)
        {
            return pts.OrderBy(p => -p.Y.roundNumber(0.01)).ThenBy(p => p.X.roundNumber(0.01)).First();
        }

        static bool byX_first = true;


        static PosCollection _orderPointlist(PosCollection pls, string sdirection)
        {
            int direction = 1;

            if (!sdirection.empty())
            {
                if (sdirection.StartsWith("-"))
                {
                    direction = -1;
                    sdirection = sdirection.Substring(1);
                }

                if (sdirection.Upper() == "X")
                    direction *= 1;
                else
                    direction *= 2;
            }

            int ax = Math.Abs(direction) - 1;// ? 0 : 1;
            int nex = (ax + 1) % 2;

            pls = pls.OrderBy(pts => direction / Math.Abs(direction) * pts.Centroid()[ax].roundNumber())
                    .ThenBy(pts => direction / Math.Abs(direction) * pts.Centroid()[nex].roundNumber()).ToCollectionSameClosed();

            pPos[] lsCenter = pls.Select(ls => ls.Centroid()).ToArray();

            List<int> adjs = new List<int> { 0 };

            while (true)
            {
                int i = adjs.Last();
                int[] indx = DE.NumericArray(0, lsCenter.Length - 1)
                    .Where(j => !adjs.Contains(j) && i != j).ToArray();
                //for (int j = 0; !adjs.Contains(j) && i != j && j < lsCenter.Length; j++)
                //    indx.Add(j);

                if (indx.Length > 0)
                {
                    indx = indx.OrderBy(j => lsCenter[i].DistanceTo(lsCenter[j])).ToArray();
                    adjs.Add(indx.First());
                }
                else
                    break;
            }

            for (int i = 0; i < adjs.Count - 2; i++)
            {
                if (lsCenter[adjs[i]].DistanceTo(lsCenter[adjs[i + 1]]) >
                    lsCenter[adjs[i]].DistanceTo(lsCenter[adjs[i + 2]]))
                {
                    int tmp = adjs[i + 1];
                    adjs[i + 1] = adjs[i + 2];
                    adjs[i + 2] = tmp;

                    //changed = true;
                }

                if (i > 0 && lsCenter[adjs[i]].DistanceTo(lsCenter[adjs[i - 1]]) >
                    lsCenter[adjs[i - 1]].DistanceTo(lsCenter[adjs[i + 1]]))
                {
                    int tmp = adjs[i];
                    adjs[i] = adjs[i - 1];
                    adjs[i - 1] = tmp;

                    //changed = true;
                }
            }

            return adjs.Select(n => pls[n]).ToCollectionSameClosed(true);
        }



        static void _showHatchArea(ObjectIdCollection ids)
        {
            List<string> lsDims = new List<string>();

            //int direction;
            string prefix = "";
            pPos pt = ACD.DB._getBound(ids)[0];
            ObjectIdCollection infoIds = new ObjectIdCollection();

            foreach (ObjectId objId in ids)
            {
                string[] xnotes = ACD.DB.GetXNotes(objId);
                string sindex = xnotes._props("Index");

                string content = "Index=" + sindex;
                int start = 1;

                if (!sindex.empty())
                {
                    var itm = sindex.StartPrefix();
                    prefix = itm.Key;
                    start = itm.Value;
                }

                if (ACD.DB._isHatch(objId))
                {
                    PosCollection pls = _orderPointlist(ACD.DB._getHatch(objId), xnotes._props("Sort"));

                    PosCollection segs = pls.GetSegment().Select(ls
                         => ls.OrderBy(p => p.X.roundNumber()).ThenBy(p
                         => p.Y.roundNumber()).ToArray()).ToCollectionSameClosed();

                    foreach (pPos[] ls in segs)
                        lsDims.Add(ls[0].X.roundNumber(0.1) + "," + ls[0].Y.roundNumber(0.1)
                            + ";" + ls[1].X.roundNumber(0.1) + "," + ls[1].Y.roundNumber(0.1));

                    for (int i = 0; i < pls.Count; i++)
                    {
                        pPos[] pts = pls[i];
                        content += "\r\n" + prefix + start + "=" + pts.ToText();

                        pPos ct = pts.CenterPoint();
                        infoIds.AddRange(_showText(prefix + start, (pts.Area()).roundNumber(0.01) + "m2", pt));
                        ACD.DB.CreateText("#M" + prefix + start, ct);
                        start++;

                        pt += new pPos(20, 0);

                        if (i % MAX_CELLS == (MAX_CELLS - 1))
                            pt = new pPos(ACD.DB._getBound(ids)[0].X, pt.Y - 13);
                    }
                }

                //ACD.DB.SetXNotes(objId, content);
            }

            string blockname = ACD.DB.uniqueBlockName("infor");
            ACD.DB.NewBlock(infoIds, blockname);
            //ACD.DB.Insert(blockname, ACD.DB._getBound(ids)[0]);

            lsDims = lsDims.Distinct().ToList();

            if (ACD.AllSelectionXNotes._props("Dim").ToBool())
                foreach (string s in lsDims)
                {
                    pPos[] ls = new PosCollection(s)[0];
                    if (!ls[0]._isVeryClosed(ls[1], 0.5))
                        IDimChain.CreateAlignDimension(ACD.DB, ls[0], ls[1], 0, 0);
                }
            //return res.ToArray();
        }

        static int MAX_CELLS = 11;
        //static int direction = 1;

        static ObjectIdCollection _showText(string name, string area, pPos pt)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            res.Add(ACD.DB.DrawPolyline(pt.Rect(20, -6), true));
            res.Add(ACD.DB.DrawPolyline((pt + new pPos(0, -6)).Rect(20, -6), true));
            res.Add(ACD.DB.CreateText("#M" + name, pt + new pPos(10, -3), 3));
            res.Add(ACD.DB.CreateText("#M" + area, pt + new pPos(10, -9), 2.4));

            return res;
        }

        static void _saveTxtInsidePosCollection(ObjectIdCollection selIds)
        {
            foreach (ObjectId objId in selIds)
                if (ACD.DB._isHatch(objId))
                {
                    PosCollection pls = ACD.DB._getHatch(objId);
                    string content = "";

                    foreach (pPos[] pts in pls)
                    {
                        string st = selIds.ToList().Where(id
                           => ACD.DB._isText(id) && ACD.DB._getPoint(id).Inside(pts))
                                .Select(id => ACD.DB._getContent(id)).ToTextStr(",");

                        if (!st.empty())
                            content += st + "=" + pts.ToText() + "\r\n";

                        //ACD.DB.SetXNotes(objId, content);
                    }
                }
        }

        static int resolution = 1;

        static ObjectIdCollection _filterHatch(ObjectIdCollection ids)
        {
            return ids.ToList().Where(id => ACD.DB._isHatch(id)).OrderBy(id => -ACD.DB._getBound(id).Rect().Area()).ToCollection();
        }

        static void _autoCollectRegionIds(ObjectIdCollection ids)
        {
            ObjectIdCollection infoIds = new ObjectIdCollection();
            //string[] xnotes = ids.ToList().SelectMany(id => ACD.DB.GetXNotes(id)).ToArray();
            string prefix = ACD.AllSelectionXNotes._props("Index");
            //ACD.WR("A2");
            int start = 0;

            if (!prefix.empty())
            {
                var itm = prefix.StartPrefix();
                prefix = itm.Key;
                start = itm.Value;
            }

            if (start == 0)
                start = 1;
            //ACD.WR("A3");
            pPos pt = ACD.DB._getBound(ids)[0];
            ObjectIdCollection txtIds = new ObjectIdCollection();

            //ACD.WR("A4");

            foreach (ObjectId id in ids)
                if (ACD.DB._isBlock(id))
                {
                    if (ACD.DB.GetXNotes(id)._props("Sub").ToBool())
                    {
                        //ACD.WR("OK0");
                        List<string> contents = new List<string>(); //ACD.DB.GetXNotes(id).ToTextStr("\r\n")
                                                                    //._setprop("Index", prefix + (start > 0 ? start.ToString() : ""))
                                                                    //._setprop("Sort", ACD.AllSelectionXNotes._props("Sort"));
                                                                    //if (start > 1)
                                                                    //xnotes = xnotes._setprops("Index", prefix + start);
                                                                    //ACD.WR("OK1.1");
                        ACD.DB.BlockEntitiesAction(id, (subIds) =>
                        {
                            //ObjectIdCollection subIds = ACD.DB.ExplodeBlock(id);

                            PosCollection pls = ACD.DB._getAllVertices(subIds, 32);//.Move(ACD.DB._getPoint(id));
                            ObjectIdCollection hIds = _filterHatch(subIds);

                            pls.AddClosedVertex();

                            int v = 1000;
                            GraphConsole.treshold = 100;

                            GraphConsole.Compute(pls.Select(ls => ls.Select(p => p * v).ToArray()));
                            PosCollection regions = GraphConsole.ResultPts.Select(ls
                                => ls.Select(p => p / v).ToArray()).ToCollectionSameClosed();

                            if (regions.Count > 0)
                            {
                                List<string> lsDims = new List<string>();

                                regions = _orderPointlist(regions, ACD.AllSelectionXNotes._props("Sort"));

                                //regions = cts.Select(p => regions[(int)p.Content.ToNumber()]).ToCollectionSameClosed(true);

                                PosCollection segs = regions.GetSegment().Select(ls
                                         => ls.OrderBy(p => p.X.roundNumber()).ThenBy(p
                                         => p.Y.roundNumber()).ToArray()).ToCollectionSameClosed();

                                foreach (pPos[] ls in segs)
                                    lsDims.Add(ls[0].X.roundNumber(0.1) + "," + ls[0].Y.roundNumber(0.1)
                                        + ";" + ls[1].X.roundNumber(0.1) + "," + ls[1].Y.roundNumber(0.1));

                                string order = ACD.AllSelectionXNotes._props("Order");
                                int[] order_list = new int[0];
                                if (!order.empty())
                                {
                                    int[] ar = order.filter(",").Select(s => (int)s.ToNumber()).ToArray();
                                }
                                //ACD.WR("OK1.3");
                                for (int i = 0; i < regions.Count; i++)
                                    regions[i][0].Content = prefix + start;

                                for (int i = 0; i < regions.Count; i++)
                                {
                                    pPos[] pts = regions[i];

                                    pPos ct = pts.Centroid();

                                    string s_start = start.ToString();

                                    if (start == 4)
                                        s_start = "3A";
                                    else if (start == 13)
                                        s_start = "12A";
                                    else if (start == 14)
                                        s_start = "12B";

                                    double area = (pts.Area()).roundNumber(0.01);

                                    if (Math.Abs(area - 500) < 8) area = 500;

                                    infoIds.AddRange(_showText(ACD.DB.GetXNotes(id)._props("Name") + s_start, area + "m2", pt));

                                    txtIds.Add(ACD.DB.CreateText("#M" + prefix + s_start, ct));

                                    contents.Add(prefix + s_start + "=" + pts.ToText());

                                    start++;

                                    pt += new pPos(20, 0);

                                    if (i % MAX_CELLS == (MAX_CELLS - 1))
                                        pt = new pPos(ACD.DB._getBound(ids)[0].X, pt.Y - 13);
                                }
                            }

                            string[] str = ACD.DB.GetXNotes(id).Where(s
                                => s._firstPropName() != s._firstPropName().ToNumber().ToString()).ToArray();

                            foreach (string s in contents)
                                str = str._setprops(s._firstPropName(), s._firstProp());

                            //ACD.DB.SetXNotes(id, str);
                        });
                    }
                }

            string blockname = ACD.DB.uniqueBlockName("txt_");
            pPos basept = ACD.DB._getBound(txtIds)[0];
            ACD.DB.MoveObject(txtIds, basept.Invert);
            ACD.DB.NewBlock(txtIds, blockname);

            ACD.DB.Insert(blockname, basept);

            blockname = ACD.DB.uniqueBlockName("infor_");
            ACD.DB.NewBlock(infoIds, blockname);
            ACD.DB.Insert(blockname, ACD.DB._getBound(ids)[0]);
        }

        static int _directionFrString(string sD)
        {
            int direction = 1;
            if (sD.StartsWith("-"))
            {
                direction = -1;
                sD = sD.Substring(1);
            }

            if (sD.Upper() == "X")
                direction *= 1;
            else
                direction *= 2;

            return direction;
        }

        //static string[] ACD.AllSelectionXNotes;
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();
                //ACD.AllSelectionXNotes = ACD.DB.ACD.AllSelectionXNotes(selIds);

                string mode = ACD.ED.GetInputString("Enter mode (Auto/Manual/Save)", "A");

                if (mode.st_("A"))
                {
                    ACD.WR("A1");
                    _autoCollectRegionIds(selIds);
                }
                else if (mode.st_("M"))
                {
                    int direction = 1;

                    bool b = selIds.ToList().SelectMany(id => ACD.DB.GetXNotes(id)).Count() > 0;
                    string prefix = "", sD;

                    if (!b)
                    {
                        prefix = ACD.ED.GetInputString("Input prefix char", "");
                        sD = ACD.ED.GetInputString("Align by (X/Y)", "X");
                        //direction = _directionFrString(sD);

                        //foreach (ObjectId objId in selIds)
                            //ACD.DB.SetXNotes(objId, "Index=" + prefix + "\r\nSort=" + sD);
                    }

                    _showHatchArea(selIds);
                    //string[] contents = _showHatchArea(selIds, prefix);
                    //string st = HatchInfo(selIds);
                    //ACD.DB.CreateText("#L" + contents.ToText("; ").Replace("\r\n",":"), ACD.DB._getBound(selIds)[0]);
                }
                else if (mode.st_("S"))
                {
                    _saveTxtInsidePosCollection(selIds);
                }
            }

            ACD.Focus();
        }
    }
}

