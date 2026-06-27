using System;
using System.IO;
using System.Collections.Generic;
//using System.Data;
using System.Linq;
using System.Diagnostics;

namespace AcadScript
{
    
    public class PDFEditCLS
    {
        public string SourceFileName, DestFileName;
        public bool Overlay = true;
        //public System.Windows.Forms.ProgressBar prgAction;
        //public System.Windows.Forms.Label lblInfo;
        System.Diagnostics.Stopwatch watch;
        public List<string> PageList;
        int[] pagenumbers;
        public int DPI = 300;
        public string message;
        
        public void ToImages(params int[] _pagenumbers)
        {
            pagenumbers = _pagenumbers;
            if (pagenumbers == null || pagenumbers.Length == 0)
                pagenumbers = DE.NumericArray(1, PageCount);

            PageList = new List<string>();

            //prgAction.Maximum = PageCount;
            //prgAction.Value = 0;
            message = "Loading " + System.IO.Path.GetFileNameWithoutExtension(SourceFileName) + "...";

            System.ComponentModel.BackgroundWorker bW1 = new System.ComponentModel.BackgroundWorker();
            bW1.WorkerReportsProgress = true;
            // This event will be raised on the worker thread when the worker starts
            bW1.DoWork += new System.ComponentModel.DoWorkEventHandler(backgroundWorker1_DoWork);
            // This event will be raised when we call ReportProgress
            bW1.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(backgroundWorker_ProgressChanged);
            bW1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);

            watch = System.Diagnostics.Stopwatch.StartNew();
            bW1.RunWorkerAsync();
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            int total = PageCount;

            //GhostscriptPngDevice dev = new GhostscriptPngDevice(GhostscriptPngDeviceType.Png16m);
            //dev.GraphicsAlphaBits = GhostscriptImageDeviceAlphaBits.V_4;
            //dev.TextAlphaBits = GhostscriptImageDeviceAlphaBits.V_4;
            //dev.ResolutionXY = new GhostscriptImageDeviceResolution(DPI, DPI);

            //dev.CustomSwitches.Add("-dDOINTERPOLATE"); // custom parameter
            //dev.InputFiles.Add(SourceFileName);

            //string outputFolder = System.IO.Path.GetDirectoryName(DestFileName) + @"\";
            //for (int page = 1; page <= total; page++)
            //{
            //    dev.Pdf.FirstPage = page;
            //    dev.Pdf.LastPage = page;

            //    dev.OutputPath = System.IO.Path.Combine(outputFolder,
            //        string.Format("{0}_{1}.png", System.IO.Path.GetFileNameWithoutExtension(DestFileName), page));

            //    PageList.Add(dev.OutputPath);

            //    dev.Process();
            //    ((System.ComponentModel.BackgroundWorker)sender).ReportProgress(page);
            //}
        }

        void backgroundWorker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            //prgAction.Value = e.ProgressPercentage;
            double n = (double)watch.ElapsedMilliseconds;
            //double remain = (double)(prgAction.Maximum - prgAction.Value) * (n / prgAction.Value);

