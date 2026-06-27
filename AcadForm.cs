using System;
using System.Collections.Generic;
//using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using AcadScript;

namespace AcadScript
{
    public partial class AcadForm : Form
    {
        public static string DefaultFileName { get { return "<untitled>"; } }
        string content;
        public string FileName = DefaultFileName;
        Dictionary<string, string> scale_list;

        public AcadForm()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            //this.Width = 200;
            
            Config.LoadAcadScriptAssembly();

            ACD.cbCategory = cbCategory;
            ACD.cbHatch = cbHatch;
            ASRun.OutputText = richTextBox2;
            ACD.cbPlot = cbPlotBy;
            ACD.cbLayerKey = cbLayer;
            ACD.cbScale = cbScale;

            MenuItem addDevice = new MenuItem("Add Device");
            addDevice.MenuItems.Add(new MenuItem("Add More .."));

            //ACD.WR("OK3");
            reloadToolStripMenuItem_Click();
            ACD.ProgressLoad = prgLoad;
            ACD.ProgressPercent = lblPercent;
            //cbPlotBy.SelectedIndex = 0;
        }
        static string[] _fileInCodes
        {
            get
            {
                return Directory.GetFiles(DE.CADLIB_CONSTRUCT + "Code", "*.cs");
            }
        }
        

        string[] IncludePublic(string filename)
        {
            string dir = @"D:\Dropbox\VS Projects\ACadScript\Public\";
            List<string> files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories).ToList();
            
            files.Add(filename);

            List<string> res = new List<string>();
            List<string> using_list = new List<string>();

            foreach (string f in files)
            {
                string[] str = File.ReadAllLines(f);

                for(int i = 0; i < str.Length; i++)
                {
                    if (str[i].StartsWith("using"))
                        using_list.Add(str[i]);
                    else
                        res.Add(str[i]);
                        //str[i] = "//" + str[i];
                }

                //res.AddRange(str);
            }

            using_list = using_list.Distinct().ToList();
            using_list.AddRange(res);
            res = using_list;

            File.WriteAllLines(@"D:\publiclog.txt", res.ToArray());

