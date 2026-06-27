using System;
using System.Collections.Generic;
using System.Linq;

namespace AcadScript
{
    
    public static class DE
    {

        public static pPos DEF_PAGE_SIZE = new pPos(PAGE_WIDTH, PAGE_HEIGHT);
        public static pPos DEF_VIEW_PT = new pPos(210, 157);

        public static List<int[]> DetectAdj(IEnumerable<object[]> lls,
            Func<IEnumerable<object[]>, int, int, bool> fn)
        {
            List<List<int>> resadj = new List<List<int>>();

            for (int i = 0; i < lls.Count(); i++)
            {
                int index = resadj.FindIndex(ls => ls.Contains(i));

                if (index == -1)
                {
                    resadj.Add(new List<int> { i });
                    index = resadj.Count - 1;
                }

                for (int j = 0; j < lls.Count(); j++)
                    if (i != j && !resadj[index].Contains(j) && fn(lls, i, j))
                    {
                        int index1 = resadj.FindIndex(ls => ls.Contains(j));

                        if (index1 == -1)
                            resadj[index].Add(j);
                        else
                        {
                            resadj[index].AddRange(resadj[index1]);
                            resadj[index1] = new List<int>();
                        }
                    }
            }

            return resadj.Where(ls => ls.Count > 0)
                .Select(ls => ls.Select(p => p).ToArray()).ToList();
        }

        public static double PAGE_WIDTH
        {
            get
            {
                return pString.INI_Value("OutputPageWidth");
            }
        }

        public static double PAGE_HEIGHT
        {
            get
            {
                return pString.INI_Value("OutputPageHeight");
            }
        }
        public static string _getNumberInString(this string st)
        {
            string key = "0123456789 ()[]{}_";
            string res = "";

            for (int i = 0; i < st.Length; i++)
                if (key.Contains(st[i]))
                    res += st[i];

            return res;
        }

        public static void AdjAddSegment(this List<int>[] adjs, int i, int j)
        {
            if (!adjs[i].Contains(j))
                adjs[i].Add(j);

            if (!adjs[j].Contains(i))
                adjs[j].Add(i);
        }

        static void _detectAdj(List<int>[] adjs, int v, List<int> path)
        {
            int[] adj = adjs[v].Select(n => n).ToArray();
            adjs[v] = new List<int>();

            foreach (int n in adj)
                if (adjs[n].Count > 0)
                {
                    path.Add(n);
                    _detectAdj(adjs, n, path);
                }
        }

        public static List<int[]> DetectAdj(this List<int>[] adjs)
        {
            List<int[]> result = new List<int[]>();

            for (int i = 0; i < adjs.Length; i++)
                if (adjs[i].Count == 0)
                    result.Add(new int[] { i });

            for (int i = 0; i < adjs.Length; i++)
                if (adjs[i].Count > 0)
                {
                    List<int> path = new List<int> { i };
                    _detectAdj(adjs, i, path);

                    if (path.Count > 0)
                        result.Add(path.ToArray());
                }

            return result;
        }


        public static pPos AC_VIEWSIZE = new pPos(1920, 1080);

        public static double DEF_BREAK_LINE_SIZE { get { return pString.INI_Value("BreakLineSize"); } }
        public static double DEF_LEADER_SIZE { get { return pString.INI_Value("LeaderSize"); } }
        public static double DEF_LOWER_STEEL_SIZE { get { return pString.INI_Value("LowerSteelSize"); } }
        public static double DEF_TEXT_SPACING { get { return pString.INI_Value("TextSpacing"); } }
        public static double DEF_TEXT_HEIGHT { get { return pString.INI_Value("TextSize_M"); } }
        public static double DEF_TITLE_HEIGHT { get { return pString.INI_Value("TitleHeight"); } }
        public static double DEF_TRACK_VALUE { get { return pString.INI_Value("TrackValue"); } }

        public static string CADLIB = @"D:\Dropbox\CADLIb\";
        public static string CAD_UI = CADLIB + @"UI\";
        public static string CADLIB_ELEMENT = CADLIB + @"Elements\";
        public static string CADLIB_CONSTRUCT = CADLIB + @"Constructs\";

        public static string CADLIB_CONSTRUCT_HATCHSAMPLE = CADLIB_CONSTRUCT + @"HatchSample.txt";
        public static string CADLIB_CONSTRUCT_HATCHSETTING = CADLIB_CONSTRUCT + @"HatchSetting.txt";

        public static string CADLIB_TEMPLATE_HATCH = CADLIB + @"Template_Hatch.dwg";

        public static string CAD_TEMPLATE_DIR = @"D:\Dropbox\CADLib\";
        public static string CAD_TEMPLATE_DIR_TEMP = @"D:\Temp\";
        public static string CAD_TEMPLATE_FILE = CAD_TEMPLATE_DIR + "Template.dwg";
        public static string CAD_TEMPLATE_ROOM_FILE = CAD_TEMPLATE_DIR + "Template_Room.dwg";
        public static string CAD_TEMPLATE_LAYOUT = CAD_TEMPLATE_DIR + "Template_Layout.dwg";
        public static string CAD_TEMPLATE_PREVIEW = CAD_TEMPLATE_DIR + @"_preview\";

        public static string CADLIB_ELEMENT_DESC = CADLIB_ELEMENT + @"Description.xlsx";
        public static string CADLIB_ELEMENT_DESC_LANDSCAPE = "Lands";
        public static string CADLIB_ELEMENT_DESC_CEILING = "Ceiling";
        public static string CADLIB_ELEMENT_LEGEND_COTEBLOCK = "G_Cote";

        public static string CADLIB_ELEMENT_LEGEND = CADLIB + @"Elements\Legend.dwg";
        public static string CADLIB_ELEMENT_FURNITURE = CADLIB + @"Elements\Furniture.dwg";

        public static string CADLIB_SAMPLES = CADLIB + @"Samples\";
        public static string CADLIB_PDF = CADLIB + @"PDF\";
        public static string CADLIB_SAMPLES_ROOM = CADLIB_SAMPLES + @"Room\";
        public static string CADLIB_SAMPLES_BLOCK = CADLIB_SAMPLES + @"Block\";
        public static string CADLIB_SAMPLES_DOOR = CADLIB_SAMPLES + @"Door\";
        public static string CADLIB_SAMPLES_WALL = CADLIB_SAMPLES + @"Wall\";
        public static string CADLIB_SAMPLES_DETAIL = CADLIB_SAMPLES + @"Detail\";

        public static string INI_FILE = CADLIB_CONSTRUCT + @"\INI_Setting.txt";
        public static string INI_DICT = CADLIB_CONSTRUCT + @"\INI_Dict.txt";
        public static string SAMPLE_COLLADA_DIR = @"D:\Dropbox\CADLib\Sample\";
        public static string SAMPLE_BOX_COLLADA = SAMPLE_COLLADA_DIR + "SampleBox1000x1000x1000.dae";
        public static string COLLADA_DIR = @"D:\Dropbox\3DLib\mesh\DAE\";
        //public static string INoteLeader.DEF_NAME = "G_NoteLeader";
        public static string DIRECTION_KEY = "@DIRECTION";

        //public static string DEF_POLYLINE_LAYER = DEF_LAYER_FIN;
        public static double DEF_POLYLINE_WIDTH = 0;

        public static string DEF_HATCH_PATTERN = "ANSI31";
        public static double DEF_HATCH_SCALE = 500;
        public static double DEF_HATCH_ANGLE = 0;

        public static int[] DEF_SCALE_LIST = new int[] { 2, 5, 10, 20, 25, 50, 80, 100, 200, 250, 400, 500, 1000, 2000 };
        public static string DEFPOINTS = "Defpoints";
        public static string DEF_BLOCK_NOTE = "G_NoteDetail";
        //public static string DEF_DIMENSION_NOTE = "G_DimNote";
        public static string DEF_BASEPOINT = "G_Basepoint";
        public static string DEF_LEGENDNO = "G_LegendNo";

        public static string DEF_LAYER_FREEZE = "A-Freeze|251|Continuous";
        public static string DEF_LAYER_CEILING = "A-Ceiling|2|Continuous";
        public static string DEF_LAYER_WALL = "A-WALL|4|Continuous";
        public static string DEF_LAYER_FIN = "A-FIN|1|Continuous";
        public static string DEF_LAYER_HATCH = "A-Hatch|8|Continuous";
        public static string DEF_LAYER_BEAM = "A-Beam|251|Continuous";
        public static string DEF_LAYER_COL = "A-Cols|2|Continuous";
        public static string DEF_LAYER_HIDDEN = "A-Hidden|8|HIDDEN2";
        public static string DEF_LAYER_SECT = "A-Sect|8|Continuous";
        public static string DEF_LAYER_TEXT = "G-Text|60|Continuous";
        public static string DEF_LAYER_REGION = "G-Region|6|Continuous";
        public static string DEF_LAYER_DIM = "G-Dim|122|Continuous";
        public static string DEF_LAYER_FURNITURE = "I-Furniture|30|Continuous";

        public static string DEF_TSTYLE = "txt 2.5 mm Times New Roman";
        public static string DEF_DIMSTYLE = "DIM 2.5 mm";
        public static string DEF_MLEADERSTYLE = "Lead 2.5 mm";
        public static string DEF_TABLESTYLE = "Tab 2.5 mm";
        public static string DEF_KHT = "KHT";
        public static string DEF_KHT_D = "KHT-D";

        public static string DEF_FILE_DIM = "Dim";
        public static string DEF_FILE_LEGEND = "Legend";
        public static string DEF_FILE_FINISH = "Finish";
        public static string DEF_FILE_LIGHT = "Light";

        public static string DEF_TITLE_VIEW = "HƯỚNG NHÌN";
        public static string DEF_TITLE_LEGEND = "MẶT BẰNG KÝ HIỆU";
        public static string DEF_TITLE_DIMENSION = "MẶT BẰNG KÍCH THƯỚC";
        public static string DEF_TITLE_FINISH = "MẶT BẰNG HOÀN THIỆN SÀN";
        public static string DEF_TITLE_LIGHT = "MẶT BẰNG TRẦN ĐÈN";

        public static string DEF_FURN_FRONT = "_F";
        public static string DEF_FURN_SIDE = "_S";

        public static int DEF_SPACING = 1000;
        public static byte DEF_LANGUAGE = 0; //0:Vietnamese,2:English
                                             //public static double DEF_TEXT_HEIGHT = 2.5;








        public static double DEF_REGION_OFFSET = 500;
        public static double DEF_DEF_HEIGHT = 3600;
        public static double DEF_DOOR_HEIGHT = 2200;

        public static double DEF_TABLE_ROWBREAK = 50;
        public static double DEF_TABLE_ROWHEIGHT = 150;
        public static double DEF_TABLE_COLUMNWIDTH = 600;

        public static int DEF_ARC_SEGMENTS = 16;

        public static double DEF_DRAWING_SPACING = 2000;
        //public static double DEF_TEXT_HEIGHT = 250;

        public static double DEF_DIM_SPACING = 15, DEF_DIM_OFFSET = 8;

        public static double DEF_VIEW_REGION_WIDTH = 385;
        public static double DEF_VIEW_REGION_HEIGHT = 255;


        public static double DEF_BEAMHEIGHT = 400;
        public static double DEF_BEAMWIDTH = 200;
        public static double DEF_SLABDEPTH = 150;

        public static double DEF_CEILING_LAMP_SPACING = 1500;
        public static string DEF_CEILING_GRID_KEYWORD = "gridCeil";
        public static string DEF_CEILING_CENTER_LAMP = "E_Center";

        public static string DEF_DOORTAG_LEGEND = "G_DoorTag";

        public static double DEF_GRIDNOTE_SPACING = 3200;
        public static string DEF_REMOVAL = "$Removal";

        public static double CADDETAIL_DWGSIZE = 50000;
        public static double CADDETAIL_SEPERATOR = 1000;

        public static string DEF_POLYLINE_LAYER = DEF_LAYER_FIN;


        public static string _replaceLineValue(this string st, string key, string new_value)
        {
            string[] lines = st.filter(";\r\n");
            st = "";

            foreach (string line in lines)
            {
                string res = line;
                if (line.Contains("="))
                {
                    string[] ar = line.filter("= ");
                    if (ar[0].Upper() == key.Upper())
                    {
                        res = key + " = " + new_value;
                    }
                }
                st += res + ";\r\n";
            }

            return st;
        }


        public static KeyValuePair<string, double> _propContent(this string src)
        {
            string st = src.st_("#") ? src.Substring(1) : src;

            string content = null;
            double size = 0;

            string[] keys = new string[] { "CONTENT", "TEXT", "NOTE" };
            string[] SizeList = new string[] { "", "S", "M", "L", "XL" };

            foreach (string s in SizeList)
                foreach (string k in keys)
                    if (!st._prop(k + s).empty())
                    {
                        content = st._prop(k + s);
                        size = pString.INI_Value(s.empty() ? "TEXTM" : "TEXT" + s);
                        break;
                    }

            return new KeyValuePair<string, double>(content, size);
        }

