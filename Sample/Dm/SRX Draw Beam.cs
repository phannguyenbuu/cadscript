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
    public class IBeamCLS
    {
        void NoteBeamCut(string txt, pPos pt)
        {
            ACD.DB.DrawPolyline(new pPos[] { new pPos(0, 0), new pPos(0, 120) }.Move(pt),
                false, "LAYER=G-Text|LWIDTH=5");
            ACD.DB.CreateText(txt, pt + new pPos(-100, 0), 1.8, 0, GP.DEF_LAYER_TEXT);
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
                    false, "LAYER=G-Text|LWIDTH=5");

            if (has_text)
            {
                ACD.DB.DrawPolyline(new pPos[] { new pPos(0, 0), p2, p3 }.Move(pt), false, "LAYER=A-Anno-Dims");
                ACD.DB.DrawCircle(pt + p4, 125, "A-Anno-Dims");
                ACD.DB.CreateText(txt1, pt + p4, 1.8, 0, "A-Anno-Dims");
                ACD.DB.CreateText(txt2, pt + p5, 1.8, 0, "A-Anno-Dims");
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


            int total = 2; // (int)((double)NSM / 2).roundNumber();


            double v = total <= 1 ? BW / 2 : (BW - 2 * A - STM) / (total - 1);

            pPos p = new pPos(0, 0);

            for (int i = 0; i < total; i++)
            {
                p = new pPos(A + STM / 2 + i * v, -A - STM / 2) + pt;
                cirIds.AddRange(ACD.DB.DrawCircle(p, STM / 2, "LAYER=B-Steel"));
                NoteSteel("#M1", "#L" + total + "%%C" + STM, p, 0, i == 0, false, L);

                p = new pPos(A + STM / 2 + i * v, -BH + A + STM / 2) + pt;
                cirIds.AddRange(ACD.DB.DrawCircle(p, STM / 2, "LAYER=B-Steel"));
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

                        cirIds.AddRange(ACD.DB.DrawCircle(p, STM / 2, "LAYER=B-Steel"));
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
            return "<>\r\n%%C" + STC + "a" + clip + "{\\H1.5x;"; // (" + thep_dai_index + ")}";
        }

        public IBeamCLS(double _C, int _NST, int _NSM,
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
        public double Blx //BottomLeftX
        {
            get
            {
                return R[0].X;
            }
        }

        public double Bly //BottomLeftX
        {
            get
            {
                return R[0].Y;
            }
        }

        public double Trx //TopRightX
        {
            get
            {
                return R[1].X;
            }
        }

        public double Try //TopRightX
        {
            get
            {
                return R[1].Y;
            }
        }

        public void DrawBeam(PosCollection pls)
        {
            using (ACD.Lock())
            {
                ACD.WR("<Draw beam>Select region rectangle by CLOSED polyline, LINE is gridline");
                ACD.WR("<Draw beam>Or select grid block and wall objects for beam and TEXT");

                //PosCollection pls = new PosCollection("<REGION/>");
                ObjectIdCollection res = new ObjectIdCollection();
                Database db = ACD.DB;

                //double A = "<A/>".ToNumber(800);
                //double B = "<B/>".ToNumber(300);

                // = 300,  = 50,  = 50,  = 2,  = 4, S2 = 8, CL1 = 200, CL2 = 200, CLP1 = 100, CLP2 = 200, STC = 6, STM = 16, STS = 12, F = 500;

                //if (pls.Count > 0)
                //{
                double SUM = pls.Sum(ls => ls.Boundary().Size().X);
                R = pls.Boundary;

                List<int> mat_cat_indexes = new List<int>();

                double BL = Trx - Blx;
                BH = Try - Bly;

                //THÉP CHỦ TRÊN
                ACD.DB.DrawPolyline(new pPos[] { new pPos(Blx + SUM - 1.5 * C, Bly + 1.5 * C),
                            new pPos(Blx + SUM - C, Bly + C),
                            new pPos(Blx + SUM - C, Bly - C + BH),
                            new pPos(Blx + C,  Bly - C + BH),
                            new pPos(Blx + C, Bly + C), new pPos(Blx + 1.5 * C, Bly + 1.5 * C)},
                        false, "LAYER=B-Main|LWIDTH=5");

                //THÉP CHỦ DƯỚI
                ACD.DB.DrawPolyline(new pPos[] {new pPos(Blx + SUM - 2.5 * C, Bly + BH/2 - C / 2),
                            new pPos(Blx + SUM - 2 * C, Bly+BH/2),
                            new pPos(Blx + SUM - 2 * C, Bly+C),
                            new pPos(Blx + 2 * C, Bly+C),
                            new pPos(Blx + 2 * C, Bly+BH/2),
                            new pPos(Blx + 2.5 * C, Bly+BH/2 - C/2) },
                        false, "LAYER=B-Main|LWIDTH=5");

                IDimChain.CreateDimension(db, new pPos(Blx, Bly),
                            new pPos(Trx, Bly),
                            new pPos(Blx, Bly - F * 2.5));

                //DIM TỔNG Y
                IDimChain.CreateDimension(db, R[0], new pPos(Blx, Try), new pPos(Blx - F, Bly), "", 0);

                int thep_tren_index = 3, thep_duoi_index = 4, mat_cat_index = 2, thep_tren_num = NST,
                        thep_duoi_num = NSB, thep_dai_index = (pls.Count + 1) * 2;
                pPos beam_section_pt = pls.Boundary[0] - new pPos(0, F * 5);

                foreach (pPos[] ls in pls)
                {
                    R = ls.Boundary();
                    BL = Trx - Blx;
                    BH = Try - Bly;

                    if (R.Size().X > 1500)
                    {
                        double thep_tren_trai = Blx + BL / S1;
                        double thep_tren_phai = Blx + BL * (S1 - 1) / S1;

                        //THÉP LƯNG TRÁI
                        if (Blx - pls.Boundary[0].X < 100)
                            db.DrawPolyline(new pPos[] { new pPos(Blx + C * 3.5, Bly + 2.5 * C),
                                    new pPos(Blx + C * 3, Bly + 2 * C),
                                    new pPos(Blx + C * 3, Bly + 2 * C),
                                    new pPos(Blx + C * 3, Bly + BH - 2 * C),
                                    new pPos(thep_tren_trai, Bly + BH - 2 * C),
                                    new pPos(thep_tren_trai - C/2, Bly + BH -  2.5 * C) }, false, "LAYER=B-Support|LWIDTH=5");
                        else
                            db.DrawPolyline(new pPos[] { new pPos(thep_tren_trai - C/2, Bly + BH -  2.5 * C),
                                    new pPos(thep_tren_trai, Bly + BH - 2 * C),
                                    new pPos(Blx, Bly + BH - 1.5 * C) }, false, "LAYER=B-Support|LWIDTH=5");

                        //THÉP LƯNG PHẢI
                        if (pls.Boundary[1].X - Trx < 100)
                            db.DrawPolyline(new pPos[] { new pPos(Trx - C * 3.5, Bly + 2.5 * C),
                                    new pPos(Trx - C * 3, Bly + 2 * C),
                                    new pPos(Trx - C * 3, Bly + 2 * C),
                                    new pPos(Trx - C * 3, Bly + BH - 2 * C),
                                    new pPos(thep_tren_phai, Try - 2 * C),
                                    new pPos(thep_tren_phai + C/2, Bly + BH  - 2.5 * C) }, false, "LAYER=B-Support|LWIDTH=5");
                        else
                            db.DrawPolyline(new pPos[] { new pPos(Trx, Try - 1.5 * C),
                                    new pPos(thep_tren_phai, Try - 2 * C),
                                    new pPos(thep_tren_phai + C, Bly + BH  - 2.5 * C) }, false, "LAYER=B-Support|LWIDTH=5");

                        //THÉP BỤNG
                        double thep_duoi_trai = Blx + BL / S2;
                        double thep_duoi_phai = Blx + BL * (S2 - 1) / S2;

                        db.DrawPolyline(new pPos[] { new pPos(thep_duoi_phai - C/2, Bly + 2.5 * C),
                                new pPos(thep_duoi_phai, Bly + 2 * C),
                                new pPos(thep_duoi_trai, Bly + 2 * C),
                                new pPos(thep_duoi_trai + C/2, Bly + 2.5 * C) }, false, "LAYER=B-Support|LWIDTH=5");

                        //ĐƯỜNG BAO
                        //ACD.DB.DrawPolyline(new pPos[] { new pPos(Blx, Bly, R[0].Z), new pPos(Blx, Try, R[1].Z),
                        //new pPos(Trx, Try, R[1].Z), new pPos(Trx, Bly, R[0].Z) }, true, "B-Hidden");
                        //ACD.DB.DrawPolyline(=B-Support|MOVE=Blx,0|VERTS=0,Try-C,R[0].Z-E),new pPos(BL/S1+CL1,Try-C,R[0].Z-E),new pPos(BL/S1-C+CL1,Try-C,R[0].Z-E-C),new pPos(),new pPos(
                        //ACD.DB.DrawPolyline(=B-Support|MOVE=Blx,0|VERTS=Trx-Blx,Try-C,R[0].Z-E),new pPos(BL*(S1-1)/S1-CL2,Try-C,R[0].Z-E),new pPos(BL*(S1-1)/S1+C-CL2,Try-C,R[0].Z-E-C),new pPos(),new pPos(
                        //ACD.DB.DrawPolyline(=B-Support|MOVE=Blx,0|VERTS=BL/S2+C+CL1,Try-C,R[1].Z+E+C),new pPos(BL/S2+CL1,Try-C,R[1].Z+E),new pPos(BL*(S2-1)/S2-CL2,Try-C,R[0].Z-BH+E),new pPos(BL*(S2-1)/S2-C-CL2,Try-C,R[0].Z-BH+E+C),new pPos(),new pPos(

                        //DIMENSION
                        //TOP DIM PHI 6 A 100
                        IDimChain.CreateDimension(db, new pPos(Blx, Try), new pPos(thep_tren_trai, Try),
                            new pPos(Blx, Try + F), _thep_dai_text(CLP1, thep_dai_index), 0);

                        IDimChain.CreateDimension(db, new pPos(thep_tren_trai, Try), new pPos(thep_tren_phai, Try),
                            new pPos(Blx + BL / S1 + CL1, Try + F), _thep_dai_text(CLP2, thep_dai_index), 0);

                        IDimChain.CreateDimension(db, new pPos(thep_tren_phai, Try), new pPos(Trx, Try),
                            new pPos(thep_tren_phai, Try + F), _thep_dai_text(CLP1, thep_dai_index), 0);

                        IDimChain.CreateDimension(db, new pPos(Blx, Bly),
                            new pPos(Trx, Bly),
                            new pPos(Blx, Bly - F * 2), "", 0);

                        IDimChain.CreateDimension(db, new pPos(thep_duoi_trai, Bly),
                            new pPos(Blx, Bly),
                            new pPos(thep_duoi_trai, Bly - F * 1.5), "", 0);

                        IDimChain.CreateDimension(db, new pPos(thep_duoi_phai, Bly),
                            new pPos(thep_duoi_trai, Bly - F),
                            new pPos(thep_duoi_phai, Bly - F * 1.5), "", 0);

                        IDimChain.CreateDimension(db, new pPos(Trx, Bly),
                            new pPos(thep_duoi_phai, Bly), new pPos(Trx, Bly - F * 1.5), "", 0);

                        double b = Blx + (BL + BL * (S2 - 1)) / (S2 * 2);

                        thep_tren_num = NST + (BL <= 4000 ? -1 : 0);
                        thep_duoi_num = NSB + (BL <= 4000 ? -1 : 0);

                        //ghi chu thep tren
                        NoteSteel("#M1", "#L" + ((double)NSM / 2).roundNumber() + "%%C" + STM, new pPos((Blx + Trx) / 2, Try - C), 0);
                        NoteSteel("#M" + thep_tren_index, "#L" + thep_tren_num + "%%C" + STM, new pPos(Blx + BL / (S1 * 2), Try - C * 2), 0);



                        //if (pls.Boundary[1].X - Trx < 100)
                        //    NoteSteel("#M" + thep_tren_index, "#L" + NSB + "%%C" + STM, new pPos(Trx - BL / (S1 * 2), Try - C * 2), 0);
                        //thep_tren_index += 2;

                        //#NOTE=NOTE_STEEL_T1|VERTS=(Blx+Trx)/2,Try-C,R[0].Z-C[#M3&#L2 _%%C_ STM]),new pPos(
                        //#NOTE=NOTE_STEEL_T1|VERTS=Blx+BL/(S1*2),Try-C,R[0].Z-C[#M4&#L1 _%%C_ STM]),new pPos(BL*(S1-1)/S1,Bly,R[0].Z-C[#M4&#L1 _%%C_ STM]),new pPos(),new pPos(

                        //ghi chu thep duoi
                        NoteSteel("#M2", "#L" + ((double)NSM / 2).roundNumber() + "%%C" + STM, new pPos(Blx + F, Bly + C), 1);
                        NoteSteel("#M" + thep_duoi_index, "#L" + thep_duoi_num + "%%C" + STM, new pPos((Blx + Trx) / 2, Bly + C * 2), 1);

                        NoteBeamCut(mat_cat_index.ToString(), new pPos(Blx + BL / (S2 * 2), Try + F * 2));
                        NoteBeamCut(mat_cat_index.ToString(), new pPos(Blx + BL / (S2 * 2), Bly - F * 3));

                        //TAO MAT CAT
                        if (!mat_cat_indexes.Contains(mat_cat_index) && mat_cat_index % 2 == 0)
                        {
                            BeamSection(beam_section_pt, mat_cat_index,
                                thep_tren_num, 0,
                                    thep_tren_index, thep_duoi_index, thep_dai_index, 0);
                            mat_cat_indexes.Add(mat_cat_index);
                        }

                        beam_section_pt.X += 5 * F + BW;
                        mat_cat_index = (mat_cat_index + 1) % 2;

                        NoteBeamCut(mat_cat_index.ToString(), new pPos(b, Try + F * 2));
                        NoteBeamCut(mat_cat_index.ToString(), new pPos(b, Bly - F * 3));

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

                        if (pls.Boundary[1].X - Trx < 100)
                            NoteSteel("#M" + thep_tren_index, "#L" + thep_tren_num + "%%C" + STM,
                                new pPos(Trx - BL / (S1 * 2), Try - C * 2), 0);

                        NoteBeamCut(mat_cat_index.ToString(), new pPos(Trx - BL / (S2 * 2), Try + F * 2));
                        NoteBeamCut(mat_cat_index.ToString(), new pPos(Trx - BL / (S2 * 2), Bly - F * 3));

                        if (!mat_cat_indexes.Contains(mat_cat_index) && mat_cat_index % 2 == 0)
                        {
                            BeamSection(beam_section_pt, mat_cat_index,
                                thep_tren_num, 0,
                                    thep_tren_index, thep_duoi_index, thep_dai_index, 0);
                            mat_cat_indexes.Add(mat_cat_index);
                        }

                        //TAO THEP DAI
                        double y1 = Try - C / 2, y2 = Bly + C / 2;

                        for (double i = Blx + CLP1; i <= thep_tren_trai; i += CLP1)
                            ACD.DB.DrawPolyline(new pPos[] { new pPos(i, y1), new pPos(i, y2) }, false, "LAYER=B-Clip");

                        for (double i = thep_tren_phai + CLP1; i <= Trx; i += CLP1)
                            ACD.DB.DrawPolyline(new pPos[] { new pPos(i, y1), new pPos(i, y2) }, false, "LAYER=B-Clip");

                        for (double i = thep_tren_trai + CLP2; i <= thep_tren_phai; i += CLP2)
                            ACD.DB.DrawPolyline(new pPos[] { new pPos(i, y1), new pPos(i, y2) }, false, "LAYER=B-Clip");


                    }
                    else
                    {
                        //THÉP LƯNG TRÁI

                        if (Blx - pls.Boundary[0].X < 100)
                        {
                            db.DrawPolyline(new pPos[] { new pPos(Blx + C * 4, Bly + 3 * C),
                                    new pPos(Blx + C * 3, Bly + 2 * C),
                                    new pPos(Blx + C * 3, Bly + 2 * C),
                                    new pPos(Blx + C * 3, Bly + BH - 2 * C),
                                    new pPos(Trx, Bly + BH - 2 * C) }, false, "LAYER=B-Support|LWIDTH=5");
                            mat_cat_index = 1;

                            NoteBeamCut(mat_cat_index.ToString(), new pPos((Blx + Trx) / 2, Try + F * 2));
                            NoteBeamCut(mat_cat_index.ToString(), new pPos((Blx + Trx) / 2, Bly - F * 3));
                            BeamSection(beam_section_pt, mat_cat_index,
                                thep_tren_num, 0,
                                    thep_tren_index, thep_duoi_index, thep_dai_index, 0);

                            beam_section_pt.X += 5 * F + BW;
                            mat_cat_index++;
                        }
                        else if (pls.Boundary[1].X - Trx < 100)
                        {
                            db.DrawPolyline(new pPos[] { new pPos(Trx - C * 4, Bly + 3 * C),
                                    new pPos(Trx - C * 3, Bly + 2 * C),
                                    new pPos(Trx - C * 3, Bly + 2 * C),
                                    new pPos(Trx - C * 3, Bly + BH - 2 * C),
                                    new pPos(Blx, Bly + BH - 2 * C) }, false, "LAYER=B-Support|LWIDTH=5");

                            NoteBeamCut(mat_cat_index.ToString(), new pPos((Blx + Trx) / 2, Try + F * 2));
                            NoteBeamCut(mat_cat_index.ToString(), new pPos((Blx + Trx) / 2, Bly - F * 3));

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

                        double y1 = Try - C / 2, y2 = Bly + C / 2;

                        for (double i = Blx + CLP1; i <= Trx - CLP1; i += CLP1)
                            ACD.DB.DrawPolyline(new pPos[] { new pPos(i, y1), new pPos(i, y2) }, false, "LAYER=B-Clip");

                        IDimChain.CreateDimension(db, new pPos(Blx, Try),
                            new pPos(Trx, Try), new pPos(Blx, Try + F), _thep_dai_text(CLP1, thep_dai_index), 0);
                        IDimChain.CreateDimension(db, new pPos(Blx, Try),
                            new pPos(Trx, Try), new pPos(Blx, Bly - F * 2), "", 0);
                        IDimChain.CreateDimension(db, new pPos(Trx, Try),
                            new pPos(Blx, Try), new pPos(Blx, Bly - F * 1.5), "", 0);
                    }
                }

                ACD.Focus();
                //}
            }
        }
    }

    public class DrawBeamCLS
    {
        static double C = 25;
        //static double E = "<E/>".ToNumber(50);
        static int NST = 2; //SỐ LƯỢNG THÉP CHỦ TRÊN
        static int NSM = 2; //SỐ LƯỢNG THÉP CHỦ DƯỚI
        static int NSB = 2; //SỐ LƯỢNG THÉP CHỦ DƯỚI
        static double S1 = 4; 
        static double S2 =8;
        static double CL1 = 200; //THÉP ĐAI GỐI
        static double CL2 = 200; //THÉP ĐAI BỤNG
        static double CLP1 = 100; //KHOẢNG CÁCH THÉP ĐAI GỐI
        static double CLP2 = 200; //KHOẢNG CÁCH THÉP ĐAI BỤNG
        static double STC = 6; //THÉP ĐAI
        static double STM = 16;
        static double STS = 12;
        static double F = 500;

        static double BW = 200;
        static double BH = 400;
        static pPos[] R;
        
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    PosCollection pls = ACD.DB._getAllVertices(selIds);
                    Database db = ACD.DB;

                    ACD.WR("pls {0}", pls.Count);

                    if (pls.Count > 0)
                    {
                        IBeamCLS beam = new IBeamCLS(C, NST, NSM, NSB, S1, S2, CL1, CL2, CLP1, CLP2, STC, STM, STS, F, BW, BH, R);
                        beam.DrawBeam(pls);

                        ACD.Focus();
                    }
                }
            }
        }
    }
}

