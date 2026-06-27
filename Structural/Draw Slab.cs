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
    public class DrawSlabCLS
    {
        static double SLB = "<SLB/>".ToNumber(50);// độ dày sàn
        static int FSLB = (int)"<FSLB/>".ToNumber(2); //đoạn sàn đầu là âm hay dương : 1:dương, -1:âm
        static int LSLB = (int)"<LSLB/>".ToNumber(2); //đoạn sàn cuối là âm hay dương
        static pPos FBS = pPos.FromString("<FBW/>"); //kích thước dầm đầu
        static pPos LBS = pPos.FromString("<LBS/>"); //kích thước dầm cuối
        static pPos BS = pPos.FromString("<BS/>"); //kích thước dầm trong
        static PosCollection BSS = new PosCollection("<BS/>"); //mảng data chứa kích thước tất cả dầm

        static pPos FBAL = pPos.FromString("<FBAL/>"); //vị trí dầm: -1:trái, 0: ở giữa, 1: phải
        static pPos LBAL = pPos.FromString("<LBAL/>"); //kích thước dầm cuối
        static string BALS = "<BALS/>"; //mảng data chứa kích thước tất cả dầm

        static double BH = "<BH/>".ToNumber(400);
        static pPos[] R;
                
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                PosCollection pls = new PosCollection("<REGION/>");
                Database db = ACD.DB;

                if(BSS.Count == 0)
                {
                    if (BS.IsNull)
                        BS = new pPos(200, 400);

                    List<pPos> ls = new List<pPos>();
                    if (!FBS.IsNull)
                        ls.Add(FBS);
                    else
                        ls.Add(BS);
                    
                    for (int i = 0; i < pls.Count; i++)
                        ls.Add(LBS);

                    if (!LBS.IsNull)
                        ls.Add(LBS);
                    else
                        ls.Add(BS);
                }

                List<int> aligns = new List<int>();

                if (BALS.empty())
                {
                    aligns.Add(-1);

                    if (pls.Count > 2)
                    {
                        aligns.Add(-1);
                        for (int i = 1; i < pls.Count - 1; i++)
                            aligns.Add(0);
                        aligns.Add(1);
                    }
                    aligns.Add(1);
                }
                else
                    aligns = BALS.filter(";,").Select(v => (int)v.ToNumber()).ToList();
                                
                if (pls.Count > 0 && BSS.Count > 0)
                {
                    pPos[] beams = BSS.First;
                    double SUM = pls.Sum(ls => ls.Boundary().Size().X);
                    R = pls.Boundary;

                    List<pPos> region = new List<pPos>();
                    region.Add(R[1]);
                    region.Add(R[0]);

                    pPos p = new pPos(0, 0);

                    for (int i = 0; i < BSS.Count; i++)
                    {
                        if (BALS.Length > i && BALS[i] == -1)
                            p = new pPos(R[0].X, R[1].Y - beams[i].Y);
                        else if (BALS.Length > i && BALS[i] == 0)
                            p = new pPos(R[0].X - beams[i].X / 2, R[1].Y - beams[i].Y);
                        else
                            p = new pPos(R[0].X - beams[i].X, R[1].Y - beams[i].Y);

                        region.Add(p.Clone());
                        p.X += beams[i].X;
                        region.Add(p.Clone());
                        p.Y += beams[i].Y - SLB;
                        region.Add(p.Clone());
                    }

                    ACD.DB.DrawPolyline(region, true, "LAYER=A-Hidden");
                    
                    ACD.Focus();
                }
            }
        }
    }
}


