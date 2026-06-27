using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace AcadScript
{
    public static class FileHelper
    {
        const int ERROR_SHARING_VIOLATION = 32;
        const int ERROR_LOCK_VIOLATION = 33;

        private static bool IsFileLocked(System.Exception exception)
        {
            int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(exception) & ((1 << 16) - 1);
            return errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION;
        }

        public static bool CanReadFile(string filePath)
        {
            //Try-Catch so we dont crash the program and can check the exception
            try
            {
                //The "using" is important because FileStream implements IDisposable and
                //"using" will avoid a heap exhaustion situation when too many handles  
                //are left undisposed.
                using (FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    if (fileStream != null) fileStream.Close();  //This line is me being overly cautious, fileStream will never be null unless an exception occurs... and I know the "using" does it but its helpful to be explicit - especially when we encounter errors - at least for me anyway!
                }
            }
            catch (IOException ex)
            {
                //THE FUNKY MAGIC - TO SEE IF THIS FILE REALLY IS LOCKED!!!
                if (IsFileLocked(ex))
                {
                    // do something, eg File.Copy or present the user with a MsgBox - I do not recommend Killing the process that is locking the file
                    return false;
                }
            }
            finally
            {
            }
            return true;
        }

        public static string GetValidFileName(string filepath)
        {
            string dir = Path.GetDirectoryName(filepath);
            string file = Path.GetFileNameWithoutExtension(filepath);
            string ext = Path.GetExtension(filepath);
            string filename = filepath;
            int count = 1;

            while (!CanReadFile(filename))
            {
                filename = dir + @"\" + file + "_" + count + "." + ext;
                count++;
            }

            return file;
        }
    }

    public static class PDFHelper
    {
        
        public static pPos[] LoadPlotInfo(string key = null)
        {
            pPos[] res = new pPos[0];
            string[] contents = pString.INI_Params("PLOTINFO");

            if (contents != null && contents.Length > 0)
                foreach (string txt in contents)
                    if (txt.StartsWith("#TXT") && !txt._prop("VERTS").empty())
                    {
                        PosCollection pls = new PosCollection(txt._prop("VERTS"));
                        if (pls.Count > 0)
                        {
                            res = pls.AllPoints;
                            break;
                        }
                    }

            if (!key.empty())
                res = res.Where(p => p.Content.Upper().StartsWith(key.Upper())).ToArray();

            return res;
        }

        public static string ReadXmpMetadata(string filename)
        {
            iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(filename);
            byte[] b = reader.Metadata;
            return Encoding.UTF8.GetString(b, 0, b.Length);
        }

        static string _getVal(Dictionary<string, string> dict, string key)
        {
            return dict.ContainsKey(key) ? dict[key] : "";
        }

        public static void WriteXmpMetadata(string filename, Dictionary<string, string> dict)
        {
            iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(filename);
            using (MemoryStream ms = new MemoryStream())
            {
                using (iTextSharp.text.pdf.PdfStamper stamper = new iTextSharp.text.pdf.PdfStamper(reader, ms))
                {
                    Dictionary<String, String> info = reader.Info;
                    info["Title"] = _getVal(dict, "Title");
                    info["Subject"] = _getVal(dict, "Subject");
                    info["Keywords"] = _getVal(dict, "Keywords");
                    info["Creator"] = _getVal(dict, "Creator");
                    info["Author"] = _getVal(dict, "Author");

                    info["A"] = _getVal(dict, "A");
                    info["O"] = _getVal(dict, "O");
                    info["P"] = _getVal(dict, "P");

                    stamper.MoreInfo = info;
                }

                reader.Close();

                byte[] ar = ms.ToArray();
                // Write out PDF from memory stream.
                using (FileStream fs = File.Create(filename))
                {
                    fs.Write(ar, 0, ar.Length);
                }
            }
        }
        public static bool MergePDFs(IEnumerable<string> fileNames, string targetPdf)
        {
            bool merged = true;
            using (FileStream stream = new FileStream(targetPdf, FileMode.Create))
            {
                iTextSharp.text.Document document = new iTextSharp.text.Document();
                iTextSharp.text.pdf.PdfCopy pdf = new iTextSharp.text.pdf.PdfCopy(document, stream);
                iTextSharp.text.pdf.PdfReader reader = null;

                try
                {
                    document.Open();
                    foreach (string file in fileNames)
                    {
                        reader = new iTextSharp.text.pdf.PdfReader(file);
                        pdf.AddDocument(reader);
                        reader.Close();
                    }
                }
                catch (System.Exception)
                {
                    merged = false;
                    if (reader != null)
                    {
                        reader.Close();
                    }
                }
                finally
                {
                    if (document != null)
                    {
                        document.Close();
                    }
                }
            }
            return merged;
        }

        public static PosCollection GetTextExtraction(string filename)
        {
            PosCollection res = new PosCollection();
            //int total = PageCount;

            using (iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(filename))
            {
                //iTextSharp.text.pdf.PRTokeniser prtTokeneiser;
                int pageFrom = 1;
                int pageTo = reader.NumberOfPages;
                //iTextSharp.text.pdf.PRTokeniser.TokType tkntype;
                //string tknValue;

                for (int i = pageFrom; i <= pageTo; i++)
                {
                    iTextSharp.text.pdf.PdfDictionary cpage = reader.GetPageN(i);
                    iTextSharp.text.pdf.PdfArray cannots = cpage.GetAsArray(iTextSharp.text.pdf.PdfName.ANNOTS);

                    if (cannots != null)
                    {
                        List<pPos> pts = new List<pPos>();
                        foreach (iTextSharp.text.pdf.PdfObject oAnnot in cannots.ArrayList)
                        {
                            iTextSharp.text.pdf.PdfDictionary cAnnotationDictironary =
                                (iTextSharp.text.pdf.PdfDictionary)reader.GetPdfObject(((iTextSharp.text.pdf.PRIndirectReference)oAnnot).Number);

                            iTextSharp.text.pdf.PdfObject obj = cAnnotationDictironary.Get(iTextSharp.text.pdf.PdfName.CONTENTS);

                            if (obj != null && obj.GetType() == typeof(iTextSharp.text.pdf.PdfString))
                            {
                                iTextSharp.text.pdf.PdfString itm = (iTextSharp.text.pdf.PdfString)obj;



                                var pd = (iTextSharp.text.pdf.PdfDictionary)obj;
                                var PDFStreamObj = (iTextSharp.text.pdf.PdfStream)obj;

                                iTextSharp.text.pdf.PdfObject subtype = PDFStreamObj.Get(iTextSharp.text.pdf.PdfName.SUBTYPE);

                                long h = ((iTextSharp.text.pdf.PdfNumber)PDFStreamObj.Get(iTextSharp.text.pdf.PdfName.LOCATION)).LongValue;
                                //int width = ((iTextSharp.text.pdf.PdfNumber)PDFStreamObj.get(iTextSharp.text.pdf.PdfName.WIDTH)).intValue();


                                pPos pt = new pPos(0, 0);
                                pt.Content = itm.ToString();

                                pts.Add(pt);
                            }
                        }

                        res.Add(pts.OrderBy(p => p.Y.roundNumber(10)).ThenBy(p => p.X).ToArray());
                    }
                }

                reader.Close();
            }

            return res;
        }
    }

    
    public class ProgressBarForm: System.Windows.Forms.Form
    {
        System.Windows.Forms.Label lblPercent;
        System.Windows.Forms.ProgressBar prgLoad;
        public delegate void CompleteEventHandler(ProgressBarForm frm);
        public event CompleteEventHandler Completed;
        
        public ProgressBarForm(string caption, int total, Action<int> action)
        {
            //ACD.WR("OK1");
            this.Width = 500;
            this.Height = 140;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Text = caption;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            lblPercent = new System.Windows.Forms.Label() { Left = 10, Top = 10, Width = 50 };
            //TextBox textBox = new TextBox() { Left = lblPercent.Left + lblPercent.Width,
            //    Top = lblPercent.Top + lblPercent.Height + 10, Width = 400 };

            //prgLoad: { Left = lblPercent.Left + lblPercent.Width,
            //    Top = lblPercent.Top + lblPercent.Height + 10, Width = 400 };
            //ACD.WR("OK2");
            prgLoad = new System.Windows.Forms.ProgressBar()
            {
                Left = lblPercent.Left + lblPercent.Width,
                Top = lblPercent.Top + lblPercent.Height + 10,
                Width = this.Width - lblPercent.Left - lblPercent.Width - 40,
                Value = 0,
                Minimum = 0,
                Maximum = total,
                Step = 1
            };
            //ACD.WR("OK3");
            System.Windows.Forms.Button okButton = new System.Windows.Forms.Button()
            {
                Text = "OK",
                
                Width = 100,
                Top = prgLoad.Top + prgLoad.Height,
                DialogResult = System.Windows.Forms.DialogResult.OK
            };

            okButton.Left = (this.Width - okButton.Width) / 2;
            //ACD.WR("OK4");
            okButton.Click += (sender, e) => { this.Close(); };
            this.Controls.AddRange(new System.Windows.Forms.Control[] { prgLoad, okButton, lblPercent });
            //ACD.WR("OK5");
            this.AcceptButton = okButton;

            this.Shown += (o,e) =>
            {
                this.Refresh();

                //prgLoad.Maximum = total;
                //prgLoad.Value = 0;

                for (int i = 0; i < total; i++)
                {
                    //ACD.WR("ITM {0}", i);
                    prgLoad.Invoke(new Action(() =>
                    {
                        //ACD.WR("OK7");
                        
                        //ACD.WR("OK8");

                        prgLoad.Value = i + 1;
                        prgLoad.Refresh();

                        lblPercent.Text = "[" + (i + 1) + "/" + total + "]";
                        lblPercent.Refresh();

                        
                        action(i);
                        //if (i == total - 2)
                        //    System.Threading.Thread.Sleep(500);

                        if (i == total - 1 && Completed != null)
                        {
                            //System.Threading.Thread.Sleep(1500);
                            Completed(this);
                        }
                    }));
                }
            };
            //this.Close();
        }
    }

    public class OverlayTitlePDFCLS
    {
        List<string> lsNumberPage;
        int start = 0;

        public string source_pdf_filename, ouput_pdf_filename, prefix;
        string[] page_datas, general_datas;
        bool within_schedule;

        public string[] PageNumberList
        {
            get
            {
                return lsNumberPage.ToArray();
            }
        }

        public int StartNumberPage
        {
            get
            {
                return start;
            }
        }
        
        public OverlayTitlePDFCLS(string _source_pdf_filename, 
            string[] _page_datas, string[] _general_datas, bool _within_schedule)
        {
            within_schedule = _within_schedule;

            page_datas = _page_datas;
            prefix = _general_datas._props("#prefix");
            if (prefix.empty()) prefix = "A";

            //ouput_pdf_filename = _ouput_pdf_filename;
            source_pdf_filename = _source_pdf_filename;
            general_datas = _general_datas;

            lsNumberPage = new List<string>();
            pPos[] plot_list = PDFHelper.LoadPlotInfo();

            generals = general_datas//.Where(pram => number_key.All(s => !pram.StartsWith("#" + s)))
                .Select(pram => _plotItemInfo(pram, plot_list)).Where(p => p != null).ToList();

            DateTime date = DateTime.Now;
            string sdate = "#L" + String.Format("{0}.{1}.{2}",
                (date.Day < 10 ? "0" : "") + date.Day,
                (date.Month < 10 ? "0" : "") + date.Month, date.Year);

            generals.AddRange(PDFHelper.LoadPlotInfo("DATE")
                .Select(p => pPos.FromString(p.X + "," + p.Y + "[" + sdate + "]")));

            _getPageInfoAndNumber(generals);
        }

        string _checkDuplicateFilename(string file, string suffix, string extension)
        {
            if (!extension.StartsWith(".")) extension = "." + extension;
            string res = file.Replace(extension, "_" + suffix + extension);

            int i = 1;
            while (File.Exists(res))
            {
                res = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(file),
                    System.IO.Path.GetFileNameWithoutExtension(file) 
                    + "_" + suffix + "(" + i + ")" + extension);
                i++;
            }

            return res;
        }

        public void OverlayPDF(string overlay_pdf_filename)
        {
            string new_file_wtext = _checkDuplicateFilename(source_pdf_filename, "wtext", ".pdf");

            PDFEditCLS pdf = new PDFEditCLS(source_pdf_filename, new_file_wtext);
            pdf.Stamps = page_datas;

            pdf.ModifyPDF(final_datas, GP.PDF_TEXT_HEIGHT, prefix.filter(".").First(), start);

            ouput_pdf_filename = _checkDuplicateFilename(new_file_wtext, "overlay", ".pdf");

            pdf = new PDFEditCLS(new_file_wtext, ouput_pdf_filename);
            pdf.Stamps = page_datas;

            pdf.ModifyPDF("#OVERLAY=" + overlay_pdf_filename);
        }

        void _getPageInfoAndNumber(IEnumerable<pPos> generals)
        {
            if (prefix.Contains("."))
                start = (int)prefix.filter(".").Last().ToNumber();

            final_datas = new PosCollection();
            pPos[] plot_list = PDFHelper.LoadPlotInfo();

            //string all_titles = "";
            string number_key = "0123456789";

            if (within_schedule)
            { 
                List<pPos> ls = generals.ToList();
                ls.Add(_plotItemInfo("--", plot_list, "#TITLE"));
                ls.Add(_plotItemInfo("#ID=--", plot_list, "#ID"));
                final_datas.Add(ls.ToArray());
            }

            for (int i = 0; i < page_datas.Length; i++)
                if (number_key.Any(s => page_datas[i].StartsWith("#" + s)))
                {
                    List<pPos> ls = generals.ToList();
                                        
                    string s = page_datas[i]._firstProp();
                    string[] ar = s.filter(";");
                    page_datas[i] = page_datas[i]._setprop(page_datas[i]._firstPropName(), ar.First());

                    ls.Add(_plotItemInfo(page_datas[i], plot_list, "#TITLE"));

                    if (ar.Length > 1)
                        ls.Add(_plotItemInfo("#SCALE=" + ar[1], plot_list, "#SCALE"));

                    string id = prefix.filter(".").First() + "."
                        + (start < 100 ? "0" : "") + (start < 10 ? "0" : "") + start;
                    
                    lsNumberPage.Add(id + "=" + page_datas[i]._firstProp());

                    ls.Add(_plotItemInfo("#ID=" + id, plot_list, "#ID"));
                    start++;

                    final_datas.Add(ls.ToArray());
                }

            final_datas.Name = page_datas.Where(pram => number_key.Any(s => pram.StartsWith("#" + s)))
                .Select(pram => pram._firstProp()).ToTextStr(";");
        }

        List<pPos> generals;
        PosCollection final_datas;
        void _addPDFInfo()
        {
            
        }

        pPos _plotItemInfo(string pram, pPos[] pts, string key = null)
        {
            pPos pt = null;
            if (key.empty())
                key = pram._firstPropName();
            string val = pram._firstProp();

            int index = Array.FindIndex(pts, p
                => key.Upper().Contains(p.Content.filter("@").First().Upper()));

            if (index != -1)
            {
                pt = new pPos(pts[index].X, pts[index].Y);

                string prefix = "#L";
                if (pts[index].Content.EndsWith("@C"))
                    prefix = "#C";
                else if (pts[index].Content.EndsWith("@R"))
                    prefix = "#R";

                pt.Content = prefix + val;
                if (key == "#TITLE")
                    pt.Content += "%" + GP.PLOT_TITLE_FONT_HEIGHT;

                if (key == "#ID")
                    pt.Content = "@" + pt.Content;
            }

            return pt;
        }
    }

    public class PlotItemCLS
    {
        public pPos[] MainRegion;
        public List<PlotItemCLS> References;
        public List<string> infors;
        public string scale = null;
        double page_w = 0, page_h = 0;
        public pPos plot_offset;
        
        static string LoadOverlay(string[] templatefiles, string pdffile, string prams,
            string fontname, float fontheight, float overlay_opacity = 1f)
        {
            string res = null;

            clsPDFAddText pdf = new clsPDFAddText(pdffile);

            foreach (string templatefile in templatefiles)
                if (File.Exists(templatefile))
                {
                    PosCollection pls = PDFHelper.GetTextExtraction(templatefile);

                    if (pls.Count > 0 )
                    {
                        pPos[] pts = pls.First();
                        string content = null;

                        if(pts.Length > 0)
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

                                if (pt != null && !pt.Content.empty())
                                    pdf.AddText(1, pt, fontname, fontheight);
                            }
                        }
                    }
                    ACD.PLOT_OVERLAY = overlay_opacity;
                    pdf.Overlay(templatefile);
                }

            //ACD.WR("OK3");
            pdf.Close();
            res = pdf.OutputFile;

            return res;
        }
        
        public static string PlotGroup(IEnumerable<PlotItemCLS> plotitems, 
            string[] prefixes, double page_w, double page_h)
        {
            List<string> res = new List<string>();

            string dir = System.IO.Path.GetDirectoryName(ACD.CurrentDWGPath) + @"\plot";
            Directory.CreateDirectory(dir);

            string[] history_files = Directory.GetFiles(dir, "*.pdf");

            if (ACD.plot_with_annotative)
            {
                ACD.DB.AnnoAllVisible = false;
                ACD.DB.AnnotativeDwg = false;
            }

            List<string> extensions = new List<string>();
            
            ACD.ProgressLoad.Maximum = plotitems.Count();
            ACD.ProgressLoad.Value = 0;
            ACD.ProgressCancel = false;

            for (int page = 0; page < plotitems.Count(); page++)
            {
                ACD.ProgressLoad.Value = page;
                ACD.ProgressLoad.Refresh();

                ACD.ProgressPercent.Text = String.Format("{0}/{1}",
                    ACD.ProgressLoad.Value, ACD.ProgressLoad.Maximum);
                ACD.ProgressPercent.Refresh();

                if (ACD.ProgressCancel)
                    break;

                PlotItemCLS itm = plotitems.ElementAt(page);
                List<string> files = new List<string>();

                if (itm.References.Count > 0)
                    files.AddRange(DE.NumericArray(0, itm.References.Count - 1)
                        .Select(n => itm.References[n].Plot(dir, (page + 1) + "-" + plotitems.Count()
                        + "(" + (n + 1) + "-" + itm.References.Count + ")")));

                files.AddRange(itm.OverlayFiles);

                string pdffile = itm.Plot(dir, (page + 1) + "-" + plotitems.Count());

                //pdffile = LoadOverlay(files.ToArray(), pdffile, itm.Description,
                    //GP.PLOT_TITLE_FONT, GP.PLOT_TITLE_FONT_HEIGHT, GP.PLOT_OVERLAY);

                //clsPDFAddText cls = new clsPDFAddText(pdffile, itm.AdditionFiles, itm.EndFiles);
                
                pPos note_pt = GP.PLOT_TITLE.Clone();
                note_pt.Content = ">>>REGION=" + itm.MainRegion[0] + ";" + itm.MainRegion[1]
                    + "|SCALE=" + itm.MainRegion.BoundScale(page_w, page_h)
                    + (prefixes != null && prefixes.Length > page ? "|PREFIX=" + prefixes[page] : "")
                    + itm.Description;
                note_pt.Rotation = 90;

                //cls.AddText(1, note_pt, GP.PLOT_TITLE_FONT, GP.PLOT_TITLE_FONT_HEIGHT);
                //res.Add(cls.OutputFile);
                //cls.Close();

                extensions.AddRange(itm.ReferencePrefix);
            }

            ACD.ProgressLoad.Value = 0;
            ACD.DB.AnnoAllVisible = true;
            ACD.DB.AnnotativeDwg = true;

            string savefile = "";

            if (!ACD.ProgressCancel)
            {
                if (res.Count > 0)
                {
                    string ext = extensions.Distinct().ToTextStr("_");
                    int month = DateTime.Now.Month;
                    int day = DateTime.Now.Day;

                    string newfile = System.IO.Path.GetFileNameWithoutExtension(ACD.CurrentDWGFileName)
                        + "_" + (month < 10 ? "0" : "") + month + "_" + (day < 10 ? "0" : "") + day;

                    savefile = dir + @"\" + newfile + ".pdf";

                    int index = 0;
                    while (File.Exists(savefile))
                    {
                        index++;
                        savefile = dir + @"\" + newfile + "(" + index + ").pdf";
                    }

                    PDFHelper.MergePDFs(res, savefile);
                    PlotItemCLS.SetPDFInfo(savefile);

                    ACD.ProgressPercent.Text = "Save in [" + savefile + "]";
                }
                else
                    ACD.ProgressPercent.Text = "No page to print";
            }
            else
                ACD.ProgressPercent.Text = "User break!";

            string[] current_files = Directory.GetFiles(dir, "*.pdf");
            foreach (string file in current_files)
                if (file != savefile && !history_files.Contains(file))
                    File.Delete(file);

            return savefile;
        }
        
        public PlotItemCLS(IEnumerable<pPos> region, string _title, 
            IEnumerable<string> txts = null, string _scale = null)
        {
            page_w = GP.PAGESIZE.X;
            page_h = GP.PAGESIZE.Y;
                        
            MainRegion = region.ToArray();
            MainRegion.First().Content = _title;
            References = new List<PlotItemCLS>();
            infors = new List<string>();
            scale = _scale;

            if(scale.empty())
                scale = MainRegion.BoundScale(page_w, page_h);

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
                //&& System.IO.Path.GetFileNameWithoutExtension(f).Upper().StartsWith(key.Upper()));

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

                if(!Description._prop("REF").empty())
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
                foreach (PlotItemCLS itm in References)
                    res.Add(this.Title.Substring(itm.Title.Length).Trim());
                return res.Distinct().ToArray();
            }
        }
        
        public string Plot(string dir, string prefix)
        {
            pPos p1, p2;
            double sc = scale.filter(":").Last().ToNumber();
            p1 = MainRegion[0];
            p2 = MainRegion[1];

            string desc = Description;

            string fname = Title.filter("\r\n;,|/&~!@#$^&*()[]").First();
            string pdf_name = dir + @"\[" + prefix + "]" + fname + ".pdf";

            pdf_name = pdf_name.Replace(@"\\\","\\").Replace(@"\\","\\").Replace(">","");

            ACD.WR("View {0},{1} [FILENAME={2}][SCALE={3}]{4}",
                MainRegion[0], MainRegion[1], pdf_name, scale, desc);
            ACD.WR("SCALE {0}", scale);

            try
            {
                ACD.DB.SetAnnotationScale(scale);
            }catch(System.Exception ex)
            {
                ACD.WR("No scale {0}", scale);
            }
            //ACD.WR("Path {0}", pdf_name);
            ACD.DB.WindowPlot(pdf_name, p1, p2, scale, plot_offset);
            
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

            PDFHelper.WriteXmpMetadata(file, keys.ToDictionary(k => k, k => prams._props(k)));
        }
    }

}
