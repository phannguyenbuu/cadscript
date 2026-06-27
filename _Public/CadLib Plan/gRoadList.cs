using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcadScript
{
    public class gRoadElement
    {
        public PosCollection EndCaps, Pavements;
        public List<int> WalkCaps; // save index of endcaps is walk
        public pPos[] _pointList;
        double road_min, road_max;

        public gRoadElement(pPos[] __src, double _rmin = 3, double _rmax = 12)
        {
            WalkCaps = new List<int>();

            road_min = _rmin;
            road_max = _rmax;

            PointList = __src;
        }

        public PosCollection ExtendCaps
        {
            get
            {
                return DE.NumericArray(0, EndCaps.Count - 1)
                    .Where(_n => !WalkCaps.Contains(_n))
                    .Select(_n => EndCaps[_n]).ToCollectionSameClosed(false);
            }
        }

        public pPos[] GeneratePavementElements(double offset, double range)
        {
            List<pPos> res = new List<pPos>();
            offset *= cReadData.__sc;
            //double pointDist = range * cReadData.__sc;

            foreach (pPos[] _pave in Pavements)
            {
                pPos[] pts = SpacingTool.ParralellLine(_pave, -offset);

                if (pts != null)
                    res.AddRange(SpacingTool.RangePoints(pts, range * cReadData.__sc, true));
            }

            return res.ToArray(); 
        }

        bool isCorner(pPos p1, pPos p2, pPos p3)
        {
            if (p2.DistanceTo(p1, p3) <= cReadData.__sc * 0.02)
                return false;

            pPos __p1 = p2.Along(3, p1);
            pPos __p3 = p2.Along(4, p3);

            double _d3 = __p3.DistanceTo(__p1);

            return Math.Abs(_d3 * _d3 - 25) <= 25;
        }

        public PosCollection _getEndCapsRoad(pPos[] pts)
        {
            PosCollection res = new PosCollection();
            pPos[] __pts = pts;

            for (int i = 0; i < __pts.Length; i++)
            {
                pPos p1 = __pts[i], p2 = __pts[(i + 1) % __pts.Length],
                    p3 = __pts[(i + 2) % __pts.Length], p4 = __pts[(i + 3) % __pts.Length];

                double _d0 = p2.DistanceTo(p3);

                //ACD.WR("Compare <{0},{1},{2}>,({3},{4}),{5},{6},{7},{8}",
                //    _d0, (road_min - .5) * cReadData.__sc, 
                //    (road_max + .5) * cReadData.__sc,
                //    (road_min - .5) * cReadData.__sc,
                //    (road_max + .5) * cReadData.__sc,
                //    road_min, road_max,
                //    p1.isLeft(p2, p3) == p4.isLeft(p2, p3),
                //    isCorner(p1, p2, p3) && isCorner(p2, p3, p4),
                //    p2.Along(_d0 * 2, p1).DistanceTo(p2.Along(_d0 * 2, p3)) <= _d0 * 3.2);

                if (_d0 >= (road_min - .5) * cReadData.__sc && _d0 <= (road_max + .5) * cReadData.__sc
                    && p1.isLeft(p2, p3) == p4.isLeft(p2, p3)
                    && isCorner(p1, p2, p3) && isCorner(p2, p3, p4)
                    && p2.Along(_d0 * 2, p1).DistanceTo(p2.Along(_d0 * 2, p3)) <= _d0 * 3.2)
                        res.Add(new pPos[] { p2, p3 });
            }

            return res;
        }

        public pPos[] CenterLine
        {
            get
            {
                //ACD.WR("EndCaps {0}", EndCaps.Count);

                if (Pavements.Count == 2)
                {
                    //ACD.WR("EndA1");
                    pPos __firstpoint = (EndCaps[0][0] + EndCaps[0][1]) / 2, __lastpoint = (EndCaps[1][0] + EndCaps[1][1]) / 2;
                    double v = EndCaps[0].Length(false) / 2;
                    //ACD.WR("EndA2 {0}", Pavements[0]);
                    pPos[] _ls1 = SpacingTool.ParralellLine(Pavements[0], v);

                    if (_ls1 != null)
                    {
                        List<pPos> res = _ls1.ToList();
                        //ACD.WR("EndC1");
                        if (res[0].DistanceTo(__firstpoint) > res[0].DistanceTo(__lastpoint))
                        {
                            res.Insert(0, __lastpoint);
                            res.Add(__firstpoint);
                        }
                        else
                        {
                            res.Insert(0, __firstpoint);
                            res.Add(__lastpoint);
                        }
                        //ACD.WR("EndC2");
                        return res.ToArray();
                    }
                }
                
                return null;
            }
        }

        bool _comparepair(pPos a, pPos b, pPos _a, pPos _b)
        {
            return (a == _a && b == _b) || (a == _b && b == _a);
        }

        public pPos[] PointList
        {
            set
            {
                _pointList = value;
                EndCaps = _getEndCapsRoad(_pointList);

                //ACD.WR("EndCaps {0}", EndCaps.Count);

                Pavements = new PosCollection();
                int total = _pointList.Length;

                for (int ax = 0; ax < EndCaps.Count; ax++)
                {
                    int __nax = (ax + 1) % EndCaps.Count;
                    List<pPos> res = new List<pPos>();
                    //ACD.WR("E0");
                    for (int i = 0; i < total; i++)
                    {
                        int nex = (i + 1) % total;

                        if (_comparepair(_pointList[i], _pointList[nex], EndCaps[ax][0], EndCaps[ax][1]))
                        {
                            res = new List<pPos> { _pointList[nex] };
                            //ACD.WR("E1");
                            for (int j = nex; j < total * 2; j++)
                            {
                                int __nex = (j + 1) % total;

                                if (!_comparepair(_pointList[j % total], _pointList[__nex], EndCaps[__nax][0], EndCaps[__nax][1]))
                                    res.Add(_pointList[__nex]);
                                else
                                    break;
                            }
                            //ACD.WR("E2");
                            break;
                        }
                    }

                    Pavements.Add(res.ToArray());
                }
            }

            get
            {
                return _pointList;
            }
        }

        public PosCollection RoadSplitToRegions
        {
            get
            {
                PosCollection res = new PosCollection();

                PosCollection pls = new PosCollection();
                pls.AddRange(Pavements);
                pls.AddRange(EndCaps);

                Pavements.Closed = DE.NumericArray(0, Pavements.Count - 1).Select(_n => false).ToArray();
                PosCollection segs = Pavements.GetSegment()
                    .Where(_ls => _ls.Length(false) >= 1 * cReadData.__sc).ToCollectionSameClosed(false);

                for (int i = 0; i < segs.Count; i++)
                    foreach(pPos _p in segs[i])
                        for (int j = 0; j < segs.Count; j++)
                            if(i != j)
                            {
                                _p.DistanceToPts(segs[j], false);
                                var _prj = pPos.DistanceTo_Projection;

                                if(_prj != null && _prj.IsBetween(segs[j][0], segs[j][1]))
                                    pls.Add(new pPos[] { _p, _prj });
                            }

                pls.Closed = DE.NumericArray(0, pls.Count - 1).Select(_n => false).ToArray();

                GraphConsole.Compute(pls);
                res = GraphConsole.ResultPts;
                
                if(res.Count  == 0)
                    res.Add(_pointList);

                res.Closed = DE.NumericArray(0, res.Count - 1).Select(_n => false).ToArray();
                return res;
            }
        }


        public static PosCollection GetWalkList(IEnumerable<gRoadElement> lsRoads)
        {
            PosCollection res = new PosCollection();
            int total = lsRoads.Count();

            //ACD.WR("Total roads {0}", total);

            List<bool[]> _history = DE.NumericArray(0, total)
                .Select(_m => DE.NumericArray(0, total).Select(_n => false).ToArray()).ToList();

            for (int i = 0; i < total - 1; i++)
                if (_history[i].Where(_b => !_b).Count() > 0)
                {
                    var mid_segs1 = lsRoads.ElementAt(i).EndCaps.Select(__ls => (__ls[0] + __ls[1]) / 2).ToArray();
                    //ACD.WR("ok2 {0}", lsRoads.ElementAt(i).EndCaps.Count);

                    for (int j = i + 1; j < total; j++)
                        if (_history[j].Where(_b => !_b).Count() > 0)
                        {
                            var segs2 = lsRoads.ElementAt(j).EndCaps;

                            for (int k = 0; k < mid_segs1.Length; k++)
                                if (!_history[i][k])
                                {
                                    for (int m = 0; m < segs2.Count; m++)
                                        if (mid_segs1[k].IsBetween(segs2[m][0], segs2[m][1]))
                                        {
                                            _history[i][k] = _history[j][m] = true;

                                            lsRoads.ElementAt(i).WalkCaps.Add(k);
                                            lsRoads.ElementAt(j).WalkCaps.Add(m);

                                            pPos p1 = segs2[m][0];
                                            pPos p2 = segs2[m][1];
                                            
                                            res.Add(new pPos[] { p1, p2 });
                                        }
                                }
                        }
                }

            //ACD.WR("Walks {0}", res.Count);
            return res;
        }
    }
}
