using System;
using System.Collections.Generic;
using System.Linq;

namespace AcadScript
{
    public class gVrayProxyElement : List<pPos>
    {
        public string ProxyName;
        public List<double> Scale;

        public gVrayProxyElement(string __name, IEnumerable<pPos> _pts, double __scale)
        {
            ProxyName = __name;
            AddRange(_pts);
            Scale = DE.NumericArray(0, _pts.Count() - 1).Select(__p => __scale).ToList();
        }

        public void ToHtml()
        {
            WriteHtmlCLS.AddVrayItem("Mesh",
                String.Format("vrs.AddVrayProxy('{0}', position = [{1}], scale=[{2}])",
                ProxyName,

                this.Select(_p => "(" + _p.X.roundNumber(0.01) + "," + _p.Y.roundNumber(0.01) + ","
                    + _p.Z.roundNumber(0.01) + "," + _p.Rotation.roundNumber(0.01) + ")").ToTextStr(","),
                Scale.ToTextDouble(","))
            );
        }
    }


    public class gRoadElement:gRegionElement
    {
        public PosCollection EndCaps, Pavements, WalkList;
        public gRoadChild[] Children;
        public double road_min = 3, road_max = 20;

        public gRoadElement(IEnumerable<pPos> _pts, double _rmin = 3, double _rmax = 20) : base(_pts)
        {
            road_min = _rmin;
            road_max = _rmax;

            //ACD.WR("R1");

            Children = new gRoadChild[0];

            //ACD.WR("R2");

            this.Clear();
            this.AddRange(_pts);
            MtlName = "MT_ROAD";

            _updateEndCapsRoad();
            _updateRoadChildren();

            //ACD.WR("R3");

            _updatePavementList();

            //ACD.WR("R4");

            if (Children.Length > 0 && RoadWidths.Length > 0)
            {
                double v = RoadWidths[0] / cReadData.__sc;
                //ACD.WR("P8");
                PosCollection _center_lines = PavementOffset(v / 2, 5, (_p, _ls) =>
                {
                    if (_p.Inside(this))
                    {
                        _ls.Add(_p);
                        return true;
                    }

                    return false;
                });

                

                foreach (var itm in Children)
                    if(itm.IsStraight)
                        itm.UpdateCenterLine(_center_lines);
            }

            //ACD.WR("R5");
        }

        public double[] RoadWidths
        {
            get
            {
                return EndCapAndWalkList.Select(_ls => _ls.Length(false)).OrderBy(_v => _v).ToArray();
            }
        }

        public PosCollection ArrowCaps
        {
            get
            {
                PosCollection res = new PosCollection();

                foreach (pPos[] pts in EndCaps)
                {
                    pPos[] _line = pts[0].Parallel(pts[1], 5 * cReadData.__sc);

                    if (_line.CenterPoint().Inside(this))
                    {
                        _line = pts[0].Parallel(pts[1], -5 * cReadData.__sc);

                        if (!_line.CenterPoint().Inside(this))
                        {
                            pPos p = _line.CenterPoint();
                            p.Rotation = (p - pts.CenterPoint()).Angle();

                            res.Add(new pPos[] { p, pts[0], pts[1] });
                        }
                    } else if (!_line.CenterPoint().Inside(this))
                    {
                        pPos p = _line.CenterPoint();
                        p.Rotation = (p - pts.CenterPoint()).Angle();

                        res.Add(new pPos[] { p, pts[0], pts[1] });
                    }
                }

                return res;
            }
        }

        public PosCollection EndCapAndWalkList
        {
            get
            {
                PosCollection res = new PosCollection();
                res.AddRange(EndCaps);
                res.AddRange(WalkList);

                return res;
            }
        }

        public void _updateRoadChildren()
        {
            PosCollection pls = new PosCollection();
            pls.Add(this.ToArray());
            pls.AddRange(WalkList.Select(_ls => _ls.ExtentLine(1000)));

            GraphConsole.Compute(pls);
            //ACD.WR("P4");
            Children = GraphConsole.ResultPts.Select(_ls => new gRoadChild(_ls)).ToArray();
            //ACD.WR("P5");
        }

