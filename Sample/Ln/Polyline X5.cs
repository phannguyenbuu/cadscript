using System;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Internal;
using AEC = Autodesk.Aec.Arch.DatabaseServices;

using System.Windows.Forms;
//using SyncObject;

namespace AcadScript
{
    public class Mesh3D
    {
        public int IndexInList;
        public pPos[] Verts;
        public string Name;
        public int LabelX, LabelY;
        public List<int[]> FaceIndexes;
        public double ExtrudeAmount;
        public bool HasPlate;

        public double BoundVolume
        {
            get
            {
                pPos sz = Verts.Size();
                return sz.X * sz.Y * sz.Z;
            }
        }

        double _get_label_idx(pPos v, int ax)
        {
            double res = 0;

            if (ax == 0)
            {
                double m = v[ax] % 12000;

                res = (int)(v[ax] / 12000);

                if (Math.Abs(m) >= 10000)
                    res += 1;
                else if (Math.Abs(m) <= 2000)
                    res = v[ax] / 12000;
            }else if(ax == 1)
            {
                double m = (v[ax] - 4500) % 12000;

                res = (int)((v[ax] - 4500) / 12000);

                if (Math.Abs(m) >= 10000)
                    res += 2;
                else if (Math.Abs(m) <= 2000)
                    res = (v[ax] - 4500) / 12000 + 1;
            }

            return res;
        }

        pPos[] __plate_sizes = new pPos[] { new pPos(150, 150, 10, "EB-003"),
            new pPos(200, 200, 12, "EB-001"),
            new pPos(250,150,10, "EB-002"),
            new pPos(220,760, 20, "AB-001"),
            new pPos(300,760, 20, "AB-001")};

        pPos _isEBPlate(pPos[] pts)
        {
            pPos sz = pts.Size().Round(10);

            return __plate_sizes.FirstOrDefault(__sz =>
                (sz.X == __sz.X && sz.Z == __sz.Y)
                || (sz.Y == __sz.X && sz.Z == __sz.Y)
                );
        }

        public Mesh3D(int index, string _name, pPos[] verts, IEnumerable<int[]> face_index)
        {
            this.IndexInList = index;
            this.Verts = verts;
            this.Name = _name;
            this.HasPlate = false;
            pPos _is_plate = _isEBPlate(verts);

            this.FaceIndexes = face_index.ToList();

            pPos __sz = this.Verts.Size();

            if (__sz.Z > 390 && __sz.Z < 710)
                this.Name = "VAI";

            if (_is_plate != null)
            {
                this.ExtrudeAmount = _is_plate.Z;
                this.Name = _is_plate.Content;
                this.HasPlate = true;
            }

            pPos ct = Verts.CenterPoint();

            LabelX = (int)Math.Floor(_get_label_idx(ct, 0)) + 1;
            LabelY = (int)Math.Floor(_get_label_idx(ct, 1)) + 1;
        }

        public string LabelXYText
        {
            get
            {
                return "X" + LabelX + "-Y" + LabelY;
            }
        }
    }

    public class Mesh3DCollection : PosCollection
    {
        public List<Mesh3D> Elements;
        public Dictionary<string, List<Mesh3D>> dicts;
        public pPos CPoint;

        public Mesh3DCollection(string[] str) : base()
        {
            dicts = new Dictionary<string, List<Mesh3D>>();

            Elements = new List<Mesh3D>();
            int idx = 0;

            foreach (string __s in str)
                if (!__s._prop("Verts").et_() && !__s._prop("Faces").et_())
                {
                    pPos[] __verts = new PosCollection(__s._prop("Verts")).First();
                    this.Add(__verts);
                    Elements.Add(new Mesh3D(idx, "", __verts,
                        __s._prop("Faces").filter(";").Select(__v
                             => __v.filter(",").Select(s___ => (int)s___.ToNumber()).ToArray())));

                    idx++;
                }

            Elements = Elements.OrderBy(__mesh => -__mesh.Verts.Size().Z).ToList();
            CPoint = Elements[0].Verts.CenterPoint();

            foreach (Mesh3D element in Elements)
            {
                if (!dicts.ContainsKey(element.LabelXYText))
                    dicts.Add(element.LabelXYText, new List<Mesh3D>());

                dicts[element.LabelXYText].Add(element);
            }

            dicts = dicts.OrderBy(itm => itm.Key.filter("-")[0].Substring(1).ToNumber())
                .ThenBy(itm => itm.Key.filter("-")[1].Substring(1).ToNumber())
                .ToDictionary(itm => itm.Key, itm => itm.Value);
        }

