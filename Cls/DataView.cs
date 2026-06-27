using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
//using System.Threading.Tasks;
//using System.Runtime.InteropServices;

namespace AcadScript
{

    public class LayoutItem
    {
        public string Id, Name, Scale, Drawer, Designer;

        public LayoutItem()
        {
            Id = "A1.01";
            Name = "#Name";
            Scale = "1:100";
            Drawer = "#Drawer";
            Designer = "#Designer";
        }
    }

    public class FXParamItem
    {
        public string FXName;
        public int RegionIndex = -1;
        public string FileDataName;
        public string[] Value;

        public FXParamItem(string fx, string[] val, string file, int region_index)
        {
            FXName = fx;
            Value = val;
            FileDataName = file;
            RegionIndex = region_index;
        }
    }
        
    public class DataViewForm: DataGridView
    {
        //Form Container;
        public string[] FileList;
        public string[] keywords; //FXParamListTitle
        string[] DV_Column_Prams;

        public string[] MasterData;

        //public DataGridView DV;
        Label textLabel1,textLabel2;
        public ComboBox cb1, cb2;

        public int region_counts;

        Button btnSaveFX, btnLoadFX, btnSaveAs;
        int FormWidth = 400, FormHeight = 400; //, cb1_currentindex;

        ProgressBar prgLoad;
        public BackgroundWorker bW;

        Stopwatch watch;

        //bool load_history_param = false;
        public List<FXParamItem> FXParamList;

        //public bool canUpdateEditing;
        
        public DataViewForm(string[] dv_column_prams, 
            int formwidth, int formheight)
        {
            //Container = frm;
            FormWidth = formwidth;
            FormHeight = formheight;
            DV_Column_Prams = dv_column_prams;
            //keywords = keys;
            FXParamList = new List<FXParamItem>();
            //FXParamList = DE.CreateList<string[]>(keywords.Length);
            //load_history_param = false;

            //BuildControl();
        }

        // Back on the 'UI' thread so we can update the progress bar
        void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // The progress percentage is a property of e
            prgLoad.Value = e.ProgressPercentage;
            textLabel1.Text = (int)(prgLoad.Value * 100 / prgLoad.Maximum) + "%..."
                + Math.Round((double)watch.ElapsedMilliseconds / 1000, 3) + "s";
        }

        void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            prgLoad.Value = 0;

            ResetData();
            //ShowFXParamList(CurrentSourceFile, -1);