        public static bool _isValidNumber(this double v)
        {
            return !double.IsInfinity(v) && !double.IsNaN(v);
        }
        public static int[] NumericArray(int start, int end)
        {
            int[] res = new int[end - start + 1];
            for (int i = start; i <= end; i++) res[i - start] = i;
            return res;
        }


        public static string _replaceShortkeys(this string st)
        {
            string val = pString.INI_String("SHORTKEYS");
            string res = st;
            if (!val.empty())
            {
                val = val.Replace("~", "=");
                string[] ls = val.filter(";");

                foreach (string s in ls)
                {
                    st = st.Replace(s._firstPropName(), s._firstProp());
                }
                res = st;
            }
            return res.TrimStart(' ');
        }

        public static pPos[] ByAxis(this IEnumerable<pPos> ls, int axis)
        {
            return ls.Select(p => p.ByAxis(axis)).ToArray();
        }
        public static bool Compare2Pts(this pPos[] src1, pPos[] src2, int tole = 10)
        {
            bool res = false;
            pPos[] ls1 = src1.Select(p => p.Round(tole)).ToArray();
            pPos[] ls2 = src2.Select(p => p.Round(tole)).ToArray();

            if (ls1.Length == ls2.Length)
            {
                pPos[] ns1 = ls1.OrderBy(p => p.Z).ThenBy(p => p.Y).ThenBy(p => p.X).ToArray();
                pPos[] ns2 = ls2.OrderBy(p => p.Z).ThenBy(p => p.Y).ThenBy(p => p.X).ToArray();

                if (NumericArray(0, ns1.Length - 1).All(n =>
                    ns1[n].X == ns2[n].X && ns1[n].Y == ns2[n].Y && ns1[n].Z == ns2[n].Z))
                {
                    int[] num1 = ls1.Select(g => Array.FindIndex(ns1, p => g.X == p.X
                        && g.Y == p.Y && g.Z == p.Z)).ToArray();

                    int[] num2 = ls2.Select(g => Array.FindIndex(ns1, p => g.X == p.X
                        && g.Y == p.Y && g.Z == p.Z)).ToArray();

                    num1.LeftShiftArray(Array.IndexOf(num1, 0));

                    num2.LeftShiftArray(Array.IndexOf(num2, 0));
                    int[] rev_num2 = num2.Reverse().ToArray();
                    rev_num2.LeftShiftArray(Array.IndexOf(num1, 0));

                    int[] ar = NumericArray(0, num1.Length - 1);
                    res = ar.All(n => num1[n] == num2[n]) || ar.All(n => num1[n] == rev_num2[n]);
                }
            }

            return res;
        }
        public static double Area3D(this IEnumerable<pPos> PtList)
        {
            int nPts = PtList.Count();
            pPos a = new pPos(0, 0);
            int j = 0;

            for (int i = 0; i < nPts; ++i)
            {
                j = (i + 1) % nPts;
                a += PtList.ElementAt(i).Cross(PtList.ElementAt(j));
            }

            a /= 2;

            return a.Length;
        }

        public static pPos[] Straighten(this IEnumerable<pPos> pts, bool closed, double min_angle = 1)
        {
            List<pPos> res = new List<pPos>();
            PosCollection segs = pts.GetSegment(closed);
            double[] angles = NumericArray(0, segs.Count - 1)
                .Select(n => Math.Abs((segs[(n + 1) % segs.Count][1] - segs[(n + 1) % segs.Count][0])
                .AngleVsBaseVector(segs[n][1] - segs[n][0])) % 180).ToArray();

            for (int i = 0; i < angles.Length; i++)
                if (angles[i] >= min_angle && angles[i] <= 180 - min_angle)
                    res.Add(pts.ElementAt((i + 1) % pts.Count()));

            return res.ToArray();
        }

        public static string ToTextBool(this IEnumerable<bool> objs, string seperator)
        {
            string res = "";
            foreach (bool obj in objs)
                res += obj.ToString() + seperator;
            if (res.Length > 0)
                res = res.Substring(0, res.Length - 1);
            return res;
        }


        public static string ToTextObj(this IEnumerable<object> objs, string seperator)
        {
            string res = "";
            foreach (double obj in objs)
                res += obj.ToString() + seperator;
            if (res.Length > 0) res = res.Substring(0, res.Length - 1);
            return res;
        }


        public static pPos[] OffsetOpen(this IEnumerable<pPos> pts, double v)
        {
            List<pPos> res = new List<pPos>();
            PosCollection segs = pts.GetSegment(false)
                .Where(seg => seg.Length(false) > 1)
                .Select(seg => seg[0].Parallel(seg[1], v)).ToCollectionSameClosed();

            if (segs.Count == 1)
                res = segs[0].ToList();
            else if (segs.Count > 1)
            {
                for (int i = 0; i < segs.Count; i++)
                    res.AddRange(segs[i]);
            }
            else
                res = pts.ToList();

            return res.ToArray();
        }

        public static pPos[] CenterMaxSegment_Seg;
        public static pPos[] FillPatternRegion_InternalRect;

        public static bool _isInSectionList(this IEnumerable<int[]> src, int[] ints)
        {
            return src.Any(ls => ints.All(v => ls.Contains(v)));
        }

        public static PosCollection CutExtrude_Sections;

        public static pPos[] MaxSegment(this IEnumerable<pPos> ls, bool closed = true)
        {
            return ls.GetSegment(closed).OrderBy(l => -l.Length())
                .ThenBy(l => l.CenterPoint().X).ThenBy(l => l.CenterPoint().Y).First();
        }

        public static PosCollection AllMaxSegment(this IEnumerable<pPos> ls, bool closed = true)
        {
            PosCollection segs = ls.GetSegment(closed);
            double d = segs.Max(l => l.Length());
            return segs.Where(l => Math.Abs(l.Length() - d) < 10).ToCollectionSameClosed();
        }

        public static PosCollection Cut(this IEnumerable<pPos> pls,
            IEnumerable<pPos> _sectline, bool isLeft = true)
        {
            List<pPos> sectline = _sectline.OrderBy(p => p.X.roundNumber(50)).ThenBy(p => p.Y).ToList();
            PosCollection res = new PosCollection();
            pPos[] r = pls.Boundary().Rect();

            pPos[] onbound = r.Where(p => p.isLeft(sectline.First(), sectline.Last()) != isLeft)
                .Select(p => p).OrderBy(p => p.ProjectLine(sectline[0], sectline[1]).DistanceTo(sectline.First())).ToArray();

            if (onbound.Length > 0)
            {
                sectline.Insert(0, onbound.First());
                sectline.Add(onbound.Last());
                res = pls.ComputePolygons(sectline, EN_POLYGON_JOIN.EN_DIFFERENCE);
            }
            else if (pls.First().isLeft(sectline.First(), sectline.Last()) == isLeft)
                res = pls.ToCollection();

            return res;
        }

        static double sinA(pPos[] seg)
        {
            return Math.Sin((seg[1] - seg[0]).Angle() * Math.PI / 180);
        }

        public static pPos Direction(this IEnumerable<pPos> pls)
        {
            PosCollection segs = pls.GetSegment();
            List<double> sins = new List<double>();

            foreach (pPos[] seg in segs)
            {
                double v = sinA(seg);

                //Console.WriteLine("Sin Angle {0}", v);

                if (!sins.Contains(v))
                    sins.Add(v);
            }

            List<double> lengths = new List<double>();

            foreach (double sin in sins)
            {
                PosCollection ls = segs.Where(seg => sinA(seg) == sin || sinA(seg) == -sin).ToCollectionSameClosed();

                double v = 0;
                foreach (pPos[] seg in ls)
                    v += seg.Length();

                lengths.Add(v);
            }

            int[] indexes = NumericArray(0, lengths.Count - 1);
            indexes = indexes.OrderBy(n => -lengths[n]).ToArray();

            //double res = Math.Asin(sins[indexes.First()]);
            //Console.WriteLine("Angle {0}", sins[indexes.First()] / Math.PI * 180);

            return new pPos(1, Math.Tan(Math.Asin(sins[indexes.First()]))).Normalize;
        }

        public static pPos[] FillPatternRegion(this IEnumerable<pPos> Pts,
            double sample_sizeX, double sample_sizeY, pPos Basepoint)
        {
            //List<pPos> res = new List<pPos>();
            pPos[] bb = Pts.Boundary();

            Console.WriteLine("Pts {0} BB {1}x{2}", Pts.Count(), bb.Size().X, bb.Size().Y);

            int total_x = (int)((bb[1].X - bb[0].X) / sample_sizeX);
            int total_y = (int)((bb[1].Y - bb[0].Y) / sample_sizeY);

            pPos mv = new pPos((Basepoint.X - bb[0].X) % sample_sizeX, (Basepoint.Y - bb[0].Y) % sample_sizeY);
            pPos[] inner = Pts.Offset(-150);

            pPos[] around_pts = new pPos[] { new pPos(sample_sizeX / 2, sample_sizeY / 2),
                new pPos(sample_sizeX / 2, 0), new pPos(0, sample_sizeY / 2), new pPos(0, 0) };

            PosCollection opts = new PosCollection();

            for (int opt = 0; opt < 4; opt++)
            {
                List<pPos> pts = new List<pPos>();
                for (int i = 0; i < total_x; i++)
                    for (int j = 0; j < total_y; j++)
                    {
                        pPos pt = new pPos(i * sample_sizeX, j * sample_sizeY)
                            + bb[0] + mv - around_pts[opt];
                        if (rectFromPoint(pt, sample_sizeX, sample_sizeY).Inside(inner))
                            pts.Add(pt);
                    }

                opts.Add(pts.ToArray());
            }

            opts = opts.OrderBy(ls => -ls.Length).ToCollectionSameClosed();
            pPos[] res = null;

            if (opts.Count > 0)
            {
                res = opts.First;

                GraphConsole.Compute(res.Select(pt => rectFromPoint(pt, sample_sizeX, sample_sizeY, true)).ToArray());
                if (GraphConsole.ResultBorders.Count > 0)
                    FillPatternRegion_InternalRect = GraphConsole.ResultBorders[0].NormalizeList();
            }
            return res.ToArray();
        }

        static pPos[] rectFromPoint(pPos pt, double sizeX, double sizeY, bool closed = false)
        {
            return closed ? new pPos[] { pt + new pPos(-sizeX / 2, -sizeY / 2),
                                pt + new pPos(sizeX / 2, -sizeY / 2),
                                pt + new pPos(sizeX / 2, sizeY / 2),
                                pt + new pPos(-sizeX / 2, sizeY / 2),
                                pt + new pPos(-sizeX / 2, -sizeY / 2)}
                            : new pPos[] { pt + new pPos(-sizeX / 2, -sizeY / 2),
                                pt + new pPos(sizeX / 2, -sizeY / 2),
                                pt + new pPos(sizeX / 2, sizeY / 2), pt + new pPos(-sizeX / 2, sizeY / 2)};
        }

        public static pPos[] Multiply(this IEnumerable<pPos> pts, double v)
        {
            return pts.Select(p => p * v).ToArray();
        }

        public static pPos[] ClosedPolyline(this IEnumerable<pPos> pts)
        {
            List<pPos> res = pts.ToList();
            if (pts.First().DistanceTo(pts.Last()) > 1)
                res.Add(pts.First());
            return res.ToArray();
        }

        public static pPos CenterMaxSegment(this IEnumerable<pPos> pts, bool closed = false)
        {
            pPos res = null;
            PosCollection segs = pts.GetSegment(closed);
            double max = segs.Max(l => l.Length(false));
            int index = segs.FindIndex(l => l.Length(false) == max);
            if (index != -1)
            {
                CenterMaxSegment_Seg = segs[index];
                res = segs[index].CenterPoint();
            }

            return res;
        }

        public static int[] SplitByAngle(this IEnumerable<pPos> pts, double min_check_angle, bool Pts_closed)
        {
            List<int> break_points = new List<int>();
            for (int i = 0; i < pts.Count(); i++)
            {
                if (Pts_closed || i < pts.Count() - 1)
                {
                    int nex = (i + 1) % pts.Count();
                    int prev = (i - 1 + pts.Count()) % pts.Count();

                    pPos p1 = pts.ElementAt(prev), p2 = pts.ElementAt(i), p3 = pts.ElementAt(nex);
                    double ang = Math.Abs((p2 - p1).AngleVsBaseVector(p3 - p1));

                    if (ang < min_check_angle)
                        break_points.Add(i);
                }
            }

            return break_points.ToArray();
        }

        public static PosCollection OrderSegments(this IEnumerable<pPos> pts, bool closed = false)
        {
            PosCollection res = pts.GetSegment(closed).OrderBy(ls => -ls.Length(false)).ToCollectionSameClosed(closed);
            CenterMaxSegment_Seg = res.First;

            return res;
        }

