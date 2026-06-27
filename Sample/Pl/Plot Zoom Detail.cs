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
    public class PoltZoomDetailCLS
    {
        
        static PosCollection GetDetailZoomZone(ObjectIdCollection selIds)
        {
            ObjectIdCollection regionIds = ACD.DB.FilterIds(selIds, "INSERT");
            PosCollection regions = new PosCollection();

            foreach (ObjectId id in regionIds)
            {
                ObjectIdCollection objIds = ACD.DB.ExplodeEntity(id);
                foreach (ObjectId objId in objIds)
                    if(ACD.DB._isPolylineClosed(objId) && ACD.DB._getVertices(objId).Length == 4)
                    regions.Add(ACD.DB._getBound(objId));
                ACD.DB.EraseObjects(objIds);
            }

            regions = regions//.OrderBy(ls => ls.Area())
                .OrderBy(ls => ls.Boundary()[0].X.roundNumber(100))
                .ThenBy(ls => ls.Boundary()[0].Y.roundNumber(100)).ToCollectionSameClosed();

            pPos[] bb = ACD.DB._getBound(selIds);
            ACD.DB.GetEntities(bb, EN_SELECT.AC_DXF, "MTEXT", "TEXT");

            List<pPos> text_pts = IR.SelectedIds.Cast<ObjectId>().Where(id 
                => ACD.DB._getContent(id).StartsWith("."))
                .Select(id => ACD.DB._getPoint(id)).ToList();
                        
            List<int> found_index = new List<int>();
            
            for (int i = 0; i < regions.Count; i++)
            {
                pPos[] bb_rect = regions[i].Boundary();
                int index = text_pts.FindIndex(pt => pt.InsideRect(bb_rect[0], bb_rect[1]));

                if(index != -1 && !found_index.Contains(index))
                {
                    regions[i][0].Content = text_pts[index].Content;
                    found_index.Add(index);
                }
            }
            
            PosCollection res = regions.Where(ls 
                => !ls[0].Content.empty()).Select(ls => ls).ToCollectionSameClosed();

            PosCollection region_ls2 = regions.Where(ls
                => ls[0].Content.empty()).Select(ls => ls).ToCollectionSameClosed();

            for (int i = 0; i < res.Count; i++)
                if(region_ls2.Count > i)
                    res[i][1].Content = region_ls2[i].ToText();

            return res;
        }

        static void _buildPlotItems(PosCollection regions)
        {
            List<iPlotItemCLS> plotitems = new List<iPlotItemCLS>();
            
            foreach (pPos[] ls in regions)
            {
                pPos[] zone = ACD.DB.GetDrawingZone(ls[0]);
                double scale = zone.BoundScale(GP.PAGESIZE.X, GP.PAGESIZE.Y).filter(":").Last().ToNumber();

                string st1 = ls[0].Content;
                string st2 = ls[1].Content;

                if (!st1.empty() && !st2.empty())
                {
                    pPos[] dest_pts = new PosCollection(st2)[0];
                    pPos p1 = ls.MidPts()[0], p2 = dest_pts.MidPts()[0];

                    ACD.DB.DrawPolyline(new pPos[] { p1, p2 }, false);
                    ACD.DB.DrawCircle(p1, 200);

                    int sc = (int)(scale * ls.Size().X / dest_pts.Size().X);
                    ACD.DB.CreateText("1:" + sc, p2);

                    plotitems.Add(new iPlotItemCLS(ls, st1, null, "1:" + sc));
                }
            }
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ACD.WR("OK1");
                
                ACD.WR("OK2");
                //PosCollection regions = new PosCollection();
                //var result = MessageBox.Show("Update zones?", "Zones", MessageBoxButtons.YesNoCancel);

                //if (result != DialogResult.Cancel)
                //{
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    PosCollection regions = GetDetailZoomZone(selIds);
                    //ACD.WR("<Regions>\r\n{0}", regions);

                    
                        //List<PlotItemCLS> plotitems = CollectPlotGroups(regions).ToList();

                    //string[] ls = plotitems.Select(itm => itm.Title
                    //    + (!itm.Title.ToUpper().Contains("PHẦN BẢN VẼ") ? " - " + itm.scale : "")).ToArray();
                    // " - refs " + itm.References.Count + " file(s)"

                    //ScheduleForm frm = new ScheduleForm(ls);

                    //frm.ButtonClick += (o, e) =>
                    //{
                    //    frm.Hide();

                    //    if (frm.OK)
                    //        using (ACD.Lock())
                    //        {
                    //            //string prefix = ACD.ED.GetInputString("Prefix", "A");
                    //            PlotGroup(plotitems, frm.Prefixes);
                    //            ACD.Focus();
                    //        }

                    //    frm.Close();
                    //};

                    //frm.Show();
                }
                //}
            }
            ACD.Focus();
        }
    }
}

