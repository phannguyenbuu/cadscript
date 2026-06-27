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
    public class ShowColumnTextCLS
    {

        static pPos stringToSize(string txt)
        {
            string[] ar = txt.filter("()");

            pPos p = new pPos(0, 0);
            p.Content = ar.First();

            if (ar.Length > 1)
            {
                string[] ar1 = ar.Last().filter("x");
                if (ar1.Length > 1)
                {
                    p.X = ar1.First().ToNumber();
                    p.Y = ar1.Last().ToNumber();
                }
            }

            return p;
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ACD.WR("OK1");
                try
                {
                    ACD.ED.Command("LAYMRG", "n", "H", " ", "n", "A-Hatch", "y");
                }catch(System.Exception ex)
                {
                    ACD.WR("Error in {0} messgae {1}", ex.StackTrace, ex.Message);
                }
                ACD.WR("OK2");
                ObjectIdCollection selIds = ACD.GetSelection();

                ACD.WR("Stage 1: CHỈNH TEXT TÊN CỘT THÀNH CÓ KÍCH THƯỚC (*X*)");
                ACD.WR("Stage 2: THỐNG KÊ TEXT TÊN CỘT THÀNH SCHEDULE");

                int stage = (int)ACD.ED.GetInputString("STAGE", "0").ToNumber();

                ObjectIdCollection txtIds = selIds._filterDXF("MTEXT", "TEXT");
                PosCollection pls = selIds._filterDXF("LWPOLYLINE").ToList()
                    .Where(id => ACD.DB._isPolylineClosed(id))
                    .Select(id => ACD.DB._getVertices(id)).ToCollectionSameClosed();
                
                if (stage == 1)
                {
                    double spacing = ACD.ED.GetInputString("Spacing", "500").ToNumber(500);

                    foreach (ObjectId id in txtIds)
                    {
                        pPos pt = ACD.DB._getPoint(id);
                        int[] indxs = DE.NumericArray(0, pls.Count - 1).OrderBy(n
                            => pls[n].CenterPoint().DistanceTo(pt)).ToArray();
                        pPos[] bb = pls[indxs.First()].Boundary();
                        pPos sz = bb.Size().Round(10);

                        ACD.DB._setLayer(IDimChain.CreateDimension(ACD.DB, bb[0],
                            new pPos(bb[1].X, bb[0].Y),
                            new pPos(bb[0].X, bb[1].Y + spacing), "", 0), "A-Anno-Dims");
                        ACD.DB._setLayer(IDimChain.CreateDimension(ACD.DB, bb[0],
                            new pPos(bb[0].X, bb[1].Y),
                            new pPos(bb[0].X - spacing, bb[1].Y), "", 0), "A-Anno-Dims");

                        string txt = ACD.DB._getContent(id).filter("()").First();
                        ACD.DB._setContent(id, txt + "(" + sz.X + "x" + sz.Y + ")");
                    }
                }else if(stage == 2)
                {
                    double spacing = ACD.ED.GetInputString("Spacing", "2000").ToNumber(2000);
                    //txtIds = txtIds.ToList().OrderBy(id => ACD.DB._getPoint(id).Y).ToCollection();
                    PosCollection zones = ACD.DB.GetDrawingZones();
                    List<pPos>[] txtPts = zones.Select(z => new List<pPos>()).ToArray();

                    //foreach(pPos[] zone in zones)
                        //ACD.WR("Z {0},{1}", zone[0],zone[1]);

                    foreach (ObjectId txtId in txtIds)
                    {
                        string txt = ACD.DB._getContent(txtId);
                        pPos pt = ACD.DB._getPoint(txtId);
                        int[] indxs = DE.NumericArray(0, zones.Count - 1).Where(n 
                            => pt.InsideRect(zones[n][0], zones[n][1])).ToArray();
                        //ACD.WR("Z {0}", zone.Length);

                        if (indxs.Length > 0)
                            txtPts[indxs.First()].Add(stringToSize(txt));
                    }

                    txtPts = txtPts.Select(ls => ls.OrderBy(p => p.Content.Upper()).ToList()).ToArray();

                    //int max = (int)txtPts.Max(ls => ls.Max(p => p.Content._getNumberInString().ToNumber()));
                    pPos basept = null;
                    
                    for (int i = 0; i < zones.Count; i++)
                    {
                        if (txtPts[i].Count > 0)
                        {
                            if (basept == null)
                                basept = zones[i].CenterPoint() - new pPos(zones[i].Size().X * 2, 0);

                            //ACD.WR("Index {0}:{1}", i, txtPts[i].ToText());
                            string[] ls = txtPts[i].Select(p => p.Content).Distinct().OrderBy(s
                                => s._getNumberInString().ToNumber()).ToArray();

                            foreach (string s in ls)
                            {
                                basept.X = zones[i].CenterPoint().X
                                    - zones[i].Size().X * 2 + spacing * s._getNumberInString().ToNumber();

                                pPos sz = txtPts[i].Where(p => p.Content == s).First();
                                ACD.DB.CreateText(s + "(" + sz.X + "x" + sz.Y + ")",
                                    basept - new pPos(0, 500), 2, 0, "ANNO=TRUE");
                                ACD.DB.DrawPolyline(basept.Rect(sz.X, sz.Y), true);
                                //ACD.DB._setLayer(IDimChain.CreateDimension(ACD.DB, basept,
                                //    new pPos(basept.X + sz.X, basept.Y + sz.Y),
                                //    new pPos(basept.X, basept.Y + sz.Y + spacing), "", 0), "A-Anno-Dims");
                                //ACD.DB._setLayer(IDimChain.CreateDimension(ACD.DB, basept,
                                //    new pPos(basept.X + sz.X, basept.Y + sz.Y),
                                //    new pPos(basept.X - spacing, basept.Y + sz.Y), "", 0), "A-Anno-Dims");
                            }

                            ACD.DB.CreateText("Zone" + i, new pPos(zones[i].CenterPoint().X
                                    - zones[i].Size().X * 2 - spacing, basept.Y), 2, 0, "ANNO=TRUE");
                            basept.Y += spacing;
                        }
                    }
                }
            }
            ACD.Focus();
        }
    }
}

