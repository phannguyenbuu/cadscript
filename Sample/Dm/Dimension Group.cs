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
    public class DimensionGroup
    {
        static void _dimTest()
        {
            //ACD.WR("<Draw beam>Select region rectangle by CLOSED polyline, LINE is gridline");
            PosCollection pls = new PosCollection("(743167,98277.74;742967,98277.74;742967,98677.74;743167,98677.74)");
            Database db = ACD.DB;
            double A = "800".ToNumber(800);
            double B = "300".ToNumber(300);
            double C = "50".ToNumber(50);
            double E = "50".ToNumber(50);
            double NS = "2".ToNumber(2);
            double S1 = "4".ToNumber(4);
            double S2 = "8".ToNumber(8);
            double CL1 = "800".ToNumber(200);
            double CL2 = "800".ToNumber(200);
            double CLP1 = "800".ToNumber(100);
            double CLP2 = "800".ToNumber(200);
            double STC = "800".ToNumber(6);
            double STM = "800".ToNumber(16);
            double STS = "800".ToNumber(12);
            double F = "800".ToNumber(500);
            double BW = "<BW/>".ToNumber(200);
            double BH = "<BH/>".ToNumber(400);
            // = 300,  = 50,  = 50,  = 2,  = 4, S2 = 8, CL1 = 200, CL2 = 200, CLP1 = 100, CLP2 = 200, STC = 6, STM = 16, STS = 12, F = 500;
            if (pls.Count > 0)
            {
                double SUM = pls.Sum(ls => ls.Boundary().Size().X);
                foreach (pPos[] R in pls)
                {
                    //THÉP LƯNG TRÁI
                    db.DrawPolyline(new pPos[] { new pPos(R[0].X + (R[1].X - R[0].X) * 0.29, R[0].Y + (R[1].Y - R[0].Y) * 0.75),
                            new pPos(R[0].X + (R[1].X - R[0].X) * 0.3, R[0].Y + (R[1].Y - R[0].Y) * 0.88),
                            new pPos(R[0].X + (R[1].X - R[0].X) * 0, R[0].Y + (R[1].Y - R[0].Y) * 0.88) }, false, "LAYER=B-Support");
                    //THÉP LƯNG PHẢI
                    db.DrawPolyline(new pPos[] { new pPos(R[0].X + (R[1].X - R[0].X) * 1, R[0].Y + (R[1].Y - R[0].Y) * 0.88),
                            new pPos(R[0].X + (R[1].X - R[0].X) * 0.7, R[0].Y + (R[1].Y - R[0].Y) * 0.88),
                            new pPos(R[0].X + (R[1].X - R[0].X) * 0.71, R[0].Y + (R[1].Y - R[0].Y) * 0.75) }, false, "LAYER=B-Support");
                    //THÉP BỤNG
                    db.DrawPolyline(new pPos[] { new pPos(R[0].X + (R[1].X - R[0].X) * 0.81, R[0].Y + (R[1].Y - R[0].Y) * 0.25),
                            new pPos(R[0].X + (R[1].X - R[0].X) * 0.83, R[0].Y + (R[1].Y - R[0].Y) * 0.12),
                            new pPos(R[0].X + (R[1].X - R[0].X) * 0.17, R[0].Y + (R[1].Y - R[0].Y) * 0.12),
                            new pPos(R[0].X + (R[1].X - R[0].X) * 0.19, R[0].Y + (R[1].Y - R[0].Y) * 0.25) }, false, "LAYER=B-Support");
                    //ĐƯỜNG BAO
                    ACD.DB.DrawPolyline(new pPos[] { new pPos(R[0].X, R[0].Y, R[0].Z), new pPos(R[0].X, R[1].Y, R[1].Z),
                            new pPos(R[1].X, R[1].Y, R[1].Z), new pPos(R[1].X, R[0].Y, R[0].Z) }, false, "B-Hidden");
                    //THÉP CHỦ TRÊN
                    ACD.DB.DrawPolyline(new pPos[] { new pPos(SUM - 2 * C, R[0].Y + E, R[1].Z + E + C),
                            new pPos(SUM - C, R[0].Y + E, R[1].Z + E),new pPos(SUM - C, R[0].Y + E, R[1].Z + E),
                            new pPos(SUM - C, R[0].Y + E, R[1].Z - C + BH),new pPos(+C, R[0].Y + E, R[1].Z - C + BH),
                            new pPos(+C, R[0].Y + E, R[1].Z + C),new pPos(C + C, R[0].Y + E, R[1].Z + C + C)},
                        false, "LAYER=B-Main");
                    //THÉP CHỦ DƯỚI
                    ACD.DB.DrawPolyline(new pPos[] {new pPos(SUM-C*2-C,R[0].Y+E,R[1].Z+BH/2-C), new pPos(SUM-C*2,R[0].Y+E,R[1].Z+BH/2),
                            new pPos(SUM-C*2,R[0].Y+E,R[1].Z+C),new pPos(C*2,R[0].Y+E,R[1].Z+C),
                            new pPos(C*2,R[0].Y+E,R[1].Z+BH/2),new pPos(C*2+C,R[0].Y+E,R[1].Z+BH/2-C) },
                        false, "LAYER=B-Main");
                    //ACD.DB.DrawPolyline(=B-Support|MOVE=R[0].X,0|VERTS=0,R[1].Y-C,R[0].Z-E),new pPos((R[1].X-R[0].X)/S1+CL1,R[1].Y-C,R[0].Z-E),new pPos((R[1].X-R[0].X)/S1-C+CL1,R[1].Y-C,R[0].Z-E-C),new pPos(),new pPos(
                    //ACD.DB.DrawPolyline(=B-Support|MOVE=R[0].X,0|VERTS=R[1].X-R[0].X,R[1].Y-C,R[0].Z-E),new pPos((R[1].X-R[0].X)*(S1-1)/S1-CL2,R[1].Y-C,R[0].Z-E),new pPos((R[1].X-R[0].X)*(S1-1)/S1+C-CL2,R[1].Y-C,R[0].Z-E-C),new pPos(),new pPos(
                    //ACD.DB.DrawPolyline(=B-Support|MOVE=R[0].X,0|VERTS=(R[1].X-R[0].X)/S2+C+CL1,R[1].Y-C,R[1].Z+E+C),new pPos((R[1].X-R[0].X)/S2+CL1,R[1].Y-C,R[1].Z+E),new pPos((R[1].X-R[0].X)*(S2-1)/S2-CL2,R[1].Y-C,R[0].Z-BH+E),new pPos((R[1].X-R[0].X)*(S2-1)/S2-C-CL2,R[1].Y-C,R[0].Z-BH+E+C),new pPos(),new pPos(
                    //IDimChain.CreateDimension(db)
                    //# DIM=DIM_BEAM_FLIP|VERTS=R[0].X,R[1].Y,R[0].Z[<> %%C STC _a_ CLP1]),new pPos(R[0].X+(R[1].X-R[0].X)/S1+CL1,R[1].Y,R[0].Z),new pPos(),new pPos(R[0].X+(R[1].X-R[0].X)/S1+CL1,R[1].Y,R[0].Z[<> %%C STC _a_ CLP2]),new pPos(R[0].X+(R[1].X-R[0].X)*(S1-1)/S1-CL2,R[1].Y,R[0].Z),new pPos(),new pPos(R[0].X+(R[1].X-R[0].X)*(S1-1)/S1-CL2,R[1].Y,R[0].Z[<> %%C STC _a_ CLP1]),new pPos(R[1].X,R[1].Y,R[0].Z),new pPos(),new pPos(
                    //# DIM=DIM_BEAM_FLIP|VERTS=R[0].X+(R[1].X-R[0].X)/S2+CL1,R[1].Y,R[1].Z),new pPos(R[0].X,R[1].Y,R[1].Z),new pPos(),new pPos(R[0].X+(R[1].X-R[0].X)*(S2-1)/S2-CL2,R[1].Y,R[1].Z),new pPos(R[0].X+(R[1].X-R[0].X)/S2+CL1,R[1].Y,R[1].Z),new pPos(),new pPos(R[1].X,R[1].Y,R[1].Z),new pPos(R[0].X+(R[1].X-R[0].X)*(S2-1)/S2-CL2,R[1].Y,R[1].Z),new pPos(),new pPos(
                    //                    ghi chu thep tren
                    //#NOTE=NOTE_STEEL_T1|VERTS=(R[0].X+R[1].X)/2,R[1].Y-C,R[0].Z-C[#M3&#L2 _%%C_ STM]),new pPos(
                    //#NOTE=NOTE_STEEL_T1|VERTS=R[0].X+(R[1].X-R[0].X)/(S1*2),R[1].Y-C,R[0].Z-C[#M4&#L1 _%%C_ STM]),new pPos((R[1].X-R[0].X)*(S1-1)/S1,R[0].Y,R[0].Z-C[#M4&#L1 _%%C_ STM]),new pPos(),new pPos(
                    //ghi chu thep duoi
                    //#NOTE=NOTE_STEEL_T2|VERTS=R[0].X+F,R[0].Y,R[1].Z+C[#M1&#L2 _%%C_ STM]),new pPos(),new pPos(
                    //#NOTE=NOTE_STEEL_T2|VERTS=(R[0].X+R[1].X)/2,R[0].Y,R[1].Z+C[#M2&#L1 _%%C_ STM]),new pPos(),new pPos(
                    //#NOTE=NOTE_BEAM_CUT|VERTS=R[0].X+(R[1].X-R[0].X)/(S2*2),R[1].Y-C,R[1].Z-A[A1]),new pPos(R[0].X+((R[1].X-R[0].X)+(R[1].X-R[0].X)*(S2-1))/(S2*2),R[0].Y,R[1].Z-A[B1]),new pPos(R[1].X-(R[1].X-R[0].X)/(S2*2),R[1].Y-C,R[1].Z-A[A2]),new pPos(),new pPos(R[0].X+(R[1].X-R[0].X)/(S2*2),R[1].Y-C,R[0].Z+A[A1]),new pPos(R[0].X+((R[1].X-R[0].X)+(R[1].X-R[0].X)*(S2-1))/(S2*2),R[0].Y,R[0].Z+A[B1]),new pPos(R[1].X-(R[1].X-R[0].X)/(S2*2),R[1].Y-C,R[0].Z+A[A2]),new pPos(),new pPos(
                    //INS = Column_Facade | VERTS = R[0].X,R[0].Y,R[0].Z),new pPos(
                    //                    R[1].X,R[0].Y,R[0].Z),new pPos(
                    //ACD.DB.DrawPolyline(=B-Clip|View=1|ARR=R[0].X+(R[1].X-R[0].X)/S1+CL1,0,R[0].Z-C/2[CLP1,0,0]|VERTS=R[0].X+CL1,0,R[0].Z-C/2),new pPos(R[0].X+CL1,0,R[0].Z-BH+C/2),new pPos(
                    //ACD.DB.DrawPolyline(=B-Clip|View=1|ARR=R[1].X-CL2,0,R[0].Z-C/2[CLP1,0,0]|VERTS=R[0].X+(R[1].X-R[0].X)*(S1-1)/S1-CL2,0,R[0].Z-C/2),new pPos(R[0].X+(R[1].X-R[0].X)*(S1-1)/S1-CL2,0,R[0].Z-BH+C/2),new pPos(
                    //ACD.DB.DrawPolyline(=B-Clip|View=1|ARR=R[0].X+(R[1].X-R[0].X)*(S1-1)/S1,0,R[0].Z-C/2[CLP2,0,0]|VERTS=R[0].X+(R[1].X-R[0].X)/S1,0,R[0].Z-C/2),new pPos(R[0].X+(R[1].X-R[0].X)/S1,0,R[0].Z-BH+C/2),new pPos(
                }
            }
        }

        static void _buildDimByAxis(PosCollection pls, int axis)
        {
            int nex = (axis + 1) % 2;

            double min_v = 0, max_v = 0;
            pPos min_p = pls[0][0];
            
            for (int i = 0; i < pls.Count; i++)
            {
                double a = Math.Min(pls[i][0][axis], pls[i][1][axis]);
                double b = Math.Max(pls[i][0][axis], pls[i][1][axis]);

                if (i == 0)
                    min_v = a;
                else if (a - max_v > 100 || i == pls.Count - 1)
                {
                    pPos p1 = new pPos(0, 0), p2 = new pPos(0,0);
                    p1[nex] = min_p[nex];
                    p2[nex] = min_p[nex];

                    p1[axis] = min_v;
                    p2[axis] = i == pls.Count - 1 ? b : max_v;

                    double spacing = pls[i][2].DistanceTo(p1, p2) + SPACING;

                    if ((axis == 0 && p1[nex] > pls[i][2][nex]) 
                        || (axis == 1 && p1[nex] < pls[i][2][nex]))
                        spacing *= -1;

                    pPos p3 = p1.Parallel(p2, spacing)[0];

                    IDimChain.CreateDimension(ACD.DB, p1, p2, p3);

                    if(i < pls.Count - 1)
                    {
                        p1[axis] = max_v;
                        p2[axis] = a;

                        IDimChain.CreateDimension(ACD.DB, p1, p2, p3,"",0);
                    }

                    min_v = a;
                    min_p = pls[i][0];
                }

                max_v = b;
            }
        }
        
        static double _getPlotZoneScale(pPos pt)
        {
            pPos[] zone = ACD.DB.GetDrawingZone(pt);
            
            if(zone == null)
                ACD.WR("Cannot recognize plot zone...");
            return zone != null ? zone.BoundScale(GP.PAGESIZE.X, GP.PAGESIZE.Y).ToNumber() : 1;
        }

        static double SPACING = 500;

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                
                ObjectIdCollection selIds = ACD.GetSelection()._filterDXF("DIMENSION");
                PosCollection dimPts = new PosCollection();

                if (selIds.Count > 0)
                {
                    //ACD.WR("Scale {0}", _getPlotZoneScale(ACD.DB._getBound(selIds)[0]));
                    SPACING = pString.INI_Value("DIM_SPACING") 
                        / _getPlotZoneScale(ACD.DB._getBound(selIds)[0]);

                    foreach (ObjectId selId in selIds)
                    {
                        pPos p1 = ACD.DB._getPoint(selId, 0);
                        pPos p2 = ACD.DB._getPoint(selId, 1);
                        pPos p3 = ACD.DB._getPoint(selId, 2);
                        p1.Rotation = ACD.DB._getRotation(selId);

                        dimPts.Add(new pPos[] { p1, p2, p3 });
                    }

                    dimPts = dimPts.OrderBy(ls => ls[0].Rotation).ToCollectionSameClosed();
                    double[] angles = dimPts.Select(ls => ls[0].Rotation).Distinct().ToArray();

                    foreach (double angle in angles)
                    {
                        int axis = angle == 0 ? 0 : 1;
                        PosCollection pls = dimPts.Where(ls
                            => ls[0].Rotation == angle).OrderBy(ls => ls[0][axis]).ToCollectionSameClosed();
                        _buildDimByAxis(pls, axis);
                    }
                }

                ACD.Focus();
            }
        }
    }
}

