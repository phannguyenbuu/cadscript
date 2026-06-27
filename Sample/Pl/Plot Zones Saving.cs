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

//
using System.Windows.Forms;
//using SyncObject;
    
namespace AcadScript
{
    public class PlotZoneSaving
    {
       
        public static void StartMonitor()
        {
            try
            {
                StopMonitor();
            }
            catch { }

            PointMonitorEventHandler fn = new PointMonitorEventHandler(ed_PointMonitor);
            
            ACD.ED.PointMonitor += fn; 
        }

        public static void StopMonitor()
        {
            ACD.ED.PointMonitor -= new PointMonitorEventHandler(ed_PointMonitor);
        }

        static string _setObjectIdsInfo(PointMonitorEventArgs e)
        {
            short pickbox = (short)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("PICKBOX");
            Point2d extents = e.Context.DrawContext.Viewport.GetNumPixelsInUnitSquare(e.Context.ComputedPoint);

            double boxWidth = pickbox / extents.X;

            Vector3d vec = new Vector3d(boxWidth / 2, boxWidth / 2, 0.0);

            PromptSelectionResult pse = ACD.ED.SelectCrossingWindow(
                e.Context.ComputedPoint - vec, e.Context.ComputedPoint + vec);

            if (pse.Status != PromptStatus.OK || pse.Value.Count <= 0)
                return null;

            string info = "";
            
            using (Transaction tr = ACD.DB.TransactionManager.StartTransaction())
            {
                ObjectId[] ids = pse.Value.GetObjectIds();


                foreach (ObjectId id in ids)
                    if (ACD.DB._isPolyline(id))
                    {
                        info += id.ObjectClass.DxfName + "'s length: " +
                            string.Format("{0:F}", ACD.DB._getLength(id)) + "\r\n"
                            + e.Context.ComputedPoint.X.roundNumber() 
                            + "," + e.Context.ComputedPoint.Y.roundNumber();
                    }
                

                tr.Commit();
            }

            return info;
        }

        static void ed_PointMonitor(object sender, PointMonitorEventArgs e)
        {
            if (!e.Context.PointComputed)
                return;

            try
            {
                string info = "";

                PosCollection zones = ACD.DB.GetDrawingZones();
                pPos p = new pPos(e.Context.ComputedPoint.X, e.Context.ComputedPoint.Y);
                PosCollection pls = zones.Where(r => p.InsideRect(r[0],r[1])).ToCollectionSameClosed();

                if (pls.Count > 0)
                    info += pls.First().First().Content + "\r\n";

                if (info != "")
                    e.AppendToolTipText(info);
            }
            catch
            {
            }
        }

        static pPos[] textpos;

        public static void Main(string[] args)
        {
            //PDFEditorCLS.WriteXmpMetadata(@"D:\Dropbox\_Documents\House\C Phuong Thu Duc\plot\[4-4]MẶT BẰNG TRỆT.pdf", "Subject=!Plot");
            //ACD.WR("PLOT_LOAD 1");
            using (ACD.Lock())
            {
                var result = MessageBox.Show("Select new zones", "Update zones?", MessageBoxButtons.YesNoCancel);

                if (result != DialogResult.Cancel)
                {
                    if (result == DialogResult.Yes)
                        ACD.DB.SetDrawingZones();
                    
                    StartMonitor();
                }
            }
            ACD.Focus();
        }
    }
}

