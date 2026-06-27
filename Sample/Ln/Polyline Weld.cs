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
    public class PolylineWeldCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();
                //ObjectIdCollection txtIds = ACD.DB.FilterIds(selIds, "CIRCLE", "MTEXT", "TEXT");

                double tole = ACD.ED.GetInputString("Weld Value", "1").ToNumber();

                selIds = selIds.Cast<ObjectId>()
                    .Where(id => (ACD.DB._isLine(id) || ACD.DB._isPolyline(id))
                    && !ACD.DB._isPolylineClosed(id))
                    .Select(id => id).ToCollection();

                PosCollection pls = selIds.Cast<ObjectId>()
                    //.Where(id => ACD.DB._isLine(id) || ACD.DB._isPolyline(id))
                    .Select(id => ACD.DB._getVertices(id).OrderBy(p => p.X).ThenBy(p => p.Y).ToArray())
                    .OrderBy(ls => ls.First().X).ThenBy(ls => ls.Last().Y).ToCollectionSameClosed();

                //PosCollection new_pls = pls.WeldSplines;

                List<int> indx = new List<int>();

                for(int i = 0; i < pls.Count; i ++)
                {
                    int nex = (i + 1) % pls.Count;

                    if( i > 0 && pls[i].Last()._isVeryClosed(pls[nex].First(), tole))
                    {
                        indx.Add(nex);
                    }else
                    {
                        List<pPos> pts = new List<pPos>();

                        foreach (int n in indx)
                            pts.AddRange(pls[n]);

                        ACD.DB.DrawPolyline(pts.Weld(tole),false,"LAYER=" + ACD.DB._getLayer(selIds.First()));
                        indx = new List<int> { i };
                    }
                }

                //foreach (pPos[] ls in new_pls)
                //{
                //    //pPos pt = ls.CenterMaxSegment();
                //    int index = pls.FindIndex(l => pt.DistanceTo(l) < 0.1);

                //    if (index != -1)
                //        ACD.DB.DrawPolyline(ls, false, selIds[index]);
                //    else
                //        ACD.DB.DrawPolyline(ls, false);

                //    if (ACD.DB._isLeader(ls, txtIds).empty())
                //    {
                //        ObjectId txtId = ACD.DB.CreateText("#M" + Math.Round(ls.Length(false) / 1000, 2).ToString() + "m",
                //            pt, 50, 0, "BACKGROUND=ON|LAYER=DEFPOINTS");
                //        ACD.DB._setRotation(txtId, -(ACD.CenterMaxSegment_Seg[1] - ACD.CenterMaxSegment_Seg[0]).Angle());
                //    }
                //}

                ACD.DB.EraseObjects(selIds);
            }
        }
    }
}

