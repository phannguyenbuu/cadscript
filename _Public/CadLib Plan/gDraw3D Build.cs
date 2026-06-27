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

        pPos __basept = new pPos(0, 0);
        public pPos __sz = new pPos(0, 0);
        pPos[] __blockPts;

        cReadData CSSData;

        public PosCollection mtlList, regionList; // Lối băng qua đường, tim đường
        public roadListObjCLS roadListObj;
        public List<pPos> MtlPoints;
        ObjectIdCollection srcIds;
        bool _isPlan = false;

        public g3DBuild(ObjectIdCollection selIds,  cReadData _css)
        {
            CSSData = _css;
            __basept = CSSData.html_basepoint;
            srcIds = new ObjectIdCollection();

            if (selIds.ToList().Any(__id => ACD.DB._getIdName(__id).st_("plan")))
            {
                _isPlan = true;
                cReadData.__sc = 1000;
            }

            //ACD.WR(">>> selIds {0} scale {1}", selIds.Count, cReadData.__sc);

            _getMtlList(selIds);

            __sz = ACD.DB._getBound(srcIds).Size();
            
            __blockPts = ACD.DB.FilterIds(srcIds, "INSERT").ToList().Select(id => ACD.DB._getPoint(id)).ToArray();

            if (__blockPts.Any(p => p.Content.ct_("grid")))
                __basept = __blockPts.First(p => p.Content.ct_("grid"));

            __blockPts = __blockPts.Where(p => !p.Content.ct_("grid") && !p.Content.st_("*U")).ToArray();

            __basept *= cReadData.__sc;
                        
            gBlockToHtml();
        }

        void __getSubMtlList(ObjectId _id)
        {
            string st = ACD.DB.GetLinetypeText(_id);

            if (ACD.DB._isPolyline(_id) || ACD.DB._isLine(_id))
            {
                if (st.st_("MTL") || st.st_("ML") || st.st_("MT"))
                {
                    pPos[] ls = ACD.DB._getVertices(_id, 0, false);

                    foreach (pPos _p in ls)
                        _p.Content = st;

                    mtlList.Add(ls);
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

        void _getMtlList(ObjectIdCollection selIds)
        {
            mtlList = new PosCollection();

            foreach (ObjectId _id in selIds)
            {
                if (ACD.DB._isBlock(_id))
                    srcIds.Add(_id);
                
                __getSubMtlList(_id);
            }
        }

        double[] getElipInfo(Database db, ObjectId objId)
        {
            double[] res = null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (objId.ObjectClass.DxfName == "ELLIPSE")
                {
                    Ellipse elp = (Ellipse)tr.GetObject(objId, OpenMode.ForRead);
                    double rot = elp.StartPoint.ToPos().AngleVector(elp.Center.ToPos(), elp.StartPoint.ToPos() + new pPos(1, 0));
                    res = new double[] { elp.MajorRadius, elp.MinorRadius, rot };
                }

                tr.Commit();
            }
            return res;
        }

        // bool isVerts_(pPos[] pts, string xnote)
        //{
        //    pPos[] bb = pts.Boundary();
        //    pPos sz = bb.Size();
        //    PosCollection segs = pts.GetSegment().OrderBy(sg => sg.Length(false)).ToCollectionSameClosed(false);

        //    string[] ar = xnote.filter(";");

        //    for (int i = 0; i < ar.Length; i++)
        //    {
        //        string res = ar[i];
        //        bool equal = !res.ct_(">") && !res.ct_("<");

        //        if (res.st_("n"))
        //        {
        //            res = pts.Length + (equal ? "=" : "") + res.Substring(1);
        //        }

        //        else if (res.st_("seg_") && res.ct_("<"))
        //            res = segs.First().Length(false) + res.Substring(4);
        //        else if (res.st_("seg_") && res.ct_(">"))
        //            res = segs.Last().Length(false) + res.Substring(4);
        //        else if (res.st_("seg") && res.ct_("<"))
        //            res = segs.Last().Length(false) + res.Substring(3);
        //        else if (res.st_("seg") && res.ct_(">"))
        //            res = segs.First().Length(false) + res.Substring(3);

        //        else if (res.st_("s") && res.ct_(">"))
        //            res = Math.Min(sz.X, sz.Y) + res.Substring(1);

        //        else if (res.st_("r") && res.ct_(">"))
        //            res = Math.Max(sz.X, sz.Y) + res.Substring(1);
        //        else if (res.st_("s") && res.ct_("<"))
        //            res = Math.Max(sz.X, sz.Y) + res.Substring(1);
        //        else if (res.st_("r") && res.ct_("<"))
        //            res = Math.Min(sz.X, sz.Y) + res.Substring(1);
        //        else if (res.st_("w"))
        //            res = sz.X + (equal ? "=" : "") + res.Substring(1);
        //        else if (res.st_("h"))
        //            res = sz.X + (equal ? "=" : "") + res.Substring(1);
        //    }

        //    return true;
        //}

        // string _getContentFromColorIndex(string content, ObjectId id)
        //{
        //    int color_index = ACD.DB._getColorIndex(id);

        //    string[] ar = content.filter("()");
        //    string res = content;

        //    if (ar.Length > color_index)
        //    {
        //        res = ar[color_index];
        //        ar = res.filter(" ");

        //        //ACD.WR("ARR {0}", ar.ToTextStr(","));
        //        res = "";

        //        for (int ii = 0; ii < ar.Length; ii++)
        //            if (!ar[ii].st_("#"))
        //            {
        //                string s = ar[ii];

        //                List<double> ls = new List<double>();
        //                string[] _ar = (s.st_("h") ? s.Substring(1) : s).filter("&");

        //                for (int i = 0; i < _ar.Length; i++)
        //                {
        //                    string _s = _ar[i];

        //                    if (_s.ct_("x"))
        //                        ls.AddRange(DE.NumericArray(0,
        //                            (int)(_s.filter("x").Last().ToNumber()) - 1)
        //                            .Select(_i => _s.filter("x").First().ToNumber() * 100));
        //                    else
        //                        ls.Add(_s.ToNumber() * 100);

        //                    _ar[i] = ls.ToTextDouble("&");
        //                }

        //                ar[ii] = (ar[ii].st_("h") ? "h" : "") + _ar.ToTextStr("&");
        //            }

        //        res = ar.ToTextStr(" ");
        //    }

        //    return res;
        //}

         string __transStrList(IEnumerable<pPos> pts, string _extent_info = "")
        {
            return "<polyline points =\""
                + pts.Select(p => new pPos(p.X - __basept.X, p.Y - __basept.Y)).ToText(false, ' ') + "\"" + _extent_info + "/>\r\n";
        }

        

        //void gWallToHtml()
        //{
        //    wallPts = new List<pPos>();
        //    wall_content = "";

        //    if (wallPts.Count > 0)
        //        mtlList.Add(wallPts.ToArray());
        //}

        string __comment(string s)
        {
            return "<!-- " + s + " -->\r\n";
        }

        pPos[] _gnTrees(pPos[] pts, double spacing = 20)
        {
            PosCollection _fsegs = pts.GetSegment(true);
            PosCollection segs = new PosCollection();
            PosCollection _pls = new PosCollection();
            spacing *= cReadData.__sc;

            for (int i = 0; i < _fsegs.Count; i++)
            {
                segs.Add(_fsegs[i][0].Parallel(_fsegs[i][1], 0.5 * cReadData.__sc));
                segs.Add(_fsegs[i][0].Parallel(_fsegs[i][1], -0.5 * cReadData.__sc));
            }

            foreach (pPos[] _seg in segs)
            {
                double d = _seg.Length(false);
                
                if (d >= spacing)
                {
                    List<pPos> ls = new List<pPos>();

                    for (int i = 0; i <= Math.Floor(d / spacing); i++)
                        ls.Add(_seg[0].Along(spacing * i, _seg[1]));

                    if (ls.Count > 0)
                        _pls.Add(ls.ToArray());
                }
            }

            return _pls.AllPoints;
        }

        //void _checkPavementRegions()
        //{
        //    string[] pointnames = regionList.AllPoints.Select(_p 
        //        => _p.X.roundNumber() + "," + _p.Y.roundNumber()).Distinct().ToArray();

        //    List<int[]> namelist = new List<int[]>();
        //    List<int> checkPointNames = new List<int>();

        //    for(int i = 0; i < regionList.Count; i ++)
        //    {
        //        var val = regionList[i].Select(_p => Array.IndexOf(pointnames, 
        //            _p.X.roundNumber() + "," + _p.Y.roundNumber())).ToArray();

        //        if (!this[i].Content.et_()) 
        //            checkPointNames.AddRange(val);
                
        //        namelist.Add(val);
        //    }

        //    checkPointNames = checkPointNames.Distinct().ToList();
        //    //ACD.WR("Road {0}", checkPointNames.ToTextInt(","));

        //    for (int i = 0; i < regionList.Count; i++)
        //        if (this[i].et_() && namelist[i].Any(n => checkPointNames.Contains(n)))
        //             this[i] = "MT_PAVE_$200";

        //    //ACD.WR("Pavement {0}", DE.NumericArray(0, regionList.Count - 1).Select(i => this[i]).ToTextStr(","));

        //}

        

        

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

        object[] __extTextValue(string txt)
        {
            double nz = 0, cote = 0, h = 0;
            string[] ar = txt.filter("_");

            h = ar.FirstOrDefault(_s => _s.st_("$")).ToNumber();
            double nlevel = ar.FirstOrDefault(_s => _s.st_("+") || _s.st_("-")).ToNumber();
            cote = ar.FirstOrDefault(_s => _s.st_("#")).ToNumber();
            nz = cote;
            //ACD.WR("F1.2");
            string sstyle = "";

            foreach (string s in ar)
                if (!s.st_("#"))
                    sstyle += s + "_";

            return new object[] { sstyle, nz, nlevel, h };
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
                    //if (ls.isClosed())
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
            cReadData.inblock_mtl_list = new PosCollection();
            string style = null;
            PosCollection pls = new PosCollection();

            foreach (ObjectId id in srcIds)
                if(ACD.DB._isBlock(id))
                {
                    style = ACD.DB._getIdName(id);
                    
                    ACD.DB.BlockEntitiesAction(id, _ids =>
                    {
                        pls.AddRange(cReadData.IdsToPts(_ids, 32, cReadData.__sc, 0.2));
                    });
                }

            GraphConsole.Compute(pls);
            regionList = GraphConsole.ResultPts;//.Where(__ls => __ls.Area() > 1000).ToCollectionSameClosed(true);

            MtlPoints = regionList.Select(__ls => __ls[0].Clone()).ToList();
            //ACD.WR("OK2");
            cReadData._generate_title_from_linears(this);

            if (style.st_("plan"))
                recognizeRoadnElements();
            //ACD.WR("OK3");
            for (int i = 0; i < regionList.Count; i++)
            {
                var pts = regionList[i];
                if (this[i].et_())
                    this[i] = "ZERO";
            }

            //ACD.WR("OK4");
        }

        public pPos[] BoundingBoxStraighten(pPos[] pts, double angle)
        {
            angle = CSVDataCLS._getRegionAngle(pts);

            PosCollection segments = pts.GetSegment().OrderBy(seg
                => -seg.Length(false)).ToCollectionSameClosed(false);

            List<pPos> bb = segments[0].ToList();
            bb.AddRange(segments[1].Reverse());

            pPos ct = bb.CenterPoint();// pts.Centroid();

            pPos[] bbs = pts.Rotate(-90 + angle, ct).Boundary();

            pPos p1 = new pPos((bbs[0].X + bbs[1].X) / 2, bbs[1].Y).Rotate(-angle + 90, ct);
            pPos p2 = new pPos((bbs[0].X + bbs[1].X) / 2, bbs[0].Y).Rotate(-angle + 90, ct);

            p1 = p2.AlongRatio(p1, 1.1);
            p2 = p1.AlongRatio(p2, 0.9);

            pPos[] itps = pts.Intersect(p1, p2).OrderBy(p => -p.Y).ToArray();

            return bbs;
        }

        void recognizeRoadnElements()
        {
            roadListObj = new roadListObjCLS();

            for (int i = 0; i < regionList.Count; i++)
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
                        {
                            this[i] = "MT_ROAD";
                        } else
                        {
                            pPos __z = CSVDataCLS._getRegionSize(regionList[i]);
                            string[] __ar = __z.Content.filter("x");
                            double __x = __ar[0].ToNumber();
                            double __y = __ar[1].ToNumber();

                            if(__x > 20 * cReadData.__sc && __y >= 2.9 * cReadData.__sc && __y <= 20 * cReadData.__sc)
                            {
                                this[i] = "MT_ROAD";
                            }
                        }
                    }

                    if (this[i].ct_("ROAD"))
                    {
                        roadListObj.Add(regionList[i]);
                    }
                }

            //CHECK WALK AREA 1: Dự phòng trường hợp khó người dùng phải tự vẽ
            for (int i = 0; i < regionList.Count; i++)
                if (this[i].et_())
                {
                    pPos[] _ls = regionList[i].Where(__p1 => roadListObj.Any(__ls => __ls.Contains(__p1))).ToArray();

                    if (_ls.Length > 1)
                    {
                        List<pPos> _walk = _ls[0].Parallel(_ls[1], 1.5 * cReadData.__sc).ToList();
                        _walk.AddRange(_ls[1].Parallel(_ls[0], 1.5 * cReadData.__sc));

                        this[i] = cReadData._getCrossRoadMtlAxis(_walk.ToArray());
                    }
                }

            //CHECK WALK AREA 2: Tự nhận dạng từ đường trùng
            //foreach (pPos[] pts in roadListObj)
            //    ACD.WR("Road {0}", pts);

            roadListObj.RecognizeWalk();
            regionList.AddRange(roadListObj.WalkList);
            MtlPoints.AddRange(roadListObj.WalkTitleList);

            roadListObj.RecognizeEndcapSegment();

        }
    }

}
