using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace AcadScript
{
    // ─────────────────────────────────────────────────────────────────────────
    // Data model
    // ─────────────────────────────────────────────────────────────────────────
    public class QTE_Entity
    {
        public string Handle    { get; set; }
        public string TextType  { get; set; }   // TEXT | MTEXT | LEADER | MULTILEADER
        public string Content   { get; set; }
        public string LayerName { get; set; }
        public string StyleName { get; set; }
        public Point3d Position { get; set; }

        public override string ToString()
        {
            string p = (Content ?? "").Replace("\r","").Replace("\n"," ").Replace("\\P"," ");
            if (p.Length > 70) p = p.Substring(0, 70) + "…";
            return string.Format("[{0,-11}] {1}", TextType, p);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Modeless Form — built entirely in code (no Designer file needed)
    // ─────────────────────────────────────────────────────────────────────────
    public class QuickTextEditorForm : Form
    {
        // ── Controls ──────────────────────────────────────────────────────────
        Button  btnAll, btnTEXT, btnMTEXT, btnML, btnLEADER, btnReload;
        TextBox txtSearch, txtContent;
        ListBox lstItems;
        ComboBox cboStyle;
        Button  btnApply;
        Label   lblLayer, lblType, lblHandle, lblStatus;
        SplitContainer split;

        // ── State ─────────────────────────────────────────────────────────────
        List<QTE_Entity> _all    = new List<QTE_Entity>();
        List<string>     _styles = new List<string>();
        string           _filter = "ALL";
        bool             _isUpdatingSelection = false;

        const string SEARCH_HINT = "Tìm kiếm…";

        // ─────────────────────────────────────────────────────────────────────
        public QuickTextEditorForm()
        {
            BuildUI();
        }

        // Helper class-level methods to replace C# 7.0 nested local functions
        Button MakeBtn(Panel pnlTop, ref int bx, string txt, string tag, int w)
        {
            var b = new Button {
                Text = txt, Tag = tag,
                Location = new System.Drawing.Point(bx, 4), Size = new Size(w, 26),
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            b.Click += FilterClick;
            pnlTop.Controls.Add(b);
            bx += w + 3;
            return b;
        }

        Label MakeLbl(Panel pnlRight, ref int ry, string txt, bool bold)
        {
            var l = new Label { Text = txt, Location = new System.Drawing.Point(8, ry),
                AutoSize = true };
            if (bold) l.Font = new System.Drawing.Font("Segoe UI", 9f, FontStyle.Bold);
            pnlRight.Controls.Add(l);
            ry += 18;
            return l;
        }

        // ─────────────────────────────────────────────────────────────────────
        void BuildUI()
        {
            // Form
            this.Text          = "✏ Quick Text Editor";
            this.Size          = new Size(900, 560);
            this.MinimumSize   = new Size(660, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font          = new System.Drawing.Font("Segoe UI", 9f);
            this.BackColor     = System.Drawing.Color.FromArgb(245, 245, 248);

            // ── Filter bar (top) ──────────────────────────────────────────────
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 36,
                BackColor = System.Drawing.Color.FromArgb(232, 232, 240), Padding = new Padding(4,4,4,2) };

            int bx = 4;
            btnAll    = MakeBtn(pnlTop, ref bx, "All",         "ALL",         48);
            btnTEXT   = MakeBtn(pnlTop, ref bx, "TEXT",        "TEXT",        54);
            btnMTEXT  = MakeBtn(pnlTop, ref bx, "MTEXT",       "MTEXT",       58);
            btnML     = MakeBtn(pnlTop, ref bx, "MULTILEADER", "MULTILEADER", 90);
            btnLEADER = MakeBtn(pnlTop, ref bx, "LEADER",      "LEADER",      60);
            btnAll.BackColor = System.Drawing.Color.FromArgb(173, 216, 230);

            btnReload = new Button { Text = "⟳ Reload", Dock = DockStyle.Right,
                Width = 80, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnReload.Click += (s, e) => LoadData();
            pnlTop.Controls.Add(btnReload);

            // ── Status bar (bottom) ───────────────────────────────────────────
            lblStatus = new Label { Dock = DockStyle.Bottom, Height = 22,
                Text = "Sẵn sàng.", Padding = new Padding(6, 3, 0, 0),
                BackColor = System.Drawing.Color.FromArgb(220, 220, 228),
                ForeColor = System.Drawing.Color.FromArgb(50, 50, 70) };

            // ── SplitContainer ────────────────────────────────────────────────
            split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 350 };
            split.Panel1.BackColor = System.Drawing.Color.White;
            split.Panel1.Padding   = new Padding(2);

            // ── Left: search + list ───────────────────────────────────────────
            txtSearch = new TextBox {
                Dock = DockStyle.Top, Height = 26,
                Text = SEARCH_HINT, ForeColor = System.Drawing.Color.Gray,
                BorderStyle = BorderStyle.FixedSingle };
            txtSearch.GotFocus  += (s,e) => { if (txtSearch.Text == SEARCH_HINT)
                { txtSearch.Text = ""; txtSearch.ForeColor = System.Drawing.Color.Black; }};
            txtSearch.LostFocus += (s,e) => { if (string.IsNullOrEmpty(txtSearch.Text))
                { txtSearch.Text = SEARCH_HINT; txtSearch.ForeColor = System.Drawing.Color.Gray; }};
            txtSearch.TextChanged += (s,e) => DoSearch();

            lstItems = new ListBox {
                Dock = DockStyle.Fill, BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 20,
                IntegralHeight = false };
            lstItems.DrawItem             += (s, e) => DrawListItem(e);
            lstItems.SelectedIndexChanged += ItemSelected;

            split.Panel1.Controls.Add(lstItems);
            split.Panel1.Controls.Add(txtSearch);

            // ── Right: edit panel ─────────────────────────────────────────────
            var pnlRight = new Panel { Dock = DockStyle.Fill, AutoScroll = true,
                Padding = new Padding(8) };

            int ry = 8;
            MakeLbl(pnlRight, ref ry, "Nội dung:", true);
            txtContent = new TextBox {
                Location = new System.Drawing.Point(8, ry), Size = new Size(300, 160),
                Multiline = true, ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            pnlRight.Controls.Add(txtContent);
            ry += 168;

            MakeLbl(pnlRight, ref ry, "Text Style:", true);
            cboStyle = new ComboBox {
                Location = new System.Drawing.Point(8, ry), Size = new Size(300, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            pnlRight.Controls.Add(cboStyle);
            ry += 32;

            btnApply = new Button {
                Text = "✓  Apply Changes",
                Location = new System.Drawing.Point(8, ry), Size = new Size(150, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = System.Drawing.Color.FromArgb(60, 180, 100), ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand };
            btnApply.Click += ApplyChanges;
            pnlRight.Controls.Add(btnApply);
            ry += 46;

            lblLayer  = MakeLbl(pnlRight, ref ry, "Layer:", false);
            lblType   = MakeLbl(pnlRight, ref ry, "Type:", false);
            lblHandle = MakeLbl(pnlRight, ref ry, "Handle:", false);
            lblHandle.ForeColor = System.Drawing.Color.Gray;

            split.Panel2.Controls.Add(pnlRight);

            // Fix right-panel control widths on resize
            split.Panel2.Resize += (s, e) => {
                int w = split.Panel2.ClientSize.Width - 20;
                txtContent.Width = w;
                cboStyle.Width   = w;
            };

            // ── Assemble form ─────────────────────────────────────────────────
            this.Controls.Add(split);
            this.Controls.Add(pnlTop);
            this.Controls.Add(lblStatus);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Real-time viewport selection events
        // ─────────────────────────────────────────────────────────────────────
        private Document _subscribedDoc = null;

        private void SubscribeDocumentEvents()
        {
            try
            {
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.DocumentActivated += DocumentManager_DocumentActivated;
                SubscribeSelection(Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument);
            }
            catch {}
        }

        private void UnsubscribeDocumentEvents()
        {
            try
            {
                Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.DocumentActivated -= DocumentManager_DocumentActivated;
                UnsubscribeSelection(_subscribedDoc);
            }
            catch {}
        }

        private void SubscribeSelection(Document doc)
        {
            if (doc == null) return;
            try
            {
                if (_subscribedDoc != doc)
                {
                    UnsubscribeSelection(_subscribedDoc);
                    doc.ImpliedSelectionChanged += Doc_ImpliedSelectionChanged;
                    _subscribedDoc = doc;
                }
            }
            catch {}
        }

        private void UnsubscribeSelection(Document doc)
        {
            if (doc == null) return;
            try
            {
                doc.ImpliedSelectionChanged -= Doc_ImpliedSelectionChanged;
                if (_subscribedDoc == doc)
                    _subscribedDoc = null;
            }
            catch {}
        }

        private void DocumentManager_DocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            if (e.Document != null)
            {
                SubscribeSelection(e.Document);
                LoadData();
            }
        }

        private void Doc_ImpliedSelectionChanged(object sender, EventArgs e)
        {
            if (_isUpdatingSelection) return;
            _isUpdatingSelection = true;
            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (doc == null) return;

                var selectResult = doc.Editor.SelectImplied();
                if (selectResult.Status == PromptStatus.OK && selectResult.Value != null && selectResult.Value.Count > 0)
                {
                    var id = selectResult.Value[0].ObjectId;
                    string handleStr = id.Handle.ToString();

                    var match = _all.FirstOrDefault(x => x.Handle == handleStr);
                    if (match != null)
                    {
                        // Check if item exists in current filtered listbox
                        int idx = -1;
                        for (int i = 0; i < lstItems.Items.Count; i++)
                        {
                            var item = lstItems.Items[i] as QTE_Entity;
                            if (item != null && item.Handle == handleStr)
                            {
                                idx = i;
                                break;
                            }
                        }

                        if (idx == -1)
                        {
                            // If not in current filtered list, automatically switch to its correct list
                            SetFilterAndSelect(match);
                        }
                        else
                        {
                            // Temporarily detach select handler to prevent zoom recursion
                            lstItems.SelectedIndexChanged -= ItemSelected;
                            lstItems.SelectedIndex = idx;
                            lstItems.SelectedIndexChanged += ItemSelected;

                            // Update UI fields
                            txtContent.Text = match.Content;
                            lblLayer.Text   = "Layer:  " + match.LayerName;
                            lblType.Text    = "Type:   " + match.TextType;
                            lblHandle.Text  = "Handle: " + match.Handle;

                            int si = _styles.IndexOf(match.StyleName);
                            cboStyle.SelectedIndex = si >= 0 ? si : (cboStyle.Items.Count > 0 ? 0 : -1);
                        }
                    }
                }
            }
            catch {}
            finally
            {
                _isUpdatingSelection = false;
            }
        }

        void SetFilterAndSelect(QTE_Entity match)
        {
            _filter = match.TextType;
            
            Button targetBtn = btnAll;
            if (_filter == "TEXT") targetBtn = btnTEXT;
            else if (_filter == "MTEXT") targetBtn = btnMTEXT;
            else if (_filter == "MULTILEADER") targetBtn = btnML;
            else if (_filter == "LEADER") targetBtn = btnLEADER;

            foreach (var b in new[]{btnAll,btnTEXT,btnMTEXT,btnML,btnLEADER})
                b.BackColor = SystemColors.Control;
            if (targetBtn != null)
                targetBtn.BackColor = System.Drawing.Color.FromArgb(173, 216, 230);

            var list = _filter == "ALL" ? _all : _all.Where(x => x.TextType == _filter).ToList();
            lstItems.DataSource = null;
            lstItems.DataSource = list;
            lblStatus.Text = string.Format("{0} / {1} entities", list.Count, _all.Count);

            int idx = -1;
            for (int i = 0; i < lstItems.Items.Count; i++)
            {
                var item = lstItems.Items[i] as QTE_Entity;
                if (item != null && item.Handle == match.Handle)
                {
                    idx = i;
                    break;
                }
            }

            if (idx != -1)
            {
                lstItems.SelectedIndexChanged -= ItemSelected;
                lstItems.SelectedIndex = idx;
                lstItems.SelectedIndexChanged += ItemSelected;

                txtContent.Text = match.Content;
                lblLayer.Text   = "Layer:  " + match.LayerName;
                lblType.Text    = "Type:   " + match.TextType;
                lblHandle.Text  = "Handle: " + match.Handle;

                int si = _styles.IndexOf(match.StyleName);
                cboStyle.SelectedIndex = si >= 0 ? si : (cboStyle.Items.Count > 0 ? 0 : -1);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Load data from current drawing
        // ─────────────────────────────────────────────────────────────────────
        public void LoadData()
        {
            try
            {
                _all    = ScanDrawing();
                _styles = GetStyles();

                cboStyle.DataSource    = null;
                cboStyle.DataSource    = _styles;
                cboStyle.SelectedIndex = _styles.Count > 0 ? 0 : -1;

                ApplyFilter(_filter);
            }
            catch (System.Exception ex) { lblStatus.Text = "Lỗi load: " + ex.Message; }
        }

        // ─────────────────────────────────────────────────────────────────────
        void FilterClick(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            _filter = (btn.Tag != null) ? btn.Tag.ToString() : "ALL";
            ApplyFilter(_filter);

            foreach (var b in new[]{btnAll,btnTEXT,btnMTEXT,btnML,btnLEADER})
                b.BackColor = SystemColors.Control;
            btn.BackColor = System.Drawing.Color.FromArgb(173, 216, 230);
        }

        void ApplyFilter(string type)
        {
            var list = type == "ALL"
                ? _all
                : _all.Where(x => x.TextType == type).ToList();

            lstItems.DataSource = null;
            lstItems.DataSource = list;
            lblStatus.Text      = string.Format("{0} / {1} entities", list.Count, _all.Count);
        }

        void DoSearch()
        {
            string q = (txtSearch.ForeColor == System.Drawing.Color.Gray) ? "" : txtSearch.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(q)) { ApplyFilter(_filter); return; }

            var src = _filter == "ALL" ? _all : _all.Where(x => x.TextType == _filter).ToList();
            var res = src.Where(x =>
                (x.Content   ?? "").ToLower().Contains(q) ||
                (x.LayerName ?? "").ToLower().Contains(q)).ToList();

            lstItems.DataSource = null;
            lstItems.DataSource = res;
            lblStatus.Text      = string.Format("Lọc: {0} / {1}", res.Count, _all.Count);
        }

        // ─────────────────────────────────────────────────────────────────────
        void DrawListItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();
            var item = lstItems.Items[e.Index] as QTE_Entity;
            if (item != null)
            {
                System.Drawing.Color clr = item.TextType == "TEXT"        ? System.Drawing.Color.FromArgb(0,80,160) :
                            item.TextType == "MTEXT"       ? System.Drawing.Color.FromArgb(0,120,60) :
                            item.TextType == "MULTILEADER" ? System.Drawing.Color.FromArgb(140,60,0) :
                                                             System.Drawing.Color.FromArgb(100,0,120);
                var rect = e.Bounds;
                // Type badge
                using (var br = new SolidBrush(System.Drawing.Color.FromArgb(30, clr)))
                    e.Graphics.FillRectangle(br, new Rectangle(rect.X, rect.Y+2, 90, rect.Height-4));
                using (var br = new SolidBrush(clr))
                    e.Graphics.DrawString(string.Format("[{0}]", item.TextType), new System.Drawing.Font("Segoe UI",8f,FontStyle.Bold),
                        br, new RectangleF(rect.X+2, rect.Y+2, 88, rect.Height));
                // Content
                string preview = (item.Content ?? "").Replace("\\P"," ").Replace("\n"," ");
                using (var br = new SolidBrush(e.ForeColor))
                    e.Graphics.DrawString(preview, e.Font, br,
                        new RectangleF(rect.X+96, rect.Y+2, rect.Width-100, rect.Height));
            }
            e.DrawFocusRectangle();
        }

        // ─────────────────────────────────────────────────────────────────────
        void ItemSelected(object sender, EventArgs e)
        {
            var item = lstItems.SelectedItem as QTE_Entity;
            if (item == null) return;

            txtContent.Text = item.Content;
            lblLayer.Text   = "Layer:  " + item.LayerName;
            lblType.Text    = "Type:   " + item.TextType;
            lblHandle.Text  = "Handle: " + item.Handle;

            int si = _styles.IndexOf(item.StyleName);
            cboStyle.SelectedIndex = si >= 0 ? si : (cboStyle.Items.Count > 0 ? 0 : -1);

            ZoomTo(item);
        }

        // ─────────────────────────────────────────────────────────────────────
        void ZoomTo(QTE_Entity item)
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            try
            {
                using (doc.LockDocument())
                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var oid = ResolveHandle(doc.Database, item.Handle);
                    var ent = tr.GetObject(oid, OpenMode.ForRead) as Entity;
                    if (ent == null) { tr.Commit(); return; }

                    Extents3d ext;
                    try { ext = ent.GeometricExtents; } catch { tr.Commit(); return; }

                    double w = ext.MaxPoint.X - ext.MinPoint.X;
                    double h = ext.MaxPoint.Y - ext.MinPoint.Y;
                    double m = Math.Max(Math.Max(w, h) * 1.5, 500.0);

                    var view = doc.Editor.GetCurrentView();
                    view.CenterPoint = new Point2d(
                        (ext.MinPoint.X + ext.MaxPoint.X) / 2.0,
                        (ext.MinPoint.Y + ext.MaxPoint.Y) / 2.0);
                    view.Width  = w + m;
                    view.Height = h + m;
                    doc.Editor.SetCurrentView(view);
                    tr.Commit();
                }
            }
            catch (System.Exception ex) { lblStatus.Text = "Zoom lỗi: " + ex.Message; }
        }

        // ─────────────────────────────────────────────────────────────────────
        void ApplyChanges(object sender, EventArgs e)
        {
            var item = lstItems.SelectedItem as QTE_Entity;
            if (item == null) { lblStatus.Text = "Chưa chọn item."; return; }

            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            string newContent = txtContent.Text;
            string newStyle   = (cboStyle.SelectedItem != null) ? cboStyle.SelectedItem.ToString() : null;

            try
            {
                using (doc.LockDocument())
                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var oid = ResolveHandle(doc.Database, item.Handle);
                    var ent = tr.GetObject(oid, OpenMode.ForWrite) as Entity;
                    if (ent == null) { tr.Commit(); return; }

                    var tst = (TextStyleTable)tr.GetObject(
                        doc.Database.TextStyleTableId, OpenMode.ForRead);

                    DBText dt = ent as DBText;
                    if (dt != null)
                    {
                        dt.TextString = newContent;
                        if (!string.IsNullOrEmpty(newStyle) && tst.Has(newStyle))
                            dt.TextStyleId = tst[newStyle];
                    }
                    else
                    {
                        MText mt = ent as MText;
                        if (mt != null)
                        {
                            mt.Contents = newContent;
                            if (!string.IsNullOrEmpty(newStyle) && tst.Has(newStyle))
                                mt.TextStyleId = tst[newStyle];
                        }
                        else
                        {
                            MLeader ml = ent as MLeader;
                            if (ml != null)
                            {
                                if (ml.ContentType == ContentType.MTextContent && ml.MText != null)
                                {
                                    var clone = ml.MText.Clone() as MText;
                                    if (clone != null) { clone.Contents = newContent; ml.MText = clone; }
                                }
                            }
                        }
                    }

                    tr.Commit();
                }

                item.Content   = newContent;
                item.StyleName = newStyle ?? item.StyleName;
                lstItems.Refresh();
                lblStatus.Text = "✅ Đã lưu.";
                doc.Editor.Regen();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Lỗi lưu: " + ex.Message, "Quick Text Editor",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        static ObjectId ResolveHandle(Database db, string hex)
        {
            return db.GetObjectId(false, new Handle(Convert.ToInt64(hex, 16)), 0);
        }

        // Form events subscription management
        // ─────────────────────────────────────────────────────────────────────
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible)
            {
                SubscribeDocumentEvents();
                LoadData();
            }
            else
            {
                UnsubscribeDocumentEvents();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                UnsubscribeDocumentEvents();
            }
            else
            {
                UnsubscribeDocumentEvents();
                base.OnFormClosing(e);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Scanner — đọc text entities
        // ─────────────────────────────────────────────────────────────────────
        static List<QTE_Entity> ScanDrawing()
        {
            var res = new List<QTE_Entity>();
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return res;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var btr = (BlockTableRecord)tr.GetObject(
                    doc.Database.CurrentSpaceId, OpenMode.ForRead);

                foreach (ObjectId id in btr)
                {
                    Entity ent;
                    try { ent = tr.GetObject(id, OpenMode.ForRead) as Entity; }
                    catch { continue; }
                    if (ent == null) continue;

                    QTE_Entity m = null;

                    DBText dbText = ent as DBText;
                    if (dbText != null)
                    {
                        m = new QTE_Entity { Handle = id.Handle.ToString(), TextType = "TEXT",
                            Content = dbText.TextString, LayerName = dbText.Layer,
                            StyleName = dbText.TextStyleName, Position = dbText.Position };
                    }
                    else
                    {
                        MText mtext = ent as MText;
                        if (mtext != null)
                        {
                            m = new QTE_Entity { Handle = id.Handle.ToString(), TextType = "MTEXT",
                                Content = mtext.Contents, LayerName = mtext.Layer,
                                StyleName = mtext.TextStyleName, Position = mtext.Location };
                        }
                        else
                        {
                            MLeader ml = ent as MLeader;
                            if (ml != null && ml.ContentType == ContentType.MTextContent && ml.MText != null)
                            {
                                string sn = "Standard";
                                try {
                                    var sr = tr.GetObject(ml.MText.TextStyleId, OpenMode.ForRead)
                                             as TextStyleTableRecord;
                                    if (sr != null) sn = sr.Name;
                                } catch {}
                                m = new QTE_Entity { Handle = id.Handle.ToString(), TextType = "MULTILEADER",
                                    Content = ml.MText.Contents, LayerName = ml.Layer,
                                    StyleName = sn, Position = ml.MText.Location };
                            }
                            else
                            {
                                Leader ldr = ent as Leader;
                                if (ldr != null && ldr.HasArrowHead && ldr.Annotation != ObjectId.Null)
                                {
                                    try {
                                        var ann = tr.GetObject(ldr.Annotation, OpenMode.ForRead);
                                        string txt = null, sn = "Standard";
                                        Point3d pos = Point3d.Origin;
                                        DBText dt2 = ann as DBText;
                                        MText mt2 = ann as MText;
                                        if (dt2 != null) { txt = dt2.TextString; sn = dt2.TextStyleName; pos = dt2.Position; }
                                        else if (mt2 != null) { txt = mt2.Contents; sn = mt2.TextStyleName; pos = mt2.Location; }
                                        if (txt != null)
                                            m = new QTE_Entity { Handle = ldr.Annotation.Handle.ToString(),
                                                TextType = "LEADER", Content = txt, LayerName = ldr.Layer,
                                                StyleName = sn, Position = pos };
                                    } catch {}
                                }
                            }
                        }
                    }

                    if (m != null) res.Add(m);
                }
                tr.Commit();
            }
            return res;
        }

        static List<string> GetStyles()
        {
            var styles = new List<string>();
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return styles;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var tst = (TextStyleTable)tr.GetObject(
                    doc.Database.TextStyleTableId, OpenMode.ForRead);
                foreach (ObjectId id in tst)
                {
                    var s = tr.GetObject(id, OpenMode.ForRead) as TextStyleTableRecord;
                    if (s != null && !string.IsNullOrEmpty(s.Name)) styles.Add(s.Name);
                }
                tr.Commit();
            }
            styles.Sort();
            return styles;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Entry point — giống pattern của Connect ZigZag, Polyline Union, v.v.
    // Dùng Application.OpenForms để tìm form đã mở (survive re-compile)
    // ─────────────────────────────────────────────────────────────────────────
    public class QuickTextEditorCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                // Tìm form đang mở (nếu script bị re-compile thì static field mất,
                // nhưng form vẫn còn trong OpenForms)
                QuickTextEditorForm form = null;
                foreach (Form f in System.Windows.Forms.Application.OpenForms)
                {
                    QuickTextEditorForm qte = f as QuickTextEditorForm;
                    if (qte != null) { form = qte; break; }
                }

                if (form == null || form.IsDisposed)
                    form = new QuickTextEditorForm();

                form.LoadData();

                if (!form.Visible)
                {
                    var wrapper = new AcadWindowHandle(
                        System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle);
                    form.Show(wrapper);
                }

                form.BringToFront();
                ACD.Focus();
            }
        }
    }

    // IWin32Window wrapper cho AutoCAD main window
    class AcadWindowHandle : System.Windows.Forms.IWin32Window
    {
        readonly IntPtr _h;
        public AcadWindowHandle(IntPtr h) { _h = h; }
        public IntPtr Handle { get { return _h; } }
    }
}
