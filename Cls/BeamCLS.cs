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

//
//using System.Windows.Forms;
//using SyncObject;

namespace AcadScript
{
    public class BeamCLS
    {
        void NoteBeamCut(string txt, pPos pt)
        {
            ACD.DB.DrawPolyline(new pPos[] { new pPos(0, 0), new pPos(0, 120) }.Move(pt),
                false, "LAYER=G-Text|LWIDTH=10");
            ACD.DB.CreateText(txt, pt + new pPos(-100, 0), 2.5, 0, GP.DEF_LAYER_TEXT);
        }

        void NoteSteel(string txt1, string txt2, pPos pt, int type,
            bool has_text = true, bool has_pointed = true, double len = 500)
        {
            pPos p1 = new pPos(-25, 25), p2 = new pPos(100, 250), p3 = new pPos(len, 250),
                p4 = new pPos(len + 125, 250), p5 = new pPos(len - 400, 260);

            if (type == 1)
            {
                p2 = p2.Invert;
                p3 = p3.Invert;
                p4 = p4.Invert;
                p5 = new pPos(-len + 50, -260);
            }
            else if (type == 2)
            {
                p2 = p2.Invert;
                p3 = p3.Invert;
                p4 = new pPos(475, 100);
                p5 = new pPos(0, 110);
            }
            else if (type == 3)
            {
                p2.Y = p3.Y = p4.Y = p5.Y = 0;
            }
            else if (type == 4)
            {
                p2.Y = p3.Y = p4.Y = p5.Y = 0;
                p2.X = -len;
                p3.X = -len;
                p4.X = -len - 125;
                p5.X = -len;
            }

            if (has_pointed)
                ACD.DB.DrawPolyline(new pPos[] { p1.Invert, p1 }.Move(pt),
                    false, "LAYER=G-Text|LWIDTH=10");

            if (has_text)
            {
                ACD.DB.DrawPolyline(new pPos[] { new pPos(0, 0), p2, p3 }.Move(pt), false, GP.DEF_LAYER_TEXT);
                ACD.DB.DrawCircle(pt + p4, 0.5, GP.DEF_LAYER_TEXT);
                ACD.DB.CreateText(txt1, pt + p4, 2.5, 0, GP.DEF_LAYER_TEXT);
                ACD.DB.CreateText(txt2, pt + p5, 2.5, 0, GP.DEF_LAYER_TEXT);
            }
            else
                ACD.DB.DrawPolyline(new pPos[] { new pPos(0, 0), p2, (p2 + p3) / 2 }.Move(pt), false, GP.DEF_LAYER_TEXT);
        }

