using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Internal;

namespace AcadScript
{
    public class IBeamCLS
    {
        double _CR = 75, _TXT = 1.8;
        void NoteBeamCut(string txt, double x, double y)
        {
            pPos pt = new pPos(x, y);
            ACD.DB.Draw2D(pt, pt.X, pt.Y + 120, "G-Text", "LWIDTH=2");
            ACD.DB.CreateText(txt, pt + new pPos(-100, 0), _TXT, 0, GP.DEF_LAYER_TEXT);
        }

        void NoteSteel(string txt1, string txt2, pPos pt, int type,
            bool has_text = true, bool has_pointed = true, double len = 500)
        {
            pPos p1 = new pPos(-25, 25), p2 = new pPos(100, 250), p3 = new pPos(len, 250),
                p4 = new pPos(len + _CR, 250), p5 = new pPos(len - 400, 260);

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
                p4.X = -len - _CR;
                p5.X = -len;
            }

            if (has_pointed)
                ACD.DB.Draw2D(p1.Invert + pt, p1 + pt, GP.DEF_LAYER_TEXT, "LWIDTH=2");

            if (has_text)
            {
                ACD.DB.Draw2D(pt, p2 + pt, p3 + pt, GP.DEF_LAYER_TEXT);
                ACD.DB.DrawCircle(pt + p4, _CR, GP.DEF_LAYER_TEXT);
                ACD.DB.CreateText(txt1, pt + p4, _TXT, 0, GP.DEF_LAYER_TEXT);
                ACD.DB.CreateText(txt2, pt + p5, _TXT, 0, GP.DEF_LAYER_TEXT);
            }
            else
                ACD.DB.Draw2D(pt, p2 + pt, (p2 + p3) / 2 + pt, GP.DEF_LAYER_TEXT);
        }

        void BeamSection(pPos pt, int mat_cat_index,
            int thep_tren_num, int thep_duoi_num,
            int thep_tren_index, int thep_duoi_index,
            int thep_dai_index, int slab_cote_type = 0) // 0:Top, 1:Bottom
        {
            double A = C / 2, L = 700;
            ObjectIdCollection cirIds = new ObjectIdCollection();
            int total = (int)((double)NSM / 2).roundNumber();
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

                            ACD.DB.Dim2D(0, 0, p2, "S" + -F / 4, "$" + pt);
                            ACD.DB.Dim2D(p2, 0, -BH, "S" + -F / 4, "$" + pt);
                        }

                        if (index == 1) p.Y = total > 1 ? -BH + A + C + STM / 2 : (-BH + A + STM / 2);

                        p += pt;

