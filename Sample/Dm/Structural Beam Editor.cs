using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Internal;

//
//using SyncObject;
using System.Runtime.InteropServices;

namespace AcadScript
{
    public class frmStructuralBeam : Form
    {
        DataGridView DV;
        TextBox spnSlabDepth, spnBeamWidth, spnBeamHeight, spnSpacing, spnMinRange;
        RichTextBox txtTemplate, txtGlobal, txtElement;
        Button btnGetInfo, btnDraw, btnSaveList, btnLoadList;
        Label lblSlab, lblBeam, lblSpacing, lblView, lblMinRange;
        ComboBox cbTemplate, cbView;
        //IPictureBox picView;
        HScrollBar hScroll;
        VScrollBar  vScroll;

        [DllImport("user32")]
        private static extern IntPtr GetWindowDC(IntPtr hwnd);

        // you also need ReleaseDC
        [DllImport("user32")]
        private static extern IntPtr ReleaseDC(IntPtr hwnd, IntPtr hdc);
        
        string[] GetDataViewContent()
        {
            List<string> res = new List<string>();
            for (int i = 0; i < DV.RowCount; i++)
            {
                string st = "";

                for (int j = 0; j < DV.ColumnCount; j++)
                    if (!DV.Columns[j].Name.empty() && DV.Rows[i].Cells[j].Value != null)
                        st += DV.Rows[i].ReadOnly ? DV.Rows[i].Cells[j].Value
                            : DV.Columns[j].Name + "=" + DV.Rows[i].Cells[j].Value + "|";

                if (st.EndsWith("|")) st = st.Remove(st.Length - 1);
                if (!st.empty())
                    res.Add(st);
            }
            return res.ToArray();
        }

        void _dataTextRow(DataGridViewRow row, int column_index, string val)
        {
            row.Cells[column_index] = new DataGridViewTextBoxCell();
            DataGridViewTextBoxCell cell = (DataGridViewTextBoxCell)row.Cells[column_index];
            cell.Value = val;
        }
        
        void _dataComboRow(DataGridViewRow row, int column_index, string val)
        {
            //ACD.WR("D0 Column {0}", column_index);
            row.Cells[column_index] = new DataGridViewComboBoxCell();
            //ACD.WR("D1");
            DataGridViewComboBoxCell cell = (DataGridViewComboBoxCell)row.Cells[column_index];

            string[] ar = val.filter("<>;,");
                        
            int index = Array.FindIndex(ar, s => s.StartsWith("!"));
            if (index == -1)
            {
                index = 0;
            }
            else
            {
                ar[index] = ar[index].Substring(1);
            }
            //ACD.WR("D2");
            cell.Items.AddRange(ar);
            cell.Value = ar[index];
        }

        void ViewParamMultiData(IEnumerable<string> paramData, bool clear = true)
        {
            if (clear)
                DV.Rows.Clear();

            DataGridViewCellStyle style = new DataGridViewCellStyle();
            style.Font = new System.Drawing.Font(DV.Font, FontStyle.Bold);

            for (int i = 0; i < paramData.Count(); i++)
            {
                DataGridViewRow row = new DataGridViewRow();
                string[] propnames = paramData.ElementAt(i)._allPropNames();

                for(int j = 0; j < DV.ColumnCount; j++)
                    row.Cells.Add(new DataGridViewTextBoxCell());

                if (paramData.ElementAt(i)._firstProp().StartsWith("#"))
                {
                    row.Cells[1].ReadOnly = true;
                    //_dataTextRow(row, 1, paramData.ElementAt(i)._firstProp());
                    row.DefaultCellStyle = style;
                } 
                //else if (paramData.ElementAt(i).Contains("="))
                //{
                    foreach (string propname in propnames)
                    {
                        //ACD.WR(propname + "_0");
                        string val = paramData.ElementAt(i)._prop(propname);

                        int column = DV.Columns[propname].Index;
                        //ACD.WR(propname + "_1=" + val);

                        if (val.Contains(";"))
                        {
                            //ACD.WR(propname + "_1.1");
                            //string st = val.Replace("!", "").Upper();
                            //st = st.Replace(";", "");
                            //st = st.Replace(",", "");

                            //if (st == "<YESNO>" || st == "<ONOFF>")
                            //    DV.Rows.Add(_dataCheckboxRow(propname, last));
                            //else
                            _dataComboRow(row, column, val);
                            //ACD.WR(propname + "_1.2");
                        }
                        else
                            _dataTextRow(row, column, val);
                        //ACD.WR(propname + "_2");

                        //ACD.WR(propname + "_3");
                    }
                //}

                DV.Rows.Add(row);
            }
        }

        public string[] DimSettings;
        
