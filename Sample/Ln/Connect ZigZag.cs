using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Internal;


using System.Windows.Forms;
//using SyncObject;

namespace AcadScript
{
    public class PolylineConnectCLS
    {
        static void _selectPoint(IEnumerable<pPos> pts, List<int> res)
        {
            int index = res.Last();
            //ACD.WR("OK1.1");
            int[] indexes = DE.NumericArray(0, pts.Count() - 1)
                .Where(i => !res.Contains(i) && pts.ElementAt(i).DistanceTo(pts.ElementAt(index)) <= 1500)
                .OrderBy(i => pts.ElementAt(i).DistanceTo(pts.ElementAt(index))).ToArray();
            //ACD.WR("OK1.2");
            if (indexes.Length > 0)
            {
                res.Add(indexes.First());
                _selectPoint(pts, res);
            }
        }

        static pPos[] _sortPoints(IEnumerable<pPos> pts, pPos basept = null)
        {
            List<pPos> res = new List<pPos>();

            if (pts.Count() > 2)
            {
                List<int[]> indx = DE.NumericArray(0, pts.Count() - 1).Select(i =>
               {
                   List<int> adj = new List<int> { i };
                   _selectPoint(pts, adj);
                   return adj.ToArray();
               }).ToList();

                if (indx.Count > 0)
                {
                    int max_count = indx.Max(ls => ls.Length);

                    ACD.WR("Max_count {0}", max_count);

                    indx = indx.Where(ls => ls.Length == max_count).ToList();

                    if (basept != null)
                    {
                        res.Add(basept);

                        foreach (int[] ls in indx)
                        {
                            if (pts.ElementAt(ls.First()).DistanceTo(basept) <= 5000)
                            {
                                res.AddRange(ls.Select(i => pts.ElementAt(i)));
                                break;
                            }
                            else if (pts.ElementAt(ls.Last()).DistanceTo(basept) <= 5000)
                            {
                                res.AddRange(ls.Select(i => pts.ElementAt(i)).Reverse());
                                break;
                            }
                        }
                    }

                    if(res.Count < 2)
                        res.AddRange(indx.First().Select(i => pts.ElementAt(i)));
                }
            }
            

            return res.ToArray();
        }

        static void _drawZigZag(pPos[] pts)
        {
            if (pts.Length >= 2)
            {
                List<pPos> lwps = new List<pPos> { pts[0] };

                for (int i = 0; i < pts.Length - 1; i++)
                {
                    pPos p = pts[i].Parallel(pts[i + 1], 200).CenterPoint();
                    //lwps.Add(pts[i]);
                    lwps.Add(p);
                    lwps.Add(pts[i + 1]);
                }
                //ACD.WR("Lwps {0}", lwps.Count);
                ACD.DB.Draw2D(lwps.ToArray());
            }
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                ObjectIdCollection selIds = ACD.GetSelection()._filterDXF("INSERT");

                if (selIds.Count == 2 || ACD.ControlHold)
                {
                    _drawZigZag(selIds.ToList().Select(id => ACD.DB._getPoint(id)).ToArray()) ;
                }
                else if (selIds.Count > 0)
                {
                    string[] blocknames = selIds.ToList().Select(id => ACD.DB._getIdName(id)).Distinct()
                        .OrderBy(s => -selIds.ToList().Count(id => ACD.DB._getIdName(id) == s)).ToArray();

                    ACD.WR("Blocknames {0}", blocknames.ToTextStr());

                    ObjectIdCollection nodeIds = selIds.ToList().Where(id => ACD.DB._getIdName(id) == blocknames.First()).ToCollection();
                    ObjectIdCollection baseIds = selIds.ToList().Where(id => ACD.DB._getIdName(id) != blocknames.First()).ToCollection();

                    List<pPos> pts = new List<pPos>();

                    //if(baseIds.Count > 0)
                    //pts.Add(ACD.DB._getPoint(baseIds.First()));

                    //pts.AddRange(nodeIds.ToList().Select(id => ACD.DB._getPoint(id)).ToArray());

                    pPos[] verts = nodeIds.ToList().Select(id => ACD.DB._getPoint(id)).ToArray();

                    _drawZigZag(verts);
                }

                ACD.Focus();
            }
        }
    }
}

