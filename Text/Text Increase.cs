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
//using SyncObject;

namespace AcadScript
{
    public class stepForm : Form
    {
        ComboBox cbType, cbAxis;
        TextBox toler, offset_toler, start_toler;
        ObjectIdCollection selIds, resultIds;
        Button btnstep;
        Label offset_label, step_label, start_label;
        CheckBox ckComma;

        public stepForm()
        {
            this.Width = 300;
            this.Height = 200;
            this.TopMost = true;
            Text = "step";
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;

            //ACD.WR("OK1");

            cbType = new ComboBox()
            {
                Left = 10,
                Top = 10,
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cbAxis = new ComboBox()
            {
                Left = this.Width / 2,
                Top = 10,
                Width = cbType.Width,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cbType.Items.Clear();
            cbType.Items.AddRange(new string[] { "Text", "Layer", "Bock" });
            cbType.SelectedIndex = 0;
            cbAxis.Items.Clear();
            cbAxis.Items.AddRange(new string[] { "X", "Y" });
            cbAxis.SelectedIndex = 0;
            //ACD.WR("OK2");
            step_label = new Label()
            {
                Left = 10,
                Top = cbType.Top + cbType.Height + 10,
                Width = 40,
                Text = "Step"
            };

            toler = new TextBox()
            {
                Left = step_label.Left + step_label.Width,
                Top = step_label.Top,
                Width = 80,
                TextAlign = HorizontalAlignment.Right,
                Text = "1"
            };
            //ACD.WR("OK3");
            start_label = new Label()
            {
                Left = cbAxis.Left,
                Top = step_label.Top,
                Width = 40,
                Text = "Start"
            };

            start_toler = new TextBox()
            {
                Left = start_label.Left + start_label.Width,
                Top = step_label.Top,
                Width = cbAxis.Width - start_label.Width,
                Text = "1"
            };

            offset_label = new Label()
            {
                Left = cbType.Left,
                Top = toler.Top + toler.Height + 10,
                Text = "Offset",
                Width = step_label.Width
            };

            offset_toler = new TextBox()
            {
                Left = toler.Left,
                Top = offset_label.Top,
                Width = toler.Width,
                TextAlign = HorizontalAlignment.Right,
                Text = "1000"
            };

            ckComma = new CheckBox()
            {
                Left = step_label.Left,
                Top = offset_label.Top + offset_label.Height,
                Width = cbAxis.Width,
                Text = "Use comma",
                //Height = 30,
                Checked = false
            };
            
            btnstep = new Button()
            {
                Left = cbAxis.Left,
                Top = ckComma.Top + ckComma.Height,
                Width = cbAxis.Width,
                Text = "Step",
                Height = 30
            };
            
            this.Controls.AddRange(new Control[] { toler, btnstep, step_label,
                offset_toler, offset_label, cbType , cbAxis, start_label, start_toler, ckComma});
            //ACD.WR("OK4");
            btnstep.Click += (o, e) =>
            {
                _runstep();
            };
        }

        void _runstep()
        {
            using (ACD.Lock())
            {
                ACD.Focus();
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    string type = cbType.SelectedItem.ToString();
                    if (type == "Text")
                    {
                        selIds = selIds._filterDXF("TEXT", "MTEXT");
                        _stepText(selIds, start_toler.Text.ToNumber(), 
                            toler.Text.ToNumber(),cbAxis.SelectedItem.ToString() == "Y");
                    }
                    else if (type == "Layer")
                    {
                        //ACD.WR("OK2");
                        string[] layers = selIds.ToList().Select(id 
                            => ACD.DB._getLayer(id)).Distinct().OrderBy(s => s).ToArray();
                        //ACD.WR("Layer={0}", layers.ToText());
                        int axis = cbAxis.SelectedItem.ToString() == "X" ? 0 : 1;

                        double offset = offset_toler.Text.ToNumber();
                        foreach(ObjectId id in selIds)
                        {
                            int index = Array.FindIndex(layers, s => ACD.DB._getLayer(id) == s);
                            if (index != -1)
                            {
                                pPos p = new pPos(0, 0);
                                p[axis] = index * offset;
                                ACD.DB.MoveObject(id, p);
                            }
                        }
                    }
                }
            }

            ACD.Focus();
        }

        void _stepText(ObjectIdCollection selIds, double start = 1, double step = 1, bool by_axis_Y = true)
        {
            selIds = selIds.ToList().OrderBy(id 
                => by_axis_Y ? ACD.DB._getPoint(id).Y : ACD.DB._getPoint(id).X).ToCollection();
            
            AttachmentPoint align = _getAlignment(ACD.DB, selIds.First());

            foreach (ObjectId txtId in selIds)
            {
                string st = ACD.DB._getContent(txtId);

                _setContent(ACD.DB, txtId, _getStringWithoutNumber(st) 
                    + (ckComma.Checked ? "(" : "") + start + (ckComma.Checked ? ")":""));
                //ACD.WR("Content {0}", _getStringWithoutNumber(ACD.DB._getContent(txtId)) + start);
                start += step;
            }
        }
    
        AttachmentPoint _getAlignment(Database db, ObjectId srcId)
        {
            AttachmentPoint align = AttachmentPoint.MiddleCenter;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                switch (srcId.ObjectClass.DxfName)
                {
                    case "TEXT":
                        DBText txt = (DBText)tr.GetObject(srcId, OpenMode.ForRead);
                        align = txt.Justify;
                        break;
                    case "MTEXT":
                        MText stxt = (MText)tr.GetObject(srcId, OpenMode.ForRead);
                        align = stxt.Attachment;
                        break;

                }
                tr.Commit();
            }
            return align;
        }

        void _applyAlignment(Database db, ObjectId toId, AttachmentPoint align)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                switch (toId.ObjectClass.DxfName)
                {
                    case "TEXT":
                        DBText txt = (DBText)tr.GetObject(toId, OpenMode.ForWrite);
                        txt.Justify = align;
                        break;
                    case "MTEXT":
                        MText stxt = (MText)tr.GetObject(toId, OpenMode.ForWrite);
                        stxt.Attachment = align;
                        break;
                }

                tr.Commit();
            }
        }

        string _getStringWithoutNumber(string _st)
        {
            string key = "0123456789";
            string st = _st.Trim().Invert();
            int n = -1;

            foreach(char c in key)
            {
                int index = st.IndexOf(c);
                //ACD.WR("_index {0} {1} {2}", st, c, index);
                if (index > n)
                    n = index;
            }

            //ACD.WR("Content {0} = {1}", _st, st.Substring(n + 1).Invert());
            
            return n != -1 ? st.Substring(n + 1).Invert() : _st;
        }

        void _setContent(Database db, ObjectId id, string value)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (id.ObjectClass.DxfName == "TEXT")
                {
                    DBText txt = (DBText)tr.GetObject(id, OpenMode.ForWrite);
                    txt.TextString = value;

                }
                else if (id.ObjectClass.DxfName == "MTEXT")
                {
                    MText txt = (MText)tr.GetObject(id, OpenMode.ForWrite);
                    txt.Contents = value;
                }
                tr.Commit();
            }
        }
    }

    public class TextstepCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                stepForm frm = new stepForm();
                frm.Show();
                ACD.Focus();
            }
        }
    }
}

