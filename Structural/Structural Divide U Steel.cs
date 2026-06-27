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
    public class StructuralDivideUSteelCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();
                //pPos[] overallbb = ACD.DB._getBound(selIds);
                //ACD.WR("OK0");
                //ObjectIdCollection gridIds = ACD.DB._getAllGrids();
                //selIds.Cast<ObjectId>()
                //    .Where(id => ACD.DB._isBlock(id) && ACD.DB._getBound(id).Size().X > overallbb.Size().X / 2
                //        && ACD.DB._getBound(id).Size().Y > overallbb.Size().Y / 2).ToCollection();

                //if (gridIds.Count > 0)
                //{
                    //PosCollection bounds = gridIds.Cast<ObjectId>().Select(id => ACD.DB._getBound(id)).ToCollection();
                    //List<PosCollection> grids = new List<PosCollection>();
                    ////ACD.WR("GRIDS {0}", gridIds.Count);

                    //foreach (ObjectId id in gridIds)
                    //{
                    //    pPos basept = ACD.DB._getPoint(id);
                    //    //ObjectIdCollection expIds = new ObjectIdCollection();
                    //    ObjectIdCollection subIds = ACD.DB.GetEntInBlock(id);

                    //    pPos[] bb = ACD.DB._getBound(id);
                    //    double len = Math.Min(bb.Size().X, bb.Size().Y);

                    //    grids.Add(subIds.Cast<ObjectId>()
                    //        .Where(i => (ACD.DB._isLine(i) || (ACD.DB._isPolyline(i)
                    //        && !ACD.DB._isPolylineClosed(i))) && ACD.DB._getLength(i) >= len / 2)
                    //        .Select(i => ACD.DB._getVertices(i)).ToCollection());
                    //}

                    double r = pString.INI_String("UpperSteelRatio").ToNumber();
                    double min = pString.INI_String("MinBeamTolerance").ToNumber();

                    //ACD.WR("R {0} MIN {1}", r, min);

                    string setting = pString.INI_Params("UpperSteel").ToTextStr("|");
                    PosCollection res = new PosCollection();
                    //ACD.WR("OK3");

                    foreach (ObjectId id in selIds)
                        if (ACD.DB._isPolyline(id) && !ACD.DB._isPolylineClosed(id))
                        {
                            pPos[] pts = ACD.DB._getVertices(id);
                            //pPos ct = pts.CenterPoint();

                            //int index = -1;
                            //for (int i = 0; i < bounds.Count; i++)
                            //    if (ct.Inside(bounds[i][0], bounds[i][1]))
                            //    {
                            //        index = i;
                            //        break;
                            //    }

                            //ACD.WR("OK4");

                            //if (index != -1)
                            //{
                            //    PosCollection grid_pts = grids[index];
                            //    List<pPos> interps = pts.ToList();

                            //    foreach (pPos[] line in grid_pts)
                            //        interps.AddRange(line.Intersect(pts, true));
                            //    //ACD.WR("OK5");
                            //    interps = interps.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();

                            //    for (int i = 0; i < interps.Count - 1; i++)
                            //    {
                                    pPos p1 = pts.First(), p2 = pts.Last();
                                    if (p1.DistanceTo(p2) > min)
                                    {
                                        res.Add(new pPos[] { p1, p1.AlongRatio(p2, 1 / r) });
                                        res.Add(new pPos[] { p1.AlongRatio(p2, (r - 1) / r), p2 });
                                    }
                                    else
                                        res.Add(new pPos[] { p1, p2 });
                            //    }
                            //}

                            ACD.DB.EraseObject(id);
                        }

                    ObjectIdCollection ids = new ObjectIdCollection();
                    res = res.Select(ls => ls.OrderBy(p => p.X).ThenBy(p => p.Y).ToArray()).ToCollectionSameClosed();

                    foreach (pPos[] line in res)
                    {
                        ids.AddRange(ACD.DB.DrawStylePolyline("USTEEL", line, setting));
                        pPos ct = line[0].Parallel(line[1], 100).CenterPoint();

                        //ObjectId txtId = ACD.DB.CreateText("%%c10a200", ct, 100);

                        //ACD.DB._setRotation(txtId, (line[1] - line[0]).Angle());

                        //ids.Add(txtId);
                    }

                    ACD.DB._setLayer(ids, "B-Support-Upper");
                
            }
            ACD.Focus();
        }
    }
}