        public PosCollection GetFlatView(Mesh3D mesh, pPos axis)
        {
            PosCollection res = new PosCollection();

            for (int f = 0; f < mesh.FaceIndexes.Count; f++)
            {
                pPos[] verts = mesh.FaceIndexes[f].Select(__n => mesh.Verts[__n]).ToArray();

                if (mesh.Name.et_() || verts.All(__p => cReadData._isPointInView(__p, axis, this.CPoint)))
                    //Là cột hoặc là (pat , vai mà thuộc View)
                {
                    //ACD.WR("OK3.3");
                    pPos[] new_verts = verts.Select(__p =>
                    {
                        pPos p = __p.Clone();

                        if (axis[1] != 0)
                            p.Y = p.Z;

                        if (axis[0] != 0)
                        {
                            p.X = p.Y;
                            p.Y = p.Z;
                        }

                        p.Z = 0;
                        p.Content = mesh.Name;

                        //if(!p.Content.et_())
                        //    ACD.WR("MESH_NAME {0}", p.Content);

                        return p;
                    }).ToArray();

                    if (new_verts.Area() > 16000)
                        res.Add(new_verts);
                }
                //ACD.WR("OK3.4");
            }
            
            return res;
        }

        static ObjectIdCollection _draw_grid_line(string lbl_axis, pPos[] __ls)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            string[] ar = lbl_axis.filter("_");

            if (ar[1] == "X" || ar[1] == "Y")
            {
                res.Add(ACD.DB.DrawPolyline(__ls, false, HIDDEN_LAYER));
                res.AddRange(ACD.DB.DrawCircle(__ls[0] + new pPos(0, -750), 350));
                res.Add(ACD.DB.CreateText("#M" + ar[1] + ar[2], __ls[0] + new pPos(0, -750), 5));
            }
            else if (ar[1] == "T")
            {
                res.Add(ACD.DB.DrawPolyline(__ls, false, HIDDEN_LAYER));

                if (ar[2].st_("Y"))
                {
                    res.AddRange(ACD.DB.DrawCircle(__ls[0] + new pPos(-1300, 0), 350));
                    res.Add(ACD.DB.CreateText("#M" + ar[2], __ls[0] + new pPos(-1300, 0), 5));
                }
                else
                {
                    res.AddRange(ACD.DB.DrawCircle(__ls[0] + new pPos(0, -1000), 350));
                    res.Add(ACD.DB.CreateText("#M" + ar[2], __ls[0] + new pPos(0, -1000), 5));
                }
            }

            return res;
        }


        static string HIDDEN_LAYER = "LAYER=1_Gridline_QHP";

