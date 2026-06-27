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
    public class PlanningCeilingCLS
    {
        static ObjectId DrawBulgePolyline(IEnumerable<pPos> ls, string style = null)
        {
            ObjectId res = ObjectId.Null;

            using (Transaction tr = ACD.DB.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(ACD.DB.BlockTableId, OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                
                Polyline pl = _bulgePolyline(ls, 0.4);
                
                res = btr.AppendEntity(pl);
                tr.AddNewlyCreatedDBObject(pl, true);

                if (!style.empty())
                    ACD.DB._setIdInfo(res, style);

                tr.Commit();
            }
            return res;
        }
        static Polyline _bulgePolyline(IEnumerable<pPos> ls, double _bulgevalue, 
            bool closed = true)
        {
            Polyline pl = new Polyline(ls.Count());
            
            for (int i = 0; i < ls.Count(); i++)
            {
                int prev = i > 0 ? i - 1 : ls.Count() - 1;
                int nex = (i + 1) % ls.Count();
                
                if (!closed)
                {
                    if (i == 0)
                        pl.AddVertexAt(i, ls.First().ToPoint2(), 0, 0, 0);
                    else if (i == ls.Count() - 1)
                        pl.AddVertexAt(i, ls.ElementAt(i).ToPoint2(), 0, 0, 0);
                    else
                        pl.AddVertexAt(i, ls.ElementAt(i).ToPoint2(), _bulgevalue, 0, 0);
                }
                else
                    pl.AddVertexAt(i, ls.ElementAt(i).ToPoint2(), _bulgevalue, 0, 0);

                //if (!closed && i == ls.Count() - 1)
                //    pl.AddVertexAt(i + 1, ls.Last().ToPoint2(), 0, 0, 0);
                //else
                //    pl.AddVertexAt(i + 1, ls.ElementAt(i).ToPoint2(), 0, 0, 0);


            }
            
            return pl;
        }
        
        static pPos[] GetInternalRect(PosCollection wallpts, pPos txt_pt, double rotation)
        {
            pPos base_vector = new pPos(Math.Cos(rotation), Math.Sin(rotation));
            List<pPos> res = new List<pPos>();

            for (int axis = 0; axis < 2; axis++)
                res.AddRange(wallpts.GetMinDimArea(txt_pt, axis, base_vector));

            pPos[] r = null;

            if (res.Count >= 4)
                r = res.Rect();
            return r;
        }
        
        static pPos[] _cellDivide(IEnumerable<pPos> region, double size)
        {
            pPos[] bb = region.Boundary();
            
            List<pPos> res = new List<pPos>();
            pPos sz = new pPos(1, 1);

            for(int ax = 0; ax <2; ax ++)
            {
                int n = (int)Math.Ceiling((bb[1][ax] - bb[0][ax]) / size);
                double m = (bb[1][ax] - bb[0][ax]) / n;

                if (m < size - 200)
                    n--;

                sz[ax] = n;
            }
            
            pPos ct = bb.CenterPoint();
            PosCollection segs = region.GetSegment(true);

            List<int[]> indexes = new List<int[]> { new int[] { 0, 1, 3},
                new int[] { 1, -1, 0 }, new int[] { 0, -1, 1 }, new int[] { 1, 1, 2 } };
            
            foreach(int[] indx in indexes)
            {
                int axis = indx[0];
                int nex = (axis + 1) % 2;

                int inf = indx[1];
                double v = bb[inf == -1 ? 1 : 0][axis];

                List<pPos> pts = new List<pPos>();
                res.Add(region.ElementAt(indx[2]));

                for (int i = 0; i < sz[axis] - 1; i++)
                {
                    v += inf * (bb[1][axis] - bb[0][axis]) / sz[axis];

                    pPos p1 = new pPos(0, 0), p2 = new pPos(0, 0);
                    p1[axis] = p2[axis] = v;
                    p1[nex] = bb[inf == -1 ? 1 : 0][nex];
                    p2[nex] = ct[nex];

                    foreach (pPos[] seg in segs)
                    {
                        pPos p3 = p1.Intersect(p2, seg[0], seg[1], true);
                        if (p3 != null)
                        {
                            pts.Add(p3);
                            break;
                        }
                    }
                }

                res.AddRange(pts.OrderBy(p => (axis == 1 ? -1 : 1) * p[axis] * inf));
            }

            return res.ToArray();
        }

        public static ObjectIdCollection DrawLineAndSwitch(pPos[] block_list_pos, pPos switch_pos)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            //pPos[] block_list_pos = _cellDivide(block_border, block_range);
            //ACD.WR("OK1");
            switch_pos.DistanceToPts(block_list_pos);
            
            if(pPos.DistanceTo_Index == -1)
            {
                if (block_list_pos.First().DistanceTo(switch_pos) < block_list_pos.Last().DistanceTo(switch_pos))
                    block_list_pos = block_list_pos.Reverse().ToArray();
            }
            else
            block_list_pos = block_list_pos.SetStartIndex(pPos.DistanceTo_Index);
            //ACD.WR("OK3");
            res.Add(DrawBulgePolyline(block_list_pos.Add(switch_pos), "LAYER=E-Line"));
            //ACD.WR("OK4");
            if (!switch_pos.Content.empty())
            {
                res.Add(ACD.DB.Insert(switch_pos.Content, switch_pos, "LAYER=E-Equiq"));
                ACD.DB._setRotation(res.Last(), switch_pos.Rotation);
            }
            //ACD.WR("OK5");
            return res;
        }

        static ObjectIdCollection BuildCeilingAround(IEnumerable<pPos> r,
            string block_name, pPos center_offset, pPos switch_pos,
            double around_offset, double block_offset, double block_range)
        {
            ObjectIdCollection res = new ObjectIdCollection();

            pPos[] border = r.Offset(-around_offset);
            pPos[] block_border = r.Offset(block_offset - around_offset);
            pPos[] block_list_pos = _cellDivide(block_border,block_range);

            //switch_pos.DistanceTo(block_list_pos);
            //block_list_pos = block_list_pos.Rotate(pPos.DistanceTo_Index);
            //res.Add(DrawBulgePolyline(block_list_pos.Add(switch_pos), "LAYER=E-Line"));

            //if (!switch_pos.Content.empty())
            //{
            //    res.Add(ACD.DB.Insert(switch_pos.Content,
            //        switch_pos, "LAYER=E-Equiq"));
            //    ACD.DB._setRotation(res.Last(), switch_pos.Rotation);
            //}

            //for (int i = 0; i < block_list_pos.Length; i++)
            //    ACD.DB.CreateText(i.ToString(), block_list_pos[i], 200);

            res.AddRange(DrawLineAndSwitch(block_list_pos, switch_pos));

            res.Add(ACD.DB.DrawPolyline(border, true, "LAYER=E-Border"));
            res.Add(ACD.DB.DrawPolyline(block_border, true, "LAYER=E-Hidden"));
            res.AddRange(ACD.DB.Insert(block_name, block_list_pos, "LAYER=E-Equiq"));

            if (center_offset == null)
                center_offset = new pPos(0, 0);

            if (!center_offset.Content.empty())
            {
                res.Add(ACD.DB.Insert(center_offset.Content,
                    r.CenterPoint() + center_offset, "LAYER=E-Equiq"));
                ACD.DB._setRotation(res.Last(), center_offset.Rotation);
            }

            

            res.Add(ACD.DB.DrawPolyline(new pPos[] { border.ElementAt(0), border.ElementAt(2) }, true, "LAYER=E-Hidden"));
            res.Add(ACD.DB.DrawPolyline(new pPos[] { border.ElementAt(1), border.ElementAt(3) }, true, "LAYER=E-Hidden"));

            return res;
        }
        
        static string[] blocknames;
        static string FindBlock(string blockkey)
        {
            
            string blockname = null;

            if (blocknames.Length > 0)
            {
                int index = Array.FindIndex(blocknames, s 
                    => s.st_("E") && s.Upper().Contains(blockkey.Upper()));

                if (index != -1)
                    blockname = blocknames[index];
            }

            return blockname;
        }


        static pPos _findMinProjectionPoint(PosCollection pls, pPos pt)
        {
            int min_index = 0;
            double min = double.PositiveInfinity;
            pPos res = pls.First().First();

            for(int i = 0;i < pls.Count; i++)
            {
                double d = pt.DistanceToPts(pls[i]);
                if(d < min)
                {
                    min = d;
                    min_index = i;
                    res = pls[i][pPos.DistanceTo_Index];
                }
            }

            return res;
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                bool bmode = ACD.ControlHold;
                ACD.Focus();
                blocknames = ACD.DB.ListBlock();

                if (bmode)
                {
                    pPos[] pts = ACD.GetSelection().ToList().Where(id 
                        => ACD.DB._isPoint(id)).Select(id => ACD.DB._getPoint(id)).ToArray();

                    ACD.WR("Select switch pos and rotation");
                    pPos switch_pos = ACD.GetPoint();
                    pPos switch_pos_2 = ACD.GetPoint();

                    if (switch_pos_2 != null)
                        switch_pos.Rotation = (switch_pos - switch_pos_2).AngleAxisVsVector(new pPos(1, 0));
                    switch_pos.Content = FindBlock("Switch");
                    
                    PlanningCeilingCLS.DrawLineAndSwitch(pts, switch_pos);
                }
                else
                {
                    double around_offset = ACD.ED.GetInputString("Around Offset", "800").ToNumber(800);
                    string bkey = ACD.ED.GetInputString("Down", "Down");
                    ACD.WR("Select internal point");
                    pPos[] pts = ACD.GetPickPts();

                    ACD.WR("Select switch pos and rotation");
                    pPos switch_pos = ACD.GetPoint();
                    pPos switch_pos_2 = ACD.GetPoint();

                    ACD.DB.GetEntities(IZone.ViewZoneIndex, EN_SELECT.AC_DXF, "AEC_WALL");
                    ObjectIdCollection selIds = IR.SelectedIds;
                    PosCollection regions = new PosCollection();

                    //ACD.WR("Region {0} {1}", ACD.ViewZoneIndex, regions.Count);

                    
                    string block_name = FindBlock(bkey);
                    switch_pos.Content = FindBlock("Switch");

                    foreach (ObjectId lwpId in selIds)
                        if (ACD.DB._isWall(lwpId))
                        {
                            ObjectId wallId = ACD.DB.GetWallShape(lwpId);
                            regions.Add(ACD.DB._getVertices(wallId));
                            ACD.DB.EraseObject(wallId);
                        }

                    foreach (pPos pt in pts)
                        if (!block_name.empty())
                        {
                            double block_offset = block_name.filter("_").Last().ToNumber(200);// ACD.ED.GetInputString("Block Offset", "200").ToNumber(200);
                            pPos center_offset = new pPos(0, 0); // new pPos(ACD.ED.GetInputString("Center Offset", "0,0"));

                            center_offset.Content = FindBlock("Center");

                            double block_range = 1200;// ACD.ED.GetInputString("Block Range", "1200").ToNumber(1200);
                            double rotation = 0; // ACD.ED.GetInputString("Rect Rotation", "0").ToNumber();

                            pPos[] r = GetInternalRect(regions, pt, rotation);

                            if (switch_pos == null)
                                switch_pos = r.First();

                            if (switch_pos_2 != null)
                                switch_pos.Rotation = (switch_pos - switch_pos_2).AngleAxisVsVector(new pPos(1, 0));
                            //ACD.WR("Angle {0}", switch_pos.Rotation);

                            if (r != null)
                                BuildCeilingAround(r, block_name, center_offset,
                                    switch_pos, around_offset, block_offset, block_range);
                        }
                        else
                            ACD.WR("Cannot find block {0}", block_name);
                }
            }    
            
            ACD.Focus();
        }
    }
}

