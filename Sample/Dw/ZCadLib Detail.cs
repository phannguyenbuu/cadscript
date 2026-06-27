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
using SyncObject;

namespace AcadScript
{
    public class DVForm : Form
    {
        DataGridView DV;
        ComboBox CB;
        Button btnOK, btnReload;
        PictureBox PV;
        ToolTip tooltip;
        List<string> prams;

        string[] Contents;

        string[] _fileInCodes
        {
            get
            {
                return Directory.GetFiles(DE.CADLIB_CONSTRUCT + "Code", "*.cs");
            }
        }
        
        public DVForm()
        {
            this.Width = 500;
            this.Height = 600;

            DV = new DataGridView()
            {
                Left = 10,
                Width = Width - 40,
                Height = 300,
            };

            CB = new ComboBox()
            {
                Left = DV.Left,
                Width = DV.Right - 130,
                Top = DV.Bottom + 10,
                DropDownStyle = ComboBoxStyle.DropDownList,
                
            };

            PV = new PictureBox()
            {
                Left = DV.Left,
                Width = DV.Right,
                Top = CB.Bottom + 10,
                Height = this.Height - CB.Bottom - 30,
                SizeMode = PictureBoxSizeMode.StretchImage,
            };

            
            btnOK = new Button()
            {
                Left = CB.Right + 5,
                Width = 60,
                Height = 30,
                Top = CB.Top,
                Text = "Run"
            };

            btnReload = new Button()
            {
                Left = btnOK.Right + 5,
                Width = btnOK.Width,
                Height = btnOK.Height,
                Top = btnOK.Top,
                Text = "Reload"
            };

            btnReload.Click += new EventHandler(_reloadFileList);

            tooltip = new ToolTip();
            tooltip.SetToolTip(CB, "");
            tooltip.Popup += new PopupEventHandler(tipButtons_Popup);
            tooltip.Draw += new DrawToolTipEventHandler(tipButtons_Draw);
            tooltip.ShowAlways = true;
            
            this.Controls.AddRange(new Control[] { DV, btnOK, btnReload, CB , PV});

            DV.UpdateEvents();
            DV.BuildColumns("Name=240", "Value = 100");
            
            btnOK.Click += (o, e) =>
            {
                pPos pt = EDSelection.GetPoint();
                pPos basept = new pPos(0,0);
                if (pt != null)
                    basept = pt.Round(50);

                string[] datas = DV.DataViewValues();
                //ACD.WR(datas.ToText("\r\n"));

                bool mode = false;
                List<string> res = new List<string>();

                for (int i = 0; i < Contents.Length; i++)
                    if (!mode)
                    {
                        res.Add(Contents[i]);
                        if (Contents[i].Contains("//[PARAMETERS DEFINED]"))
                        {
                            res.Add("\t\tstatic string BASEPOINT=\"" + basept.ToString() + "\";");
                            res.AddRange(datas.Select(s => "\t\tstatic string " + s._firstPropName() + "=\"" + s._firstProp() + "\";"));
                            mode = true;
                        }
                    }
                    else if (Contents[i].Contains("//[PARAMETERS ENDED]"))
                    {
                        res.Add(Contents[i]);
                        mode = false;

                    }

                Clipboard.SetText(res.ToText("\r\n"));
                Contents = res.ToArray();
                ACD.CompileAndRun(Contents.ToText("\r\n"));

                this.Dispose();
            };

            //CB.DrawItem += (o, e) =>
            //{
            //    if (e.Index == -1)
            //    {
            //        return;
            //    }

            //    previewFile = (DE.CADLIB_CONSTRUCT + @"Code\" + CB.Items[e.Index].ToString()).Replace(".cs", ".jpg");
            //    ACD.WR(previewFile);
            //    if (File.Exists(previewFile))
            //        PV.Image = System.Drawing.Image.FromFile(previewFile);

            //    cbLongTexts.PointToClient(new Point(Control.MousePosition.X + e.Bounds.Width, Control.MousePosition.Y + e.Bounds.Height))
            //    System.Drawing.Point p = new System.Drawing.Point(CB.Location.X, CB.Location.Y + CB.Height);
            //    user mouse is hovering over this drop - down item, update its data
            //    if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            //    {
            //        this tooltip simply shows the displayed text to the right of the drop-down  box, customize as needed
            //        tooltip.Show(CB.Items[e.Index].ToString(), this,
            //            new Point(e.Bounds.X + e.Bounds.Width, e.Bounds.Y));
            //        p);
            //    }
            //    e.DrawBackground();
            //    draw text strings
            //    e.Graphics.DrawString(
            //        CB.Items[e.Index].ToString(),
            //        e.Font,
            //        System.Drawing.Brushes.Black, new System.Drawing.Point(e.Bounds.X, e.Bounds.Y));


            //    string s = CB.Items[e.Index].ToString();
            //    e.DrawBackground();
            //    e.Graphics.DrawString(s, e.Font, System.Drawing.Brushes.Black, e.Bounds.X, e.Bounds.Y);

            //    if (e.State == DrawItemState.Selected)
            //    {
            //        string[] files = _fileInCodes;

            //        this.Text = s;

            //        foreach (string f in files)
            //            if (f.Upper().EndsWith(".JPG") && Path.GetFileNameWithoutExtension(f) == s)
            //                previewFile = f;

            //        ACD.WR(previewFile);
            //        tooltip.Show(Path.GetFileNameWithoutExtension(previewFile), CB);

            //    }
            //};

            CB.SelectedIndexChanged += (o, e) =>
            {
                string sel = CB.SelectedItem.ToString();
                string[] files = _fileInCodes;

                foreach(string f in files)
                    if(Path.GetFileNameWithoutExtension(f) == sel)
                    {
                        bool mode = false;
                        Contents = File.ReadAllLines(f);
                        prams = new List<string>();

                        foreach (string line in Contents)
                            if (line.Contains("//[PARAMETERS DEFINED]"))
                                mode = true;
                            else if (mode)
                            {
                                string s = line.Replace("static string", "").Replace(";", "").Replace("\"", "").Upper();
                                if (s._firstPropName() != "BASEPOINT")
                                    prams.Add(s);

                                if (line.Contains("//[PARAMETERS ENDED]"))
                                    break;
                            }

                        DV.ViewParamData(prams);

                        previewFile = f.Replace(".cs", ".jpg");
                        if (File.Exists(previewFile))
                            PV.Image = System.Drawing.Image.FromFile(previewFile);
                    }
            };

            _reloadFileList();
        }

