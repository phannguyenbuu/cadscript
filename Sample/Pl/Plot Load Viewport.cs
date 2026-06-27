using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Publishing;
using Autodesk.AutoCAD.Geometry;

using System.Runtime.InteropServices;
//
//using System.Windows.Forms;
//using SyncObject;

namespace AcadScript
{
    public static class CustomPlotEngine
    {

        [DllImport("accore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "acedTrans")]
        static extern int acedTrans(Point3d point, IntPtr fromRb, IntPtr toRb, int disp, out Point3d result);

        static public void WindowPlot(string filename,
            pPos pt1, pPos pt2, string scale, pPos offset = null)
        {
            Database db = ACD.DB;

            ResultBuffer rbFrom = new ResultBuffer(new TypedValue(5003, 1)), rbTo = new ResultBuffer(new TypedValue(5003, 2));

            Point3d p1 = Point3d.Origin, p2 = Point3d.Origin;

            acedTrans(pt1.ToPoint3(), rbFrom.UnmanagedObject, rbTo.UnmanagedObject, 0, out p1);
            acedTrans(pt2.ToPoint3(), rbFrom.UnmanagedObject, rbTo.UnmanagedObject, 0, out p2);

            Extents2d window = new Extents2d(p1.X, p1.Y, p2.X, p2.Y);




            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead);
                Layout lo = (Layout)tr.GetObject(btr.LayoutId, OpenMode.ForRead);

                PlotInfo pi = new PlotInfo();
                pi.Layout = btr.LayoutId;

                PlotSettings ps = new PlotSettings(lo.ModelType);

                ps.CopyFrom(lo);





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

                if (offset == null)
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


                ACD.WR("OK2");
                try
                {
                    ACD.WR(string.Format("=== PLOT DEBUG ==="));
                    ACD.WR(string.Format("Window: ({0:F2},{1:F2}) -> ({2:F2},{3:F2})", p1.X, p1.Y, p2.X, p2.Y));
//                    ACD.WR(string.Format("Window Area: {0:F2}", window.Area));
                    ACD.WR(string.Format("Scale Num: {0:F2}", scale));
//                    ACD.WR(string.Format("CustomScale: {0}", ps.CustomPrintScale.CustomScale));
                    ACD.WR(string.Format("PlotType: {0}", ps.PlotType));
                    ACD.WR(string.Format("Paper: {0}", ps.CanonicalMediaName));
                    ACD.WR(string.Format("Layout: {0}", lo.LayoutName));

                    piv.Validate(pi);
                    ACD.WR("✅ PlotInfo VALID!");
                }
                catch (System.Exception ex)
                {
                    ACD.WR(string.Format("❌ Validate FAIL: {0}", ex.Message));
                }



                piv.Validate(pi);
                ACD.WR("OK3");

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

    public class iPlotItemCLS
    {
        const double PAGE_BASE_SCALE = 105;

        static string NormalizePageScale(string scaleText)
        {
            double denominator = scaleText.filter(":").Last().ToNumber();
            return "1:" + NormalizePageScaleDenominator(denominator).ToString();
        }

        static int NormalizePageScaleDenominator(double rawDenominator)
        {
            if (rawDenominator <= 0)
                return (int)PAGE_BASE_SCALE;

            double[] factors = new double[] { 0.1, 0.2, 0.25, 0.5, 1 };
            var candidates = factors
                .Select(f => PAGE_BASE_SCALE * f)
                .Distinct()
                .OrderBy(v => v)
                .ToArray();

            double ratio = rawDenominator / PAGE_BASE_SCALE;
            int magnitude = (int)Math.Pow(10, Math.Floor(Math.Log10(Math.Max(ratio, 0.0000001))));
            if (magnitude < 1) magnitude = 1;

            List<double> scaledCandidates = new List<double>();
            foreach (double c in candidates)
                foreach (int m in new int[] { magnitude / 10, magnitude, magnitude * 10 })
                    if (m > 0)
                        scaledCandidates.Add(c * m);

            double best = scaledCandidates
                .Distinct()
                .OrderBy(v => Math.Abs(v - rawDenominator))
                .FirstOrDefault();

            if (best <= 0) best = PAGE_BASE_SCALE;
            return (int)Math.Round(best);
        }

        public string PLOT_SCALE_PAGE
        {
            get
            {
                string res = "1:" + ((int)PAGE_BASE_SCALE).ToString();
                try
                {
                    using (var tr = ACD.DB.TransactionManager.StartTransaction())
                    {
                        var layoutMgr = Autodesk.AutoCAD.DatabaseServices.LayoutManager.Current;
                        var layoutId = layoutMgr.GetLayoutId(layoutMgr.CurrentLayout);
                        var layout = (Autodesk.AutoCAD.DatabaseServices.Layout)tr.GetObject(layoutId, Autodesk.AutoCAD.DatabaseServices.OpenMode.ForRead);
                        var s = layout.CustomPrintScale;
                        if (s.Numerator > 0)
                            res = NormalizePageScale("1:" + Math.Round(s.Denominator / s.Numerator).ToString());
                        tr.Commit();
                    }
                }
                catch {}
                return res;
            }
        }

        public pPos[] MainRegion;
        public List<iPlotItemCLS> References;
        public List<string> infors;
        public string scale = null;
        double page_w = 0, page_h = 0;
        //public pPos[] TitleBlockContents;
        

        static string LoadOverlay(string[] templatefiles, string pdffile, string prams,
            string fontname, float fontheight, float overlay_opacity = 1f)
        {
            string res = null;
            //ACD.WR("P3.0.1");
            //clsPDFAddText pdf = new clsPDFAddText(pdffile);
            //ACD.WR("P3.0.2");
            foreach (string templatefile in templatefiles)
                if (File.Exists(templatefile))
                {
                    PosCollection pls = new PosCollection();// PDFHelper.GetTextExtraction(templatefile);

                    if (pls.Count > 0)
                    {
                        pPos[] pts = pls.First();
                        string content = null;

                        if (pts.Length > 0)
                        {
                            pts = pts.Where(p => p.Content.StartsWith(">>>")).ToArray();
                            if (pts.Length > 0)
                                content = pts.First().Content.Replace(">>>", "");
                        }

                        if (!content.empty())
                        {
                            //ACD.WR("PRAMS {0}", prams);

                            foreach (string propname in content._allPropNames())
                            {
                                pPos pt = pPos.FromString(content._prop(propname));
                                string val = prams._prop(propname);

                                //ACD.WR("Propname {0}={1} at {2}", propname, val, pt);

                                pt.Content = val;

                                //if (pt != null && !pt.Content.empty())
                                    //pdf.AddText(1, pt, fontname, fontheight);
                            }
                        }
                    }

                    //ACD.WR("P3.0.3");

                    ACD.PLOT_OVERLAY = overlay_opacity;
                    //pdf.Overlay(templatefile);
                }

            return res;
        }


        static void _overlayPDF(PdfPage page, string[] overlayFiles, IEnumerable<pPos> pts = null)
        {
            for (int i = 0; i < overlayFiles.Length; i++)
                using(XPdfForm xdf = XPdfForm.FromFile(overlayFiles[i]))
                    using (XGraphics gfx = XGraphics.FromPdfPage(page, XGraphicsUnit.Point))
                    {
                        pPos pt = new pPos(0, 0);

                        if (pts != null && pts.Count() > i)
                            pt = pts.ElementAt(i);

                        ACD.WR("Rect {0},{1},{2},{3} - {4}",
                            pt.X, pt.Y, page.Width, page.Height, overlayFiles[i]);

                        gfx.DrawImage(xdf, new XRect(pt.X, pt.Y, page.Width, page.Height), 
                            new XRect(0,0, page.Width, page.Height), XGraphicsUnit.Point);
                    }
        }

        public string XNotes;

        static pPos _pointFr(string[] formats, string key)
        {
            string title_pos = formats._props(key);

            pPos pt = new pPos(0, 0);
            if (!title_pos.empty())
                pt = pPos.FromString(title_pos.Replace(" ", ","));

            return pt;
        }



        static string[] file_ref_list(string overlay_file_txt, ref pPos[] pts)
        {
            List<string> res = new List<string>();
            string[] ar = overlay_file_txt.filter(";");
            pPos pt = new pPos(0, 0);

            for (int i = 0; i < ar.Length; i++)
            {
                string ch = ar[i];
                string s1 = ar[i], s2 = "";

                if (ch.Contains("(") && ch.Trim().EndsWith(")"))
                {
                    s1 = ch._getBeforeComma();
                    s2 = ch._getInComma();
                }
                    
                if (!s2.empty())
                    pt = pPos.FromString(s2.Replace(" ", ","));

                pts = pts.Add(pt);

                if (s1.ToLower() == "#template")
                    res.Add(@"D:\Dropbox\CADLib\PDF\TitleBlock_A3.pdf");
                else if (File.Exists(s1))
                    res.Add(s1);
            }

            return res.ToArray();
        }

        public iPlotItemCLS(IEnumerable<pPos> region, string _title,
            IEnumerable<string> txts = null, string _scale = null)
        {
            page_w = GB.PLOT_PAGE_W;
            page_h = GB.PLOT_PAGE_H;

            MainRegion = region.ToArray();
            MainRegion.First().Content = _title;
            References = new List<iPlotItemCLS>();
            infors = new List<string>();
            scale = _scale;

            if (scale.empty()) scale = PLOT_SCALE_PAGE; //PLOT SCALE PAGE
                //scale = MainRegion.BoundScale(page_w, page_h);
            //ACD.WR("Scale {0}", scale);
            if (txts != null)
                infors = txts.ToList();
        }

        string _translateContent(string prefix, string src)
        {
            string[] ar = src.Replace(">>>", "").filter("\r\n");
            string res = "|";
            if (ar.Length == 1)
                res += ar.First();
            else
                for (int i = 0; i < ar.Length; i++)
                    res += prefix + (i + 1).ToString() + "=" + ar[i] + "|";

            return res;
        }

        public string Description
        {
            get
            {
                string res = "|TITLE=" + MainRegion.First().Content;

                for (int i = 0; i < infors.Count; i++)
                    res += _translateContent(((char)(i + 64)).ToString(), infors[i]) + "|";

                return res;
            }
        }

        public string[] GetReferenceFilesInTitle(string key)
        {
            List<string> res = new List<string>();
            string desc = Description;

            string[] files = Directory.GetFiles(@"D:\Dropbox\CADLib\PDF\", "*.pdf");
            string[] ar = Description._prop(key).filter(";");
            //ACD.WR("ITM: {0} KEY_TITLE: {1}", Title, Description._prop(key));

            foreach (string s in ar)
            {
                int index = Array.FindIndex(files, f
                    => System.IO.Path.GetFileNameWithoutExtension(f).Upper().Contains(s.Upper()));
                //&& System.IO.Path.GetFileNameWithoutExtension(f).st_(key.Upper()));

                if (index != -1)
                    res.Add(files[index]);
            }

            return res.ToArray();
        }

        public PosCollection InRefRegions
        {
            get
            {
                PosCollection res = null;

                if (!Description._prop("INREF").empty())
                {
                    string st = (Description._prop("INREF") + "|" + MainRegion.ToInfo()).ReplaceEquation();
                    res = new PosCollection(st);
                }

                return res;
            }
        }

        public PosCollection RefRegions
        {
            get
            {
                PosCollection res = null;

                if (!Description._prop("REF").empty())
                {
                    string st = (Description._prop("REF") + "|" + MainRegion.ToInfo()).ReplaceEquation();
                    res = new PosCollection(st);
                }

                return res;
            }
        }

        public string[] AdditionFiles
        {
            get
            {
                return GetReferenceFilesInTitle("ADD");
            }
        }

        public string[] EndFiles
        {
            get
            {
                return GetReferenceFilesInTitle("END");
            }
        }

        public string[] OverlayFiles
        {
            get
            {
                return GetReferenceFilesInTitle("OVER");
            }
        }

        public string[] ReferencePrefix
        {
            get
            {
                List<string> res = new List<string>();
                foreach (iPlotItemCLS itm in References)
                    res.Add(this.Title.Substring(itm.Title.Length).Trim());
                return res.Distinct().ToArray();
            }
        }



        public string Plot(string dir, string prefix, int _page_index)
        {
            ACD.DB.GetEntities(null, EN_SELECT.AC_NAME, GB.TITLE_BLOCKS);
            ObjectId block_title_id = ObjectId.Null;

            if (IR.SelectedIds.Count > 0)
                block_title_id = IR.SelectedIds.First();

            pPos p1, p2;
            double sc = scale.filter(":").Last().ToNumber();

            p1 = MainRegion[0];
            p2 = MainRegion[1];

            string desc = Description;

            string fname = Title.filter("\r\n:;,|/&~!@#$^&*()[]").First();
            string pdf_name = dir + @"\[" + prefix + "]" + fname + ".pdf";

            pdf_name = pdf_name.Replace(@"\\\", "\\").Replace(@"\\", "\\").Replace(">", "");

            ACD.WR("View {0},{1} [FILENAME={2}][SCALE={3}]{4}",
                MainRegion[0], MainRegion[1], pdf_name, scale, desc);
            
            try
            {
                string[] display_list = ACD.DB.ListDisplayConfig();
                string display = display_list.Where(s => s.st_("High")).FirstOrDefault();

                ACD.WR("Display {0}", display);

                ACD.DB.SetAnnotationScale(scale);
                if (!display.et_())
                    ACD.DB.SetDisplayConfigCS(display);

                for (int i = 0; i < 3; i++)
                {
                    ACD.Focus();
                    ACD.ED.Regen();
                }
            }
            catch (System.Exception ex)
            {
                ACD.WR("No scale {0}", scale);
            }

            ObjectId title_txt_id = ACD.DB.CreateText(String.Format("S-{0}", _page_index + 1), p1 + GB.txt_page_no, 3);

            if(!block_title_id.IsNull)
                ACD.DB.MoveObject(block_title_id, p1 - ACD.DB._getPoint(block_title_id));

                //ACD.WR("WindowPlot {0},{1},{2},{3},{4}", pdf_name, p1, p2, scale, plot_offset);
                CustomPlotEngine.WindowPlot(pdf_name, p1, p2, scale);
            ACD.DB.EraseObject(title_txt_id);

            return pdf_name;
        }

        public string Title
        {
            get
            {
                return MainRegion.First().Content.filter("|").First();
            }
        }

        public string[] ReferenceTitles
        {
            get
            {
                return References.Select(itm => itm.Title).ToArray();
            }
        }

        public override string ToString()
        {
            return "Main Title <" + Title + "> References<" + ReferenceTitles.ToTextStr()
                + "> Overlay<" + OverlayFiles.ToTextStr() + ">";
        }

        public static void SetPDFInfo(string file)
        {
            string[] keys = new string[] { "Subject", "Title", "Author", "Creator", "Keywords", "A", "O", "P" };
            string[] prams = ACD.DB.GetAllDrawingProp();

            //PDFHelper.WriteXmpMetadata(file, keys.ToDictionary(k => k, k => prams._props(k)));

            using (PdfDocument document = PdfReader.Open(file))
            {
                foreach (string propertyName in keys)
                {
                    var properties = document.Info.Elements;

                    if (properties.ContainsKey("/" + propertyName))
                        properties.SetValue("/" + propertyName, new PdfString(prams._props(propertyName)));
                    else
                        properties.Add(new KeyValuePair<String, PdfItem>("/" + propertyName, new PdfString(prams._props(propertyName))));
                }
                document.Save(file);
            }
        }
    }

    public class ScheduleForm : System.Windows.Forms.Form
    {
        System.Windows.Forms.Button btnPlot, btnSchedule, btnUpdate, btnAutoScale;
        System.Windows.Forms.Label lbScaleList;
        System.Windows.Forms.ListView lvSchedule;
        System.Windows.Forms.TextBox editBox, txtScaleList;
        System.Windows.Forms.ComboBox lsTitleBlock, lsPrefix, lsListTitle, cbScalePick, cbOrder;
        System.Windows.Forms.ListViewItem currentEditingItem;
        int currentEditingSubItem = -1;
        bool isRefreshingScalePick = false;
        List<iPlotItemCLS> sourcePlotItems = new List<iPlotItemCLS>();

        public bool OK, Cancel;
        public bool Update;
        public string[] Items;
        public string[] Prefixes;
        public event EventHandler ButtonClick = new EventHandler((o, e) => { });
        const string DEFAULT_SCALE_LIST = "5,10,20,25,100,200,500";

        public bool WithSchedule
        {
            get { return true; }
        }

        public string TitleBlockOverlay
        {
            get
            {
                return lsTitleBlock.SelectedIndex != -1 ? lsTitleBlock.Items[lsTitleBlock.SelectedIndex].ToString() : null;
            }
        }

        public string[] TitleDatas
        {
            get
            {
                SyncItemsFromView();
                return DE.NumericArray(0, Items.Length - 1).Select(i => "#" + i + "=" + Items[i]).ToArray();
            }
        }

        public string Prefix
        {
            get
            {
                return lsPrefix.Items[lsPrefix.SelectedIndex].ToString();
            }
        }

        public string ListTitle
        {
            get
            {
                return lsListTitle.Items[lsListTitle.SelectedIndex].ToString();
            }
        }

        public string ScaleListText
        {
            get
            {
                string res = txtScaleList == null ? DEFAULT_SCALE_LIST : txtScaleList.Text.Trim();
                return res.empty() ? DEFAULT_SCALE_LIST : res;
            }
        }

        static void ParseItemLine(string line, out string content, out string scale)
        {
            content = "";
            scale = "";
            if (line.empty())
                return;

            if (line.Contains(";"))
            {
                string[] ar = line.filter(";");
                content = ar.FirstOrDefault() ?? "";
                if (ar.Length > 1)
                    scale = ar.LastOrDefault() ?? "";
            }
            else
                content = line;

            string extractedScale = "";
            Match m = Regex.Match(content, @"(?i)\bTL\s*1\s*[:/]\s*(\d+)\b");
            if (m.Success)
            {
                extractedScale = "1:" + m.Groups[1].Value;
                content = Regex.Replace(content, @"(?i)\bTL\s*1\s*[:/]\s*\d+\b", " ");
                while (content.Contains("  "))
                    content = content.Replace("  ", " ");
                content = content.Trim().Trim('-', ';', ',', ':');
            }

            if (scale.empty() && !extractedScale.empty())
                scale = extractedScale;

            scale = NormalizeScaleValue(scale);
        }

        static string GetContentOnly(string line)
        {
            string content, scale;
            ParseItemLine(line, out content, out scale);
            return content;
        }

        static string NormalizeScaleValue(string scale)
        {
            if (scale.empty())
                return scale;

            Match m = Regex.Match(scale, @"(\d+(\.\d+)?)\s*$");
            if (!m.Success)
                return scale;

            double denominator = m.Groups[1].Value.ToNumber();
            if (denominator <= 0)
                return scale;

            return "1:" + ((int)Math.Round(denominator)).ToString();
        }

        void RefreshScalePickItems(bool keepSelection = true)
        {
            isRefreshingScalePick = true;
            string current = keepSelection ? (cbScalePick.SelectedItem == null ? "" : cbScalePick.SelectedItem.ToString()) : "";
            cbScalePick.Items.Clear();

            List<int> values = txtScaleList.Text
                .Replace(";", ",")
                .filter(",")
                .Select(s => s.Trim().ToNumber())
                .Where(v => v > 0)
                .Select(v => (int)Math.Round(v))
                .Distinct()
                .OrderBy(v => v)
                .ToList();

            if (values.Count == 0)
                values.AddRange(new int[] { 5, 10, 20, 25, 100, 200, 500 });

            foreach (int v in values)
                cbScalePick.Items.Add("1:" + v);

            if (!current.empty() && cbScalePick.Items.Contains(current))
                cbScalePick.SelectedItem = current;
            else if (cbScalePick.Items.Count > 0)
                cbScalePick.SelectedIndex = 0;
            isRefreshingScalePick = false;
        }

        pPos GetAnchorPoint(System.Windows.Forms.ListViewItem row)
        {
            int sourceIndex = row.Tag is int ? (int)row.Tag : -1;
            if (sourceIndex < 0 || sourceIndex >= sourcePlotItems.Count)
                return new pPos(0, 0);

            return sourcePlotItems[sourceIndex].MainRegion.Boundary()[0];
        }

        void RefreshNoColumn()
        {
            for (int i = 0; i < lvSchedule.Items.Count; i++)
                lvSchedule.Items[i].SubItems[0].Text = (i + 1).ToString();
        }

        void ReorderByCombo()
        {
            CommitEdit();
            if (lvSchedule.Items.Count == 0 || sourcePlotItems.Count == 0)
                return;

            List<System.Windows.Forms.ListViewItem> rows = lvSchedule.Items.Cast<System.Windows.Forms.ListViewItem>().ToList();
            IEnumerable<System.Windows.Forms.ListViewItem> ordered = rows;

            switch (cbOrder.SelectedIndex)
            {
                case 0: // X-Plot (KC)
                    ordered = rows.OrderBy(it => GetAnchorPoint(it).X).ThenBy(it => GetAnchorPoint(it).Y);
                    break;
                case 1: // Y-Plot (KC)
                    ordered = rows.OrderBy(it => GetAnchorPoint(it).Y).ThenBy(it => GetAnchorPoint(it).X);
                    break;
                case 2: // Y-Plot (MB/DN)
                    ordered = rows.OrderByDescending(it => GetAnchorPoint(it).Y).ThenBy(it => GetAnchorPoint(it).X);
                    break;
                case 3: // Reverse X (KC)
                    ordered = rows.OrderByDescending(it => GetAnchorPoint(it).X).ThenBy(it => GetAnchorPoint(it).Y);
                    break;
                case 4: // Reverse Y (MB)
                    ordered = rows.OrderBy(it => GetAnchorPoint(it).X).ThenByDescending(it => GetAnchorPoint(it).Y);
                    break;
            }

            lvSchedule.BeginUpdate();
            lvSchedule.Items.Clear();
            foreach (var row in ordered)
                lvSchedule.Items.Add(row);
            RefreshNoColumn();
            lvSchedule.EndUpdate();
        }

        void SyncItemsFromView()
        {
            CommitEdit();
            Items = new string[lvSchedule.Items.Count];
            for (int i = 0; i < lvSchedule.Items.Count; i++)
            {
                string scale = lvSchedule.Items[i].SubItems[1].Text.Trim();
                string content = lvSchedule.Items[i].SubItems[2].Text.Trim();
                Items[i] = content + (scale.empty() ? "" : ";" + scale);
            }
        }

        public void ApplyEditsTo(List<iPlotItemCLS> plotitems)
        {
            SyncItemsFromView();
            if (plotitems == null)
                return;

            List<iPlotItemCLS> src = plotitems.ToList();
            List<iPlotItemCLS> ordered = new List<iPlotItemCLS>();

            for (int i = 0; i < lvSchedule.Items.Count; i++)
            {
                var row = lvSchedule.Items[i];
                int sourceIndex = row.Tag is int ? (int)row.Tag : i;
                if (sourceIndex < 0 || sourceIndex >= src.Count)
                    sourceIndex = i < src.Count ? i : src.Count - 1;
                if (sourceIndex < 0)
                    continue;

                var item = src[sourceIndex];

                string content, scale;
                ParseItemLine(Items[i], out content, out scale);
                item.MainRegion.First().Content = content;
                if (!scale.empty())
                    item.scale = NormalizeScaleValue(scale);

                ordered.Add(item);
            }

            plotitems.Clear();
            plotitems.AddRange(ordered);
        }

        void BeginEditAt(System.Drawing.Point location)
        {
            var hit = lvSchedule.HitTest(location);
            if (hit == null || hit.Item == null || hit.SubItem == null)
                return;

            int subIndex = hit.Item.SubItems.IndexOf(hit.SubItem);
            if (subIndex != 1 && subIndex != 2)
                return;

            // Scale column uses combobox options from scale-list input.
            if (subIndex == 1)
            {
                lvSchedule.SelectedItems.Clear();
                hit.Item.Selected = true;
                string currentScale = hit.Item.SubItems[1].Text;
                if (!currentScale.empty() && cbScalePick.Items.Contains(currentScale))
                    cbScalePick.SelectedItem = currentScale;
                else if (cbScalePick.Items.Count > 0)
                    cbScalePick.SelectedIndex = 0;

                cbScalePick.Focus();
                cbScalePick.DroppedDown = true;
                return;
            }

            currentEditingItem = hit.Item;
            currentEditingSubItem = subIndex;

            var bounds = hit.SubItem.Bounds;
            editBox.SetBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            editBox.Text = hit.SubItem.Text;
            editBox.Visible = true;
            editBox.BringToFront();
            editBox.Focus();
            editBox.SelectAll();
        }

        void CommitEdit()
        {
            if (!editBox.Visible)
                return;

            if (currentEditingItem != null && currentEditingSubItem >= 0)
                currentEditingItem.SubItems[currentEditingSubItem].Text = editBox.Text;

            editBox.Visible = false;
            currentEditingItem = null;
            currentEditingSubItem = -1;
        }

        void CancelEdit()
        {
            editBox.Visible = false;
            currentEditingItem = null;
            currentEditingSubItem = -1;
        }

        public ScheduleForm(IEnumerable<string> data, List<iPlotItemCLS> plotItems = null, string scaleListText = null)
        {
            sourcePlotItems = plotItems != null ? plotItems.ToList() : new List<iPlotItemCLS>();
            this.Width = 350;
            this.Height = 655;
            this.TopMost = true;
            this.Text = "DrawingList";
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;

            lvSchedule = new System.Windows.Forms.ListView()
            {
                Left = 10,
                Top = 10,
                Width = this.Width - 40,
                Height = this.Height - 195,
                View = System.Windows.Forms.View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false,
                MultiSelect = false,
                Scrollable = true
            };
            lvSchedule.Columns.Add("No", 23);
            lvSchedule.Columns.Add("Scale", 120);
            lvSchedule.Columns.Add("Content", 280);

            editBox = new System.Windows.Forms.TextBox()
            {
                Visible = false,
                BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
            };

            editBox.LostFocus += (o, e) => CommitEdit();
            editBox.KeyDown += (o, e) =>
            {
                if (e.KeyCode == System.Windows.Forms.Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    CommitEdit();
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    CancelEdit();
                }
            };
            lvSchedule.MouseDoubleClick += (o, e) => BeginEditAt(e.Location);

            Items = data.ToArray();
            lvSchedule.Items.Clear();
            for (int i = 0; i < Items.Length; i++)
            {
                string content, scale;
                ParseItemLine(Items[i], out content, out scale);

                var item = new System.Windows.Forms.ListViewItem((i + 1).ToString());
                item.SubItems.Add(scale);
                item.SubItems.Add(content);
                item.Tag = i;
                lvSchedule.Items.Add(item);
            }
            RefreshNoColumn();

            lbScaleList = new System.Windows.Forms.Label()
            {
                Left = lvSchedule.Left,
                Top = lvSchedule.Bottom + 11,
                Width = 48,
                Height = 18,
                Text = "SC_LIST"
            };

            txtScaleList = new System.Windows.Forms.TextBox()
            {
                Left = lbScaleList.Right + 4,
                Top = lvSchedule.Bottom + 8,
                Width = lvSchedule.Width - lbScaleList.Width - 4,
                Text = scaleListText.empty() ? DEFAULT_SCALE_LIST : scaleListText
            };

            cbScalePick = new System.Windows.Forms.ComboBox()
            {
                DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList,
                Top = txtScaleList.Bottom + 6,
                Left = lvSchedule.Left,
                Width = 95
            };

            cbOrder = new System.Windows.Forms.ComboBox()
            {
                DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList,
                Top = cbScalePick.Top,
                Left = cbScalePick.Right + 8,
                Width = lvSchedule.Width - cbScalePick.Width - 8
            };
            cbOrder.Items.AddRange(new object[]
            {
                "X-Plot (KC)",
                "Y-Plot (KC)",
                "Y-Plot (MB/DN)",
                "Reverse X (KC)",
                "Reverse Y (MB)"
            });
            if (ACD.PlotMethodType == 0) cbOrder.SelectedIndex = 1;
            else if (ACD.PlotMethodType == 1) cbOrder.SelectedIndex = 0;
            else if (ACD.PlotMethodType == 2) cbOrder.SelectedIndex = 2;
            else if (ACD.PlotMethodType == 3) cbOrder.SelectedIndex = 4;
            else cbOrder.SelectedIndex = 0;

            RefreshScalePickItems(false);
            txtScaleList.TextChanged += (o, e) => RefreshScalePickItems(true);
            txtScaleList.Leave += (o, e) => RefreshScalePickItems(true);
            cbScalePick.SelectedIndexChanged += (o, e) =>
            {
                if (isRefreshingScalePick)
                    return;
                if (lvSchedule.SelectedItems.Count == 0 || cbScalePick.SelectedItem == null)
                    return;

                var row = lvSchedule.SelectedItems[0];
                row.SubItems[1].Text = NormalizeScaleValue(cbScalePick.SelectedItem.ToString());
            };
            lvSchedule.SelectedIndexChanged += (o, e) =>
            {
                if (lvSchedule.SelectedItems.Count == 0)
                    return;
                string scale = lvSchedule.SelectedItems[0].SubItems[1].Text;
                if (!scale.empty() && cbScalePick.Items.Contains(scale))
                    cbScalePick.SelectedItem = scale;
            };
            cbOrder.SelectedIndexChanged += (o, e) => ReorderByCombo();

            lsPrefix = new System.Windows.Forms.ComboBox()
            {
                DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList,
                Top = cbOrder.Bottom + 8,
                Left = lvSchedule.Left,
                Width = (lvSchedule.Width - 20) / 3
            };

            lsPrefix.Items.Clear();
            lsPrefix.Items.AddRange("A,B,C,D,E,F,G,H,I,M".filter());
            lsPrefix.SelectedIndex = 0;

            lsTitleBlock = new System.Windows.Forms.ComboBox()
            {
                DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList,
                Top = lsPrefix.Top,
                Left = lsPrefix.Left + lsPrefix.Width + 10,
                Width = lsPrefix.Width
            };

            lsTitleBlock.Items.Clear();
            lsTitleBlock.Items.AddRange(Directory.GetFiles(DE.CADLIB + @"PDF\", "*.pdf")
                .Where(f => Path.GetFileNameWithoutExtension(f).st_("TITLE"))
                .Select(f => Path.GetFileNameWithoutExtension(f)).ToArray());
            if (lsTitleBlock.Items.Count > 0)
                lsTitleBlock.SelectedIndex = 0;

            lsListTitle = new System.Windows.Forms.ComboBox()
            {
                DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList,
                Top = lsPrefix.Top,
                Left = lsTitleBlock.Left + lsTitleBlock.Width + 10,
                Width = lsPrefix.Width
            };

            lsListTitle.Items.Clear();
            lsListTitle.Items.AddRange(GP.PLOT_TITLE_DRAWING_LIST_TITLE);
            if (lsListTitle.Items.Count > 0)
                lsListTitle.SelectedIndex = 0;

            btnPlot = new System.Windows.Forms.Button()
            {
                Left = lsPrefix.Left,
                Top = lsPrefix.Bottom + 6,
                Width = lsPrefix.Width,
                Text = "Plot"
            };

            btnSchedule = new System.Windows.Forms.Button()
            {
                Left = lsTitleBlock.Left,
                Top = btnPlot.Top,
                Width = lsPrefix.Width,
                Height = btnPlot.Height,
                Text = "Schedule"
            };

            btnUpdate = new System.Windows.Forms.Button()
            {
                Left = lsListTitle.Left,
                Top = btnPlot.Top,
                Width = lsPrefix.Width,
                Height = btnPlot.Height,
                Text = "Update"
            };

            btnAutoScale = new System.Windows.Forms.Button()
            {
                Left = lvSchedule.Left,
                Top = btnPlot.Bottom + 6,
                Width = lvSchedule.Width,
                Height = btnPlot.Height,
                Text = "Auto Scale Ratio"
            };

            Controls.AddRange(new System.Windows.Forms.Control[]
                { lvSchedule, editBox, lbScaleList, txtScaleList, cbScalePick, cbOrder, lsPrefix, btnPlot, btnSchedule, btnUpdate, btnAutoScale, lsTitleBlock, lsListTitle });

            OK = false;
            Cancel = false;
            Update = false;

            btnPlot.Click += (o, e) =>
            {
                SyncItemsFromView();
                Prefixes = new string[Items.Length];
                for (int i = 0; i < Items.Length; i++)
                    Prefixes[i] = GetItemPrefix(Prefix, i);

                OK = true;
                ButtonClick(this, new EventArgs());
            };

            btnUpdate.Click += (o, e) =>
            {
                Update = true;
                ButtonClick(this, new EventArgs());
            };

            btnAutoScale.Click += (o, e) =>
            {
                for (int i = 0; i < lvSchedule.Items.Count; i++)
                {
                    int sourceIndex = lvSchedule.Items[i].Tag is int ? (int)lvSchedule.Items[i].Tag : i;
                    if (sourceIndex < 0 || sourceIndex >= sourcePlotItems.Count)
                        continue;

                    var item = sourcePlotItems[sourceIndex];
                    double w = Math.Abs(item.MainRegion[1].X - item.MainRegion[0].X);
                    double h = Math.Abs(item.MainRegion[1].Y - item.MainRegion[0].Y);

                    // A3 paper: 420 x 297 mm
                    double rawRatio = Math.Max(w / 420.0, h / 297.0);
                    int denominator = (int)Math.Ceiling(rawRatio);
                    if (denominator < 1) denominator = 1;

                    lvSchedule.Items[i].SubItems[1].Text = "1:" + denominator;
                }
            };

            btnSchedule.Click += (o, e) =>
            {
                SyncItemsFromView();
                using (ACD.Lock())
                {
                    ACD.Focus();

                    pPos pt = ACD.GetPoint();
                    if (pt != null)
                    {
                        double nx = pt.X, ny = pt.Y;
                        double PLOT_SCHEDULE_TITLE_Y_SCALE_IN_ROW = pString.INI_Value("PLOT_SCHEDULE_TITLE_Y_SCALE_IN_ROW");

                        string prefix = ACD.ED.GetInputString("Prefix", Prefix);
                        double size = ACD.ED.GetInputString("Size", "1").ToNumber(1) * 1000;
                        int line_limit = (int)ACD.ED.GetInputString("Enter line limit", "20").ToNumber(20);
                        int columns = (int)Math.Ceiling((double)(Items.Length - 1) / line_limit);
                        double w = 10 * size, h = (line_limit + 1) * size;

                        ObjectIdCollection ids = new ObjectIdCollection();
                        ids.Add(ACD.DB.CreateText(GP.PLOT_TITLE_DRAWING_LIST_TITLE[0],
                            new pPos(pt.X + size * 1.5, pt.Y - size), 4, 0, "ANNO=TRUE"));
                        ids.Add(ACD.DB.DrawPolyline(new pPos(pt.X,
                                pt.Y - size * 1.5).Rect((size * 2 + w) * columns, size * 1.5), true, GP.DEF_LAYER_TEXT));

                        pt.Y = ny - size * 2;
                        Prefixes = new string[Items.Length];

                        for (int i = 0; i < Items.Length; i++)
                        {
                            string st = Items[i];
                            double sz = size / 2;
                            Prefixes[i] = GetItemPrefix(prefix, i);

                            if (st.ToUpper().Contains("PHẦN BẢN VẼ"))
                            {
                                sz *= 2;
                                ids.Add(ACD.DB.CreateText("#C" + Prefixes[i].filter(".").First(),
                                   new pPos(pt.X + size / 2, pt.Y - sz * PLOT_SCHEDULE_TITLE_Y_SCALE_IN_ROW), 3, 0, "ANNO=TRUE"));
                            }
                            else
                                ids.Add(ACD.DB.CreateText("#C" + Prefixes[i],
                                   new pPos(pt.X + size / 2, pt.Y - sz * PLOT_SCHEDULE_TITLE_Y_SCALE_IN_ROW), 2, 0, "ANNO=TRUE"));

                            ids.Add(ACD.DB.CreateText(Items[i], new pPos(pt.X + size * 1.5,
                                pt.Y - sz * PLOT_SCHEDULE_TITLE_Y_SCALE_IN_ROW),
                                sz == size ? 3 : 2, 0, "ANNO=TRUE"));

                            ids.Add(ACD.DB.DrawPolyline(new pPos[] { new pPos(pt.X, pt.Y - sz),
                                new pPos(pt.X + w + size * 2, pt.Y - sz) }, false, GP.DEF_LAYER_TEXT));

                            pt.Y -= size / 2 + sz;
                            if (i > 0 && i % line_limit == 0)
                            {
                                pt.X += w + size * 2;
                                pt.Y = ny - size * 2;
                            }
                        }

                        pPos[] bb = ACD.DB._getBound(ids);
                        h = bb.Size().Y;

                        for (int i = 0; i < columns; i++)
                        {
                            double x = (size * 2 + w) * i;
                            ids.Add(ACD.DB.DrawPolyline(new pPos(nx + x, ny - h).Rect(size * 2 + w,
                                h - size * 1.5), true, GP.DEF_LAYER_TEXT));
                            ids.Add(ACD.DB.DrawPolyline(new pPos[] { new pPos(nx + size + x, ny - size * 1.5),
                                new pPos(nx + size + x, ny - h) }, false, GP.DEF_LAYER_TEXT));
                        }
                    }
                }
                ACD.Focus();
            };
        }

        public string GetItemPrefix(string start_prefix, int item_index)
        {
            int index = 0;
            string next_prefix = start_prefix;

            for (int i = 0; i <= item_index; i++)
            {
                string st = Items[i];
                if (st.ToUpper().Contains("PHẦN BẢN VẼ"))
                {
                    start_prefix = next_prefix;
                    next_prefix = ((char)((int)(char)start_prefix[0] + 1)).ToString();
                    index = 0;
                }
                else
                    index++;
            }

            return start_prefix + "." + (index <= 99 ? "0" : "")
                + (index <= 9 ? "0" : "") + index;
        }
    }

    public static class PlotLoadViewportCLS
    {
        public const double BASE_SCALE = 105;
        static readonly double[] SCALE_FACTORS = new double[] { 0.1, 0.2, 0.25, 0.5, 1 };

        public static string NormalizeScale(string scaleText)
        {
            double denominator = scaleText.filter(":").Last().ToNumber();
            return "1:" + NormalizeScaleDenominator(denominator).ToString();
        }

        public static int NormalizeScaleDenominator(double rawDenominator)
        {
            if (rawDenominator <= 0)
                return (int)BASE_SCALE;

            var candidates = SCALE_FACTORS
                .Select(f => BASE_SCALE * f)
                .Distinct()
                .OrderBy(v => v)
                .ToArray();

            double ratio = rawDenominator / BASE_SCALE;
            int magnitude = (int)Math.Pow(10, Math.Floor(Math.Log10(Math.Max(ratio, 0.0000001))));
            if (magnitude < 1) magnitude = 1;

            List<double> scaledCandidates = new List<double>();
            foreach (double c in candidates)
                foreach (int m in new int[] { magnitude / 10, magnitude, magnitude * 10 })
                    if (m > 0)
                        scaledCandidates.Add(c * m);

            double best = scaledCandidates
                .Distinct()
                .OrderBy(v => Math.Abs(v - rawDenominator))
                .FirstOrDefault();

            if (best <= 0) best = BASE_SCALE;
            return (int)Math.Round(best);
        }

        public static double PAGESIZE_W = 841;
        public static double PAGESIZE_H = 594;

        public static List<iPlotItemCLS> plotitems;
        public static List<string> ResultPDFList;
        public static pPos[] TitleBlockContents;
        public static string[] xnotes;
        public static List<pPos> TitleList;

        class PlotItemCacheRow
        {
            public pPos P1;
            public pPos P2;
            public string Content;
            public string Scale;
        }

        static string CacheFilePath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "ACadScriptCache");
                Directory.CreateDirectory(dir);

                string dwg = Path.GetFileNameWithoutExtension(ACD.CurrentDWGFileName);
                if (dwg.empty())
                    dwg = "drawing";
                return Path.Combine(dir, dwg + "_PlotLoadViewport.cache.txt");
            }
        }

        static string ToInvariant(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        static double ToNumberInvariant(string value)
        {
            double number;
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out number))
                return number;
            return 0;
        }

        static string EncodeCache(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? ""));
        }

