using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AcadScript
{
    //using SIndex = KeyValuePair<int, int>;

    public enum EN_POLYGON_JOIN
    {
        EN_UNION = 0,
        EN_INTERSECTION = 1,
        EN_XOR = 2,
        EN_DIFFERENCE = 3
    }

    
    public class OutterPolygonCls
    {
        public PosCollection Outter, Inner, pls;

        public OutterPolygonCls()
        {
            pls = new PosCollection();
        }

        public void AddPolygon(PosCollection _pls)
        {
            pls.AddRange(_pls);
        }

        public void Compute()
        { 
            pls = pls.OrderBy(ls => -ls.Area()).ToCollectionSameClosed();
            Outter = new PosCollection();
            Inner = new PosCollection();

            for(int i = 0; i < pls.Count - 1; i ++)
                if(!Inner.Contains(pls[i]))
                    for (int j = i + 1; j < pls.Count; j++)
                        if (!Inner.Contains(pls[j]) && pls[j].Inside(pls[i]))
                            Inner.Add(pls[j]);

            foreach (pPos[] ls in pls)
                if (!Inner.Contains(ls))
                    Outter.Add(ls);
        }
    }

    public class pPosExtrude
    {
        public List<int[]> FaceList,SectionList;
        public List<pPos> VertList;
        public PosCollection Pts;
        public pPos[] SectLine;
        int start_index = 1;
        bool isLeft;
        public pPosExtrude(PosCollection pls, double height, bool _isLeft, pPos[] sect = null)
        {
            isLeft = _isLeft;
            SectLine = sect;
            
            Pts = pls;

            if (Pts.Count > 0)
            {
                VertList = new List<pPos>();
                FaceList = new List<int[]>();
                SectionList = new List<int[]>();

                start_index = 1;
                
                foreach (pPos[] ls in Pts)
                    ExtrudeSubList(ls, height);
            }
        }
        
        void ExtrudeSubList(IEnumerable<pPos> _pls, double height)
        {
            VertList.AddRange(_pls);
            VertList.AddRange(_pls.Select(p => p + new pPos(0, 0, height)));

            int total = _pls.Count();

            FaceList.Add(DE.NumericArray(start_index, total + start_index - 1));
            FaceList.Add(DE.NumericArray(total + start_index, total * 2 + start_index - 1));
            
            //int[] sect_indexes = null;

            for (int i = 0; i < total; i++)
            {
                int nex = (i + 1) % total;
                int[] ls = new int[] { start_index + i, start_index + nex,
                        start_index  + total + nex, start_index + total + i};

                if (SectLine != null && _pls.ElementAt(i).DistanceTo(SectLine[0], SectLine[1]) < 1
                    && _pls.ElementAt(nex).DistanceTo(SectLine[0], SectLine[1]) < 1)
                    SectionList.Add(ls);
                else
                    FaceList.Add(ls);
            }
            
            start_index += total * 2;
        }
    }

    public class PosIndexesResult
    {
        public pPos[] Verts;
        public List<int[]> Indexes;
    }

    public class PosCollection: Collection<pPos[]>
    {
        public PosIndexesResult PosIndexes(int round)
        {
            PosCollection pls = this.Select(__ls
               => __ls.Select(__p => __p.Round(round)).ToArray()).ToCollectionClosedList(this.Closed);

            string[] __txts = pls.AllPoints.Select(__p => __p.Round(round).ToString()).Distinct().ToArray();

            PosIndexesResult res = new PosIndexesResult();

            res.Verts = __txts.Select(__s => pPos.FromString(__s)).ToArray();
            res.Indexes = pls.Select(__ls => __ls.Select(__p
                => Array.IndexOf(__txts, __p.Round(round).ToString())).ToArray()).ToList();

            return res;
        }

        int ROUND = 50;

        pPos[] drawLineByPixel(int x, int y, int x2, int y2)
        {
            List<pPos> res = new List<pPos>();
            int w = x2 - x;
            int h = y2 - y;
            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;

            int RND = ROUND;

            //if (Math.Abs(x - x2) > ROUND && Math.Abs(y - y2) > ROUND)
            //    RND = 1;

            if (w < 0) dx1 = -RND; else if (w > 0) dx1 = RND;
            if (h < 0) dy1 = -RND; else if (h > 0) dy1 = RND;
            if (w < 0) dx2 = -RND; else if (w > 0) dx2 = RND;

            int longest = Math.Abs(w);
            int shortest = Math.Abs(h);

            if (!(longest > shortest))
            {
                longest = Math.Abs(h);
                shortest = Math.Abs(w);
                if (h < 0) dy2 = -RND; else if (h > 0) dy2 = RND;
                dx2 = 0;
            }

            int numerator = longest >> 1;

            for (int i = 0; i <= longest; i += RND)
            {
                res.Add(new pPos(x, y).Round(RND));

                numerator += shortest;
                if (!(numerator < longest))
                {
                    numerator -= longest;
                    x += dx1;
                    y += dy1;
                }
                else
                {
                    x += dx2;
                    y += dy2;
                }
            }

            return res.ToArray();
        }

        double _getAngle(IEnumerable<object[]> lls, int i)
        {
            pPos[] ln1 = lls.ElementAt(i).Select(o => (pPos)o).ToArray();
            return (ln1[0] - ln1[1]).Angle() / 180 * Math.PI;
        }

        bool adjIsSameAngle(IEnumerable<object[]> lls, int i, int j)
        {
            return Math.Abs(Math.Abs(Math.Sin(_getAngle(lls, i))) - Math.Abs(Math.Sin(_getAngle(lls, j)))) < 0.01;
        }

        bool adjIsIntersect(IEnumerable<object[]> lls, int i, int j)
        {
            return lls.ElementAt(i).Intersect(lls.ElementAt(j)).Count() > 1;
        }

        PosCollection _segmentFromStrings(List<string[]> _lls, int[] ints)
        {
            PosCollection pls = _lls.Select(ls => ls.Select(s
                => pPos.FromString(s)).ToArray()).ToCollectionSameClosed(false);
            List<string[]> lls = ints.Select(n => _lls[n]).ToList();

            PosCollection selpls = ints.Select(n => pls[n]).ToCollectionSameClosed(false);
            pPos[] bb = selpls.Boundary;

            int axis = bb.Size().X < 1 ? 1 : 0;
            int nex = (axis + 1) % 2;

            List<int> not_values = new List<int>();

            string res = "";

            for (int i = (int)bb[0][axis]; i <= bb[1][axis]; i += ROUND)
                if (selpls.All(ls => ls.Boundary()[0][axis] > i || i > ls.Boundary()[1][axis]))
                    res += "|";
                else
                    res += i + ",";

            PosCollection result = new PosCollection();

            foreach (string s in res.filter("|"))
            {
                string[] ar = s.filter(",");

                pPos p1 = new pPos(0, 0);
                p1[axis] = ar.First().ToNumber();
                p1[nex] = bb[0][nex];

                pPos p2 = new pPos(0, 0);
                p2[axis] = ar.Last().ToNumber();
                p2[nex] = bb[0][nex];

                result.Add(new pPos[] { p1, p2 });
            }

            return result;
        }

        PosCollection _rotateList(PosCollection pls, double angle, bool need_axis)
        {
            PosCollection res = pls.Select(ls => ls.Rotate(angle, new pPos(0, 0)))
                                    .ToCollectionSameClosed(false);

            res.Name = "rotate";

            pPos sz = res[0].Size();
            if (need_axis && sz.X > ROUND && sz.Y > ROUND)
            {
                res = pls.Select(ls => ls.Rotate(-angle, new pPos(0, 0)))
                                    .ToCollectionSameClosed(false);
                res.Name = "rotate_inverse";
            }

            return res;
        }

        public PosCollection StraightenByRound(int _round)
        {
            ROUND = _round;
            pPos basept = this.CenterPoint;
            //string[] colors = new string[] { "", "Red", "Yellow", "Green", "Cyan", "Blue", "Magenta", "White", "Gray" };

            PosCollection res = new PosCollection();
            PosCollection segments = this.GetSegment().Move(basept.Invert);
            var resadj = DE.DetectAdj(segments, adjIsSameAngle);

            //foreach (int[] ints in resadj)
            //    ACD.WR("Adj Angle {0} , {1}",
            //        Math.Sin( (segments[ints[0]][0] 
            //        - segments[ints[0]][1]).Angle() / 180 * Math.PI), ints.ToTextInt());

            //Same angle list
            foreach (int[] ints in resadj)
            {
                PosCollection subsegs = ints.Select(n
                    => segments[n]).ToArray().ToCollectionSameClosed(false);

                double angle = Math.Abs((subsegs.First()[0] - subsegs.First()[1]).Angle());
                double sin = Math.Abs(Math.Sin(angle / 180 * Math.PI));

                //subsegs = SortByRound(subsegs);

                if (sin >= 0.05 && sin <= 0.95)
                {
                    subsegs = _rotateList(subsegs, -angle, true);

                    //foreach (pPos[] ls in subsegs)
                    //ACD.DB.DrawPolyline(ls, false, "LAYER=A-Anno-Dims");
                }

                List<string[]> lls = subsegs.Select(seg =>
                    drawLineByPixel((int)seg[0].X, (int)seg[0].Y, (int)seg[1].X, (int)seg[1].Y)
                    .Select(p => p.ToString()).ToArray()).ToList();

                resadj = DE.DetectAdj(lls, adjIsIntersect);

                //Intersect list in same angle list
                for (int i = 0; i < resadj.Count; i++)
                {
                    //ACD.WR("Adj {0}: {1}", colors[i], resadj[i].ToTextInt());

                    //foreach (int n in resadj[i])
                    //    _drawIndexCircle(segments[ints[n]].CenterPoint() 
                    //        + basept, clr, ints[n].ToString());

                    PosCollection segs = _segmentFromStrings(lls, resadj[i]);

                    if (subsegs.Name.Contains("rotate"))
                        segs = _rotateList(segs, (subsegs.Name.Contains("inverse") ? -1 : 1) * angle, false);

                    res.AddRange(segs.Move(basept).ToCollectionSameClosed(false));
                }
            }

            return res;
        }

        public PosCollection MergeLines
        {
            get
            {
                PosCollection res = new PosCollection();
                PosCollection pls = this.Select(ls => ls.OrderBy(p => p.X)
                    .ThenBy(p => p.Y).ThenBy(p => p.Z).ToArray()).ToCollectionSameClosed();

                pPos[] pts = pls.AllPoints;

                int[] nums = DE.NumericArray(0, pts.Length - 1).OrderBy(n => pts[n].X)
                    .ThenBy(n => pts[n].Y).ThenBy(n => pts[n].Z).ToArray();

                int a = 0;
                int b = 0;

                while (b < nums.Length - 1)
                {
                    b = Array.IndexOf(nums, nums[a] % 2 == 1 ? nums[a] - 1 : nums[a] + 1);

                    //SU.WriteLine("A= {0}; B = {1}", a, b);

                    for (int i = a + 1; i < b; i++)
                    {
                        int k = Array.IndexOf(nums, nums[i] % 2 == 1 ? nums[i] - 1 : nums[i] + 1);
                        if (k > b) b = k;
                    }

                    res.Add(new pPos[] { pts[nums[a]], pts[nums[b]] });
                    a = b + 1;
                }

                if (res.Count > 0)
                    res.Closed = res.Select(ls => false).ToArray();

                return res;
            }
        }

        public PosCollection CheckDuplicate(int tole = 10)
        {
            for (int i = 0; i < this.Count - 1; i++)
                if (this[i] != null)
                    for (int j = i + 1; j < this.Count; j++)
                        if (this[j] != null && this[i].Length == this[j].Length
                            && this.Closed[i] == this.Closed[j]
                            && this[i].Compare2Pts(this[j]))
                        {
                            this[j] = null;
                        }

            return this.Where(ls => ls != null).ToCollectionSameClosed();
        }

        public void SetPts(IEnumerable<pPos[]> value)
        {
            this.Clear();
            foreach (pPos[] ls in value)
                this.Add(ls);
        }
        
        public bool[] Closed = new bool[0];
        //public List<double> Angles = new List<double>();
        public PosCollection CutExtrude_Sections;
        public string Name;
        
        public string ToInfo
        {
            get
            {
                string st = "";
                pPos[] bb = Boundary;
                pPos[] r = bb.Rect();

                st += "|W=" + bb.Size().X.ToString();
                st += "|H=" + bb.Size().Y.ToString();
                st += "|D=" + bb.Size().Z.ToString();

                st += "|R0X=" + bb[0].X;
                st += "|R0Y=" + bb[0].Y;
                st += "|R0Z=" + bb[0].Z;
                st += "|R1X=" + bb[1].X;
                st += "|R1Y=" + bb[1].Y;
                st += "|R1Z=" + bb[1].Z;

                st += "|AX=" + bb[0].X;
                st += "|AY=" + bb[0].Y;
                st += "|AZ=" + bb[0].Z;
                st += "|BX=" + bb[1].X;
                st += "|BY=" + bb[1].Y;
                st += "|BZ=" + bb[1].Z;

                pPos sz = bb.Size(), ct = bb.CenterPoint();

                st += "|CX=" + ct.X;
                st += "|CY=" + ct.Y;
                st += "|CZ=" + ct.Z;
                st += "|SX=" + sz.X;
                st += "|SY=" + sz.Y;
                st += "|SZ=" + sz.Z;

                return st;
            }
        }

        public pPos[] AllPoints
        {
            get
            {
                List<pPos> res = new List<pPos>();
                foreach (pPos[] ls in this)
                    res.AddRange(ls);
                return res.ToArray();
            }
        }
        
        public Dictionary<pPos[], string> _dimensionContent(string content)
        {
            double r = content._prop("round").ToNumber(50);
            double sp = content._prop("space").ToNumber(1000);
            string txt = content._prop("text");

            string[] overwrites = txt.filter("&");
            //Console.WriteLine("[content]{0}\r\n[overwrites]{1}",content, overwrites.ToText(","));

            Dictionary<pPos[], string> res = new Dictionary<pPos[], string>();

            foreach (pPos[] pts in this)
                for (int j = 0; j < pts.Length - 1; j++)
                {
                    pPos p1 = pts[j], p2 = pts[j + 1];
                    res.Add(new pPos[] { pts[j], pts[j + 1] }, 
                        overwrites.Length > 0 && j < overwrites.Length ? overwrites[j] : null);
                }

            return res;
        }
        
        public PosCollection(string str = null)
        {
            //Angles = new List<double>();
            this.Clear();

            if (!str.empty())
            {
                string[] lines = str.Replace(";;", "\n").filter("\n");

                //Console.WriteLine("<Line>");
                //foreach (string st in lines)
                //    Console.Write(st + ",");
                //Console.WriteLine("</Line>");

                Closed = lines.Select(s => s.First() == '(' && s.Last() == ')').ToArray();

                if (Closed.Length < lines.Length - 1)
                    for (int i = Closed.Length; i < lines.Length; i++)
                        Closed = Closed.Add(false);

                for (int i = 0; i < lines.Length; i++)
                {
                    List<pPos> pls = lines[i].filter(";").Select(s => pPos.FromString(s)).Where(p => !p.IsNull).ToList();
                    

                    if (pls.Count > 0)
                        if (Closed[i])
                        //{
                            pls.Add(pls.First());
                            //Angles.Add(Angles.First());
                        //}
                        else
                            Closed[i] = pls.Last().DistanceTo(pls.First()) < 0.1;

                    if (pls.Count > 0)
                        this.Add(pls.ToArray());
                }

            }
        }

        public pPos[] First
        {
            get
            {
                return this.First();
            }
        }

        public pPos[] Last
        {
            get
            {
                return this.Last();
            }
        }

        public string ToRelation(params pPos[] nodes)
        {
            string st = "";

            foreach (pPos[] ls in this)
                st += ls.ToRelation(nodes) + ";";

            return st;
        }

        public void AddRange(IEnumerable<pPos[]> pls)
        {
            foreach(pPos[] ls in pls)
                this.Add(ls);
        }

        //public void AddRange(PosCollection pls)
        //{
        //    foreach (pPos[] ls in pls)
        //        this.Add(ls);
        //}

        public override string ToString()
        {
            string res = "";

            //List<string> ls = new List<string>();

            //if (Closed == null)
            //    Closed = new bool[] { false };

            if (Closed.Length < this.Count)
                for (int i = Closed.Length; i < this.Count; i++)
                    Closed = Closed.Add(false);

            for (int i = 0; i < this.Count; i++)
                if (this[i].Length > 0)
                {
                    if (this.Closed[i])
                        res += "(";
                    for (int j = 0; j < this[i].Length; j++)
                        res += this[i][j].X + "," + this[i][j].Y + ","
                            + (this[i][j].Z != 0 ? this[i][j].Z.ToString() : "") + ";";
                    
                    res = res.Substring(0, res.Length - 1);
                    
                    if (this.Closed[i])
                        res += ")";
                    
                    res += ";;";
                }
            
            return res;
        }

        public pPos[] Boundary
        {
            get
            {
                List<pPos> res = new List<pPos>();
                foreach (pPos[] ls in this)
                    res.AddRange(ls);
                return res.Boundary();
            }
        }

        public PosCollection[] GroupPts
        {
            get
            {
                List<PosCollection> res = new List<PosCollection>();
                PosCollection pls = this.OrderBy(ls => -ls.Area()).ToCollectionSameClosed();

                bool[] T = new bool[pls.Count];
                for (int i = 0; i < pls.Count; i++)
                    T[i] = false;

                for (int i = 0; i < pls.Count - 1; i++)
                    if (!T[i])
                    {
                        PosCollection tmp_pls = pls[i].ToCollection();
                        T[i] = true;

                        for (int j = i + 1; j < pls.Count; j++)
                            if (!T[j] && pls[j].Inside(pls[i]))
                            {
                                T[j] = true;
                                tmp_pls.Add(pls[j]);
                            }

                        res.Add(tmp_pls.OrderBy(ls => ls.Area()).ToCollectionSameClosed());
                    }
                return res.ToArray();
            }
        }

        public PosCollection Cut(IEnumerable<pPos> sectline, bool isLeft = true)
        {
            PosCollection res = new PosCollection();
            //List<pPos> points = new List<pPos>();
            ////CutExtrude_Sections = new PosCollection();

            //foreach (pPos[] ls in this)
            //    foreach (pPos[] cutt in ls.Cut(sectline, isLeft))
            //        foreach (pPos[] seg in cutt.GetSegment(true))
            //            if (!seg.All(p => p.DistanceTo(sectline) < 1))
            //                res.Add(seg);
            //            else
            //                points.AddRange(seg);

            //points = points.OrderBy(p => p.DistanceTo(sectline.First())).ToList();

            //for (int i = 0; i < points.Count; i += 2)
            //    res.Add(new[] { points[i], points[i + 1] });

            //GraphConsole.Compute(res);
            //res.AddRange(GraphConsole.ResultPts);

            //CutExtrude_Sections = new PosCollection();

            //foreach (pPos[] ls in res)
            //    for (int i = 0; i < ls.Length; i++)
            //    {
            //        int nex = (i + 1) % ls.Length;

            //        if (ls[i].DistanceTo(sectline.ElementAt(0), sectline.ElementAt(1)) < 1
            //            && ls[nex].DistanceTo(sectline.ElementAt(0), sectline.ElementAt(1)) < 1)
            //            CutExtrude_Sections.Add(new pPos[] { ls[i], ls[nex] });
            //    }

            return res;
        }

        public PosCollection MoveZByScale(int axis, double scale)
        {
            return (this.Select(ls => ls.Move(axis < 2 ? new pPos(0, ls.Boundary()[1].Y * (scale - 1))
                : new pPos(0, ls.Boundary()[1].Z * (scale - 1))))).ToCollectionSameClosed();
        }

        public PosCollection LayerPolygons
        {
            get
            {
                PosCollection union = First.ToCollection();
                PosCollection res = First.ToCollection();

                for (int i = 1; i < this.Count; i++)
                {
                    //res.AddRange(this[i].ToCollection().MergePolygons(union, ClipType.ctDifference));
                    //union = this[i].ToCollection().MergePolygons(union);
                }

                return res;
            }
        }

        public PosCollection ByAxis(int view_axis, double move_y_in_view = 0)
        {
            pPos mv = new pPos(0, move_y_in_view * view_axis);

            PosCollection res = this.Select(ls => ls.Select(p => p.ByAxis(view_axis)).Move(mv)).ToCollectionSameClosed();
            res.Closed = this.Closed;
            return res;
        }

        public PosCollection FromParams(string pram) //ModifyPts
        {
            PosCollection res = this;
            pPos move_pt = new pPos(0, 0);

            string key = pram._firstPropName();
            string val = pram._firstProp();

            bool[] closed = res.Select( ls => true).ToArray();

            if (!pram._prop("VERTS").empty())
            {
                res = new PosCollection(pram._prop("VERTS"));
                closed = res.Closed;
            }

            if (!pram._prop("MOVE").empty())
                res = res.Select(ls => ls.Move(pPos.FromString(pram._prop("MOVE")))).ToCollectionSameClosed();

            if (!pram._prop("MIRR").empty()) //format: p1x,p1y,p1z;p2x,p2y,p2z(yes/no)
            {
                string st = pram._prop("MIRR");
                PosCollection pls = new PosCollection(st.filter("()").First());

                if (pls.Count > 0 && pls.First().Length > 1)
                {
                    //Console.WriteLine("Line {0};{1}", pls.First().First(), pls.First().Last());
                    PosCollection new_res = res.Select(ls => ls.Mirror(pls.First().First(), pls.First().Last())).ToCollectionSameClosed();
                    if (st._getInComma().ToBool())
                        res = new_res;
                    else
                        res.AddRange(new_res);
                }
            }

            if (!pram._prop("OFFSET").empty())
            {
                res = res.Select(ls => ls.Offset(pram._prop("OFFSET").ToNumber())).ToCollectionSameClosed();
                closed = res.Select(ls => true).ToArray();
            }

            if (!pram._prop("PARALLEL").empty())
            {
                pPos[] bb = this.Boundary;
                res = new pPos(bb[0].X, bb[1].Y).Parallel(bb[1], 
                    pram._prop("PARALLEL").ToNumber()).ToCollection();
                closed = res.Select(ls => false).ToArray();
            }

            if (!pram._prop("LOFFSET").empty())
            {
                res = res.Select(ls => ls.LOffset(pram._prop("LOFFSET").ToNumber())).ToCollectionSameClosed();
                closed = res.Select(ls => true).ToArray();
            }

            res.Closed = closed;
            
            return res;
        }

        public pPos[] AllKnots
        {
            get
            {
                List<pPos> res = new List<pPos>();
                List<pPos> mids = new List<pPos>();

                foreach (pPos[] ls in this)
                {
                    res.AddRange(ls);

                    mids.Add(ls.CenterPoint());
                    PosCollection segs = ls.GetSegment(true);
                    //Console.WriteLine("Segs {0}", segs.Count);
                    foreach (pPos[] seg in segs)
                        mids.Add(seg.CenterPoint());
                }

                res.AddRange(mids);
                return res.ToArray();
            }
        }
        
        public List<double[]> ExtractPtsXY(double round, double minvalue)
        {
            List<pPos> pts = new List<pPos>();

            foreach (pPos[] ls in this)
                pts.AddRange(ls);

            return pts.ExtractPtsXY(round, minvalue);
        }

        public PosCollection UnionPolygons
        {
            get
            {
                PosCollection res = First.ToCollection();

                //for (int i = 1; i < this.Count; i++)
                //    res = this[i].ToCollection().MergePolygons(res);

                return res;
            }
        }

        public PosCollection Trim(pPos p1, pPos p2)
        {
            List<pPos> newls = new List<pPos>();

            foreach (pPos[] ls in this)
                newls.AddRange(ls.Offset(-1).Intersect(p1, p2, true));

            newls = newls.OrderBy(v => v.DistanceTo(p1)).ToList();

            PosCollection res = new PosCollection();

            for (int i = 0; i < newls.Count - 1; i++)
                if (!this.Any(ls => ((newls[i] + newls[i + 1]) / 2).Inside(ls)))
                    res.Add(new pPos[] { newls[i], newls[i + 1] });

            return res;
        }

        public PosCollection Move(pPos mv)
        {
            return this.Select(ls => ls.Move(mv)).ToCollectionClosedList(this.Closed);
        }

        public PosCollection ComputePolygons(PosCollection clip, EN_POLYGON_JOIN jointype)
        {
            PosCollection res = new PosCollection();

            //if (clip.Count > 0 || Count > 0)
            //{
            //    List<List<point2>> subjects = this.Select(ls => ls.Select(p => new point2(p.X, p.Y)).ToList()).ToList();
            //    List<List<point2>> clips = clip.Select(ls => ls.Select(p => new point2(p.X, p.Y)).ToList()).ToList();
            //    List<List<point2>> solution = new List<List<point2>>();

            //    ClipType cliptype = ClipType.ctUnion;

            //    switch (jointype)
            //    {
            //        case EN_POLYGON_JOIN.EN_INTERSECTION:
            //            cliptype = ClipType.ctIntersection;
            //            break;
            //        case EN_POLYGON_JOIN.EN_DIFFERENCE:
            //            cliptype = ClipType.ctDifference;
            //            break;
            //        case EN_POLYGON_JOIN.EN_XOR:
            //            cliptype = ClipType.ctXor;
            //            break;
            //    }

            //    Clipper c = new Clipper();
            //    c.AddPaths(subjects, PolyType.ptSubject, true);
            //    c.AddPaths(clips, PolyType.ptClip, true);
            //    solution.Clear();
            //    bool succeeded = c.Execute(cliptype, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

            //    if (succeeded)
            //        res = solution.Select(ls => ls.Select(p => new pPos(p.X, p.Y)).ToArray()).ToCollection();
            //}

            return res;
        }

        public PosCollection ComputeList(EN_POLYGON_JOIN jointype)
        {
            PosCollection res = new PosCollection();

            if (jointype == EN_POLYGON_JOIN.EN_UNION || jointype == EN_POLYGON_JOIN.EN_XOR)
            {
                res.Add(First);
                for (int i = 1; i < this.Count(); i++)
                    res = res.ComputePolygons(this[i].ToCollection(), jointype);
            }
            else if (jointype == EN_POLYGON_JOIN.EN_DIFFERENCE)
            {
                pPos[] current = First.ToArray();
                bool[] T = this.Select(ls => false).ToArray();

                for (int i = 0; i < this.Count; i++)
                    if (!T[i])
                    {
                        T[i] = true;
                        res.Add(this[i]);

                        for (int j = i + 1; j < this.Count; j++)
                            if (!T[j] && current.IsIntersect(this[j]))
                            {
                                T[j] = true;
                                current = current.ToCollection()
                                    .ComputePolygons(this[j].ToCollection(), EN_POLYGON_JOIN.EN_UNION).First;
                                res.AddRange(current.ToCollection()
                                    .ComputePolygons(this[j].ToCollection(), EN_POLYGON_JOIN.EN_DIFFERENCE));
                            }
                    }
            }

            return res;
        }

        //public PosCollection MergePolygons(PosCollection _clip, ClipType cliptype = ClipType.ctUnion)
        //{
        //    PosCollection res = new PosCollection();

        //    List<List<point2>> subjects = this.Select(ls => ls.Select(p => new point2(p.X, p.Y)).ToList()).ToList();
        //    List<List<point2>> clips = _clip.Select(ls => ls.Select(p => new point2(p.X, p.Y)).ToList()).ToList();

        //    List<List<point2>> solution = new List<List<point2>>();

        //    Clipper c = new Clipper();
        //    c.AddPaths(subjects, PolyType.ptSubject, true);
        //    c.AddPaths(clips, PolyType.ptClip, true);
        //    solution.Clear();
        //    bool succeeded = c.Execute(cliptype, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

        //    if (succeeded)
        //        res = solution.Select(ls => ls.Select(p => new pPos(p.X, p.Y)).ToArray()).ToCollection();

        //    return res;
        //}

        public PosCollection TrackingBorder()
        {
            //PosCollection cvs = pls;
            PosCollection res = new PosCollection();

            int prevCnt = this.Count + 1;

            bool[] T = this.Select(c => false).ToArray();
            int[] indexes = DE.NumericArray(0, T.Length - 1);

            int first_index = indexes.First(n => !T[n]);

            List<pPos> ls = this[first_index].ToList();
            T[first_index] = true;

            while (T.Count(b => !b) > 0 && T.Count(b => !b) < prevCnt)
            {
                prevCnt = T.Count(b => !b);
                //Console.WriteLine("Cnt {0} First {1} LS {2}", prevCnt, first_index, ls.Count);

                for (int i = 0; i < this.Count; i++)
                    if (!T[i])
                    {
                        pPos[] cv = this[i];
                        pPos nextPt = ls.Last();

                        if (cv.First().DistanceTo(nextPt) < 10 || cv.Last().DistanceTo(nextPt) < 10)
                        {
                            T[i] = true;
                            pPos pt = cv.First().DistanceTo(nextPt) < 10 ? cv.Last() : cv.First();

                            if (pt.DistanceTo(ls.First()) < 10)
                            {
                                if (ls.Count > 2)
                                    res.Add(ls.ToArray());

                                if (T.Count(b => !b) > 0)
                                {
                                    first_index = indexes.First(n => !T[n]);
                                    Console.WriteLine("<Start New>");

                                    ls = this[first_index].ToList();
                                    T[first_index] = true;
                                }

                                break;
                            }
                            else
                            {
                                ls.Add(pt);
                            }
                        }
                    }
            }

            //Console.WriteLine("Final {0} First {1} LS {2}", res.Count, first_index, ls.Count);
            if (ls.Count > 2)
                res.Add(ls.ToArray());

            return res.OrderBy(l => -l.Area()).ToCollectionSameClosed();
        }
        
        //public PosCollection WeldSplines
        //{
        //    get
        //    {
        //        PosCollection res = new PosCollection();
        //        LinkedList<int>[] adj = new LinkedList<int>[this.Count()];
        //        bool[] T = this.Select(ls => false).ToArray();
        //        List<int> history = new List<int>();

        //        for (int index = 0; index < this.Count; index++)
        //            if (!T[index])
        //            {
        //                for (int i = 0; i < this.Count; i++)
        //                    adj[i] = new LinkedList<int>();

        //                for (int i = 0; i < this.Count - 1; i++)
        //                    if (!T[i])
        //                        for (int j = i + 1; j < this.Count; j++)
        //                            if (!T[j] && (this[i][0].DistanceTo(this[j][0]) < DE.Tole
        //                                || this[i][0].DistanceTo(this[j].Last()) < DE.Tole
        //                                || this[i].Last().DistanceTo(this[j].Last()) < DE.Tole
        //                                || this[i].Last().DistanceTo(this[j][0]) < DE.Tole))
        //                            {
        //                                if (!adj[i].Contains(j))
        //                                    adj[i].AddFirst(j);
        //                                if (!adj[j].Contains(i))
        //                                    adj[j].AddFirst(i);
        //                            }

        //                //for (int i = 0; i < adj.Length; i++)
        //                //db.WRArray("List " + i.ToString(), adj[i]);

        //                LinkedList<int> visited = new LinkedList<int>();
        //                visited.AddFirst(index);
        //                SearchGraph sgp = new SearchGraph();
        //                sgp.START = index;
        //                sgp.END = this.Count;
        //                sgp.adj = adj;
        //                sgp.DepthFirstSearch(visited);

        //                history.AddRange(visited);
        //                //db.WRArray("Path {0}", visited);
        //                foreach (int i in visited)
        //                    T[i] = true;

        //                List<pPos> tmp = this[visited.First()].ToList();
        //                for (int i = 1; i < visited.Count; i++)
        //                {
        //                    pPos[] ls = this[visited.ElementAt(i)];

        //                    if (ls.First().DistanceTo(tmp.First()) < DE.Tole)
        //                        tmp.Reverse();
        //                    else if (ls.Last().DistanceTo(tmp.Last()) < DE.Tole)
        //                        ls = ls.Reverse().ToArray();
        //                    else if (ls.Last().DistanceTo(tmp.First()) < DE.Tole)
        //                    {
        //                        tmp.Reverse();
        //                        ls = ls.Reverse().ToArray();
        //                    }

        //                    for (int j = 1; j < ls.Length; j++)
        //                        tmp.Add(ls[j]);
        //                }

        //                res.Add(tmp.ToArray());
        //            }

        //        for (int i = 0; i < this.Count; i++)
        //            if (!history.Contains(i))
        //                res.Add(this[i]);

        //        return res;
        //    }
        //}
        
        public PosCollection VisibleSplines
        {
            get
            {
                PosCollection res = new PosCollection();
                List<List<int>> spline_index = new List<List<int>>();

                for (int i = 0; i < this.Count - 1; i++)
                    for (int i_a = 0; i_a < this[i].Length - 1; i_a++)
                    {
                        int index = spline_index._findIndexes(i, i_a);

                        List<int> ls = index == -1 ? new List<int> { i, i_a } : spline_index[index];
                        pPos[] line1 = this.GetSegmentByShapeIndexAndPointIndex(i, i_a);

                        for (int j = i + 1; j < this.Count(); j++)
                            for (int j_a = 0; j_a < this.ElementAt(j).Length - 1; j_a++)
                                if (ls._findIndex(j, j_a) == -1
                                    && this.GetSegmentByShapeIndexAndPointIndex(j, j_a).All(p => p.IsOnLine(line1[0], line1[1])))
                                    ls.AddRange(new int[] { j, j_a });

                        if (ls.Count > 2 && index == -1)
                            spline_index.Add(ls);
                    }

                string st = "";
                List<int> indexes = new List<int>();

                PosCollection new_segs = new PosCollection();

                foreach (List<int> indx in spline_index)
                {
                    st += "(";
                    foreach (int n in indx)
                        st += n.ToString() + ",";
                    st += ")\r\n";

                    indexes.AddRange(indx);
                    List<pPos> tmp = new List<pPos>();

                    for (int i = 0; i < indx.Count; i += 2)
                    {
                        pPos p = this.ElementAt(indx[i])[indx[i + 1]];

                        //ACD.DB.CreateText(indx[i].ToString() + "," + indx[i + 1].ToString(), p, 200);
                        tmp.AddRange(this.GetSegmentByShapeIndexAndPointIndex(indx[i], indx[i + 1]));
                    }

                    new_segs.AddRange(tmp.VisibleSplines(1));
                }

                //ACD.WR(st);

                res = this.RemoveSegments(indexes);
                res.AddRange(new_segs);

                return res;
            }
        }
        
        public PosCollection GetSegment()
        {
            PosCollection res = new PosCollection();
            for (int i = 0; i < this.Count(); i++)
            {
                for (int j = 0; j < this.ElementAt(i).Length - 1; j++)
                    res.Add(new pPos[] { this.ElementAt(i)[j], this.ElementAt(i)[j + 1] });

                if (this.ElementAt(i).Count() > 0 && Closed[i])
                    res.Add(new pPos[] { this.ElementAt(i).Last(), this.ElementAt(i).First() });
            }
            return res;
        }
        
        public PosCollection AlignAxisWall(double w, int axis)
        {
            pPos[] bb = this.Boundary;
            PosCollection segs = this.GetSegment();

            double max_l = segs.Max(seg => seg[1].DistanceTo(seg[0]));

            int[] indexes = DE.NumericArray(0, segs.Count - 1)
                .Where(n => Math.Abs(Math.Abs(Math.Sin((segs[n][1] - segs[n][0])
                    .Angle().roundNumber(10) / 180 * Math.PI)) - axis) < 0.1
                && segs[n][1].DistanceTo(segs[n][0]) >= max_l / 2)
                .Select(n => n).OrderBy(n => segs[n][0].X - bb[0].X).ToArray();

            PosCollection res = new PosCollection();

            foreach (int i in indexes)
            {
                pPos[] ls = segs[i];

                if (ls[0].X == bb[0].X || ls[0].X - bb[0].X > 2 * w)
                    res.Add(ls[0].Parallel(ls[1], w / 2).CenterPoint().InsidePls(this)
                        ? ls : ls.Reverse());
            }

            return res;
        }

        public PosCollection AlignWall(string wallstyle)
        {
            pPos[] bb = this.Boundary;

            PosCollection wall_regions = new PosCollection();
            List<string> wall_styles = new List<string>();
            List<double> wall_widths = new List<double>();

            bool[] T = this.Select(ls => false).ToArray();

            for (int i = 0; i < this.Count; i++)
                if (!T[i])
                {
                    pPos[] ls = this[i];
                    //List<pPos> res = null;
                    string style = "Wall100";
                    double val = 0;
                    T[i] = true;

                    pPos basePoint = new pPos(bb[0].X, bb[1].Y);

                    basePoint = ls[0].Intersect(ls[1], basePoint,
                        ls[0].IsParrallel(ls[1], basePoint, bb[1]) ? bb[0] : bb[1], false);

                    //db.WR("Basepoint {0}", basePoint);

                    ls = ls.OrderBy(p => p.DistanceTo(basePoint)).ToArray();
                    pPos minp = ls[0], maxp = ls[1];

                    for (int j = i + 1; j < this.Count; j++)
                    {
                        pPos[] new_ls = this[j];
                        double w = ls[0].DistanceTo(new_ls[0], new_ls[1]);

                        if (!T[j] && ls[0].IsParrallel(ls[1], new_ls[1], new_ls[0], 10)
                            && w <= 250 && _anyBetweens(ls, new_ls))
                        {
                            T[j] = true;

                            if (val == 0 && w > 10)
                            {
                                val = w;

                                if (minp.Parallel(maxp, w).First().DistanceTo(new_ls[0], new_ls[1]) > w)
                                {
                                    //db.WR("Change offset");
                                    val = -w;
                                }

                                for (int n = 0; n < 400; n += 100)
                                    if (Math.Abs(w - n) <= 10)
                                    {
                                        style = wallstyle + n;
                                        break;
                                    }

                                pPos[] line = new_ls.ProjectOn(ls[0], ls[1])
                                    .OrderBy(p => p.DistanceTo(basePoint)).ToArray();

                                if (line.First().DistanceTo(basePoint) < minp.DistanceTo(basePoint))
                                    minp = line.First();
                                if (line.Last().DistanceTo(basePoint) > maxp.DistanceTo(basePoint))
                                    maxp = line.Last();
                            }
                        }
                    }

                    if (val == 0)
                        val = 100;

                    List<pPos> tmps = new List<pPos> { minp, maxp };
                    tmps.AddRange(minp.Parallel(maxp, val).Reverse());

                    wall_styles.Add(style);
                    wall_regions.Add(tmps.ToArray());
                    wall_widths.Add(val);
                }

            PosCollection res = new PosCollection();

            for (int i = 0; i < wall_regions.Count; i++)
            {
                if (wall_regions[i].CenterPoint().isLeft(wall_regions[i][0], wall_regions[i][1]))
                    res.Add(new pPos[] { wall_regions[i][0], wall_regions[i][1] });
                else
                    res.Add(new pPos[] { wall_regions[i][1], wall_regions[i][0] });
            }

            AlignWall_Styles = wall_styles.ToArray();
            AlignWall_Widths = wall_widths.ToArray();
            return res;
        }
        
        bool _anyBetweens(pPos[] line1, pPos[] line2)
        {
            return line1.Any(p => p.ProjectLine(line2[0], line2[1]).IsBetween(line2[0], line2[1]))
                || line2.Any(p => p.ProjectLine(line1[0], line1[1]).IsBetween(line1[0], line1[1]));
        }
        
        public PosCollection Stretch(pPos[] region, pPos movept)
        {
            return this.Select(ls => ls.Stretch(region, movept)).ToCollectionSameClosed();
        }

        public string[] AlignWall_Styles;
        public double[] AlignWall_Widths;
        
        public pPos[] Intersect(bool _insegment = true)
        {
            List<pPos> res = new List<pPos>();
            PosCollection lsx = new PosCollection();

            foreach (pPos[] ls in this)
                for (int i = 0; i < ls.Length; i++)
                    lsx.Add(new pPos[] { ls[i], ls[(i + 1) % ls.Length] });

            for (int i = 0; i < lsx.Count - 1; i++)
                for (int j = i + 1; j < lsx.Count; j++)
                {
                    pPos pt = lsx[i][0].Intersect(lsx[i][1], lsx[j][0], lsx[j][1], _insegment);
                    if (pt != null && res.All(p => p.DistanceTo(pt) > 10))
                        res.Add(pt);
                }

            return res.ToArray();
        }

        public pPos[] NearestProjectOn(pPos prj1, pPos prj2, bool _isLeft)
        {
            List<pPos> pts = new List<pPos>();

            foreach (pPos[] ls in this)
                pts.AddRange(ls);

            return pts.NearestProjectOn(prj1, prj2, _isLeft);
        }

        public PosCollection JoinPolygons(PosCollection _clip, int _cliptype = 1)
        {
            PosCollection res = new PosCollection();
            //ClipType cliptype = ClipType.ctUnion;

            //if (_cliptype == 0)
            //    cliptype = ClipType.ctIntersection;
            //else if (_cliptype == 2)
            //    cliptype = ClipType.ctDifference;
            //else if (_cliptype == 3)
            //    cliptype = ClipType.ctXor;

            //OutterPolygonCls cls = new OutterPolygonCls();
            //cls.AddPolygon(this);
            //cls.AddPolygon(_clip);
            //cls.Compute();

            //if (cls.Outter.Count > 1)
            //{
            //    List<List<point2>> subjects = new List<List<point2>> { cls.Outter.First.Select(p => new point2(p.X, p.Y)).ToList() };
            //    List<List<point2>> clips = new List<List<point2>>();

            //    for (int i = 1; i < cls.Outter.Count; i++)
            //        clips.Add(cls.Outter[i].Select(p => new point2(p.X, p.Y)).ToList());

            //    List<List<point2>> solution = new List<List<point2>>();

            //    Clipper c = new Clipper();
            //    c.AddPaths(subjects, PolyType.ptSubject, true);
            //    c.AddPaths(clips, PolyType.ptClip, true);
            //    solution.Clear();
            //    bool succeeded = c.Execute(cliptype, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

            //    if (succeeded)
            //        res = solution.Select(ls => ls.Select(p => new pPos(p.X, p.Y)).ToArray()).ToCollection();
            //}
            //else
            //    res.AddRange(cls.Outter);

            //res.AddRange(cls.Inner);

            return res;
        }

        public pPos Size
        {
            get
            {
                pPos[] bb = Boundary;
                return (bb[1] - bb[0]).Abs;
            }
        }
        
        public pPos CenterPoint
        {
            get
            {
                return AllPoints.CenterPoint();
            }
        }

        public pPos[] SelfIntersect
        {
            get
            {
                List<pPos> res = new List<pPos>();

                for (int i = 0; i < this.Count - 1; i++)
                    for (int j = i + 1; j < this.Count; j++)
                        res.AddRange(this[i].IntersectPts(this[j]));

                return res.ToArray();
            }
        }
        
        public pPos[] Region
        {
            get
            {
                pPos[] res = null;
                GraphConsole.Compute(this);
                if (GraphConsole.ResultPts.Count > 0)
                    res = GraphConsole.ResultPts[0];
                return res;
            }
        }
        
        public pPos[] SliceIntersect(pPos p1, pPos p2)
        {
            List<pPos> res = new List<pPos>();
            foreach (pPos[] ls in this)
            {
                for (int i = 0; i < ls.Length; i++)
                {
                    int nex = (i + 1) % ls.Length;
                    pPos pt = ls[i].Intersect(ls[nex], p1, p2);
                    if (pt != null)
                        res.Add(pt);
                }
            }
            return res.ToArray();
        }

        //public int[] OrderFaceByView(int view)
        //{
        //    List<KeyValuePair<int, pPos[]>> tmps = new List<KeyValuePair<int, pPos[]>>();
        //    for (int i = 0; i < this.Count; i++)
        //        tmps.Add(new KeyValuePair<int, pPos[]>(i, this[i]));

        //    pPos vector = ACD.ViewEyes[view][0];
        //    pPos facept = ACD.ViewEyes[view][1];

        //    tmps = tmps.OrderBy(itm => itm.Value.CenterPoint().PointPlaneDistance(facept, vector)).ToList();
        //    return tmps.Select(itm => itm.Key).ToArray();
        //}
        
        //public pPos[] xline_temp_this, xline_boundary_intersect;

        //public xLine[] ToXline
        //{
        //    get
        //    {
        //        xLine[] res = new xLine[4];

        //        List<pPos> tmp_pls = new List<pPos>();

        //        for (int i = 0; i < this.Count - 1; i++)
        //            for (int j = i + 1; j < this.Count; j++)
        //                tmp_pls.AddRange(this[i].Intersect(this[j]));

        //        xline_temp_this = tmp_pls.ToArray();
        //        xline_boundary_intersect = tmp_pls.Boundary();
        //        pPos[] rect = xline_boundary_intersect.Rect();

        //        for (int i = 0; i < 4; i++)
        //        {
        //            res[i] = new xLine(rect[(i + 1) % 4], rect[i]);
        //            res[i].addPoint(tmp_pls.Cast<pPos>()
        //                .Where(pt => pt.DistanceTo(res[i].StartPoint, res[i].EndPoint) < 2000).Select(pt => pt));
        //        }
                
        //        return res;
        //    }
        //}

        public int FindIndex( Predicate<pPos[]> fn)
        {
            return this.FindIndex(fn);
        }

        //public PosCollection ArrayList(string pram)
        //{
        //    PosCollection res = this.Select(ls => ls).ToCollection();
        //    res.Closed = this.Closed;

        //    if (!pram._prop("ARR").empty())
        //    {
        //        string info = pram._allVariableAndValues();
        //        PosCollection pls = new PosCollection(pram._prop("ARR")._getInComma("[]").ReplaceEquation());

        //        //Console.WriteLine("[AR_COMMA]{0};{1}", pram._prop("ARR")._getInComma(), 
        //        //    pram._prop("ARR")._getInComma().ReplaceEquation(val));

        //        if (pls.Count > 0)
        //        {
        //            pPos[] step_list = pls.First;
        //            pPos to_values = new pPos(pram._prop("ARR")._getBeforeComma("[]").ReplaceEquation());

        //            //Console.WriteLine("ARR:{0} FROM {1} TO:{2}", pram._prop("ARR"), this1, to_values);

        //            pPos[] bb = this.Boundary;
        //            pPos ct = bb.CenterPoint();

        //            //Console.WriteLine("[ARRAY]FROM {0} TO:{1} BY:{2}\r\nPRAM:{2}",
        //            //    bb[0], to_values, step_list.ToText(), pram);

        //            foreach (pPos step in step_list)
        //            {
        //                int[] indexes = DE.NumericArray(0, 2)
        //                    .Select(n => step[n] == 0 ? 1 : ((int)Math.Ceiling((to_values[n] - ct[n]) / step[n]) + 1)).ToArray();

        //                for (int i = 0; i < indexes[0]; i++)
        //                    for (int j = 0; j < indexes[1]; j++)
        //                        for (int k = 0; k < indexes[2]; k++)
        //                            if (i != 0 || j != 0 || k != 0)
        //                                res.AddRange(this.Select(ls => ls.Move(new pPos(i * step.X, j * step.Y, k * step.Z))));
        //            }
        //        }
        //    }
        //    return res;
        //}
    }

    public class pPos:MarshalByRefObject
    {
        public string Content;
        public double Rotation = 0;

        public pPos[] DrawCSection(pPos p2, pPos sz, double ratio = 0.2)
        {
            
            pPos[] ln = this.Parallel(p2, sz.Y);
            pPos p3 = ln[0];
            pPos p4 = ln[1];
            return new pPos[] { p4.AlongRatio(p2, ratio), p4, p3, this, p2, p2.AlongRatio(p4, ratio) };
        }

        public bool _isVeryClosed(pPos p2, double tole = 1)
        {
            return Math.Abs(this.X - p2.X) < tole && Math.Abs(this.Y - p2.Y) < tole;
        }
        
        public pPos Cross(pPos v2)
        {
            double x, y, z;
            x = this.Y * v2.Z - v2.Y * this.Z;
            y = (this.X * v2.Z - v2.X * this.Z) * -1;
            z = this.X * v2.Y - v2.X * this.Y;

            var rtnvector = new pPos(x, y, z);
            return rtnvector.Normalize;
        }

        public pPos FaceNormal(pPos b, pPos c)
        {
            pPos norm = (c - b).Cross(this - b).Normalize;
            if (norm.Y < 0) //or whatever direction up is
                norm = norm.Invert;
            return norm;
        }
        
        public static double Dot(pPos a, pPos b)
        {
            return (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
        }

        public static pPos Projection(pPos a, pPos b)
        {
            return Dot(a, b) * b / Dot(b, b);
        }

        public static pPos Rejection(pPos a, pPos b)
        {
            return a - Projection(a, b);
        }

        public pPos _getScale()
        {
            pPos res = null;
            if (!this.Content.empty())
                foreach (string s in this.Content.Upper().filter(";"))
                    if (s.StartsWith("S"))
                        res = pPos.FromString(s.Replace("S", ""));
            return res;
        }

        public pPos _getRotation()
        {
            pPos res = null;
            if (!this.Content.empty())
                foreach (string s in this.Content.Upper().filter(";"))
                    if (s.StartsWith("R"))
                        res = pPos.FromString(s.Replace("R", ""));
            return res;
        }

        string _addValue(double v)
        {
            string res = "";
            if (v > 0)
                res = "+" + v;
            else if (v < 0)
                res = v.ToString();
            return res;
        }
        
        //public pPos dim_flip_over(pPos dimpoint1, pPos dimpoint2, double dim_flip_range)
        //{
        //    pPos pt = this.Clone();
        //    if (dimpoint1.DistanceTo(dimpoint2) <= dim_flip_range)
        //    {
        //        if (ACD.flip == 0)
        //            ACD.flip = -1;
        //        else if (ACD.flip == -1)
        //            ACD.flip = -2;
        //        //else if (DE.flip == -2)
        //        //    DE.flip = 0;
        //        else
        //            ACD.flip = -1;
        //    }
        //    else
        //        ACD.flip = 0;

        //    string overwrite = (ACD.flip == 1 ? "{+f}" : "") 
        //        + (ACD.flip == -1 ? "{-f}" : "") + (ACD.flip == -2 ? "{ff}" : "");

        //    //if(!overwrite.empty())
        //    //    Console.WriteLine("Overwrite {0} flip {1}", overwrite, dim_flip_range);

        //    double n = dim_flip_range * 0.75;

        //    if (overwrite=="{-f}")
        //        pt = pt.Along(n * 2, pt.Project(dimpoint1, dimpoint2));
        //    else if (overwrite=="{+f}")
        //        pt = pt.Along(-n, pt.Project(dimpoint1, dimpoint2));
        //    else if (overwrite=="{ff}")
        //        pt = pt.Along(-n, pt.Project(dimpoint1, dimpoint2));

        //    pt.Rotation = this.Rotation;
        //    return pt;
        //}


        public string RelativeRect(IEnumerable<pPos> r, bool relative_scale = false)
        {
            int index = DE.NumericArray(0, r.Count() - 1)
                                        .OrderBy(n => DistanceTo(r.ElementAt(n))).First();
            double x = (this - r.ElementAt(index)).X, y = (this - r.ElementAt(index)).Y.roundNumber();
            pPos[] bb = r.Boundary();

            double w = bb.Size().X, h = bb.Size().Y;
            string sx = "", sy = "";

            if (relative_scale)
            {
                sx = ((this - r.ElementAt(index)).X < 0 ? "-" : "+") + "W*"
                    + ((this - r.ElementAt(index)).X / w).roundNumber(0.01);
                sy = ((this - r.ElementAt(index)).Y < 0 ? "-" : "+") + "H*"
                    + ((this - r.ElementAt(index)).Y / h).roundNumber(0.01);
            }
            else
            {
                sx = _addValue((this - r.ElementAt(index)).X.roundNumber());
                sy = _addValue((this - r.ElementAt(index)).Y.roundNumber());
            }

            return "R" + index + "X" + sx + ",R" + index + "Y" + sy;
        }
        
        public pPos[] BreakLine(pPos p2, double extend)
        {
            pPos mid = (this + p2) / 2;
            pPos[] line1 = this.Parallel(p2, extend / 2);
            pPos[] line2 = this.Parallel(p2, -extend / 2);
            
            return new pPos[] {this.Along(-extend,p2), mid.Along(extend/4,this),
            line1.CenterPoint(),line2.CenterPoint(), mid.Along(extend/4, p2), p2.Along(-extend, this) };
        }
        
        public pPos ByAxis(int axis)
        {
            pPos res = new pPos(0, 0);

            switch (axis)
            {
                case 0:
                    res = new pPos(this[1], this[2]);
                    break;
                case 1:
                    res = new pPos(this[0], this[2]);
                    break;
                case 2:
                    res = new pPos(this[0], this[1]);
                    break;
            }

            res.Rotation = Rotation;
            res.Content = Content;

            return res;
        }

        public double[] ToArray
        {
            get
            {
                return new double[] { this.X, this.Y, this.Z };
            }
        }

        public System.Drawing.PointF ToPoint(double scale)
        {
            //get
            {
                return new System.Drawing.PointF((float)(this.X * scale), (float)(this.Y * scale));
            }
        }
        
        public pPos PointPlaneProject(pPos faceA, pPos nABC)
        {
            pPos p = faceA - this;
            double d = System.Numerics.Vector3.Dot(
                new System.Numerics.Vector3((float)p.X, (float)p.Y, (float)p.Z), 
                new System.Numerics.Vector3((float)nABC.X, (float)nABC.Y, (float)nABC.Z));
            return this + (d * nABC);
        }

        public double PointPlaneDistance(pPos faceA, pPos nABC)
        {
            return DistanceTo(PointPlaneProject(faceA, nABC));
        }

        public pPos Mirror(pPos line1, pPos line2)
        {
            pPos prj = this.ProjectLine(line1, line2);
            double d = DistanceTo(prj);
            
            //Console.WriteLine("{0} Mirr ({1}:{2}) = {3} Projection {4}", this, line1, line2, 
            //    this.Along(d * 2, prj ),prj);

            return this.Along(d * 2, prj);
        }

        public double _getOffsetValue(IEnumerable<pPos> region)
        {
            double v = this.DistanceToPts(region);
            return v > 1 ? (this.Inside(region.Offset(-1)) ? -v : v) : 0;
        }

        public pPos Rotate(double theta, pPos o)
        {
            theta = theta * Math.PI / 180;
            return new pPos(Math.Cos(theta) * (X - o.X) - Math.Sin(theta) * (Y - o.Y) + o.X,
                        Math.Sin(theta) * (X - o.X) + Math.Cos(theta) * (Y - o.Y) + o.Y);
        }

        public pPos[] ToRoom(pPos pt2)
        {
            pPos[] rect = new pPos[] { this, pt2 }.Boundary().Rect();
            double dist = rect[0].DistanceTo(rect[1]);

            return new pPos[] { rect[0].Along(dist / 4, rect[1]), rect[0], rect[3], rect[2],rect[1],
                rect[0].Along(3 * dist / 4, rect[1])};
        }

        private int _getn(double v, double sz)
        {
            return (int)(v / Math.Abs(v) * Math.Floor(Math.Abs(v) / sz));
        }

        public pPos NearestPoint(pPos basept, double sizeX, double sizeY)
        {
            return basept + new pPos(sizeX * _getn(this.X - basept.X, sizeX),
                sizeY * _getn(this.Y - basept.Y, sizeY));
        }

        public static int nearestPoint_Index;
        public static double nearestPoint_Distance;

        public pPos NearestPoints(IEnumerable<pPos> pts)
        {
            nearestPoint_Distance = double.PositiveInfinity;
            for (int i = 0; i < pts.Count(); i++)
            {
                pPos pt = pts.ElementAt(i);
                double dist = this.DistanceTo(pt);
                if (nearestPoint_Distance > dist)
                {
                    nearestPoint_Distance = dist;
                    nearestPoint_Index = i;
                }
            }
            return pts.ElementAt(nearestPoint_Index);
        }

        public pPos NearestPointPls(PosCollection pls)
        {
            double nmin = double.PositiveInfinity;
            pPos new_pt = null;
            foreach (pPos[] ls in pls)
            {
                pPos pt = NearestPoints(ls);
                double dist = this.DistanceTo(pt);
                if (nmin > dist)
                {
                    nmin = dist;
                    new_pt = pt;
                }
            }
            return new_pt;
        }

        public pPos Abs
        {
            get
            {
                return new pPos(Math.Abs(this.X), Math.Abs(this.Y), Math.Abs(this.Z));
            }
        }

        public bool IsParrallelWithVector(pPos vector2, double tole = 1)
        {
            return Math.Abs(Math.Sin(this.AngleVsBaseVector(vector2) / 180 * Math.PI)) <= tole;
        }

        public bool IsParrallel(pPos p2, pPos p3, pPos p4, double tole = 1)
        {
            return this.isLeft(p3, p4) == p2.isLeft(p3, p4) &&
                Math.Abs(this.DistanceTo(p3, p4) - p2.DistanceTo(p3, p4)) <= tole;
        }

        public pPos Intersect(pPos p2, pPos p3, pPos p4, bool in_segment = true)
        {
            pPos res = null;
            SegIntersect.CalcIntersection(1, this, p2, p3, p4);
            if (!in_segment || SegIntersect.SegmentIntersect)
                res = SegIntersect.Intersection;
            return res;
        }

        public pPos Clone()
        {
            return new pPos(X, Y, Z);
        }

        public static KeyValuePair<int, double> _getLengthestSplineIndex(PosCollection pls)
        {
            double maxx = 0;
            int maxi = 0;
            for (int i = 0; i < pls.Count; i++)
            {
                double v = pls[i].Length();
                if (maxx < v)
                {
                    maxx = v;
                    maxi = i;
                }
            }

            return new KeyValuePair<int, double>(maxi, maxx);
        }

        public double openedPathParam(IEnumerable<pPos> pls)
        {
            double res = 0;
            this.DistanceToPts(pls);
            pPos pt = DistanceTo_Projection;

            if (pls.ElementAt(0).IsBetween(pt, pls.ElementAt(1)))
                res = -pls.ElementAt(0).DistanceTo(pt);
            else if (pls.ElementAt(pls.Count() - 1).IsBetween(pls.ElementAt(pls.Count() - 2), pt))
                res = pls.Length(false) + pls.ElementAt(pls.Count() - 1).DistanceTo(pt);
            else
            {
                for (int i = 1; i < pls.Count(); i++)
                    if (this.IsBetween(pls.ElementAt(i), pls.ElementAt(i - 1)))
                    {
                        res += pls.ElementAt(i - 1).DistanceTo(pt);
                        break;
                    }
                    else
                        res += pls.ElementAt(i).DistanceTo(pls.ElementAt(i - 1));
            }

            return res / pls.Length(false);
        }

        public bool CompareTo(pPos b, pPos center)
        {
            if (this.X - center.X >= 0 && b.X - center.X < 0)
                return true;
            else if (this.X - center.X < 0 && b.X - center.X >= 0)
                return false;
            if (this.X - center.X == 0 && b.X - center.X == 0)
            {
                if (this.Y - center.Y >= 0 || b.Y - center.Y >= 0)
                    return this.Y > b.Y;
                return b.Y > this.Y;
            }

            // compute the cross product of vectors (center -> this) x (center -> b)
            double det = (this.X - center.X) * (b.Y - center.Y) - (b.X - center.X) * (this.Y - center.Y);
            if (det < 0)
                return true;
            if (det > 0)
                return false;

            // points this and b are on the same line from the center
            // check which point is closer to the center
            double d1 = (this.X - center.X) * (this.X - center.X) + (this.Y - center.Y) * (this.Y - center.Y);
            double d2 = (b.X - center.X) * (b.X - center.X) + (b.Y - center.Y) * (b.Y - center.Y);
            return d1 > d2;
        }

        //public pPos[] Rect(pPos p2)
        //{
        //    return new pPos[] { this, new pPos(p2.X, this.Y), p2, new pPos(this.X, p2.Y) };
        //}

        public pPos[] Rect(double szX, double szY, double offset = 0)
        {
            pPos[] bb = new pPos[] { this, this + new pPos(szX, szY) };
            return bb.Rect(offset);
        }

        public pPos[] RectToPoint(pPos pt2, double offset = 0)
        {
            return new pPos[] { this, pt2 }.Rect(offset);
        }

        public static pPos getPointNearestCentroid(pPos[] pls, pPos centr)
        {
            //pPos centr = Centroid(pls.ToList());

            int index = 0;
            double min = Double.PositiveInfinity;
            for (int i = 0; i < pls.Length; i++)
            {
                double n = pls[i].DistanceTo(centr);
                if (n < min)
                {
                    min = n;
                    index = i;
                }
            }

            //db.WR("MAX = {0} INDEX = {1}", min,index);

            return pls[index];
        }

        public pPos Along(double dist, pPos pB)
        {
            double ratio = dist / this.DistanceTo(pB);
            return AlongRatio(pB, ratio);
        }

        public pPos AlongRatio(pPos pB, double ratio)
        {
            return this + ((pB - this) * ratio);
        }

        public pPos Invert { get { return new pPos(-this.X, -this.Y,-this.Z); } }

        /*public static pPos pPosImport(point2 pt)
        {
            return new pPos(pt.X, pt.Y);
        }*/

        public pPos(double x, double y, double z = 0, string content = null)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            Content = content;
        }

        public static double pPos_Angle;

        public static pPos FromString (string st)
        {
            pPos res = new pPos(0, 0);
            if (!st.empty())
            {
                res.Content = st._getInComma("[]");
                
                string[] ar = st.filter("<[]");

                if (st.Contains("<"))
                {
                    res.Rotation = ar[1].ToNumber();
                    st = ar.First();
                }

                ar = ar.First().filter("[],");
                res.X = res.Y = double.PositiveInfinity;
                res.Z = 0;

                if (ar.Length >= 2)
                {
                    res[0] = ar[0].ToNumber();
                    res[1] = ar[1].ToNumber();

                    if (ar.Length >= 3)
                        res[2] = ar[2].ToNumber();
                }

            }

            return res;
        }

        public bool InsideRect(pPos rect1, pPos rect2)
        {
            return rect1.X <= X + 1 && X <= rect2.X + 1 && rect1.Y <= Y + 1 && Y <= rect2.Y + 1;
        }

        public bool Inside(IEnumerable<pPos> pts)
        {
            bool res = false;
            if (pts != null && pts.Count() > 0)
            {
                if (pts.Count() == 2)
                {
                    res = this.InsideRect(pts.ElementAt(0), pts.ElementAt(1));
                }
                else
                {
                    pPos[] bb = pts.Boundary();
                    //ACD.WR("PTS {0} BB1 {1} BB2 {2}", pts.Count(), bb[0], bb[1]);
                    if (this.InsideRect(bb[0], bb[1]))
                    {
                        if (this.DistanceToPts(pts) < 1)
                            res = true;
                        else
                        {
                            List<pPos> ls = pts.ToList();// : pts.Offset(offset).ToList();
                            if (ls.isClosed())
                                ls.Add(pts.First());

                            int n = ls.Count;
                            double angle = 0;

                            for (int i = 0; i < n; i++)
                                angle += DE.Angle(ls[i].X - this.X, ls[i].Y - this.Y,
                                    ls[(i + 1) % n].X - this.X, ls[(i + 1) % n].Y - this.Y);

                            res = (Math.Abs(angle) >= Math.PI);
                        }
                    }
                }
            }
            return res;
        }

        public bool InsidePls(PosCollection _pls)
        {
            bool res = false;
            if (_pls.Count > 0)
            {
                PosCollection pls = _pls.OrderBy(ls => ls.Area()).ToCollectionSameClosed();
                res = this.Inside(pls.Last);
                if (res && pls.Count > 1)
                    for (int i = 0; i < pls.Count - 1; i++)
                    {
                        res = !this.Inside(pls[i]);
                        if (!res) break;
                    }
            }
            return res;
        }

        public double AngleVector(pPos v1, pPos v2)
        {
            return (v1 - this).AngleVsBaseVector(v2 - this);
        }

        //public Vector3D ToVector()
        //{
        //    return new Vector3D(X, Y, Z);
        //}

        //public double AngleVector3D(pPos v)
        //{
        //    Vector3D n1 = this.ToVector();
        //    n1.Normalize();
        //    Vector3D n2 = v.ToVector();
        //    n2.Normalize();

        //    return Math.Acos(Vector3D.DotProduct(n1, n2));
        //}

        //public pPos GetPlaneNormal(pPos pB, pPos pC)
        //{
        //    Vector3D dir = Vector3D.CrossProduct((pB - this).ToVector(), (pC - this).ToVector());
        //    dir.Normalize();
        //    return new pPos(dir.X,dir.Y,dir.Z);
        //}

        public double AngleVsBaseVector(pPos vector2)
        {
            double sin = this.X * vector2.Y - vector2.X * this.Y;
            double cos = this.X * vector2.X + this.Y * vector2.Y;

            return Math.Atan2(sin, cos) * (180 / Math.PI);// + 360) % 360;
        }

        public pPos Round(int round)
        {
            //get
            {
                pPos res = this.Clone();
                for (int i = 0; i < 3; i++) res[i] = res[i].roundNumber(round);

                return res;
            }
        }

        public double X = 0, Y = 0, Z = 0;
        
        public bool IsNull { get { return Double.IsNaN(X) || Double.IsNaN(Y) || Double.IsNaN(Z)
                    || Double.IsInfinity(X) || Double.IsInfinity(Y) || Double.IsInfinity(Z)
                    || X > Math.Pow(10,10) || Y > Math.Pow(10, 10) || Z > Math.Pow(10, 10); } }
        public bool IsZero { get { return X == 0 && Y == 0 && Z == 0; } }
        
        public double this[int index]
        {
            get
            {
                double res = 0;
                switch (index)
                {
                    case 0: res = this.X; break;
                    case 1: res = this.Y; break;
                    case 2: res = this.Z; break;
                }
                return res;
            }
            set
            {
                switch (index)
                {
                    case 0: this.X = value; break;
                    case 1: this.Y = value; break;
                    case 2: this.Z = value; break;
                }
            }
        }

        public static pPos operator - (pPos p1, pPos p2)
        {
            pPos res = new pPos(0,0);
            for (int i = 0; i < 3; i++) res[i] = p1[i] - p2[i];
            res.Content = "";

            if (!p1.Content.empty())
                res.Content += p1.Content;

            if (!p2.Content.empty())
            {
                if (!res.Content.empty())
                    res.Content += "&";
                res.Content += p2.Content;
            }
            return res;
        }

        public static pPos operator + (pPos p1, pPos p2)
        {
            pPos res = new pPos(0,0);
            for (int i = 0; i < 3; i++) res[i] = p1[i] + p2[i];
            res.Content = "";

            if (!p1.Content.empty())
                res.Content += p1.Content;

            if (!p2.Content.empty())
            {
                if (!res.Content.empty())
                    res.Content += "&";
                res.Content += p2.Content;
            }
            return res;
        }

        public static pPos operator * (pPos pt, double v)
        {
            pPos res = new pPos(0,0);
            for (int i = 0; i < 3; i++) res[i] = pt[i] * v;
            res.Content = pt.Content;
            return res;
        }

        public static pPos operator * (double v, pPos pt)
        {
            pPos res = pt * v;
            res.Content = pt.Content;
            res.Rotation = pt.Rotation;
            return res;
        }

        public static pPos operator / (pPos pt, double v)
        {
            return pt * (1/v);
        }

        public double DistanceToPoint(pPos pt)
        {
            return Math.Sqrt((this.X - pt.X) * (this.X - pt.X) 
                + (this.Y - pt.Y) * (this.Y - pt.Y) + (this.Z - pt.Z) * (this.Z - pt.Z));
        }
        public double DistanceTo(pPos pt)
        {
            return DistanceToPoint(pt);
        }
        public override string ToString()
        {
            string res =  Math.Round(this.X, 2) + "," + Math.Round(this.Y, 2)
                + (Math.Round(this.Z, 2) != 0 ? "," + Math.Round(this.Z, 2) : "");
            if (Rotation != 0)
                res += "<" + Rotation;
            if (!Content.empty())
                res += "[" + Content + "]";
            return res;
        }

        //public string Text
        //{
        //    get
        //    {
        //        string st = String.Format("{0},{1}", Math.Round(this.X, 2), Math.Round(this.Y, 2));
        //        //if (this.Z != 0) st += String.Format(",{0}", Math.Round(this.Z, 2));
        //        return st;
        //    }
        //    set
        //    {
        //        pPos p3d = StringToP3(value);
        //        if (p3d != null) for (int i = 0; i < 2; i++) this[i] = p3d[i];
        //    }
        //}

        public static pPos StringToP3(string value)
        {
            if (value != null)
            {
                List<string> parts = value.Split(',')
                    .Where(p => p.Trim() != String.Empty)
                    .Select(p => p.Trim()).ToList();

                pPos p3d = new pPos(0,0);

                if (parts.Count >= 2)
                    for (int i = 0; i < 2; i++) p3d[i] = Math.Round(Convert.ToDouble(parts[i]));

                if (parts.Count >= 3)
                p3d.Z = Math.Round(parts.Count > 2 ? Convert.ToDouble(parts[2]) : 0,2);
                return p3d;
            }
            return null;
        }

        //------------COMPARE---------------//
        public bool IsEqualTo(pPos pt)
        {
            bool res = true;
            for (int i = 0; i < 3; i++) res &= this[i] == pt[i];
            return res;
        }


        //--------------LIST----------------//
        
        public static pPos[] GetDefaultByAxis(pPos[] ls, int axis)
        {
            List<pPos> res = new List<pPos>();
            foreach (pPos pt in ls)
            {
                int index = res.FindIndex(itm => Math.Abs(itm[axis] - pt[axis]) < 1);
                if (index == -1) res.Add(pt);
            }
            return res.ToArray();
        }

        public pPos Normalize
        {
            get
            {
                return this / this.Length;
            }
        }

        public pPos[] ParallelOffset(pPos p1, double offset)
        {
            List<pPos> __ls = this.Parallel(p1, - offset).ToList();
            __ls.AddRange(this.Parallel(p1, offset).Reverse());

            return __ls.ToArray();
        }

        public pPos[] Parallel(pPos pt, double offset)
        {
            //pPos p1 = this.X < pt.X ? this : (this.X == pt.X ? (this.Y < pt.Y ? this : pt) : pt);
            //pPos p2 = p1 == this ? pt : this;
            pPos vLine = (new pPos(pt.X - this.X, pt.Y - this.Y)).Normalize;
            pPos vPerp = new pPos(-vLine.Y * offset, vLine.X * offset);
            pPos newp1 = this + vPerp;
            pPos newp2 = pt + vPerp;
            return new pPos[] { newp1, newp2 };
        }
        
        public pPos[] Parallel(pPos pt, double offset_in, double offset_out)
        {
            pPos[] line1 = Parallel(pt, offset_in);
            pPos[] line2 = Parallel(pt, offset_out);
            pPos[] rect =  new pPos[] { line1[0], line1[1], line2[1], line2[0] };

            return rect.SortClockwise();
        }

        static pPos _alongAB(pPos pA, pPos pB, double t)
        {
            double dx = pB.X - pA.X;
            double dy = pB.Y - pA.Y;
            double l = Math.Sqrt(dx * dx + dy * dy);
            return new pPos(pA.X + dx/l * t, pA.Y + dy / l * t);
        }

        double _ratioAB(pPos pA, pPos pB)
        {
            double d = pA.DistanceTo(pB);
            double a = this.DistanceTo(pA) / d;
            double b = this.DistanceTo(pB);

            if (pA.IsBetween(this,pB)) a = -a;
            return a;
        }

        public double DistanceTo()
        {
            return DistanceTo(new pPos(0,0));
        }

        public double DistanceTo(pPos line1, pPos line2)
        {
            pPos.DistanceTo_Projection = ProjectLine(line1, line2);
            return DistanceTo(pPos.DistanceTo_Projection);
        }

        public static pPos[] DistanceTo_Pts;
        public static int DistanceTo_Index;
        public static pPos DistanceTo_Projection;

        public double DistanceToPts(IEnumerable<pPos> pts, bool closed = true)
        {
            double min = double.PositiveInfinity;

            if (pts != null)
                if(pts.Count() > 2)
                {
                    pPos.DistanceTo_Projection = null;
                    pPos.DistanceTo_Index = -1;
                    pPos.DistanceTo_Pts = pts.ToArray();
                    int total = closed ? pts.Count() : pts.Count() - 1;

                    for (int i = 0; i < total; i++)
                    {
                        int nex = (i + 1) % pts.Count();
                        pPos prj = this.ProjectLine(pts.ElementAt(i), pts.ElementAt(nex));

                        if (prj.IsBetween(pts.ElementAt(i), pts.ElementAt(nex)))
                        {
                            double n = this.DistanceTo(prj);

                            if (min > n)
                            {
                                min = n;
                                pPos.DistanceTo_Projection = prj;
                                pPos.DistanceTo_Index = i;
                            }
                        }
                    }
                }
                else if(pts.Count() == 2)
                    min = DistanceTo(pts.ElementAt(0), pts.ElementAt(1));

            return min;
        }

        public bool isLeft(pPos a, pPos b)
        {
            return ((b.X - a.X) * (Y - a.Y) - (b.Y - a.Y) * (X - a.X)) > 0;
        }

        public pPos Project()
        {
            return new pPos(-this.Y, this.X);
        }

        public static double DotProduct(pPos A, pPos B)
        {
            return A.X * B.X + A.Y * B.Y + A.Z * B.Z;
        }

        //public static pPos CrossProduct(pPos A, pPos B)
        //{
        //    Vector3D v1 = new Vector3D(A.X, A.Y, A.Z);
        //    Vector3D v2 = new Vector3D(B.X, B.Y, B.Z);
        //    Vector3D res = Vector3D.CrossProduct(v1, v2);
        //    return new pPos(res.X, res.Y, res.Z);
        //}

        public double Length
        {
            get
            {
                return Math.Sqrt(this.X * this.X + this.Y * this.Y + this.Z * this.Z);
            }
        }

        public pPos ProjectLine(pPos line1, pPos line2)
        {
            pPos res = null;
            //if (line1.X == line2.X && line1.Y == line2.Y)
            //{
            //    res= new pPos(line1.X, line1.Y, this.Z);
            //}
            //else if (line1.Y == line2.Y && line1.Z == line2.Z)
            //    res = new pPos(this.X, line1.Y, line1.Z);
            //else if (line1.X == line2.X && line1.Z == line2.Z)
            //    res = new pPos(line1.X, this.Y, line1.Z);
            //else
            //{
                pPos vAB = line2 - line1;
                pPos vAC = this - line1;

                double d = DotProduct(vAB.Normalize, vAC.Normalize);
                res = line1 + (vAB * (d * (vAC.Length / vAB.Length))) ;
            //}
            return res;
        }

        public pPos Project2D(pPos line1, pPos line2)
        {
           
            pPos res = null;
            //ACD.WR("Line {0},{1}", line1, line2);
            if (line2.X != line1.X)
            {
                double m = (double)(line2.Y - line1.Y) / (line2.X - line1.X);
                double b = (double)line1.Y - (m * line1.X);

                double x = (m * this.Y + this.X - m * b) / (m * m + 1);
                double y = (m * m * this.Y + m * this.X + b) / (m * m + 1);

                res = new pPos(x, y);
            }
            else
            {
                res = new pPos(line1.X, Y);
            }

            return res;
        }
        
        public bool IsBetween(pPos p1, pPos p2, double tole = 1)
        {
            double n = p1.DistanceTo(p2);
            return this.DistanceTo(p1) + this.DistanceTo(p2) <= n + tole;
        }

        public bool IsOnLine(pPos pA, pPos pB)
        {
            return DistanceTo(pA, pB) < 1;
        }

        public double Angle()
        {
            return AngleVsBaseVector(new pPos(1, 0));
        }

        public int AngleAxisVsVector(pPos p)
        {
            return Math.Abs((this - p).Angle().roundNumber(90)) == 90 ? 1 : 0;
        }

        public int AngleAxis()
        {
            return Math.Abs(this.Angle().roundNumber(90)) == 90 ? 1 : 0;
        }
    }
}
