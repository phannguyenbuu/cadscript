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
    public class StructuralEBeamCLS
    {
        static pPos[] _collectPoints(ObjectIdCollection ids, Func<ObjectId,bool> act, Func<ObjectId, pPos[]> act_result)
        {
            List<pPos> res = new List<pPos>();

            foreach(ObjectId id in ids)
                if(act(id))
                    res.AddRange(act_result(id));

            foreach(ObjectId id in ids)
                if(ACD.DB._isBlock(id))
                    ACD.DB.BlockEntitiesAction(id, _ids => { res.AddRange(_collectPoints(_ids, act, act_result));});

            return res.ToArray();
        }

        static PosCollection _collectPos(ObjectIdCollection ids, Func<ObjectId, bool> act, Func<ObjectId, pPos[]> act_result)
        {
            PosCollection res = new PosCollection();

            res.AddRange(ids.ToList().Where(id => act(id)).Select(id => act_result(id)));

            foreach (ObjectId id in ids)
                if (ACD.DB._isBlock(id))
                    ACD.DB.BlockEntitiesAction(id, _ids => { res.AddRange(_collectPos(_ids, act, act_result)); });

            return res;
        }

        

        static void _parseTxts(pPos[] pts)
        {
            string num = "123456789";
            string[] titles = pts.Where(p => p.Content.st_("D")).Select(p => p.Content.Upper()).Distinct().ToArray();
            pPos[] lsSizes = pts.Where(p => !p.Content.st_("D") && num.Any(n => p.Content.st_(n.ToString())) && p.Content.ct_("x")).ToArray();

            //ACD.WR("Titles {0} Sizes {1}", titles.ToTextStr(), lsSizes.ToText());

            BeamTitleList = new List<pPos>();

            if(titles.Length > 0 && lsSizes.Length > 0)
                foreach(string title in titles)
                {
                    pPos pt = pts.FirstOrDefault(p => p.Content.Upper() == title);

                    pPos[] ls = lsSizes.Where(p => p.Rotation == pt.Rotation)
                        .OrderBy(p => p.DistanceTo(pt)).ToArray();

                    pt.Content += "[" + ls[0].Content + "]";
                    

                    BeamTitleList.Add(pt);
                }

            //ACD.WR("Beam Sizes {0}", BeamTitleList.ToText());
        }

        static pPos _getBeamName(pPos seg1, pPos seg2)
        {
            int axis = Math.Abs(seg1.Y - seg2.Y) < 10 ? 0 : 1;

            pPos[] ls = BeamTitleList.Where(p
                => (p.Rotation == 0 || p.Rotation == 360  && axis == 0) || (p.Rotation != 0 && axis == 1))
                .OrderBy(p => p.DistanceTo(seg1,seg2)).ToArray();

            return ls.Length > 0 ? ls.First() : null;
        }

        static PosCollection groupBeamLine(PosCollection groups, double tole = 250)
        {
            PosCollection res = new PosCollection();
            //double beam_height = beam_title.Content.filter("x").Last().ToNumber(30) * 10;

            ObjectIdCollection tmpIds = new ObjectIdCollection();

            //int ax = beam_title.Rotation == 0 ? 0 : 1;

            for (int ax = 0; ax < 2; ax++)
            {
                PosCollection pls = groups.Where(ls
                        => ls[0].AngleAxisVsVector(ls[1]) == ax)
                        .Select(ls => new pPos[] { ls[0].Round(50), ls[1].Round(50) }).ToCollectionSameClosed(false);

                int nex = (ax + 1) % 2;
                double[] lnexs = pls.Select(ls => ls[0][nex]).Distinct().OrderBy(v => v).ToArray();

                for (int i = 0; i < lnexs.Length; i++)
                {
                    List<double[]> segs = pls.Where(ls => ls[0][nex] == lnexs[i])
                        .Select(ls => new double[] { ls[0][ax], ls[1][ax] }.OrderBy(v => v).ToArray())
                        .OrderBy(ns => ns[0]).ToList();

                    if (segs.Count > 0)
                    {
                        double vmin = segs.Min(ls => ls[0]), vmax = segs.Max(ls => ls[1]);
                        List<double> break_points = new List<double> { vmin, vmax };

                        //foreach (double v in break_points)
                        //    ACD.DB.DrawCircle(new pPos(v, lnexs[i]), 200);

                        for (double v = vmin; v <= vmax; v += tole)
                            if (!segs.Any(ns => ns[0] - tole <= v && v <= ns[1] + tole))
                                break_points.Insert(1, v);

                        //foreach (double v in break_points)
                        //tmpIds.AddRange(ACD.DB.DrawCircle(new pPos(v, lnexs[i]), 100));

                        //ACD.WR("segs {0}", break_points.ToTextDouble(","));

                        for (int j = 0; j < break_points.Count - 1; j++)
                        {
                            List<double[]> tmps = segs.Where(ns => break_points[j] <= ns[0]
                                && ns[1] <= break_points[j + 1]).ToList();

                            if (tmps.Count > 0)
                            {
                                pPos p1 = new pPos(0, 0);
                                pPos p2 = new pPos(0, 0);

                                p1[ax] = tmps.Min(ns => ns[0]);
                                p2[ax] = tmps.Max(ns => ns[1]);

                                p1[nex] = lnexs[i];
                                p2[nex] = lnexs[i];

                                res.Add(new pPos[] { p1, p2 });
                            }
                        }

                        //ACD.DB.NewBlock(tmpIds, ACD.DB.uniqueBlockName("tmp_circle"),true,true, basept);
                    }
                }
            }

            return res;
        }

        static List<pPos> BeamTitleList;
        static pPos beam_title;
        static pPos basept;
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                //bool control_mode = ACD.ControlHold;
                //ObjectIdCollection selIds = ACD.GetSelection();
                pPos pt = ACD.GetPoint();

                if (pt != null)
                {
                    pPos[] zone = ACD.DB.GetZoneFromPoint(pt);

                    if (zone != null)
                    {
                        //ACD.WR("Ok2");
                        ACD.DB.GetEntities(zone, EN_SELECT.AC_DXF, "INSERT", "MTEXT", "TEXT");

                        ObjectIdCollection selIds = IR.SelectedIds;
                        ACD.WR("Ok_txt {0},{1}", selIds.Count, selIds.ToList().Select(id => ACD.DB._getPoint(id)).ToText());

                        pPos[] txts = _collectPoints(selIds.ToList().Where(id => ACD.DB._isBlock(id) 
                            && ACD.DB._getIdName(id).ct_("txt")).ToCollection(),
                            id => { return ACD.DB._isText(id); },
                            id => { pPos _p = ACD.DB._getPoint(id); _p.Rotation = ACD.DB._getRotation(id).roundNumber(30); return new pPos[] { _p }; });

                        _parseTxts(txts);
                        //beam_title = _getBeamName(pt, 0);
                        //ACD.WR("Ok_txt {0}", txts.Length);

                        PosCollection gridPls = _collectPos(selIds.ToList().Where(id => ACD.DB._isBlock(id) 
                            && ACD.DB._getIdName(id).ct_("grid")).ToCollection(),
                            id => { return ACD.DB._isLine(id) ||  ACD.DB._isPolyline(id); },
                            id => { return ACD.DB._getVertices(id); });
                        
                        //ACD.WR("Ok_grid {0}", gridPls.Count);

                        List<double[]> gridXys = gridPls.ExtractPtsXY(50, 50);
                        //ACD.WR("Ok_grid {0}; {1}", gridXys[0].ToTextDouble(","), gridXys[1].ToTextDouble(","));

                        PosCollection beamPls = _collectPos(selIds.ToList().Where(id => ACD.DB._isBlock(id) 
                            && ACD.DB._getIdName(id).ct_("beam")).ToCollection(),
                            id => { return ACD.DB._isLine(id) || ACD.DB._isPolyline(id); },
                            id => { return ACD.DB._getVertices(id); })
                            .Where(ls => ls.Length > 1).ToCollectionSameClosed(true);

                        basept = beamPls.Boundary[0];

                        if (beamPls.Count > 0)
                        {
                            PosCollection beamSegments = groupBeamLine(beamPls.GetSegment()
                                .OrderBy(ls => -ls.Length(false)).ToCollectionSameClosed(false));

                            //ACD.WR("Beam Count {0}", beamSegments.Count);

                            foreach (pPos bname in BeamTitleList)
                            {
                                int ax = bname.Rotation == 0 ? 0 : 1;
                                PosCollection segs = beamSegments.Where(seg
                                    => _getBeamName(seg[0],seg[1]).Content == bname.Content)
                                    .OrderBy(seg => -seg.Length(false)).ToCollectionSameClosed(false);

                                if (segs.Count > 0)
                                {
                                    double bh =  bname.Content.filter("x").Last().ToNumber(30) * 10;
                                    pPos p2 = segs.First()[1];
                                    p2[(ax + 1) % 2] -= bh;

                                    ObjectId lwpId = ACD.DB.Draw2D(segs.First()[0].RectToPoint(p2), "c");
                                    ACD.DB.DrawHatch(segs.First()[0].RectToPoint(p2), "HPATTERN=ANSI31|HSCALE=100");
                                    ACD.DB.SetXNotes(lwpId, "Name=" + bname.Content);
                                }
                            }

                            //foreach (pPos[] seg in beamSegments)
                            //    if (pt._isVeryClosed(pt.ProjectLine(seg[0], seg[1]), 200))
                            //    {
                            //        //ACD.DB.Draw2D(seg[0], seg[1]);
                            //        List<double> lsDist = new List<double>();

                            //        int ax = Math.Abs(seg[0].Y - seg[0].Y) < 100 ? 0 : 1;
                            //        int nex = (ax + 1) % 2;
                            //        //ACD.WR("Ok_5 {0} Beam Name {1}", ax, _getBeamName(pt));

                            //        double curv = double.NegativeInfinity;
                            //        //ACD.WR("Ok_6");

                            //        if (seg[0][ax] < gridXys[ax][0])
                            //        {
                            //            curv = seg[0][ax];
                            //            lsDist.Add(seg[0][ax]);
                            //        }

                            //        lsDist.AddRange(gridXys[ax].Where(v => v >= seg[0][ax]
                            //            && v <= seg[1][ax] && !lsDist.Contains(v.roundNumber(50))));

                            //        //ACD.WR("Ok_Dist {0}", lsDist.ToTextDouble(","));
                            //        break;
                            //    }
                            
                        }
                    }
                }
            }

            ACD.Focus();
        }
    }
}

