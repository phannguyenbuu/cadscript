using System;
using System.Collections.Generic;
using System.Linq;

namespace AcadScript
{
    public class SEG_PAIR
    {
        public int[] Segs, duplicateIndexes;
        public pPos Intersect;

        public SEG_PAIR(int[] _segs, pPos pt)
        {
            Segs = _segs;
            Intersect = pt;
            duplicateIndexes = new int[] { };
        }

        void _addIntersect(int index)
        {
            List<pPos> tmp = SEGMENT_CROSS.SegList[Segs[index]].ToList();
            tmp.Add(Intersect);
            SEGMENT_CROSS.SegList[Segs[index]] = tmp.ToArray();
        }

        public void DivideSeg()
        {
            if (duplicateIndexes.Length > 0)
                foreach (int i in duplicateIndexes)
                {
                    int a = Segs[i < 2 ? 0 : 1];
                    int b = i == 0 || i == 2 ? 0 : 1;
                    SEGMENT_CROSS.SegList[a][b] = Intersect;
                }

            if (!(duplicateIndexes.Contains(0) && duplicateIndexes.Contains(1)))
                _addIntersect(0);
            if (!(duplicateIndexes.Contains(2) && duplicateIndexes.Contains(3)))
                _addIntersect(1);
        }
    }

    public static class SEGMENT_CROSS
    {
        public static double treshold = 1;
        public static PosCollection SegList;
        public static List<int> lsSplineIndex;
        static List<bool[]> lsSplineExt; //-1:cannot 0:first 1:second 2:all
        static int current_spline;

        public static void Init()
        {
            SegList = new PosCollection();
            lsSplineIndex = new List<int>();
            lsSplineExt = new List<bool[]>();
            current_spline = 0;
        }

        public static void AddVertList(pPos[] ls)
        {
            bool closed = ls.First().DistanceTo(ls.Last()) < treshold;

            for (int i = 0; i < ls.Length; i++)
            {
                int nex = i == ls.Length - 1 ? (closed ? 0 : -1) : i + 1;
                if (nex != -1)
                {
                    SegList.Add(new pPos[] { ls[i], ls[nex] });
                    lsSplineIndex.Add(current_spline);

                    bool[] ar = new bool[] { false, false };
                    if (ls.Length == 2)
                        ar = new bool[] { true, true };
                    else if (!closed)
                    {
                        if (i == 0) ar[0] = true;
                        else if (i == ls.Length - 2) ar[1] = true;
                    }

                    lsSplineExt.Add(ar);
                }
            }

            current_spline++;
        }
        
        public static void collectSegments()
        {
            List<SEG_PAIR> seg_pairs = new List<SEG_PAIR>();

            for (int i = 0; i < SegList.Count - 1; i++)
            {
                List<pPos> _intersectsIn = new List<pPos>();
                List<SEG_PAIR> tmpSEG = new List<SEG_PAIR>();

                for (int j = i + 1; j < SegList.Count; j++)
                    if (lsSplineIndex[i] != lsSplineIndex[j])
                    {
                        SegIntersect.can_extents = new bool[]{lsSplineExt[i][0],lsSplineExt[i][1],
                                                            lsSplineExt[j][0],lsSplineExt[j][1]};

                        SegIntersect.CalcIntersection(treshold,
                            new pPos[] { SegList[i].First(), SegList[i].Last(), SegList[j].First(), SegList[j].Last() });

                        SEG_PAIR itm = new SEG_PAIR(new int[] { i, j }, SegIntersect.Intersection);
                        itm.duplicateIndexes = SegIntersect.duplicateIndexes.ToArray();

                        if (SegIntersect.SegmentIntersect)
                        {
                            seg_pairs.Add(itm);
                            _intersectsIn.Add(itm.Intersect);
                        }
                        else if (SegIntersect.HalfIntersect)
                            tmpSEG.Add(itm);
                    }

                foreach (SEG_PAIR tmp in tmpSEG)
                    if (tmp.Intersect != null && !_intersectsIn.Any(pt => pt.DistanceTo(tmp.Intersect) < treshold))
                        seg_pairs.Add(tmp);
            }
            //_drawSeg();
            foreach (SEG_PAIR itm in seg_pairs) itm.DivideSeg();

            //_drawSeg();

            for (int i = 0; i < SegList.Count; i++)
            {
                pPos pt = new pPos(SegList[i][0].X, SegList[i][0].Y);
                SegList[i] = SegList[i].OrderBy(itm => itm.DistanceTo(pt)).ToArray();
            }

            //_drawSeg();
        }
    }

