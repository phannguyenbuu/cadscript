using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace AcadScript
{
    
    public class RoadCrossPickCLS
    {
        static double __sc = 1000;
        static PosCollection mtlList = new PosCollection();

        static void _getCrossRegion(ObjectId id, pPos pt)
        {
            string style = ACD.DB._getIdName(id);

            ACD.DB.BlockEntitiesAction(id, _ids =>
            {
                pPos[] _is_ceil_list = _ids.ToList().Where(_id
                    => ACD.DB._isCircle(_id)).Select(_id => ACD.DB._getPoint(_id)).ToArray();

                PosCollection pls = ACD.DB._getAllVertices(_ids, 12);

                for (int i = 0; i < pls.Count; i++)
                    if (pls.Closed[i])
                        pls[i] = pls[i].Add(pls[i].First());

                pls = pls.Select(_ls => _ls.Select(_p => _p * __sc).ToArray()).ToCollectionSameClosed(false);

                //if (pls.Count > 0)
                    //bounds = pls.OrderBy(_ls => -_ls.Area()).ToCollectionSameClosed(true).First();

                GraphConsole.Compute(pls);
                pls = GraphConsole.ResultPts.Select(_ls => _ls.Straighten(true)).ToCollectionSameClosed(true);

                ACD.WR("FLOOR 00:{0}", pls.Count);
                foreach (pPos[] pts in pls)
                    if (pt.Inside(pts.Select(_p => _p / __sc)))
                    {
                        PosCollection segs = pts.GetSegment();

                        foreach (pPos[] seg in segs)
                            if (seg[0].DistanceTo(seg[1]) > 2 * __sc)
                            {
                                ACD.DB.DrawPolyline(new pPos[] { seg[0] / __sc, seg[1] / __sc }, false, "LAYER=A-FIN");
                            }

                        break;
                    }
            });
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                ACD.WR("!!! For ceiling object, put circle for low ceil plane !!!");
                ACD.WR("!!! Char $ = height, # = cote, +- = level !!!");
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    pPos pt = ACD.GetPoint();

                    _getCrossRegion(selIds[0], pt);
                }
            }

            ACD.Focus();
        }
    }
}