        public PosCollection PavementOffset(double offset, double range, Func<pPos, List<pPos>, bool> fn)
        {
            PosCollection res = new PosCollection();
            offset *= cReadData.__sc;

            PosCollection fillet_pls = FilletPoints;
            
            foreach (pPos[] _pave in Pavements)
            {
                List<pPos> pts_1 = new List<pPos>();
                List<bool> res_bool_1 = new List<bool>();
                List<pPos> pts_2 = new List<pPos>();
                List<bool> res_bool_2 = new List<bool>();

                pPos[] points = SpacingTool.RangePoints(_pave, range * cReadData.__sc, true);
                
                foreach (pPos _p in points)
                    if (!_p.InsidePls(fillet_pls))
                    {
                        _p.DistanceToPts(_pave);
                        if (pPos.DistanceTo_Index >= 0 && pPos.DistanceTo_Index < _pave.Length)
                        {
                            pPos p = _pave[pPos.DistanceTo_Index];
                            
                            pPos p1 = _pave[pPos.DistanceTo_Index == 0 ? _pave.Length - 1 : pPos.DistanceTo_Index - 1];
                            pPos p2 = _pave[pPos.DistanceTo_Index == _pave.Length - 1 ? 0 : pPos.DistanceTo_Index + 1];
                            
                            if (_p.IsBetween(p, p2))
                                p1 = p2;
                            
                            pPos vt = p - p1;
                            vt = new pPos(-vt.Y, vt.X).Normalize;
                            
                            res_bool_1.Add(fn(_p + vt * offset, pts_1));
                            res_bool_2.Add(fn(_p - vt * offset, pts_2));
                        }
                    }

                if (pts_1.Count > 0 || pts_2.Count > 0)
                    res.Add(res_bool_1.Count(_b => _b) > res_bool_2.Count(_b => _b) ? pts_1.ToArray() : pts_2.ToArray());
            }
            
            return res; 
        }

        public void _updateEndCapsRoad()
        {
            WalkList = new PosCollection();
            EndCaps = new PosCollection();
            //ACD.WR("Length {0}", this.Count);

            for (int i = 0; i < this.Count; i++)
                if (!_isTurnRoad(i))
                {
                    if (!_isTurnRoad(i == this.Count - 1 ? 0 : i + 1))
                    {
                        pPos prev = prevPoint(i);
                        int nex = i == this.Count - 1 ? 0 : i + 1;
                        //ACD.WR("T2");
                        double _d0 = this[i].DistanceTo(nextPoint(i));
                        if (_d0 >= (road_min - .5) * cReadData.__sc && _d0 <= (road_max + .5) * cReadData.__sc)
                        {
                            //pPos[] __pts = ArcCornerFilletPoints(i);
                            //ACD.WR("T3");
                            if (ArcCornerFilletPoints(i) != null && ArcCornerFilletPoints(nex) != null)
                                EndCaps.Add(new pPos[] { this[i], nextPoint(i) });
                            //ACD.WR("T4");
                        }
                    }
                }
                else
                {
                    pPos[] triples = ArcCornerFilletPoints(i, corner_value);
                    //ACD.WR("T5");
                    if(triples != null)
                        for (int ax = 1; ax <= 2; ax++)
                        {
                            pPos pt = triples[ax];
                            //ACD.WR("T5.1");
                            pPos vt = (pt - this[i]).Normalize;
                            vt = new pPos(-vt.Y, vt.X);
                            //ACD.WR("T6");
                            pPos[] interps = this.Intersect(pt, pt + vt * 1000)
                                .Where(_p => _p._isVeryClosed(pt, 2 * road_max * cReadData.__sc)
                                            && !_p._isVeryClosed(pt, 1000))
                                .ToArray();

                            //ACD.WR("Tri :{0}", interps.Length);
                            //ACD.WR("T7");
                            if (interps.Length > 0 
                                && (WalkList.Count == 0 
                                || WalkList.All(_ts => !_ts.CenterPoint()._isVeryClosed((pt + interps[0]) / 2, 2 * cReadData.__sc))))
                                    WalkList.Add(new pPos[] { pt, interps[0] });
                            //ACD.WR("T8");
                        }
                }
        }
        

        public List<int[]> SplitList(int total, params int[] remove_indexes)
        {
            List<int[]> res = new List<int[]>();
            List<int> _ls = new List<int>();

            for (int i = 0; i < total; i++)
                if (remove_indexes.Contains(i))
                {
                    if (_ls.Count > 1)
                    {
                        _ls.Add(i);
                        res.Add(_ls.ToArray());
                        _ls = new List<int>();
                    }
                }
                else
                    _ls.Add(i);
                
            if (_ls.Count > 1)
                res.Add(_ls.ToArray());

            return res;
        }

        public List<int[]> SplitList(IEnumerable<object> src, params object[] values)
        {
            List<int[]> res = new List<int[]>();
            int total = src.Count();

            List<int> _ls = new List<int>();

            for (int i = 0; i < total; i++)
                if (src.ElementAt(i) == null || values.Contains(src.ElementAt(i)))
                {
                    if (_ls.Count > 1)
                    {
                        _ls.Add(i);
                        res.Add(_ls.ToArray());
                        _ls = new List<int>();
                    }
                }
                else
                    _ls.Add(i);
                
            if (_ls.Count > 1)
                res.Add(_ls.ToArray());

            return res;
        }

