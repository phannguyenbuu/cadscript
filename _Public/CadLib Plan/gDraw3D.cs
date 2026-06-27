using System;
using System.Collections;
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

    public class g3DBuild
    {
        public string block_content;

        //pPos __basept = new pPos(0, 0);
        public pPos __sz = new pPos(0, 0);
        //pPos[] __blockPts;

        cReadData CSSData;

        public PosCollection mtlList, regionList, walkList; // Lối băng qua đường, tim đường
        public List<gRoadElement> roadList;
        public List<pPos> MtlPoints;
        ObjectIdCollection srcIds;
        //bool _isPlan = false;

        public g3DBuild(ObjectIdCollection selIds,  cReadData _css)
        {
            CSSData = _css;
            srcIds = new ObjectIdCollection();

            mtlList = new PosCollection();
            walkList = new PosCollection();

            foreach (ObjectId _id in selIds)
            {
                if (ACD.DB._isBlock(_id))
                    srcIds.Add(_id);

                __getSubMtlList(_id);
            }
            
            gBlockToHtml();
        }

        void __getSubMtlList(ObjectId _id)
        {
            string st = ACD.DB.GetLinetypeText(_id);

            if (ACD.DB._isPolyline(_id) || ACD.DB._isLine(_id))
            {
                pPos[] ls = ACD.DB._getVertices(_id, 16);

                if (st.st_("MTL") || st.st_("ML") || st.st_("MT"))
                {
                    foreach (pPos _p in ls)
                        _p.Content = st;

                    mtlList.Add(ls);
                }else if(st.st_("#"))
                {
                    walkList.Add(cReadData.__to2D(ls));
                }
            } else if (ACD.DB._isBlock(_id))
            {
                ACD.DB.BlockEntitiesAction(_id, _subIds =>
                {
                    foreach (ObjectId _sid in _subIds)
                        __getSubMtlList(_sid);
                });
            }
        }

        public int Count
        {
            get
            {
                return regionList.Count;
            }
        }

        public string this[int index]
        {
            get
            {
                return MtlPoints[index].Content;
            }

            set
            {

                MtlPoints[index].Content = value;
            }
        }

        bool ___InsidePts(pPos p, IEnumerable<pPos> pts)
        {
            bool res = false;
            if (pts != null && pts.Count() > 0)
            {
                pPos[] bb = pts.Boundary();
                //ACD.WR("PTS {0} BB1 {1} BB2 {2}", pts.Count(), bb[0], bb[1]);
                if (p.InsideRect(bb[0], bb[1]))
                {
                    List<pPos> ls = pts.ToList();// : pts.Offset(offset).ToList();
                    ls.Add(pts.First());

                    int n = ls.Count;
                    double angle = 0;

                    for (int i = 0; i < n; i++)
                        angle += DE.Angle(ls[i].X - p.X, ls[i].Y - p.Y,
                            ls[(i + 1) % n].X - p.X, ls[(i + 1) % n].Y - p.Y);

                    res = (Math.Abs(angle) >= Math.PI);
                }
            }

            return res;
        }

        public void Print()
        {
            string msg = "";

            for (int i = 0; i < regionList.Count; i++)
                msg += this[i] + ";";

            ACD.WR("Region List {0}", msg);
        }

        void gBlockToHtml() //MAIN METHOD
        {
            regionList = new PosCollection();
            
            string style = null;
            PosCollection pls = new PosCollection();

            foreach (ObjectId id in srcIds)
                if(ACD.DB._isBlock(id))
                {
                    style = ACD.DB._getIdName(id);
                    
                    ACD.DB.BlockEntitiesAction(id, _ids =>
                    {
                        pls.AddRange(CSSData.PointList);
                    });
                }

            GraphConsole.Compute(pls);
            regionList = GraphConsole.ResultPts;//.Where(__ls => __ls.Area() > 1000).ToCollectionSameClosed(true);

            MtlPoints = regionList.Select(__ls => __ls[0].Clone()).ToList();

            cReadData._generate_title_from_linears(this);

            //if (style.st_("plan"))
            recognizeRoadnElements();
            //ACD.WR("OK3");
            for (int i = 0; i < regionList.Count; i++)
            {
                var pts = regionList[i];
                if (this[i].et_())
                    this[i] = "ZERO";
            }
        }

        
        void recognizeRoadnElements()
        {
            roadList = new List<gRoadElement>();

            for (int i = 0; i < regionList.Count; i++)
            { 
                if (this[i].et_())
                {
                    foreach (pPos[] __ls in cReadData.inblock_mtl_list)
                    {
                        string key = __ls[0].Content;

                        if (key.st_("MT_"))
                            foreach (pPos __p in __ls)
                            {
                                if (___InsidePts(__p, regionList[i]))
                                {
                                    this[i] = __p + "[" + key + "]";
                                    break;
                                }
                            }
                    }

                    if (this[i].et_() && regionList[i].Area() > 60) //CHECK_ROAD
                    {
                        double __v = regionList[i].Length(true) / 4;

                        if (__v * __v * 0.72 >= regionList[i].Area())
                            this[i] = "MT_ROAD";
                        else
                        {
                            pPos __z = CSVDataCLS._getRegionSize(regionList[i]);
                            string[] __ar = __z.Content.filter("x");
                            double __x = __ar[0].ToNumber();
                            double __y = __ar[1].ToNumber();

                            if(__x > 20 * cReadData.__sc && __y >= 2.9 * cReadData.__sc && __y <= 20 * cReadData.__sc)
                                this[i] = "MT_ROAD";
                        }
                    }
                }

                if (this[i].ct_("ROAD"))
                    roadList.Add(new gRoadElement(regionList[i]));
            }

            walkList.AddRange(gRoadElement.GetWalkList(roadList));
        }

        public PosCollection AllWalkListBars
        {
            get
            {
                PosCollection res = new PosCollection();

                foreach (pPos[] _w in walkList)
                {
                    pPos[] _pts = SpacingTool.RangePoints(_w, 0.9 * cReadData.__sc, true, true);
                    double v = _w.Length == 2 ? 3: 2;

                    for (int i = 1; i < _pts.Length - 1; i++) //Cắt bỏ đầu và cuối
                    {
                        var p = _pts[i];

                        res.Add(new pPos(p.X - 0.3 * cReadData.__sc, p.Y - v / 2 * cReadData.__sc)
                            .Rect(0.6 * cReadData.__sc, v * cReadData.__sc)
                            .Rotate(-p.Rotation, p));
                    }
                }
                
                return res;
            }
        }
    }

    public static class SpacingTool
    {

        static pPos _interpolate(pPos[] segment, double t)
        {
            var dx = segment[1].X - segment[0].X;
            var dy = segment[1].Y - segment[0].Y;
            var l = Math.Sqrt(dx * dx + dy * dy);

            return new pPos(segment[0].X + dx / l * t, segment[0].Y + dy / l * t);
        }

        public static pPos[] RangePoints(pPos[] pts, double pointDist, bool divison_equals = true, bool align = false)
        {
            List<pPos> res = new List<pPos>();
            double polyLineLength = pts.Length(false);
            //ACD.WR("p0");
            //pPos[] __pts = ParralellLine(pts,1 * cReadData.__sc);

            if (divison_equals)
                pointDist = polyLineLength / (Math.Floor(polyLineLength / pointDist));

            int numDist = (int)(polyLineLength / pointDist);
            //ACD.WR("p1");
            double pointPosition = 0.0;
            double prevSegmentsLength = 0.0;
            double segmentsLength = 0.0;
            int currentSegment = 0;
            PosCollection segments = pts.GetSegment(false);
            var segment = segments.First;
            //ACD.WR("p2");
            for (int i = 0; i <= numDist; i++)
            {
                while (pointPosition > segmentsLength && currentSegment < segments.Count)
                {
                    prevSegmentsLength = segmentsLength;
                    //ACD.WR("p2.1");
                    segment = segments[currentSegment]; //ACD.WR("p2.2");
                    segmentsLength += segment.Length(false);
                    currentSegment++;
                }

                var point = _interpolate(segment, pointPosition - prevSegmentsLength);
                
                res.Add(point);
                pointPosition += pointDist;
            }

            if(pts.Length == 2)
            {
                for (int i = 0; i < res.Count; i++)
                    res[i].Rotation = (pts.First() - pts.Last()).Angle();

                //ACD.DB.DrawPolyline(pts.Select(__p => __p / cReadData.__sc), false);
                //ACD.DB.CreateText(res[0].Rotation.roundNumber(0.1).ToString(), pts.CenterPoint() / cReadData.__sc, 2);
            }
            else if (res.Count > 1)
            {
                for (int i = 0; i < res.Count - 1; i++)
                    res[i].Rotation = (res[i]-res[i + 1]).Angle();

                res[res.Count - 1].Rotation = (res[res.Count - 1] - res[res.Count - 2]).Angle();
            }

            return res.ToArray();
        }

        static pPos[] _parallelSeg(pPos p1, pPos p2, double v)
        {
            var L = p1.DistanceTo(p2);

            var x1p = p1.X + v * (p2.Y - p1.Y) / L;
            var x2p = p2.X + v * (p2.Y - p1.Y) / L;
            var y1p = p1.Y + v * (p1.X - p2.X) / L;
            var y2p = p2.Y + v * (p1.X - p2.X) / L;

            return new pPos[] { new pPos(x1p, y1p), new pPos(x2p, y2p) };
        }

        public static pPos[] ParralellLine(pPos[] pts, double v_out)//, pPos __firstpoint, pPos __lastpoint)
        {
            PosCollection pls = new PosCollection();
            //ACD.WR("PAR 0");
            for (int i = 0; i < pts.Length - 1; i++)
                pls.Add(_parallelSeg(pts[i], pts[i + 1], v_out));

            if (pls.Count > 0)
            {
                //ACD.WR("PAR 1 {0}", pls);
                List<pPos> res = new List<pPos>() { pls[0][0] };
                //ACD.WR("PAR 1.1");

                for (int i = 0; i < pls.Count - 1; i++)
                {
                    //ACD.WR("PAR 1.2");
                    var __p = pls[i][0].Intersect(pls[i][1], pls[i + 1][0], pls[i + 1][1], false);
                    //ACD.WR("PAR 2");
                    if (__p != null)
                    {
                        if (res.Count < 2)
                            res.Add(__p);
                        else
                        {
                            pPos __p1 = res[res.Count - 2];
                            pPos __p2 = res[res.Count - 1];
                            __p.DistanceTo(__p1, __p2);

                            if (!pPos.DistanceTo_Projection.IsBetween(__p1, __p2))
                                res.Add(__p);
                        }
                    }
                }

                res.Add(pls.Last().Last());
                return res.ToArray();
            }
            else
                return null;
        }
    }
}