                        cirIds.AddRange(ACD.DB.DrawCircle(p, STM / 2, "LAYER=B-Steel"));
                        NoteSteel("#M" + note_index, "#L" + total + "%%C" + STM, p, 3, i == 0, false, L);
                    }
            }

            NoteSteel("#MD", "%%C" + STC + "a"
                + (mat_cat_index % 2 == 0 ? CLP1 : CLP2), new pPos(A, -BH / 2) + pt, 4, true, false, L);

            //ACD.DB.MoveObject(cirIds, pt);
            ObjectIdCollection hIds = ACD.DB.DrawHatchFromIds(cirIds, "HATCH=SOLID");
            ACD.DB._setLayer(hIds, "B-Steel");

            double SLB = 100;

            if (slab_cote_type == 0)
                ACD.DB.Draw2D(BW + SLB, 0, 0, 0, 0, -BH, BW, -BH, BW, -SLB, BW + SLB, -SLB, "$" + pt, "0");
            else if (slab_cote_type == 1)
                ACD.DB.Draw2D(BW + SLB, -SLB, BW, -SLB, BW, 0, 0, 0, 0, -BH, BW + SLB, -BH, "$" + pt, "0");

            ACD.DB.Draw2D(new pPos(A, -A).RectToPoint(new pPos(BW - A, -BH + A)).Move(pt), "C",
                "B-Clip", "F 0" + STM, "F 1" + STM, "F 2" + STM, "F 3" + STM);
            ACD.DB.DrawBreakLine(new pPos(BW + SLB, 0) + pt, new pPos(BW + SLB, -SLB) + pt, A);

            ACD.DB.CreateText("{\\L" + mat_cat_index + "-" + mat_cat_index + "}",
                new pPos(0, -BH - F * 1.5) + pt, _TXT * 2, 0, "ANNO=TRUE");
            ACD.DB.CreateText("TL:1/25", new pPos(0, -BH - F * 1.8) + pt, _TXT, 0, "ANNO=TRUE");

            ACD.DB.Dim2D(0, 0, 0, -BH, "$" + pt, "S" + -0.5 * F);
            ACD.DB.Dim2D(0, -BH, BW, -BH, "$" + pt, "S" + (-F));
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
        public double BL = 0;

        public pPos[] R;

        string _thep_dai_text(double clip, int thep_dai_index)
        {
            return "<>\r\n%%C" + STC + "a" + clip + "{\\H1.5x;";// (" + thep_dai_index + ")}";
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

        double thep_tren_trai { get { return BL / S1; } }
        double thep_tren_phai { get { return BL * (S1 - 1) / S1; } }
        double thep_duoi_trai { get { return BL / S2; } }
        double thep_duoi_phai { get { return BL * (S2 - 1) / S2; } }

        public void DrawBeam(PosCollection pls)
        {
            double ext_beam_section = 3.6 * F + BW;

            //ObjectIdCollection res = new ObjectIdCollection();
            Database db = ACD.DB;

            double SUM = pls.Sum(ls => ls.Boundary().Size().X);
            R = pls.Boundary;

            List<int> mat_cat_indexes = new List<int>();

            BL = R[1].X - R[0].X;
            BH = R[1].Y - R[0].Y;

            //THÉP CHỦ TRÊN



            //ACD.WR("Draw 2D-A");
            db.Draw2D(SUM - 2 * C, 4 * C,
                            SUM - C, 2 * C,
                            SUM - C, -C + BH,
                            C, -C + BH,
                            C, 2 * C,
                            2 * C, 4 * C,
                        "$" + R[0], "B-Main", "LWIDTH=2", "F 3 " + C, "F 2 " + C);

            //THÉP CHỦ DƯỚI
            db.Draw2D(SUM - 4 * C, +1.5 * C, SUM - C, C,
                            C, C, 4 * C, 1.5 * C,
                            "$" + R[0], "B-Main", "LWIDTH=2", "F 3 " + C, "F 2 " + C);

            //ACD.WR("Draw 2D-B");

            db.Dim2D(R[0].X, R[1].Y, R[1].X, R[1].Y, "S" + (F * 1.36));

            //DIM TỔNG Y
            //db.Dim2D(R[0], R[0].X, R[1].Y, "S" + (F * 0.5));

            int thep_tren_index = 3, thep_duoi_index = 4, mat_cat_index = 2, thep_tren_num = NST,
                    thep_duoi_num = NSB, thep_dai_index = (pls.Count + 1) * 2;

            pPos beam_section_pt = pls.Boundary[0] + new pPos(BL + F * 3, BH);


            bool size_large = pls.All(_ls => _ls.Size().X > 1500);
            thep_tren_num = NST + (pls.Any(_ls => _ls.Size().X >= 4000) ? 0 : -1);
            thep_duoi_num = NSB + (pls.Any(_ls => _ls.Size().X >= 4000) ? 0 : -1);

            for (int _i = 0; _i < pls.Count; _i++)
            {
                pPos[] ls = pls[_i];

                R = ls.Boundary();

                BL = R[1].X - R[0].X;
                BH = R[1].Y - R[0].Y;

                if (size_large)
                {
                    //THÉP LƯNG TRÁI
                    if (R[0].X - pls.Boundary[0].X < 100)
                        db.Draw2D(2.5 * C, 6 * C,
                                2 * C, 3 * C,
                                2 * C, BH - 2 * C,
                                2 * C, BH - 2 * C,
                                thep_tren_trai, BH - 2 * C,
                                thep_tren_trai - 3 * C, BH - 2.5 * C,
                                "$" + R[0], "B-Support", "LWIDTH=2", "F 3 " + C);
                    else
                        db.Draw2D(thep_tren_trai - 4 * C, BH - 2.5 * C,
                                thep_tren_trai, BH - 2 * C,
                                0, BH - 2 * C,
                                "$" + R[0], "B-Support", "LWIDTH=2");

                    //THÉP LƯNG PHẢI
                    if (pls.Boundary[1].X - R[1].X < 100)
                        db.Draw2D(BL - 2.5 * C, 6 * C,
                            BL - 2 * C, 3 * C,
                            BL - 2 * C, BH - 2 * C,
                            thep_tren_phai, BH - 2 * C,
                            thep_tren_phai + 3 * C, BH - 2.5 * C,
                            "$" + R[0], "B-Support", "LWIDTH=2", "F 2 " + C);
                    else
                        db.Draw2D(BL, -2 * C,
                            thep_tren_phai, -2 * C,
                            thep_tren_phai + 4 * C, -2.5 * C,
                            "$" + R[0].X + " " + R[1].Y, "B-Support", "LWIDTH=2");

                    //THÉP BỤNG


                    db.Draw2D(thep_duoi_phai - 3 * C, 2.5 * C,
                            thep_duoi_phai, 2 * C,
                            thep_duoi_trai, 2 * C,
                            thep_duoi_trai + 3 * C, 2.5 * C,
                            "$" + R[0], "B-Support", "LWIDTH=2");

                    //DIMENSION
                    //TOP DIM PHI 6 A 100
                    db.Dim2D(0, BH, thep_tren_trai, BH,
                        "T" + _thep_dai_text(CLP1, thep_dai_index),
                        "$" + R[0], "S" + F);

                    db.Dim2D(thep_tren_trai, BH, thep_tren_phai, BH,
                        "T" + _thep_dai_text(CLP2, thep_dai_index),
                        "$" + R[0], "S" + F);

                    db.Dim2D(thep_tren_phai, BH, BL, BH,
                        "T" + _thep_dai_text(CLP1, thep_dai_index),
                        "$" + R[0], "S" + F);

                    db.Dim2D(0, BH, BL, BH, "S-" + F * 1.8, "$" + R[0]);

                    db.Dim2D(thep_duoi_trai, BH, 0, BH, "S-" + F * 1.5, "$" + R[0]);
                    db.Dim2D(thep_duoi_phai, BH, thep_duoi_trai, BH, "S-" + F * 1.5, "$" + R[0]);
                    db.Dim2D(BL, BH, thep_duoi_phai, BH, "S-" + F * 1.5, "$" + R[0]);

                    double b = R[0].X + (BL + BL * (S2 - 1)) / (S2 * 2);

                    //ghi chu thep tren
                    NoteSteel("#M1", "#L" + ((double)NSM / 2).roundNumber() + "%%C" + STM, new pPos((R[0].X + R[1].X) / 2, R[1].Y - C), 0);
                    NoteSteel("#M" + thep_tren_index, "#L" + thep_tren_num + "%%C" + STM, new pPos(R[0].X + BL / (S1 * 2), R[1].Y - 2 * C), 0);

                    //ghi chu thep duoi
                    NoteSteel("#M2", "#L" + ((double)NSM / 2).roundNumber() + "%%C" + STM, new pPos(R[0].X + F, R[0].Y + C), 1);
                    NoteSteel("#M" + thep_duoi_index, "#L" + thep_duoi_num + "%%C" + STM, new pPos((R[0].X + R[1].X) / 2, R[0].Y + 2 * C), 1);

                    NoteBeamCut(mat_cat_index.ToString(), R[0].X + BL / (S2 * 2), R[1].Y + F * 1.5);
                    NoteBeamCut(mat_cat_index.ToString(), R[0].X + BL / (S2 * 2), R[0].Y - F * 1.5);

                    //TAO MAT CAT
                    if (!mat_cat_indexes.Contains(mat_cat_index) && mat_cat_index % 2 == 0)
                    {
                        BeamSection(beam_section_pt, mat_cat_index,
                            thep_tren_num, 0,
                                thep_tren_index, thep_duoi_index, thep_dai_index, 0);
                        mat_cat_indexes.Add(mat_cat_index);
                    }

                    beam_section_pt.X += ext_beam_section;
                    mat_cat_index++;
                    if (mat_cat_index > 3) mat_cat_index = 2;

                    if (mat_cat_index > 3)
                        mat_cat_index = 2;

                    NoteBeamCut(mat_cat_index.ToString(), b, R[1].Y + F * 1.5);
                    NoteBeamCut(mat_cat_index.ToString(), b, R[0].Y - F * 1.5);

                    if (!mat_cat_indexes.Contains(mat_cat_index) && mat_cat_index % 2 == 1)
                    {
                        BeamSection(beam_section_pt, mat_cat_index,
                            0, thep_duoi_num,
                            thep_tren_index, thep_duoi_index, thep_dai_index, 0);
                        mat_cat_indexes.Add(mat_cat_index);
                    }

                    beam_section_pt.X += ext_beam_section;
                    mat_cat_index++;
                    if (mat_cat_index > 3) mat_cat_index = 2;

                    if (mat_cat_index > 3)
                        mat_cat_index = 2;

                    //thep_duoi_index += 2;
                    //thep_tren_index += 2;

                    if (pls.Boundary[1].X - R[1].X < 100)
                        NoteSteel("#M" + thep_tren_index, "#L" + thep_tren_num + "%%C" + STM,
                            new pPos(R[1].X - BL / (S1 * 2), R[1].Y - C * 2), 0);

                    NoteBeamCut(mat_cat_index.ToString(), R[1].X - BL / (S2 * 2), R[1].Y + F * 1.5);
                    NoteBeamCut(mat_cat_index.ToString(), R[1].X - BL / (S2 * 2), R[0].Y - F * 1.5);

                    if (!mat_cat_indexes.Contains(mat_cat_index) && mat_cat_index % 2 == 0)
                    {
                        BeamSection(beam_section_pt, mat_cat_index,
                            thep_tren_num, 0,
                                thep_tren_index, thep_duoi_index, thep_dai_index, 0);
                        mat_cat_indexes.Add(mat_cat_index);
                    }
                }
                else
                {
                    //THÉP LƯNG TRÁI

                    if (R[0].X - pls.Boundary[0].X < 100)
                    {
                        db.Draw2D(C * 4, 3 * C,
                                C * 3, 2 * C,
                                C * 3, 2 * C,
                                C * 3, BH - 2 * C,
                                BL, BH - 2 * C,
                                "$" + R[0], "B-Support", "LWIDTH=2", "F 2 " + C);
                        mat_cat_index = 1;

                        NoteBeamCut(mat_cat_index.ToString(), (R[0].X + R[1].X) / 2, R[1].Y + F * 1.5);
                        NoteBeamCut(mat_cat_index.ToString(), (R[0].X + R[1].X) / 2, R[0].Y - F * 1.5);
                        BeamSection(beam_section_pt, mat_cat_index,
                            thep_tren_num, 0,
                                thep_tren_index, thep_duoi_index, thep_dai_index, 0);

                        beam_section_pt.X += ext_beam_section;
                        mat_cat_index++;
                    }
                    else if (pls.Boundary[1].X - R[1].X < 100)
                    {
                        db.Draw2D(BL - C * 4, 3 * C,
                                BL - C * 3, 2 * C,
                                BL - C * 3, 2 * C,
                                BL - C * 3, BH - 2 * C,
                                0, BH - 2 * C,
                                "$" + R[0], "B-Support", "LWIDTH=2", "F 3 " + C);

                        NoteBeamCut(mat_cat_index.ToString(), (R[0].X + R[1].X) / 2, R[1].Y + F * 1.5);
                        NoteBeamCut(mat_cat_index.ToString(), (R[0].X + R[1].X) / 2, R[0].Y - F * 1.5);

                        if (!mat_cat_indexes.Contains(mat_cat_index))
                        {
                            BeamSection(beam_section_pt, mat_cat_index,
                                thep_tren_num, 0,
                                    thep_tren_index, thep_duoi_index, thep_dai_index, 0);
                            mat_cat_indexes.Add(mat_cat_index);
                        }

                        beam_section_pt.X += ext_beam_section;
                        //mat_cat_index++;
                    }

                    double y1 = R[1].Y - C / 2, y2 = R[0].Y + C / 2;

                    //for (double i = R[0].X + CLP1; i <= R[1].X - CLP1; i += CLP1)
                    //    ACD.DB.Draw2D(i, y1, i, y2, "B-Clip");

                    db.Dim2D(0, BH, BL, BH, "T" + _thep_dai_text(CLP1, thep_dai_index), "$" + R[0], "S" + F);
                    db.Dim2D(0, BH, BL, BH, "$" + R[0], "S-" + F * 1.8);
                    db.Dim2D(BL, BH, 0, BH, "$" + R[0], "S-" + F * 1.5);
                }

                //TAO THEP DAI
                _drawClip();
            }

            //ACD.DB.CreateText(pls.Name, R[0], 4);
            //ACD.DB.DrawPolyline(pls.First().Boundary()[0].RectToPoint(pls.Last().Boundary()[1]), true, "0");

        }

        void _drawClip()
        {
            double y1 = R[0].Y + C / 2;
            double y2 = R[1].Y - C / 2;

            for (double i = 0; i < 5; i++)
            {
                double curx = R[0].X - (4 - i) * CLP1 + thep_tren_trai;
                ACD.DB.Draw2D(curx, y1, curx, y2, "B-Clip");

                curx = R[0].X + i * CLP1 + thep_tren_phai;
                ACD.DB.Draw2D(curx, y1, curx, y2, "B-Clip");

                curx = (R[0].X + R[1].X) / 2 + (i - 2) * CLP2;
                ACD.DB.Draw2D(curx, y1, curx, y2, "B-Clip");
            }
        }
    }
}