            textLabel1.Text = "Topic1";
                //+ Math.Round((double)watch.ElapsedMilliseconds / 1000, 3) + "s";
            watch.Stop();
        }

        public void UpdateAllDataFile(object sender = null, DoWorkEventArgs e = null)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                string fx = keywords[i];
                string[] files = Directory.GetFiles(DE.CADLIB_CONSTRUCT, "*.txt", SearchOption.AllDirectories)
                        .Where(f => Path.GetFileName(f).st_(fx)).Select(f => f).ToArray();

                if (files != null && files.Length > 0)
                {
                    //FXParamItem itm = new FXParamItem(fx, File.ReadAllLines(files.First()));
                    //FXParamList.Add(itm);
                }

                bW.ReportProgress(i);
            }
        }

        public string[] ShowFXParamList(string fname, int regionindex = -1)
        {
            FXParamItem[] sels = FXParamList.Where(itm => itm.FileDataName == fname
                    && (regionindex == -1 || itm.RegionIndex == regionindex))
                    .Select(itm => itm).ToArray();
            List<string> str = new List<string>();

            foreach (FXParamItem itm in sels)
            {
                str.Add("Chapter=" + itm.FXName);
                str.AddRange(itm.Value);
            }

            this.ViewParamData(str);

            //cb1.SelectedIndex = -1;

            return str.ToArray();
        }
        
        public Dictionary<string,string[]> Chapters
        {
            get
            {
                return this.DataViewValues().GetChapters();
            }
        }

        public void AddFX(string fname, int regionindex)
        {
            string[] str = this.DataViewValues(true);
            var dict = str.GetChapters();

            string[] keysorts = new string[] { "GRID", "REGION", "LEGEND", "DM" };

            for(int i = 0; i < dict.Count; i ++)
            {
                int index = FXParamList.FindIndex(itm => itm.FXName == dict.Keys.ElementAt(i)
                    && itm.RegionIndex == regionindex && itm.FileDataName.Upper() == fname.Upper());

                //Console.WriteLine("FX Index {0}", index);
                
                if (index != -1)
                {
                    FXParamList[index].Value = dict.Values.ElementAt(i);
                }
                else
                {
                    FXParamList.Add(new FXParamItem(dict.Keys.ElementAt(i),
                        dict.Values.ElementAt(i), fname, regionindex));

                    FXParamList = FXParamList.OrderBy(fx => Array.IndexOf(keysorts, fx.FXName)).ToList();
                }
            }

            //str = File.ReadAllLines(fname);
            //dict = str.GetChapters();

            //List<string> res = new List<string>();
            ////res.Add("Chapter=" + dict.Keys.First());
            //res.AddRange(dict.Values.First());
            //res.Add("\r\n");

            //foreach(var fx in FXParamList)
            //    if(fx.FileDataName == fname)
            //    {
            //        res.Add("Chapter=" + fx.FXName);
            //        res.AddRange(fx.Value);
            //    }

            //File.WriteAllLines(fname, res);
        }

        public void AddEventClick(Action<object, EventArgs> DV_event)
        {
            this.AddDVEventClick(DV_event);
            cb1.SelectedIndexChanged += new  EventHandler(DV_event);
            cb2.SelectedIndexChanged += new EventHandler(DV_event);
        }

        public void BuildControl()
        {
            //canUpdateEditing = false;

            textLabel1 = new Label() { Left = FormWidth - 240, Top = 40, Width = 40, Height = 18, Text = "Topic 1" };
            textLabel2 = new Label() { Left = FormWidth - 240, Top = 70, Width = 40, Height = 18, Text = "Topic 2" };

            cb1 = new ComboBox()
            {
                Left = 10,// textLabel1.Left + textLabel1.Width + 60,
                Top = textLabel1.Top,
                Width = FormWidth,
                DropDownStyle = ComboBoxStyle.DropDownList,

            };

            cb2 = new ComboBox()
            {
                Left = 10,//textLabel1.Left + textLabel1.Width + 60,
                Top = textLabel2.Top,
                Width = FormWidth,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            btnSaveFX = new Button()
            {
                Text = "Save FX",
                Left = this.Left,
                Width = 60,
                Top = this.Top + this.Height + 10,
                Height = 30,
                DialogResult = DialogResult.OK
            };
            btnLoadFX = new Button()
            {
                Text = "Load FX",
                Left = btnSaveFX.Left + btnSaveFX.Width + 20,
                Width = 60,
                Top = Top + Height + 10,
                Height = 30,
                DialogResult = DialogResult.OK
            };
            btnSaveAs = new Button()
            {
                Text = "Save Params",
                Width = 80,
                Left = this.Left + FormWidth - 80,
                Top = this.Top + this.Height + 10,
                Height = 30,
                DialogResult = DialogResult.OK
            };

            prgLoad = new ProgressBar()
            {
                Top = textLabel2.Top,
                Width = 100,
                Height = 10,
                Left = btnSaveFX.Left
            };

            if (this.Parent != null)
            {
                this.Parent.Controls.Add(btnSaveAs);
                //this.Parent.Controls.Add(DV);
                this.Parent.Controls.Add(cb1);
                this.Parent.Controls.Add(cb2);
                this.Parent.Controls.Add(prgLoad);
            }

            cb1.SelectedIndexChanged += (sender, e) =>
            {
                this.Rows.Clear();
                this.Refresh();

                cb2.Items.Clear();

                if (cb1.SelectedIndex != -1)
                    ShowFile(cb1.SelectedItem.ToString());
            };

            //return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : null;
            cb2.SelectedIndexChanged += (sender, e) =>
            {
                string file = CurrentSourceFile;
                    
                if (!file.empty())
                {
                    List<string> str = new List<string> ();

                    str.AddRange(File.ReadAllLines(file).ToList());

                    if (!str.First().st_("CHAPTER="))
                        str.Insert(0,"Chapter=" + Topic1);

                    if (region_counts > 0)
                    {
                        string val = "{!";
                        for (int i = 0; i < region_counts; i++)
                            val += i + ",";
                        val += "}";

                        for (int i = 0; i < str.Count; i++)
                            if (!str[i].empty() && str[i]._firstProp().Upper() == "#REGIONLIST")
                            {
                                str[i] = str[i].Replace("#REGIONLIST", val);
                            }
                    }

                    this.ViewParamData(str);


                    //load_history_param = false;
                    //FXParamList[cb1_currentindex] = DV.DataViewValues(true);
                }
            };

            btnSaveFX.Click += (sender, e) =>
            {
                SaveFileDialog SFD = new SaveFileDialog();

                SFD.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                SFD.FilterIndex = 1;
                SFD.RestoreDirectory = true;

                string file = "Untitled";
                
                SFD.InitialDirectory = DE.CADLIB_CONSTRUCT;
                SFD.FileName = Path.GetFileName(file);

                if (SFD.ShowDialog() == DialogResult.OK)
                {
                    file = SFD.FileName.EndsWith(".txt") ? SFD.FileName : SFD.FileName + "*.txt";

                    List<string> str = new List<string>();
                    foreach(var fx in FXParamList )
                    {
                        str.Add("Chapter=" + fx.FXName);
                        //str.Add(fx.Value);
                        //str.Add()
                    }

                    File.WriteAllLines(file, str);
                }
            };


            btnLoadFX.Click += (sender, e) =>
            {
                string file = CurrentSourceFile;

                if (!file.empty())
                    this.SaveParamData(file);
            };

            btnSaveAs.Click += (sender, e) =>
            {
                var dicts = this.DataViewValues().GetChapters();

                if (dicts.Count > 0)
                {
                    SaveFileDialog SFD = new SaveFileDialog();

                    SFD.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                    SFD.FilterIndex = 1;
                    SFD.RestoreDirectory = true;

                    string file = "";
                    foreach (string k in dicts.Keys)
                        file += k + "_";

                    SFD.InitialDirectory = DE.CADLIB_CONSTRUCT;
                    SFD.FileName = Path.GetFileName(file);

                    if (SFD.ShowDialog() == DialogResult.OK)
                    {
                        file = SFD.FileName.EndsWith(".txt") ? SFD.FileName : SFD.FileName + "*.txt";
                        this.SaveParamData(file);
                    }
                }
            };

            this.UpdateEvents();
            
            watch = Stopwatch.StartNew();

            bW = new BackgroundWorker();
            bW.WorkerReportsProgress = true;
            // This event will be raised on the worker thread when the worker starts
            bW.DoWork += new DoWorkEventHandler(UpdateAllDataFile);
            // This event will be raised when we call ReportProgress
            bW.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);
            bW.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
            //bW.RunWorkerAsync();
        }

        public void ResetData()
        {
            cb1.Items.Clear();

            if (keywords != null)
            {
                cb1.Items.AddRange(keywords);
                prgLoad.Maximum = keywords.Length;
            }
            //cb1_currentindex = -1;
            
            this.Columns.Clear();

            DataGridViewCell cell = new DataGridViewCheckBoxCell();
            DataGridViewColumn col = new DataGridViewColumn();
            col.HeaderText = "";
            col.Name = "";
            col.Width = 20;
            col.CellTemplate = cell;
            this.Columns.Add(col);

            foreach (string st in DV_Column_Prams)
            {
                cell = new DataGridViewTextBoxCell();
                col = new DataGridViewColumn();
                col.HeaderText = st._firstPropName();
                col.Name = col.HeaderText;
                col.Width = (int)st._firstProp().ToNumber(100);
                col.CellTemplate = cell;
                this.Columns.Add(col);
            }
        }

        public string CurrentFXName;
        //{
        //    get
        //    {
        //        return FXParamList[CB1_Index];
        //    }
        //    set
        //    {
        //        FXParamList[CB1_Index] = value;
        //    }
        //}

        public int CB1_Index
        {
            get
            {
                return cb1.SelectedIndex;
            }
        }

        public int CB2_Index
        {
            get
            {
                return cb2.SelectedIndex;
            }
        }

        public string CurrentSourceFile
        {
            get
            {
                string file = cb2.SelectedItem.ToString();
                int index = Array.FindIndex(FileList, f => Path.GetFileName(f) == file);
                return index == -1 ? null : FileList[index];
            }
        }
        
        private void ShowFile(string keyword)
        {
            FileList = Directory.GetFiles(DE.CADLIB_CONSTRUCT, "*.txt", SearchOption.AllDirectories)
                    .Where(f => Path.GetFileName(f).st_(keyword)).Select(f => f).ToArray();

            if (FileList != null && FileList.Length > 0)
            {
                cb2.Items.Clear();
                cb2.Items.AddRange(FileList.Select(f => Path.GetFileName(f)).ToArray());
                cb2.SelectedIndex = 0;
            }
        }
        
        public string Prop(string propname)
        {
            return this.DataViewValues(propname);
        }

        public string[] Params
        {
            get
            {
                return this.DataViewValues();
            }
        }

        public string Topic1
        {
            get
            {
                return cb1.SelectedItem != null ? cb1.SelectedItem.ToString(): null;
            }
        }

        public string Topic2
        {
            get
            {
                return cb2.SelectedItem != null ? cb2.SelectedItem.ToString() : null;
            }
        }
    }

    public class CustomToolTip : ToolTip
    {
        public CustomToolTip()
        {
            this.OwnerDraw = true;
            this.Popup += new PopupEventHandler(this.OnPopup);
            this.Draw += new DrawToolTipEventHandler(OnPaint);
        }

        private void OnPopup(object sender, PopupEventArgs e)
        {
            e.ToolTipSize = new Size(200, 100);
        }

        private void OnPaint(object sender, DrawToolTipEventArgs e)
        {
            //base.OnPaint(e);
            Image img = Image.FromFile(DataGridViewExtension.ViewFilename);
            e.Graphics.DrawImage(img, 0, 0);
            var YourTipTextPoint = new PointF(0, 0);
            e.Graphics.DrawString("Hello World", SystemFonts.DefaultFont, Brushes.Black, YourTipTextPoint);
        }
    }

    public static class DataGridViewExtension
    {
        public static List<string> ExtentDatas;
        public static string ViewFilename;

        public static void BuildColumns(this DataGridView DV, params string[] DV_Column_Prams)
        {
            DV.Columns.Clear();

            DataGridViewCell cell = new DataGridViewCheckBoxCell();
            DataGridViewColumn col = new DataGridViewColumn();
            col.HeaderText = "";
            col.Name = "";
            col.Width = 20;
            col.CellTemplate = cell;
            DV.Columns.Add(col);

            foreach (string st in DV_Column_Prams)
            {
                cell = new DataGridViewTextBoxCell();
                col = new DataGridViewColumn();
                col.HeaderText = st._firstPropName();
                col.Name = col.HeaderText;
                col.Width = (int)st._firstProp().ToNumber(100);
                col.CellTemplate = cell;
                DV.Columns.Add(col);
            }

            DV.UpdateEvents();
        }
       
        public static pPos ReadSize(this string[] prams)
        {
            double x = 0, y = 0, z = 0;
            pPos res = null;
            string st = prams._props("SizeX");
            if (!st.empty())
                x = st.ToNumber(double.PositiveInfinity);

            st = prams._props("SizeY");
            if (!st.empty())
                y = st.ToNumber(double.PositiveInfinity);

            st = prams._props("SizeZ");
            if (!st.empty())
                z = st.ToNumber(double.PositiveInfinity);

            if (!double.IsInfinity(x) && !double.IsInfinity(y) && !double.IsInfinity(z))
                res = new pPos(x, y, z);
            return res.IsNull ? null : res;
        }

        public static void AddDVEventClick(this DataGridView DV, Action<object, EventArgs> DV_event)
        {
            DV.CellClick += new DataGridViewCellEventHandler(DV_event);
            DV.CellValueChanged += new DataGridViewCellEventHandler(DV_event);
        }

        public static void UpdateEvents(this DataGridView DV)
        {
            DV.DataError += (sender, e) => { };

            DV.CellEndEdit += (sender, e) =>
            {
                if (sender is ComboBox)
                {
                    ComboBox subcb = (ComboBox)sender;
                    subcb.SelectedIndexChanged += (sender1, e1) =>
                    {
                        DV.CurrentCell.Value = subcb.SelectedItem.ToString();
                    };
                }
                else if (sender is TextBox)
                {
                    TextBox subtxt = (TextBox)sender;
                    //subtxt.DeselectAll();
                    subtxt.Enter += (sender1, e1) =>
                    {
                        DV.CurrentCell.Value = subtxt.Text;
                    };
                }
                else if (sender is CheckBox)
                {
                    CheckBox ck = (CheckBox)sender;
                    //Console.WriteLine("Checbox5 = {0}", ck.Checked);
                    ck.MouseClick += (sender1, e1) =>
                    {
                        //Console.WriteLine("Checked = {0}", ck.Checked);
                        DV.CurrentCell.Value = ck.Checked;
                    };
                }
                //Console.WriteLine("Current = {0}", DV.CurrentCell.Value);
            };

            DV.EditingControlShowing += (sender, e) =>
            {
                if (e.Control is ComboBox)
                {
                    ComboBox subcb = (ComboBox)e.Control;
                    subcb.SelectedIndexChanged += (sender1, e1) =>
                                {
                                    DV.CurrentCell.Value = subcb.SelectedItem.ToString();
                                };
                }
                else if (e.Control is TextBox)
                {
                    TextBox subtxt = (TextBox)e.Control;
                    //subtxt.DeselectAll();
                    subtxt.Enter += (sender1, e1) =>
                                        {
                                            DV.CurrentCell.Value = subtxt.Text;
                                        };
                }
                else if (e.Control is CheckBox)
                {
                    CheckBox ck = (CheckBox)e.Control;
                    //Console.WriteLine("Checbox5 = {0}", ck.Checked);
                    ck.MouseClick += (sender1, e1) =>
                    {
                        //Console.WriteLine("Checked = {0}", ck.Checked);
                        DV.CurrentCell.Value = ck.Checked;
                    };
                }
                Console.WriteLine("Current = {0}", DV.CurrentCell.Value);
            };

            DV.CellContentClick += (sender, e) =>
            {
                //Console.WriteLine("Current = {0}", DV.CurrentCell.Value);
                DV.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
        }

        public static string DataViewValues(this DataGridView DV, string propname)
        {
            string[] prams = DV.DataViewValues();
            return prams._props(propname);
        }

        public static string DataViewValues(this DataGridView DV, int index, bool include_all_options = false)
        {
            string res = null;
            if (DV.RowCount > 1 && index <= DV.RowCount)
            { 
                object style = DV.Rows[index].Cells["Name"].Value;

                if (style != null)
                {
                    string tag = DV.Rows[index].Tag != null ? DV.Rows[index].Tag.ToString() : null;
                    string val = "";

                    var cell = DV.Rows[index].Cells["Value"];

                    if (cell != null)
                    {
                        if (cell is DataGridViewCheckBoxCell)
                        {
                            DataGridViewCheckBoxCell bcell = (DataGridViewCheckBoxCell)cell;
                            val = (bool)bcell.Value ? "Yes" : "No";
                        }
                        else
                            val = cell.Value == null ? null: cell.Value.ToString();

                        if (include_all_options)
                        {
                            if (cell is DataGridViewComboBoxCell)
                            {
                                DataGridViewComboBoxCell cb = (DataGridViewComboBoxCell)cell;

                                string st = "<";
                                for (int i = 0; i < cb.Items.Count; i++)
                                {
                                    if (cb.Items[i].ToString() == val)
                                        st += "!";
                                    st += cb.Items[i] + (i < cb.Items.Count - 1 ? "," : "");
                                }
                                val = st + ">";
                            }else if (cell is TrackBarCell)
                            {
                                TrackBarCell trackcell = (TrackBarCell)cell;

                                string st = "{";
                                for (int i = 0; i < trackcell.ValueList.Length; i++)
                                {
                                    if (trackcell.ValueList[i].ToString() == val)
                                        st += "!";
                                    st += trackcell.ValueList[i] + (i < trackcell.ValueList.Length - 1 ? "," : "");
                                }
                                val = st + "}";
                            }else if(cell is DataGridViewCheckBoxCell)
                            {
                                DataGridViewCheckBoxCell bcell = (DataGridViewCheckBoxCell)cell;
                                val = (bool)bcell.Value ? "<!Yes,No>" : "<Yes,!No>";
                            }
                        }
                    }

                    res = String.Format("{0}={1}", style, val);
                }
            }
            return res;
        }

        public static string[] DataViewValues(this DataGridView DV, bool include_all_options = false)
        {
            List<string> res = new List<string>();

            DV.Update();
            for(int i = 0; i < DV.ColumnCount; i++)
                for (int j = 0; j < DV.RowCount; j++)
                    DV.UpdateCellValue(i, j);
            DV.Refresh();
            DV.RefreshEdit();

            if (DV.RowCount > 1)
                for (int i = 0; i < DV.RowCount; i++)
                {
                    string st = DV.DataViewValues(i, include_all_options);
                    if (!st.empty())
                        res.Add(st);
                }
            return res.ToArray();
        }

        public static void SaveParamData(this DataGridView DV, string fname)
        {
            string[] str = DV.DataViewValues(true);
            File.WriteAllLines(fname, str, Encoding.UTF8);
        }
        
        public static Image img;
        public static Form form;

        public static void ViewPreview(this DataGridView DV,int row)
        {
            //if (ExtentDatas != null && ExtentDatas.Count > row)
            //{
                //string st = ExtentDatas[row];
                //string fname = st._prop("Preview");
            string fname = @"D:\a.jpg";
            if (File.Exists(fname))
            {
                img = Bitmap.FromFile(fname);

                using (form = new Form())
                {
                    form.Text = "About Us";

                    // form.Controls.Add(...);
                    PictureBox pic = new PictureBox();
                    
                    //form.Controls.Add(pic);
                    pic.Location = new System.Drawing.Point(0, 0);
                    pic.Width = form.Width;
                    pic.Height = form.Height;
                    pic.Visible = true;

                    //form.Controls.Add(new Label() { Text = "Version 5.0" });
                    form.ShowDialog();
                    //form.Paint += new PaintEventHandler(DV_init);
                    
                }
            }
        }

        static void DV_init(object sender, EventArgs arg)
        {
            form.BackgroundImage = img;
        }

        public static void ViewParamData(this DataGridView DV, IEnumerable<string> paramData, bool clear = true)
        {
            if (clear)
                DV.Rows.Clear();

            for (int i = 0; i < paramData.Count(); i++)
                if (paramData.ElementAt(i).Contains("="))
                {
                    string first = paramData.ElementAt(i).filter("=").First().Trim();
                    string last = paramData.ElementAt(i).Substring(paramData.ElementAt(i).IndexOf("=") + 1).Trim();

                        if (first.IsDVTitle())
                            DV.Rows.Add(_dataTitleRow(first, last));
                        else if (last.StartsWith("<") && last.EndsWith(">"))
                        {
                            string st = last.Replace("!", "").Upper();
                            st = st.Replace(";", "");
                            st = st.Replace(",", "");

                            if (st == "<YESNO>" || st == "<ONOFF>")
                                DV.Rows.Add(_dataCheckboxRow(first, last));
                            else
                                DV.Rows.Add(_dataComboRow(first, last));
                        }
                        else if (last.StartsWith("{") && last.EndsWith("}"))
                            DV.Rows.Add(_dataTrackBarRow(first, last));
                        else
                            DV.Rows.Add(_dataTextRow(first, last));
                }
        }

        public static void ViewParamMultiData(this DataGridView DV, IEnumerable<string> paramData, bool clear = true)
        {
            if (clear)
                DV.Rows.Clear();

            for (int i = 0; i < paramData.Count(); i++)
                if (paramData.ElementAt(i).Contains("="))
                {
                    DataGridViewRow row = new DataGridViewRow();
                    string[] propnames = paramData.ElementAt(i)._allPropNames();

                    foreach (string propname in propnames)
                        row.Cells.Add(new DataGridViewTextBoxCell());

                    foreach (string propname in propnames)
                    {
                        string val = paramData.ElementAt(i)._prop(propname);
                        int column = DV.Columns[propname].Index;

                        if (val.StartsWith("<") && val.EndsWith(">"))
                        {
                            string st = val.Replace("!", "").Upper();
                            st = st.Replace(";", "");
                            st = st.Replace(",", "");

                            //if (st == "<YESNO>" || st == "<ONOFF>")
                            //    DV.Rows.Add(_dataCheckboxRow(propname, last));
                            //else
                            _dataComboRow(row, column, val);
                        }
                        else
                            row = _dataTextRow(row, column, val);
                        DV.Rows.Add(row);
                    }
                }
        }

        public static void UnHighligtAll(this DataGridView DV)
        {
            for (int i = 0; i < DV.Rows.Count; i ++)
            {
                DataGridViewRow row = DV.Rows[i];
                if(row.DefaultCellStyle.BackColor != Color.White)
                    row.DefaultCellStyle.BackColor = Color.White;

                DV.SetRowChecked(i, false);
            }
        }

        public static void SetRowHighligt(this DataGridView DV, int row_index)
        {
            DV.UnHighligtAll();
            DataGridViewRow row = DV.Rows[row_index];
            if(row.DefaultCellStyle.BackColor != Color.Cyan)
                row.DefaultCellStyle.BackColor = Color.Cyan;

            DV.SetRowChecked(row_index, true);
        }

        public static void SetRowChecked(this DataGridView DV, int row_index, bool val)
        {
            DataGridViewCheckBoxCell cell = (DataGridViewCheckBoxCell)DV.Rows[row_index].Cells[0];
            cell.Value = val;
        }

        public static int GetRowKey(this DataGridView DV, string key)
        {
            int res = -1;
            for (int i = 0; i < DV.Rows.Count; i++)
            {
                DataGridViewCell cell = DV.Rows[i].Cells[1];
                string value = cell.Value.ToString();
                if (value.Upper() == key.Upper())
                    res = i;
            }
            return res;
        }

        public static bool GetRowChecked(this DataGridView DV, int row_index)
        {
            DataGridViewCheckBoxCell cell = (DataGridViewCheckBoxCell)DV.Rows[row_index].Cells[0];
            return Convert.ToBoolean(cell.Value);
        }

        public static bool inLayoutSection(this DataGridView DV, int rowindex)
        {
            string[] str = DV.DataViewValues();
            int index = DV.LayoutSection();
            return index != -1 && rowindex > index;
        }

        public static int LayoutSection(this DataGridView DV)
        {
            string[] str = DV.DataViewValues();
            int index = Array.FindIndex(str, st => st.Upper() == "TITLE=LAYOUT");
            return index;
        }

        static DataGridViewRow _dataTextRow(DataGridViewRow row, int column_index, string val)
        {
            //bool read_only = false;

            //if (name.StartsWith("!"))
            //{
            //    read_only = true;
            //    name = name.Substring(1);
            //}

            row.Cells[column_index] = new DataGridViewTextBoxCell();
            DataGridViewTextBoxCell cell = (DataGridViewTextBoxCell)row.Cells[column_index];
            cell.Value = val;
            
            return row;
        }

        static DataGridViewRow _dataTextRow(string name, string val)
        {
            bool read_only = false;

            if (name.StartsWith("!"))
            {
                read_only = true;
                name = name.Substring(1);
            }

            DataGridViewTextBoxCell cell1 = new DataGridViewTextBoxCell();
            cell1.Value = name.filter("<>").First();

            DataGridViewTextBoxCell cell2 = new DataGridViewTextBoxCell();
            cell2.Value = val;
            
            DataGridViewRow row = new DataGridViewRow();
            row.addCheckbox(name.StartsWith("<") && name.EndsWith(">"));

            row.Cells.Add(cell1);
            row.Cells.Add(cell2);

            cell1.ReadOnly = true;
            cell2.ReadOnly = read_only;
            return row;
        }

        static DataGridViewRow _dataTitleRow(string key, string val)
        {
            DataGridViewCellStyle style = new DataGridViewCellStyle();
            style.Font = new Font("Arial", 8, FontStyle.Bold);
            style.BackColor = Color.LightGray;

            DataGridViewRow row = new DataGridViewRow();
            row.addCheckbox(false);

            DataGridViewTextBoxCell cell1 = new DataGridViewTextBoxCell();
            cell1.Value = key;
            cell1.Style = style;
            row.Cells.Add(cell1);
            cell1.ReadOnly = true;

            DataGridViewTextBoxCell cell2 = new DataGridViewTextBoxCell();
            cell2.Value = val;
            cell2.Style = style;
            row.Cells.Add(cell2);
            cell2.ReadOnly = true;

            return row;
        }

        static DataGridViewRow _dataCheckboxRow(string name, string val)
        {
            DataGridViewCheckBoxCell cell2 = new DataGridViewCheckBoxCell();
            string[] val_ar = val.filter("<>;,");

            int index = Array.FindIndex(val_ar, s => s.StartsWith("!"));
            if (index == -1)
                index = 0;
            else
                val_ar[index] = val_ar[index].Substring(1);
            
            cell2.Value = val_ar[index].Upper() == "YES" || val_ar[index].Upper() == "ON";

            DataGridViewRow row = new DataGridViewRow();
            row.addCheckbox(name.StartsWith("<") && name.EndsWith(">"));

            DataGridViewTextBoxCell cell1 = new DataGridViewTextBoxCell();
            cell1.Value = name.filter("<>").First();

            row.Cells.Add(cell1);
            row.Cells.Add(cell2);
            cell1.ReadOnly = true;

            return row;
        }


        static DataGridViewRow _dataComboRow(string name, string val)
        {
            DataGridViewComboBoxCell cell2 = new DataGridViewComboBoxCell();
            string[] val_ar = val.filter("<>;,");

            int index = Array.FindIndex(val_ar, s => s.StartsWith("!"));
            if (index == -1)
            {
                index = 0;
            }
            else
            {
                val_ar[index] = val_ar[index].Substring(1);
            }

            cell2.Items.AddRange(val_ar);
            cell2.Value = val_ar[index];
                        
            DataGridViewRow row = new DataGridViewRow();
            row.addCheckbox(name.StartsWith("<") && name.EndsWith(">"));

            DataGridViewTextBoxCell cell1 = new DataGridViewTextBoxCell();
            cell1.Value = name.filter("<>").First();

            row.Cells.Add(cell1);
            row.Cells.Add(cell2);
            cell1.ReadOnly = true;

            return row;
        }

        static DataGridViewRow _dataComboRow(DataGridViewRow row, int column_index, string val)
        {
            row.Cells[column_index] = new DataGridViewComboBoxCell();
            DataGridViewComboBoxCell cell = (DataGridViewComboBoxCell)row.Cells[column_index];

            string[] val_ar = val.filter("<>;,");

            int index = Array.FindIndex(val_ar, s => s.StartsWith("!"));
            if (index == -1)
            {
                index = 0;
            }
            else
            {
                val_ar[index] = val_ar[index].Substring(1);
            }

            cell.Items.AddRange(val_ar);
            cell.Value = val_ar[index];
            
            return row;
        }

        static DataGridViewRow _dataTrackBarRow(string name, string val)
        {
            TrackBarCell cell2 = new TrackBarCell();
            string[] val_ar = val.filter("{};,");

            int index = Array.FindIndex(val_ar, s => s.StartsWith("!"));
            if (index == -1)
            {
                index = 0;
            }
            else
            {
                val_ar[index] = val_ar[index].Substring(1);
            }

            cell2.ValueList = val_ar;
            cell2.Value = val_ar[index];

            DataGridViewRow row = new DataGridViewRow();
            row.addCheckbox(name.StartsWith("{") && name.EndsWith("}"));

            DataGridViewTextBoxCell cell1 = new DataGridViewTextBoxCell();
            cell1.Value = name.filter("{}").First();

            row.Cells.Add(cell1);
            row.Cells.Add(cell2);
            cell1.ReadOnly = true;

            return row;
        }

        public static DataGridViewRow addCheckbox(this DataGridViewRow row, bool has_check_box = true)
        {
            if (has_check_box)
            {
                DataGridViewCheckBoxCell cell = new DataGridViewCheckBoxCell();
                row.Cells.Add(cell);
                cell.Value = true;
            }
            else
            {
                DataGridViewCheckBoxCell cell = new DataGridViewCheckBoxCell();
                row.Cells.Add(cell);
                //cell.ReadOnly = true;
                cell.Value = false;
                cell.ThreeState = true;
            }

            return row;
        }

        public static void SetCheckBoxes(this DataGridView DV, bool check_box)
        {
            foreach (DataGridViewRow row in DV.Rows)
            {
                DataGridViewCell cell = row.Cells[0];
                if(cell is DataGridViewCheckBoxCell)
                {
                    DataGridViewCheckBoxCell c_cell = (DataGridViewCheckBoxCell)cell;
                    c_cell.Value = true;
                }
            }
        }

        public static string[] LoadValuesFromFile(this IEnumerable<string> prams, string path)
        {
            if (File.Exists(path))
            {
                string[] data = File.ReadAllLines(path);

                List<string> keys = prams.Where(s => s.Contains("=")).Select(s => s.filter("=").First().ToUpper()).ToList();

                foreach(string k in keys)
                {
                    string val = data._props(k);
                    if (!val.empty())
                        prams._setprops(k, val);
                }
            }

            return prams.ToArray();
        }
        
        static int current_shorten_index = 0;
        static string current_shorten_key = null;

        public static string PropFromPram(this string pram, string prop)
        {
            pPos[] res = null;
            string key = pram.filter("=").First();
            string val = pram.filter("=").Last();
            val = val.Replace("(", "|");
            val = val.Replace(")", "=");

            return val._prop(prop.ToUpper());

            //if (!sPos.empty())
            //{
            //    res = ACD.FromString(sPos);
            //    //res[i] = key.shortenKeyIndex();
            //}

            //return res;
        }

        public static string[] GetPramsIndex(this string[] prams)
        {
            string[] res = new string[prams.Length];
            string current = null, current_title = null;
            current_shorten_key = null;

            for (int i = 0; i < prams.Length; i++)
            {
                string key = prams[i].filter("=").First();
                string val = prams[i].filter("=").Last();

                if (key.IsDVTitle())
                {
                    current_title = val;
                }
                else if (!key.StartsWith("[Ref"))
                {
                    val = val.Replace("(", "|");
                    val = val.Replace(")", "=");

                    res[i] = key.Length >= 3 ? key.shortenKeyIndex() : key;
                }
                current = prams[i];
            }
            return res;
        }

        private static string shortenKeyIndex(this string st)
        {
            string res = null;
            string key = st.Substring(0, 2);

            if (current_shorten_key == null || current_shorten_key.ToUpper() != key.ToUpper())
                current_shorten_index = 1;
            else
                current_shorten_index++;

            res = st.Substring(0, 2) + ((current_shorten_index < 10 ? "0" : "")
                + current_shorten_index.ToString());

            current_shorten_key = key;
            return res;
        }

        public static string[] PramsToSchedule(this string[] prams)
        {
            List<string> res = new List<string>();
            int a = 0, b = 0;
            string current = null, current_title = null;
            current_shorten_key = null;

            string[] indexes = prams.GetPramsIndex();

            for (int i = 0; i < prams.Length; i++)
            {
                string key = prams[i].filter("=").First();
                string val = prams[i].filter("=").Last();

                if (key.IsDVTitle())
                {
                    current_title = val;
                    a++;
                    b = 0;
                    res.Add("_NO" + a.ToString() + "|Index="
                            + indexes[i] + "|Key=" + key + "|Name=<" + val + ">");
                }
                else if (!key.StartsWith("["))
                {
                    b++;
                    if (val.Contains("(") && val.Contains(")"))
                    {
                        val = val.Replace("(", "|");
                        val = val.Replace(")", "=");
                        
                        val = val._setprop("POS", null);

                        val = val.Replace("AEC_", "");
                        val = val.Replace("Width", "Width");
                        val = val.Replace("Height", "Height");
                        val = val.Replace("Obj", "Obj");

                        res.Add("_NO" + a.ToString() + "." + b.ToString() + "|Index=" 
                            + indexes[i] + "|Key=" + key + "|" + val );
                    }
                    else
                        res.Add("_NO" + a.ToString() + "." + b.ToString() + "|Key=" + key + "|Quatity = " + val);
                }else
                {
                    res.Add("Key=" + key + "|Name=" + val);
                }
                current = prams[i];
            }

            return res.ToArray();
        }
    }

    public class CalendarColumn : DataGridViewColumn
    {
        public CalendarColumn() : base(new CalendarCell())
        {
        }

        public override DataGridViewCell CellTemplate
        {
            get
            {
                return base.CellTemplate;
            }
            set
            {
                // Ensure that the cell used for the template is a CalendarCell.
                if (value != null &&
                    !value.GetType().IsAssignableFrom(typeof(CalendarCell)))
                {
                    throw new InvalidCastException("Must be a CalendarCell");
                }
                base.CellTemplate = value;
            }
        }
    }

    public class CalendarCell : DataGridViewTextBoxCell
    {

        public CalendarCell()
            : base()
        {
            // Use the short date format.
            this.Style.Format = "d";
        }

        public override void InitializeEditingControl(int rowIndex, object
            initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
        {
            // Set the value of the editing control to the current cell value.
            base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);
            CalendarEditingControl ctl = DataGridView.EditingControl as CalendarEditingControl;
            // Use the default row value when Value property is null.
            if (this.Value == null)
            {
                ctl.Value = (DateTime)this.DefaultNewRowValue;
            }
            else
            {
                ctl.Value = (DateTime)this.Value;
            }
        }

        public override Type EditType
        {
            get
            {
                // Return the type of the editing control that CalendarCell uses.
                return typeof(CalendarEditingControl);
            }
        }

        public override Type ValueType
        {
            get
            {
                // Return the type of the value that CalendarCell contains.

                return typeof(DateTime);
            }
        }

        public override object DefaultNewRowValue
        {
            get
            {
                // Use the current date and time as the default value.
                return DateTime.Now;
            }
        }
    }

    class CalendarEditingControl : DateTimePicker, IDataGridViewEditingControl
    {
        DataGridView dataGridView;
        private bool valueChanged = false;
        int rowIndex;

        public CalendarEditingControl()
        {
            this.Format = DateTimePickerFormat.Short;
        }

        // Implements the IDataGridViewEditingControl.EditingControlFormattedValue 
        // property.
        public object EditingControlFormattedValue
        {
            get
            {
                return this.Value.ToShortDateString();
            }
            set
            {
                if (value is String)
                {
                    try
                    {
                        // This will throw an exception of the string is 
                        // null, empty, or not in the format of a date.
                        this.Value = DateTime.Parse((String)value);
                    }
                    catch
                    {
                        // In the case of an exception, just use the 
                        // default value so we're not left with a null
                        // value.
                        this.Value = DateTime.Now;
                    }
                }
            }
        }

        // Implements the 
        // IDataGridViewEditingControl.GetEditingControlFormattedValue method.
        public object GetEditingControlFormattedValue(
            DataGridViewDataErrorContexts context)
        {
            return EditingControlFormattedValue;
        }

        // Implements the 
        // IDataGridViewEditingControl.ApplyCellStyleToEditingControl method.
        public void ApplyCellStyleToEditingControl(
            DataGridViewCellStyle dataGridViewCellStyle)
        {
            this.Font = dataGridViewCellStyle.Font;
            this.CalendarForeColor = dataGridViewCellStyle.ForeColor;
            this.CalendarMonthBackground = dataGridViewCellStyle.BackColor;
        }

        // Implements the IDataGridViewEditingControl.EditingControlRowIndex 
        // property.
        public int EditingControlRowIndex
        {
            get
            {
                return rowIndex;
            }
            set
            {
                rowIndex = value;
            }
        }

        // Implements the IDataGridViewEditingControl.EditingControlWantsInputKey 
        // method.
        public bool EditingControlWantsInputKey(Keys key, bool dataGridViewWantsInputKey)
        {
            // Let the DateTimePicker handle the keys listed.
            switch (key & Keys.KeyCode)
            {
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                case Keys.Right:
                case Keys.Home:
                case Keys.End:
                case Keys.PageDown:
                case Keys.PageUp:
                    return true;
                default:
                    return !dataGridViewWantsInputKey;
            }
        }

        // Implements the IDataGridViewEditingControl.PrepareEditingControlForEdit 
        // method.
        public void PrepareEditingControlForEdit(bool selectAll)
        {
            // No preparation needs to be done.
        }

        // Implements the IDataGridViewEditingControl
        // .RepositionEditingControlOnValueChange property.
        public bool RepositionEditingControlOnValueChange
        {
            get
            {
                return false;
            }
        }

        // Implements the IDataGridViewEditingControl
        // .EditingControlDataGridView property.
        public DataGridView EditingControlDataGridView
        {
            get
            {
                return dataGridView;
            }
            set
            {
                dataGridView = value;
            }
        }

        // Implements the IDataGridViewEditingControl
        // .EditingControlValueChanged property.
        public bool EditingControlValueChanged
        {
            get
            {
                return valueChanged;
            }
            set
            {
                valueChanged = value;
            }
        }

        // Implements the IDataGridViewEditingControl
        // .EditingPanelCursor property.
        public Cursor EditingPanelCursor
        {
            get
            {
                return base.Cursor;
            }
        }

        protected override void OnValueChanged(EventArgs eventargs)
        {
            // Notify the DataGridView that the contents of the cell have changed.
            valueChanged = true;
            this.EditingControlDataGridView.NotifyCurrentCellDirty(true);
            base.OnValueChanged(eventargs);
        }
    }

    //public class ComboBoxCell : DataGridViewComboBoxCell
    //{
    //    public string[] ValueList;
    //    public ToolTip ToolTip;

    //    public ComboBoxCell() : base()
    //    {

    //    }
        
    //    public override Type EditType
    //    {
    //        get
    //        {
    //            // Return the type of the editing control that CalendarCell uses.
    //            return typeof(ComboBoxEditingControl);
    //        }
    //    }
    //}


    //class ComboBoxEditingControl : ComboBoxCell, IDataGridViewEditingControl
    //{
    //    DataGridView dataGridView;
    //    private bool valueChanged = false;
    //    int rowIndex;
    //    ToolTip ttp;

    //    public ComboBoxEditingControl()
    //    {
    //        //this.Format = DateTimePickerFormat.Short;
    //        //ttp = new ToolTip();
    //    }


    //    // Implements the IDataGridViewEditingControl.EditingControlFormattedValue 
    //    // property.
    //    public object EditingControlFormattedValue
    //    {
    //        get
    //        {
    //            return (int)this.Value;
    //        }
    //        set
    //        {
    //            //Console.WriteLine("Value {0}", this.Value);
    //            this.Value = (int)value;
    //        }
    //    }

    //    // Implements the 
    //    // IDataGridViewEditingControl.GetEditingControlFormattedValue method.
    //    public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context)
    //    {
    //        return EditingControlFormattedValue;
    //    }

    //    // Implements the 
    //    // IDataGridViewEditingControl.ApplyCellStyleToEditingControl method.
    //    public void ApplyCellStyleToEditingControl(
    //        DataGridViewCellStyle dataGridViewCellStyle)
    //    {
    //        //this.Font = dataGridViewCellStyle.Font;
    //        //this.CalendarForeColor = dataGridViewCellStyle.ForeColor;
    //        //this.CalendarMonthBackground = dataGridViewCellStyle.BackColor;
    //    }

    //    // Implements the IDataGridViewEditingControl.EditingControlRowIndex 
    //    // property.
    //    public int EditingControlRowIndex
    //    {
    //        get
    //        {
    //            return rowIndex;
    //        }
    //        set
    //        {
    //            rowIndex = value;
    //        }
    //    }

    //    // Implements the IDataGridViewEditingControl.EditingControlWantsInputKey 
    //    // method.
    //    public bool EditingControlWantsInputKey(Keys key, bool dataGridViewWantsInputKey)
    //    {
    //        // Let the DateTimePicker handle the keys listed.
    //        switch (key & Keys.KeyCode)
    //        {
    //            case Keys.Left:
    //            case Keys.Up:
    //            case Keys.Down:
    //            case Keys.Right:
    //            case Keys.Home:
    //            case Keys.End:
    //            case Keys.PageDown:
    //            case Keys.PageUp:
    //                return true;
    //            default:
    //                return !dataGridViewWantsInputKey;
    //        }
    //    }

    //    // Implements the IDataGridViewEditingControl.PrepareEditingControlForEdit 
    //    // method.
    //    public void PrepareEditingControlForEdit(bool selectAll)
    //    {
    //        // No preparation needs to be done.
    //    }

    //    // Implements the IDataGridViewEditingControl
    //    // .RepositionEditingControlOnValueChange property.
    //    public bool RepositionEditingControlOnValueChange
    //    {
    //        get
    //        {
    //            return false;
    //        }
    //    }

    //    // Implements the IDataGridViewEditingControl
    //    // .EditingControlDataGridView property.
    //    public DataGridView EditingControlDataGridView
    //    {
    //        get
    //        {
    //            return dataGridView;
    //        }
    //        set
    //        {
    //            dataGridView = value;
    //        }
    //    }

    //    // Implements the IDataGridViewEditingControl
    //    // .EditingControlValueChanged property.
    //    public bool EditingControlValueChanged
    //    {
    //        get
    //        {
    //            return valueChanged;
    //        }
    //        set
    //        {
    //            valueChanged = value;
    //        }
    //    }

    //    // Implements the IDataGridViewEditingControl
    //    // .EditingPanelCursor property.
    //    public Cursor EditingPanelCursor
    //    {
    //        get
    //        {
    //            return null;
    //        }
    //    }

    //    protected override void OnClick(DataGridViewCellEventArgs e)
    //    {
    //        valueChanged = true;

    //        ComboBoxCell cell = (ComboBoxCell)this.dataGridView.CurrentCell;

    //        if (cell != null)
    //        {
    //            this.dataGridView.CurrentCell.Value = cell..ToString();
    //            ttp.SetToolTip(this, cell.Value.ToString());
    //            this.EditingControlDataGridView.NotifyCurrentCellDirty(true);
    //            base.OnValueChanged(eventargs);
    //        }
    //    }

    //    protected override void OnMouseUp(MouseEventArgs eventargs)
    //    {
    //        //Console.WriteLine("Final value {0}", this.Value);
    //        //valueChanged = true;
    //        this.EditingControlDataGridView.NotifyCurrentCellDirty(false);
    //        base.OnMouseUp(eventargs);
    //    }
    //}

    public class TrackBarCell : DataGridViewTextBoxCell
    {
        public string[] ValueList;
        public ToolTip ToolTip;

        public TrackBarCell(): base()
        {
            
        }

        public override void InitializeEditingControl(int rowIndex, object
            initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
        {
            // Set the value of the editing control to the current cell value.
            base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);
            TrackBar ctl = DataGridView.EditingControl as TrackBar;
            ctl.Minimum = 0;
            ctl.Maximum = ValueList.Length - 1;
            ctl.TickStyle = TickStyle.BottomRight;
            // Use the default row value when Value property is null.
            if (this.Value == null)
            {
                ctl.Value = 0;
            }
            else
            {
                int index = Array.FindIndex( ValueList, s => this.Value.ToString().Upper() == s.Upper());
                ctl.Value = index == -1 ? 0 : index;
            }
        }

        public override Type EditType
        {
            get
            {
                // Return the type of the editing control that CalendarCell uses.
                return typeof(TrackBarEditingControl);
            }
        }

        public override Type ValueType
        {
            get
            {
                // Return the type of the value that CalendarCell contains.

                return typeof(int);
            }
        }

        public override object DefaultNewRowValue
        {
            get
            {
                // Use the current date and time as the default value.
                return 0;
            }
        }
    }

    class TrackBarEditingControl : TrackBar, IDataGridViewEditingControl
    {
        DataGridView dataGridView;
        private bool valueChanged = false;
        int rowIndex;
        ToolTip ttp;

        public TrackBarEditingControl()
        {
            //this.Format = DateTimePickerFormat.Short;
            ttp = new ToolTip();
        }

        // Implements the IDataGridViewEditingControl.EditingControlFormattedValue 
        // property.
        public object EditingControlFormattedValue
        {
            get
            {
                return (int)this.Value;
            }
            set
            {
                //Console.WriteLine("Value {0}", this.Value);
                this.Value = (int)value;
            }
        }

        // Implements the 
        // IDataGridViewEditingControl.GetEditingControlFormattedValue method.
        public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context)
        {
            return EditingControlFormattedValue;
        }

        // Implements the 
        // IDataGridViewEditingControl.ApplyCellStyleToEditingControl method.
        public void ApplyCellStyleToEditingControl(
            DataGridViewCellStyle dataGridViewCellStyle)
        {
            //this.Font = dataGridViewCellStyle.Font;
            //this.CalendarForeColor = dataGridViewCellStyle.ForeColor;
            //this.CalendarMonthBackground = dataGridViewCellStyle.BackColor;
        }

        // Implements the IDataGridViewEditingControl.EditingControlRowIndex 
        // property.
        public int EditingControlRowIndex
        {
            get
            {
                return rowIndex;
            }
            set
            {
                rowIndex = value;
            }
        }

        // Implements the IDataGridViewEditingControl.EditingControlWantsInputKey 
        // method.
        public bool EditingControlWantsInputKey(Keys key, bool dataGridViewWantsInputKey)
        {
            // Let the DateTimePicker handle the keys listed.
            switch (key & Keys.KeyCode)
            {
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                case Keys.Right:
                case Keys.Home:
                case Keys.End:
                case Keys.PageDown:
                case Keys.PageUp:
                    return true;
                default:
                    return !dataGridViewWantsInputKey;
            }
        }

        // Implements the IDataGridViewEditingControl.PrepareEditingControlForEdit 
        // method.
        public void PrepareEditingControlForEdit(bool selectAll)
        {
            // No preparation needs to be done.
        }

        // Implements the IDataGridViewEditingControl
        // .RepositionEditingControlOnValueChange property.
        public bool RepositionEditingControlOnValueChange
        {
            get
            {
                return false;
            }
        }

        // Implements the IDataGridViewEditingControl
        // .EditingControlDataGridView property.
        public DataGridView EditingControlDataGridView
        {
            get
            {
                return dataGridView;
            }
            set
            {
                dataGridView = value;
            }
        }

        // Implements the IDataGridViewEditingControl
        // .EditingControlValueChanged property.
        public bool EditingControlValueChanged
        {
            get
            {
                return valueChanged;
            }
            set
            {
                valueChanged = value;
            }
        }

        // Implements the IDataGridViewEditingControl
        // .EditingPanelCursor property.
        public Cursor EditingPanelCursor
        {
            get
            {
                return base.Cursor;
            }
        }
        
        protected override void OnScroll(EventArgs eventargs)
        {
            //Console.WriteLine("Value {0}", this.Value);
            valueChanged = true;
            
            TrackBarCell cell = (TrackBarCell)this.dataGridView.CurrentCell;

            if (cell != null)
            {
                cell.Value = cell.ValueList[ this.Value];
                ttp.SetToolTip(this, cell.Value.ToString());
                this.EditingControlDataGridView.NotifyCurrentCellDirty(true);
                base.OnValueChanged(eventargs);
            }
        }

        protected override void OnMouseUp(MouseEventArgs eventargs)
        {
            //Console.WriteLine("Final value {0}", this.Value);
            //valueChanged = true;
            this.EditingControlDataGridView.NotifyCurrentCellDirty(false);
            base.OnMouseUp(eventargs);
        }
    }
    

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Rect
    {
        public int Left;

        public int Top;

        public int Right;

        public int Bottom;
    }

    public class NativeMethods
    {
        [ComImport]
        [Guid("0000010D-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IViewObject
        {
            void Draw([MarshalAs(UnmanagedType.U4)] uint dwAspect, int lindex, IntPtr pvAspect, [In] IntPtr ptd, IntPtr hdcTargetDev, IntPtr hdcDraw, [MarshalAs(UnmanagedType.Struct)] ref RECT lprcBounds, [In] IntPtr lprcWBounds, IntPtr pfnContinue, [MarshalAs(UnmanagedType.U4)] uint dwContinue);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public static void GetImage(object obj, Image destination, Color backgroundColor)
        {
            using (Graphics graphics = Graphics.FromImage(destination))
            {
                IntPtr deviceContextHandle = IntPtr.Zero;
                RECT rectangle = new RECT();

                rectangle.Right = destination.Width;
                rectangle.Bottom = destination.Height;

                graphics.Clear(backgroundColor);

                try
                {
                    deviceContextHandle = graphics.GetHdc();

                    IViewObject viewObject = obj as IViewObject;
                    viewObject.Draw(1, -1, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, deviceContextHandle, ref rectangle, IntPtr.Zero, IntPtr.Zero, 0);
                }
                finally
                {
                    if (deviceContextHandle != IntPtr.Zero)
                    {
                        graphics.ReleaseHdc(deviceContextHandle);
                    }
                }
            }
        }
    }

    public static class ControlExtension
    {
        public static Bitmap GetBitmap(this WebBrowser browser, Size size)
        {
            var bitmap = new Bitmap(size.Width, size.Height);

            NativeMethods.GetImage(browser.Document.DomDocument, bitmap, Color.White);
            return bitmap;
        }

        public static void SetColorKeyword(this RichTextBox box, IEnumerable<string> keywords, Color color)
        {
            string st = box.Text.ToUpper();
            int index = 0;
            int sel_index = box.SelectionStart;
            int sel_length = box.SelectionLength;

            foreach (string key in keywords)
            {
                index = 0;
                while (index != -1)
                {
                    index = st.IndexOf(key.ToUpper(), index + 1);

                    if (index != -1)
                    {
                        box.SelectionStart = index;
                        box.SelectionLength = key.Length;

                        box.SelectionColor = color;
                        box.SelectionFont = new Font(box.Font,FontStyle.Bold);
                        //box.AppendText(text);
                        //box.SelectionColor = box.ForeColor;
                    }
                }
            }

            box.SelectionStart = sel_index;
            box.SelectionLength = sel_length;
        }

        [DllImport("user32", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(HandleRef hWnd, int msg, int wParam, ref PARAFORMAT lParam);

        private const int WM_USER = 0x0400;
        private const int EM_SETEVENTMASK = (WM_USER + 69);
        private const int WM_SETREDRAW = 0x0b;
        private static IntPtr OldEventMask;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        
        const int PFM_SPACEBEFORE = 0x00000040;
        const int PFM_SPACEAFTER = 0x00000080;
        const int PFM_LINESPACING = 0x00000100;
        const int SCF_SELECTION = 1;
        const int EM_SETPARAFORMAT = 1095;

        //private const int WM_USER = 0x0400;
        //private const int EM_SETEVENTMASK = (WM_USER + 69);
        //private const int WM_SETREDRAW = 0x0b;
        //private static IntPtr OldEventMask;

        public static void StopRepaint(this Control txtContent)
        {
            SendMessage(txtContent.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            OldEventMask = (IntPtr)SendMessage(txtContent.Handle, EM_SETEVENTMASK, IntPtr.Zero, IntPtr.Zero);
        }

        public static void StartRepaint(this Control txtContent)
        {
            SendMessage(txtContent.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            SendMessage(txtContent.Handle, EM_SETEVENTMASK, IntPtr.Zero, OldEventMask);
        }

        static int selection_start, selection_len, topview_start;

        public static void SaveTextBoxSelection(this RichTextBox txtContent)
        {
            //topview_start = txtContent.
            selection_start = txtContent.SelectionStart;
            selection_len = txtContent.SelectionLength;
        }

        public static void LoadTextBoxSelection(this RichTextBox txtContent)
        {
            txtContent.SelectionStart = selection_start;
            txtContent.SelectionLength = selection_len;
        }

        public static void SetLineFormat(this RichTextBox txtContent, byte rule = 1, int space = 100)
        {
            txtContent.StopRepaint();

            PARAFORMAT fmt = new PARAFORMAT();
            fmt.cbSize = Marshal.SizeOf(fmt);
            fmt.dwMask = PFM_LINESPACING;
            fmt.dyLineSpacing = space;
            fmt.bLineSpacingRule = rule;
            txtContent.SelectAll();
            SendMessage(new HandleRef(txtContent, txtContent.Handle),
                            EM_SETPARAFORMAT, SCF_SELECTION, ref fmt);
            //Console.WriteLine("#OK0");
            string[] editor_keywords = pString.INI_String("EDITOR_KEYWORDS").filter(",");
            
            txtContent.SetColorKeyword(editor_keywords, Color.Blue);
            //Console.WriteLine("#OK1");
            txtContent.SetColorKeyword(new string[] { "|", "=", "#", ";", "#C","#L","#M","#R"}, Color.Red);
            //Console.WriteLine("#OK2");
            //txtContent.SetColorKeyword(new string[] {"+","-","*",":","/" ,@"\","^", ",",
            //    "R0X", "R0Y", "R0Z", "R1X", "R1Y", "R1Z" }, Color.DarkCyan);
            //Console.WriteLine("#OK3");

            txtContent.StartRepaint();
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PARAFORMAT
        {
            public int cbSize;
            public uint dwMask;
            public short wNumbering;
            public short wReserved;
            public int dxStartIndent;
            public int dxRightIndent;
            public int dxOffset;
            public short wAlignment;
            public short cTabCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public int[] rgxTabs;
            // PARAFORMAT2 from here onwards
            public int dySpaceBefore;
            public int dySpaceAfter;
            public int dyLineSpacing;
            public short sStyle;
            public byte bLineSpacingRule;
            public byte bOutlineLevel;
            public short wShadingWeight;
            public short wShadingStyle;
            public short wNumberingStart;
            public short wNumberingStyle;
            public short wNumberingTab;
            public short wBorderSpace;
            public short wBorderWidth;
            public short wBorders;
        }


    }

}
