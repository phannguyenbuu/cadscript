using System;
using System.Collections.Generic;
using System.Linq;

//using System.Threading.Tasks;
//using iTextSharp.text.pdf.parser;
//using iTextSharp.text;
//using iTextSharp.text.pdf;
using System.IO;
//using SyncObject;

namespace AcadScript
{ 
    
    public static class OverlayPDF
    {
        static void _addTemplate(iTextSharp.text.pdf.PdfContentByte cb, iTextSharp.text.pdf.PdfTemplate page, int rotation,
            float x, float y)
        {
            float w = page.Width, h = page.Height;
            x = iTextSharp.text.Utilities.MillimetersToPoints(x);
            y = iTextSharp.text.Utilities.MillimetersToPoints(y);

            switch (rotation) // overlaXRotations[j]
            {
                case 0:
                    cb.AddTemplate(page, 1f, 0, 0, 1f, x, y);
                    break;
                case 90:
                    cb.AddTemplate(page, 0, -1f, 1f, 0, y, w + x);
                    break;
                case 180:
                    cb.AddTemplate(page, -1f, 0, 0, -1f, x, y);
                    break;
                case 270:
                    cb.AddTemplate(page, 0, 1.0F, -1.0F, 0, h + x, y);
                    break;
                default:
                    break;
            }
            //ACD.WR("Value {0},{1}", h + Utilities.MillimetersToPoints(x), Utilities.MillimetersToPoints(y));
        }

        public static void Overlay(IEnumerable<pPos> points, string save_file)
        {
            float opacity = ACD.PLOT_OVERLAY; //(float)DE.INI_String("PlotOverlay").ToNumber(1);
            //ACD.WR("Opacity: {0}", opacity);
            iTextSharp.text.pdf.PdfReader[] ls = points.Select(p => new iTextSharp.text.pdf.PdfReader(p.Content)).ToArray();
            iTextSharp.text.Rectangle pagesize = ls[0].GetPageSizeWithRotation(1);
            
            iTextSharp.text.Document document = new iTextSharp.text.Document(new iTextSharp.text.Rectangle(pagesize));
            //ACD.WR("PF1");
            System.IO.FileStream fs = new System.IO.FileStream(save_file, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, fs);
            document.Open();
            //ACD.WR("PF2");
            // the pdf content
            iTextSharp.text.pdf.PdfContentByte cb = writer.DirectContent;
            
            cb.SaveState();
            
            for (int i = 0; i < points.Count(); i++)
            {
                iTextSharp.text.pdf.PdfReader overlay_reader = ls[i];
                float x = (float)points.ElementAt(i).X, y = (float)points.ElementAt(i).Y;

                //if (i >= 1)
                //{
                //    PdfGState gstate = new PdfGState();
                //    gstate.FillOpacity = opacity;
                //    gstate.StrokeOpacity = opacity;

                //    cb.SetGState(gstate);
                //}

                for (int pg = 1; pg <= overlay_reader.NumberOfPages; pg++)
                    _addTemplate(cb, writer.GetImportedPage(overlay_reader, pg),
                            overlay_reader.GetPageRotation(1), x, y);
            }

            cb.RestoreState();
            document.Close();
            //ACD.WR("OK5");
            fs.Close();
            //ACD.WR("OK6");
            writer.Close();
            //ACD.WR("OK7");
            foreach(iTextSharp.text.pdf.PdfReader reader in ls)
                reader.Close();
            //ACD.WR("OK8");
        }
    }
    

    
    public class RectAndText
    {
        public iTextSharp.text.Rectangle Rect;
        public String Text;
        public RectAndText(iTextSharp.text.Rectangle rect, String text)
        {
            this.Rect = rect;
            this.Text = text;
        }
    }
    //And here's the subclass:
    
    

    public class clsPDFAddText
    {
        iTextSharp.text.pdf.PdfWriter writer;
        iTextSharp.text.Document document;
        iTextSharp.text.pdf.PdfContentByte cb;
        FileStream fs;
        iTextSharp.text.pdf.PdfReader reader;
        iTextSharp.text.Rectangle pagesize;
        public string OutputFile;
        int align = 0;
        iTextSharp.text.Font font;
        float PDF_SCALE_VALUE = 72f / 25.4f;
        List<iTextSharp.text.pdf.PdfReader> overlay_readers, addition_readers;
        string[] additionfiles, endfiles;
        
        public clsPDFAddText(string filename, string[] _additionfiles = null, string[] _endfiles = null)
        {
            

            ACD.WR("Filename {0}", filename);
            
            //reader = new iTextSharp.text.pdf.PdfReader(filename);

            using (Stream pdfStream = new FileStream(filename, FileMode.Open))
            {
                pdfStream.Position = 0;

                reader = new iTextSharp.text.pdf.PdfReader(pdfStream);
                addition_readers = new List<iTextSharp.text.pdf.PdfReader>();
                overlay_readers = new List<iTextSharp.text.pdf.PdfReader>();

                pagesize = reader.GetPageSizeWithRotation(1);

                document = new iTextSharp.text.Document(pagesize);

                OutputFile = filename.ToLower().Replace(".pdf", "_tmp.pdf");
                //ACD.WR("OutputFile {0}", OutputFile);
                fs = new FileStream(OutputFile, FileMode.Create, FileAccess.Write);
                writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, fs);

                document.Open();
                cb = writer.DirectContent;

                additionfiles = _additionfiles;
                endfiles = _endfiles;

                _clonePages();
            }
        }
        