        void BeamSection(pPos pt, int mat_cat_index,
            int thep_tren_num, int thep_duoi_num,
            int thep_tren_index, int thep_duoi_index,
            int thep_dai_index, int slab_cote_type = 0) // 0:Top, 1:Bottom
        {
            pPos[] region = new pPos[0];
            double SLB = 100;

            if (slab_cote_type == 0)
                region = new pPos[] { new pPos(BW + SLB, 0), new pPos(0, 0), new pPos(0, -BH),
                    new pPos(BW, -BH), new pPos(BW, -SLB), new pPos(BW + SLB, -SLB) };
            else if (slab_cote_type == 1)
                region = new pPos[] { new pPos(BW + SLB, -SLB), new pPos(BW, -SLB),
                    new pPos(BW, 0), new pPos(0, 0), new pPos(0, -BH), new pPos(BW + SLB, -BH)};

            double A = C / 2, L = 700;
            ObjectIdCollection cirIds = new ObjectIdCollection();
            int total = (int)((double)NSM / 2).roundNumber();
            double v = total <= 1 ? BW / 2 : (BW - 2 * A - STM) / (total - 1);

            pPos p = new pPos(0, 0);

            for (int i = 0; i < total; i++)
            {
                p = new pPos(A + STM / 2 + i * v, -A - STM / 2) + pt;
                cirIds.AddRange(ACD.DB.DrawCircle(p, STM / 20, "LAYER=B-Steel"));
                NoteSteel("#M1", "#L" + total + "%%C" + STM, p, 0, i == 0, false, L);

                p = new pPos(A + STM / 2 + i * v, -BH + A + STM / 2) + pt;
                cirIds.AddRange(ACD.DB.DrawCircle(p, STM / 20, "LAYER=B-Steel"));
                NoteSteel("#M2", "#L" + total + "%%C" + STM, p, 1, i == 0, false, L);
            }

            

            for (int index = 0; index < 2; index++)
            {
                total = index == 0 ? thep_tren_num : thep_duoi_num;
                int note_index = index == 0 ? thep_tren_index : thep_duoi_index;

                if (total != 0)
                    for (int i = 0; i < total; i++)
                    {
                        p = new pPos(A + STM / 2 + i * (BW - 2 * A - STM) / (total - 1),
                            total > 1 ? -A - C - STM / 2 : (-A - STM / 2));

                        if (total == 1)
                            p.X = BW / 2;
                        else
                        {
                            pPos p2 = new pPos(0, -C - A);
                            if (index == 1)
                                p2.Y = -BH + C + A;
                            p2 += pt;
                            IDimChain.CreateDimension(ACD.DB, new pPos(0, 0) + pt, p2, new pPos(-F * 2, 0) + pt, "", 0);
                            IDimChain.CreateDimension(ACD.DB, p2, new pPos(0, -BH) + pt, new pPos(-F * 2, 0) + pt, "", 0);
                        }

                        if (index == 1) p.Y = total > 1 ? -BH + A + C + STM / 2 : (-BH + A + STM / 2);

                        p += pt;

                        cirIds.AddRange(ACD.DB.DrawCircle(p, STM / 20, "LAYER=B-Steel"));
                        NoteSteel("#M" + note_index, "#L" + total + "%%C" + STM, p, 3, i == 0, false, L);
                    }
            }

            NoteSteel("#M" + thep_dai_index, "%%C" + STC + "a" + CLP1, new pPos(A, -BH / 2) + pt, 4, true, false, L);

            //ACD.DB.MoveObject(cirIds, pt);
            ObjectIdCollection hIds = ACD.DB.DrawHatchFromIds(cirIds, "HATCH=SOLID");
            ACD.DB._setLayer(hIds, "B-Steel");

            ACD.DB.DrawPolyline(region.Move(pt), false, "LAYER=0");
            ACD.DB.DrawPolyline(new pPos(A, -A).RectToPoint(new pPos(BW - A, -BH + A)).Move(pt), true, "LAYER=B-Clip|ROUND=" + STM);
            ACD.DB.DrawBreakLine(new pPos(BW + SLB, 0) + pt, new pPos(BW + SLB, -SLB) + pt, A);

            ACD.DB.CreateText("{\\L" + mat_cat_index + "-" + mat_cat_index + "}",
                new pPos(0, -BH - F * 2) + pt, 4, 0, "ANNO=TRUE|LAYER=G-Text");
            ACD.DB.CreateText("TL:1/50", new pPos(0, -BH - F * 2.5) + pt, 2.5, 0, "ANNO=TRUE|LAYER=G-Text");

            IDimChain.CreateDimension(ACD.DB, new pPos(0, 0) + pt, new pPos(0, -BH) + pt, new pPos(-2.5 * F, 0) + pt, "", 0);
            IDimChain.CreateDimension(ACD.DB, new pPos(0, -BH) + pt, new pPos(BW, -BH) + pt, new pPos(BW, -BH - F) + pt, "", 0);
        }

        public double C = "<C/>".ToNumber(50);
        //static double E = "<E/>".ToNumber(50);
        public int NST = (int)"<NST/>".ToNumber(2); //SỐ LƯỢNG THÉP CHỦ TRÊN
        public int NSM = (int)"<NSM/>".ToNumber(2); //SỐ LƯỢNG THÉP CHỦ DƯỚI
        public int NSB = (int)"<NSB/>".ToNumber(2); //SỐ LƯỢNG THÉP CHỦ DƯỚI
        public double S1 = "<S1/>".ToNumber(4);
        public double S2 = "<S2/>".ToNumber(8);
        public double CL1 = "<CL1/>".ToNumber(200); //THÉP ĐAI GỐI
        public double CL2 = "<CL2/>".ToNumber(200); //THÉP ĐAI BỤNG
        public double CLP1 = "<CLP1/>".ToNumber(100); //KHOẢNG CÁCH THÉP ĐAI GỐI
        public double CLP2 = "<CLP2/>".ToNumber(200); //KHOẢNG CÁCH THÉP ĐAI BỤNG
        public double STC = "<STC/>".ToNumber(6); //THÉP ĐAI
        public double STM = "<STM/>".ToNumber(16);
        public double STS = "<STS/>".ToNumber(12);
        public double F = "<F/>".ToNumber(500);