        public static pPos[] GetSegmentByShapeIndexAndPointIndex(this IEnumerable<pPos[]> src, int index, int segment_index)
        {
            return (src.ElementAt(index).GetSegmentByIndex(segment_index));
        }

        public static PosCollection ToCollection(this IEnumerable<pPos> src, bool closed = true)
        {
            return new List<pPos[]> { src.ToArray() }.ToCollectionClosedList(new bool[] { closed });
        }

        public static PosCollection ToCollectionClosedList(this IEnumerable<pPos[]> src, IEnumerable<bool> closed)
        {
            PosCollection res = new PosCollection();
            foreach (pPos[] ls in src)
                res.Add(ls);
            res.Closed = closed.ToArray();

            return res;
        }

        public static PosCollection ToCollectionSameClosed(this IEnumerable<pPos[]> src, bool closed = true)
        {
            PosCollection res = new PosCollection();
            foreach (pPos[] ls in src)
                res.Add(ls);
            res.Closed = res.Select(l => closed).ToArray();

            return res;
        }

        public static PosCollection RemoveSegments(this IEnumerable<pPos[]> src, IEnumerable<int> remove_index)
        {
            //odd: spline_index, even: segment_index

            List<int>[] indexes = src.Select(ls => new List<int>()).ToArray();

            for (int i = 0; i < remove_index.Count(); i += 2)
            {
                int n1 = remove_index.ElementAt(i);
                int n2 = remove_index.ElementAt(i + 1);
                indexes[n1].Add(n2);
            }

            PosCollection res = new PosCollection();

            for (int i = 0; i < indexes.Length; i++)
                if (indexes[i].Count > 0)
                    res.AddRange(src.ElementAt(i).RemoveSegmentsByTole(1, indexes[i].ToArray()));
                else
                    res.Add(src.ElementAt(i));

            return res;
        }

        public static pPos[] GetSegmentByIndex(this IEnumerable<pPos> src, int segment_index)
        {
            pPos[] res = null;
            if (segment_index < src.Count())
            {
                res = new pPos[] { src.ElementAt(segment_index), src.ElementAt((segment_index + 1) % src.Count()) };
            }
            return res;
        }

        public static PosCollection GetSegment(this IEnumerable<pPos> src, bool closed = true)
        {
            PosCollection res = new PosCollection();
            int total = closed ? src.Count() : src.Count() - 1;
            for (int segment_index = 0; segment_index < total; segment_index++)
                res.Add(src.GetSegmentByIndex(segment_index));

            return res;
        }

        public static int _findIndex(this List<int> ls, int index, int index_a)
        {
            for (int i = 0; i < ls.Count; i += 2)
                if (ls[i] == index && ls[i + 1] == index_a)
                    return i;
            return -1;
        }

        public static int _findIndexes(this List<List<int>> spline_index, int index, int index_a)
        {
            for (int i = 0; i < spline_index.Count; i++)
            {
                int n = spline_index[i]._findIndex(index, index_a);
                if (n != -1)
                    return i;
            }
            return -1;
        }

        public static PosCollection RemoveSegmentsByTole(this IEnumerable<pPos> src, double tole, params int[] _indxs)
        {
            bool closed = src.First().DistanceTo(src.Last()) < tole;
            int[] indxs = _indxs.OrderBy(v => v).ToArray();

            PosCollection pls = new PosCollection();
            int cur = 0;
            List<pPos> ls = new List<pPos>();

            for (int i = 0; i < src.Count(); i++)
                if (indxs.Contains(i))
                {
                    ls = new List<pPos>();
                    for (int j = cur; j <= i; j++)
                        ls.Add(src.ElementAt(j));

                    if (ls.Count > 1)
                        pls.Add(ls.ToArray());
                    cur = i + 1;
                }

            if (cur < src.Count())
            {
                ls = new List<pPos>();
                for (int j = cur; j < src.Count(); j++)
                    ls.Add(src.ElementAt(j));
                pls.Add(ls.ToArray());
            }

            if (closed && !_indxs.Contains(0))
            {
                ls = pls.Last.ToList();
                ls.RemoveAt(ls.Count - 1);
                ls.AddRange(pls[0]);

                pls[0] = ls.ToArray();
                pls.RemoveAt(pls.Count - 1);
            }

            return pls;
        }

        public static PosCollection VisibleSpline(pPos p1, pPos p2, pPos p3, pPos p4)
        {
            pPos[] res = null;
            if (p1.IsOnLine(p3, p4) && p2.IsOnLine(p3, p4))
            {
                if (p1.IsBetween(p3, p4) || p2.IsBetween(p3, p4) || p3.IsBetween(p1, p2) || p4.IsBetween(p1, p2))
                {
                    pPos[] pts = new pPos[] { p1, p2, p3, p4 }.OrderBy(p => p.X).ThenBy(p => p.Y).ToArray();
                    res = new pPos[] { pts.First(), pts.Last() };
                }
            }

            return res == null ? new List<pPos[]> { new pPos[] { p1, p2 }, new pPos[] { p3, p4 } }.ToCollectionSameClosed() : res.ToCollection();
        }

        public static PosCollection VisibleSplines(this IEnumerable<pPos> segments, double tole)
        {
            //pair points
            PosCollection res = new PosCollection();
            pPos[] new_pls = segments.OrderBy(p => p.X).ThenBy(p => p.Y).ToArray();
            int cur = 0;

            for (int i = 0; i < new_pls.Length - 1; i++)
                if (new_pls[i].DistanceTo(new_pls[i + 1]) > tole)
                {
                    pPos mid = (new_pls[i] + new_pls[i + 1]) / 2;

                    bool found = false;
                    for (int j = 0; j < segments.Count(); j += 2)
                        if (mid.IsBetween(segments.ElementAt(j), segments.ElementAt(j + 1)))
                        {
                            found = true;
                            break;
                        }

                    if (!found)
                    {
                        //ACD.WR("Not found {0},{1}", cur, i);
                        res.Add(new pPos[] { new_pls[cur], new_pls[i] });
                        cur = i + 1;
                    }
                }

            //ACD.WR("Found WX");
            if (cur < new_pls.Length - 1)
                res.Add(new pPos[] { new_pls[cur], new_pls[new_pls.Length - 1] });

            return res;
        }

        public static pPos[] WeldSplines(this IEnumerable<pPos> pts1, IEnumerable<pPos> pts2, double tole)
        {
            List<pPos> res = null;
            if (pts1.First().DistanceTo(pts2.First()) < tole)
            {
                res = new List<pPos>();
                res.AddRange(pts1.Reverse());
                res.AddRange(pts2);
            }
            else if (pts1.Last().DistanceTo(pts2.Last()) < tole)
            {
                res = new List<pPos>();
                res.AddRange(pts1);
                res.AddRange(pts2.Reverse());
            }
            else if (pts1.Last().DistanceTo(pts2.First()) < tole)
            {
                res = new List<pPos>();
                res.AddRange(pts1);
                res.AddRange(pts2);
            }
            else if (pts1.First().DistanceTo(pts2.Last()) < tole)
            {
                res = new List<pPos>();
                res.AddRange(pts1.Reverse());
                res.AddRange(pts2.Reverse());
            }
            return res.ToArray();
        }

        public static PosCollection Trim(this IEnumerable<pPos> _src, IEnumerable<pPos> region,
            bool _is_src_closed = false, bool inside_region = true)
        {
            PosCollection res = new PosCollection();
            List<pPos> src = _src.ToList();
            if (_is_src_closed)
            {
                //GraphConsole.Compute(new PosCollection { _src.ToArray(), region.ToArray() });
                res = _src.ToCollection().ComputePolygons(region.ToCollection(),
                    inside_region ? EN_POLYGON_JOIN.EN_INTERSECTION : EN_POLYGON_JOIN.EN_DIFFERENCE);

                if (!inside_region)
                {
                    res = res.Where(ls => ls.All(p => p.Inside(_src))).ToCollectionSameClosed();
                }

                res = res.Select(ls => ls.Add(ls.First())).ToCollectionSameClosed();
            }
            else
            {
                for (int i = 0; i < src.Count - 1; i++)
                {
                    PosCollection pts = _hiddenSegment(src[i], src[i + 1], region, inside_region);

                    if (pts != null)
                    {
                        //if (pts.All(ls => ls.MidPts().Inside(region)) == outside_region)
                        res.AddRange(pts);
                    }
                    else
                    {
                        pPos[] ls = new pPos[] { src[i], src[i + 1] };
                        //if ((ls.MidPts().Inside(region)) == outside_region)
                        res.Add(ls);

                    }
                }

                //res = res.Where(ls => ls.MidPts()
                //    .All(p => p.Inside(region) == inside_region)).ToCollection().WeldSplines;
            }
            return res;
        }

        static PosCollection _hiddenSegment(pPos p1, pPos p2, IEnumerable<pPos> region, bool inside_region = true)
        {
            PosCollection res = new PosCollection();
            List<pPos> ints = new pPos[] { p1, p2 }.IntersectPts(region, false, true, false).ToList();

            if (ints.Count > 0)
            {
                ints.Add(p1);
                ints.Add(p2);
                ints = ints.OrderBy(p => p.DistanceTo(p1)).ToList();

                for (int i = 0; i < ints.Count - 1; i++)
                {
                    pPos midpt = (ints[i] + ints[i + 1]) / 2;
                    if (midpt.Inside(region) == inside_region)
                        res.Add(new pPos[] { ints[i], ints[i + 1] });
                }

                for (int i = 0; i < res.Count; i++)
                    res[i] = res[i].OrderBy(p => p.DistanceTo(p1)).ToArray();

                res = res.OrderBy(ls => ls[0].DistanceTo(p1)).ToCollectionSameClosed();
            }
            else if ((!p1.Inside(region.Offset(-1)) || !p2.Inside(region.Offset(-1))))
                res = null;

            return res;
        }

        public static PosCollection ComputePolygons(this IEnumerable<pPos> sub, IEnumerable<pPos> clip, EN_POLYGON_JOIN jointype)
        {
            return sub.ToCollection().ComputePolygons(clip.ToCollection(), jointype);
        }


        public static bool IsIntersect(this IEnumerable<pPos> sub, IEnumerable<pPos> clip)
        {
            return sub.ToCollection().ComputePolygons(clip.ToCollection(),
                EN_POLYGON_JOIN.EN_INTERSECTION).Count > 0;
        }

        public static double EllipsePerimeter(double a, double b)
        {
            return 2 * Math.PI * Math.Sqrt((a * a + b * b) / 2);
        }

        public static pPos[] EllipseSegments(double a, double b, pPos ct, int divs)
        {
            //double perm = EllipsePerimeter(a, b) / 2;
            //int divs = (int)(Math.Ceiling(perm / limit) * 2);

            //db.WR("DIVS = {0}\r\n", divs);

            double t = 0;
            pPos lastpoint = new pPos(ct.X - a, ct.Y);
            var res = new List<pPos>();

            while (t < Math.PI * 2)
            {
                double ePX = ct.X + (a * Math.Cos(t));
                double ePY = ct.Y + (b * Math.Sin(t));

                res.Add(new pPos(ePX, ePY));
                t += Math.PI * 2 / divs;
            }

            return res.ToArray();
        }

        private static pPos DegreesToXY(double degrees, double radius, pPos origin)
        {
            double radians = degrees * Math.PI / 180.0;

            pPos xy = new pPos(Math.Cos(radians) * radius + origin.X,
                                    Math.Sin(-radians) * radius + origin.Y);

            return xy;
        }

        public static pPos[] EllipseInsideRect(this pPos[] pts)
        {
            pPos[] bb = pts.Boundary();
            double a = bb.Last().X - bb.First().X, b = bb.Last().Y - bb.First().Y;

            return new pPos[] { bb.First(), new pPos(a, b) };
        }

        public static KeyValuePair<pPos, double> CircleInsideRect(this pPos[] pts)
        {
            pPos[] bb = pts.Boundary();
            double a = bb.Last().X - bb.First().X, b = bb.Last().Y - bb.First().Y;
            double d = a < b ? a : b;
            return new KeyValuePair<pPos, double>(bb.CenterPoint(), d / 2);
        }