        void _clonePages()
        {
            if(additionfiles != null && additionfiles.Length > 0)
            {
                foreach (string f in additionfiles)
                {
                    //ACD.WR("AddFile {0}", f);
                    iTextSharp.text.pdf.PdfReader r = new iTextSharp.text.pdf.PdfReader(f);
                    addition_readers.Add(r);

                    for (int i = 1; i <= r.NumberOfPages; i++)
                    {
                        document.SetPageSize(reader.GetPageSizeWithRotation(1));
                        document.NewPage();

                        AddTemplate(writer.GetImportedPage(r, i), reader.GetPageRotation(1));
                    }
                    //r.Close();
                }
            }

            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                //Make sure the new page's page size macthes the original document
                document.SetPageSize(reader.GetPageSizeWithRotation(1));
                document.NewPage();

                AddTemplate(writer.GetImportedPage(reader, i), reader.GetPageRotation(1));
            }
        }

        void _getFont(string fontname, float text_height = 10)
        {
            string ARIALUNI_TFF = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), fontname);
            iTextSharp.text.pdf.BaseFont bf = iTextSharp.text.pdf.BaseFont.CreateFont(ARIALUNI_TFF,
                iTextSharp.text.pdf.BaseFont.IDENTITY_H, iTextSharp.text.pdf.BaseFont.NOT_EMBEDDED);

            //Create a specific font object
            font = new iTextSharp.text.Font(bf, text_height, iTextSharp.text.Font.NORMAL);
        }

        void _getAlign(string content)
        {
            align = 0;
            
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
        }

        public void AddPageWithObject(IEnumerable<int> page_numbers = null, Action<object> act = null)
        {
            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                ////Make sure the new page's page size macthes the original document
                //document.SetPageSize(reader.GetPageSizeWithRotation(i));
                //document.NewPage();

                //AddTemplate(writer.GetImportedPage(reader, i), reader.GetPageRotation(1));
                
                if ((page_numbers == null || page_numbers.Contains(i)) && act != null)
                    act(current_pos);
            }
        }

        void _addText(pPos pt)
        {
            iTextSharp.text.Phrase ph = new iTextSharp.text.Phrase(pt.Content, font);

            iTextSharp.text.pdf.ColumnText.ShowTextAligned(cb, align, ph,
                (float)pt.X * PDF_SCALE_VALUE,
                pagesize.Height - (float)pt.Y * PDF_SCALE_VALUE, (float)pt.Rotation);
        }

        public pPos current_pos;

        public void AddText(int page_number, pPos pt, string fontname,  float text_height = 10)
        {
            _getFont(fontname, text_height);
            _getAlign(pt.Content);

            iTextSharp.text.pdf.PdfAction.GotoLocalPage(page_number, new iTextSharp.text.pdf.PdfDestination(0), writer);
            _addText(pt);
        }

        public void AddText(pPos pt, string fontname, float text_height = 10)
        {
            _getFont(fontname, text_height);
            _getAlign(pt.Content);

            for(int i = 1; i < reader.NumberOfPages; i++)
            {
                iTextSharp.text.pdf.PdfAction.GotoLocalPage(i, new iTextSharp.text.pdf.PdfDestination(0), writer);
                _addText(pt);
            }
        }

        
        public void Overlay(params string[] files)
        {
            float opacity = ACD.PLOT_OVERLAY; 
            overlay_readers.AddRange(files.Select(f => new iTextSharp.text.pdf.PdfReader(f)));

            iTextSharp.text.pdf.PdfGState gstate = new iTextSharp.text.pdf.PdfGState();
            gstate.FillOpacity = opacity;
                //.setFillOpacity(0.3f);
            gstate.StrokeOpacity = opacity;

            //PdfContentByte contentunder = writer.getDirectContentUnder();
            cb.SaveState();
            cb.SetGState(gstate);

            foreach (var overlayReader in overlay_readers)
                for (int i = 1; i <= overlayReader.NumberOfPages; i++)
                {
                    AddTemplate(writer.GetImportedPage(overlayReader, i), overlayReader.GetPageRotation(1));
                }

            //cb.endText();
            cb.RestoreState();
        }

        public void Close()
        {
            if (endfiles != null && endfiles.Length > 0)
            {
                foreach (string f in endfiles)
                {
                    iTextSharp.text.pdf.PdfReader r = new iTextSharp.text.pdf.PdfReader(f);
                    addition_readers.Add(r);

                    for (int i = 1; i <= r.NumberOfPages; i++)
                    {
                        document.SetPageSize(reader.GetPageSizeWithRotation(i));
                        document.NewPage();

                        AddTemplate(writer.GetImportedPage(r, i), reader.GetPageRotation(1));
                    }
                    
                }
            }

            //ACD.WR("OK4");
            document.Close();
            //ACD.WR("OK5");
            fs.Close();
            //ACD.WR("OK6");
            writer.Close();
            //ACD.WR("OK7");
            reader.Close();

            foreach (var r in overlay_readers)
                r.Close();

            foreach (var r in addition_readers)
                r.Close();
        }
        
        void AddTemplate(iTextSharp.text.pdf.PdfTemplate page, int rotation, float x = 0f, float y = 0f)
        {
            float w = pagesize.Width, h = pagesize.Height;
            switch (rotation) // overlaXRotations[j]
            {
                case 0:
                    cb.AddTemplate(page, 1f, 0, 0, 1f, x, y);
                    break;
                case 90:
                    cb.AddTemplate(page, 0, -1f, 1f, 0, x, h - y);
                    break;
                case 180:
                    cb.AddTemplate(page, -1f, 0, 0, -1f, x, y);
                    break;
                case 270:
                    cb.AddTemplate(page, 0, 1.0F, -1.0F, 0, w - x, y);
                    break;
                default:
                    break;
            }
        }
    }
    
}
