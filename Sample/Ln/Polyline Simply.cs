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
    public class PolylineRForm : Form
    {
        ComboBox cbAngle, cbDimStyle;
        NumericUpDown toler;
        ObjectIdCollection selIds, resultIds;
        Button btnBuildGrid, btnSelectNew, btnSaveHyperlink, btnBounding;

        public PolylineRForm()
        {
            this.Width = 420;
            this.Height = 150;
            this.TopMost = true;
            Text = "Polyline Simply";
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;

            cbDimStyle = new ComboBox()
            {
                Left = this.Width / 2,
                Top = 10,
                Width = this.Width / 2 - 40,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };

            toler = new NumericUpDown()
            {
                Left = 10,
                Top = 10,
                Width = this.Width / 2 - 20,
                Maximum = 100,
                Minimum = 0,
                Value = 30
            };

            btnBuildGrid = new Button()
            {
                Left = 10,
                Top = toler.Top + toler.Height + 10,
                Width = this.Width / 4 - 20,
                Text = "Build Grid",
                Height = 30
            };
            
            btnSelectNew = new Button()
            {
                Left = this.Width / 4,
                Top = toler.Top + toler.Height + 10,
                Width = this.Width / 4 - 20,
                Text = "Reset",
                Height = 30
            };

            btnSaveHyperlink = new Button()
            {
                Left = this.Width * 2 / 4,
                Top = toler.Top + toler.Height + 10,
                Width = this.Width / 4 - 20,
                Text = "Hyperlink",
                Height = 30
            };

            btnBounding = new Button()
            {
                Left = this.Width * 3 / 4,
                Top = toler.Top + toler.Height + 10,
                Width = this.Width / 4 - 20,
                Text = "Bounding",
                Height = 30
            };

            cbDimStyle.Items.Clear();
            cbDimStyle.Items.AddRange(new string[] { "1 chain", "2 chains", "3 chains" });
            cbDimStyle.SelectedIndex = 0;

            this.Controls.Add(btnBounding);
            this.Controls.Add(cbDimStyle);
            this.Controls.Add(toler);
            this.Controls.Add(btnBuildGrid);
            this.Controls.Add(btnSelectNew);
            this.Controls.Add(btnSaveHyperlink);

            btnSaveHyperlink.Click += (o, e) =>
            {
                using (ACD.Lock())
                {
                    if (selIds != null && selIds.Count == 0)
                        selIds = ACD.GetSelection()._filterDXF("LWPOLYLINE", "HATCH", "LINE", "AEC_WALL");

                    if (selIds.Count > 0)
                        foreach(ObjectId id in selIds)
                            ACD.DB.SetXNotes(id, "Simply=" 
                                + toler.Value.ToString() + "|Scale=" + ACD.DB.Cannoscale.Name);
                }
            };

            btnBounding.Click += (o, e) =>
            {
                using (ACD.Lock())
                {
                    ObjectIdCollection selIds = ACD.GetSelection();
                    
                    foreach(ObjectId id in selIds)
                    {
                        pPos[] bb = ACD.DB._getBound(id);
                        pPos ct = bb.CenterPoint();
                        pPos sz = bb.Size();

                        ACD.DB.DrawPolyline(bb.Rect(), true, "LAYER=DEFPOINTS");
                        ACD.DB.SetXNotes(id, "Simply=-1");

                        ACD.DB.DrawPolyline(new pPos[] { ct - new pPos(sz.X, 0), ct + new pPos(sz.X, 0) }, false, "LAYER=A-Hidden");
                        ACD.DB.DrawPolyline(new pPos[] { ct - new pPos(0, sz.Y), ct + new pPos(0, sz.Y) }, false, "LAYER=A-Hidden");
                    }
                }
            };

            btnSelectNew.Click += (o, e) =>
            {
                using (ACD.Lock())
                {
                    resultIds = new ObjectIdCollection();
                    selIds = new ObjectIdCollection();
                }
            };

            btnBuildGrid.Click += (o, e) =>
            {
                using (ACD.Lock())
                {
                    selIds = ACD.GetSelection();

                    if (selIds.Count > 0)
                        _buildGrid(selIds);

                    ACD.Focus();
                }
            };

            selIds = new ObjectIdCollection();
            resultIds = new ObjectIdCollection();

            toler.ValueChanged += (o, e) =>
            {
                using (ACD.Lock())
                {
                    if (selIds != null && selIds.Count == 0)
                        selIds = ACD.GetSelection()._filterDXF("LWPOLYLINE", "HATCH", "LINE", "AEC_WALL");

                    if (selIds.Count > 0)
                    {
                        if (resultIds != null && resultIds.Count > 0)
                            try
                            {
                                ACD.DB.EraseObjects(resultIds);
                            }
                            catch (System.Exception ex)
                            {

                            }

                        resultIds = new ObjectIdCollection();
                        string layer = ACD.DB._getLayer(selIds.First());
                        pPos[] boundary = ACD.DB._getBound(selIds);
                        //ACD.DB.DrawPolyline(boundary.Rect(), true);

                        double ang = (double)toler.Value;

                        foreach (ObjectId id in selIds)
                        {
                            pPos[] ls = ACD.DB._getVertices(id).Straighten(ACD.DB._isPolylineClosed(id), ang);

                            if (ls.Length > 0)
                                resultIds.Add(ACD.DB.DrawPolyline(ls, true, "LAYER=DEFPOINTS"));

                        }

                        foreach(ObjectId id in selIds)
                            ACD.DB.SetXNotes(id, "Simply="
                                + toler.Value.ToString() + "|Scale=" + ACD.DB.Cannoscale.Name);
                    }
                }
                ACD.Focus();
            };
        }

        void _buildGrid(ObjectIdCollection selIds)
        {
            using (ACD.Lock())
            {
                PosCollection pls = new PosCollection();
                string current_anno = ACD.DB.Cannoscale.Name;

                double round = 1;
                double sc = current_anno.filter(":").Last().ToNumber();
                if (sc >= 50)
                    round = 50;
                else if (sc >= 20)
                    round = 5;
                else
                    round = 1;

                ObjectIdCollection ids = selIds.ToList().All(id => !ACD.DB._isWall(id)) ?
                    selIds._filterDXF("LWPOLYLINE") : selIds.ToList().Where(id => ACD.DB._isWall(id)).ToCollection();
                

                foreach (ObjectId id in ids)
                    //if(ACD.DB._isWall(id))
                    { 
                        if (ACD.DB._isPolyline(id))
                        {
                            pPos[] ls = ACD.DB._getVertices(id);

                            if (ls.Length() >= 100)
                            {
                                string hyperlink = ACD.DB._getHyperlink(id);

                                double simply = hyperlink._prop("Simply").ToNumber();
                                string anno = hyperlink._prop("Scale");

                                if (simply == -1)
                                    ls = ls.Boundary();
                                else if (simply > 0)
                                {
                                    if (anno == current_anno) ls = ls.Straighten(ACD.DB._isPolylineClosed(id), simply);
                                    //ACD.DB.DrawPolyline(ls, ACD.DB._isPolylineClosed(id), "LAYER=DEFPOINTS");
                                }

                                pls.Add(ls);
                            }

                        }
                        else if(ACD.DB._isBlock(id) && ACD.DB._getIdName(id).StartsWith("grid"))
                        {
                            PosCollection tmps = ACD.DB.GetIdPts(id);

                            ACD.WR("tmps {0},{1}", tmps.Closed.ToTextBool(","), tmps.SelfIntersect.Length);

                            tmps = DE.NumericArray(0, tmps.Count - 1).Where(n 
                                => !tmps.Closed[n]).Select(n => tmps[n]).ToCollectionSameClosed();

                        
                            pls.Add(tmps.SelfIntersect);
                        }else
                            pls.AddRange(ACD.DB.GetIdPts(id));

                        pls.Add(ACD.DB._getBound(id));
                    }

                    //ObjectIdCollection gridIds = ACD.DB.BuildLayout2DGrid(pls.AllPoints, 
                    //    round, "Defpoints", 
                    //    "Scale=" + current_anno + "|Dim=" + cbDimStyle.SelectedItem);

                    
                }

            ACD.Focus();
        }
    }

    public class PolylineRPointsCLS
    {
        static void _buildGrid(ObjectIdCollection selIds)
        {
            using (ACD.Lock())
            {
                PosCollection pls = new PosCollection();
                string current_anno = ACD.DB.Cannoscale.Name;

                double round = 1;
                double sc = current_anno.filter(":").Last().ToNumber();
                if (sc >= 50)
                    round = 50;
                else if (sc >= 20)
                    round = 5;
                else
                    round = 1;

                ObjectIdCollection ids = selIds._filterDXF("LWPOLYLINE","AEC_WALL");


                foreach (ObjectId id in ids)
                //if(ACD.DB._isWall(id))
                {
                    if (ACD.DB._isPolyline(id))
                    {
                        pPos[] ls = ACD.DB._getVertices(id);

                        if (ls.Length() >= 100)
                        {
                            string hyperlink = ACD.DB._getHyperlink(id);

                            double simply = hyperlink._prop("Simply").ToNumber();
                            string anno = hyperlink._prop("Scale");

                            if (simply == -1)
                                ls = ls.Boundary();
                            else if (simply > 0)
                            {
                                if (anno == current_anno)
                                    ls = ls.Straighten(ACD.DB._isPolylineClosed(id), simply);
                                //ACD.DB.DrawPolyline(ls, ACD.DB._isPolylineClosed(id), "LAYER=DEFPOINTS");
                            }

                            pls.Add(ls);
                        }

                    }
                    else if (ACD.DB._isBlock(id) && ACD.DB._getIdName(id).StartsWith("grid"))
                    {
                        PosCollection tmps = ACD.DB.GetIdPts(id);

                        ACD.WR("tmps {0},{1}", tmps.Closed.ToTextBool(","), tmps.SelfIntersect.Length);

                        tmps = DE.NumericArray(0, tmps.Count - 1).Where(n
                            => !tmps.Closed[n]).Select(n => tmps[n]).ToCollectionSameClosed();


                        pls.Add(tmps.SelfIntersect);
                    }
                    else
                        pls.AddRange(ACD.DB.GetIdPts(id));

                    pls.Add(ACD.DB._getBound(id));
                }

                //ObjectIdCollection gridIds = ACD.DB.BuildLayout2DGrid(pls.AllPoints,
                //    round, "Defpoints",
                //    "Scale=" + current_anno); 
                //+ "|Dim=" + cbDimStyle.SelectedItem);


            }

            ACD.Focus();
        }
        public static void Main(string[] args)
        {
            //PolylineRForm frm = new PolylineRForm();
            //frm.Show();

            _buildGrid(ACD.GetSelection());
        }
    }
}

