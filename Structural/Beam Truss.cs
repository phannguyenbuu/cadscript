using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;

//fileIn Cls.IReader
//fileIn Cls.Global
//fileIn Cls.MethodEntity
//fileIn Cls.IZone

//fileIn Cls.ACD
//fileIn Cls.IS
//fileIn Cls.IDraw

//fileIn Cls.IBlock
//fileIn Cls.IAnno
//fileIn Cls.IDimChain

namespace AcadScript
{
    public class BeamTrussCLS
    {
        static PosCollection _gridToMullion(PosCollection pls, pPos[] region, double offset)
        {
            PosCollection res = new PosCollection() { region.Add(region.First()) };

            foreach (pPos[] ls in pls)
            {
                res.Add(ls[0].Parallel(ls[1], offset / 2));
                res.Add(ls[0].Parallel(ls[1], -offset / 2));
            }
            
            GraphConsole.Compute(res);
            res = GraphConsole.ResultPts.Where(ls => ls.Length == 3).ToCollectionSameClosed();

            return res;
        }

        static PosCollection _getRegionGrids(pPos pA, pPos pB, double h, int div, pPos sz)
        {
            List<pPos> ls1 = new List<pPos>();
            List<pPos> ls2 = new List<pPos>();

            pPos[] ln = pA.Parallel(pB, -h);
            pPos p3 = ln.Last();
            pPos p4 = ln.First();

            PosCollection res = new PosCollection();

            pPos[] region = new pPos[] { pA, pB, p3, p4 };
            pPos[] pts = region.Offset(-sz.Y);
            //res.Add(pts);

            pPos p1 = pts.OrderBy(p => p.DistanceTo(pA)).First();
            pPos p2 = pts.OrderBy(p => p.DistanceTo(pB)).First();
            p3 = pts.OrderBy(p => p.DistanceTo(p3)).First();
            p4 = pts.OrderBy(p => p.DistanceTo(p4)).First();
            
            for (int i = 0; i <= div; i++)
            {
                ls1.Add(p1.AlongRatio(p2, (double)i / div));
                ls2.Add(p4.AlongRatio(p3, (double)i / div));
            }

            for (int i = 0; i <= div; i++)
            {
                if(i > 0 && i < ls1.Count - 1)
                    res.Add(new pPos[] { ls1[i], ls2[i] });

                if (i % 2 == 0 && i < ls1.Count - 1)
                    res.Add(new pPos[] { ls2[i], ls1[i + 1] });
                else if(i % 2 == 1 && i < ls1.Count - 1)
                    res.Add(new pPos[] { ls1[i], ls2[i + 1] });
            }

            res = _gridToMullion(res, pts, sz.Y);
            res.Add(region);
            res.Closed = res.Select(ls => true).ToArray();

            int total = res.Count;

            res.AddRange(DE.NumericArray(0, div).Select(i 
                => pA.AlongRatio(pB, (double)i / div) ).CSectionList(sz));

            for (int i = total; i < res.Count; i++)
                res.Closed = res.Closed.Add(false);

            return res;
        }
                
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ACD.Get2Points();

                if (ACD.FirstPoint != null && ACD.LastPoint != null)
                {
                    double h = ACD.ED.GetInputString("Truss depth (300)","300").ToNumber();
                    int div = (int)ACD.ED.GetInputString("Division (4)", "4").ToNumber();
                    string s = ACD.ED.GetInputString("Cross section size (30x60)", "30x60");

                    if (s != null && s.Upper().Contains("X"))
                    {
                        pPos sz  = pPos.FromString(s.Upper().Replace("X", ","));
                    
                        PosCollection grids = _getRegionGrids(ACD.FirstPoint, ACD.LastPoint, h, div, sz);

                        for (int i = 0; i < grids.Count; i++)
                            ACD.DB.DrawPolyline(grids[i], grids.Closed[i]);
                    }
                }
                
            }
        }
    }
}