        public double BW = "<BW/>".ToNumber(200);
        public double BH = "<BH/>".ToNumber(400);
        public pPos[] R;

        string _thep_dai_text(double clip, int thep_dai_index)
        {
            return "<>\r\n%%C" + STC + "a" + clip + "{\\H1.5x; (" + thep_dai_index + ")}";
        }

        public BeamCLS(double _C, int _NST, int _NSM,
            int _NSB, double _S1, double _S2, double _CL1, double _CL2,
            double _CLP1, double _CLP2, double _STC, double _STM, double _STS,
            double _F, double _BW, double _BH, pPos[] _R)
        {
            C = _C;
            //static double E = "<E/>".ToNumber(50);
            NST = _NST;
            NSM = _NSM;
            NSB = _NSB;
            S1 = _S1;
            S2 = _S2;
            CL1 = _CL1; //THÉP ĐAI GỐI
            CL2 = _CL2; //THÉP ĐAI BỤNG
            CLP1 = _CLP1; //KHOẢNG CÁCH THÉP ĐAI GỐI
            CLP2 = _CLP2; //KHOẢNG CÁCH THÉP ĐAI BỤNG
            STC = _STC; //THÉP ĐAI
            STM = _STM;
            STS = _STS;
            F = _F;

            BW = _BW;
            BH = _BH;

            R = _R;
        }

