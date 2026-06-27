using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Internal;

namespace AcadScript
{
    public class gWallElement: gRoadElement
    {
        public PosCollection WallList, WindowList, DoorList;

        public gWallElement(ObjectId wallId):base(new pPos[0],100,300)
        {
            WallList = new PosCollection();
            WindowList = new PosCollection();
            DoorList = new PosCollection();

            ObjectIdCollection _ids = ACD.DB.ExplodeEntity(wallId);
            ObjectIdCollection __newIds = new ObjectIdCollection();

            foreach (ObjectId _id in _ids)
            {
                __newIds.Add(_id);

                if (ACD.DB._isBlock(_id))
                    __newIds.AddRange(ACD.DB.ExplodeEntity(_id));
            }

            Dictionary<int, PosCollection> _dicts = new Dictionary<int, PosCollection>();

            foreach (ObjectId _id in __newIds)
                if (ACD.DB._isLine(_id))
                {
                    int _n = ACD.DB._getColorIndex(_id);

                    if (!_dicts.ContainsKey(_n))
                        _dicts.Add(_n, new PosCollection());

                    _dicts[_n].Add(ACD.DB._getVertices(_id));
                }

            ACD.DB.EraseObjects(__newIds);

            PosCollection res = new PosCollection();

            foreach (int _n in _dicts.Keys)
            {
                GraphConsole.Compute(_dicts[_n]);
                res.AddRange(GraphConsole.ResultPts);
            }

            res = res.OrderBy(_ls => -_ls.Area()).ToCollectionSameClosed(true);
            //ACD.WR("Res {0}", res.Count);

            if (res.Count > 0)
            {
                PointList = res.First;
                WallList = new PosCollection { res.First };

                var _openings = ACD.DB._getWallOpeningIds(wallId).ToList()
                    .Select(_id => ACD.DB._getVertices(_id)).ToCollectionSameClosed(false);
                //ACD.WR("_openings {0}", _openings.Count);

                if (_openings.Count > 0)
                {
                    WallList = _sliceWallByOpenings(res.First, _openings);
                    //ACD.WR("P5");
                    //foreach (pPos[] _ls in res)
                    //ACD.DB.DrawPolyline(_ls, true);
                    //ACD.WR("P6");
                }
            }

            double _w = this.EndCaps.Min(__ls => __ls.Length(false));

            //ACD.WR("Wall {0}", _w);

            foreach (pPos __p in WallList.AllPoints)
                __p.Content = "Wall" + _w.roundNumber(10);
        }

        PosCollection _sliceWallByOpenings(pPos[] _wallpts, PosCollection _openings)
        {
            PosCollection res = new PosCollection();
            var ct_line = this.CenterLine;
            //ACD.WR("P0");
            if (ct_line != null && ct_line.Length > 0)
            {
                //ACD.WR("P0.1");
                PosCollection __ck_pls = new PosCollection() { this.Pavements[0], this.Pavements[1], null, null };
                List<pPos> _paths = new List<pPos>() { ct_line.First(), ct_line.Last() };
                //ACD.WR("P1");
                foreach (pPos[] __pts in _openings)
                    _paths.AddRange(__pts);

                _paths = _paths.OrderBy(_p => _p.openedPathParam(ct_line)).ToList();
                //ACD.WR("P2");
                for (int _i = 0; _i < _paths.Count - 1; _i++)
                {
                    pPos _p1 = _paths[_i];
                    pPos _p2 = _paths[_i + 1];

                    __ck_pls[2] = _projectionSegments(_paths[_i], __ck_pls[0], __ck_pls[1]);
                    __ck_pls[3] = _projectionSegments(_paths[_i + 1], __ck_pls[0], __ck_pls[1]);

                    GraphConsole.Compute(__ck_pls);
                    res.AddRange(GraphConsole.ResultPts);
                }

                res = res.Where(_ls => !_openings.Any(_op
                    => _op.CenterPoint()._isVeryClosed(_ls.CenterPoint(), 300)))
                    .ToCollectionSameClosed(true);
            }

            return res;
        }

        pPos[] _projectionSegments(pPos pt, pPos[] line1, pPos[] line2)
        {
            pt.DistanceToPts(line1);
            pPos _p1 = pPos.DistanceTo_Projection;

            pt.DistanceToPts(line2);
            pPos _p2 = pPos.DistanceTo_Projection;

            return new pPos[] { _p1, _p2 }.ExtentLine(100);
        }
    }
}
