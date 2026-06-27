using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AcadScript
{
    public class gRegionElement:List<pPos>
    {
        public pPos MtlPoint;
        public string MtlName, Note = "";
        public pPos AxisPoint;
        public int[] ConcaveCorners;
        double _region_area = 0;

        public gRegionElement(IEnumerable<pPos> _pts)
        {
            MtlPoint = _pts.ElementAt(0).Clone();

            this.Clear();
            this.AddRange(_pts);
            this.Add(this.First());

            _region_area = this.Area();

            ConcaveCorners = DE.NumericArray(0, this.Count - 1).Where(i => _isTurnRoad(i)).ToArray();
        }

        public bool ___InsidePts(pPos p)
        {
            bool res = false;
            
            pPos[] bb = this.Boundary();
            //ACD.WR("PTS {0} BB1 {1} BB2 {2}", pts.Count(), bb[0], bb[1]);
            if (p.InsideRect(bb[0], bb[1]))
            {
                List<pPos> ls = this.ToList();// : pts.Offset(offset).ToList();

                if (ls.First()._isVeryClosed(ls.Last()))
                    ls.RemoveAt(ls.Count - 1);

                int n = ls.Count;
                double angle = 0;

                for (int i = 0; i < n; i++)
                    angle += DE.Angle(ls[i].X - p.X, ls[i].Y - p.Y,
                        ls[(i + 1) % n].X - p.X, ls[(i + 1) % n].Y - p.Y);

                res = Math.Abs(angle) >= Math.PI;
            }

            return res;
        }

        public pPos _divHalfVector(int i)
        {
            int prevIndex = i == 0 ? this.Count - 1 : i - 1;
            int nextIndex = i == this.Count - 1 ? 0 : i + 1;

            pPos prevPoint = this[prevIndex];
            pPos currentPoint = this[i];
            pPos nextPoint = this[nextIndex];

            pPos vector1 = currentPoint - prevPoint;
            pPos vector2 = nextPoint - currentPoint;

            // Chuẩn hóa vector
            float length1 = (float)Math.Sqrt(vector1.X * vector1.X + vector1.Y * vector1.Y);
            float length2 = (float)Math.Sqrt(vector2.X * vector2.X + vector2.Y * vector2.Y);

            vector1.X /= length1;
            vector1.Y /= length1;
            vector2.X /= length2;
            vector2.Y /= length2;

            // Tính vector bisector
            pPos bisector = new pPos(- vector1.Y - vector2.Y, vector1.X + vector2.X);

            return bisector / (float)Math.Sqrt(bisector.X * bisector.X + bisector.Y * bisector.Y);
        }

        double __sin(pPos pO, pPos pA, pPos pB)
        {
            double vector1X = pA.X - pO.X;
            double vector1Y = pA.Y - pO.Y;
            double vector2X = pB.X - pO.X;
            double vector2Y = pB.Y - pO.Y;

            // Tính độ dài của hai vectơ
            double length1 = Math.Sqrt(vector1X * vector1X + vector1Y * vector1Y);
            double length2 = Math.Sqrt(vector2X * vector2X + vector2Y * vector2Y);

            // Tính tích vô hướng của hai vectơ
            double dotProduct = vector1X * vector2X + vector1Y * vector2Y;
            double cosTheta = dotProduct / (length1 * length2);

            // Tính sin của góc chia đôi
            double sinHalfTheta = Math.Sqrt(1 - cosTheta * cosTheta);
            // Tính sin của góc
            return sinHalfTheta;
        }

        public pPos prevPoint(int i) { return this[i == 0 ? this.Count - 1 : i - 1]; }
        public pPos nextPoint(int i) { return this[i == this.Count - 1 ? 0 : i + 1]; }

        public pPos[] ArcCornerFilletPoints(int i, double ext = 0) //center-point-point
        {
            pPos[] res = null;

            pPos vt = _divHalfVector(i);

            double __nsin = __sin(this[i], this[i] + 1000 * vt, prevPoint(i));
            bool _is_big_angle = Math.Asin(__nsin) > 1.5 * Math.PI / 4;

            if (!_is_big_angle)
            {
                double _m = Math.Abs(corner_value / __nsin);

                // Tính tâm của vòng tròn fillet
                pPos filletCenter = this[i] + _m * vt;

                filletCenter.DistanceTo(this[i], prevPoint(i));
                pPos p1 = pPos.DistanceTo_Projection;
                if (ext != 0)
                    p1 = p1.Along(ext, prevPoint(i));

                filletCenter.DistanceTo(this[i], nextPoint(i));
                pPos p2 = pPos.DistanceTo_Projection;
                if (ext != 0)
                    p2 = p2.Along(ext, nextPoint(i));

                res = new pPos[] { filletCenter, p1, p2 };
            }

            return res;
        }

        public pPos[] _fillet(int i)
        {
            List<pPos> res = null;
            pPos[] triples = ArcCornerFilletPoints(i); //center-point-point

            if (triples != null)
            {
                PosCollection _tmps = new PosCollection();

                double endAngle = Math.Atan2(triples[1].Y - triples[0].Y, triples[1].X - triples[0].X);
                double startAngle = Math.Atan2(triples[2].Y - triples[0].Y, triples[2].X - triples[0].X);
                
                // Tính số lượng điểm trên cung tròn
                for (int _t = 0; _t <= 1; _t++)
                {
                    if (_t == 1)
                    {
                        double v = startAngle;
                        startAngle = endAngle;
                        endAngle = v + Math.PI * 2;
                    }

                    int numPoints = (int)(Math.Abs(endAngle - startAngle) / (Math.PI / 180));

                    List<pPos> pts = new List<pPos>() { this[i] };
                    // Tính các điểm trên cung tròn fillet và thêm vào danh sách
                    for (int j = 0; j <= numPoints; j++)
                    {
                        double angle = startAngle + j * (endAngle - startAngle) / numPoints;
                        pts.Add(triples[0] + corner_value * new pPos(Math.Cos(angle), Math.Sin(angle)));
                    }

                    _tmps.Add(pts.ToArray());
                }

                res = _tmps.OrderBy(_ls => _ls.Area()).First().ToList();
                res.Add(this[i]);
            }

            return res != null ? res.ToArray() : null;
        }

        public double corner_value = 5000;

        public PosCollection FilletPoints
        {
            get
            {
                return ConcaveCorners.Select(i => _fillet(i))
                    .Where(_ls => _ls != null)
                    .ToCollectionSameClosed(true);
            }
        }

        public PosCollection TempLines = new PosCollection();

        public PosCollection BivectorPoints
        {
            get
            {
                return ConcaveCorners.Where(i => _fillet(i) != null)
                    .Select(i => new pPos[] { this[i], this[i] + _divHalfVector(i) * 20000 }).ToCollectionSameClosed(false);
            }
        }

        public pPos[] ShowPoints
        {
            get
            {
                if (this is gRoadElement && !(this is gRoadChild))
                {
                    PosCollection pls = new PosCollection();
                    pls.Add(this.ToArray());
                    pls.AddRange(FilletPoints);

                    pls.Closed = pls.Select(_ => true).ToArray();

                    //ACD.WR("Areas: {0}, {1}",pls.Count, pls.UnionPolygons.Count);
                    //return pls.UnionPolygons[0];
                    GraphConsole.Compute(pls);
                    return GraphConsole.ResultBorders[0].Add(GraphConsole.ResultBorders[0][0]);
                }
                else
                    return this.ToArray();
            }
        }


        public double Rotation
        {
            get
            {
                return MtlPoint.Rotation;
            }
        }

        public bool _isTurnRoad(int i)
        {
            return DE.NumericArray(0, this.Count - 1).Where(_n => _n != i)
                        .Select(_n => this[_n]).ToArray().Area() - _region_area > 1000000;
        }

        public void __detectRoad()
        {
            
        }

        public bool IsRoad
        {
            get
            {
                bool res = false;

                if (this.Area() > 500000000) //CHECK_ROAD
                {
                    if (DetectConcave.Length > 0 || ConcaveCorners.Length > 0)
                        res = true;
                    else
                    {
                        pPos __z = CSVDataCLS._getRegionSize(this);
                        string[] __ar = __z.Content.filter("x");
                        double __x = __ar[0].ToNumber();
                        double __y = __ar[1].ToNumber();

                        if (__x > 20 * cReadData.__sc && __y >= 2.9 * cReadData.__sc && __y <= 20 * cReadData.__sc)
                            res = true;
                    }
                }

                return res;
            }
        }

        public int[] DetectConcave
        {
            get
            {
                int[] hullIndices = ConvexHull(this);
                return  DE.NumericArray(0,this.Count - 1).Where(i => Array.Exists(hullIndices, element => element != i)).ToArray();
            }
        }

        int[] ConvexHull(IEnumerable<pPos> points)
        {
            // Tính toán Convex Hull bằng thuật toán Jarvis March (Gift Wrapping)
            List<int> hull = new List<int>();

            int n = points.Count();

            if (n < 3)
                throw new ArgumentException("At least 3 points are required");

            int leftmost = 0;

            for (int i = 1; i < n; i++)
                if (points.ElementAt(i).X < points.ElementAt(leftmost).X)
                    leftmost = i;

            int current = leftmost;
            int next;

            do
            {
                hull.Add(current);
                next = (current + 1) % n;

                for (int i = 0; i < n; i++)
                    if (Orientation(points.ElementAt(current), points.ElementAt(i), points.ElementAt(next)) == 2)
                        next = i;

                current = next;
            }
            while (current != leftmost);

            return hull.ToArray();
        }

        int Orientation(pPos p, pPos q, pPos r)
        {
            double val = (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);
            return val == 0 ? 0 : (val > 0 ? 1 : 2); // Clockwise or Counterclockwise
        }

        public void SetAxisPoint(IEnumerable<gRoadElement> _roadList)
        {
            foreach (var itm in _roadList)
            {
                pPos pt = MtlPoint;
                Dictionary<pPos, double> _tdicts = new Dictionary<pPos, double>();

                foreach (pPos[] __sg in itm.GetSegment(true))
                    if (__sg.Length(false) >= 3000 && __sg.All(__p => this.Contains(__p)))
                    {
                        double d = pt.DistanceToPts(__sg, false);
                        if (pPos.DistanceTo_Projection.IsBetween(__sg[0], __sg[1]))
                            _tdicts.Add(pPos.DistanceTo_Projection, d);
                    }

                if (_tdicts.Count > 0)
                {
                    pPos nearest_point = _tdicts.OrderBy(__itm => -__itm.Value).First().Key;

                    pPos[] line = this.MaxSegment().OrderBy(p => p.X).ThenBy(p => p.Y).ToArray();
                    pPos vt = line[0] - line[1];
                    var A = line[0];
                    var B = line[1];

                    double dx = B[0] - A[0];
                    double dy = B[1] - A[1];

                    var n_C = new pPos(MtlPoint[0] + dx, MtlPoint[1] + dy);
                    pPos[] pts = this.Intersect(MtlPoint, n_C).OrderBy(_p => _p.DistanceTo(nearest_point)).ToArray();

                    AxisPoint = pts.Length > 0 ? pts.First() : (A + B) / 2;

                    MtlPoint.Rotation = (AxisPoint - MtlPoint).AngleVsBaseVector(new pPos(1, 0)).roundNumber(0.01);
                    Note = "_" + Rotation;
                }
            }
        }
    }

    public class cBuilder3D: List<gRegionElement>
    {
        //public PosCollection mtlList; // Lối băng qua đường, tim đường
        
        public gRoadElement[] RoadList
        {
            get
            {
                return this.Where(_itm => _itm is gRoadElement).Select(_itm => (gRoadElement)_itm).ToArray();
            }
        }
                
        public cBuilder3D(PosCollection pls, bool _read_road = false) //, PosCollection __mtls, PosCollection __walks)
        {
            //mtlList = __mtls;
            //walkList = __walks;

            GraphConsole.Compute(pls);

            //this = new List<gRegionElement>();
            this.AddRange(GraphConsole.ResultPts.Select(_r => new gRegionElement(_r)));

            //ACD.WR("OWF1");

            cReadData._generate_title_from_linears(this);

            //ACD.WR("OWF2");

            foreach (var itm in this)
                foreach (pPos[] __ls in cReadData.inblock_mtl_list)
                    if (__ls[0].Content.st_("MT_"))
                        foreach (pPos __p in __ls)
                            if (__p.Inside(itm))
                            {
                                itm.MtlName = __ls[0].Content;
                                break;
                            }


            //ACD.WR("FWR3");

            if (_read_road)
            {
                for (int i = 0; i < this.Count; i++)
                    if (this[i].MtlName.ct_("_ROAD") || (this[i].MtlName.et_() && this[i].IsRoad))
                        this[i] = new gRoadElement(this[i]);

                //walkList.AddRange(gRoadElement.GetWalkList(RoadList));

                foreach (var _reg in this)
                    _reg.SetAxisPoint(RoadList);
            }

            //ACD.WR("FWR4");

            foreach (var itm in this)
                if (itm.MtlName.et_())
                    itm.MtlName = "ZERO";

            //ACD.WR("FWR5");
        }

        public PosCollection AllWalkListBars
        {
            get
            {
                PosCollection res = new PosCollection();

                foreach (var itm in this)
                    if(itm is gRoadElement)
                        foreach (pPos[] _ls in ((gRoadElement)itm).WalkList)
                        {
                            pPos[] _pts = SpacingTool.RangePoints(_ls, 0.9 * cReadData.__sc, true, true);
                            double v = 2;

                            for (int i = 1; i < _pts.Length - 1; i++) //Cắt bỏ đầu và cuối
                            {
                                var p = _pts[i];

                                res.Add(new pPos(p.X - 0.3 * cReadData.__sc, p.Y - v / 2 * cReadData.__sc)
                                    .Rect(0.6 * cReadData.__sc, v * cReadData.__sc)
                                    .Rotate(- p.Rotation, p));
                            }
                        }
                
                return res;
            }
        }
    }

}