        public double dim_chains { get { return DimSettings._props("DimFlipValue").ToNumber(250); } }
        public double dim_flip_value = 250;
        public bool dim_overall { get { return DimSettings._props("DimOverall").ToBool(); } }
        public double dim_region_distance { get { return DimSettings._props("region").ToNumber(1000); } }
        public pPos dim_detail_division = new pPos(2, 2);
        public double dim_detail_region_distance { get { return DimSettings._props("detailregion").ToNumber(1000); } }
        public double dim_round { get { return DimSettings._props("round").ToNumber(50); } }
        public double dim_space { get { return DimSettings._props("space").ToNumber(1000); } }
        public double dim_seperate { get { return dim_space; } }
        public double dim_minvalue { get { return DimSettings._props("minvalue").ToNumber(200); } }
        public string dim_chain_detail_methods { get { return DimSettings._props("ChainDetail"); } }
        public bool dim_with_hatch_layer { get { return DimSettings._props("WithHatchLayer").ToBool(); } }

        public frmStructuralBeam()
        {
            gridXys = new List<double[]>();

            this.Width = 500;
            this.Height = 800;
            this.TopMost = true;
            Text = "Structural Beam";
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;

            DV = new DataGridView()
            {
                Left = 10,
                Top = 10,
                Width = this.Width - 35,
                Height = 200
            };

            DV.BuildColumns("No=50", "Distance=100", "Type=220");


            lblSlab = new Label()
            {
                Left = DV.Left,
                Top = DV.Bottom + 10,
                Width = 20,
                Text = "[S]"
            };

            spnSlabDepth = new TextBox()
            {
                Left = lblSlab.Right + 10,
                Top = lblSlab.Top,
                Width = 55,
                Text = "120",
                TextAlign = HorizontalAlignment.Right
            };

            lblBeam = new Label()
            {
                Left = spnSlabDepth.Right + 10,
                Top = lblSlab.Top,
                Width = lblSlab.Width,
                Text = "[B]"
            };

            spnBeamWidth = new TextBox()
            {
                Width = 80,
                Left = lblBeam.Right + 10,
                Top = lblSlab.Top,
                Text = "200",
                TextAlign = spnSlabDepth.TextAlign
            };
               
            spnBeamHeight = new TextBox()
            {
                Left = spnBeamWidth.Right + 10,
                Top = lblSlab.Top,
                Width = spnSlabDepth.Width,
                Text = "400",
                TextAlign = spnSlabDepth.TextAlign
            };
            
            lblSpacing = new Label()
            {
                Left = spnBeamHeight.Right + 10,
                Top = lblSlab.Top,
                Width = lblSlab.Width,
                Text = "[ ]"
            };

            //ACD.WR("OK1");

            spnSpacing = new TextBox()
            {
                Left = lblSpacing.Right + 10,
                Top = lblSlab.Top,
                Width = spnSlabDepth.Width,
                Text = "10000",
                TextAlign = spnSlabDepth.TextAlign
            };

            lblMinRange = new Label()
            {
                Left = spnSpacing.Right + 10,
                Top = lblSlab.Top,
                Width = lblSlab.Width,
                Text = "[m]"
            };

            //ACD.WR("OK2");

            spnMinRange = new TextBox()
            {
                Left = lblMinRange.Right,
                Top = lblSlab.Top,
                Width = spnSpacing.Width,
                Text = "2000",
                TextAlign = spnSlabDepth.TextAlign
            };
            
            cbTemplate = new ComboBox()
            {
                Left = 10,
                Top = lblSlab.Bottom + 10,
                Width = DV.Width,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            //ACD.WR("OK3");

            

            cbTemplate.Items.Clear();
            cbTemplate.Items.Add("[None]");
            cbTemplate.Items.AddRange(File.ReadAllLines(DE.CADLIB_CONSTRUCT + "Beam.txt")
                .GetChapters().Keys.Select(s => s._firstProp()).ToArray());

            if (cbTemplate.Items.Count > 0)
                cbTemplate.SelectedIndex = 0;

            txtGlobal = new RichTextBox()
            {
                Left = 10,
                Top = cbTemplate.Bottom + 10,
                Width = DV.Width,
                Height = 50,
                Multiline = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Text = ""
            };

            txtElement = new RichTextBox()
            {
                Left = 10,
                Top = txtGlobal.Bottom + 10,
                Width = DV.Width,
                Height = 100,
                Multiline = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Text = ""
            };

            txtTemplate = new RichTextBox()
            {
                Left = 10,
                Top = txtElement.Bottom + 10,
                Width = DV.Width,
                Height = this.Height - (txtElement.Bottom + 20 + 80),
                Multiline = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Text = ""
            };
            
            lblView = new Label()
            {
                Left = DV.Left,
                Top = txtTemplate.Top + txtTemplate.Height + 10,
                Width = 30,
                Text = "View"
            };

            cbView = new ComboBox()
            {
                Left = lblView.Right + 10,
                Top = lblView.Top,
                Width = 60,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cbView.Items.Clear();
            cbView.Items.AddRange(new string[] { "0", "1", "2" });
            cbView.SelectedIndex = 1;

            btnGetInfo = new Button()
            {
                Left = cbView.Right + 10,
                Top = lblView.Top,
                Width = 75,
                Height = 35,
                Text = "Get Info"
            };
            //ACD.WR("OK4");
            btnLoadList = new Button()
            {
                Left = btnGetInfo.Right + 10,
                Top = btnGetInfo.Top,
                Width = btnGetInfo.Width,
                Height = btnGetInfo.Height,
                Text = "Load List"
            };

            btnSaveList = new Button()
            {
                Left = btnLoadList.Right + 10,
                Top = btnGetInfo.Top,
                Width = btnGetInfo.Width,
                Height = btnGetInfo.Height,
                Text = "Save List"
            };
            
            btnDraw = new Button()
            {
                Left = btnSaveList.Right + 10,
                Top = btnGetInfo.Top,
                Width = btnGetInfo.Width,
                Height = btnGetInfo.Height,
                Text = "Draw"
            };
            //ACD.WR("OK5");
            //picView = new IPictureBox()
            //{
            //    Left = 10,
            //    Top = btnDraw.Top + btnDraw.Height + 10,
            //    Width = this.Width - 50,
            //    Height = this.Height - btnDraw.Top - btnDraw.Height - 70,
            //    BorderStyle = BorderStyle.FixedSingle

            //};

            //hScroll = new HScrollBar()
            //{
            //    Left = picView.Left,
            //    Width = picView.Width,
            //    Top = picView.Top + picView.Height
            //};

            //vScroll = new VScrollBar()
            //{
            //    Left = picView.Left + picView.Width,
            //    Height = picView.Height,
            //    Top = picView.Top
            //};

            //picView.vScroll = vScroll;
            //picView.hScroll = hScroll;

            this.Controls.AddRange(
                new Control[] { DV, spnSlabDepth, spnBeamWidth, spnBeamHeight, spnSpacing, spnMinRange,
                            lblSlab, lblBeam, lblSpacing, lblView, lblMinRange,
                            btnGetInfo, btnDraw, btnLoadList, btnSaveList,
                            cbTemplate, cbView,
                            txtGlobal, txtTemplate, txtElement });
            //,picView
            //,hScroll
            //,vScroll


            //txtTemplate.SaveTextBoxSelection();
            
            cbTemplate.SelectedIndexChanged += (o, e) =>
            {
                string[] contents = current_selected_template;
                string[] keywords = new string[] { "#Element", "Verts", "Faces" };

                txtGlobal.Text = current_selected_template.Where(s 
                    => s.st_("#GLOBAL")).ToTextStr("\r\n");
                txtElement.Text = current_selected_template.Where(s 
                    => keywords.Any(k => s.st_(k.Upper()))).ToTextStr("\r\n");
                txtTemplate.Text = current_selected_template.Where(s 
                    => !s.st_("#GLOBAL") && keywords.All(k 
                    => !s.st_(k.Upper()))).ToTextStr("\r\n");

                //ImportTextCLS itext = new ImportTextCLS();
                //itext.ReadText(contents);

                //ACD.WR("ITM {0}", itext.ElementList.Count);
                //foreach (Element3D itm in itext.ElementList)
                //{
                //    ACD.WR("ITM_3D {0}", itm.ToString());
                //}
                //_updateEditor();
            };
            
            btnGetInfo.Click += (o, e) =>
            {
                using (ACD.Lock())
                {
                    pPos basept = ACD.GetPoint();
                    if (basept != null)
                    {
                        LoadData(GetInfo());
                        DrawBeamBox(basept);
                    }
                }
                ACD.Focus();
            };

            btnSaveList.Click += (o, e) =>
            {
                bool new_file = false;

                //string[] str = content_chapters;
                List<string> res = new List<string>();
                Dictionary<string, string[]> chapters = content_chapters;

                foreach (string key in chapters.Keys)
                {
                    string[] contents = chapters[key];
                    List<string> propnames = new List<string>();
                    foreach (string s in contents)
                        propnames.AddRange(s._allPropNames());

                    propnames = propnames.Distinct().ToList();

                    string stt = "Chapter=" + key + "|";
                    foreach (string propname in propnames)
                    {
                        string st = propname + "=";
                        foreach (string s in contents)
                            if (!s._prop(propname).empty())
                                st += s._prop(propname) + ";";

                        if (st.EndsWith(";"))
                            st = st.Remove(st.Length - 1);

                        stt += st + "|";
                    }
                    if (stt.EndsWith("|"))
                        stt = stt.Remove(stt.Length - 1);

                    res.Add(stt);
                }

                string filename = struct_file;

                if (File.Exists(filename))
                    if (MessageBox.Show("Save New List", "Start new file?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        new_file = true;

                if (new_file)
                    File.WriteAllLines(filename, res);
                else
                    File.AppendAllLines(filename, res);
            };

            btnLoadList.Click += (o, e) =>
            {
                string filename = struct_file;

                if (File.Exists(filename))
                    LoadData(File.ReadAllLines(filename));
            };

            btnDraw.Click += (o, e) =>
            {
                using (ACD.Lock())
                {
                    ACD.WR("<Draw beam>Select region rectangle by CLOSED polyline, LINE is gridline");
                    ObjectIdCollection selIds = ACD.GetSelection();
                    
                    if(selIds.Count > 0)
                    {
                        ObjectIdCollection regionIds = selIds.ToList().Where(id => ACD.DB._isPolylineClosed(id)).ToCollection();

                        ACD.DB.GetEntities(ACD.bRect, EN_SELECT.AC_DXF, "LWPOLYLINE", "LINE");
                        PosCollection line_pts = ACD.DB._getAllVertices(IR.SelectedIds.ToList().Where(id 
                            => !ACD.DB._isPolylineClosed(id)).ToCollection());

                        _initVariables();
                        
                        foreach (ObjectId regionId in regionIds)
                        {
                            ObjectIdCollection ids = new ObjectIdCollection();

                            pPos[] region = ACD.DB._getVertices(regionId);
                            pPos[] bb = region.Boundary();
                            beam_height = bb.Size().Y;

                            List<pPos> intps = region.ToList();

                            foreach (pPos[] line in line_pts)
                                intps.AddRange(region.Intersect(line[0], line[1], true));

                            List<double[]> xys = intps.ExtractPtsXY(10);

                            for (int i = 0; i < xys[0].Length - 1; i++)
                            {
                                //ACD.DB.DrawPolyline(new pPos(xys[0][i], bb[0].Y).Rect(new pPos(xys[0][i + 1], bb[1].Y)));

                                double d = xys[0][i + 1] - xys[0][i];
                                double m = xys[0][i]- xys[0][0];

                                //if (d > min_range)
                                {
                                    pPos[] r = (new pPos(0, 0).RectToPoint(new pPos(d, bb[1].Y - bb[0].Y)));

                                    ObjectIdCollection subIds = DrawParams(txtTemplate.Text.filter("\r\n"), r,
                                        txtGlobal.Text + "|SUM=" + region.Size().X.roundNumber(10), 
                                        new pPos[] { new pPos(-m, 0), new pPos(bb[1].X - bb[0].X - m, -bb[1].Y + bb[0].Y)}.Boundary());

                                    ACD.DB.MoveObject(subIds, new pPos(m, 0));
                                    ids.AddRange(subIds);
                                }
                            }
                            //.Move(new pPos(0,bb[0].X,bb[1].Y))
                            ACD.DB.MoveObject(ids, new pPos(bb[0].X, bb[1].Y));
                        }
                    }

                    ACD.Focus();
                }
            };
        }

        List<double[]> gridXys;

        void _selectGridIds(ObjectIdCollection ids)
        {
            gridXys = new List<double[]>();
            if (ids != null)
            {
                ObjectIdCollection gridIds = ids.ToList().Where(id
                    => ACD.DB._isBlock(id) && !ACD.DB._isArray(id)
                    && ACD.DB._getIdName(id).StartsWith("grid")).ToCollection();

                ACD.WR("GRID_COUNT {0}", gridIds.Count);

                if (gridIds.Count == 0)
                {
                    pPos[] zone = ACD.DB.GetDrawingZone(ACD.DB._getBound(ids).CenterPoint());
                    if (zone != null)
                    {
                        ACD.DB.GetEntities(zone, EN_SELECT.AC_DXF, "INSERT");
                        gridIds = IR.SelectedIds.ToList().Where(id
                            => ACD.DB._getIdName(id).StartsWith("grid")).ToCollection();
                    }
                }

                if (gridIds.Count > 0)
                {
                    PosCollection gridpls = ACD.DB.GetIdPts(gridIds.First());

                    gridXys = DE.NumericArray(0, gridpls.Count - 1)
                        .Where(n => !gridpls.Closed[n] && gridpls[n].Length(false) > 1000)
                        .Select(n => gridpls[n]).ToCollectionSameClosed().SelfIntersect.ExtractPtsXY(50, 0);
                }
            }

            if(gridXys.Count == 0)
                ACD.WR("No grid object");
        }

        string[] GetInfo()
        {
            string[] res = null;
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();
                _selectGridIds(selIds);

                if (gridXys.Count > 0)
                {
                    //ACD.DB.DrawPolyline(new pPos[] { new pPos(xys[0].Min(), xys[1].Min()), new pPos(xys[0].Max(), xys[1].Max()) }, false);

                    List<BeamInfoCLS> beamlist = new List<BeamInfoCLS>();
                    foreach (ObjectId id in selIds)
                        if (ACD.DB._isWall(id))
                        {
                            BeamInfoCLS beam = new BeamInfoCLS(id, gridXys);
                            beamlist.Add(beam);
                        }

                    res = BeamInfoCLS.CreateBeamSizeTextPoints(beamlist, min_range);
                }
            }
            ACD.Focus();

            return res;
        }

        void _initVariables()
        {
            beam_width = spnBeamWidth.Text.ToNumber();
            beam_height = spnBeamHeight.Text.ToNumber();
            slab_depth = spnSlabDepth.Text.ToNumber();
            spacing = spnSpacing.Text.ToNumber();
            min_range = spnMinRange.Text.ToNumber();
            current_view = (int)cbView.SelectedItem.ToString().ToNumber();
        }

        void _variableByName(string k)
        {
            string[] ar = k.Trim().filter(" ").Last().filter("x");

            if (ar.Length > 1)
            {
                beam_width = ar[0].ToNumber();
                if (beam_width < 100)
                    beam_width *= 10;
                beam_height = ar[1].ToNumber();
                if (beam_height < 100)
                    beam_height *= 10;
            }
        }

        void DrawBeamBox(pPos basept)
        {
            if (gridXys.Count == 0)
                _selectGridIds(ACD.GetSelection());

            if (gridXys.Count > 0)
            {
                _initVariables();
                Dictionary<string, string[]> chapters = content_chapters;
                
                double ny = 0;
                ObjectIdCollection res = new ObjectIdCollection();

                string[] prams = current_selected_template;
                string curr_key_name = null;
                
                foreach (string k in chapters.Keys)
                {
                    current_beam_name = k;
                    _variableByName(k);

                    if (curr_key_name != current_beam_name.Substring(0, 2))
                        ny += spacing;

                    curr_key_name = current_beam_name.Substring(0, 2);

                    string[] contents = chapters[k];
                    double[] vals = contents.Select(s => s._prop("Distance").ToNumber()).ToArray();

                    if (vals.Length > 1)
                    {
                        double start = vals[0], end = vals[1];
                        double sum = vals.Sum();
                        int beam_axis = k[1].ToString().ToUpper() == "X" ? 0 : 1;

                        ObjectIdCollection ids = _gridLineAxis(beam_axis, start, end);
                        pPos[] r = new pPos(start, 0).RectToPoint(new pPos(gridXys[beam_axis].Last()
                            - gridXys[beam_axis].First() + end, -beam_height));

                        ids.Add(ACD.DB.DrawPolyline(r, true, "LAYER=B-Beam"));

                        for (int j = 0; j < 4; j++)
                            ids.Add(IDimChain.CreateDimension(ACD.DB, r[j], r[(j + 1) % 4],
                                r[j].Parallel(r[(j + 1) % 4], min_range * 1.5).CenterPoint()));

                        ids.Add(ACD.DB.CreateText(k, new pPos(start + 500, beam_height * 2), 100, 0, "ANNO=1:25"));
                        ACD.DB.MoveObject(ids, new pPos(0, ny));

                        res.AddRange(ids);
                        ny += spacing;
                    }
                }

                ACD.DB.MoveObject(res, basept);
            }
        }

        ObjectIdCollection _gridLineAxis(int beam_axis, double start_offset, double end_offset)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            double nx = 0;
            double[] ls = gridXys[beam_axis];

            for (int i = 0; i < ls.Length; i++)
            {
                if(ls.First() + start_offset - 10 <= ls[i] && ls.Last() + end_offset + 10 >= ls[i])
                    res.AddRange(_columnAxisLine(nx, beam_axis == 0 ? (i + 1).ToString()
                        : ((char)(65 + i)).ToString(), new pPos(ls[i] - ls[0], 0), min_range));
            }
            return res;
        }

        void LoadData(IEnumerable<string> contents)
        {
            List<string> propnames = new List<string>();

            //string firstkey = "Chapter";

            foreach (string s in contents)
                propnames.AddRange(s._allPropNames().Where(c => c.Upper() != "CHAPTER"));

            propnames = propnames.Distinct().ToList();

            List<string> str = new List<string>();

            foreach (string s in contents)
            {
                str.Add("No=#Beam|Distance=" + s._firstProp());
                List<string[]> ls = DE.NumericArray(0, propnames.Count - 1)
                        .Select(n => s._prop(propnames[n]).filter(";")).ToList();

                int total = ls.Max(ar => ar.Length);

                for (int j = 0; j < total; j++)
                {
                    string st = "";

                    for (int i = 0; i < ls.Count; i++)
                        if (j < ls[i].Length)
                            st += propnames[i] + "=" + ls[i][j] + "|";

                    if (st.EndsWith("|"))
                        st = st.Remove(st.Length - 1);

                    str.Add(st);
                }
            }

            for (int i = 0; i < str.Count; i++)
            {
                string type = str[i]._prop("Type");
                if (!type.empty())
                {
                    int index = Array.FindIndex(types, s => s.Upper().Contains(type.Upper()));
                    if (index != -1)
                    {
                        string stype = types.ToTextStr(";");
                        stype = stype.Replace(types[index], "!" + types[index]);
                        str[i] = str[i]._setprop("Type", stype);
                    }
                }
            }

            File.WriteAllLines(@"D:\log.txt", str);

            ViewParamMultiData(str);
        }

        void _updateEditor()
        {
            Cursor.Hide();

            if (txtTemplate.Text.Length > 0)
            {
                txtTemplate.SetLineFormat(1, 100);
                txtTemplate.LoadTextBoxSelection();
            }

            if (txtGlobal.Text.Length > 0)
            {
                txtGlobal.SetLineFormat(1, 100);
                txtGlobal.LoadTextBoxSelection();
            }

            if (txtElement.Text.Length > 0)
            {
                txtElement.SetLineFormat(1, 100);
                txtElement.LoadTextBoxSelection();
            }

            Cursor.Show();
        }

        string current_beam_name = "";

        ObjectIdCollection _columnAxisLine(double nx, string letter, pPos mv, double colum_name_offset)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            pPos p1 = new pPos(0, -colum_name_offset);
            res.Add(ACD.DB.DrawPolyline(new pPos[] {
                new pPos(0, -colum_name_offset), new pPos(0, colum_name_offset) }, false, "LAYER=A-Hidden"));
            res.AddRange(ACD.DB.DrawCircle(p1, 200));

            if(!letter.empty())
                res.Add(ACD.DB.CreateText("#M" + letter, p1, 100));
            ACD.DB.MoveObject(res, mv);

            return res;
        }

        ObjectIdCollection DrawParams(IEnumerable<string> _str, IEnumerable<pPos> region, 
            string global, IEnumerable<pPos> boundary_limit)
        {
            List<string> str = _str.Select(s => s.Trim()).ToList();
            //int index = str.FindIndex(s => s.StartsWith("#GLOBAL"));

            //string global = "";
            //if (index != -1)
            //    global = str.ElementAt(index);

            //ACD.WR("OK1");

            ObjectIdCollection res = new ObjectIdCollection();
            pPos[] bb = region.Boundary();
            pPos sz = bb.Size();

            string rect_info = String.Format("R0X={0}|R0Y={1}|R0Z={2}|R1X={3}|R1Y={4}|R1Z={5}|BW={6}|BH={7}",
                bb[0].X, bb[0].Y, bb[0].Z, bb[0].X + sz.X, 
                bb[0].Y + beam_width, bb[0].Z - beam_height, beam_width, beam_height);

            //ACD.WR("RECT {0}", rect_info);
            //picViewBmp = new Bitmap(4000, 4000);
            //GraphExt.viewScale = 1;
            //GraphExt.canvas_quality = 1;
            //GraphExt.canvas_width = picViewBmp.Width;
            //GraphExt.canvas_height = picViewBmp.Height;

            //using (Graphics g = Graphics.FromImage(picViewBmp))
            //{
                //g.Clear(this.BackColor);
                
                for (int i = 0; i < str.Count; i++)
                {
                    string s = str[i];
                    if (!s._prop("VERTS").empty())
                    {
                        string tmp = (s + "|" + global + "|" + rect_info).ReplaceEquation();

                        foreach (string propname in s._allPropNames())
                            if(propname != s._firstPropName())
                                str[i] = str[i]._setprop(propname, tmp._prop(propname));
                    }
                }

                //GraphExt.UpdateViewScale(_getParamsBoundary(str),1);
                //ACD.WR("Screen {0}x{1},{2}", GraphExt.canvas_width, 
                //    GraphExt.canvas_height, GraphExt.viewScale);

                string[] note_txts = str.Where(st => st.StartsWith("#NOTE")).ToArray();

                foreach (string st in note_txts)
                {
                    pPos mv = st._prop("MOVE").empty() ? new pPos(0, 0) : pPos.FromString(st._prop("MOVE"));
                    PosCollection verts = new PosCollection(st._prop("VERTS")).Move(mv);

                    string[] prams = _getLibraryNote(st._firstProp(), verts);

                    ACD.WR(prams.ToTextStr("\r\n"));
                    if(prams != null) str.AddRange(prams);
                }

                foreach (string st in str)
                    if (st.StartsWith("#") && !st._prop("VERTS").empty())
                    {
                        ObjectIdCollection ids = new ObjectIdCollection();
                        string key = st._firstProp();
                        //string content = (st + "|" + global + "|" + rect_info).ReplaceEquation();
                        pPos mv = st._prop("MOVE").empty() ? new pPos(0, 0) : pPos.FromString(st._prop("MOVE"));
                        PosCollection verts = new PosCollection(st._prop("VERTS")).Move(mv);
                        
                        verts = _generateArrayList(verts, st);

                        //ACD.WR("AR02 {0}", verts.Count);

                        string object_style = st.Replace(st._firstPropName(), "LAYER");

                        //ACD.WR("AR02.0 {0}", object_style);
                        if (st.StartsWith("#LW"))
                        {
                            //ACD.WR("Verts:{0}", verts);
                            bb = verts.Boundary;
                            pPos ct = bb.CenterPoint();
                            //ACD.WR("AR02.1 {0}", verts.Count);
                            for (int i = 0; i < verts.Count; i++)
                            {
                            pPos[] pts = verts[i].Select(p => p.ByAxis(current_view)).ToArray();
                                //ObjectId lwpId = ACD.DB.DrawPolyline(pts, verts.Closed[i], object_style);

                            //ids.Add(ACD.DB.DrawPolyline(new pPos[] { boundary_limit.First(), boundary_limit.Last() }, false));
                                if(boundary_limit == null || pts.All(p 
                                    => p.X >= boundary_limit.First().X && p.X <= boundary_limit.Last().X))
                                    ids.Add(ACD.DB.DrawPolyline(pts, verts.Closed[i], object_style));
                            }
                            //ACD.WR("AR03");
                        }
                        else if (st.StartsWith("#CC"))
                        {
                            foreach (pPos p in verts.AllPoints)
                                res.AddRange(ACD.DB.DrawCircle(p.ByAxis(current_view),
                                    st._prop("RADIUS").ToNumber(20), object_style));
                        }
                        else if (st.StartsWith("#DIM"))
                        {
                            DimSettings = pString.INI_Params(key);

                            foreach (pPos[] ls in verts)
                            {
                                string over_txt = _get_over_text(ls[0].Content, global + "|" + rect_info);
                                
                                for (int i = 0; i < ls.Length - 1; i++)
                                {
                                    pPos p1 = ls[i].ByAxis(current_view), p2 = ls[i + 1].ByAxis(current_view);
                                    res.Add(IDimChain.CreateDimension(ACD.DB, p1, p2,
                                        p1.Parallel(p2, dim_space).CenterPoint(), over_txt, dim_flip_value));
                                }
                            }
                        }else if (st.StartsWith("#TXT"))
                        {
                            //ACD.WR("OK3.0");
                            foreach (pPos p in verts.AllPoints)
                            {
                                ObjectId id = ACD.DB.CreateText(_get_over_text(p.Content, global + "|" + rect_info),
                                    p.ByAxis(current_view), (int)st._prop("HEIGHT").ToNumber(100));
                                ACD.DB._setRotation(id, p.Rotation);
                                //ACD.WR("OK3.0A");
                                ACD.DB._setLayer(id, st._firstProp());
                                //ACD.WR("OK3.0B");
                                ids.Add(id);
                            }
                            //ACD.WR("OK3.1");
                        }
                        else if (st.StartsWith("#INS"))
                        {
                            ids.AddRange(ACD.DB.Insert(key, verts.AllPoints.Select(p => p.ByAxis(current_view)), object_style));
                        }

                        res.AddRange(ids);
                    }
            //}

            //picView.ImageValue = picViewBmp;
            //ACD.WR("OK3");
            //picViewBmp = (Bitmap)Bitmap.FromFile(@"D:\QRCode12MS.png");

            return res;
        }

        string _get_over_text(string st, string setting)
        {
            if (!st.empty())
            {
                string over_txt = ("TXT=" + st + "|" + setting).ReplaceEquation()._firstProp();
                over_txt = over_txt.Replace("_ ", "");
                over_txt = over_txt.Replace(" _", "");

                return over_txt;
            }
            else
                return "";
        }

        string[] _getLibraryNote(string key, PosCollection verts)
        {
            List<string> res = new List<string> ();
            string[] prams = pString.INI_Params(key);

            if(prams != null && prams.Length > 0)
            {
                foreach(pPos p in verts.AllPoints)
                {
                    //ACD.WR("Content {0}", p);
                    string[] ar = p.Content.filter("&");

                    for(int i = 0; i < prams.Length; i++)
                    {
                        string s = prams[i];
                        s += "|MOVE=" + p.X + "," + p.Y + "," + p.Z;

                        if(s.StartsWith("#TXT") && !p.Content.empty())
                        {
                            PosCollection pls = new PosCollection(s._prop("VERTS"));

                            foreach (pPos[] ls in pls)
                                for (int j = 0; j < ar.Length; j++)
                                    if (j < ls.Length)
                                        ls[j].Content = ar[j];

                            s = s._setprop("VERTS", pls.ToString());
                        }

                        res.Add(s);
                    }
                }
            }

            return res.ToArray();
        }

        PosCollection _generateArrayList(PosCollection src, string pram)
        {
            PosCollection res = src.Select(ls => ls).ToCollectionSameClosed();
            res.Closed = src.Closed;

            if (!pram._prop("ARR").empty())
            {
                //string info = pram._allVariableAndValues();
                PosCollection pls = new PosCollection(pram._prop("ARR")._getInComma("[]"));

                ACD.WR("[AR_COMMA]{0};{1}", pram._prop("ARR")._getInComma(), 
                    pram._prop("ARR")._getInComma());

                if (pls.Count > 0)
                {
                    pPos[] step_list = pls.First;
                    pPos to_values = pPos.FromString(pram._prop("ARR")._getBeforeComma("[]"));

                    //Console.WriteLine("ARR:{0} FROM {1} TO:{2}", pram._prop("ARR"), this1, to_values);

                    pPos[] bb = src.Boundary;
                    pPos ct = bb.CenterPoint();

                    ACD.WR("[ARRAY]FROM {0} TO:{1} BY:{2}\r\nPRAM:{2}",
                        bb[0], to_values, step_list.ToText(), pram);

                    foreach (pPos step in step_list)
                    {
                        int[] indexes = DE.NumericArray(0, 2)
                            .Select(n => step[n] == 0 ? 1 : ((int)Math.Ceiling((to_values[n] - ct[n]) / step[n]) + 1)).ToArray();

                        for (int i = 0; i < indexes[0]; i++)
                            for (int j = 0; j < indexes[1]; j++)
                                for (int k = 0; k < indexes[2]; k++)
                                    if (i != 0 || j != 0 || k != 0)
                                    {
                                        List<bool> ar = res.Closed.ToList();
                                        ar.AddRange(src.Closed);
                                        res.AddRange(src.Select(ls => ls.Move(new pPos(i * step.X, j * step.Y, k * step.Z))));
                                        res.Closed = ar.ToArray();
                                    }
                    }


                }
            }
            return res;
        }
        
        pPos[] _getParamsBoundary(IEnumerable<string> str)
        {
            PosCollection pls = new PosCollection();
            foreach (string st in str)
                if (!st._prop("VERTS").empty())
                    pls.AddRange(new PosCollection(st._prop("VERTS")));

            return pls.Boundary;
        }

        string[] current_selected_template
        {
            get
            {
                var dict = File.ReadAllLines(DE.CADLIB_CONSTRUCT + "Beam.txt").GetChapters();
                int index = dict.Keys.ToList().FindIndex(s => s.Upper() == cbTemplate.SelectedItem.ToString().Upper());

                string[] res = null;
                if (index != -1)
                    res = dict.Values.ElementAt(index);

                return res;
            }
        }

        Dictionary<string, string[]> content_chapters
        {
            get
            {
                string[] str = GetDataViewContent();

                for (int i = 0; i < str.Length; i++)
                    str[i] = str[i].Replace("No=#Beam|Distance=", "CHAPTER=");
                
                Dictionary<string, string[]> chapters = str.GetChapters();
                return chapters;
            }
        }

        string struct_file
        {
            get
            {
                string cadfile = ACD.CurrentDWGPath;
                return Path.Combine(Path.GetDirectoryName(cadfile),
                    Path.GetFileNameWithoutExtension(ACD.CurrentDWGFileName) + "_beam.txt");
            }
        }

        int current_view = 2;
        double beam_height, beam_width, slab_depth, spacing, min_range;
        string[] types = new string[] { "Column Fit","Column Middle", "Extent","Beam Intersect"};
        Bitmap picViewBmp;
    }

    public class StructuralBeamCLS
    {
        public static void Main(string[] args)
        {
            frmStructuralBeam frm = new AcadScript.frmStructuralBeam();
            frm.Show();
        }
    }
}
