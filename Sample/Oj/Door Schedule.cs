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
    public class DoorScheduleForm : Form
    {
        Button btnPlot, btnCancel, btnSchedule;
        ListBox lsSchedule;
        ComboBox lsTitleBlock, lsPrefix, lsListTitle;

        public bool OK, Cancel;
        public string[] Items;
        public string[] Prefixes;
        public event EventHandler ButtonClick = new EventHandler((o, e) => { });

        public bool WithSchedule
        {
            get
            {
                return true;
            }
        }

        public string TitleBlockOverlay
        {
            get
            {
                return lsTitleBlock.SelectedIndex != -1 ? lsTitleBlock.Items[lsTitleBlock.SelectedIndex].ToString() : null;
            }
        }

        public string[] TitleDatas
        {
            get
            {
                return DE.NumericArray(0, Items.Length - 1).Select(i => "#" + i + "=" + Items[i]).ToArray();
            }
        }

        public string Prefix
        {
            get
            {
                return lsPrefix.Items[lsPrefix.SelectedIndex].ToString();
            }
        }


        public string ListTitle
        {
            get
            {
                return lsListTitle.Items[lsListTitle.SelectedIndex].ToString();
            }
        }

        void _showInfors()
        {
            List<string> str = new List<string>();
            List<string> names = new List<string>();

            foreach (pPos[] info in door_infos)
            {
                string s = info[0].Content;
                names.Add("STYLE=" + s._prop("STYLE") + "|WIDTH=" + s._prop("WIDTH") + "|HEIGHT=" + s._prop("HEIGHT"));
            }

            names = names.Distinct().OrderBy(s => s).ToList();
            int index = 1;
            string save = "";

            foreach (string ss in names)
            {
                if (ss._firstProp() == save)
                    index++;
                else
                    index = 1;

                List<string> countlist = new List<string>();
                string id = "";
                foreach (pPos[] info in door_infos)
                {
                    string s = info[0].Content;
                    string name = "STYLE=" + s._prop("STYLE") + "|WIDTH=" + s._prop("WIDTH") + "|HEIGHT=" + s._prop("HEIGHT");
                    if (name == ss)
                    {
                        countlist.Add(s);
                        id += s._prop("ID") + ",";
                    }
                }

                str.Add(ss._prop("STYLE") + index + "___" + ss._prop("WIDTH") + "___" + ss._prop("HEIGHT") + "____" + countlist.Count + "[" + id + "]");
                save = ss._firstProp();
            }

            lsSchedule.Items.Clear();
            lsSchedule.Items.AddRange(str.ToArray());
        }

        public void UpdateDoorInfos()
        {
            door_infos = new PosCollection();
            foreach (pPos[] zone in zones)
            {
                ACD.DB.GetEntities(zone, EN_SELECT.AC_DXF, "INSERT", "AEC_DOOR", "AEC_WINDOW");
                ObjectIdCollection door_blocks = IR.SelectedIds.ToList().Where(id => ACD.DB._isDoor(id)).ToCollection();

                foreach (ObjectId id in door_blocks)
                {
                    List<pPos> ls = new List<pPos>();
                    ls.Add(ACD.DB._getPoint(id));
                    ls.AddRange(ACD.DB._getBound(id));
                    ls[0].Content = "TYPE=" + id.ObjectClass.DxfName + "|STYLE="
                        + ACD.DB._getIdName(id) + "|ID=" + id.Handle.Value
                        + "|WIDTH=" + ACD.DB._getDoorWidth(id) + "|HEIGHT=" + ACD.DB._getDoorHeight(id);
                    door_infos.Add(ls.ToArray());
                }

                door_blocks = IR.SelectedIds.ToList().Where(id
                    => ACD.DB._isBlock(id) && ACD.DB._getIdName(id).Upper().Contains("DOORTAG")).ToCollection();

                foreach (ObjectId id in door_blocks)
                {
                    List<pPos> ls = new List<pPos>();
                    ls.Add(ACD.DB._getPoint(id));
                    ls.AddRange(ACD.DB._getBound(id));
                    ls[0].Content = "TYPE=" + id.ObjectClass.DxfName + "|NAME="
                        + ACD.DB.GetBlockAtt(id, "NAME") + "|" + ACD.DB.GetXNotes(id) + "|ID=" + id.Handle.Value;
                    door_infos.Add(ls.ToArray());
                }
            }

            _showInfors();
        }

        PosCollection zones = new PosCollection();
        PosCollection door_infos = new PosCollection();

        public DoorScheduleForm(PosCollection _zones)
        {
            zones = _zones;

            this.Width = 600;
            this.Height = 600;
            this.TopMost = true;
            this.Text = "DrawingList";
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            //ACD.WR("OK1");
            lsSchedule = new ListBox()
            {
                Left = 10,
                Top = 10,
                Width = this.Width - 40,
                Height = this.Height - 150
            };

            //Items = data.ToArray();
            //lsSchedule.Items.Clear();

            //for (int i = 0; i < data.Count(); i++)
            //    lsSchedule.Items.Add((i < 9 ? "0" : "") + (i + 1) + " - " + data.ElementAt(i));

            lsPrefix = new ComboBox()
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Top = lsSchedule.Bottom + 10,
                Left = lsSchedule.Left,
                Width = lsSchedule.Width / 3 - 5
            };

            lsPrefix.Items.Clear();
            lsPrefix.Items.AddRange("A,B,C,D,E,F,G,H,I,M".filter());
            lsPrefix.SelectedIndex = 0;

            lsTitleBlock = new ComboBox()
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Top = lsPrefix.Top,
                Left = lsPrefix.Left + lsPrefix.Width + 10,
                Width = lsPrefix.Width
            };

            lsTitleBlock.Items.Clear();
            //lsTitleBlock.Items.Add("<None>");
            lsTitleBlock.Items.AddRange(Directory.GetFiles(DE.CADLIB + @"PDF\", "*.pdf")
                .Where(f => Path.GetFileNameWithoutExtension(f).st_("TITLE"))
                .Select(f => Path.GetFileNameWithoutExtension(f)).ToArray());
            lsTitleBlock.SelectedIndex = 0;

            lsListTitle = new ComboBox()
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Top = lsPrefix.Top,
                Left = lsTitleBlock.Left + lsTitleBlock.Width + 10,
                Width = lsPrefix.Width
            };

            lsListTitle.Items.Clear();
            //string s = pString.INI_String("PLOT_TITLE_DRAWING_LIST_TITLE");
            //if (!s.empty())
            lsListTitle.Items.AddRange(GP.PLOT_TITLE_DRAWING_LIST_TITLE);
            lsListTitle.SelectedIndex = 0;

            btnPlot = new Button()
            {
                Left = lsPrefix.Left,
                Top = lsPrefix.Bottom + 10,
                Width = lsPrefix.Width,
                Text = "Plot"
            };

            btnSchedule = new Button()
            {
                Left = lsTitleBlock.Left,
                Top = btnPlot.Top,
                Width = lsPrefix.Width,
                Height = btnPlot.Height,
                Text = "Schedule"
            };

            btnCancel = new Button()
            {
                Left = lsListTitle.Left,
                Top = btnPlot.Top,
                Width = lsPrefix.Width,
                Height = btnPlot.Height,
                Text = "Cancel"
            };

            Controls.AddRange(new Control[] { lsPrefix, lsSchedule, btnPlot, btnCancel, btnSchedule, lsTitleBlock, lsListTitle });

            OK = false;
            Cancel = false;

            //ACD.WR("OK3");

            btnPlot.Click += (o, e) =>
            {
                OK = true;
                ButtonClick(this, new EventArgs());
            };

            btnCancel.Click += (o, e) =>
            {
                Cancel = true;
                ButtonClick(this, new EventArgs());
            };

            btnSchedule.Click += (o, e) =>
            {
                UpdateDoorInfos();
            };

            lsSchedule.Click += (o, e) =>
            {
                string content = lsSchedule.SelectedItem.ToString();
                string id = content.filter("[]")[1];
                string name = content.Replace("___", "|").filter("|")[0];

                ObjectIdCollection ids = new ObjectIdCollection();
                foreach (pPos[] info in door_infos)
                {
                    if (name.StartsWith(info[0].Content._prop("STYLE")))
                        ids.AddRange(ACD.DB.strToObjectId(id));
                }
                ACD.WR("Name {0} Selection {1}",name, ids.Count);
                ACD.ED.SetImpliedSelection(ids.ToArray());
                ACD.Focus();
            };

            UpdateDoorInfos();
        }

        public string GetItemPrefix(string start_prefix, int item_index)
        {
            int index = 0;
            string next_prefix = start_prefix;

            for (int i = 0; i <= item_index; i++)
            {
                string st = Items[i];

                if (st.ToUpper().Contains("PHẦN BẢN VẼ"))
                {
                    start_prefix = next_prefix;
                    next_prefix = ((char)((int)(char)start_prefix[0] + 1)).ToString();

                    index = 0;
                }
                else
                    index++;
            }

            return start_prefix + "." + (index <= 99 ? "0" : "")
                + (index <= 9 ? "0" : "") + index;
        }
    }

    public class DoorPointCLS
    {
        static pPos _blockNote(ObjectId id, ObjectIdCollection doorIds)
        {
            pPos res = ACD.DB._getPoint(id);
            res.Content = ACD.DB.GetBlockAtt(id);

            ObjectIdCollection dIds = doorIds.ToList().OrderBy(i 
                => ACD.DB._getBound(i).CenterPoint().DistanceTo(res)).ToCollection();

            if(dIds.Count > 0)
            {
                ObjectId dId = dIds.First();
                res.Content += "|ID=" + dId.ToString();
            }

            return res;
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                List<pPos> pts = new List<pPos>();
                PosCollection door_infos = new PosCollection();

                while (true)
                {
                    ACD.WR("Pick zone to count door...");
                    pPos pt = ACD.GetPoint();

                    if (pt == null)
                        break;
                    else
                        pts.Add(pt);
                }

                PosCollection zones = new PosCollection();

                foreach (pPos pt in pts)
                {
                    pPos[] zone = ACD.DB.GetDrawingZone(pt);
                    if (zone == null)
                        ACD.WR("No recognize zone... processing failed");
                    else
                        zones.Add(zone);
                }

                DoorScheduleForm frm = new DoorScheduleForm(zones);

                frm.ButtonClick += (o, e) =>
                {
                };

                frm.Show();

                ACD.Focus();
            }
        }
    }
}