    public class pADJ
    {
        public List<double> Angles;
        public List<int> list;
        public pPos pos;

        public pADJ(pPos _pt)
        {
            pos = _pt;
            list = new List<int>();
            Angles = new List<double>();
        }

        public void removeAt(int v)
        {
            list.RemoveAt(v);
            Angles.RemoveAt(v);
        }

        public bool contain(int v) { return list.Contains(v); }

        public void add(int v) { list.Add(v); }

        public void remove(int v) { list.Remove(v); }

        public void clear(int index)
        {
            //ACD.DB.DrawCircle(verts[i], 500);
            //ACD.DB.CreateText("v" + index.ToString(), GraphConsole.verts[index], 500);
            foreach (int v in list)
            {
                GraphConsole.lsAdj[v].remove(index);
                if (GraphConsole.lsAdj[v].list.Count == 1) GraphConsole.lsAdj[v].clear(v);
            }

            list = new List<int>();
            Angles = new List<double>();
        }

        public void sort()
        {
            list = list.OrderBy(v => v).ToList();
        }

        public static List<pADJ> CloneAdjList(List<pADJ> lsSrc)
        {
            List<pADJ> res = new List<pADJ>();
            foreach (pADJ pj in lsSrc)
            {
                pADJ tmp_pj = new pADJ(pj.pos);
                tmp_pj.list.AddRange(pj.list);
                tmp_pj.Angles.AddRange(pj.Angles);
                res.Add(tmp_pj);
            }
            return res;
        }
    }

    public static class GraphConsole
    {
        public static double treshold = 1;
        public static List<pADJ> lsAdj;
        public static List<pPos> verts;
        public static PosCollection ResultPts, ResultBorders;
        public static List<int[]> cycles;

        static pPos[] _vListInt(IEnumerable<int> ls)
        {
            return (from n in ls select verts[n]).ToArray();
        }

        static bool _isIntersectCycle(int[] cycle1, int[] cycle2)
        {
            return cycle1.Any(v => cycle2.Contains(v));
        }

        static int _inHis(List<int[]> results, List<int[]> cycles, int v)
        {
            int res = -1;
            for (int i = 0; i < results.Count; i++)
            {
                int index = Array.FindIndex(results[i], n => _isIntersectCycle(cycles[n], cycles[v]));
                if (index != -1)
                {
                    res = i;
                    break;
                }
            }
            return res;
        }
        
        static int[] _findOutlines(List<int[]> cycles)
        {
            bool[] T = (from itm in cycles select false).ToArray();
            List<int[]> his = new List<int[]>();

            for (int i = 0; i < cycles.Count; i++)
                if (!T[i])
                {
                    T[i] = true;
                    int index = _inHis(his, cycles, i);
                    List<int> ls;
                    if (index == -1)
                    {
                        ls = new List<int> { i };
                        index = his.Count;
                    }
                    else
                        ls = his[index].ToList();

                    for (int j = i + 1; j < cycles.Count; j++)
                        if (!T[j] && ls.Any(k => _isIntersectCycle(cycles[k], cycles[j])))
                        {
                            T[j] = true;
                            ls.Add(j);
                        }

                    if (index == his.Count)
                        his.Add(ls.ToArray());
                    else
                        his[index] = ls.ToArray();
                }

            List<int> res = new List<int>();

            for (int i = 0; i < his.Count; i++)
            {
                int[] ls = his[i].OrderBy(index => _vListInt(cycles[index]).Area()).ToArray();
                res.Add(ls.Last());
            }

            return res.ToArray();
        }

        static void _extractResultsAndBorders(List<int[]> cycles)
        {
            int[] border_indexes = _findOutlines(cycles);

            ResultPts = new PosCollection();
            ResultBorders = new PosCollection();

            for (int i = 0; i < cycles.Count; i++)
                if (!border_indexes.Contains(i))
                    ResultPts.Add(cycles[i].Select(v => verts[v]).ToArray());
                else
                    ResultBorders.Add(cycles[i].Select(v => verts[v]).ToArray());

            ResultPts = ResultPts.OrderBy(ls => -ls.Area()).ToCollectionSameClosed();
            ResultBorders = ResultBorders.OrderBy(ls => -ls.Area()).ToCollectionSameClosed();
        }
                