            //lblInfo.Text = message + " " + (int)(prgAction.Value * 100 / prgAction.Maximum) + "%..."
                //+ Math.Round(n / 1000, 3) + "s (-" + Math.Round(remain / 1000, 3) + "s)";
        }

        void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            //prgAction.Value = 0;
            OnToImageCompleted(this, EventArgs.Empty);

            //lblInfo.Text = "Completed in ..."
                //+ Math.Round((double)watch.ElapsedMilliseconds / 1000, 3) + "s";
            watch.Stop();
        }

        protected virtual void OnToImageCompleted(object sender, EventArgs e)
        {
            EventHandler handler = ToImageCompleted;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler ToImageCompleted;

        public string[] Infor
        {
            get
            {
                List<string> res = new List<string> { "Title=General" };
                iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(SourceFileName);
                iTextSharp.text.Document document = new iTextSharp.text.Document(DocumentSize);

                string[] keys = pString.INI_String("TITLEBLOCK_A3_HORIZONTAL").filter(";")
                    .Select(s => s.filter("(").First()).ToArray();

                var info = reader.Info;

                foreach (string k in keys)
                {
                    string st = info.Keys.Contains(k) ? reader.Info[k] : k + "=None";
                    res.Add(st);
                }

                return res.ToArray();
            }

            set
            {
                iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(SourceFileName);
                DocumentSize = reader.GetPageSizeWithRotation(1);
                iTextSharp.text.Document doc = new iTextSharp.text.Document(DocumentSize);

                FileStream fs = new FileStream(DestFileName, FileMode.Create, FileAccess.Write);
                iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(doc, fs);
                //doc.Open();

                // the pdf content


                string[] keys = pString.INI_String("TITLEBLOCK_A3_HORIZONTAL").filter(";")
                    .Select(s => s.filter("(").First()).ToArray();
                pPos[] key_pos = pString.INI_String("TITLEBLOCK_A3_HORIZONTAL").filter(";")
                    .Select(s => pPos.FromString(s._getInComma())).ToArray();

                //using (FileStream fs = new FileStream(DestFileName, FileMode.Create))
                {
                    doc.Open();

                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (value._props(keys[i]).empty())
                        {
                            string content = value._props(keys[i]);
                            //AddText(reader, writer, content, (float)key_pos[i].X, (float)key_pos[i].Y);
                        }
                    }

                    foreach (string st in value)
                        doc.AddHeader(st._firstPropName(), st._firstProp());

                    doc.Close();
                }


                doc.Close();
                fs.Close();
                writer.Close();
                reader.Close();
            }
        }

        public void OverLayPDF(params string[] _overlayFiles)
        {
            overlayFiles = _overlayFiles;

            //prgAction.Maximum = PageCount;
            //prgAction.Value = 0;
            message = "Overlay " + System.IO.Path.GetFileNameWithoutExtension(SourceFileName) + "...";

            System.ComponentModel.BackgroundWorker bW1 = new System.ComponentModel.BackgroundWorker();
            bW1.WorkerReportsProgress = true;
            // This event will be raised on the worker thread when the worker starts
            bW1.DoWork += new System.ComponentModel.DoWorkEventHandler(backgroundWorker2_DoWork);
            // This event will be raised when we call ReportProgress
            bW1.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(backgroundWorker_ProgressChanged);
            bW1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(backgroundWorker2_RunWorkerCompleted);

            watch = System.Diagnostics.Stopwatch.StartNew();
            bW1.RunWorkerAsync();
        }

        public string[] ParamList;

        public void ModifyPDF(params string[] _paramlist)
        {
            ParamList = _paramlist;
            //overlayFiles = _overlayFiles;

            //prgAction.Maximum = PageCount;
            //prgAction.Value = 0;
            message = "Modify [" + System.IO.Path.GetFileNameWithoutExtension(SourceFileName) + "]...";

            System.ComponentModel.BackgroundWorker bW1 = new System.ComponentModel.BackgroundWorker();
            bW1.WorkerReportsProgress = true;
            // This event will be raised on the worker thread when the worker starts
            bW1.DoWork += new System.ComponentModel.DoWorkEventHandler(modify_DoWork);
            bW1.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(backgroundWorker_ProgressChanged);
            bW1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(backgroundWorker2_RunWorkerCompleted);

            watch = System.Diagnostics.Stopwatch.StartNew();
            bW1.RunWorkerAsync();
        }

        public void AddPageSchedule(IEnumerable<string> datas, string title = "", int columns = 1)
        {
            iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(SourceFileName);
            iTextSharp.text.Document inputDoc = new iTextSharp.text.Document(reader.GetPageSizeWithRotation(1));
            string ARIALUNI_TFF = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "TAHOMA.TTF");

            //Create a base font object making sure to specify IDENTITY-H
            iTextSharp.text.pdf.BaseFont bf = iTextSharp.text.pdf.BaseFont.CreateFont(ARIALUNI_TFF,
                iTextSharp.text.pdf.BaseFont.IDENTITY_H, iTextSharp.text.pdf.BaseFont.NOT_EMBEDDED);

            //Create a specific font object
            iTextSharp.text.Font f = new iTextSharp.text.Font(bf, 8f, iTextSharp.text.Font.NORMAL);

            using (FileStream fs = new FileStream(DestFileName, FileMode.Create))
            {
                //Create the PDF Writer to create the new PDF Document
                iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(inputDoc, fs);
                DocumentSize = reader.GetPageSizeWithRotation(1);

                inputDoc.Open();
                inputDoc.SetPageSize(reader.GetPageSizeWithRotation(1));

                //Create the Content Byte to stamp to the wrtiter
                iTextSharp.text.pdf.PdfContentByte cb = writer.DirectContent;
                inputDoc.NewPage();

                iTextSharp.text.pdf.PdfPTable table = new iTextSharp.text.pdf.PdfPTable(columns * 2);

                List<float> widths = new List<float>();

                float w = 370f, w1 = 50f;
                for (int i = 0; i < columns; i++)
                {
                    widths.Add(w1);
                    widths.Add(w - w1);
                }

                table.TotalWidth = w * columns;

                table.SetWidths(widths.ToArray());

                float h = (float)pString.INI_String("PLOT_TITLE_DRAWING_LIST_TITLE_FONT_HEIGHT").ToNumber(8);
                _addText(writer, title, 100f, writer.PageSize.Height - 60f, h);

                for (int i = 0; i < datas.Count(); i ++)
                {
                    string content = datas.ElementAt(i).filter("=").First();
                    if (content == "0")
                        content = "--";

                    iTextSharp.text.pdf.PdfPCell cell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(content, f));
                    cell.FixedHeight = 20f;
                    cell.HorizontalAlignment = 1;
                    cell.VerticalAlignment = 1;
                    table.AddCell(cell);

                    content = datas.ElementAt(i).filter("=").Last().filter(";").First();
                    cell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(content, f));
                    cell.FixedHeight = 20f;
                    cell.HorizontalAlignment = 0;
                    cell.VerticalAlignment = 1;
                    table.AddCell(cell);
                }
                
                //inputDoc.Add(table);
                table.WriteSelectedRows(0, -1, 100f, writer.PageSize.Height - 80f, cb);

                for (int i = 0; i < reader.NumberOfPages; i++)
                {
                    inputDoc.SetPageSize(reader.GetPageSizeWithRotation(i + 1));
                    inputDoc.NewPage();

                    _addTemplate(cb, writer.GetImportedPage(reader, i + 1),
                        reader.GetPageRotation(1), DocumentSize.Width, DocumentSize.Height);
                }

                inputDoc.Close();
                writer.Close();
                writer.Dispose();
            }

            reader.Close();
            reader.Dispose();
        }
        

        public void ModifyPDF(PosCollection pls, float text_height, string id = null, int page = 1)
        {
            int total = PageCount;

            iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(SourceFileName);
            iTextSharp.text.Document inputDoc = new iTextSharp.text.Document(reader.GetPageSizeWithRotation(1));
            List<string> log = new List<string>();

            using (FileStream fs = new FileStream(DestFileName, FileMode.Create))
            {
                //Create the PDF Writer to create the new PDF Document
                iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(inputDoc, fs);
                DocumentSize = reader.GetPageSizeWithRotation(1);

                inputDoc.Open();
                //Create the Content Byte to stamp to the wrtiter
                iTextSharp.text.pdf.PdfContentByte cb = writer.DirectContent;

                pPos[] id_pts = PDFHelper.LoadPlotInfo("ID");

                string[] ar = pls.Name.filter(";");

                Debug.Print("[Collection]" + pls);

                for (int i = 0; i < total; i++)
                    if (pls.Count > i && pls[i].Length > 0)
                    {
                        //Make sure the new page's page size macthes the original document
                        inputDoc.SetPageSize(reader.GetPageSizeWithRotation(i + 1));
                        inputDoc.NewPage();

                        _addTemplate(cb, writer.GetImportedPage(reader, i + 1),
                            reader.GetPageRotation(1), DocumentSize.Width, DocumentSize.Height);

                        iTextSharp.text.pdf.PdfDictionary pageDict = reader.GetPageN(i + 1);
                        //PdfNumber userUnit = pageDict.GetAsNumber(iTextSharp.text.pdf.PdfName.USERUNIT);
                        //Console.OutputEncoding = Encoding.UTF8;
                        //Console.WriteLine("ĐÂY ƯỚC {0}[{2}x{3}]:{1}", i + 1, pls[i].ToText(),
                        //inputDoc.PageSize.Width, inputDoc.PageSize.Height);

                        bool hasId = false;

                        foreach (pPos pt in pls[i])
                            if (!pt.Content.empty() && pt.Content != "--")
                            {
                                string[] ars = pt.Content.filter("%");
                                float h = text_height;

                                if (ars.Length > 1)
                                    h = (float)ars.Last().ToNumber();

                                string st = ars.First();

                                if(st.StartsWith("@"))
                                {
                                    st = st.Substring(1);
                                    hasId = true;
                                }

                                //Debug.Print("Line {0} = {1}", ars.First(), ars.Last());

                                _addText(writer, st, iTextSharp.text.Utilities.MillimetersToPoints((float)pt.X),
                                    DocumentSize.Height - iTextSharp.text.Utilities.MillimetersToPoints((float)(pt.Y)), h);
                            }

                        if (!hasId && id_pts.Length > 0)
                        {
                            int n = page + i;
                            string st = id + "." + (n < 10 ? "00" : "")
                                + (n > 9 && n < 100 ? "0" : "") + n;

                            if (i < ar.Length)
                                log.Add(st + "=" + ar[i]);

                            foreach (pPos pt in id_pts)
                            {
                                string[] ars = pt.Content.filter("%");
                                float h = text_height;

                                if (ars.Length > 1)
                                    h = (float)ars.Last().ToNumber();

                                _addText(writer, "#L" + st, iTextSharp.text.Utilities.MillimetersToPoints((float)pt.X),
                                    DocumentSize.Height - iTextSharp.text.Utilities.MillimetersToPoints((float)(pt.Y)), h);
                            }
                        }
                    }

                inputDoc.Close();
            }
            reader.Close();
            //File.WriteAllLines(DestFileName.Replace(".pdf",".txt"), log.ToArray());
        }

        string[] overlayFiles;
        public string[] Stamps;

        public void SetInfors(IEnumerator<string> info)
        {
            int total = PageCount;
        }

        void _overlayPDF(iTextSharp.text.pdf.PdfReader reader, iTextSharp.text.pdf.PdfWriter writer, int page_index)
        {
            iTextSharp.text.pdf.PdfImportedPage page = writer.GetImportedPage(reader, page_index);
            int rotation = reader.GetPageRotation(page_index);
            iTextSharp.text.pdf.PdfContentByte cb = writer.DirectContent;

            switch (DocumentSize.Rotation)
            {
                case 0:
                    cb.AddTemplate(page, 1f, 0, 0, 1f, 0, 0);
                    break;
                case 90:
                    cb.AddTemplate(page, 0, -1f, 1f, 0, 0, DocumentSize.Height);
                    break;
                case 180:
                    cb.AddTemplate(page, -1f, 0, 0, -1f, 0, 0);
                    break;
                case 270:
                    cb.AddTemplate(page, 0, 1.0F, -1.0F, 0, DocumentSize.Width, 0);
                    break;
                default:
                    break;
            }
        }

        static void _addTemplate(iTextSharp.text.pdf.PdfContentByte cb, 
            iTextSharp.text.pdf.PdfTemplate page, int rotation, float w, float h)
        {
            switch (rotation) // overlayRotations[j]
            {
                case 0:
                    cb.AddTemplate(page, 1f, 0, 0, 1f, 0, 0);
                    break;
                case 90:
                    cb.AddTemplate(page, 0, -1f, 1f, 0, 0, h);
                    break;
                case 180:
                    cb.AddTemplate(page, -1f, 0, 0, -1f, 0, 0);
                    break;
                case 270:
                    cb.AddTemplate(page, 0, 1.0F, -1.0F, 0, w, 0);
                    break;
                default:
                    break;
            }
        }

        public void ChangeFont(string fontname)
        {
            iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(SourceFileName);

            ////Get first page,Generally we get font information on first page,however we can loop throw pages e.g for(int i=0;i<=pdfReader.NumberOfPages;i++)
            iTextSharp.text.pdf.PdfDictionary cpage = reader.GetPageN(1);
            //if (cpage == null)
            //    return;
            iTextSharp.text.pdf.PdfDictionary dictFonts = cpage.GetAsDict(iTextSharp.text.pdf.PdfName.RESOURCES).GetAsDict(iTextSharp.text.pdf.PdfName.FONT);
            if (dictFonts != null)
            {
                foreach (var font in dictFonts)
                {
                    var dictFontInfo = dictFonts.GetAsDict(font.Key);

                    if (dictFontInfo != null)
                    {
                        foreach (var f in dictFontInfo)
                        {
                            //Get the font name-optional code
                            var baseFont = dictFontInfo.Get(iTextSharp.text.pdf.PdfName.BASEFONT);
                            string strFontName = System.Text.Encoding.ASCII.GetString(baseFont.GetBytes(), 0,
                                                                                      baseFont.Length);
                            //

                            //Remove the current font
                            dictFontInfo.Remove(iTextSharp.text.pdf.PdfName.BASEFONT);
                            //Set new font eg. Braille, Areal etc
                            dictFontInfo.Put(iTextSharp.text.pdf.PdfName.BASEFONT, new iTextSharp.text.pdf.PdfString(iTextSharp.text.pdf.BaseFont.COURIER));
                            break;

                        }
                    }
                }
            }

            //Now create a new document with updated font
            using (FileStream FS = new FileStream(DestFileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (iTextSharp.text.Document Doc = new iTextSharp.text.Document())
                {
                    using (iTextSharp.text.pdf.PdfCopy writer = new iTextSharp.text.pdf.PdfCopy(Doc, FS))
                    {
                        Doc.Open();
                        for (int j = 1; j <= reader.NumberOfPages; j++)
                        {
                            writer.AddPage(writer.GetImportedPage(reader, j));
                        }
                        Doc.Close();
                    }
                }
            }
        }

        private void modify_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            int total = PageCount;

            iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(SourceFileName);
            iTextSharp.text.Document inputDoc = new iTextSharp.text.Document(reader.GetPageSizeWithRotation(1));

            using (FileStream fs = new FileStream(DestFileName, FileMode.Create))
            {
                //Create the PDF Writer to create the new PDF Document
                iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(inputDoc, fs);
                DocumentSize = reader.GetPageSizeWithRotation(1);

                inputDoc.Open();
                //Create the Content Byte to stamp to the wrtiter
                iTextSharp.text.pdf.PdfContentByte cb = writer.DirectContent;

                List<iTextSharp.text.pdf.PdfReader[]> overlay_readers = new List<iTextSharp.text.pdf.PdfReader[]>();

                foreach (string content in ParamList)
                    if (content.StartsWith("#OVERLAY"))
                        overlay_readers.Add(content._firstProp().filter(";")
                                .Select(f => new iTextSharp.text.pdf.PdfReader(f)).ToArray());

                float text_height = (float)pString.INI_String("PDFTEXTHEIGHT").ToNumber();

                for (int i = 1; i <= total; i++)
                {
                    //Make sure the new page's page size macthes the original document
                    inputDoc.SetPageSize(reader.GetPageSizeWithRotation(i));
                    inputDoc.NewPage();

                    _addTemplate(cb, writer.GetImportedPage(reader, i),
                            reader.GetPageRotation(1), DocumentSize.Width, DocumentSize.Height);

                    int overlay_reader_index = 0;
                    foreach (string content in ParamList)
                    {
                        if (content.StartsWith("#OVERLAY"))
                        {
                            iTextSharp.text.pdf.PdfReader[] overlayReaders = overlay_readers[overlay_reader_index];
                            for (int j = 0; j < overlayReaders.Length; j++)
                                _addTemplate(cb, writer.GetImportedPage(overlayReaders[j], 1),
                                    overlayReaders[j].GetPageRotation(1), DocumentSize.Width, DocumentSize.Height);

                            overlay_reader_index++;
                        }
                        else if (content.StartsWith("#TXT"))
                        {
                            foreach (string st in content._firstProp().filter(";"))
                            {
                                pPos pt = pPos.FromString(st);
                                _addText(writer, st._getInComma(), iTextSharp.text.Utilities.MillimetersToPoints((float)pt.X),
                                    DocumentSize.Height - iTextSharp.text.Utilities.MillimetersToPoints((float)pt.Y), text_height);
                            }
                        }
                    }
                    ((System.ComponentModel.BackgroundWorker)sender).ReportProgress(i);
                }

                inputDoc.Close();

                //string[] infors = PDFEditorCLS.ReadXmpMetadata(SourceFileName);
                //PDFEditorCLS.WriteXmpMetadata(DestFileName, infors.ToDictionary(k => k, k => infors._prop(k)));

                foreach (var ls in overlay_readers)
                    foreach (var overlayReader in ls)
                        overlayReader.Close();
            }

            reader.Close();
        }

        public pPos GetDocumentSize()
        {
            iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(SourceFileName);
            DocumentSize = reader.GetPageSizeWithRotation(1);
            reader.Close();
            double scale = 25.4 / 72;
            return new pPos(DocumentSize.Width * scale, DocumentSize.Height * scale);
        }

        private void backgroundWorker2_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            int total = PageCount;

            iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(SourceFileName);
            iTextSharp.text.Document inputDoc = new iTextSharp.text.Document(reader.GetPageSizeWithRotation(1));

            using (FileStream fs = new FileStream(DestFileName, FileMode.Create))
            {
                //Create the PDF Writer to create the new PDF Document
                iTextSharp.text.pdf.PdfWriter outputWriter = iTextSharp.text.pdf.PdfWriter.GetInstance(inputDoc, fs);
                DocumentSize = reader.GetPageSizeWithRotation(1);

                inputDoc.Open();
                //Create the Content Byte to stamp to the wrtiter
                iTextSharp.text.pdf.PdfContentByte cb = outputWriter.DirectContent;

                //Get the PDF document to use as overlay

                iTextSharp.text.pdf.PdfReader[] overlayReaders = overlayFiles.Select(f => new iTextSharp.text.pdf.PdfReader(f)).ToArray();
                iTextSharp.text.pdf.PdfImportedPage[] overlayPages = overlayReaders.Select(o => outputWriter.GetImportedPage(o, 1)).ToArray();

                //Get the overlay page rotation
                int[] overlayRotations = overlayReaders.Select(o => o.GetPageRotation(1)).ToArray();

                for (int i = 1; i <= total; i++)
                {
                    //Make sure the new page's page size macthes the original document
                    inputDoc.SetPageSize(reader.GetPageSizeWithRotation(i));
                    inputDoc.NewPage();

                    iTextSharp.text.pdf.PdfImportedPage page = outputWriter.GetImportedPage(reader, i);
                    int rotation = reader.GetPageRotation(i);
                    
                    switch (DocumentSize.Rotation)
                    {
                        case 0:
                            cb.AddTemplate(page, 1f, 0, 0, 1f, 0, 0);
                            break;
                        case 90:
                            cb.AddTemplate(page, 0, -1f, 1f, 0, 0, DocumentSize.Height);
                            break;
                        case 180:
                            cb.AddTemplate(page, -1f, 0, 0, -1f, 0, 0);
                            break;
                        case 270:
                            cb.AddTemplate(page, 0, 1.0F, -1.0F, 0, DocumentSize.Width, 0);
                            break;
                        default:
                            break;
                    }

                    for (int j = 0; j < overlayPages.Length; j++)
                    {
                        iTextSharp.text.pdf.PdfImportedPage pg = overlayPages[j];
                        //page = outputWriter.GetImportedPage(overlayReader, 1);

                        switch (overlayRotations[j])
                        {
                            case 0:
                                cb.AddTemplate(pg, 1f, 0, 0, 1f, 0, 0);
                                break;
                            case 90:
                                cb.AddTemplate(pg, 0, -1f, 1f, 0, 0, DocumentSize.Height);
                                break;
                            case 180:
                                cb.AddTemplate(pg, -1f, 0, 0, -1f, 0, 0);
                                break;
                            case 270:
                                cb.AddTemplate(pg, 0, 1.0F, -1.0F, 0, DocumentSize.Width, 0);
                                break;
                            default:
                                break;
                        }
                    }

                    ((System.ComponentModel.BackgroundWorker)sender).ReportProgress(i);
                }

                if (Stamps != null && Stamps.Length > 0)
                    _addStamp(inputDoc);

                inputDoc.Close();
                //Close the reader for the overlay file

                foreach (var overlayReader in overlayReaders)
                    overlayReader.Close();
            }

            reader.Close();
        }

        void backgroundWorker2_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            //prgAction.Value = 0;
            OnToOverlayCompleted(this, EventArgs.Empty);

            //lblInfo.Text = "Completed in ..."
                //+ Math.Round((double)watch.ElapsedMilliseconds / 1000, 3) + "s";
            watch.Stop();
        }

        protected virtual void OnToOverlayCompleted(object sender, EventArgs e)
        {
            EventHandler handler = ToOverlayCompleted;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler ToOverlayCompleted;

        public PDFEditCLS(string _sourcefile, string _destfile = null)
        {
            SourceFileName = _sourcefile;
            DestFileName = _destfile;
        }

        public iTextSharp.text.Rectangle DocumentSize;

        public int PageCount
        {
            get
            {
                iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(SourceFileName);
                return reader.NumberOfPages;
            }
        }

        void _addStamp(iTextSharp.text.Document doc)
        {
            foreach (string st in Stamps)
                doc.AddHeader(st._firstPropName(), st._firstProp());
        }

        public void AddCicle()
        {
            // open the reader
            iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(SourceFileName);
            DocumentSize = reader.GetPageSizeWithRotation(1);
            iTextSharp.text.Document document = new iTextSharp.text.Document(DocumentSize);

            FileStream fs = new FileStream(DestFileName, FileMode.Create, FileAccess.Write);
            iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, fs);
            document.Open();

            // the pdf content
            iTextSharp.text.pdf.PdfContentByte cb = writer.DirectContent;

            cb.SetColorStroke(iTextSharp.text.BaseColor.GREEN);
            cb.Circle(150f, 150f, 50f);
            cb.Stroke();

            if (Overlay)
            {
                iTextSharp.text.pdf.PdfImportedPage page = writer.GetImportedPage(reader, 1);
                cb.AddTemplate(page, 0, 0);
            }

            // close the streams and voilá the file should be changed :)
            document.Close();
            fs.Close();
            writer.Close();
            reader.Close();
        }

        public string FontName = "Arial";
        public int FontSize = 12;

        public static string AddText(string filename, pPos pt, string fontname, float text_height = 10)
        {
            iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(filename);
            iTextSharp.text.Rectangle pagesize = reader.GetPageSizeWithRotation(1);

            iTextSharp.text.Document document = new iTextSharp.text.Document(pagesize);

            string newfile = filename.ToLower().Replace(".pdf", "_tmp.pdf");
            FileStream fs = new FileStream(newfile, FileMode.Create, FileAccess.Write);
            iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, fs);
            document.Open();

            iTextSharp.text.pdf.PdfContentByte cb = writer.DirectContent;

            string ARIALUNI_TFF = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), fontname);

            //Create a base font object making sure to specify IDENTITY-H
            iTextSharp.text.pdf.BaseFont bf = iTextSharp.text.pdf.BaseFont.CreateFont(ARIALUNI_TFF, 
                iTextSharp.text.pdf.BaseFont.IDENTITY_H, iTextSharp.text.pdf.BaseFont.NOT_EMBEDDED);

            //Create a specific font object
            iTextSharp.text.Font f = new iTextSharp.text.Font(bf, text_height, iTextSharp.text.Font.NORMAL);

            int align = 0;
            string content = pt.Content;
            if (content.StartsWith("#L"))
            {
                align = 0;
                content = content.Substring(2);
            }
            else if (content.StartsWith("#R"))
            {
                align = 2;
                content = content.Substring(2);
            }
            else if (content.StartsWith("#C"))
            {
                align = 1;
                content = content.Substring(2);
            }

            for (int i = 0; i < reader.NumberOfPages; i++)
            {
                //Make sure the new page's page size macthes the original document
                document.SetPageSize(reader.GetPageSizeWithRotation(i + 1));
                document.NewPage();

                _addTemplate(cb, writer.GetImportedPage(reader, i + 1),
                        reader.GetPageRotation(1), pagesize.Width, pagesize.Height);

                iTextSharp.text.Phrase ph = new iTextSharp.text.Phrase(content, f);
                iTextSharp.text.pdf.ColumnText.ShowTextAligned(cb, align, ph, (float)pt.X, (float)pt.Y, 0);
            }

            document.Close();
            fs.Close();
            writer.Close();
            reader.Close();

            return newfile;
        }
        
        void _addText(iTextSharp.text.pdf.PdfWriter writer, string content, float mmx, float mmy, float text_height = 0)
        {
            iTextSharp.text.pdf.PdfContentByte cb = writer.DirectContent;

            string ARIALUNI_TFF = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "TAHOMA.TTF");

            //Create a base font object making sure to specify IDENTITY-H
            iTextSharp.text.pdf.BaseFont bf = iTextSharp.text.pdf.BaseFont.CreateFont(ARIALUNI_TFF, iTextSharp.text.pdf.BaseFont.IDENTITY_H, iTextSharp.text.pdf.BaseFont.NOT_EMBEDDED);

            //Create a specific font object
            iTextSharp.text.Font f = new iTextSharp.text.Font(bf, text_height, iTextSharp.text.Font.NORMAL);

            int align = 0;
            if (content.StartsWith("#L"))
            {
                align = 0;
                content = content.Substring(2);
            }
            else if (content.StartsWith("#R"))
            {
                align = 2;
                content = content.Substring(2);
            }
            else if (content.StartsWith("#C"))
            {
                align = 1;
                content = content.Substring(2);
            }

            if (text_height <= 0)
                text_height = FontSize;
            iTextSharp.text.Phrase ph = new iTextSharp.text.Phrase(content, f);

            iTextSharp.text.pdf.ColumnText.ShowTextAligned(cb, align, ph, mmx, mmy, 0);
        }

        float ruler(float val)
        {
            return iTextSharp.text.Utilities.MillimetersToPoints(val);
        }

        public void AddTable(string content)
        {
            iTextSharp.text.pdf.PdfReader reader = new iTextSharp.text.pdf.PdfReader(SourceFileName);
            DocumentSize = reader.GetPageSizeWithRotation(1);

            iTextSharp.text.Document document = new iTextSharp.text.Document(DocumentSize);

            FileStream fs = new FileStream(DestFileName, FileMode.Create, FileAccess.Write);
            iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, fs);
            document.Open();

            // the pdf content
            iTextSharp.text.pdf.PdfContentByte cb = writer.DirectContent;
            iTextSharp.text.pdf.PdfPCell cell = null;
            iTextSharp.text.pdf.PdfPTable table = new iTextSharp.text.pdf.PdfPTable(2);

            table.HorizontalAlignment = iTextSharp.text.Element.ALIGN_LEFT;
            table.SetWidths(new float[] { 0.3f, 1f });
            table.SpacingBefore = 20f;

            cell =PhraseCell(new iTextSharp.text.Phrase("Cell 1",
                iTextSharp.text.FontFactory.GetFont(FontName, FontSize, 0, iTextSharp.text.BaseColor.BLACK)), iTextSharp.text.pdf.PdfPCell.ALIGN_CENTER);
            table.AddCell(cell);

            cell =PhraseCell(new iTextSharp.text.Phrase("Cell 2",
                iTextSharp.text.FontFactory.GetFont(FontName, FontSize, 0, iTextSharp.text.BaseColor.BLACK)), iTextSharp.text.pdf.PdfPCell.ALIGN_CENTER);
            table.AddCell(cell);

            iTextSharp.text.pdf.ColumnText ct = new iTextSharp.text.pdf.ColumnText(cb);
            ct.AddElement(table);
            iTextSharp.text.Rectangle rect = new iTextSharp.text.Rectangle(46, 190, 530, 36);

            ct.SetSimpleColumn(rect);
            ct.Go();
            //stamper.Close();
            //This is Dummy Table you can

            if (Overlay)
            {
                iTextSharp.text.pdf.PdfImportedPage page = writer.GetImportedPage(reader, 1);
                cb.AddTemplate(page, 0, 0);
            }

            // close the streams and voilá the file should be changed :)
            document.Close();
            fs.Close();
            writer.Close();
            reader.Close();
        }

        private iTextSharp.text.pdf.PdfPCell PhraseCell(iTextSharp.text.Phrase phrase, int align)
        {
            iTextSharp.text.pdf.PdfPCell cell = new iTextSharp.text.pdf.PdfPCell(phrase);
            cell.BorderColor = iTextSharp.text.BaseColor.WHITE;
            cell.VerticalAlignment = iTextSharp.text.pdf.PdfPCell.ALIGN_TOP;
            cell.HorizontalAlignment = align;
            cell.PaddingBottom = 2f;
            cell.PaddingTop = 0f;
            return cell;
        }
    }
}