        void _reloadFileList(object o = null, EventArgs e = null)
        {
            CB.Items.Clear();
            CB.Items.AddRange(_fileInCodes.Select(f => Path.GetFileNameWithoutExtension(f)).Distinct().ToArray());

            if(CB.Items.Count > 0)
                CB.SelectedIndex = 0;
        }
        
        private const int Margin = 10, width = 400, height = 250;
        string previewFile;
        // Set the tooltip's bounds.
        private void tipButtons_Popup(object sender, PopupEventArgs e)
        {
            int image_wid = 2 * Margin + width;
            int image_hgt = 2 * Margin + height;

            int wid = e.ToolTipSize.Width + 2 * Margin + image_wid;
            int hgt = e.ToolTipSize.Height;
            if (hgt < image_hgt)
                hgt = image_hgt;

            e.ToolTipSize = new System.Drawing.Size(wid, hgt);
        }

        // Draw the tooltip.
        private void tipButtons_Draw(object sender, DrawToolTipEventArgs e)
        {
            // Draw the background and border.
            e.DrawBackground();
            e.DrawBorder();

            if(File.Exists(previewFile))
            // Draw the image.
                e.Graphics.DrawImage(System.Drawing.Bitmap.FromFile(previewFile), Margin, Margin);

            // Draw the text.
            using (System.Drawing.StringFormat sf = new System.Drawing.StringFormat())
            {
                sf.Alignment = System.Drawing.StringAlignment.Near;
                sf.LineAlignment = System.Drawing.StringAlignment.Center;

                int image_wid = 2 * Margin + width;

                System.Drawing.Rectangle rect = new System.Drawing.Rectangle(
                    image_wid, 0,
                    e.Bounds.Width - image_wid, e.Bounds.Height);
                e.Graphics.DrawString(
                    e.ToolTipText, e.Font, System.Drawing.Brushes.Green, rect, sf);
            }
        }
    }

    public class DrawingDetailLibrary
    {

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                //string file = @"D:\Dropbox\CADLib\Constructs\Code\Foundation Low Level[M1].cs";
                DVForm frm = new DVForm();

                frm.Show();


                //ACD.Focus();
            }
        }
    }
}