        public static ObjectIdCollection _drawColElevation(string key, PosCollection pls, pPos pt)
        {
            ObjectIdCollection ids = new ObjectIdCollection();
            pPos ext = new pPos(500, 200);

            PosCollection dims = new PosCollection();
            PosCollection dims_x = new PosCollection();

            pls = pls.OrderBy(__ls => -__ls.Area()).ToCollectionSameClosed(true);
            //ACD.WR("OK1 {0}", pls.Count);
            dims.Add(pls[0]);
            dims.Add(pls[1]);
            //ACD.WR("OK2");
            List<string> save_dim_x = new List<string>();

            for (int i = 0; i < pls.Count; i++)
            {
                var __ls = pls[i];

                if (__ls[0].Content.st_("AXIS"))
                    ids.AddRange(_draw_grid_line(__ls[0].Content, __ls));
                else
                    ids.Add(ACD.DB.DrawPolyline(__ls, true));
                //ACD.WR("OK3");
                pPos sz = __ls.Size().Round(10);

                pPos[] bb = __ls.Boundary();
                pPos ct = bb.CenterPoint();

                bool a = ct.Inside(pls[0]), b = ct.Inside(pls[1]);
                if (a || b)
                {
                    int idx = a ? 0 : 1;

                    pPos p1 = new pPos(pls[idx].Boundary()[0].X, ct.Y);
                    pPos p2 = new pPos(bb[0].X, ct.Y);
                    pPos p3 = new pPos(bb[1].X, ct.Y);
                    pPos p4 = new pPos(pls[idx].Boundary()[1].X, ct.Y);

                    string st = Math.Abs(p1.X - p2.X) + "-" + Math.Abs(p3.X - p4.X);

                    if (!save_dim_x.Contains(st) && __ls[0].Content != "VAI" && !__ls[0].Content.st_("AXIS"))
                    {
                        save_dim_x.Add(st);
                        dims_x.Add(new pPos[] { p1, p2 });
                        dims_x.Add(new pPos[] { p3, p4 });
                    }
                }

                ct.Y = __ls.Boundary()[0].Y;

                if (__ls[0].Content != "VAI" && !__ls[0].Content.st_("AXIS"))
                {
                    if (!__ls[0].Content.et_())
                    {
                        ids.Add(ACD.DB.Insert(__ls[0].Content, ct));
                        ids.AddRange(ACD.DB.CreateMLeader(__ls[0].Content, ct, ct + ext));
                    }

                    dims.Add(__ls);
                }

            }
            //ACD.WR("OK5");
            pPos mv = new pPos(0, 20) - pls.Boundary[0];
            ACD.DB.MoveObject(ids, mv);
            var xys = dims.Move(mv).ExtractPtsXY(5, 5);

            ids.Add(ACD.DB.CreateText(@"{\L" + key + "}", new pPos(-1500, -2000), 10));
            ids.Add(ACD.DB.CreateText("SCALE: 1/50", new pPos(-1500, -3000), 10));

            int ax = 1;
            int nex = (ax + 1) % 2;

            for (int i = 0; i < xys[ax].Length - 1; i++)
                if (Math.Abs(xys[ax][i] - xys[ax][i + 1]) > 25)
                {
                    pPos p1 = new pPos(0, 0);
                    p1[ax] = xys[ax][i];

                    pPos p2 = new pPos(0, 0);
                    p2[nex] = p1[nex] = 0;
                    p2[ax] = xys[ax][i + 1];

                    pPos p3 = p1.Parallel(p2, 1000)[0];

                    ids.Add(IDimChain.CreateDimension(ACD.DB, p1, p2, p3, "<>", 200));

                    if (Math.Abs(xys[ax][i] - xys[ax][i + 1]) < 155)
                        ACD.DB._setDimTextPos(ids.Last(), new pPos(p3.X + 400, (p1.Y + p2.Y) / 2));
                }

            foreach (pPos[] __ls in dims_x)
                if (Math.Abs(__ls[0].X - __ls[1].X) > 25)
                    ids.Add(IDimChain.CreateDimension(ACD.DB, __ls[0] + mv, __ls[1] + mv, 200, 200));

            ACD.DB.MoveObject(ids, pt);

            return ids;
        }

        static void _mirror_Pls(PosCollection pls, double nx)
        {
            for (int i = 0; i < pls.Count; i++)
                for (int j = 0; j < pls[i].Length; j++)
                    pls[i][j].X = nx + nx - pls[i][j].X;
        }

