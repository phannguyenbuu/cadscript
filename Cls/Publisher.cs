using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Publishing;
using Autodesk.AutoCAD.Geometry;

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
//using System.Collections;
using System.Linq;
//using SyncObject;

namespace AcadScript
{
    public static class PublishTools
    {
        static void Publisher_AboutToBeginPublishing(object sender,
            Autodesk.AutoCAD.Publishing.AboutToBeginPublishingEventArgs e)
        {
            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(
                "\nAboutToBeginPublishing!!");
        }

        private static string _getPrefixCode(string st)
        {
            string[] ar = st.filter();
            return ar[0];
        }


        static public void PlotPDF(this Database db, string filename)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead);
                Layout lo = (Layout)tr.GetObject(btr.LayoutId, OpenMode.ForRead);

                PlotInfo pi = new PlotInfo();
                pi.Layout = btr.LayoutId;

                PlotSettings ps = new PlotSettings(lo.ModelType);
                ps.CopyFrom(lo);

                PlotSettingsValidator psv = PlotSettingsValidator.Current;
                psv.SetUseStandardScale(ps, false);

                psv.SetPlotType(ps, (Autodesk.AutoCAD.DatabaseServices.PlotType)GP.PLOTREGIONTYPE);
                
                string st = pString.INI_String("PAGESCALE");
                if (st.empty())
                    st = "1:50";

                double a = st.filter(":").First().ToNumber(), b = st.filter(":").Last().ToNumber();

                psv.SetPlotWindowArea(ps, GP.PLOTWINDOW);
                                
                psv.SetCustomPrintScale(ps, 
                    new CustomScale(st.filter(":").First().ToNumber(),
                    st.filter(":").Last().ToNumber()));
                psv.SetPlotCentered(ps, true);
                
                psv.SetPlotConfigurationName(ps, GP.PLOT_DEVICE, GP.PAGESETUP);

                pi.OverrideSettings = ps;
                double x = pi.OverrideSettings.PlotOrigin.X, X = pi.OverrideSettings.PlotOrigin.X;
                psv.SetPlotWindowArea(ps, new Extents2d(x - 210, X - 150, x + 210, X + 150));
                pi.OverrideSettings = ps;
                
                ACD.WR("Origin {0},{1}- Window [Min {2},{3}; Max {4},{5}]", 
                    pi.OverrideSettings.PlotOrigin.X, pi.OverrideSettings.PlotOrigin.X, 
                    pi.OverrideSettings.PlotWindowArea.MinPoint.X, pi.OverrideSettings.PlotWindowArea.MinPoint.X,
                    pi.OverrideSettings.PlotWindowArea.MaxPoint.X, pi.OverrideSettings.PlotWindowArea.MaxPoint.X);

                PlotInfoValidator piv = new PlotInfoValidator();
                piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                piv.Validate(pi);