            return res.ToArray();
        }

        
        bool Open()
        {
            if (!ModifiedCheckCanContinue())
                return false;
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
                return false;

            FileName = openFileDialog1.FileName;
            ASRun.OpenScript(openFileDialog1.FileName);
            return true;
        }
        
        bool Save(bool bForceShowDialog = false)
        {
            if (FileName == DefaultFileName || bForceShowDialog)
            {
                if (saveFileDialog1.ShowDialog() != DialogResult.OK)
                    return false;
                FileName = saveFileDialog1.FileName;
            }
            File.WriteAllText(FileName, content);
            //richTextBox1.Modified = false;
            return true;
        }

        bool ModifiedCheckCanContinue()
        {
            //if (!richTextBox1.Modified)
            //    return true;
            //var r = MessageBox.Show("Text modified, save?", "Save and continue", MessageBoxButtons.YesNoCancel);

            //if (r == DialogResult.Cancel)
            //    return false;
            //else if (r == DialogResult.No)
            //    return true;
            //else
            //    return Save();
            return true;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Open();
        }

        private void EditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true; // Never really close.
            if (ModifiedCheckCanContinue())
                Hide();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();
        }
        
        private void richTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Tab:
                    {
                        //richTextBox1.SelectedText = "    ";
                        e.Handled = true;
                        break;
                    }
                case Keys.Enter:
                    {
                        //int a = richTextBox1.GetFirstCharIndexOfCurrentLine();
                        //int b = richTextBox1.SelectionStart;
                        //string tmp = content.Substring(a, b - a);
                        //int n = tmp.Length - tmp.TrimStart().Length;
                        //string indent = tmp.Substring(0, n);
                        //richTextBox1.SelectedText = "\n" + indent;
                        e.Handled = true;
                    }
                    break;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Tab)
            {
                //richTextBox1.SelectedText = "    ";
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private int CurrentCharIndex
        {
            get { return richTextBox2.SelectionStart - richTextBox2.GetFirstCharIndexOfCurrentLine(); }
        }

        private int CurrentLineIndex
        {
            get { return richTextBox2.GetLineFromCharIndex(richTextBox2.SelectionStart); }
        }

        private void richTextBox2_SelectionChanged(object sender, EventArgs e)
        {
            //this.toolStripStatusLabel1.Text = String.Format("Column {0}, Line {1}", CurrentCharIndex, CurrentLineIndex);
        }

        private void richTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\t')
                e.Handled = true;
        }

        private void saveToolStripMenuItem1_Click_1(object sender, EventArgs e)
        {
            Save(true);
        }

        private void openConfigurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!ModifiedCheckCanContinue())
                return;
            ASRun.OpenScript(Config.FilePath);
        }

        private void runToolStripMenuItem_Click(object sender = null, EventArgs e = null)
        {
            ASRun.CompileAndRun(content);
        }

        private void pythonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //pythonToolStripMenuItem.Checked = true;
            //cToolStripMenuItem.Checked = false;
        }

        private void cToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //cToolStripMenuItem.Checked = true;
            //pythonToolStripMenuItem.Checked = false;
        }

        private void newPythonScriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //SetTextToDefaultPythonScript();
        }

        private void newToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            //if (!ModifiedCheckCanContinue())
            //    return;
            //New();
        }

        private void EditorForm_Shown(object sender, EventArgs e)
        {
            //pythonToolStripMenuItem.Visible = File.Exists(Path.Combine(Application.StartupPath, "_MaxPlus.pyd"));
        }
        
        private void reloadToolStripMenuItem_Click(object sender = null, EventArgs e = null)
        {
            using (ACD.Lock())
            {
                string[] prams = ACD.LoadChapter(DE.INI_FILE, "TEXT_REMINDER");
                cbText.Items.Clear();
                cbText.Items.AddRange(prams.Select(s => s._firstPropName()).OrderBy(s=>s).ToArray());

                //ACD.WR("OK1");
                cbLayer.Items.Clear();
                cbLayer.Items.AddRange(pString.INI_String("Favourite_Elements").filter(","));

                string scales_ini = pString.INI_String("SCALE_LIST");
                if (string.IsNullOrEmpty(scales_ini))
                    scales_ini = string.Join(";", ACD.DEF_SCALE_LIST.Select(s => "1:" + s));
                
                if (!scales_ini.Contains("1:80"))
                     scales_ini += ";1:80";
                    
                scale_list = scales_ini.filter(";").ToDictionary(s => s.filter("(").First(), s => s._getInComma() ?? "");
                cbScale.Items.Clear();
                cbScale.Items.AddRange(scale_list.Keys.ToArray());
                int idx100 = cbScale.Items.IndexOf("1:100");
                if (idx100 != -1) cbScale.SelectedIndex = idx100;
                //ACD.WR("OK2.1");
                cbDisplay.Items.Clear();
                //ACD.WR("OK2.2");
                //cbDisplay.Items.AddRange(ACD.DB.ListDisplayConfig());
                
                
                //ACD.WR("OK3");
                string[] titles = pString.INI_String("ACAD_BUTTON").filter(";");
                //ACD.WR("OK4");
                Button[] buttons = titles.Select(s => new Button() { Name = s, Top = 25, Width = 35, Height = 35 }).ToArray();
                string dir = pString.INI_String("ACAD_SAMPLE_DIR");
                //ACD.WR("OK New Dir:{0}", dir);
                //ACD.WR("OK5");
                string[] files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
                string[] icons = Directory.GetFiles(dir, "*.png", SearchOption.AllDirectories);
                //ACD.WR("OK2 {0}", icons.Length);
                //ACD.WR("OK4");
                for (int i = 0; i < titles.Length; i++)
                {
                    ToolTip tt = new ToolTip();
                    tt.ShowAlways = true;

                    buttons[i].Left = 10 + i * 40;
                    buttons[i].Text = "";
                    buttons[i].Tag = titles[i];
                    tt.SetToolTip(buttons[i], titles[i]);

                    int index = Array.FindIndex(icons, s =>
                        titles[i].ToUpper().Contains(Path.GetFileNameWithoutExtension(s).ToUpper()));

                    if (index != -1)
                    {
                        System.Drawing.Image img = System.Drawing.Image.FromFile(icons[index]);
                        buttons[i].Image = (System.Drawing.Image)img.Clone();
                        buttons[i].ImageAlign = ContentAlignment.MiddleLeft;
                        img.Dispose();
                    }

                    buttons[i].Click += (o, arg) =>
                    {
                        Button itm = (Button)o;
                        index = Array.FindIndex(files, s 
                            => Path.GetFileNameWithoutExtension(s).ToUpper().Contains(itm.Tag.ToString().ToUpper()));

                        if (index != -1)
                        {
                            FileName = files[index];
                            content = ASRun.OpenScript(FileName);

                            //ACD.WR("File {0} Content {1}", FileName, content);

                            ASRun.CompileAndRun(content);
                        }
                        else
                        {
                            ACD.WR("Cannot find {0}", itm.Text);
                        }
                    };

                    this.Controls.Add(buttons[i]);
                }
                //ACD.WR("OK5");
                //ACD.WR("OK3");
                while (MM.Items.Count > 1)
                    MM.Items.RemoveAt(0);

                string[] subdirs = Directory.GetDirectories(dir).OrderBy(s 
                    => Path.GetFileNameWithoutExtension(s)).Reverse().ToArray();
                //ACD.WR("OK4");
                foreach (string d in subdirs)
                {
                    ToolStripMenuItem sub = new ToolStripMenuItem(Path.GetFileNameWithoutExtension(d));
                    //sub.Name = Path.GetFileNameWithoutExtension(d);
                    MM.Items.Insert(0, sub);
                                        
                    string[] subfiles = Directory.GetFiles(d, "*.cs");
                    string current_prefix = null;

                    foreach (string f in subfiles)
                    {
                        string txt = Path.GetFileNameWithoutExtension(f);

                        if (txt.filter(" ").First().Upper() != current_prefix && !current_prefix.empty())
                            sub.DropDownItems.Add(new ToolStripSeparator());

                        current_prefix = txt.filter(" ").First().Upper();

                        ToolStripItem item = sub.DropDownItems.Add(txt);

                        int index = Array.FindIndex(icons, s =>
                            item.Text.ToUpper().Contains(Path.GetFileNameWithoutExtension(s).ToUpper()));

                        if (index != -1)
                        {
                            System.Drawing.Image img = System.Drawing.Image.FromFile(icons[index]);
                            item.Image = (System.Drawing.Image)img.Clone();
                            item.ImageAlign = ContentAlignment.MiddleLeft;
                            img.Dispose();
                        }

                        item.Click += (o, arg) =>
                        {
                            ToolStripItem itm = (ToolStripItem)o;
                        //string filename = Path.Combine(dir, itm.Owner.Text, itm.Text + ".cs");
                        index = Array.FindIndex(files, s => Path.GetFileNameWithoutExtension(s).ToUpper().Contains(itm.Text.ToUpper()));

                            if (index != -1)
                            {
                                FileName = files[index];
                                content = ASRun.OpenScript(FileName);
                                ASRun.CompileAndRun(content);
                            }
                            else
                            {
                                ACD.WR("Cannot find {0}", itm.Text);
                            }
                        };
                    }

                    //recentsToolStripMenuItem.DropDownItems.Add(sub);
                }
                //ACD.WR("OK6");

                string file = ACD.CADLIB_CONSTRUCT + @"\Import.txt";

                if(File.Exists(file))
                {
                    var dicts = File.ReadAllLines(file).GetChapters();
                    //cbImport.Items.Clear();
                    //cbImport.Items.AddRange(dicts.Keys.OrderBy(s => s).ToArray());
                }
                //ACD.WR("OK7");
            }

            FileName = ACD.FindFullname("Init Hatch");
            ACD.WR("Init Hatch {0}", FileName);

            if (FileName != null)
            {
                content = ASRun.OpenScript(FileName);
                ASRun.CompileAndRun(content);
            }
        }
        
        private void cbScale_SelectedIndexChanged(object sender, EventArgs e)
        {
            using (ACD.Lock())
            {
                string key = cbScale.SelectedItem.ToString();
                ACD.DB.ApplyAnno(key);
                ACD.DB.SetAnnotationScale(key);

                int index = scale_list.Keys.ToList().FindIndex(itm => itm == key);

                if (index != -1)
                {
                    string display = scale_list.Values.ElementAt(index);
                    index = cbDisplay.Items.Cast<string>().ToList()
                        .FindIndex(s => s.st_(display.Upper()));

                    if (index != -1)
                        cbDisplay.SelectedIndex = index;
                }
                ACD.Focus();
            }
        }

        private void cbDisplay_SelectedIndexChanged(object sender = null, EventArgs e = null)
        {
            using (ACD.Lock())
            {
                string display = cbDisplay.SelectedItem.ToString();
                //ACD.WR("Display {0}", display);
                //ACD.SetDisplayConfigCS(display);

                ACD.Focus();
                //ACD.ED.Regen();
            }
        }

        private void ckAnnotative_CheckedChanged(object sender, EventArgs e)
        {
            //ACD.plot_with_annotative = ckAnnotative.Checked;
        }

        private void cbImport_SelectedIndexChanged(object sender, EventArgs e)
        {
            //ACD.CURRENT_IMPORT_CHAPTER = cbImport.SelectedItem.ToString();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            ACD.ProgressCancel = true;
        }


        
        private void cbPlotBy_SelectedIndexChanged(object sender, EventArgs e)
        {
            ACD.PlotMethodType = cbPlotBy.SelectedIndex;

            FileName = ACD.FindFullname("Plot Load Viewport");

            if (FileName != null)
            {
                content = ASRun.OpenScript(FileName);

                ASRun.CompileAndRun(content);
            }
        }

        private void cbLayer_SelectedIndexChanged(object sender, EventArgs e)
        {
            FileName = ACD.FindFullname("Object Layer");

            if (FileName != null)
            {
                content = ASRun.OpenScript(FileName);
                content = content.Replace("<LAYER/>", cbLayer.Items[cbLayer.SelectedIndex].ToString());

                ASRun.CompileAndRun(content);
            }
        }

        private void cbText_SelectedIndexChanged(object sender, EventArgs e)
        {
            FileName = ACD.FindFullname("Text Reminder");

            if (FileName != null)
            {
                content = ASRun.OpenScript(FileName);
                content = content.Replace("<KEYWORD/>", cbText.Items[cbText.SelectedIndex].ToString());

                ASRun.CompileAndRun(content);
            }
        }

        private void AcadForm_Activated(object sender, EventArgs e)
        {
            this.Opacity = 1;
            this.Width = 500;
        }

        private void AcadForm_Deactivate(object sender, EventArgs e)
        {
            this.Opacity = 0.5;
            this.Width = 200;
        }
    }
}