        public string DrawMeshViewGroup(string key, pPos plot_pt)
        {
            int cnt = 0;
            List<string> __content = new List<string>();

            for (int i = 0; i < cReadData.AxisData.Length; i++)
            {
                var axis = cReadData.AxisData[i];
                PosCollection view_pls = new PosCollection();

                int lblX = 0, lblY = 0;

                foreach (var mesh in this.dicts[key])
                {
                    view_pls.AddRange(this.GetFlatView(mesh, axis));
                    lblX = mesh.LabelX - 1;
                    lblY = mesh.LabelY - 1;
                }

                if (view_pls.Count > 0)
                {
                    if (i > 0)
                    {
                        double __nX = 12000 * (i == 1 || i == 2 ? lblX : lblY) + (i == 1 || i == 2 ? 0 : 4500);
                        var __line = new pPos[] { new pPos(__nX, 0), new pPos(__nX, 25000) };
                        __line[0].Content = __line[1].Content = "AXIS_" + (i < 3 ? "X_" + (lblX + 1) : "Y_" + (lblY + 1));
                        view_pls.Add(__line);

                        if (i == 1 || i == 4)
                            _mirror_Pls(view_pls, __line[0].X);
                    }
                    else
                    {
                        double __nX = 12000 * lblX;
                        double __nY = 12000 * lblY + 4500;

                        var __line = new pPos[] { new pPos(__nX, __nY), new pPos(__nX, __nY + 2000) };
                        __line[0].Content = __line[1].Content = "AXIS_T_Y" + (lblY + 1);
                        view_pls.Add(__line);

                        __line = new pPos[] { new pPos(__nX, __nY), new pPos(__nX + 2000, 12000 * lblY) };
                        __line[0].Content = __line[1].Content = "AXIS_T_X" + (lblX + 1);
                        view_pls.Add(__line);
                    }

                    _drawColElevation(key + " - " + axis.Content, view_pls,
                        plot_pt + new pPos(i == 0 ? -6000 : i * 6000, 0));

                    __content.Add(view_pls.ToString());
                }
            }

            ACD.DB.Insert("levelBlock", plot_pt + new pPos(6000, 0));

            cnt++;
            plot_pt.X += 50000;

            if (cnt % 20 == 0)
            {
                plot_pt.X = -6000;
                plot_pt.Y += 50000;
            }

            string filename = @"D:\Temp\" + ACD.CurrentDWGFileName + key + ".txt";
            File.WriteAllLines(filename, __content.ToArray());

            return filename;
        }
    }


    public class PolylineElevatorCLS
    {
        static bool _need_show_info = false;
        static Mesh3DCollection MeshList;
        static string factory_name = "X5";

        static string PrintValue(object obj)
        {
            string res = "";
            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
            {
                string name = descriptor.Name;
                object value = descriptor.GetValue(obj);
                res = name + "=" + value + ";";
            }
            return res;
        }

        static async Task PerformTaskAsync()
        {
            int page_index = 0;

            foreach (string key in MeshList.dicts.Keys)
            {
                ACD.WR("Plot page {0}", page_index);

                ACD.ProgressLoad.Value = page_index;
                ACD.ProgressLoad.Refresh();

                ACD.ProgressPercent.Text = String.Format("{0}/{1}", ACD.ProgressLoad.Value, ACD.ProgressLoad.Maximum);
                ACD.ProgressPercent.Refresh();

                //ACD.WR("P1");
                //if (ACD.ProgressCancel)
                //    return;
    
                if (MeshList.dicts[key].Any(mesh => mesh.HasPlate))
                    MeshList.DrawMeshViewGroup(key, _page_pt);

                page_index++;
            }

        }

        //static bool _draw_done = false;
        static int page_index = 0;
        static string infofile = "";
                

        static pPos _page_pt = new pPos(-6000, 0);

        static async void RunTask()
        {
            await PerformTaskAsync();
        }

        public static async void Main(string[] args)
        {
            using (ACD.Lock())
            {
                string[] lsTxt = File.ReadAllLines(@"D:\column_extract.txt");
                MeshList = new Mesh3DCollection(lsTxt);
                
                ACD.ProgressLoad.Maximum = MeshList.dicts.Count;
                ACD.ProgressLoad.Value = 0;
                ACD.ProgressCancel = true;

                page_index = 1;

                RunTask();

                ACD.Focus();
            }
        }
    }
}


