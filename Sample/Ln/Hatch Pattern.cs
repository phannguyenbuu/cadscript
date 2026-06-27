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
    public class CommandForm : Form
    {
        Button btnRun, btnCancel;//, btnSchedule;
        ListBox lsContents;
        public bool OK, Cancel;
        public string[] Items;
        public event EventHandler ButtonClick = new EventHandler((o, e) => { });

        public CommandForm(IEnumerable<string> data)
        {
            this.Width = 600;
            this.Height = 480;
            this.TopMost = true;
            this.Text = "Command List";
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            //ACD.WR("OK1");
            lsContents = new ListBox()
            {
                Left = 10,
                Top = 10,
                Width = this.Width - 40,
                Height = this.Height - 100
            };

            Items = data.ToArray();
            lsContents.Items.Clear();

            for (int i = 0; i < data.Count(); i++)
            {
                string s = data.ElementAt(i);
                string key = s.filter("<>").First();
                string val = s.Replace(s.filter("<>").First(), "").Replace("<","").Replace(">", "").Trim();
                lsContents.Items.Add((i < 9 ? "0" : "") + (i + 1) + " - " + key + " - " + val);
            }

            btnRun = new Button()
            {
                Left = lsContents.Left,
                Top = lsContents.Bottom + 10,
                Width = lsContents.Width / 3 - 20,
                Height = 30,
                Text = "Run"
            };
            //ACD.WR("OK2");
            //btnSchedule = new Button()
            //{
            //    Left = btnRun.Right + 5,
            //    Top = btnRun.Top,
            //    Width = btnRun.Width,
            //    Height = btnRun.Height,
            //    Text = "Schedule"
            //};

            btnCancel = new Button()
            {
                Left = btnRun.Right + 5,
                Top = btnRun.Top,
                Width = btnRun.Width,
                Height = btnRun.Height,
                Text = "Cancel"
            };

            this.Controls.Add(lsContents);
            this.Controls.Add(btnRun);
            this.Controls.Add(btnCancel);
            //this.Controls.Add(btnSchedule);

            OK = false;
            Cancel = false;

            //ACD.WR("OK3");

            btnRun.Click += (o, e) =>
            {
                OK = true;
                ButtonClick(this, new EventArgs());
            };

            btnCancel.Click += (o, e) =>
            {
                Cancel = true;
                ButtonClick(this, new EventArgs());
            };
        }
    }
    public class HatchPatternCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                //string st = "";
                //foreach (string s in Autodesk.AutoCAD.Windows.Data.HatchPatterns.Instance.AllPatterns)
                //    st += s + "\r\n";

                //Clipboard.SetText(st);
                List<string> lsNotes = new List<string>();
                ObjectIdCollection selIds = ACD.GetSelection();
                foreach(ObjectId objId in selIds)
                    if(ACD.DB._isPolyline(objId)|| ACD.DB._isLine(objId))
                    {
                        //ACD.DB._setLayer(objId, "Defpoints");
                        string[] ar = ACD.DB.GetXNotes(objId);

                        if(ar.Length > 0)
                            lsNotes.Add(ar.ToTextStr("\r\n"));
                    }

                lsNotes = lsNotes.Distinct().OrderBy(s => s).ToList();

                if(lsNotes.Count > 0)
                {
                    CommandForm cmdFrm = new AcadScript.CommandForm(lsNotes);
                    cmdFrm.Show();
                    cmdFrm.ButtonClick += (o, e) =>
                    {
                        using (ACD.Lock())
                        {
                            if (cmdFrm.OK)
                            {
                                foreach (ObjectId objId in selIds)
                                    if (ACD.DB._isPolyline(objId) || ACD.DB._isLine(objId))
                                    {
                                        string[] ls = ACD.DB.GetXNotes(objId);

                                        if (ls.Length > 0)
                                        {
                                            ACD.DB._setLayer(objId, "Defpoints");
                                            string key = ls[0].filter("<>").First();
                                            ACD.DB.HatchKey(ACD.DB._getVertices(objId).ToCollection(), key);
                                        }
                                    }
                            }
                        }
                        ACD.Focus();

                        cmdFrm.Hide();
                    };
                }

                ACD.Focus();
            }
        }
    }
}

