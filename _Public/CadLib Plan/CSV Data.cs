using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace AcadScript
{
    public class CSVDataCLS
    {
        List<string> contents;
        cReadData CssData;
        double total_area = 0;
        int total = 0;
        public int rows = 32;

        public static double _getRegionAngle(pPos[] pts)
        {
            pPos[] line = pts.MaxSegment().OrderBy(p => p.X).ThenBy(p => p.Y).ToArray();
            return (line[0] - line[1]).Angle();// / 180 * Math.PI;
        }

        public static pPos _getRegionSize(pPos[] pts, double scale = 1)
        {
            PosCollection segments = pts.GetSegment().OrderBy(seg
                => -seg.Length(false)).ToCollectionSameClosed(false);

            List<pPos> ls = segments[0].ToList();
            ls.AddRange(segments[1].Reverse());

            pPos ct = ls.CenterPoint();

            pPos[] bb = pts.Rotate(-90 + _getRegionAngle(pts), ct).Boundary();

            ct.Content = _rounN(bb.Size().X * scale, 5, 8, 16, 20) + " x " + _rounN(bb.Size().Y * scale, 5, 8, 16, 20);

            //pPos p1 = new pPos((bbs[0].X + bbs[1].X) / 2, bbs[1].Y).Rotate(-angle + 90, ct);
            //pPos p2 = new pPos((bbs[0].X + bbs[1].X) / 2, bbs[0].Y).Rotate(-angle + 90, ct);
            //pPos[] itps = pts.Intersect(p2.AlongRatio(p1, 1.1), p1.AlongRatio(p2, 0.9)).OrderBy(p => -p.Y).ToArray();

            return ct;
        }

        public CSVDataCLS(cReadData _css)
        {
            //filename = _filename;
            contents = new List<string>();

            total_area = 0;
            total = 0;

            CssData = _css;
        }

        public string SaveCSV(g3DBuild g3d, string filename)
        {
            string alphabet = "ABCDEFGHIJKLMNOQRSTUV";
            //ACD.WR("Total {0}", g3d.regionList.Count);

            for(int i = 0; i < g3d.regionList.Count; i ++)
                if(!g3d[i].st_("MT") && alphabet.ct_(g3d[i][0].ToString()))
                    AppendCSV(g3d.regionList[i], g3d[i]);
            
            //ACD.WR("Content: {0}", contents.ToTextStr("\r\n"));
            string res = "<button class=\"open-button\" onclick=\"openForm()\">INFO</button>\r\n"
                + "<button class=\"open-button view-3d-button\" onclick = \"view3DForm()\">3D</button>\r\n"
                + "<button class=\"open-button save-3d-button\" onclick=\"save3D()\">DAE</button>\r\n"
                + "<div class=\"form-popup\" id=\"myForm\">\r\n"
                + "\t<form class=\"form-container\">\r\n"
                + "\t\t<table>\r\n";

            contents = contents.OrderBy(s => s.filter(",")[0][0])
                .ThenBy(s => s.filter(",")[0].Substring(1).ToNumber())
                .ThenBy(s => s.filter(",")[0]).ToList();

            contents = DE.NumericArray(0, contents.Count - 1).Select(n => (n + 1) + "," + contents[n]).ToList();

            contents.Add(">,Total," + total_area.roundNumber(0.01) + "(m2)," + total + "(items)");

            res += "\t\t\t<tr>";
            

            for (int j = 0; j <= Math.Ceiling((double)(contents.Count / rows)); j++)
            {
                res += "<th>No</th>";
                res += "<th>Name</th>";
                res += "<th>Area</th>";
                res += "<th>Dimension</th>";
                //res += "<th>Price</th>";
                //res += "<th>|</th>\r\n";
            }

            res += "</tr>\r\n";
            
            for(int i = 0; i < rows; i ++)
            {
                string st = "";
                res += "\t\t\t<tr>";

                for (int j = 0; j <= Math.Ceiling((double)(contents.Count / rows)); j++)
                {
                    int index = j * rows + i;

                    if (index < contents.Count)
                        st += contents[index] + ",";
                }

                int cnt = 0;
                string[] ar = st.filter(",");
                //res += "\t\t\t\t";

                foreach (string _s in st.filter(","))
                {
                    if(st.ct_("Total"))
                        res += "<th style = \"background: yellow;\">";
                    else if(cnt == 2)
                        res += "<th style = \"background: lightgray;\">";
                    else
                        res += "<th>";

                    res += _s + "</th>"; 

                    cnt++;
                }
                
                res += "</tr>\r\n";
            }

            contents.Insert(0, "No,Name,Area,Dimension");
            contents.Add("Count," + total + " items");
            contents.Add("Area," + total_area);

            File.WriteAllLines(filename, contents.ToArray());

            res += "\t\t</table>\r\n" + "\t</form>\r\n</div>\r\n";

            return res;
        }

        public static double _rounN(double v, params double[] base_rounds)
        {
            double res = v;

            foreach (double base_round in base_rounds)
                if (v < base_round && Math.Abs(v - base_round) < 0.8)
                {
                    res = base_round;
                    break;
                }

            return res.roundNumber(0.01);
        }

        public void AppendCSV(pPos[] pts, string s_caption, int counter = 0)
        {

            string s_area = "";
            pPos __txt = cReadData.AreaList.FirstOrDefault(__p => __p.Inside(pts));

            if (__txt != null)
                s_area = __txt.Content;

            double area = (pts.Area()/1000000).roundNumber(0.1);

            if (s_area.empty())
                s_area = "" + _rounN(area, 80, 100, 200, 500, 1000);

                //_appendCSV(pts, s_caption, s_area);
                
            pPos sz = _getRegionSize(pts);

            //Kích thước lô đất lưu trong pt.content
            contents.Add(String.Format("{0},{1},{2}x{3}", s_caption,
                //fullname + "-" + xnotes[i]._firstPropName(),
                s_area.empty() ? _rounN(pts.Area(), 80, 100, 200, 500, 1000).ToString() : s_area,
                (sz.Content.filter("x")[0].ToNumber()/1000).roundNumber(0.1),
                (sz.Content.filter("x")[1].ToNumber()/1000).roundNumber(0.1)));

            string[] exclude_keys = new string[] { "PARK","LAKE","ROAD","WATER"};

            if (exclude_keys.All(___s => !s_caption.ct_(___s)))
                total_area += area;

            total++;
            //}
        }
    }
}
