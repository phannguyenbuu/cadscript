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
    public class DrawingModeForm:Form
    {
        ComboBox cbMode, cbHatch;
        Button btnUndo;
        string archtick = "_ARCHTICK";
        pPos[] bounding;
        double extend_bounding = 0;
        PosCollection grid_points;

        string[] filterLayers(string[] layers, string keys)
        {
            List<string> res = new List<string>();

            foreach (string k in keys.filter(","))
                foreach (string layername in layers)
                    if ((k.Length == 1 && layername.st_(k.Upper()))
                        || (k.Length > 1 && layername.Upper().Contains(k.Upper())))
                        res.Add(layername);

            return res.ToArray();
        }

        public DrawingModeForm()
        {
            extend_bounding = pString.INI_String("EXTEND_DIM_POINTS").ToNumber();
            this.Width = 400;
            this.Height = 120;
            this.TopMost = true;
            Text = "Drawing Mode";
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;

            cbMode = new ComboBox()
            {
                Left = 10,
                Top = 10,
                Width = this.Width - 100,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            btnUndo = new Button()
            {
                Left = cbMode.Width + cbMode.Left + 10,
                Top = 10,
                Width = 40,
                Height = 20,
                Text = "Undo"
            };

            btnUndo.Click += (o, e) =>
            {
                if (resultIds != null)
                {
                    ACD.DB.GetEntities(ACD.bRect, EN_SELECT.AC_DXF, "AEC_DIMENSION_GROUP");
                    //resultIds = IR.SelectedIds.ToList().Where(id => !saveIds.Contains(id)).ToCollection();
                    //ACD.WR("Result {0} dim groups", resultIds.Count);
                    ACD.DB.EraseObjects(IR.SelectedIds);
                    resultIds = null;
                }
            };

            cbHatch = new ComboBox()
            {
                Left = 10,
                Top = 10 + cbMode.Height + 10,
                Width = this.Width - 40,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };

            //ACD.WR("OK1");

            this.Controls.Add(cbMode);
            this.Controls.Add(cbHatch);
            this.Controls.Add(btnUndo);
            //ACD.WR("OK2");
            cbMode.Items.Clear();
            cbMode.Items.AddRange(File.ReadAllLines(DE.INI_FILE)
                .Where(s => s.StartsWith("DMODE")).Select(s => s._firstPropName()).ToArray());

            cbHatch.Items.Clear();
            cbHatch.Items.AddRange(File.ReadAllLines(DE.CADLIB_CONSTRUCT + @"\Hatch.txt")
                .GetChapters().Keys.OrderBy(s => s).ToArray());

            if (cbHatch.Items.Count > 0)
                cbHatch.SelectedIndex = 0;

            cbMode.SelectedIndexChanged += (o, e) =>
            {
                using (ACD.Lock())
                {
                    string[] str = File.ReadAllLines(DE.INI_FILE);
                    var dict = str.GetChapters();

                    string[] layers = ACD.DB.ListLayers();

                    foreach (var itm in dict)
                        if (itm.Key.Upper() == "LAYER_COLOR")
                        {
                            foreach (string st in itm.Value)
                            {
                                string layer = st._firstPropName();
                                if (layers.Contains(layer))
                                    ACD.DB._setLayerColor(layer, (short)st._firstProp().ToNumber());
                            }
                        }

                    if (cbMode.SelectedItem != null)
                    {
                        string key = cbMode.SelectedItem.ToString();
                        string[] prams = pString.INI_Params(key);

                        if (prams.Length > 0)
                        {
                            //string[] layers = ACD.DB.ListLayers();
                            string pram = prams.First();

                            foreach (string k in pram._allPropNames())
                            {
                                string val = pram._prop(k);

                                foreach (string layername in filterLayers(layers, val))
                                    if (key == "LAYON")
                                        ACD.DB._setLayerState(layername, true);
                                    else if (key == "LAYOFF")
                                        ACD.DB._setLayerState(layername, false);
                                    else if (key == "LAYGREY")
                                        ACD.DB._setLayerColor(layername, 8);
                            }
                        }

                        if(key == "DMODE_DIM_POINTS")
                        {
                            GenerateDimPoints();
                        }
                    }

                    ACD.Focus();
                }
            };

            cbHatch.SelectedIndexChanged += (o, e) =>
            {
                using (ACD.Lock())
                {
                    string f = DE.CADLIB_CONSTRUCT + @"\Hatch.txt";
                    if (File.Exists(f))
                    {
                        string[] prams = File.ReadAllLines(f);

                        if (prams != null && prams.Length > 0)
                        {
                            pPos basept = new pPos(0, 0); // ACD.GetPoint();

                            ObjectIdCollection selIds = ACD.GetSelection();

                            string val = cbHatch.SelectedItem.ToString();
                            var dict = prams.GetChapters();

                            //Console.WriteLine("[KEY]{0}", val);

                            if (dict.ContainsKey(val))
                            {
                                string[] str = dict[val];

                                if (str != null && str.Length > 0 && basept != null)
                                {
                                    List<string> contents = new List<string>();

                                    foreach (ObjectId id in selIds)
                                        if (ACD.DB._isPolyline(id) || ACD.DB._isLine(id))
                                        {
                                            foreach (string st in str)
                                                if (st.StartsWith("#"))
                                                {
                                                    contents.Add(st._setprop("VERTS",
                                                        ACD.DB._getVertices(id).ToText(ACD.DB._isPolylineClosed(id))) + ";");
                                                    //ACD.WR("[LINE]{0}GP.DEF_LAYER_TEXT{1}", st, st._setprop("VERTS",
                                                    //ACD.DB._getVertices(id).ToText(ACD.DB._isPolylineClosed(id))));
                                                }
                                        }

                                    if (str._props("#DELETE_SOURCE").ToBool())
                                        ACD.DB.EraseObjects(selIds);

                                    ACD.WR("[RESULT]\r\n{0}\r\n[/RESULT]", contents.ToTextStr("\r\n"));
                                    //ACD.DB.DrawData(contents, null);
                                }
                            }
                        }
                    }
                }
                ACD.Focus();
            };

                //ACD.WR("OK3");
        }

        List<int[]> intersectInAdj(PosCollection pls, int[] indexes)
        {
            PosCollection new_pls = indexes.Select(n => pls[n]).ToCollectionSameClosed();
            List<int>[] adjs = new_pls.Select(ls => new List<int>()).ToArray();

            for (int i = 0; i < new_pls.Count - 1; i++)
                for (int j = i + 1; j < new_pls.Count; j++)
                    if (new PosCollection() { new_pls[i], new_pls[j] }.Intersect().Length > 0)
                        adjs.AdjAddSegment(i, j);

            List<int[]> adj_result = adjs.DetectAdj();

            return adj_result.Select(ls => ls.Select(n => indexes[n]).ToArray()).ToList();
        }

        PosCollection GroupPointCloud(IEnumerable<pPos> pts, IEnumerable<pPos> bb, int div_x, int div_y)
        {
            pPos p1 = bb.First();
            pPos p2 = bb.Last();

            double unit_size_x = (p2.X - p1.X) / div_x;
            double unit_size_y = (p2.Y - p1.Y) / div_y;

            int a = (int)Math.Ceiling((p2[0] - p1[0]) / unit_size_x);
            int b = (int)Math.Ceiling((p2[1] - p1[1]) / unit_size_y);

            List<int>[] cells = DE.NumericArray(0, (a + 1) * (b + 1))
                .Select(n => new List<int>()).ToArray();
            List<int>[] adjs = cells.Select(ls => new List<int>()).ToArray();

            //ACD.WR("Cells_1 {0} A {1} B {2} PTS {3}", cells.Length, a, b, pts.Count());

            for (int i = 0; i < pts.Count(); i++)
            {
                pPos p = pts.ElementAt(i);

                int index = ((int)Math.Abs(Math.Floor((p.X - p1.X) / unit_size_x))) * b
                    + (int)Math.Abs(Math.Floor((p.Y - p1.Y) / unit_size_y));

                if (index < cells.Length)
                    cells[index].Add(i);
                else
                {
                    ACD.WR("Over {0}", index);
                    break;
                }
            }

            //ACD.WR("Cells_2");

            for (int i = 0; i < cells.Length - 1; i++)
                if (cells[i].Count > 0)
                    for (int j = i + 1; j < cells.Length; j++)
                        if (cells[j].Count > 0)
                        {
                            int a1 = (int)(i / a), a2 = (int)(j / a);
                            int b1 = (int)(i % b), b2 = (int)(j % b);

                            int r1 = Math.Abs(a1 - a2), r2 = Math.Abs(b1 - b2);

                            if ((r1 == 0 && r2 <= 1) || (r2 == 0 && r1 <= 1))
                                adjs.AdjAddSegment(i, j);
                        }

            //ACD.WR("Cells_3");
            List<int[]> adj_result = adjs.DetectAdj();
            //ACD.WR("Cells_4");
            PosCollection res = new PosCollection();

            foreach (int[] adj in adj_result)
            {
                List<pPos> ls = new List<pPos>();
                foreach (int n in adj)
                    ls.AddRange(cells[n].Select(m => pts.ElementAt(m)));

                if (ls.Count > 0)
                    res.Add(ls.ToArray());
            }

            return res;
        }

        pPos[] GetIdPoints(ObjectId id)
        {
            pPos[] res = null;
            if (ACD.DB._isPoint(id))
                res = new pPos[] { ACD.DB._getPoint(id) };
            else if (ACD.DB._isWall(id))
            {
                ObjectId lwpId = ACD.DB.GetWallShape(id,0);
                res = ACD.DB._getVertices(lwpId, 16);
                ACD.DB.EraseObject(lwpId);
            }
            else
                res = ACD.DB._getVertices(id);
            return res;
        }
        List<double[]> grid_xys = null;

        void GetDimPoints(ObjectIdCollection selIds, double round, double min)
        {
            PosCollection pls = new PosCollection();
            PosCollection bbs = selIds.ToList().Select(id => ACD.DB._getBound(id)).ToCollectionSameClosed();
            //ACD.DB.DrawPolyline(bounding.Rect(), true);
            //ACD.WR("DOK1");

            List<int>[] adjs = bbs.Select(ls => new List<int>()).ToArray();
            //List<pPos[]> bbs = pls.Select(ls => ls.Boundary()).ToList();

            for (int i = 0; i < bbs.Count - 1; i++)
                for (int j = i + 1; j < bbs.Count; j++)
                    if (bbs[i].IntersectBounding(bbs[j]))
                        adjs.AdjAddSegment(i, j);

            //ACD.WR("DOK2");
            //ACD.WR("Time 2 {0}", DE.watch.ElapsedMilliseconds / 1000);
            List<int[]> adj_result = adjs.DetectAdj();
            //int[] indx = DE.NumericArray(0, selIds.Count - 1);
            //ACD.WR("adj_result:{0}", adj_result.Count);
            //ACD.WR("DOK3");
            foreach (int[] adj in adj_result)
            {
                //ACD.WR("adj_result:{0}", adj.ToText());
                ObjectIdCollection ids = adj.Select(n => selIds[n]).ToCollection();
                PosCollection sbbs = adj.Select(n => bbs[n]).ToCollectionSameClosed();

                //ACD.DB.DrawPolyline(sbbs.Boundary.Rect(), true);
                CollectAdjRegions(ids, sbbs);
            }
        }

        List<pPos> points;
        bool show_points = false;
        PosCollection allpoints;

        void _getGridData(ObjectIdCollection ids)
        {
            //ACD.WR("DOK4.1");
            ObjectIdCollection gridIds = ids.ToList()
                .Where(id => ACD.DB._isBlock(id)
                && ACD.DB._getIdName(id).st_("GRID")).ToCollection();
            //ACD.WR("DOK4.2");
            grid_points = new PosCollection();
            grid_points.Closed = new bool[0];
            bounding = null;
            //ACD.WR("DOK5");

            if (gridIds.Count > 0)
            {
                foreach (ObjectId id in gridIds)
                {
                    ObjectIdCollection subIds = ACD.DB.ExplodeEntity(id);

                    foreach (ObjectId subId in subIds)
                        if ((ACD.DB._isLine(subId) || ACD.DB._isPolyline(subId))
                            && ACD.DB._getLength(subId) >= 5000)
                        {
                            grid_points.Add(ACD.DB._getVertices(subId));
                            grid_points.Closed = grid_points.Closed.Add(ACD.DB._isPolylineClosed(id));
                        }

                    ACD.DB.EraseObjects(subIds);
                    ids.Remove(id);
                }
            }
            //ACD.WR("DOK6");
            grid_xys = new List<double[]> { new double[0], new double[0] };
            if (grid_points.Count > 0)
            {
                grid_xys = grid_points.Intersect().ExtractPtsXY(50);
                bounding = new pPos[] {new pPos(grid_xys[0].Min(), grid_xys[1].Min()),
                    new pPos(grid_xys[0].Max(), grid_xys[1].Max())};
            }
            else
                bounding = ACD.DB._getBound(ids);

        }

        void CollectAdjRegions(ObjectIdCollection ids, PosCollection ids_bounds)
        {
            int divX = 20, divY = 10;
            pPos[] bb = ids_bounds.Boundary; // adj.Select(n => ids_bounds[n]).ToCollection().Boundary;

            _getGridData(ids);

            allpoints = ids.ToList().Select(id => GetIdPoints(id)).ToCollectionSameClosed();
            points = allpoints.AllPoints.ToList();

            //ACD.WR("DOK4");
            if (grid_points.Count > 0)
                points.AddRange(grid_points.Intersect());

            //ACD.WR("DOK7");
            //ACD.WR("OK1");
            //List<pPos> pts = bb.ToList();
            //ACD.DB.DrawPolyline(bb.Rect(), true, "LAYER=G-Text|LWIDTH=200");
            //ACD.WR("A {0} B {1}", Math.Abs(bb[1].X - bb[0].X), Math.Abs(bb[1].Y - bb[0].Y));
            //ACD.WR("Time 3 {0}", DE.watch.ElapsedMilliseconds / 1000);
            if (Math.Abs(bb[1].X - bb[0].X) > 500 && Math.Abs(bb[1].Y - bb[0].Y) > 500)
            {
                //PosCollection pointclouds = adj.Select(n => allpoints[n]).ToCollection();
                //pointclouds.Add(pointclouds.Intersect(true));
                //ACD.WR("DOK8");

                PosCollection pointclouds = GroupPointCloud(points, bb, divX, divY);
                //ACD.WR("adj_result_points:{0}", points.Count);
                //ACD.WR("Time 4 {0} Points {1}", DE.watch.ElapsedMilliseconds / 1000, points.AllPoints.Length);

                //ACD.DB.DrawCircle( points.AllPoints,200, "LAYER=G-DimPointNode");
                
                //ACD.WR("DOK7");
                PosCollection regions = new PosCollection();

                ct = bounding.CenterPoint();
                pPos sz = bounding.Size() / 5;
                double d = Math.Min(sz.X / 2, sz.Y / 2);

                regions.Add(new pPos[] { bounding[0], new pPos(bounding[1].X, bounding[0].Y + d)});
                regions.Add(new pPos[] { bounding[0], new pPos(bounding[0].X + d, bounding[1].Y) });
                regions.Add(new pPos[] { bounding[1],  new pPos(bounding[0].X, bounding[1].Y - d) });
                regions.Add(new pPos[] { bounding[1], new pPos(bounding[1].X - d, bounding[0].Y) });

                //ACD.DB.DrawCircle(points, 100);
                double[] w1 = new double[] { 0, 50, 0 };
                double[] w2 = new double[] { 0, 0, 0 };

                for (int i = 0; i < regions.Count; i++)
                {
                    string blockname = ACD.DB.uniqueBlockName("Ruler" + i);
                    BuildRuler(regions[i].Boundary(), i == 0 || i == 2 ? 0 : 1, regions[i].First());
                }

                pPos[] cts = pointclouds.Select(ls => ls.CenterPoint()).ToArray();
                //ACD.WR("DOK8");

                List<double[]> tmps = cts.ExtractPtsXY(50, d);
                ruler_h = 500;

                List<double[]> xys = new List<double[]>();

                foreach (double[] ds in tmps)
                {
                    List<double> ls = new List<double>();
                    for (int i = 0; i < ds.Length - 1; i++)
                    {
                        ls.Add(ds[i]);
                        ls.Add((ds[i + 1] + ds[i]) / 2);
                        ls.Add(ds[i + 1]);
                    }
                    xys.Add(ls.ToArray());
                }

                for (int axis = 0; axis < 2; axis++)
                {
                    int nex = (axis + 1) % 2;
                    double[] ds = xys[axis];

                    for (int i = 0; i < ds.Length - 1; i++)
                    {
                        //double v = (ds[i + 1] + ds[i])/ 2;
                        //if (ds[i] - bounding[0][axis] > d && bounding[1][axis] - ds[i] > d)
                        //{
                        pPos p1 = new pPos(0, 0);
                        p1[axis] = ds[i];
                        p1[nex] = bounding[0][nex];

                        pPos p2 = new pPos(0, 0);
                        p2[axis] = ds[i + 1];
                        p2[nex] = bounding[1][nex];

                        string blockname = ACD.DB.uniqueBlockName("Ruler" + i);
                        BuildRuler(new pPos[] { p1, p2 }, nex, (p1 + p2) / 2);
                        //}
                    }
                }

                if(grid_xys[0].Length > 0)
                    BuildGridDims();
            }
        }

        pPos _posByV(int axis, double v, double s)
        {
            pPos p = new pPos(0, 0);
            p[axis] = v;
            p[(axis + 1) % 2] = s;
            return p;
        }

        void AddDimChain(pPos[] pts, int axis, pPos dim_point)
        {
            string cmd = "_DIMADD P ";

            foreach (pPos p in pts)
                cmd += p + ",0 ";
            cmd += " R " + (axis == 0 ? 0 : 90) + " " + dim_point + ",0 ";
            ACD.WR("CMD {0}", cmd);
            //String.Format("_DIMADD P 3,3,0 300,3,0 1300,3,0 9000,3,0  R 0 0,5000,0 ");
            ACD.DOC.SendStringToExecute(cmd, true, false, false);
        }

        double ruler_h = 500;
        pPos ct;
        void BuildGridDims()
        {
            for (int axis = 0; axis < 2; axis++)
            {
                int nex = (axis + 1) % 2;
                double[] ds = grid_xys[axis];
                pPos[] ls1 = new pPos[grid_xys[axis].Length];
                pPos[] ls2 = new pPos[grid_xys[axis].Length];

                for (int i = 0; i < grid_xys[axis].Length; i++)
                {
                    ls1[i] = new pPos(0, 0);
                    ls1[i][axis] = grid_xys[axis][i];
                    ls1[i][nex] = bounding[0][nex];

                    ls2[i] = new pPos(0, 0);
                    ls2[i][axis] = grid_xys[axis][i];
                    ls2[i][nex] = bounding[1][nex];
                }

                ls2 = ls2.Reverse().ToArray();

                pPos mv = new pPos(0, 0);
                mv[nex] = -ruler_h * 2;

                AddDimChain(ls1, axis, ls1[0] + 1.5 * mv);
                AddDimChain(new pPos[] { ls1.First(), ls1.Last() }, axis, ls1[0] + 2 * mv);
                AddDimChain(ls2, axis, ls2[0] - 1.5 * mv);
                AddDimChain(new pPos[] { ls2.First(), ls2.Last() }, axis, ls2[0] - 2 * mv);
            }
        }

        void BuildRuler(pPos[] bb, int axis, pPos basepoint)
        {
            int nex = (axis + 1) % 2;
            //ACD.WR("OK1");
            List<pPos> pts = points.Where(p => p.InsideRect(bb[0], bb[1])).ToList();
            pts.AddRange(allpoints.Select(t => t.Intersect(bb[0], bb[1])).ToCollectionSameClosed().AllPoints);
            pts.AddRange(bounding);
            //ACD.WR("OK2");
            if (show_points)
                foreach (pPos p in pts)
                {
                    pPos px = new pPos(p.X, p.Y);
                    px[nex] = basepoint[nex];
                    ACD.DB.DrawPolyline(new pPos[] { p, px }, false, GP.DEF_LAYER_TEXT);
                }
            //ACD.WR("OK3 {0}");
            int inf = basepoint[nex] > ct[nex] ? 1 : -1;
            //ACD.WR("OK4");

            double x = basepoint[nex] + inf * ruler_h;
            pPos[] ls = pts.ExtractPtsXY(50)[axis]
                    .Select(v => _posByV(axis, v, x)).ToArray();

            pPos p1 = new pPos(0, 0);
            p1[axis] = bounding[0][axis];
            p1[nex] = basepoint[nex];

            pPos p2 = new pPos(0, 0);
            p2[axis] = bounding[1][axis];
            p2[nex] = x;

            //ObjectIdCollection ids = new ObjectIdCollection() { ACD.DB.DrawPolyline(p1.Rect(p2), true)};
            pPos mv = new pPos(0, 0);
            //ACD.WR("OK5");
            for (int i = 0; i < ls.Length; i++)
            {
                mv[nex] = - ruler_h;
                ls[i][nex] -= inf * ruler_h;
                //if (grid_xys[axis].Any(v => Math.Abs(v - ls[i][axis]) < 10))
                    //mv[nex] = - ruler_h * 0.75;

                //ObjectId lwpId = ACD.DB.DrawPolyline(new pPos[] { ls[i], ls[i] - inf * mv }, false);
                //ACD.DB._setLineworkLineColor(lwpId, 5);
                //ids.Add(lwpId);
            }
            //ACD.WR("OK6");
            AddDimChain(ls, axis, ls[0] - 2 * inf * mv);
            
            //ACD.DB.NewBlock(ids, blockname, true, false, basepoint);
            //ACD.DB.Insert(blockname, basepoint, "LAYER=G-DimPointNode");
        }

        ObjectIdCollection resultIds;

        void GenerateDimPoints()
        {
            //DE.StartWatch();
            ACD.DB.GetEntities(null, EN_SELECT.AC_DXF, "AEC_DIMENSION_GROUP");
            ObjectIdCollection saveIds = IR.SelectedIds;

            //if (IR.SelectedIds.Count ==0)
            //{
            ACD.DB.GetEntities(ACD.bRect, EN_SELECT.AC_DXF, "INSERT", "LWPOLYLINE",
                        "LINE", "AEC_WALL", "AEC_DOOR", "AEC_WINDOW", "CIRCLE", "ELLIPSE");

            double round = pString.INI_String("DIM_ROUND").ToNumber();
            double min = pString.INI_String("DIM_MIN").ToNumber();

            GetDimPoints(IR.SelectedIds.ToList()
                .Where(id => ACD.DB._getBound(id) != null).ToCollection(), round, min);
            
            
        }
    }
    public class DrawingMode
    {
        static DrawingModeForm frm;

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                try
                {
                    frm = new DrawingModeForm();
                    frm.Show();
                }
                catch (System.Exception ex)
                {
                    ACD.WR("Error {0},{1}", ex.Message, ex.StackTrace);
                }

               ACD.Focus();
            }
        }
    }
}