        static int _addVert(pPos pt)
        {
            int res = verts.FindIndex(itm => itm.DistanceTo(pt) < treshold);

            if (res == -1)
            {
                verts.Add(pt);
                lsAdj.Add(new pADJ(pt));

                res = verts.Count - 1;
            }

            return res;
        }

        static void _removeEdge(List<pADJ> ls, int v, int w)
        {
            if ((v >= 0 && v < verts.Count) && (w >= 0 && w < verts.Count) && v != w)
            {
                if (ls[v].contain(w)) ls[v].remove(w);
                if (ls[w].contain(v)) ls[w].remove(v);
            }
        }

        static void _addEdge(int v, int w)
        {
            if ((v >= 0 && v < verts.Count) && (w >= 0 && w < verts.Count) && v != w)
            {
                if (!lsAdj[v].contain(w)) lsAdj[v].add(w);
                if (!lsAdj[w].contain(v)) lsAdj[w].add(v);
            }
        }

        static void _collectVerts(PosCollection SegList)
        {
            verts = new List<pPos>();

            foreach (pPos[] ls in SegList)
            {
                int[] tmp = (from pt in ls select _addVert(pt)).ToArray();
                for (int i = 0; i < tmp.Length - 1; i++)
                    if (tmp[i] != tmp[i + 1]) _addEdge(tmp[i], tmp[i + 1]);
            }
        }

        static void _collectAdj()
        {
            bool b = true;
            while (b)
            {
                b = false;
                for (int i = 0; i < lsAdj.Count; i++)
                    if (lsAdj[i].list.Count == 1)
                    {
                        _removeEdge(lsAdj, i, lsAdj[i].list.First());
                        lsAdj[i].clear(i);
                        b = true;
                    }
            }
            //_drawNode(500);

            for (int i = 0; i < lsAdj.Count; i++)
                if (lsAdj[i].list.Count > 0)
                {
                    pADJ itm = lsAdj[i];
                    itm.sort();
                    itm.Angles = new List<double>();

                    for (int j = 0; j < itm.list.Count; j++)
                    {
                        pPos p = itm.pos - lsAdj[itm.list[j]].pos;
                        itm.Angles.Add(Math.Atan2(p.Y, p.X) / Math.PI * 180);
                    }
                }
        }

        static List<int[]> _detectCycles(List<pADJ> _lsSrcAdj)
        {
            List<pADJ> adjs = pADJ.CloneAdjList(_lsSrcAdj);
            List<int[]> result = new List<int[]>();

            //DE._loadPRG(adjs.Count);

            for (int i = 0; i < adjs.Count; i++)
            {
                if (adjs[i].list.Count > 0)
                {
                    pADJ adj = adjs[i];

                    while (adj.list.Count > 0)
                    {
                        List<int> pos_array = new List<int> { i };
                        int w = i;
                        int j = adj.list[0];
                        //--------------- select next point again and again
                        if (j != i)
                            pos_array.Add(j);

                        double nulla = adjs[w].Angles[0] - 180;
                        adj.removeAt(0);

                        if (nulla < 0)
                            nulla += 360;

                        while (j != i)
                        {
                            double min = 360;
                            int min_i = 0;

                            for (int k = 0; k < adjs[j].list.Count; k++)
                                if (adjs[j].list[k] != w)
                                {
                                    double x = adjs[j].Angles[k] - nulla;
                                    if (x < 0)
                                        x += 360;
                                    if (x < 0)
                                        x += 360;
                                    if (x >= 360)
                                        x -= 360;
                                    if (x < min)
                                    {
                                        min = x;
                                        min_i = k;
                                    }
                                }

                            w = j;
                            j = adjs[j].list[min_i];
                            nulla = adjs[w].Angles[min_i] - 180;
                            if (nulla < 0)
                                nulla += 360;
                            if (j != i)
                                pos_array.Add(j);

                            adjs[w].removeAt(min_i);
                        }

                        if (pos_array.Count > 2)
                        {
                            //db.WRArray("PARRY", pos_array);
                            result.Add(pos_array.ToArray());
                        }
                    }
                }
                //DE._increasePRG();
            }
            //DE._unloadPRG();
            result = result.OrderBy(itm => _vListInt(itm).Area()).ToList();
            return result;
        }