        public void DrawBeam(PosCollection pls)
        { 
            using (ACD.Lock())
            {
                //ACD.WR("<Draw beam>Select region rectangle by CLOSED polyline, LINE is gridline");
                //ACD.WR("<Draw beam>Or select grid block and wall objects for beam and TEXT");

                //PosCollection pls = new PosCollection("<REGION/>");
                Database db = ACD.DB;

                //double A = "<A/>".ToNumber(800);
                //double B = "<B/>".ToNumber(300);

                // = 300,  = 50,  = 50,  = 2,  = 4, S2 = 8, CL1 = 200, CL2 = 200, CLP1 = 100, CLP2 = 200, STC = 6, STM = 16, STS = 12, F = 500;

                //if (pls.Count > 0)
                //{
                    double SUM = pls.Sum(ls => ls.Boundary().Size().X);
                    R = pls.Boundary;

                    List<int> mat_cat_indexes = new List<int>();

                    double BL = R[1].X - R[0].X;
                    BH = R[1].Y - R[0].Y;

                    //THÉP CHỦ TRÊN
                    ACD.DB.DrawPolyline(new pPos[] { new pPos(R[0].X + SUM - 2 * C, R[0].Y + 2 * C),
                            new pPos(R[0].X + SUM - C, R[0].Y + C),
                            new pPos(R[0].X + SUM - C, R[0].Y - C + BH),
                            new pPos(R[0].X + C,  R[0].Y - C + BH),
                            new pPos(R[0].X + C, R[0].Y + C), new pPos(R[0].X + 2 * C, R[0].Y + 2 * C)},
                            false, "LAYER=B-Main|LWIDTH=10");

                    //THÉP CHỦ DƯỚI
                    ACD.DB.DrawPolyline(new pPos[] {new pPos(R[0].X + SUM-3 * C, R[0].Y+BH/2-C),
                            new pPos(R[0].X + SUM - 2 * C, R[0].Y+BH/2),
                            new pPos(R[0].X + SUM - 2 * C, R[0].Y+C),
                            new pPos(R[0].X + 2 * C, R[0].Y+C),
                            new pPos(R[0].X + 2 * C, R[0].Y+BH/2),
                            new pPos(R[0].X + 3 * C, R[0].Y+BH/2-C) },
                            false, "LAYER=B-Main|LWIDTH=10");

                    IDimChain.CreateDimension(db, new pPos(R[0].X, R[1].Y),
                                new pPos(R[1].X, R[1].Y),
                                new pPos(R[0].X, R[0].Y - F * 2.5));

                //DIM TỔNG Y
                IDimChain.CreateDimension(db, R[0], new pPos(R[0].X, R[1].Y), new pPos(R[0].X - F, R[0].Y), "", 0);
                
                int thep_tren_index = 3, thep_duoi_index = 4, mat_cat_index = 2, thep_tren_num = NST,
                        thep_duoi_num = NSB, thep_dai_index = (pls.Count + 1) * 2;
                    pPos beam_section_pt = pls.Boundary[0] - new pPos(0, F * 5);

                    foreach (pPos[] ls in pls)
                    {
                        R = ls.Boundary();
                        BL = R[1].X - R[0].X;
                        BH = R[1].Y - R[0].Y;

                        if (R.Size().X > 1500)
                        {
                            double thep_tren_trai = R[0].X + BL / S1;
                            double thep_tren_phai = R[0].X + BL * (S1 - 1) / S1;

                            //THÉP LƯNG TRÁI
                            if (R[0].X - pls.Boundary[0].X < 100)
                                db.DrawPolyline(new pPos[] { new pPos(R[0].X + C * 4, R[0].Y + 3 * C),
                                    new pPos(R[0].X + C * 3, R[0].Y + 2 * C),
                                    new pPos(R[0].X + C * 3, R[0].Y + 2 * C),
                                    new pPos(R[0].X + C * 3, R[0].Y + BH - 2 * C),
                                    new pPos(thep_tren_trai, R[0].Y + BH - 2 * C),
                                    new pPos(thep_tren_trai - C, R[0].Y + BH -  3 * C) }, false, "LAYER=B-Support|LWIDTH=10");
                            else
                                db.DrawPolyline(new pPos[] { new pPos(thep_tren_trai - C, R[0].Y + BH -  3 * C),
                                    new pPos(thep_tren_trai, R[0].Y + BH - 2 * C),
                                    new pPos(R[0].X, R[0].Y + BH - 2 * C) }, false, "LAYER=B-Support|LWIDTH=10");

                            //THÉP LƯNG PHẢI
                            if (pls.Boundary[1].X - R[1].X < 100)
                                db.DrawPolyline(new pPos[] { new pPos(R[1].X - C * 4, R[0].Y + 3 * C),
                                    new pPos(R[1].X - C * 3, R[0].Y + 2 * C),
                                    new pPos(R[1].X - C * 3, R[0].Y + 2 * C),
                                    new pPos(R[1].X - C * 3, R[0].Y + BH - 2 * C),
                                    new pPos(thep_tren_phai, R[1].Y - 2 * C),
                                    new pPos(thep_tren_phai + C, R[0].Y + BH  - 3 * C) }, false, "LAYER=B-Support|LWIDTH=10");
                            else
                                db.DrawPolyline(new pPos[] { new pPos(R[1].X, R[1].Y - 2 * C),
                                    new pPos(thep_tren_phai, R[1].Y - 2 * C),
                                    new pPos(thep_tren_phai + C, R[0].Y + BH  - 3 * C) }, false, "LAYER=B-Support|LWIDTH=10");

                            //THÉP BỤNG
                            double thep_duoi_trai = R[0].X + BL / S2;
                            double thep_duoi_phai = R[0].X + BL * (S2 - 1) / S2;
                            db.DrawPolyline(new pPos[] { new pPos(thep_duoi_phai - C, R[0].Y + 3 * C),
                                new pPos(thep_duoi_phai, R[0].Y + 2 * C),
                                new pPos(thep_duoi_trai, R[0].Y + 2 * C),
                                new pPos(thep_duoi_trai + C, R[0].Y + 3 * C) }, false, "LAYER=B-Support|LWIDTH=10");

                            //ĐƯỜNG BAO
                            //ACD.DB.DrawPolyline(new pPos[] { new pPos(R[0].X, R[0].Y, R[0].Z), new pPos(R[0].X, R[1].Y, R[1].Z),
                            //new pPos(R[1].X, R[1].Y, R[1].Z), new pPos(R[1].X, R[0].Y, R[0].Z) }, true, "B-Hidden");
                            //ACD.DB.DrawPolyline(=B-Support|MOVE=R[0].X,0|VERTS=0,R[1].Y-C,R[0].Z-E),new pPos(BL/S1+CL1,R[1].Y-C,R[0].Z-E),new pPos(BL/S1-C+CL1,R[1].Y-C,R[0].Z-E-C),new pPos(),new pPos(
                            //ACD.DB.DrawPolyline(=B-Support|MOVE=R[0].X,0|VERTS=R[1].X-R[0].X,R[1].Y-C,R[0].Z-E),new pPos(BL*(S1-1)/S1-CL2,R[1].Y-C,R[0].Z-E),new pPos(BL*(S1-1)/S1+C-CL2,R[1].Y-C,R[0].Z-E-C),new pPos(),new pPos(
                            //ACD.DB.DrawPolyline(=B-Support|MOVE=R[0].X,0|VERTS=BL/S2+C+CL1,R[1].Y-C,R[1].Z+E+C),new pPos(BL/S2+CL1,R[1].Y-C,R[1].Z+E),new pPos(BL*(S2-1)/S2-CL2,R[1].Y-C,R[0].Z-BH+E),new pPos(BL*(S2-1)/S2-C-CL2,R[1].Y-C,R[0].Z-BH+E+C),new pPos(),new pPos(

                            //DIMENSION
                            //TOP DIM PHI 6 A 100
                            IDimChain.CreateDimension(db, new pPos(R[0].X, R[1].Y), new pPos(thep_tren_trai, R[1].Y),
                                new pPos(R[0].X, R[1].Y + F), _thep_dai_text(CLP1, thep_dai_index), 0);

                            IDimChain.CreateDimension(db, new pPos(thep_tren_trai, R[1].Y), new pPos(thep_tren_phai, R[1].Y),
                                new pPos(R[0].X + BL / S1 + CL1, R[1].Y + F), _thep_dai_text(CLP2, thep_dai_index), 0);

                            IDimChain.CreateDimension(db, new pPos(thep_tren_phai, R[1].Y), new pPos(R[1].X, R[1].Y),
                                new pPos(thep_tren_phai, R[1].Y + F), _thep_dai_text(CLP1, thep_dai_index), 0);

                            IDimChain.CreateDimension(db, new pPos(R[0].X, R[1].Y),
                                new pPos(R[1].X, R[1].Y),
                                new pPos(R[0].X, R[0].Y - F * 2), "", 0);

                            IDimChain.CreateDimension(db, new pPos(thep_duoi_trai, R[0].Y),
                                new pPos(R[0].X, R[0].Y),
                                new pPos(thep_duoi_trai, R[0].Y - F * 1.5), "", 0);

                            IDimChain.CreateDimension(db, new pPos(thep_duoi_phai, R[0].Y),
                                new pPos(thep_duoi_trai, R[0].Y - F),
                                new pPos(thep_duoi_phai, R[0].Y - F * 1.5), "", 0);

                            IDimChain.CreateDimension(db, new pPos(R[1].X, R[0].Y),
                                new pPos(thep_duoi_phai, R[0].Y), new pPos(R[1].X, R[0].Y - F * 1.5), "", 0);

                            double b = R[0].X + (BL + BL * (S2 - 1)) / (S2 * 2);

                            thep_tren_num = NST + (BL <= 4000 ? -1 : 0);
                            thep_duoi_num = NSB + (BL <= 4000 ? -1 : 0);

                            //ghi chu thep tren
                            NoteSteel("#M1", "#L" + ((double)NSM / 2).roundNumber() + "%%C" + STM, new pPos((R[0].X + R[1].X) / 2, R[1].Y - C), 0);
                            NoteSteel("#M" + thep_tren_index, "#L" + thep_tren_num + "%%C" + STM, new pPos(R[0].X + BL / (S1 * 2), R[1].Y - C * 2), 0);



                            //if (pls.Boundary[1].X - R[1].X < 100)
                            //    NoteSteel("#M" + thep_tren_index, "#L" + NSB + "%%C" + STM, new pPos(R[1].X - BL / (S1 * 2), R[1].Y - C * 2), 0);
                            //thep_tren_index += 2;

                            //#NOTE=NOTE_STEEL_T1|VERTS=(R[0].X+R[1].X)/2,R[1].Y-C,R[0].Z-C[#M3&#L2 _%%C_ STM]),new pPos(
                            //#NOTE=NOTE_STEEL_T1|VERTS=R[0].X+BL/(S1*2),R[1].Y-C,R[0].Z-C[#M4&#L1 _%%C_ STM]),new pPos(BL*(S1-1)/S1,R[0].Y,R[0].Z-C[#M4&#L1 _%%C_ STM]),new pPos(),new pPos(

                            //ghi chu thep duoi
                            NoteSteel("#M2", "#L" + ((double)NSM / 2).roundNumber() + "%%C" + STM, new pPos(R[0].X + F, R[0].Y + C), 1);
                            NoteSteel("#M" + thep_duoi_index, "#L" + thep_duoi_num + "%%C" + STM, new pPos((R[0].X + R[1].X) / 2, R[0].Y + C * 2), 1);

                            NoteBeamCut(mat_cat_index.ToString(), new pPos(R[0].X + BL / (S2 * 2), R[1].Y + F * 2));
                            NoteBeamCut(mat_cat_index.ToString(), new pPos(R[0].X + BL / (S2 * 2), R[0].Y - F * 3));

                            //TAO MAT CAT
                            if (!mat_cat_indexes.Contains(mat_cat_index) && mat_cat_index % 2 == 0)
                            {
                                BeamSection(beam_section_pt, mat_cat_index,
                                    thep_tren_num, 0,
                                        thep_tren_index, thep_duoi_index, thep_dai_index, 0);
                                mat_cat_indexes.Add(mat_cat_index);
                            }

                            beam_section_pt.X += 5 * F + BW;
                            mat_cat_index++;

                            NoteBeamCut(mat_cat_index.ToString(), new pPos(b, R[1].Y + F * 2));
                            NoteBeamCut(mat_cat_index.ToString(), new pPos(b, R[0].Y - F * 3));

                            if (!mat_cat_indexes.Contains(mat_cat_index) && mat_cat_index % 2 == 1)
                            {
                                BeamSection(beam_section_pt, mat_cat_index,
                                    0, thep_duoi_num,
                                    thep_tren_index, thep_duoi_index, thep_dai_index, 0);
                                mat_cat_indexes.Add(mat_cat_index);
                            }

                            beam_section_pt.X += 5 * F + BW;
                            mat_cat_index++;
                            thep_duoi_index += 2;
                            thep_tren_index += 2;

                            if (pls.Boundary[1].X - R[1].X < 100)
                                NoteSteel("#M" + thep_tren_index, "#L" + thep_tren_num + "%%C" + STM,
                                    new pPos(R[1].X - BL / (S1 * 2), R[1].Y - C * 2), 0);

                            NoteBeamCut(mat_cat_index.ToString(), new pPos(R[1].X - BL / (S2 * 2), R[1].Y + F * 2));
                            NoteBeamCut(mat_cat_index.ToString(), new pPos(R[1].X - BL / (S2 * 2), R[0].Y - F * 3));

                            if (!mat_cat_indexes.Contains(mat_cat_index) && mat_cat_index % 2 == 0)
                            {
                                BeamSection(beam_section_pt, mat_cat_index,
                                    thep_tren_num, 0,
                                        thep_tren_index, thep_duoi_index, thep_dai_index, 0);
                                mat_cat_indexes.Add(mat_cat_index);
                            }

                            //TAO THEP DAI
                            double y1 = R[1].Y - C / 2, y2 = R[0].Y + C / 2;

                            for (double i = R[0].X + CLP1; i <= thep_tren_trai; i += CLP1)
                                ACD.DB.DrawPolyline(new pPos[] { new pPos(i, y1), new pPos(i, y2) }, false, "LAYER=B-Clip");

                            for (double i = thep_tren_phai + CLP1; i <= R[1].X; i += CLP1)
                                ACD.DB.DrawPolyline(new pPos[] { new pPos(i, y1), new pPos(i, y2) }, false, "LAYER=B-Clip");

                            for (double i = thep_tren_trai + CLP2; i <= thep_tren_phai; i += CLP2)
                                ACD.DB.DrawPolyline(new pPos[] { new pPos(i, y1), new pPos(i, y2) }, false, "LAYER=B-Clip");


                        }
                        else
                        {
                            //THÉP LƯNG TRÁI

                            if (R[0].X - pls.Boundary[0].X < 100)
                            {
                                db.DrawPolyline(new pPos[] { new pPos(R[0].X + C * 4, R[0].Y + 3 * C),
                                    new pPos(R[0].X + C * 3, R[0].Y + 2 * C),
                                    new pPos(R[0].X + C * 3, R[0].Y + 2 * C),
                                    new pPos(R[0].X + C * 3, R[0].Y + BH - 2 * C),
                                    new pPos(R[1].X, R[0].Y + BH - 2 * C) }, false, "LAYER=B-Support|LWIDTH=10");
                                mat_cat_index = 1;

                                NoteBeamCut(mat_cat_index.ToString(), new pPos((R[0].X + R[1].X) / 2, R[1].Y + F * 2));
                                NoteBeamCut(mat_cat_index.ToString(), new pPos((R[0].X + R[1].X) / 2, R[0].Y - F * 3));
                                BeamSection(beam_section_pt, mat_cat_index,
                                    thep_tren_num, 0,
                                        thep_tren_index, thep_duoi_index, thep_dai_index, 0);

                                beam_section_pt.X += 5 * F + BW;
                                mat_cat_index++;
                            }
                            else if (pls.Boundary[1].X - R[1].X < 100)
                            {
                                db.DrawPolyline(new pPos[] { new pPos(R[1].X - C * 4, R[0].Y + 3 * C),
                                    new pPos(R[1].X - C * 3, R[0].Y + 2 * C),
                                    new pPos(R[1].X - C * 3, R[0].Y + 2 * C),
                                    new pPos(R[1].X - C * 3, R[0].Y + BH - 2 * C),
                                    new pPos(R[0].X, R[0].Y + BH - 2 * C) }, false, "LAYER=B-Support|LWIDTH=10");

                                NoteBeamCut(mat_cat_index.ToString(), new pPos((R[0].X + R[1].X) / 2, R[1].Y + F * 2));
                                NoteBeamCut(mat_cat_index.ToString(), new pPos((R[0].X + R[1].X) / 2, R[0].Y - F * 3));

                                if (!mat_cat_indexes.Contains(mat_cat_index))
                                {
                                    BeamSection(beam_section_pt, mat_cat_index,
                                        thep_tren_num, 0,
                                            thep_tren_index, thep_duoi_index, thep_dai_index, 0);
                                    mat_cat_indexes.Add(mat_cat_index);
                                }

                                beam_section_pt.X += 5 * F + BW;
                                mat_cat_index++;
                            }

                            double y1 = R[1].Y - C / 2, y2 = R[0].Y + C / 2;

                            for (double i = R[0].X + CLP1; i <= R[1].X - CLP1; i += CLP1)
                                ACD.DB.DrawPolyline(new pPos[] { new pPos(i, y1), new pPos(i, y2) }, false, "LAYER=B-Clip");

                            IDimChain.CreateDimension(db, new pPos(R[0].X, R[1].Y),
                                new pPos(R[1].X, R[1].Y), new pPos(R[0].X, R[1].Y + F), _thep_dai_text(CLP1, thep_dai_index), 0);
                            IDimChain.CreateDimension(db, new pPos(R[0].X, R[1].Y),
                                new pPos(R[1].X, R[1].Y), new pPos(R[0].X, R[0].Y - F * 2), "", 0);
                            IDimChain.CreateDimension(db, new pPos(R[1].X, R[1].Y),
                                new pPos(R[0].X, R[1].Y), new pPos(R[0].X, R[0].Y - F * 1.5), "", 0);
                        }
                    }

                    ACD.Focus();
                //}
            }
        }
    }
}