        public void _updatePavementList()
        {
            var pts = this.ShowPoints;
            int total = pts.Length;
            
            List<int> remove_indexes = new List<int>();

            for (int i = 0; i < total; i++)
            {
                int nex = (i + 1) % total;
                
                for (int j = 0; j < EndCaps.Count; j++)
                    if (((pts[i] + pts[nex])/2)._isVeryClosed((EndCaps[j][0] + EndCaps[j][1]) / 2, 1000))
                    {
                        remove_indexes.Add(i);
                        break;
                    }
            }

            Pavements = SplitList(total, remove_indexes.ToArray())
                .Select(_ls => _ls.Select(_n => pts[_n]).ToArray()).ToCollectionSameClosed(false);
        }
    }

    public class gRoadChild: gRoadElement
    {
        public PosCollection CenterLines;
        public gRoadChild(IEnumerable<pPos> _pts, double _rmin = 3, double _rmax = 20) : base(_pts)
        {
            //ACD.WR("P0");
            road_min = _rmin;
            road_max = _rmax;

            CenterLines = new PosCollection();

            this.Clear();
            this.AddRange(_pts);
            this.Add(this[0]);
            //ACD.WR("P1");
            _updateEndCapsRoad();
            _updatePavementList();
            //ACD.WR("P2");
        }
        
        public void UpdateCenterLine(PosCollection _center_lines)
        {
            //ACD.WR("P3");
            CenterLines = _center_lines.Select(_ls => _ls.Where(__p => __p.Inside(this)).ToArray())
                .Where(_ls => _ls.Length > 0)
                .ToCollectionSameClosed(false);
            //ACD.WR("P4");
            PosCollection extent_targets = EndCaps;

            if (extent_targets.Count > 0)
            {
                //ACD.WR("P5");
                for (int i = 0; i < CenterLines.Count; i++)
                {
                    List<pPos> _nls = CenterLines[i].ToList();
                    //ACD.WR("P4.1 {0}", extent_targets.Count);
                    var _seg = extent_targets.OrderBy(_ls => CenterLines[i][0].DistanceTo(_ls[0], _ls[1])).First();
                    //ACD.WR("P4.2");
                    CenterLines[i][0].DistanceTo(_seg[0], _seg[1]);
                    //ACD.WR("P4.3");
                    _nls.Insert(0, pPos.DistanceTo_Projection);

                    _seg = extent_targets.OrderBy(_ls => CenterLines[i].Last().DistanceTo(_ls[0], _ls[1])).First();
                    CenterLines[i].Last().DistanceTo(_seg[0], _seg[1]);
                    _nls.Add(pPos.DistanceTo_Projection);

                    CenterLines[i] = _nls.ToArray();
                }
                //ACD.WR("P6");
                //Nếu trùng nhau thì chọn đường dài nhất
                if (CenterLines.Count >= 2)
                {
                    PosCollection remove_items = new PosCollection();
                    PosCollection __pls = CenterLines.OrderBy(_r => -_r.Length(false)).ToCollectionSameClosed(false);

                    if (__pls[1][0].DistanceToPts(__pls[0], false) < 1000
                        || __pls[1].Last().DistanceToPts(__pls[0], false) < 1000)
                        remove_items.Add(__pls[1]);

                    CenterLines = __pls.Where(_r => !remove_items.Contains(_r)).ToCollectionSameClosed(false);
                }
            }
        }

        public pPos Size
        {
            get
            {
                return pPos.FromString(CSVDataCLS._getRegionSize(this).Content.Replace("x", ","));
            }
        }

        public bool IsStraight
        {
            get
            {
                pPos sz = Size;
                return sz.Y / sz.X >= 2.5;
            }
        }

        public pPos[] GenerateElements(double offset, double range)
        {
            offset *= cReadData.__sc;
            List<pPos> res = new List<pPos>();

            if(CenterLines.Count > 0)
            {
                pPos[] ct_line = CenterLines[0];
                pPos[] points = SpacingTool.RangePoints(ct_line, range * cReadData.__sc, true);
                
                for(int i = 1; i< points.Length - 1; i ++)
                {
                    pPos _p = points[i];
                    _p.DistanceToPts(ct_line);

                    if (pPos.DistanceTo_Index >= 0 && pPos.DistanceTo_Index < ct_line.Length)
                    {
                        pPos vt = ct_line[pPos.DistanceTo_Index]
                                - ct_line[pPos.DistanceTo_Index == 0 ? ct_line.Length - 1 : pPos.DistanceTo_Index - 1];

                        vt = new pPos(-vt.Y, vt.X).Normalize;
                        res.AddRange(this.Intersect(_p, _p + vt * 1000).Select(__np => __np.Along(-offset, _p)));
                    }
                }
            }

            return res.ToArray();
        }
    }
}
