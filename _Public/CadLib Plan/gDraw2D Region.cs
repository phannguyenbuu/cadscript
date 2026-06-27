using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;

namespace AcadScript
{
    public class gDraw2DRegionCLS:gDraw2DDCLS
    {
        public gDraw2DRegionCLS(cReadData css) : base(css) { }

        public static PosCollection ComputeRegions(PosCollection pls)
        {
            int v = 1000;

            for (int i = 0; i < pls.Count; i++)
                if (pls.Closed[i])
                    pls[i] = pls[i].Add(pls[i][0]);

            GraphConsole.treshold = 100;
            GraphConsole.Compute(pls.Select(ls => ls.Select(p => p * v).ToArray()));

            return GraphConsole.ResultPts.Select(ls
                => ls.Select(p => p / v).ToArray()).Where(ls => ls.Area() > 1).ToCollectionSameClosed();
        }

        pPos[] _sortRectanglePoints(pPos[] pts) //Hình chữ nhật đứng
        {
            pPos p1,p2, p3, p4;

            p1 = pts[0];

            if(pts[0].DistanceToPoint(pts[1]) > pts[0].DistanceToPoint(pts[3]))
            {
                p2 = pts[1];
                p3 = pts[3];
                p4 = pts[2];
            }
            else
            {
                p2 = pts[3];
                p3 = pts[1];
                p4 = pts[2];
            }

            return new pPos[]{p1,p2,p3,p4};
        }

        void _drawRoadCenterLine(pPos p1, pPos p2)
        {
            AppendPolylineHtml(new pPos[] { p1, p2 }, 
                false, " style='stroke:yellow;stroke-width:2;stroke-dasharray:\"4 1 2\"'");
        }

        void _drawCrossRoad(pPos[] pts)
        {
            pPos[] r = _sortRectanglePoints(pts);
            pPos p1 = r[0], p2 = r[1], p3 = r[2], p4 = r[3];

            double space = 1;
            double total = p1.DistanceTo(p2) / 0.6;

            p1 = p1.Along(space, p4);
            p4 = p4.Along(space, p1);
            p2 = p2.Along(space, p3);
            p3 = p3.Along(space, p2);

            for(int i = 0; i < total - 1; i += 2)
                AppendPolylineHtml(new pPos[]{

                        p1.Along(0.6 * i, p2), 
                        p1.Along(0.6 * (i + 1), p2), 
                        p3.Along(0.6 * (i + 1), p4), 
                        p3.Along(0.6 * i, p4)
                    
                    }, 
                    
                    true, " style='stroke-opacity:0;fill:rgb(255,255,255)'");
        }

        public void DrawColor(pPos[] pts, string title, string stylename = null, int color_index = -1)
        {
            string[] clrs = new string[] {
                "255,247,153","196, 223, 155","109,207,246","131,147,202","189,140,191","251,175,93","0,191,243",
                "255,247,153","196, 223, 155","109,207,246","131,147,202","189,140,191","251,175,93","0,191,243",
                "255,247,153","196, 223, 155","109,207,246","131,147,202","189,140,191","251,175,93","0,191,243"
            };

            if (!stylename.et_())
            {
                AppendPolylineHtml(pts, true, stylename);
            }
            else
            {
                if (title.ct_("WALK") && pts.Length > 3)
                    _drawCrossRoad(pts);
                else
                {
                    color_index = (color_index + 1) % clrs.Length;
                    var clr = clrs[color_index];

                    if (title == "CV" || title.ct_("PARK") || title.ct_("GRASS"))
                        clr = "100,190,70";


                    if (title.ct_("ZERO"))
                        clr = "210,210,210";

                    if (title.ct_("LAND"))
                        clr = "45,85,30";

                    if (title.ct_("GRAVE"))
                        clr = "200,200,200";

                    if (title.ct_("GROUND"))
                        clr = "150,100,100";

                    if (title.ct_("SAND"))
                        clr = "255,190,120";

                    if (title.ct_("_DEF"))
                        clr = "200,200,200";

                    if (title.ct_("ROAD"))
                        clr = "0,0,0";

                    //if (title.ct_("WALK"))
                    //    clr = "150,150,150";

                    if (title.ct_("PAVE"))
                        clr = "180,170,100";

                    if (title.ct_("WATER")|| title.ct_("LAKE"))
                        clr = "35,150,250";

                    AppendPolylineHtml(pts, true, " style='fill:rgba(" + clr + ",0.5)'");

                    //if(title.ct_("ROAD") && pts.Length == 4)
                    //    _drawRoadCenterLine(pts);
                        
                }
            }
        }
    }
}