        /*public static pPos[] InternalRect(this IEnumerable<pPos> pts, pPos basept)
        {
            pPos v = pts.Last() - pts.First();
            pPos[] res = null;

            foreach(pPos np in pts)
            {
                pPos[] lA = pts.Intersect(np, np + v);
                PosCollection ls = new PosCollection { lA };

                for (int i = 0; i < 2; i++)
                //if(pts.All(p => p.DistanceTo(pt) > 10))
                {
                    pPos pt = i == 0 ? lA.First() : lA.Last();
                    pPos[] lB = pts.Intersect(pt, pt + v.Project());

                    if (lB.All(p => p.Inside(pts)))
                        ls.Add(lB);

                    foreach (pPos pt2 in lB)
                    {
                        pPos[] lC = pts.Intersect(pt2, pt2 + v);
                        if (lC.All(p => p.Inside(pts)))
                            res.Add(lC);
                    }
                }

                GraphConsole.Compute(ls.ToArray());
                Console.Write("LS {0} Result {1}\n", ls.Count, GraphConsole.ResultPts.Count);

                if (GraphConsole.ResultPts.Count > 0)
                {
                    int index =  GraphConsole.ResultPts.FindIndex(tmp => basept.Inside(ls));

                    Console.Write("LS {0} Index {1}\n", ls.Count, index);

                    if (index != -1)
                    {
                        res = GraphConsole.ResultPts[index];
                        break;
                    }
                }
            }
            return res;
        }*/

        public static PosCollection _intRect(IEnumerable<pPos> pts, pPos basept)
        {
            pPos v = pts.Last() - pts.First();
            pPos[] lA = pts.Intersect(basept, basept + v);
            PosCollection res = lA.ToCollection();

            for (int i = 0; i < 2; i++)
            //if(pts.All(p => p.DistanceTo(pt) > 10))
            {
                pPos pt = i == 0 ? lA.First() : lA.Last();
                pPos[] lB = pts.Intersect(pt, pt + v.Project());

                if (lB.All(p => p.Inside(pts))) res.Add(lB);

                foreach (pPos pt2 in lB)
                {
                    pPos[] lC = pts.Intersect(pt2, pt2 + v);
                    if (lC.All(p => p.Inside(pts)))
                        res.Add(lC);
                }
            }

            GraphConsole.Compute(res);

            return GraphConsole.ResultPts;//.Where(ls => DeepInside(ls, pts)).Select(ls => ls).ToList();
        }


        public static pPos[] AllKnots(this IEnumerable<pPos> pts)
        {
            return pts.ToCollection().AllKnots;
        }

        public static pPos[] ThreePointRect(this IEnumerable<pPos> pts)
        {
            pPos pA = null, pB = null, pC = null, pD = null;
            for (int i = 0; i < 3; i++)
            {
                pPos prev = pts.ElementAt((i - 1 + pts.Count()) % pts.Count());
                pPos nex = pts.ElementAt((i + 1) % pts.Count());
                pPos prj = pts.ElementAt(i).ProjectLine(prev, nex);

                if (prj.DistanceTo(prev) < 1)
                {
                    pA = pts.ElementAt(i);
                    pB = prev;
                    pC = nex;
                }
                else if (prj.DistanceTo(nex) < 1)
                {
                    pA = pts.ElementAt(i);
                    pB = nex;
                    pC = prev;
                    break;
                }
            }

            if (pA != null)
                pD = pA.Intersect(pA + pC - pB, pC, pC + pB - pA, false);
            return pD != null ? new pPos[] { pC, pB, pA, pD } : null;
        }

        public static bool DeepInside(this IEnumerable<pPos> ls, IEnumerable<pPos> lsParent)
        {
            bool res = false;

            //db.WR("LS {0} LSPARENT {1}", ls, lsParent);
            if (ls.Inside(lsParent))
            {
                res = ls.GetSegment().All(seg => seg[0].Along(100, seg[1]).Inside(lsParent)
                && seg[1].Along(100, seg[0]).Inside(lsParent));
            }
            return res;
        }

        public static PosCollection CornerlineFRpt(this pPos pt, IEnumerable<pPos> pts)
        {
            PosCollection res = new PosCollection();
            pPos v = pts.Last() - pts.First();

            for (int i = 0; i < 2; i++)
            {
                pPos[] intps = pts.Intersect(pt, pt + (i == 0 ? v : v.Project()));

                if (intps != null && intps.Length > 0)
                    res.Add(intps);
            }
            return res;
        }

        public static pPos[] Subtract(this IEnumerable<pPos> pts, IEnumerable<pPos> inner)
        {
            pPos[] res = null;

            //For closed polyline, need to add firstpoint to last
            GraphConsole.Compute(new pPos[][] { pts.ToArray().Add(pts.First()),
                inner.ToArray().Add(inner.First()) });
            PosCollection regions = GraphConsole.ResultPts;

            //ACD.WR("regions_result {0}", regions.Count);

            if (regions.Count > 0)
            {
                pPos[] outer_inner = inner.Offset(1);
                regions = regions.OrderBy(ls => -ls.Area()).ToCollectionSameClosed();
                int index = regions.FindIndex(ls => !ls.All(pt => pt.Inside(outer_inner)));
                if (index != -1)
                    res = regions[index];
            }
            return res;
        }

        public static double LayoutRotation(this IEnumerable<pPos> Pts)
        {
            return Math.Abs(Pts.First().Y - Pts.Boundary()[1].Y) < 5 ? 90 : 0;
        }

        public static bool LayoutOtherPage(this IEnumerable<pPos> Pts)
        {
            return Math.Abs(Pts.First().X - Pts.Boundary()[1].X) < 5;
        }

        public static PosCollection RectSegments(this IEnumerable<pPos> r)
        {
            return new List<pPos[]> { new pPos[] {r.ElementAt(0),r.ElementAt(1) },
                new pPos[] { r.ElementAt(1), r.ElementAt(2) },
                new pPos[] { r.ElementAt(2), r.ElementAt(3) },
                new pPos[] { r.ElementAt(3), r.ElementAt(0) }}.ToCollectionSameClosed();
        }

        public static bool IsRoom(this IEnumerable<pPos> pts)
        {
            bool res = false;
            if (pts != null && pts.Count() > 5)
            {
                pPos pt1 = pts.First();
                pPos pt2 = pts.ElementAt(1);
                pPos pt_last = pts.Last();
                pPos pt_t = pts.ElementAt(pts.Count() - 2);

                res = pt_t.IsOnLine(pt1, pt2)
                    && pt1.IsBetween(pt2, pt_last) && pt_last.IsBetween(pt_t, pt1)
                    && pt2.IsOnLine(pt1, pt_last) && pt1.DistanceTo(pt2) >= 50; // && pts.Area() > 1000000;
            }
            return res;
        }

        //public static pPos[] Straighten(this IEnumerable<pPos> pts)
        //{
        //    List<int> removes = new List<int>();
        //    List<pPos> res = pts.ToList();
        //    for (int i = 0; i < pts.Count(); i++)
        //    {
        //        int nex = (i + 1) % pts.Count();
        //        int prev = (i - 1 + pts.Count()) % pts.Count();

        //        if(pts.ElementAt(nex).DistanceTo(pts.ElementAt(i)) < 10 ||
        //            Math.Abs((pts.ElementAt(nex) - pts.ElementAt(i)).Angle() 
        //            - (pts.ElementAt(i) - pts.ElementAt(prev)).Angle()) <= 5)
        //        {
        //            res[nex] = pts.ElementAt(nex).Project(pts.ElementAt(prev), pts.ElementAt(i));
        //            removes.Add(i);
        //        }
        //    }

        //    List<pPos> cvs = new List<pPos>();
        //    for (int i = 0; i < res.Count; i++)
        //        if(!removes.Contains(i)) cvs.Add(res[i]);
        //    return cvs.ToArray();
        //}

        public static bool isProject(this IEnumerable<pPos> pts, pPos line1, pPos line2)
        {
            pPos v1 = (pts.ElementAt(1) - pts.ElementAt(0)).Normalize;
            pPos v2 = (line2 - line1).Normalize;

            return Math.Abs(v1.X * v2.X + v1.Y * v2.Y) < 0.1;
        }

        public static pPos[] RecognizeDoor(this IEnumerable<pPos> pts)
        {
            pPos[] res = new pPos[0];
            if (pts != null && pts.Count() > 5)
                res = new pPos[] {pts.ElementAt(1), pts.ElementAt(0), (pts.ElementAt(0) + pts.ElementAt(pts.Count() - 1)) / 2,
                            pts.ElementAt(pts.Count() - 1), pts.ElementAt(pts.Count() - 2)};
            return res;
        }

        //public static pPos[] OpenPts(this IEnumerable<pPos> pts)
        //{
        //    List<int[]>[] indexes = pts.RecognizeOpening();
        //    List<pPos> res = new List<pPos>();

        //    for (int i = 0; i < pts.Count(); i++)
        //    {
        //        bool found = false;

        //        for (int j = 1; j < indexes.Length; j++)
        //        {
        //            List<int[]> ls = indexes[j];
        //            if (ls.Any(sub_ls => sub_ls[0] <= i && i <= sub_ls[sub_ls.Length - 1]))
        //            {
        //                found = true;
        //                break;
        //            }
        //        }
        //        if (!found)
        //            res.Add(pts.ElementAt(i));
        //    }
        //    return res.ToArray();
        //}

        public static pPos[] StandardRect(this IEnumerable<pPos> pts, pPos[] r)
        {
            pts.ElementAt(0).DistanceToPts(r);
            pPos[] Rect = new pPos[4];

            for (int j = 0; j < 4; j++)
                Rect[j] = r[(pPos.DistanceTo_Index + j) % r.Length];

            pPos MidOpening = (pts.First() + pts.Last()) / 2;

            if (MidOpening.isLeft((Rect[0] + Rect[1]) / 2, (Rect[2] + Rect[3]) / 2))
            {
                pts = pts.Reverse();

                pPos tmp = Rect[0];
                Rect[0] = Rect[1];
                Rect[1] = tmp;

                tmp = Rect[2];
                Rect[2] = Rect[3];
                Rect[3] = tmp;
            }

            return Rect;
        }

        public static bool _isRoom(this IEnumerable<pPos> pts)
        {
            bool res = false;
            int count = pts.Count();

            res = count > 4 && pts.ElementAt(0).IsOnLine(pts.ElementAt(1), pts.ElementAt(count - 1))
                && pts.ElementAt(0).IsOnLine(pts.ElementAt(count - 1), pts.ElementAt(count - 2))
                && pts.ElementAt(0).IsBetween(pts.ElementAt(1), pts.ElementAt(count - 1))
                && pts.ElementAt(count - 1).IsBetween(pts.ElementAt(0), pts.ElementAt(count - 2));

            return res;
        }

        public static pPos[] Stretch(this IEnumerable<pPos> pts, pPos[] region, pPos movept)
        {
            return pts.Cast<pPos>().Select(pt => pt.Inside(region) ? pt + movept : pt).ToArray();
        }


        public static int countPoints(this IEnumerable<pPos> pts, Func<IEnumerable<pPos>, bool> fn)
        {
            int count = 0;
            for (int i = 0; i < pts.Count(); i++)
                if (fn(pts))
                    count++;
            return count;
        }

        public static pPos[] MoveByAxis(this IEnumerable<pPos> pts, double x, double y)
        {
            return pts.Move(new pPos(x, y));
        }


        public static pPos[] Move(this IEnumerable<pPos> pts, pPos movept)
        {
            pPos[] res = new pPos[pts.Count()];
            for (int i = 0; i < pts.Count(); i++)
            {
                res[i] = pts.ElementAt(i) + movept;
                res[i].Content = pts.ElementAt(i).Content;
            }
            return res;
        }

        public static double Angle(double x1, double y1, double x2, double y2)
        {
            double dtheta, theta1, theta2;

            theta1 = Math.Atan2(y1, x1);
            theta2 = Math.Atan2(y2, x2);
            dtheta = theta2 - theta1;
            while (dtheta > Math.PI)
                dtheta -= (Math.PI * 2);
            while (dtheta < -Math.PI)
                dtheta += (Math.PI * 2);
            return (dtheta);
        }

        public static pPos[] Reverse(this IEnumerable<pPos> cc)
        {
            List<pPos> lsRes = new List<pPos>();
            for (int i = cc.Count() - 1; i >= 0; i--)
                lsRes.Add(cc.ElementAt(i));
            return lsRes.ToArray();
        }

        public static double Length(this IEnumerable<pPos> lsPoint, bool closed = true)
        {
            double res = 0;
            if (lsPoint.Count() == 2)
                closed = false;

            for (int i = 0; i < lsPoint.Count(); i++)
            {
                int a = i;
                int b = i + 1;

                if (b >= lsPoint.Count())
                {
                    if (closed)
                        b = 0;
                    else
                        break;
                }

                res += lsPoint.ElementAt(a).DistanceTo(lsPoint.ElementAt(b));
            }
            return res;
        }

        public static bool IntersectBounding(this IEnumerable<pPos> set1, IEnumerable<pPos> set2, bool withInside = false)
        {
            var bb1 = set1.Boundary();
            var bb2 = set2.Boundary();
            bool b = false;

            if (!withInside && ((bb1[0].InsideRect(bb2[0], bb2[1]) && bb1[1].InsideRect(bb2[0], bb2[1]))
                || (bb2[0].InsideRect(bb1[0], bb1[1]) && bb2[1].InsideRect(bb1[0], bb1[1]))))
                b = false;

            //ACD.WR("[{0},{1}],[{2}],{3}]", bb1[0], bb1[1], bb2[0], bb2[1]);

            if (bb1[0] != null && bb1[1] != null && bb2[0] != null && bb2[1] != null)
            {
                b = true;
                for (int i = 0; i < 3; i++)
                    b &= !(bb1[1][i] < bb2[0][i] || bb2[1][i] < bb1[0][i]);
            }

            if (!b)
                b = bb1.Rect().IntersectPts(bb2.Rect()).Length > 1;
            return b;
        }

