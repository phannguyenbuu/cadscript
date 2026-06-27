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

using AEC = Autodesk.Aec.Arch.DatabaseServices;
using Autodesk.Aec.ArchDACH.DatabaseServices;


using System.Windows.Forms;
//using SyncObject;

namespace AcadScript
{
    public class DimensionAroundFlipCLS
    {
        

        static double RANGE = 2000, SPACING = 200, MIN_VALUE = 10, FLIP_VALUE = 400;

        static void _dimAround(List<double[]> xys, double spacing, pPos[] bb, pPos mv)
        {
            for (int axis = 0; axis < 2; axis++)
            {
                pPos p1 = null, p2 = null, p3 = null;


                int nex = (axis + 1) % 2;


                p1 = new pPos(0, 0);
                p2 = new pPos(0, 0);

                p1[axis] = xys[axis].First().roundNumber(10);
                p2[axis] = xys[axis].Last().roundNumber(10);
                p1[nex] = p2[nex] = bb[1][nex];

                p3 = p1.Parallel(p2, 2 * (axis == 0 ? spacing : -spacing))[0];

                ACD.DB.SetXNotes(IDimChain.CreateDimension(ACD.DB, p1 + mv, p2 + mv, p3 + mv, "", FLIP_VALUE), "dir=" + axis + "(1)");

                for (int i = 0; i < xys[axis].Length - 1; i++)
                {
                    p1 = new pPos(0, 0);
                    p2 = new pPos(0, 0);

                    p1[axis] = xys[axis][i].roundNumber();
                    p2[axis] = xys[axis][i + 1].roundNumber();

                    //p1[nex] = p2[nex] = bb[0][nex];
                    //p3 = p1.Parallel(p2, axis == 0 ? -spacing : spacing)[0];
                    //ACD.DB.SetXNotes(IDimChain.CreateDimension(ACD.DB, p1 + mv, p2 + mv, p3 + mv, "", FLIP_VALUE), "dir=" + axis + "(1)");

                    p1[nex] = p2[nex] = bb[1][nex];
                    p3 = p1.Parallel(p2, axis == 0 ? spacing : -spacing)[0];
                    ACD.DB.SetXNotes(IDimChain.CreateDimension(ACD.DB, p1 + mv, p2 + mv, p3 + mv, "", FLIP_VALUE), "dir=" + axis + "(1)");
                }

                
            }
        }

        static PosCollection _getDeepVerts(ObjectIdCollection ids)
        {
            PosCollection res = new PosCollection();

            foreach (ObjectId id in ids)
                if (ACD.DB._isPolyline(id))
                    res.Add(ACD.DB._getVertices(id));
                else if (ACD.DB._isLine(id))
                    res.Add(ACD.DB._getVertices(id));
                else if (ACD.DB._isBlock(id))
                    ACD.DB.BlockEntitiesAction(id, _ids => { res.AddRange(_getDeepVerts(_ids)); });

            return res;
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                //RANGE = pString.INI_String("DIM_EFFECT_RANGE").ToNumber(2000);
                //MIN_VALUE = pString.INI_String("DIM_MIN_VALUE").ToNumber(200);
                //FLIP_VALUE = pString.INI_String("DIM_FLIP_VALUE").ToNumber(200);

                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    string[] xnotes = ACD.DB.GetXNotes(selIds);
                    double spacing = SPACING;

                    pPos mv = new pPos(0, 0);

                    //var xys = ACD.DB.GetGridXY(selIds, 
                    //    xnotes._props("dim.round").ToNumber(50), 
                    //    xnotes._props("dim.range").ToNumber(100));


                    //_dimAround(xys, spacing, ACD.DB._getBound(selIds), mv);

                    List<double> lsX = new List<double>();
                    List<double> lsY = new List<double>();

                    foreach (ObjectId id in selIds)
                        if (ACD.DB._isGrid(id))
                        {
                            PosCollection pls = new PosCollection();
                            ACD.DB.BlockEntitiesAction(id, _ids => { pls.AddRange(_getDeepVerts(_ids)); });

                            var xys = pls.SelfIntersect.ExtractPtsXY(xnotes._props("dim.round").ToNumber(50),
                                xnotes._props("dim.range").ToNumber(10));

                            lsX.AddRange(xys[0]);
                            lsY.AddRange(xys[1]);
                        }else
                        {
                            var xys = ACD.DB.GetGridXY(selIds,
                                xnotes._props("dim.round").ToNumber(10),
                                xnotes._props("dim.range").ToNumber(10));

                            lsX.AddRange(xys[0]);
                            lsY.AddRange(xys[1]);
                        }

                    _dimAround(new List<double[]> { lsX.Distinct().ToArray(), lsY.Distinct().ToArray() },
                        spacing, ACD.DB._getBound(selIds), mv);

                    ACD.Focus();
                }
            }
        }
    }
}