        static string DecodeCache(string value)
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value ?? ""));
            }
            catch
            {
                return "";
            }
        }

        static string ZoneSignature(pPos[] bounds)
        {
            if (bounds == null || bounds.Length < 2)
                return "";

            double x1 = Math.Round(bounds[0].X, 1);
            double y1 = Math.Round(bounds[0].Y, 1);
            double x2 = Math.Round(bounds[1].X, 1);
            double y2 = Math.Round(bounds[1].Y, 1);
            return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", x1, y1, x2, y2);
        }

        static bool TryLoadCache(string zoneSignature, out List<PlotItemCacheRow> rows, out pPos[] titleBlockContents, out string scaleListText)
        {
            rows = new List<PlotItemCacheRow>();
            titleBlockContents = new pPos[0];
            scaleListText = null;

            try
            {
                if (!File.Exists(CacheFilePath))
                    return false;

                string[] lines = File.ReadAllLines(CacheFilePath);
                if (lines.Length == 0)
                    return false;

                string[] first = lines[0].Split('\t');
                if (first.Length < 2 || first[0] != "ZONE" || first[1] != zoneSignature)
                    return false;

                List<pPos> titlePoints = new List<pPos>();

                foreach (string line in lines.Skip(1))
                {
                    string[] ar = line.Split('\t');
                    if (ar.Length == 0)
                        continue;

                    if (ar[0] == "SL" && ar.Length >= 2)
                    {
                        scaleListText = DecodeCache(ar[1]);
                    }
                    else if (ar[0] == "TB" && ar.Length >= 4)
                    {
                        pPos p = new pPos(ToNumberInvariant(ar[1]), ToNumberInvariant(ar[2]));
                        p.Content = DecodeCache(ar[3]);
                        titlePoints.Add(p);
                    }
                    else if (ar[0] == "IT" && ar.Length >= 7)
                    {
                        rows.Add(new PlotItemCacheRow()
                        {
                            P1 = new pPos(ToNumberInvariant(ar[1]), ToNumberInvariant(ar[2])),
                            P2 = new pPos(ToNumberInvariant(ar[3]), ToNumberInvariant(ar[4])),
                            Content = DecodeCache(ar[5]),
                            Scale = DecodeCache(ar[6])
                        });
                    }
                }

                titleBlockContents = titlePoints.ToArray();
                return rows.Count > 0;
            }
            catch
            {
                rows = new List<PlotItemCacheRow>();
                titleBlockContents = new pPos[0];
                scaleListText = null;
                return false;
            }
        }

        static void SaveCache(string zoneSignature, List<iPlotItemCLS> items, pPos[] titleBlockContents, string scaleListText = null)
        {
            try
            {
                List<string> lines = new List<string>();
                lines.Add("ZONE\t" + zoneSignature);
                lines.Add("SL\t" + EncodeCache(scaleListText));

                if (titleBlockContents != null)
                    foreach (pPos p in titleBlockContents)
                        lines.Add("TB\t" + ToInvariant(p.X) + "\t" + ToInvariant(p.Y)
                            + "\t" + EncodeCache(p.Content));

                if (items != null)
                    foreach (iPlotItemCLS item in items)
                        lines.Add("IT\t" + ToInvariant(item.MainRegion[0].X) + "\t" + ToInvariant(item.MainRegion[0].Y)
                            + "\t" + ToInvariant(item.MainRegion[1].X) + "\t" + ToInvariant(item.MainRegion[1].Y)
                            + "\t" + EncodeCache(item.Title) + "\t" + EncodeCache(item.scale));

                File.WriteAllLines(CacheFilePath, lines.ToArray());
            }
            catch { }
        }

        public static string[] title_keywords = new string[] { "PHẦN BẢN VẼ","BẢN VẼ", "MẶT BẰNG", "SƠ ĐỒ","SCALE", "MB","THỐNG KÊ","DANH SÁCH", "CHI TIẾT", "MẶT ĐỨNG", "MẶT CẮT", "GHI CHÚ", "PLAN", "VIEW", "SECTION", "ELEV" };
        public static double extraX = -5, extraY = 1.5;

        public static string[] append_txt_keys = new string[] {
                "#T", "#D", "#A", "#P",
                "#Add" ,"#S", "#D","#C","#N"};

        static void mergePDF(string[] pdfs, string outPutPDF)//, string[] formats)
        {
            using (PdfDocument targetDoc = new PdfDocument())
            {
                foreach (string pdf in pdfs)
                    using (PdfDocument pdfDoc = PdfReader.Open(pdf, PdfDocumentOpenMode.Import))
                    {
                        for (int i = 0; i < pdfDoc.PageCount; i++)
                        {
                            targetDoc.AddPage(pdfDoc.Pages[i]);

                            int index = targetDoc.PageCount - 1;
                            var currentpage = targetDoc.Pages[index];

                            currentpage.Orientation = PdfSharp.PageOrientation.Portrait;

                            //_addInfoToSinglePage(currentpage, titles[index]);
                        }
                    }

                targetDoc.Save(outPutPDF);
            }
        }

        static void _addText(PdfPage page, string content,
            pPos pt, double width = 200, double height = 200,
            string info = "font:Arial;size:5;align:center middle;bold:off;color:black")
        {
            info = info.Replace(";", "|").Replace(":", "=");

            using (XGraphics gfx = XGraphics.FromPdfPage(page, XGraphicsUnit.Millimeter))
            {
                XPdfFontOptions options = new XPdfFontOptions(PdfFontEncoding.Unicode, PdfFontEmbedding.Always);

                // Create a font
                XFont font = new XFont(info._prop("font"), info._prop("size").ToNumber(20),
                    info._prop("bold").ToBool() ? XFontStyle.Bold : XFontStyle.Regular, options);

                XStringFormat format = XStringFormats.TopLeft;
                                
                gfx.RotateAtTransform(90, new XPoint(pt.X, pt.Y));
                //gfx.TranslateTransform(gfx.PageSize.Height / 2, gfx.PageSize.Width / 2);

                //ACD.WR("Text point {0},{1}", pt.X, pt.Y);

                // Draw the text
                
                gfx.DrawString(content, font, XBrushes.Black,
                    new XRect(pt.X, pt.Y, width, height), format);
            }
        }

        static string _genrPDFSaveFile(string dir)
        {
            int month = DateTime.Now.Month;
            int day = DateTime.Now.Day;

            string newfile = System.IO.Path.GetFileNameWithoutExtension(ACD.CurrentDWGFileName)
                + "_" + (month < 10 ? "0" : "") + month + "_" + (day < 10 ? "0" : "") + day;

            string savefile = dir + @"\" + newfile + ".pdf";

            int index = 0;

            while (File.Exists(savefile))
            {
                index++;
                savefile = dir + @"\" + newfile + "(" + index + ").pdf";
            }

            return savefile;
        }

        


        static void addInfoToSinglePage(PdfPage page, string title, string no, string[] formats = null)
        {
            //ACD.WR("OK00 {0}", TitleBlockContents);
            foreach (pPos p in TitleBlockContents)
            {
                string content = "";

                if (p.Content == "#T")
                    content = title;
                else if(p.Content == "#D")
                    content = DateTime.Now.Day + "/" + DateTime.Now.Month + "/" + DateTime.Now.Year;
                else if (p.Content == "#N")
                    content = no;

                ACD.WR("Info {0}x{1}:{2}", p.X * 390, p.Y * 260, content);

                if (!content.empty())
                    _addText(page, content, new pPos(p.Y * 420 + extraX, p.X * 297 + extraY));
            }

            //string overlays = formats._props("Plot.Place");

            //if (!overlays.empty())
            //{
            //    pPos[] pts = null;
            //    string[] overlayFiles = file_ref_list(overlays, ref pts);

            //    if (overlayFiles.Length > 0)
            //        _overlayPDF(page, overlayFiles, pts);
            //}
        }


        public static void PlotGroup(string[] prefixes, double page_w, double page_h, string[] formats)
        {
            List<string> res = new List<string>();
            //string[] history_files = Directory.GetFiles(dir, "*.pdf");

            if (ACD.plot_with_annotative)
            {
                ACD.DB.AnnoAllVisible = false;
                ACD.DB.AnnotativeDwg = false;
            }

            List<string> extensions = new List<string>();

            ACD.ProgressLoad.Maximum = plotitems.Count();
            ACD.ProgressLoad.Value = 0;
            ACD.ProgressCancel = false;

            ACD.WR(">Pages {0}", plotitems.Count());

            ResultPDFList = new List<string>();
            PlotSingleFile(0);

            //return savefile;
        }

        private static async System.Threading.Tasks.Task<bool> WaitForFile(string path, string title,
            TimeSpan timeout, Action<string, string> fn, 
            System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
        {
            DateTimeOffset timeoutAt = DateTimeOffset.UtcNow + timeout;

            while (true)
            {
                if (File.Exists(path))
                {
                    fn(path, title);
                    return true;
                }

                if (DateTimeOffset.UtcNow >= timeoutAt) return false;

                cancellationToken.ThrowIfCancellationRequested();
                await System.Threading.Tasks.Task.Delay(10);
            }
        }


        static void PlotSingleFile(int page_index)
        {
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\plot";
            Directory.CreateDirectory(dir);

            if (page_index < plotitems.Count)
            {
                ACD.WR("Plot page {0}", page_index);

                ACD.ProgressLoad.Value = page_index;
                ACD.ProgressLoad.Refresh();

                ACD.ProgressPercent.Text = String.Format("{0}/{1}",
                    ACD.ProgressLoad.Value, ACD.ProgressLoad.Maximum);
                ACD.ProgressPercent.Refresh();

                //ACD.WR("P1");
                if (ACD.ProgressCancel)
                    return;

                //ACD.WR("P2");
                iPlotItemCLS itm = plotitems[page_index];
                List<string> files = new List<string>();

                if (itm.References.Count > 0)
                    files.AddRange(DE.NumericArray(0, itm.References.Count - 1)
                        .Select(n => itm.References[n].Plot(dir, (page_index + 1) + "-" + plotitems.Count
                        + "(" + (n + 1) + "-" + itm.References.Count + ")", n)));

                files.AddRange(itm.OverlayFiles);
                //ACD.WR("P3 Files {0}", itm.OverlayFiles.Length);

                string pdffile = itm.Plot(dir, (page_index + 1) + "-" + plotitems.Count, page_index);

                ResultPDFList.Add(pdffile);
                //extensions.AddRange(itm.ReferencePrefix);



                WaitForFile(pdffile, itm.Title, new TimeSpan(0,1,0),
                    (p,s) =>
                    {
                        using (PdfDocument pdfDoc = PdfReader.Open(p, PdfDocumentOpenMode.Modify))
                        {
                            addInfoToSinglePage(pdfDoc.Pages[0], s, 
                                (page_index < 9 ? "0" : "") + (page_index + 1) + "/" + plotitems.Count);
                            pdfDoc.Save(pdffile);
                        }

                        PlotSingleFile(page_index + 1);

                    });












                //var watcher = new FileSystemWatcher(dir);
                
                //    watcher.NotifyFilter = NotifyFilters.Attributes
                //                         | NotifyFilters.CreationTime
                //                         | NotifyFilters.DirectoryName
                //                         | NotifyFilters.FileName
                //                         | NotifyFilters.LastAccess
                //                         | NotifyFilters.LastWrite
                //                         | NotifyFilters.Security
                //                         | NotifyFilters.Size;

                //    watcher.Created += (o, e) =>
                //    {
                //        //if (File.Exists(pdffile))
                //        {
                            
                //        }
                //    };

                //watcher.Filter = "*.pdf";
                //watcher.IncludeSubdirectories = true;
                //watcher.EnableRaisingEvents = true;
                
            }
            else
            {
                ACD.ProgressLoad.Value = 0;
                ACD.DB.AnnoAllVisible = true;
                ACD.DB.AnnotativeDwg = true;

                string savefile = _genrPDFSaveFile(dir);

                if (!ACD.ProgressCancel)
                {
                    if (ResultPDFList.Count > 0)
                    {
                        //if (System.Windows.Forms.MessageBox.Show("Do you want to merge all pdf to:<"
                        //        + Path.GetFileName(savefile) + "> and erase all single pdfs?",
                        //        "Merge", System.Windows.Forms.MessageBoxButtons.YesNo)
                        //        == System.Windows.Forms.DialogResult.Yes)
                        { 
                            mergePDF(ResultPDFList.ToArray(), savefile);
                        
                            //ACD.ProgressPercent.Text = "Save in [" + savefile + "]";
                            ACD.WR("Save file [{0}]", savefile);

                            System.Windows.Forms.Clipboard.SetText(savefile);

                            foreach (string file in ResultPDFList)
                                File.Delete(file);
                        }
                    }
                    else
                        ACD.ProgressPercent.Text = "No page to print";
                }
            }
        }

        static pPos[] TitleBlockPositionFromKeys(PosCollection pls)
        {
            List<pPos> res = new List<pPos>();

            ACD.WR("Title_PreList: {0}", TitleList.ToText());

            foreach (string k in append_txt_keys)
            {
                pPos p = TitleList.FirstOrDefault(_t => _t.Content == k);
                
                if (p != null)
                {
                    pPos[] ls = pls.FirstOrDefault(_ls => p.Inside(_ls));

                    if(ls != null)
                    {
                        pPos sz = ls.Size();
                        pPos pt = p - ls.Boundary()[0];
                        double sc = ls.BoundScale(841, 594).filter(":").Last().ToNumber();

                        ACD.WR("SC {0}", sc);

                        res.Add(new pPos(pt.X / (390 * sc), pt.Y / (260 * sc)));
                        res.Last().Content = k;
                    }
                }
            }

            return res.ToArray();
        }

        //public static pPos[] TitleBlockContents;

        static iPlotItemCLS[] CollectPlotGroups(PosCollection pls)
        {
            //đây là function đầu tiên của Plot, nhiệm vụ là duyệt qua các vùng chọn, chọn vùng chọn phụ thuộc (reference)
            //vào vùng chọn khác
            //khai báo danh sách plot

            plotitems = new List<iPlotItemCLS>();

            //vị trí text trong tilte block


            TitleBlockContents = TitleBlockPositionFromKeys(pls);

            ACD.WR("Title_List {0}", TitleBlockContents.ToText());
            //ACD.WR("COLL 01");
            //gom các vùng chọn có title có bắt đầu bằng . và >>>
            //pPos[] txt_points = pls.Select(ls => ls.First())
            //.Where(p => !p.Content.empty() && (p.Content.Contains(">>>") || p.Content.StartsWith(".")))
            //.Select(p => new pPos(p.X, p.Y, p.Z, p.Content)).ToArray();
            //ACD.WR("COLL 02");
            //duyệt từng vùng chọn

            for (int i = 0; i < pls.Count; i++)
            {
                //gom các điểm text nằm trong vùng chọn,sau đó sắp xếp theo Y, nếu Y bằng thì theo X
                //pPos[] txts = txt_points.Where(p
                //   => p.Inside(pls[i])).OrderBy(p => p.Y.roundNumber(100))
                //   .ThenBy(p => p.X.roundNumber(100)).ToArray();
                                
                pPos[] ls = TitleList.Where(p1 => p1.Inside(pls[i])).ToArray();

                if (ls.Length > 0)
                {
                    pPos pt_title = ls.OrderBy(p =>
                            title_keywords.ToList().FindIndex(k => p.Content.ct_(k)))
                        .ThenBy(p => - p.Content.Length)
                        .ThenBy(p => p.Y.roundNumber(100)).ThenBy(p => p.X).First();

                    //lấy vùng chọn đầu tiên trong danh sách
                    //string[] ls = txts.Length > 1 ? DE.NumericArray(1,txts.Length)
                    //.Select(n => txt_points.ElementAt(n).Content).ToArray() : null;
                    //đưa vào danh sách plot

                    //string content = pt_title;
                    //if (content.StartsWith("."))
                    //    content = content.Substring(1);

                    plotitems.Add(new iPlotItemCLS(pls[i], pt_title.Content));
                    //plotitems.Last().TitleBlockContents = TitleBlockContents;
                }
            }
            //ACD.WR("COLL 03");
            //sắp xếp danh sách plot theo độ dài title, mục đích để tìm ra vùng chọn có title là suffix của vùng chọn khác
            //để tìm reference
            //ACD.WR("OK2 {0}", pls.Count);
            plotitems = plotitems.OrderBy(itm => -itm.Title.Length).ToList();

            //duyệt qua xem
            _updateRefAndInRef(plotitems);

            List<iPlotItemCLS> res = new List<iPlotItemCLS>();
            //ACD.WR("COLL 04");
            //danh sách plot đã có duyệt qua
            bool[] T = plotitems.Select(itm => false).ToArray();
            //ACD.WR("OK4 {0}", plotitems.Count);
            for (int i = 0; i < plotitems.Count; i++)
                if (!T[i] && !_isRef(plotitems[i]))
                {
                    T[i] = true;
                    res.Add(plotitems[i]);

                    for (int j = 0; j < plotitems.Count; j++)
                        //nếu 2 title khác nhau, duyệt qua xem có đúng là reference không
                        if (i != j && plotitems[i].Title != plotitems[j].Title
                            && plotitems[i].Title.st_(plotitems[j].Title.Upper()))
                        {
                            //nếu đúng, đưa hết toàn bộ reference vùng chọn con về vùng chọn mẹ
                            T[j] = true;
                            plotitems[i].References.AddRange(plotitems[j].References);
                            //đưa vùng chọn con là reference của vùng chọn mẹ
                            plotitems[i].References.Add(plotitems[j]);
                        }
                }
            //ACD.WR("COLL 05");
            //sắp xếp thứ tự tùy theo người dùng
            if (ACD.PlotMethodType == 0)
                res = res.OrderBy(itm => itm.MainRegion.Boundary()[0].Y.roundNumber(100))
                    .ThenBy(itm => itm.MainRegion.Boundary()[0].X.roundNumber(100)).ToList();
            else if(ACD.PlotMethodType == 1)
                res = res.OrderBy(itm => itm.MainRegion.Boundary()[0].X.roundNumber(100))
                    .ThenBy(itm => itm.MainRegion.Boundary()[0].Y.roundNumber(100)).ToList();
            else if (ACD.PlotMethodType == 2)
                res = res.OrderBy(itm => -itm.MainRegion.Boundary()[0].Y.roundNumber(100))
                    .ThenBy(itm => itm.MainRegion.Boundary()[0].X.roundNumber(100)).ToList();
            else if (ACD.PlotMethodType == 3)
                res = res.OrderBy(itm => itm.MainRegion.Boundary()[0].X.roundNumber(100))
                    .ThenBy(itm => -itm.MainRegion.Boundary()[0].Y.roundNumber(100)).ToList();
            //ACD.WR("COLL 06");
            return res.ToArray();
        }

        static bool _isRef(iPlotItemCLS itm)
        {
            //sử dụng để ref file ngoài
            return itm.Title.st_("USING")
                    || itm.Title.st_("FILEIN");
        }

        static void _updateRefAndInRef(List<iPlotItemCLS> plotitems)
        {
            for (int i = 0; i < plotitems.Count; i++)
                if (_isRef(plotitems[i]))
                {
                    string key = plotitems[i].Title._firstProp().Upper();

                    for (int j = 0; j < plotitems.Count; j++)
                        if (i != j && plotitems[i].Title != plotitems[j].Title
                            && plotitems[j].Title.Upper().Contains(key)
                            && !plotitems[j].References.Contains(plotitems[i]))
                        {
                            plotitems[j].References.AddRange(plotitems[i].References);
                            plotitems[j].References.Add(plotitems[i]);
                        }
                }

            List<iPlotItemCLS> refplotitems = new List<iPlotItemCLS>();

            for (int i = 0; i < plotitems.Count; i++)
            {
                PosCollection ref_plots = plotitems[i].RefRegions;

                if (ref_plots != null)
                {
                    foreach (pPos[] pts in ref_plots)
                    {
                        string st = null;
                        foreach (pPos pt in pts)
                            if (!pt.Content.empty())
                                st = pt.Content;
                        string scale = plotitems[i].scale;

                        if (st.Contains(":x"))
                        {
                            string[] ar = st.filter(":x");
                            st = ar.First();
                            scale = "1:" + NormalizeScaleDenominator(scale.filter(":").Last().ToNumber() / ar.Last().ToNumber());
                        }
                        refplotitems.Add(new iPlotItemCLS(pts, st, null, scale));
                    }
                }
            }

            for (int i = 0; i < plotitems.Count; i++)
            {
                PosCollection inref_plots = plotitems[i].InRefRegions;

                if (inref_plots != null)
                {
                    foreach (pPos pt in inref_plots.AllPoints)
                    {
                        string st = pt.Content;
                        string scale = plotitems[i].scale;

                        if (st.Contains(":x"))
                        {
                            string[] ar1 = st.filter(":x");
                            st = ar1.First();
                            scale = "1:" + NormalizeScaleDenominator(scale.filter(":").Last().ToNumber() / ar1.Last().ToNumber());
                        }

                        iPlotItemCLS[] ar = refplotitems.Where(itm => itm.Title.Upper() == st.Upper()).ToArray();

                        if (ar.Length > 0)
                        {
                            iPlotItemCLS refp = ar.First();
                            double sc = plotitems[i].scale.filter(":").Last().ToNumber();

                            iPlotItemCLS plotitm = new iPlotItemCLS(refp.MainRegion, st, null, scale);

                            pPos p = (pt - plotitems[i].MainRegion[0]) / sc; // - new pPos(-page_w / 2, page_h / 2);
                            //plotitm.plot_offset = new pPos(p.Y + plot_offset.X, p.X + plot_offset.Y);
                            //ACD.WR("Offset {0}", plotitm.plot_offset);
                            plotitems[i].References.Add(plotitm);
                            //ACD.DB.DrawPolyline(refp.MainRegion.Rect(), true,"LAYER=Defpoints|LWIDTH=100");
                        }
                    }
                }
            }
        }

        

        static void _collectTitles()
        {
            TitleList = new List<pPos>();
            ACD.DB.GetEntities(null, EN_SELECT.AC_DXF, "TEXT", "MTEXT", "INSERT");

            TitleList = IR.SelectedIds.ToList()
                .Where(id => ACD.DB._isText(id))
                .Select(id => ACD.DB._getPoint(id)).ToList();

            foreach(ObjectId id in IR.SelectedIds)
                if(ACD.DB._isBlock(id))
                    ACD.DB.BlockEntitiesAction(id, _ids =>
                    {
                        foreach(ObjectId _id in _ids)
                            if(ACD.DB._isText(_id))
                                TitleList.Add(ACD.DB._getPoint(_id));
                    });

            TitleList = TitleList.Where(p => append_txt_keys.Contains(p.Content) 
                || title_keywords.Any(k => p.Content.ct_(k.Upper()))).ToList();
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();
                ObjectIdCollection regionIds = ACD.DB.FilterIds(selIds, "INSERT");

                if (regionIds.Count > 0)
                {
                    pPos[] bb = ACD.DB._getBound(selIds);
                    string zoneSignature = ZoneSignature(bb);
                    xnotes = selIds.ToList().SelectMany(id => ACD.DB.GetXNotes(id)).ToArray();

                    List<PlotItemCacheRow> cachedRows;
                    pPos[] cachedTitleBlockContents;
                    string cachedScaleListText;
                    bool hasCache = TryLoadCache(zoneSignature, out cachedRows, out cachedTitleBlockContents, out cachedScaleListText);

                    if (hasCache)
                    {
                        TitleBlockContents = cachedTitleBlockContents;
                        plotitems = cachedRows.Select(r =>
                                new iPlotItemCLS(new pPos[] { r.P1, r.P2 }, r.Content, null, r.Scale))
                            .ToList();

                        ACD.WR("Plot cache hit for zone [{0}] with {1} item(s)", zoneSignature, plotitems.Count);
                    }
                    else
                    {
                        PosCollection regions = new PosCollection();
                        foreach (ObjectId id in selIds)
                            ACD.DB.BlockEntitiesAction(id, (_ids) =>
                            {
                                foreach (ObjectId _id in _ids)
                                    regions.Add(ACD.DB._getBound(_id));
                            });

                        _collectTitles();
                        plotitems = CollectPlotGroups(regions).ToList();
                        ACD.WR("Plot cache miss for zone [{0}], recollect titles", zoneSignature);
                    }

                    string[] ls = plotitems.Select(itm => itm.Title 
                        + (!itm.Title.ToUpper().Contains("PHẦN BẢN VẼ") ? ";" + itm.scale : "")).ToArray();
                    // " - refs " + itm.References.Count + " file(s)"

                    ACD.WR("Plot Groups {0}, {1}", plotitems.Count, plotitems);

                    string project = ACD.DB.GetDrawingProp("PROJECT");
                    string owner = ACD.DB.GetDrawingProp("OWNER");
                    string address = ACD.DB.GetDrawingProp("ADDRESS");
                        
                    ACD.WR("Project:{0}\r\nOwner:{1}\r\nAddress:{2}", project, owner, address);
                    ScheduleForm frm = new ScheduleForm(ls, plotitems, cachedScaleListText);
                    //ACD.WR("OK2");
                    frm.ButtonClick += (o, e) =>
                    {
                        frm.Hide();

                        if (frm.Update)
                        {
                            frm.Close();
                            Main(args);
                            return;
                        }

                        if (frm.OK)
                            using (ACD.Lock())
                            {
                                frm.ApplyEditsTo(plotitems);
                                SaveCache(zoneSignature, plotitems, TitleBlockContents, frm.ScaleListText);
                                PlotGroup(frm.Prefixes, GP.PAGESIZE.X, GP.PAGESIZE.Y, xnotes);
                                //string savefile = @"D:\Dropbox\_Documents\Bao Loc\Van Kim Di Linh\Construct\plot\test.pdf";
                                
                                string[] generals = new string[] { "#PROJECT=--", "#OWNER=--", "#ADDRESS=--", "#PREFIX=A" };
                                //ACD.WR("OK1");
                                    
                                //ACD.WR("OK2");
                                if (!project.empty()) generals = generals._setprops("#PROJECT", project);
                                if (!owner.empty()) generals = generals._setprops("#OWNER", owner);
                                if (!address.empty()) generals = generals._setprops("#ADDRESS", address);
                                generals = generals._setprops("#PREFIX", frm.Prefix);
                                   
                                //else if (File.Exists(savefile))
                                //    System.Windows.Forms.Clipboard.SetText(savefile);

                                ACD.Focus();
                            }

                        frm.Close();
                    };

                    frm.Show();
                }

                ACD.Focus();
            }
        }
    }
}