        static public pPos[] MidPts(this IEnumerable<pPos> lsPoint)
        {
            List<pPos> lsRes = new List<pPos>();
            for (int i = 0; i < lsPoint.Count(); i++)
            {
                int nex = (i + 1) % lsPoint.Count();
                lsRes.Add(new pPos((lsPoint.ElementAt(i).X + lsPoint.ElementAt(nex).X) / 2,
                    (lsPoint.ElementAt(i).Y + lsPoint.ElementAt(nex).Y) / 2));
            }

            return lsRes.ToArray();
        }

        static string[] filter(string st, params char[] keys)
        {
            return (from ch in st.Split(keys) where ch != "" select ch).ToArray();
        }

        public static pPos Centroid(this IEnumerable<pPos> lsPoint)
        {
            double X = 0;
            double Y = 0;
            double second_factor;
            var pts = lsPoint;

            for (int i = 0; i < lsPoint.Count(); i++)
            {
                int a = i;
                int b = (i + 1) % lsPoint.Count();
                if (b >= lsPoint.Count())
                    b = 0;
                second_factor = pts.ElementAt(a).X * pts.ElementAt(b).Y - pts.ElementAt(b).X * pts.ElementAt(a).Y;
                X += (pts.ElementAt(a).X + pts.ElementAt(b).X) * second_factor;
                Y += (pts.ElementAt(a).Y + pts.ElementAt(b).Y) * second_factor;
            }

            // Divide by 6 times the polygon's area.
            double polygon_area = lsPoint.Area();
            X /= (6 * polygon_area);
            Y /= (6 * polygon_area);

            // If the values are negative, the polygon is
            // oriented counterclockwise so reverse the signs.
            if (X < 0)
            {
                X = -X;
                Y = -Y;
            }

            return new pPos(X, Y);
        }

        /*public static pPos CentroidPoint(this IEnumerable<pPos> pls)
        {
            List<pPos> midpoints = pls.MidPts().ToList();
            midpoints.AddRange(pls);
            List<pPos> lsres = new List<pPos>();

            List<double> lsX = new List<double>(), lsY = new List<double>();
            foreach (pPos pt in midpoints)
            {
                lsX.Add(pt.X);
                lsY.Add(pt.Y);
            }

            lsX = lsX.OrderBy(v => v).ToList();
            lsY = lsY.OrderBy(v => v).ToList();

            for (int i = 0; i < lsX.Count; i++)
                for (int j = 0; j < lsY.Count; j++)
                {
                    pPos pt = new pPos(lsX[i], lsY[j]);
                    if (pt.Inside(pls))
                        lsres.Add(pt);
                }

            pPos ct = pls.Centroid();
            pPos res = null;
            double min_v = double.PositiveInfinity;

            foreach (pPos pt in lsres)
            {
                double n = pt.DistanceTo(res);
                if (n < min_v)
                {
                    min_v = n;
                    res = pt;
                }
            }

            return res;
        }*/

        public static pPos MinPoint(this IEnumerable<pPos> lsPoint)
        {
            if (lsPoint != null && lsPoint.Count() > 0)
                return new pPos(lsPoint.Min(pt => pt.X), lsPoint.Min(pt => pt.Y), lsPoint.Min(pt => pt.Z));
            else
                return null;
        }

        public static pPos MaxPoint(this IEnumerable<pPos> lsPoint)
        {
            if (lsPoint.Count() > 0)
                return new pPos(lsPoint.Max(pt => pt.X), lsPoint.Max(pt => pt.Y), lsPoint.Max(pt => pt.Z));
            else
                return null;
        }

        public static PosCollection Explode(this IEnumerable<pPos> pts, double extent_first_end = 0, bool closed = false)
        {
            if (extent_first_end != 0)
                pts = pts.ExtentLine(extent_first_end);
            PosCollection res = new PosCollection();

            for (int i = 0; i < pts.Count() - 1; i++)
                res.Add(new pPos[] { pts.ElementAt(i), pts.ElementAt(i + 1) });

            if (closed)
                res.Add(new pPos[] { pts.Last(), pts.First() });
            return res;
        }

        //public static string ExportString(this IEnumerable<pPos> lsP, string key = null)
        //{
        //    string st = key != null && key != "" ? "(" + key + ")" : "";
        //    for (int i = 0; i < lsP.Count(); i++)
        //    {
        //        st += lsP.ElementAt(i).ToString();
        //        if (i != lsP.Count() - 1) st += "|";
        //    }

        //    return st;
        //}

        //public static pPos[] ParseString(string txt)
        //{
        //    string[] ar;
        //    if (txt.Contains("(") || txt.Contains(")"))
        //    {
        //        ar = filter(txt, '(', ')');
        //        txt = ar[1];
        //    }
        //    List<pPos> lsRes = new List<pPos>();
        //    ar = filter(txt, '|');

        //    foreach (string st in ar)
        //        if (st != "")
        //        {
        //            string[] arSub = filter(st, ',');
        //            lsRes.Add(new pPos(Convert.ToDouble(arSub[0]), Convert.ToDouble(arSub[1])));
        //        }

        //    return lsRes.ToArray();
        //}

        public static bool ibScreenangle(this IEnumerable<pPos> pls)
        {
            if (pls.Count() == 4)
            {
                double d1 = pls.ElementAt(0).DistanceTo(pls.ElementAt(1));
                double d2 = pls.ElementAt(2).DistanceTo(pls.ElementAt(3));

                if (Math.Abs(d1 - d2) < 0.1)
                {
                    d1 = pls.ElementAt(0).DistanceTo(pls.ElementAt(3));
                    d2 = pls.ElementAt(1).DistanceTo(pls.ElementAt(2));

                    if (Math.Abs(d1 - d2) < 0.1)
                    {
                        d1 = pls.ElementAt(0).DistanceTo(pls.ElementAt(2));
                        d2 = pls.ElementAt(1).DistanceTo(pls.ElementAt(3));

                        return (Math.Abs(d1 - d2) < 0.1);
                    }
                }
            }
            return false;
        }

        public static pPos[] SelfIntersect(this IEnumerable<pPos> pts)
        {
            List<pPos> res = new List<pPos>();
            PosCollection line_pls = pts.Explode();

            foreach (pPos[] lines in line_pls)
            {
                pPos[] exts = pts.Intersect(lines[0], lines[1])
                    .Where(pt => pt.Inside(pts) &&
                    pts.All(p2 => p2.DistanceTo(pt) > 10))
                    .Select(pt => pt).ToArray();
                res.AddRange(exts);
            }

            return res.ToArray();
        }

        public static pPos[] ExtentLine(this IEnumerable<pPos> pls, double extent)
        {
            pPos[] res = (from pt in pls select pt).ToArray();

            if (res.First().DistanceTo(res.Last()) > 1)
            {
                pPos p1 = res[pls.Count() - 2];
                pPos p2 = res[pls.Count() - 1];
                res[pls.Count() - 1] = p1.AlongRatio(p2, 1 + extent / p1.DistanceTo(p2));

                p1 = pls.ElementAt(1);
                p2 = pls.ElementAt(0);
                res[0] = p1.AlongRatio(p2, 1 + extent / p1.DistanceTo(p2));
            }
            return res;
        }


        /*public static pPos[] DividePolyline(this IEnumerable<pPos> pts, double range)
        {
            List<pPos> res = new List<pPos>();
            double n = 0;
            for (int i = 0; i < pts.Count(); i++)
            {
                pPos p1 = pts.ElementAt(i);
                pPos p2 = pts.ElementAt((i + 1) % pts.Count());
                double d = p1.DistanceTo(p2);
                if (n + d > range)
                {
                    res.Add(p1.Along(range - n, p2));
                    n = range - n;
                }
                else
                    n += d;
            }
            return res.ToArray();
        }*/

        public static pPos[] DivideEllipse(double a, double b, pPos ct, double range)
        {
            double perm = EllipsePerimeter(a, b) / 4;
            int divs = (int)Math.Ceiling(perm / range);

            pPos[] segs = EllipseSegments(a, b, ct, 200);
            List<pPos> pts = new List<pPos>();
            for (int i = 0; i < segs.Length / 4; i++)
                pts.Add(segs[i]);

            range = perm / divs;
            List<pPos> res = pts.Divide(range, false).ToList();

            pPos[] basepoints = new pPos[] {new pPos(ct.X, ct.Y - b), new pPos(ct.X, ct.Y + b) ,
                new pPos(ct.X - a, ct.Y) , new pPos(ct.X + a, ct.Y) };

            for (int i = 0; i < res.Count; i++)
                foreach (pPos basept in basepoints)
                    if (basept.DistanceTo(res[i]) <= range / 2)
                        res[i] = basept;

            res.AddRange(res.Mirror(basepoints[0], basepoints[1]));
            res.AddRange(res.Mirror(basepoints[2], basepoints[3]));
            //Console.Write("Perm {0} Divs {1} Segs {2} Res {3} Range {3}\r\n", 
            //perm, divs, segs.Length, res.Count, range);
            return res.ToArray();
        }

        public static pPos[] DivideOpponent(this IEnumerable<pPos> ls, double dist, bool closed)
        {
            List<pPos> res = new List<pPos>();
            pPos[] r = ls.RectByDirection(ls.ElementAt(1) - ls.ElementAt(0));

            for (int i = 0; i < 2; i++)
            {
                //pPos v = r.ElementAt((i + 2) % ls.Count()) - ls.ElementAt(i + 1) ;
                pPos[] div = new pPos[] { r[i], r[i + 1] }.Divide(dist, closed);
                //res.AddRange(div);

                foreach (pPos p in div)
                {
                    pPos p1 = p + r[i + 2] - r[i + 1];
                    res.AddRange(ls.Intersect(p, p1));
                }
            }

            return res.ToArray();
        }

        public static pPos[] Divide(this IEnumerable<pPos> ls, double dist, bool closed)
        {
            double polyLineLength = ls.Length(closed);
            int numDist = (int)Math.Floor(polyLineLength / dist);

            List<pPos> res = new List<pPos> { };
            double pointPosition = 0.0;
            double prevSegmentsLength = 0.0;
            double segmentsLength = 0.0;
            int current = 0, nex = 0, prev;

            for (int i = 0; i <= numDist; i++)
            {
                while (pointPosition >= segmentsLength && nex != -1)
                {
                    prevSegmentsLength = segmentsLength;

                    nex = (current + 1) % ls.Count();

                    if (nex >= ls.Count() && closed)
                        nex = closed ? 0 : -1;

                    if (nex != -1)
                    {
                        segmentsLength += ls.ElementAt(current).DistanceTo(ls.ElementAt(nex));
                        current = nex;
                    }
                }

                prev = (current - 1 + ls.Count()) % ls.Count();

                if (prev < 0)
                    prev = closed ? ls.Count() - 1 : -1;

                if (prev != -1)
                {
                    var pt = ls.ElementAt(prev).AlongRatio(ls.ElementAt(current),
                        (pointPosition - prevSegmentsLength) 
                        / ls.ElementAt(prev).DistanceToPoint(ls.ElementAt(current)));
                    res.Add(pt);
                    pointPosition += dist;
                }
                else
                    break;
            }

            return res.ToArray();
        }

        public static pPos[] _generateBasePointFromMidPoint(this IEnumerable<pPos> pls)
        {
            pPos[] midpoints = pls.MidPts();
            List<pPos> res = new List<pPos>();

            for (int i = 0; i < midpoints.Length; i++)
                for (int j = 0; j < midpoints.Length; j++)
                    if (i != j)
                        res.Add(new pPos(midpoints[i].X, midpoints[j].Y));

            return (res.ToArray());
        }

        static PosCollection _gridToMullionCShape(pPos[] points, pPos sz)
        {
            PosCollection res = new PosCollection();

            for (int i = 0; i < points.Length; i++)
                if (i == 0)
                    res.Add(points[0].DrawCSection(points[0].Along(sz.X, points[1]), sz));
                else if (i == points.Length - 1)
                    res.Add(points.Last().Along(sz.X, points[points.Length - 2]).DrawCSection(points.Last(), sz));
                else
                    res.Add(points[i].Along(sz.X / 2, points.First()).DrawCSection(points[i].Along(sz.X / 2, points.Last()), sz));

            return res;
        }