        public static int FindByPoint(pPos pt)
        {
            int res = -1;
            for (int i = 0; i < ResultPts.Count; i++)
                if (pt.Inside(ResultPts[i]))
                {
                    res = i;
                    break;
                }
            return res;
        }

        public static void Compute(IEnumerable<pPos[]> pls)
        {
            SEGMENT_CROSS.treshold = treshold;

            verts = new List<pPos>();
            lsAdj = new List<pADJ>();

            SEGMENT_CROSS.Init();

            for (int i = 0; i < pls.Count(); i++)
            {
                PosCollection tmp_ls = pls.ElementAt(i).Explode();
                foreach(pPos[] ls in tmp_ls) SEGMENT_CROSS.AddVertList(ls);
            }

            SEGMENT_CROSS.collectSegments();
            _collectVerts(SEGMENT_CROSS.SegList);

            /*string st = "Count " + lsAdj.Count + ";";
            for (int i = 0; i < GraphConsole.lsAdj.Count; i++)
            {
                st += i + ":" + lsAdj[i].pos + "(" + lsAdj[i].list.Count + "):";
                foreach (int n in lsAdj[i].list)
                    st += n + ";";

                st += "\r\n";
            }
            ACD.WR("ADJ {0}", st);*/

            _collectAdj();
            
            cycles = _detectCycles(lsAdj);
            //ResultIndexes = cycles;
            _extractResultsAndBorders(cycles);

            //for (int i = 0; i < cycles.Count; i++)
            //db.WRArray(String.Format("Cycle {0}", i),cycles[i]);
        }

        static bool _isList(params int[] ls)
        {
            return (ls[0] == ls[2] && ls[1] == ls[3]) || (ls[0] == ls[3] && ls[1] == ls[2]);
        }
    }

    public class DepthFirstSearch
    {
        private bool[] marked;
        private int[] edgeTo;
        private int s;

        public DepthFirstSearch(GraphAdjList G, int s)
        {
            marked = new bool[G.VertexCount];
            edgeTo = new int[G.VertexCount];
            this.s = s;
        }

        public void DFS(GraphAdjList G, int v)
        {
            marked[s] = true;

            foreach (var w in G.GetAdj(v))
            {
                if (!marked[w])
                {
                    DFS(G, w);
                    edgeTo[w] = v;
                }
            }
        }

        public bool HasPathTo(int v)
        {
            return marked[v];
        }

        public IEnumerable<int> GetPathTo(int v)
        {
            if (!HasPathTo(v))
                return null;

            var stack = new Stack<int>();

            for (var x = v; x != s; x = edgeTo[x])
            {
                stack.Push(x);
            }

            stack.Push(s);

            return stack;
        }
    }

    public class GraphAdjList
    {
        private readonly int V;
        private readonly List<int>[] Adj;

        public GraphAdjList(int v)
        {
            V = v;
            Adj = new List<int>[V];

            for (int i = 0; i < V; i++)
            {
                Adj[i] = new List<int>();
            }
        }

        public void AddEdge(int v, int w)
        {
            Adj[v].Add(w);
            Adj[w].Add(v);
        }

        public List<int> GetAdj(int v)
        {
            return Adj[v];
        }

        public int VertexCount
        {
            get
            {
                return V;
            }
        }
    }
    
    public class SearchGraph
    {
        public int START;
        public int END;
        public LinkedList<int>[] adj;

        public void DepthFirstSearch(LinkedList<int> visited)
        {
            // int v = visited.Last();

            LinkedList<int> nodes = adj[visited.Last()];

            // examine adjacent nodes
            foreach (int node in nodes)
            {
                if (visited.Contains(node))
                    continue;

                if (node == END)
                {
                    visited.AddFirst(node);
                    //printPath(visited);
                    //visited.RemoveLast();
                    break;
                }
            }

            // in breadth-first, recursion needs to come after visiting adjacent nodes
            foreach (int node in nodes)
            {
                if (visited.Contains(node) || node == END)
                    continue;

                visited.AddLast(node);
                DepthFirstSearch(visited);
                //visited.RemoveLast();
            }

            //printPath(visited);
        }

        public void printPath(LinkedList<int> visited)
        {
            //string st = "";
            //foreach (int node in visited)
            //    st += node.ToString() + ",";

            //ACD.WR("Node_PAath {0}", st);
        }
    }
}