                if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
                {
                    using (PlotEngine pe = PlotFactory.CreatePublishEngine())
                    {
                        using (PlotProgressDialog ppd = new PlotProgressDialog(false, 1, true))
                        {
                            ppd.set_PlotMsgString(PlotMessageIndex.DialogTitle, "Plot Progress [" + filename + "]");
                            ppd.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Cancel Job");
                            ppd.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "Cancel Sheet");
                            ppd.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Sheet Set Progress");
                            ppd.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "Sheet Progress");

                            ppd.LowerPlotProgressRange = 0;
                            ppd.UpperPlotProgressRange = 100;
                            ppd.PlotProgressPos = 0;

                            ppd.OnBeginPlot();
                            ppd.IsVisible = true;
                            pe.BeginPlot(ppd, null);

                            pe.BeginDocument(pi, Path.GetFileNameWithoutExtension(filename),
                                null, 1, true, filename);

                            ppd.OnBeginSheet();
                            ppd.LowerSheetProgressRange = 0;
                            ppd.UpperSheetProgressRange = 100;
                            ppd.SheetProgressPos = 0;

                            PlotPageInfo ppi = new PlotPageInfo();
                            
                            pe.BeginPage(ppi, pi, true, null);
                            pe.BeginGenerateGraphics(null);
                            pe.EndGenerateGraphics(null);
                            pe.EndPage(null);

                            ppd.SheetProgressPos = 100;
                            ppd.OnEndSheet();
                            pe.EndDocument(null);

                            ppd.PlotProgressPos = 100;
                            ppd.OnEndPlot();
                            pe.EndPlot(null);
                        }
                    }
                }
                else
                {
                    ACD.WR("\nAnother plot is in progress.");
                }
                tr.Commit();
            }
        }
                
        [DllImport("accore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "acedTrans")]
        static extern int acedTrans(Point3d point, IntPtr fromRb, IntPtr toRb, int disp, out Point3d result);

        static public void WindowPlot(this Database db, string filename, 
            pPos pt1, pPos pt2, string scale, pPos offset = null)
        {


            ResultBuffer rbFrom = new ResultBuffer(new TypedValue(5003, 1)), rbTo = new ResultBuffer(new TypedValue(5003, 2));

            Point3d p1 = Point3d.Origin, p2 = Point3d.Origin;

            acedTrans(pt1.ToPoint3(), rbFrom.UnmanagedObject, rbTo.UnmanagedObject,0,out p1);
            acedTrans(pt2.ToPoint3(), rbFrom.UnmanagedObject,rbTo.UnmanagedObject,0,out p2);

            Extents2d window = new Extents2d(p1.X,p1.Y,p2.X,p2.Y);




            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead);
                Layout lo = (Layout)tr.GetObject(btr.LayoutId, OpenMode.ForRead);
                
                PlotInfo pi = new PlotInfo();
                pi.Layout = btr.LayoutId;

                PlotSettings ps = new PlotSettings(lo.ModelType);

                ps.CopyFrom(lo);


                ACD.WR($"\nScale parsed: 1:{scale:F2}");
                ACD.WR($"\nWindow size: {window.MaxPoint.X - p1.X:F2}x{p2.Y - p1.Y:F2}");
                ACD.WR($"\nCurrent plot config: {ps.PlotConfigurationName}");


                PlotSettingsValidator psv = PlotSettingsValidator.Current;

                //psv.SetPlotPaperUnits(ps, PlotPaperUnit.Millimeters);
                psv.SetPlotWindowArea(ps, window);
                psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Window);
                psv.SetUseStandardScale(ps, false);
                //psv.SetStdScaleType(ps, StdScaleType.ScaleToFit);

                //double scale_inches = GP.PLOT_SCALE_MULTIPLY;

                //double a = scale.filter(":").First().ToNumber();
                double b = scale.filter(":").Last().ToNumber() * GP.PLOT_SCALE_MULTIPLY;
                //ACD.WR("Plot Scale = {0}:{1}", a, b);
                psv.SetCustomPrintScale(ps, new CustomScale(1, b));

                if(offset == null)
                    psv.SetPlotCentered(ps, true);
                else
                {
                    psv.SetPlotCentered(ps, false);
                    psv.SetPlotOrigin(ps, new Point2d(offset.X, offset.Y));
                }

                //psv.SetPlotConfigurationName(ps, pString.INI_String("PlotDevice"), pString.INI_String("PageSetup"));
                
                pi.OverrideSettings = ps;

                PlotInfoValidator piv = new PlotInfoValidator();
                piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                piv.Validate(pi);

                if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
                {
                    using (PlotEngine pe = PlotFactory.CreatePublishEngine())
                    {
                        using (PlotProgressDialog ppd = new PlotProgressDialog(false, 1, true))
                        {
                            ppd.set_PlotMsgString(PlotMessageIndex.DialogTitle, Path.GetFileNameWithoutExtension(filename));
                            ppd.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Cancel Job");
                            ppd.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "Cancel Sheet");
                            ppd.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Sheet Set Progress");
                            ppd.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "Sheet Progress");

                            ppd.LowerPlotProgressRange = 0;
                            ppd.UpperPlotProgressRange = 100;
                            ppd.PlotProgressPos = 0;

                            ppd.OnBeginPlot();
                            ppd.IsVisible = true;
                            pe.BeginPlot(ppd, null);

                            pe.BeginDocument(pi, Path.GetFileNameWithoutExtension(filename),
                                null, 1, true, filename);

                            ppd.OnBeginSheet();
                            ppd.LowerSheetProgressRange = 0;
                            ppd.UpperSheetProgressRange = 100;
                            ppd.SheetProgressPos = 0;

                            PlotPageInfo ppi = new PlotPageInfo();
                            
                            pe.BeginPage(ppi, pi, true, null);
                            pe.BeginGenerateGraphics(null);
                            pe.EndGenerateGraphics(null);
                            pe.EndPage(null);

                            ppd.SheetProgressPos = 100;
                            ppd.OnEndSheet();
                            pe.EndDocument(null);

                            ppd.PlotProgressPos = 100;
                            ppd.OnEndPlot();
                            pe.EndPlot(null);
                        }
                    }
                }
                else
                    ACD.WR("\nAnother plot is in progress.");

                tr.Commit();
            }
        }
    }
}