        public static PosCollection CSectionList(this IEnumerable<pPos> points, pPos sz)
        {
            PosCollection res = new PosCollection();

            for (int i = 0; i < points.Count(); i++)
                if (i == 0)
                    res.Add(points.First().DrawCSection(points.First().Along(sz.X, points.ElementAt(1)), sz));
                else if (i == points.Count() - 1)
                    res.Add(points.Last().Along(sz.X, 
                        points.ElementAt(points.Count() - 2)).DrawCSection(points.Last(), sz));
                else
                    res.Add(points.ElementAt(i).Along(sz.X / 2, 
                        points.First()).DrawCSection(points.ElementAt(i).Along(sz.X / 2, points.Last()), sz));

            return res;
        }

        public static pPos GetCenterPoint(this IEnumerable<pPos> lsPoint)
        {
            pPos[] bb = lsPoint.Boundary();
            return (bb[0] + bb[1]) / 2;
        }

        public static pPos GetCentroidInside(this IEnumerable<pPos> lsPoint)
        {
            pPos res = null;
            int total = lsPoint.Count();
            pPos[] inside = lsPoint.Offset(-200);

            for (int i = 0; i < total; i++)
                if (res == null)
                {
                    int nex = (i + 1) % total;
                    int prev = (i - 1) % total;

                    for (int j = 0; j < total; j++)
                        if (res == null && i != j && nex != j && prev != j)
                        {
                            pPos pt = (lsPoint.ElementAt(i) + lsPoint.ElementAt(j)) / 2;
                            if (pt.Inside(inside))
                                res = pt;
                        }
                }

            return res == null ? lsPoint.GetCenterPoint() : res;
        }

        public static void BreakPolyline(this pPos[] pls)
        {
            if (pls.isClosed())
            {
                pls[pls.Length - 1] = pls[pls.Length - 2].AlongRatio(pls[pls.Length - 1], 0.9);
            }
        }

        public static pPos[] Boundary(this IEnumerable<pPos> lsRegion)
        {
            pPos p1 = lsRegion.MinPoint();
            pPos p2 = lsRegion.MaxPoint();
            return p1 != null && p2 != null ? new pPos[] { p1, p2 } : null;
        }

        public static double[] PtsRatio(this IEnumerable<pPos> pts)
        {
            List<double> res = new List<double>();
            double d = pts.First().DistanceTo(pts.Last());

            for (int i = 1; i < pts.Count() - 1; i++)
            {
                pPos pt = pts.ElementAt(i);
                pPos prj = pt.ProjectLine(pts.First(), pts.Last());
                double v = prj.openedPathParam(new pPos[] { pts.First(), pts.Last() });

                res.Add(v);
            }

            return res.OrderBy(v => v).ToArray();
        }

        public static pPos Size(this IEnumerable<pPos> pts)
        {
            pPos[] bb = pts.Boundary();
            return (bb.ElementAt(1) - bb.ElementAt(0)).Abs;
        }

        public static double AreaPls(this PosCollection pls)
        {
            double area = 0;

            if (pls.Count > 1)
            {
                PosCollection res = pls.OrderBy(ls => ls.Area()).ToCollectionSameClosed();
                List<List<int>> combs = new List<List<int>>();

                for (int i = 0; i < res.Count - 1; i++)
                    for (int j = i + 1; j < res.Count; j++)
                        if (res[i].All(pt => pt.Inside(res[j])))
                        {
                            int n = combs.FindIndex(itm => itm.Contains(i));
                            if (n == -1)
                                combs.Add(new List<int> { i, j });
                            else
                                combs[n].Add(j);
                        }

                for (int i = 0; i < combs.Count; i++)
                {
                    double max_inside_area = 0;
                    combs[i] = combs[i].OrderBy(v => v).Reverse().ToList();

                    //area += pPos.Area(res[combs[i][0]]);

                    for (int j = 1; j < combs[i].Count; j++)
                        if (res[combs[i][j]] != null)
                        {
                            double n = res[combs[i][j]].Area();
                            if (n > max_inside_area)
                                max_inside_area = n;
                            res[combs[i][j]] = null;
                        }

                    area -= max_inside_area;
                }

                //db.WR("AREA_MAX {0}\n", area);

                for (int i = 0; i < res.Count; i++)
                    if (res[i] != null)
                        area += res[i].Area();
            }
            else
                area = pls.First.Area();

            return area;
        }

        public static pPos[] RectByDirection(this IEnumerable<pPos> pts, pPos direction)
        {
            pPos ct = pts.CenterPoint();
            IEnumerable<pPos> prjX = pts.Cast<pPos>().Select(pt => pt.ProjectLine(ct, ct + direction))
                .OrderBy(pt => pt.X).ThenBy(pt => pt.Y);

            pPos pA = prjX.First(), pB = prjX.Last();

            pPos vt = new pPos(-direction.Y, direction.X);
            IEnumerable<pPos> prjY = pts.Cast<pPos>().Select(pt => pt.ProjectLine(ct, ct + vt))
                .OrderBy(pt => pt.X).ThenBy(pt => pt.Y).ToArray();

            pPos pC = prjY.First(), pD = prjY.Last();

            pPos[] e1 = new pPos[] { pD, pD + direction }, e2 = new pPos[] { pB, pB + vt },
                e3 = new pPos[] { pC, pC + direction }, e4 = new pPos[] { pA, pA + vt };

            return new pPos[] { e4[0].Intersect(e4[1], e1[0], e1[1], false),
                e1[0].Intersect(e1[1], e2[0], e2[1], false),
                e2[0].Intersect(e2[1], e3[0], e3[1], false),
                e3[0].Intersect(e3[1], e4[0], e4[1], false)};
        }

        public static PosCollection PolylineViews(this IEnumerable<pPos> pts, double height)
        {
            PosCollection res = new PosCollection();
            pPos[] r = pts.Rect();

            PosCollection segs = pts.GetSegment();
            PosCollection views = r.GetSegment();

            //for (int i = 2; i < 4; i++)
            //    views[i] = views[i].Reverse();

            double nx = 0;
            for (int i = 0; i < 4; i++)
            {
                int nex = (i + 1) % 4;

                res.Add(new pPos(nx, 0)
                    .RectToPoint(new pPos(nx + r[nex].DistanceTo(r[i]), height)));

                nx += Math.Abs(r[nex].DistanceTo(r[i]));
            }

            foreach (pPos[] seg in segs)
            {
                pPos ct = seg.CenterPoint();
                int index = NumericArray(0, views.Count - 1).OrderBy(n => ct.DistanceToPts(views[n])).ToArray().First();

                double[] range = seg.Select(p => p.ProjectLine(views[index][0], views[index][1])
                    .DistanceTo(views[index][0])).ToArray();

                if (Math.Abs(range[1] - range[0]) > 0)
                {
                    nx = 0;

                    for (int i = 0; i < index; i++)
                        nx += Math.Abs(views[i][1].DistanceTo(views[i][0]));

                    res.Add(new pPos(nx + range[0], 0).RectToPoint(new pPos(nx + range[1], height)));
                }
            }

            return res;
        }

        public static pPos[] Rect(this IEnumerable<pPos> pts, double offset = 0)
        {
            if (pts == null)
                return null;
            pPos[] bb = pts.Boundary();
            pPos[] rect = new pPos[]{  new pPos(bb.ElementAt(0).X - offset,bb.ElementAt(0).Y - offset),
                                new pPos(bb.ElementAt(1).X + offset, bb.ElementAt(0).Y - offset),
                                new pPos(bb.ElementAt(1).X + offset,bb.ElementAt(1).Y + offset),
                                new pPos(bb.ElementAt(0).X - offset, bb.ElementAt(1).Y + offset)};

            return rect;
        }

        public static bool isClosed(this IEnumerable<pPos> ls, double tolerance = 1)
        {
            return ls.First().DistanceTo(ls.Last()) < tolerance;
        }

        public static double Area(this IEnumerable<pPos> lsPoint)
        {
            double area = 0;

            for (int i = 0; i < lsPoint.Count(); i++)
            {
                int nex = i < lsPoint.Count() - 1 ? i + 1 : 0;
                area += (lsPoint.ElementAt(nex).X - lsPoint.ElementAt(i).X) * (lsPoint.ElementAt(nex).Y + lsPoint.ElementAt(i).Y) / 2;
            }
            return Math.Abs(area);
        }

        public static bool Inside(this IEnumerable<pPos> ls, IEnumerable<pPos> lsParent)
        {
            bool res = false;
            if (ls != null && lsParent != null)
                res = ls.All(pt => pt.Inside(lsParent));
            return res;
        }

        public static bool Inside(this IEnumerable<pPos> pts, pPos bb1, pPos bb2)
        {
            return pts.All(pt => pt.InsideRect(bb1, bb2));
        }

        public static pPos[] SetStartIndex(this IEnumerable<pPos> pts, int startIndex)
        {
            int total = pts.Count();
            pPos[] res = new pPos[total];
            for (int i = 0; i < total; i++)
                res[i] = pts.ElementAt((i + startIndex) % total);
            return res;
        }



        //public static xLine[] ToXline(this IEnumerable<pPos> pts, IEnumerable<double> corner_offsets = null)
        //{
        //    xLine[] Xlines = new xLine[pts.Count() - 1];

        //    for (int i = 0; i < Xlines.Length; i++)
        //    {
        //        pPos pt = pts.ElementAt(i);

        //        if (i > 0 && corner_offsets != null && corner_offsets.Count() > i - 1)
        //        {
        //            double prop = Math.Abs(corner_offsets.ElementAt(i - 1)) / pts.ElementAt(i).DistanceTo(pts.ElementAt(i + 1));
        //            pt = pt.Along(pts.ElementAt(i + 1), prop);
        //        }
        //        Xlines[i] = new xLine(pt, pts.ElementAt(i + 1));

        //        if(corner_offsets != null && corner_offsets.Count() > i)
        //            Xlines[i].offset = corner_offsets.ElementAt(i);
        //    }

        //    return Xlines;
        //}

        public static pPos[] RectToRoom(this IEnumerable<pPos> rect, bool rotated = false)
        {
            pPos[] res = null;

            double w = rect.ElementAt(0).DistanceTo(rect.ElementAt(1));
            double h = rect.ElementAt(1).DistanceTo(rect.ElementAt(2));

            if (rotated && h < w)
            {
                pPos p1 = rect.ElementAt(1).Along(h / 4, rect.ElementAt(2));
                pPos p2 = rect.ElementAt(2).Along(h / 4, rect.ElementAt(1));
                res = new pPos[] {p2, rect.ElementAt(2),
                rect.ElementAt(3), rect.ElementAt(0), rect.ElementAt(1), p1 };
            }
            else
            {
                pPos p1 = rect.ElementAt(0).Along(w / 4, rect.ElementAt(1));
                pPos p2 = rect.ElementAt(1).Along(w / 4, rect.ElementAt(0));
                res = new pPos[] {p1, rect.ElementAt(0),
                rect.ElementAt(3), rect.ElementAt(2), rect.ElementAt(1), p2 };
            }
            return res;
        }

        public static pPos[] RectToLayout(this IEnumerable<pPos> r)
        {
            pPos[] rect = r.Boundary().Rect().SetStartIndex(3);
            pPos[] res = null;

            double w = rect.ElementAt(0).DistanceTo(rect.ElementAt(1));
            double h = rect.ElementAt(1).DistanceTo(rect.ElementAt(2));

            pPos p1 = rect.First().Along(-h / 4, rect.ElementAt(3));
            pPos p2 = rect.First().Along(-h / 4, rect.ElementAt(1));

            return new pPos[] { p2, rect.ElementAt(1), rect.ElementAt(2), rect.Last(), p1 };
        }

        public static pPos[] LayoutToRect(this IEnumerable<pPos> r)
        {
            return new pPos[] { r.ElementAt(0).Intersect(r.ElementAt(1), r.ElementAt(3), r.ElementAt(4)),
                r.ElementAt(1), r.ElementAt(2), r.ElementAt(3) };
        }

        public static pPos[] ToRoom(this IEnumerable<pPos> pts, int index = 0)
        {
            List<pPos> res = pts.SetStartIndex(index).ToList();
            int nex = (index + 1) % pts.Count();
            pPos p1 = res[0].AlongRatio(res.Last(), 1 / 3);
            pPos p2 = res.Last().AlongRatio(res[0], 1 / 3);
            res.Insert(0, p1);
            res.Insert(res.Count, p2);
            return res.ToArray();
        }

        public static string ToText(this IEnumerable<pPos> pts, bool closed = true, char seperator = ';')
        {
            string res = "";
            foreach (pPos pt in pts)
                res += pt.X.roundNumber(0.01) + "," + pt.Y.roundNumber(0.01)
                    + (pt.Z != 0 ? "," + pt.Z.roundNumber(0.01) : "")
                    + (pt.Rotation != 0 ? "<" + pt.Rotation : "")
                    + (!pt.Content.empty() ? "[" + pt.Content + "]" : "")
                    + seperator;

            if (res.EndsWith(seperator.ToString()))
                res = res.Substring(0, res.Length - 1);

            return closed ? "(" + res + ")" : res;
        }

