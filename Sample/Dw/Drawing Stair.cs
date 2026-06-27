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
    public class DrawingStairCLS
    {
        static PosCollection regions;
        static pPos[] max_point_list;
        static double tread_depth = 30, slab_depth = 150, tread_exp = 20, railing_height = 1000;
        static pPos[] center_line;

        static ObjectId _drawRegionSection(IEnumerable<pPos> region, 
            int axis, int index, double depth, double ny = 0, PosCollection pls = null)
        {
            int nex = (axis + 1) % 2;

            pPos[] bb = region.Boundary();
            
            if (axis == 1)
                bb = bb.Select(p => new pPos(p.Y, p.X)).ToArray();

            bb[1].Y = bb[0].Y = index * step_height - ny;

            if (pls != null)
                pls.Add(bb);

            return ACD.DB.DrawPolyline(bb[0].RectToPoint(bb[1] - new pPos(0, depth - ny)), true);
        }

        static ObjectIdCollection _drawSection(int axis)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            int nex = (axis + 1) % 2;

            PosCollection pls = new PosCollection();

            for(int i = 0; i < regions.Count; i ++)
            {
                res.Add(_drawRegionSection(regions[i].Offset(tread_exp), axis, i, tread_exp));

                ObjectId lwp = _drawRegionSection(regions[i], axis, i, step_height, tread_exp, pls);
                res.Add(lwp);

                res.AddRange(_drawNumStep(i + 1, ACD.DB._getBound(lwp).CenterPoint()));
            }

            pPos[] line1 = pls.Select(ls => ls[0]).ToArray();
            pPos[] line2 = pls.Select(ls => ls[1]).ToArray();
            
            res.Add(ACD.DB.DrawPolyline(line1.Move(new pPos(0, - slab_depth - step_height)), false));
            res.Add(ACD.DB.DrawPolyline(line1.Move(new pPos(0, railing_height)), false));
            res.Add(ACD.DB.DrawPolyline(line2.Move(new pPos(0, -slab_depth - step_height)), false));
            res.Add(ACD.DB.DrawPolyline(line2.Move(new pPos(0, railing_height)), false));
            return res;
        }

        static PosCollection _cutIntersectSegment(IEnumerable<pPos> region, IEnumerable<pPos> cutline)
        {
            // Xác định 2 đường railing bằng cách loại bỏ segments bị cắt bởi đường spline

            PosCollection res = new PosCollection();
            PosCollection segments = region.GetSegment();
            //region.Intersect(cutline, true, false, false);
            List<int> indx = DE.NumericArray(0, segments.Count - 1)
                .Where(n => cutline.Intersect(segments[n][0],segments[n][1],true).Length > 0).OrderBy(n=>n).ToList();
            
            for (int i = 0; i < indx.Count; i++)
            {
                if (i == 0)
                {
                    if (indx[i] > 1)
                        res.Add(DE.NumericArray(0, indx[i])
                            .Select(v => region.ElementAt(v)).ToArray());
                }

                if (i == indx.Count - 1)
                {
                    if(indx[i] + 1 < region.Count() - 1)
                        res.Add(DE.NumericArray(indx[i] + 1, region.Count() - 1)
                            .Select(v => region.ElementAt(v)).ToArray());
                }

                if (0 < i && i < indx.Count - 1)
                {
                    if (indx[i - 1] + 1 < indx[i] - 1)
                        res.Add(DE.NumericArray(indx[i - 1] + 1, indx[i] - 1)
                            .Select(v => region.ElementAt(v)).ToArray());
                }
            }

            //ACD.WR("Indx {0}", indx.ToText());

            return res;
        }

        static bool _intersect(ObjectId id, IEnumerable<pPos> pts)
        {
            bool res = false;
            
            if (ACD.DB._isVertice(id))
                res = ACD.DB._getVertices(id, splitarc).IntersectPts(pts, 
                    ACD.DB._isPolylineClosed(id), false).Length > 0;
            
            return res;
        }
        
        static pPos[] RailingOffset(IEnumerable<pPos> pts, double v_out)
        {
            int total = pts.Count();

            PosCollection lsSeg = DE.NumericArray(0,pts.Count() - 2)
                .Select(i => pts.ElementAt(i)
                .Parallel(pts.ElementAt(i + 1), v_out)).ToCollectionSameClosed();
            
            SegIntersect.can_extents = DE.NumericArray(0,3).Select(n => true).ToArray();

            List<pPos> res = new List<pPos> { lsSeg[0][0] };

            for (int i = 0; i < lsSeg.Count - 1; i++)
            {
                if(i == 0)
                    res.Add(lsSeg[i][0]);

                SegIntersect.CalcIntersection(v_out, lsSeg[i][0], 
                    lsSeg[i][1], lsSeg[i + 1][0], lsSeg[i + 1][1]);

                if (SegIntersect.Intersection != null)
                    res.Add(SegIntersect.Intersection);
            }

            res.Add(lsSeg.Last().Last());
            //res.AddRange(pts.Reverse());

            return res.Count > 2 ? res.ToArray() : pts.ToArray();
        }
        
        static ObjectIdCollection getEntFromCurvePoints(IEnumerable<pPos> pts, double height)
        {
            ObjectIdCollection ids = new ObjectIdCollection();

            PosCollection res = new PosCollection();
            pPos[] bounding = pts.Boundary();

            string[] keywords = pString.INI_String("NOT_VALID_REGION_LAYER").filter(";");
            ACD.DB.GetEntities(ACD.bRect, EN_SELECT.AC_DXF, "INSERT", "LWPOLYLINE", "LINE", "ARC");

            //ACD.DB.DrawPolyline(bounding.Rect());

            foreach (ObjectId id in IR.SelectedIds)
                if(!keywords.Any(s => ACD.DB._getLayer(id).Upper().Contains(s.Upper())))
                {
                    pPos[] bb = ACD.DB._getBound(id);
                    if (pts.Any(p => bb[0].X <= p.X && bb[1].X >= p.X 
                        && bb[0].Y <= p.Y && bb[1].Y >= p.Y))
                    {
                        //ACD.DB._setLayer(id, "G-Text");

                        if (ACD.DB._isBlock(id) && !ACD.DB._getIdName(id).st_("GRID"))
                        {
                            ACD.WR("Block {0}", ACD.DB._getIdName(id));
                            ObjectIdCollection subIds = ACD.DB.ExplodeEntity(id);
                            foreach (ObjectId sid in subIds)
                                if ((ACD.DB._isPolyline(sid) || ACD.DB._isLine(sid)) && _intersect(sid, pts))
                                {
                                    //ids.Add(ACD.DB.CloneObjects(sid));
                                    res.Add(ACD.DB._getVertices(sid).ExtentLine(50));
                                }
                            ACD.DB.EraseObjects(subIds);
                        }
                        else if (ACD.DB._isPolyline(id) && _intersect(id, pts))
                        {
                            res.Add(ACD.DB._getVertices(id, splitarc).ExtentLine(50));
                           // ids.Add(ACD.DB.CloneObjects(id));
                        }
                    }
                }

            max_point_list = res.OrderBy(ls => -ls.Length()).First();
            
            GraphConsole.Compute(res);
            regions = GraphConsole.ResultPts.Where(ls => ls.Area() > 1000).ToCollectionSameClosed();
            //ACD.WR("Select Ids {0} Result {1}", IR.SelectedIds.Count, GraphConsole.ResultPts.Count);

            //PosCollection segments = pts.GetSegment(false);
            double[] indx = regions.Select(ls => 0.0).ToArray();

            railings = _cutIntersectSegment(max_point_list, pts)
                .OrderBy(ls => -ls.Length()).ToCollectionSameClosed();

            center_line = null;
            if (railings.Count > 1)
            {
                double d = railings[1][0].DistanceToPts(railings[0]);
                center_line = RailingOffset(railings[1], -d/2);

                if (center_line.Any(p => !p.Inside(max_point_list)))
                    center_line = RailingOffset(railings[1], d/2);
            }
            railings.Closed = railings.Select(ls => false).ToArray();
            //ACD.DB.DrawPolyline(railings, "LAYER=G-Dim");

            for (int i = 0; i < regions.Count; i++)
            {
                pPos[] reg = regions[i].Add(regions[i].First());
                double[] ints = reg.IntersectPts(pts, true, false, false)
                    .Select(p => p.openedPathParam(pts)).OrderBy(v => v).ToArray();

                if (ints.Length > 0)
                    indx[i] = ints.First();
            }

            regions = DE.NumericArray(0, regions.Count - 1)
                .OrderBy(n => indx[n]).Select(n => regions[n]).ToCollectionSameClosed();

            

            for (int i = 0; i < regions.Count; i++)
            {
                pPos pt = regions[i].CenterPoint();

                if (railings.Count > 0)
                {
                    pt.DistanceToPts(railings.First());
                    pt = pPos.DistanceTo_Projection.Along(200, pt);
                }
                ids.Add(ACD.DB.DrawPolyline(regions[i], true));
                ids.AddRange(_drawNumStep(i + 1, pt));
            }
            
            step_height = height / (regions.Count + 1);
            ids.Add(ACD.DB.CreateText("#LTotal=" + height +  "\r\nStep=" 
                + step_height.roundNumber(1), pts.First(), 100));

            return ids;
        }

        static ObjectIdCollection _drawNumStep(int i, pPos pt)
        {
            ObjectIdCollection ids = new ObjectIdCollection();
            ids.AddRange(ACD.DB.DrawCircle(pt, 100));
            ids.Add(ACD.DB.CreateText("#M" + i.ToString(), pt, 100));
            return ids;
        }

        static ObjectIdCollection CenterLineArrow( IEnumerable<pPos> pts)
        {
            if (center_line.First().DistanceTo(pts.Last()) < center_line.Last().DistanceTo(pts.Last()))
                center_line = center_line.Reverse();
            
            pPos p1 = center_line.Last();

            int i = center_line.Length - 2;
            while (p1._isVeryClosed(center_line[i]))
                i--;

            ObjectIdCollection ids = new ObjectIdCollection();
            ObjectIdCollection tmp = ACD.DB.DrawCircle (center_line.First(),50);

            ids.AddRange(ACD.DB.DrawHatchFromIds(tmp, "HPATTERN=SOLID"));
            ACD.DB.EraseObjects(tmp);

            if (i > 0)
            {
                pPos p2 = p1.Along(100, center_line[i]);
                pPos[] ls = new pPos[] { p1.Parallel(p2, 200).Last(), p1, p1.Parallel(p2, -200).Last() };

                ids.Add(ACD.DB.DrawPolyline(ls, false, "LAYER=A-Hatch|LWIDTH=50"));
            }
            return ids;
        }
        
        static double step_height;
        static PosCollection railings;
        static int splitarc = 0;
        static pPos railing_size = new pPos(0,0), cur_POS;
        static double level_height;

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                try
                {
                    splitarc = (int)pString.INI_String("SPLITARC").ToNumber();
                    level_height = pString.INI_String("IMPORT_STAIR_LEVEL_HEIGHT").ToNumber();
                    railing_size = pPos.FromString(pString.INI_String("STAIR_RAILING_SIZE").Replace("x",","));

                    if (railing_size.X == 0) railing_size.X = 100;
                    if (railing_size.Y == 0) railing_size.X = 50;

                    ObjectIdCollection selIds = ACD.GetSelection();

                    pPos[] curve_points = selIds.ToList()
                        .Where(id => id.isCurve()).Select(id => ACD.DB.GetCurvePoints(id).First()).ToArray();

                    pPos[] contents = ACD.DB.GetTextNearPoint(curve_points);

                    int i = 0;
                    
                    foreach (ObjectId id in selIds)
                        if(id.isCurve())
                        {
                            pPos[] pts = ACD.DB.GetCurvePoints(id);
                            pts = ACD.DB.GetCurvePoints(id, 50, false);
                            
                            pPos txt_pos = contents[i];
                            if (txt_pos != null && !txt_pos.Content.empty())
                                level_height = txt_pos.Content._firstProp().ToNumber(level_height);

                            cur_POS = txt_pos.Clone();

                            railings = new PosCollection();
                            ObjectIdCollection planIds = getEntFromCurvePoints(pts, level_height);
                            
                            if (center_line != null)
                            {
                                planIds.Add(ACD.DB.DrawPolyline(center_line, false, "LAYER=A-Hidden"));
                                planIds.AddRange(CenterLineArrow(pts));
                            }

                            if (railings.Count > 0)
                            {
                                foreach(pPos[] rail in railings)
                                {
                                    pPos[] ls1 = RailingOffset(rail, railing_size.X);
                                    
                                    if (ls1.Any(p => !p.Inside(max_point_list)))
                                        ls1 = RailingOffset(rail, -railing_size.X);

                                    planIds.Add(ACD.DB.DrawPolyline(ls1, false, "LAYER=A-Hidden"));
                                }
                            }

                            pPos[] bb = ACD.DB._getBound(planIds);
                            ACD.DB.MoveObject(planIds, cur_POS - bb[0]);
                            cur_POS.X += bb.Size().X + 2000;
                            
                            //SECTION

                            for (int axis = 0; axis < 2; axis ++)
                            {
                                ObjectIdCollection sectionIds = _drawSection(axis);

                                bb = ACD.DB._getBound(sectionIds);
                                ACD.DB.MoveObject(sectionIds, cur_POS - bb[0]);
                                //bb = ACD.DB._getBound(sectionIds);
                                //ACD.DB.DrawPolyline(bb.Rect(), true);
                                cur_POS.X += bb.Size().X + 2000;
                            }

                            i++;
                        }
                }
                catch (System.Exception ex)
                {
                    ACD.WR("Error {0},{1}", ex.Message, ex.StackTrace);
                }

                ACD.Focus();
            }
        }
    }
}