        //public static PosCollection ToRoom(this IEnumerable<pPos> pts, IEnumerable<pPos> points,
        //    IEnumerable<double> sizes, IEnumerable<double> offsets)
        //{
        //    xLine[] Xlines = pts.ToXline(offsets);
        //    pPos[] bb = pts.Boundary();

        //    for (int i = 0; i < points.Count(); i++)
        //    {
        //        points.ElementAt(i).DistanceTo(pts);
        //        xLine xl = Xlines[pPos.DistanceTo_Index];

        //        pPos pB = xl.StartPoint;
        //        pPos pA = pPos.DistanceTo_Projection;

        //        double prop = sizes.ElementAt(i) / (2 * pA.DistanceTo(pB));
        //        xl.addPoint(pA.Along(pB, prop), pA.Along(pB, -prop));
        //    }

        //    PosCollection res = new PosCollection();
        //    for (int index = 0; index < Xlines.Length; index++)
        //    {
        //        PosCollection pls = Xlines[index].RoomList(Xlines[index].StartPoint, Xlines[index].EndPoint);

        //        if (pls.Count > 0)
        //        {
        //            if (index < Xlines.Length - 1)
        //            {
        //                pPos[] ls = pls[pls.Count - 1];
        //                ls[ls.Length - 1] = ls[ls.Length - 2].Along(ls[ls.Length - 1], 
        //                    Math.Abs(offsets.ElementAt(index + 1)) / ls[ls.Length - 2].DistanceTo(ls[ls.Length - 1]));
        //            }
        //            res.AddRange(pls);
        //        }
        //    }

        //    return res;
        //}

        static int _numberSize(this pPos[] bb, int axis, double size)
        {
            int n = (int)Math.Ceiling((bb[1][axis] - bb[0][axis]) / size);
            double m = (bb[1][axis] - bb[0][axis]) / n;

            if (m < size - 200)
                n--;

            //ACD.WR("N = {0} M = {1}", n, m);
            return n;
        }

        public static pPos[] CellDivide(this IEnumerable<pPos> region, double size)
        {
            pPos[] bb = region.Boundary();

            //ACD.WR("CELL_SIZE {0} BB {1}; {2}", size, bb[0], bb[1]);

            List<pPos> res = bb.Rect().Cast<pPos>().Where(pt => pt.Inside(region.Offset(100))).Select(pt => pt).ToList();
            int nX = bb._numberSize(0, size);
            int nY = bb._numberSize(1, size);

            //ACD.WR("CELL_NX_NY {0}, {1}", nX, nY);

            if (nX > 100)
            {
                nX = 100;
                //ACD.WR("CellDivide Overside Error");
            }

            if (nY > 100)
            {
                nY = 100;
                //ACD.WR("CellDivide Overside Error");
            }

            double v = bb[0].X;
            for (int i = 0; i < nX; i++)
            {
                pPos[] pts = region.Intersect(new pPos(v, bb[0].Y), new pPos(v, bb[1].Y));

                foreach (pPos pt in pts)
                    if (res.All(p => p.DistanceTo(pt) > 100))
                        res.Add(pt);
                v += (bb[1].X - bb[0].X) / nX;
            }

            //ACD.WR("CELL_REGION {0}", region.Count());

            v = bb[0].Y;
            for (int i = 0; i < nY; i++)
            {
                pPos[] pts = region.Intersect(new pPos(bb[0].X, v), new pPos(bb[1].X, v));
                foreach (pPos pt in pts)
                    if (res.All(p => p.DistanceTo(pt) > 100))
                        res.Add(pt);
                v += (bb[1].Y - bb[0].Y) / nY;
            }

            //ACD.WR("CELL_RES {0}", res.Count);

            return res.ToArray();
        }

        //public static pPos[] Trim(this IEnumerable<pPos> ls1, IEnumerable<pPos> ls2)
        //{
        //    pPos[] inters = ls1.Intersect(ls2);
        //    List<pPos> res = new List<pPos>();

        //    if(inters.Length >= 2)
        //    {
        //        int[] T = new int[ls1.Count() + inters.Length];
        //        int[] indexes = new int[inters.Length];
        //        for (int i = 0; i < indexes.Length; i++)
        //        {
        //            inters[i].DistanceTo(ls1);
        //            indexes[i] = pPos.DistanceTo_Index;
        //        }

        //        int inf = inters[indexes[0] + 1].Inside(ls2) ? -1: 1;

        //        int index = indexes[0];
        //        res.Add(inters[0]);

        //        while(index != indexes[1])
        //        {
        //            res.Add(ls1.ElementAt(index));
        //            index += inf;
        //        }

        //        res.Add(inters[1]);
        //    }

        //    return res.Weld(10);
        //}

        public static List<int> Intersect_SegmentIndexes;

        public static pPos[] Intersect(this IEnumerable<pPos> ls, pPos line1, pPos line2, bool segment = false)
        {
            List<pPos> res = new List<pPos>();
            Intersect_SegmentIndexes = new List<int>();

            for (int i = 0; i < ls.Count() - 1; i++)
            {
                int nex = (i + 1) % ls.Count();
                SegIntersect.CalcIntersection(0, ls.ElementAt(i), ls.ElementAt(nex), line1, line2);

                if (!segment || SegIntersect.SegmentIntersect)
                {
                    if (SegIntersect.LineIntersect && SegIntersect.Intersection.IsBetween(ls.ElementAt(i), ls.ElementAt(nex)))
                    {
                        Intersect_SegmentIndexes.Add(i);
                        res.Add(SegIntersect.Intersection);
                    }
                }
            }

            return res.OrderBy(pt => pt.X).ThenBy(pt => pt.Y).ToArray();
        }

        public static pPos[] IntersectPts(this IEnumerable<pPos> ls1, IEnumerable<pPos> ls2,
            bool _isPolyline1Closed = true, bool _isPolyline2Closed = true, bool just_need_once = true)
        {
            List<pPos> res = new List<pPos>();
            int total1 = _isPolyline1Closed ? ls1.Count() : ls1.Count() - 1;
            int total2 = _isPolyline2Closed ? ls2.Count() : ls2.Count() - 1;

            for (int i = 0; i < total1; i++)
            {
                int nexi = (i + 1) % ls1.Count();

                for (int j = 0; j < total2; j++)
                {
                    int nexj = (j + 1) % ls2.Count();
                    pPos pt = ls1.ElementAt(i).Intersect(ls1.ElementAt(nexi), ls2.ElementAt(j), ls2.ElementAt(nexj));
                    if (pt != null)
                    {
                        if (just_need_once)
                            return new pPos[] { pt };
                        res.Add(pt);
                    }
                }
            }
            return res.ToArray();
        }

        //public static bool closed ;

        public static bool IsModifyClosed(this string st)
        {
            double rect_offset = st._prop("RECT").ToNumber(double.NaN);
            //double offset = st._prop("OFFSET").ToNumber();
            double loffset = st._prop("LOFFSET").ToNumber();
            double parrallel = st._prop("PARALLEL").ToNumber();
            //double depth = st._prop("DEPTH").ToNumber();
            //pPos move_pt = new pPos(st._prop("MOVE"));

            return !double.IsNaN(rect_offset) || parrallel != 0 || loffset != 0;
        }

        public static pPos[] ModifyPts(this IEnumerable<pPos> pts, string st)
        {
            pPos[] res = pts.ToArray();
            pPos[] bb = pts.Boundary();
            string key = st._firstPropName();
            string val = st._firstProp();

            //Dictionary<string, string> dicts = new Dictionary<string, string>();
            //dicts.Add("W", st._prop("W"));
            //dicts.Add("H", st._prop("H"));
            //dicts.Add("D", st._prop("D"));

            double rect_offset = st._prop("RECT").ToNumber(double.NaN);
            double offset = st._prop("OFFSET").ToNumber();
            double loffset = st._prop("LOFFSET").ToNumber();
            double parrallel = st._prop("PARALLEL").ToNumber();
            double depth = st._prop("DEPTH").ToNumber();
            pPos move_pt = pPos.FromString(st._prop("MOVE"));

            //closed = false;

            if (!double.IsNaN(rect_offset))
            {
                Console.WriteLine("[{0},{1}]", res.First(), res.Last());
                res = res.First().RectToPoint(res.Last(), rect_offset);
                //closed = true;
            }

            res = pts.Offset(offset).Move(move_pt);

            if (parrallel != 0)
            {
                res = new pPos(bb[0].X, bb[1].Y).Parallel(bb[1], parrallel);
                if (depth > 0)
                    res = res[0].RectToPoint(new pPos(res[1].X, res[1].Y - depth));

                //closed = true;
            }

            if (loffset != 0)
            {
                res = res.LOffset(loffset);
                //closed = true;
            }

            return res;
        }

        public static double ValueByKey(this IEnumerable<pPos> pls, string key)
        {
            double res = 0;
            pPos[] r = pls.Rect();

            switch (key)
            {
                case "R0X":
                    res = r[0].X.roundNumber();
                    break;
                case "R0Y":
                    res = r[0].Y.roundNumber();
                    break;
                case "R1X":
                    res = r[1].X.roundNumber();
                    break;
                case "R1Y":
                    res = r[1].Y.roundNumber();
                    break;
                case "R2X":
                    res = r[2].X.roundNumber();
                    break;
                case "R2Y":
                    res = r[2].Y.roundNumber();
                    break;
                case "R3X":
                    res = r[3].X.roundNumber();
                    break;
                case "R3Y":
                    res = r[3].Y.roundNumber();
                    break;
                case "W":
                    res = Math.Abs(r[0].X - r[2].X).roundNumber();
                    break;
                case "H":
                    res = Math.Abs(r[0].Y - r[2].Y).roundNumber();
                    break;
            }

            return res;
        }

        public static pPos[] ProjectOn(this IEnumerable<pPos> ls, pPos prj1, pPos prj2)
        {
            pPos[] res = ls.Select(p => p.ProjectLine(prj1, prj2)).OrderBy(p => p.DistanceTo(prj1)).ToArray();
            return res.Boundary();
        }

        public static pPos[] NearestProjectOn(this IEnumerable<pPos> ls, pPos prj1, pPos prj2, bool _isLeft)
        {
            pPos[] pts = ls.Where(p => p.isLeft(prj1, prj2) == _isLeft
                && p.ProjectLine(prj1, prj2).IsBetween(prj1, prj2))
                .Select(p => p).OrderBy(p => p.ProjectLine(prj1, prj2).DistanceTo(prj1)).ToArray();

            bool[] T = pts.Select(p => false).ToArray();

            List<pPos> res = new List<pPos>();

            for (int i = 0; i < pts.Length; i++)
                if (!T[i])
                {
                    T[i] = true;
                    double d = pts[i].ProjectLine(prj1, prj2).DistanceTo(prj1).roundNumber(50);

                    int[] indexes = NumericArray(i, pts.Length - 1)
                        .Where(n => !T[n] && pts[n].ProjectLine(prj1, prj2).DistanceTo(prj1).roundNumber(50) == d)
                        .Select(n => n).OrderBy(n => pts[n].DistanceTo(prj1, prj2)).ToArray();

                    foreach (int n in indexes)
                        T[n] = true;

                    res.Add(pts.ElementAt(indexes.First()));
                }

            return res.Select(p => p.ProjectLine(prj1, prj2)).OrderBy(p => p.DistanceTo(prj1)).ToArray();
        }

        public static List<double[]> ExtractPtsXY(this IEnumerable<pPos> pts, double round = 100, double minvalue = 0)
        {
            List<double[]> res = new List<double[]> { null, null };
            double cur_v = double.NegativeInfinity;
            //List<pPos> pts = new List<pPos>();

            for (int axis = 0; axis < 2; axis++)
            {
                List<double> ls = new List<double>();

                foreach (pPos p in pts)
                    if (ls.All(v => Math.Abs(v - p[axis]) > round))
                        ls.Add(p[axis]);

                ls = ls.OrderBy(v => v).ToList();

                //int nex = (axis + 1) % 2;
                List<double> new_pts = new List<double>();

                for (int i = 0; i < ls.Count; i++)
                    if (i == 0 || i == ls.Count - 1
                        || (ls.Last() - ls[i] > minvalue && ls[i] - cur_v > minvalue))
                    {
                        new_pts.Add(ls[i]);
                        cur_v = ls[i];
                    }

                res[axis] = new_pts.ToArray();
            }

            return res;
        }

        public static pPos[] OffsetByListValue(this IEnumerable<pPos> pls, IEnumerable<double> dists)
        {
            if (pls == null || pls.Count() == 0)
                return null;

            List<pPos> res = new List<pPos>();

            pPos[] lsPoint = pls.ToArray();
            PosCollection lsSeg = new PosCollection();

            int total = pls.Count();

            for (int i = 0; i < total; i++)
            {
                int nex = (i + 1) % total;
                double v_out = dists.ElementAt(i);
                pPos[] ar = lsPoint.ElementAt(i).Parallel(lsPoint.ElementAt(nex), v_out);

                pPos mid = new pPos((ar[0].X + ar[1].X) / 2, (ar[0].Y + ar[1].Y) / 2);

                bool b = mid.Inside(lsPoint);

                if (((!b) && v_out < 0) || (b && v_out > 0))
                    ar = lsPoint.ElementAt(i).Parallel(lsPoint.ElementAt(nex), -v_out);

                lsSeg.Add(ar);
            }

            //ACD.WR("Segments {0}", lsSeg.Count);

            bool[] can_extents = new bool[4];
            for (int i = 0; i < 4; i++)
                can_extents[i] = true;

            SegIntersect.can_extents = can_extents;

            for (int i = 0; i < lsSeg.Count; i++)
            {
                double v_out = dists.ElementAt(i);
                int nex = i + 1;
                if (i >= lsSeg.Count - 1)
                    nex = 0;

                if (nex != -1)
                {
                    SegIntersect.CalcIntersection(v_out, lsSeg[i][0], lsSeg[i][1], lsSeg[nex][0], lsSeg[nex][1]);

                    if (SegIntersect.Intersection != null)
                        res.Add(SegIntersect.Intersection);
                }
            }

            //if (lastpoint != null)
            //res.Add(lastpoint);

            //res = res.SortClockwise().ToList();

            //bool valid = (res.Count > 2);// && (v_out < 0 && res.Inside(pls)) || (v_out > 0 && pls.Inside(res));

            return res.Count > 2 ? res.ToArray() : pls.ToArray();
        }

        public static pPos[] Offset(this IEnumerable<pPos> pls, double v_out)
        {
            if (pls == null || pls.Count() == 0)
                return null;

            List<pPos> res = new List<pPos>();

            if (Math.Abs(v_out) > 0.1)
            {
                pPos[] lsPoint = pls.ToArray(); //.NormalizeList();
                PosCollection lsSeg = new PosCollection();

                if (lsPoint.First().DistanceTo(lsPoint.Last()) > 1)
                    lsPoint = lsPoint.Add(lsPoint.First());

                lsPoint = lsPoint.NormalizeList();
                int total = lsPoint.Length;

                //pPos lastpoint = null;

                for (int i = 0; i < total; i++)
                {
                    int nex = (i + 1) % total;

                    pPos[] ar = lsPoint.ElementAt(i).Parallel(lsPoint.ElementAt(nex), v_out);
                    pPos mid = new pPos((ar[0].X + ar[1].X) / 2, (ar[0].Y + ar[1].Y) / 2);

                    bool b = mid.Inside(lsPoint);

                    if (((!b) && v_out < 0) || (b && v_out > 0))
                        ar = lsPoint.ElementAt(i).Parallel(lsPoint.ElementAt(nex), -v_out);

                    lsSeg.Add(ar);
                }

                //ACD.WR("Segments {0}", lsSeg.Count);

                bool[] can_extents = new bool[4];
                for (int i = 0; i < 4; i++) can_extents[i] = true;

                SegIntersect.can_extents = can_extents;

                for (int i = 0; i < lsSeg.Count; i++)
                {
                    int nex = i + 1;
                    if (i >= lsSeg.Count - 1) nex = 0;

                    if (nex != -1)
                    {
                        SegIntersect.CalcIntersection(v_out, lsSeg[i][0], lsSeg[i][1], lsSeg[nex][0], lsSeg[nex][1]);

                        if (SegIntersect.Intersection != null)
                            res.Add(SegIntersect.Intersection);
                    }
                }

                //res = res.SortClockwise().ToList();
            }

            //bool valid = (res.Count > 2);// && (v_out < 0 && res.Inside(pls)) || (v_out > 0 && pls.Inside(res));

            return res.Count > 2 ? res.ToArray() : pls.ToArray();
        }


        static bool _inSegment(pPos[] seg, pPos p)
        {
            pPos p1 = p.ProjectLine(seg[0], seg[1]);
            return p1.DistanceTo(seg[0]) < 10 || p1.DistanceTo(seg[1]) < 10 || p1.IsBetween(seg[0], seg[1]);
        }

        public static pPos _pointNearest(this IEnumerable<pPos> pls, pPos p)
        {
            pPos res = null;
            PosCollection segs = pls.GetSegment(false);

            PosCollection tmps = segs.Where(seg => _inSegment(seg, p))
                .OrderBy(seg => p.DistanceTo(seg[0], seg[1])).ToCollectionSameClosed();

            if (tmps.Count > 0)
                res = p.ProjectLine(tmps.First()[0], tmps.First()[1]);

            return res;
        }

        public static double _lengthPointNearestFromIndex(this IEnumerable<pPos> pls, int index)
        {
            double res = 0;
            PosCollection segs = pls.GetSegment(false);

            if (index > 0)
                for (int i = 0; i < index; i++)
                    res += segs[i].Length(false);

            return res;
        }

        public static double _lengthPointNearest(this IEnumerable<pPos> pls, pPos p)
        {
            double res = 0;
            PosCollection segs = pls.GetSegment(false);

            PosCollection tmps = segs.Where(seg => _inSegment(seg, p))
                .OrderBy(seg => p.DistanceTo(seg[0], seg[1])).ToCollectionSameClosed();

            if (tmps.Count > 0)
            {
                int index = segs.IndexOf(tmps.First());
                pPos[] seg = segs[index];

                if (index > 0)
                    for (int i = 0; i < index; i++)
                        res += segs[i].Length(false);

                res += p.ProjectLine(seg[0], seg[1]).DistanceTo(seg[0]);


                //if (Math.Abs(res - 189.486235616967) < 1)
                //    SU.WriteLine("inf {0}", index);
            }

            return res;
        }

        public static string ToRelation(this IEnumerable<pPos> src, params pPos[] nodes)
        {
            string saxis = "XYZ";
            string st = "";

            for (int i = 0; i < src.Count(); i++)
            {
                int index = NumericArray(0, nodes.Length - 1)
                    .OrderBy(n => src.ElementAt(i).DistanceTo(nodes[n])).First();

                pPos mv = src.ElementAt(i) - nodes[index];

                for (int axis = 0; axis < 2; axis++)
                {
                    double n = mv[axis].roundNumber();
                    st += "P" + index + saxis[axis] + (n <= 0 ? "" : "+")
                        + (n != 0 ? n.ToString() : "") + (axis < 1 ? "," : "");
                }

                st += ";";
            }

            return st;
        }

        public static string ToInfo(this IEnumerable<pPos> src)
        {
            return src.ToCollection(true).ToInfo;
        }

        public static pPos[] LOffset(this IEnumerable<pPos> pts, double v_out)
        {
            List<pPos> res = new List<pPos>();

            if (Math.Abs(v_out) > 0.1)
            {
                PosCollection lsSeg = new PosCollection();

                int total = pts.Count();

                for (int i = 0; i < total - 1; i++)
                {
                    pPos[] ar = pts.ElementAt(i).Parallel(pts.ElementAt(i + 1), v_out);
                    pPos mid = new pPos((ar[0].X + ar[1].X) / 2, (ar[0].Y + ar[1].Y) / 2);
                    lsSeg.Add(ar);
                }

                res.Add(lsSeg[0][0]);

                for (int i = 0; i < lsSeg.Count - 1; i++)
                {
                    int nex = i + 1;

                    if (nex != -1)
                        res.Add(lsSeg[i][0].Intersect(lsSeg[i][1], lsSeg[nex][0], lsSeg[nex][1], false));
                }

                res.Add(lsSeg.Last[1]);
            }

            if (res.Count < 2)
                return pts.ToArray();
            else
            {
                res.Reverse();
                res.AddRange(pts);
                res.Add(res.First());
                return res.ToArray();
            }
        }

        public static void SwapObjs<T>(this IEnumerable<T> array, int position1, int position2)
        {
            T[] tmp_ls = array.ToArray();
            T temp = tmp_ls[position1]; // Copy the first position's element
            tmp_ls[position1] = tmp_ls[position2]; // Assign to the second element
            tmp_ls[position2] = temp; // Assign to the first element
            array = tmp_ls.AsEnumerable();
        }

        public static pPos[] SortClockwise(this IEnumerable<pPos> pts, pPos basept = null)
        {
            return (pts._isClockwise(basept) ? pts.ToArray() : pts.Reverse()).ToArray();
        }

        public static pPos _isNearBounding(this pPos[] ls1, pPos[] ls2)
        {
            pPos[] bb1 = ls1.Boundary();
            pPos[] bb2 = ls2.Boundary();

            return new pPos(bb1[0].DistanceTo(bb2[0]), bb1[1].DistanceTo(bb2[1]));
        }


        public static pPos[] Weld(this IEnumerable<pPos> pts, double thresdhold = 1)
        {
            List<pPos> ls = SortClockwise(pts).ToList();
            List<int> indexes = new List<int> { 0 };
            for (int i = 1; i < ls.Count; i++)
                if (ls[i].DistanceTo(ls[i - 1]) > thresdhold && ls[i].DistanceTo(ls[0]) > thresdhold)
                    indexes.Add(i);

            return indexes.Cast<int>().Select(n => ls[n]).ToArray();
        }

        public static pPos[] NormalizeList(this IEnumerable<pPos> pts, double threshold = 1)
        {
            if (pts == null || pts.Count() == 0)
                return null;

            pPos[] ls = pts.ToArray();// .Weld(threshold);

            List<int> indexes = NumericArray(0, ls.Length - 1).ToList();

            while (true)
            {
                bool b = false;

                for (int i = 0; i < indexes.Count; i++)
                {
                    int cur = indexes[i];
                    int prev = indexes[(i - 1 + indexes.Count) % indexes.Count];
                    int nex = indexes[(i + 1) % indexes.Count];

                    if (ls[cur].IsOnLine(ls[prev], ls[nex]))
                    {
                        indexes.Remove(cur);

                        b = true;
                        break;
                    }
                }

                if (!b)
                    break;
            }

            return indexes.Select(n => ls[n]).ToArray();
        }

        public static bool _isClockwise(this IEnumerable<pPos> cc, pPos basepoint = null)
        {
            if (basepoint == null) basepoint = cc.Centroid();
            return (cc.Count() >= 2 && cc.ElementAt(0).CompareTo(cc.ElementAt(1), basepoint));
        }

        public static pPos CenterPoint(this IEnumerable<pPos> pts)
        {
            pPos res = null;

            if (pts.Count() > 1)
            {
                res = pts.First();
                for (int i = 1; i < pts.Count(); i++)
                    res += pts.ElementAt(i);
                res /= pts.Count();
            }
            else if (pts.Count() > 0)
                res = pts.First();
            return res;
        }

        public static pPos[] Rotate(this IEnumerable<pPos> pts, double theta, pPos basept)
        {
            return pts.Select(pt => pt.Rotate(theta, basept)).ToArray();
        }

        public static pPos[] Mirror(this IEnumerable<pPos> pts, pPos line1, pPos line2)
        {
            return pts.Cast<pPos>().Select(pt => pt.Mirror(line1, line2)).ToArray();
        }

        public static System.Drawing.PointF[] ToPts(this IEnumerable<pPos> pts, double scale)
        {
            return pts.Multiply(scale).Select(p => new System.Drawing.PointF((float)p.X, ((float)p.Y))).ToArray();
        }

        public static bool IsThinRegion(this IEnumerable<pPos> pts, double range) //to reconize walls
        {
            PosCollection segs = pts.GetSegment();
            bool res = false;

            if (pts.Count() <= 4)
                res = segs.Count(ls => ls.Length <= range) >= pts.Count() - 2;
            else
            {
                int[] seg_length_orders = NumericArray(0, segs.Count - 1).OrderBy(v => -segs[v].Length()).ToArray();
                int max_index = seg_length_orders.First();
                pPos p1 = segs[max_index][0], p2 = segs[max_index][1];

                for (int i = 1; i < seg_length_orders.Length; i++)
                {
                    pPos p3 = segs[seg_length_orders[i]][0], p4 = segs[seg_length_orders[i]][1];
                    double d1 = p1.DistanceTo(p3, p4);
                    double d2 = p2.DistanceTo(p3, p4);

                    if (d1 > 20 && d2 > 20 && Math.Abs(d1 - d2) < range && d1 < range && d2 < range)
                    {
                        res = true;
                        break;
                    }
                }
            }
            return res;
        }

        public static bool _isVeryClosed(this IEnumerable<pPos> ls1, IEnumerable<pPos> ls2, double tole = 1)
        {
            return ls1.First()._isVeryClosed(ls2.Last(), tole)
                        || ls1.Last()._isVeryClosed(ls2.Last(), tole)
                        || ls1.First()._isVeryClosed(ls2.First(), tole)
                        || ls1.Last()._isVeryClosed(ls2.First(), tole);
        }
    }
}
