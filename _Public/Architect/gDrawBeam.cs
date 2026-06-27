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
    public class gDraw2DStruct:gDraw2DDCLS
    {
        public double __display_steel_section = 0.1;
        public gDraw2DStruct(cReadData css) : base(css) { }

        void BeamSection(pPos pt, int mat_cat_index,
            //int thep_tren_num, int thep_duoi_num,
            int thep_tren_index, int thep_duoi_index, int slab_cote_type = 0) // 0:Top, 1:Bottom
        {
            pPos[] region = new pPos[0];
            

            if (slab_cote_type == 0)
                region = new pPos[] { new pPos(BW + SLB, 0), new pPos(0, 0), new pPos(0, -BH),
                    new pPos(BW, -BH), new pPos(BW, -SLB), new pPos(BW + SLB, -SLB) };
            else if (slab_cote_type == 1)
                region = new pPos[] { new pPos(BW + SLB, -SLB), new pPos(BW, -SLB),
                    new pPos(BW, 0), new pPos(0, 0), new pPos(0, -BH), new pPos(BW + SLB, -BH)};

            double A = C / 2;
            //ObjectIdCollection cirIds = new ObjectIdCollection();
            int total = NST;
            double v = total <= 1 ? BW / 2 : (BW - 2 * A - STM) / (total - 1);

            pPos p = new pPos(0, 0);

            p = new pPos(A + STM / 2, -A - STM / 2) + pt;
            AppendCircleHtml("b9-beam-note", p, __display_steel_section);

            p.X += v;
            AppendCircleHtml("b9-beam-note", p, __display_steel_section);
            AppendNoteLeader("b9-beam-note", "1", total + "Ø" + STM, p, 0, false);

            p = new pPos(A + STM / 2, -BH + A + STM / 2) + pt;
            AppendCircleHtml("b9-beam-note", p, __display_steel_section);

            p.X += v;
            AppendCircleHtml("b9-beam-note", p, __display_steel_section);
            AppendNoteLeader("b9-beam-note", "2", total + "Ø" + STM, p, 1, false);
            

            int index = 0;

            total = NST; // index == 0 ? thep_tren_num : thep_duoi_num;
            int note_index = index == 0 ? thep_tren_index : thep_duoi_index;
                    
            p = new pPos(A + STM / 2,  -A - C - STM / 2);
            AppendCircleHtml("b9-beam-note", p, __display_steel_section);


            p.X += (BW - 2 * A - STM) / (NST - 1);

            pPos p2 = new pPos(0, -C - A);

            p2 += pt;

            AppendDimension("b9-dimension", pt, p2, - F * 2);
            AppendDimension("b9-dimension", p2, new pPos(0, -BH) + pt, -F * 2);
                        

            //if (index == 1) p.Y = total > 1 ? -BH + A + C + STM / 2 : (-BH + A + STM / 2);

            p += pt;

            AppendCircleHtml("b9-beam-note", p, __display_steel_section);

            AppendNoteLeader("b9-beam-note", note_index.ToString(), total + "Ø" + STM, p, 3, false);

            AppendNoteLeader("b9-beam-note", "D", "Ø" + STC + "a" + CLP1, new pPos(A, -BH / 2) + pt, 4, false);

            AppendPolylineHtml("b9-beam-bounding", region.Move(pt), false);
            AppendPolylineHtml("b1-beam-clip", new pPos(A, -A).RectToPoint(new pPos(BW - A, -BH + A)).Move(pt), true);

            AppendBreakLine("b1-beam-clip", BW + SLB + pt.X, pt.Y, BW + SLB + pt.X, pt.Y - SLB, C);

            AppendTextHtml("b0-beam-title", "{" + mat_cat_index + "-" + mat_cat_index + "}", new pPos(0, -BH - F * 2) + pt);
            AppendTextHtml("b0-beam-title", "TL:1/50", new pPos(0, -BH - F * 2.5) + pt);

            AppendDimension("b9-dimension", pt.X, pt.Y, pt.X, pt.Y - BH, -2.5 * F);
            AppendDimension("b9-dimension", new pPos(0, -BH) + pt, new pPos(BW, -BH) + pt, -F);
        }

        
        string _thep_dai_text(double clip, int thep_dai_index)
        {
            return "<> - " + STC + "a" + clip + "(" + thep_dai_index + ")";
        }

        public void DrawBeam(pPos[] src)
        {
            double SUM = src.Last().X - src.First().X;
            R = src.First().Rect(SUM, BH);

            //ACD.WR("Bound {0}, SUM {1}", R.Size(), SUM);
            
            double BL = R[1].X - R[0].X;
            BH = R[2].Y - R[0].Y;

            //ĐƯỜNG BAO
            AppendPolylineHtml("b0-beam-bounding-box", R, true);

            //THÉP CHỦ TRÊN
            AppendPolylineHtml("b0-beam-main-steel-above", R[0].X + SUM - 2 * C, R[0].Y + 2 * C,
                        R[0].X + SUM - C, R[0].Y + C, R[0].X + SUM - C, R[2].Y - C,
                        R[0].X + C,  R[2].Y - C, R[0].X + C, R[0].Y + C, R[0].X + 2 * C, R[0].Y + 2 * C);

            //THÉP CHỦ DƯỚI
            AppendPolylineHtml("b0-beam-main-steel-below", R[0].X + SUM - 5 * C, R[0].Y + 2 * C,
                        R[0].X + SUM - 4 * C, R[0].Y + C, R[0].X + SUM - 4 * C, R[0].Y + C,
                        R[0].X + 4 * C, R[0].Y + C, R[0].X + 5 * C, R[0].Y + 2 * C);

            AppendDimensionX("b9-dimension", R[0].X, R[0].Y, R[1].X, - 1.75 * F); //DIM TỔNG X
            AppendDimensionY("b9-dimension", R[0].X, R[0].Y, R[2].Y, F); //DIM TỔNG Y

            //List<int> mat_cat_indexes = new List<int>();
            int thep_tren_index = 3, thep_duoi_index = 4, mat_cat_index = 2, thep_dai_index = (src.Length) * 2;
            pPos beam_section_pt = src.First() - new pPos(0, F * 5);

            for (int i = 0; i < src.Length; i++)
                AppendPolylineHtml("b0-beam-grid-hidden", src[i].X, src[i].Y - F * 2.5, src[i].X, src[i].Y + F * 3);
            
            for (int i = 0; i < src.Length - 1; i++)
            {
                var __R = src[i].Rect(src[i].DistanceTo(src[i+1]), BH);
                pPos p2 = new pPos(src[i + 1].X, BH), p1 = src[i];

                var __BL = __R[1].X - __R[0].X;

                //GHI CHÚ THÉP CHỦ
                if (i == 0 || i == src.Length - 2 || i ==(int)(src.Length / 2))
                {
                    AppendNoteLeader("b9-beam-note", "1", NST + "Ø" + STM, new pPos((__R[0].X + __R[2].X) / 2, __R[2].Y - C), 0);
                    AppendNoteLeader("b9-beam-note", "2", NST + "Ø" + STM, new pPos(__R[0].X + __BL * 0.75, __R[0].Y + C), 1);
                }

                AppendDimensionX("b9-dimension", __R[0].X, __R[2].Y, __R[1].X, F * 1.5);

                if (__R.Size().X > 1500)
                {
                    double thep_tren_trai = __R[0].X + __BL / S1;
                    double thep_tren_phai = __R[0].X + __BL * (S1 - 1) / S1;

                    //THÉP LƯNG TRÁI
                    if (p1.X - R[0].X < 100)
                        AppendPolylineHtml("b1-beam-support-steel-above-left", 
                            __R[0].X + C * 3, __R[0].Y + 2 * C,
                            __R[0].X + C * 2, __R[0].Y + C, 
                            __R[0].X + C * 2, __R[2].Y - 2 * C,
                            thep_tren_trai, __R[2].Y - 2 * C,
                            thep_tren_trai - C, __R[2].Y -  3 * C);
                    else
                        AppendPolylineHtml("b1-beam-support-steel-above-left", 
                            thep_tren_trai - C, __R[2].Y - 3 * C,
                            thep_tren_trai, __R[2].Y - 2 * C, __R[0].X, __R[2].Y - 2 * C);

                    //THÉP LƯNG PHẢI
                    if (R[1].X - p2.X < 100)
                        AppendPolylineHtml("b1-beam-support-steel-above-right", __R[1].X - C * 3, __R[0].Y + 2 * C,
                                __R[1].X - C * 2, __R[0].Y + C, 
                                __R[1].X - C * 2, __R[2].Y - 2 * C,
                                thep_tren_phai, __R[2].Y - 2 * C,
                                thep_tren_phai + C, __R[2].Y - 3 * C);
                    else
                        AppendPolylineHtml("b1-beam-support-steel-above-right", __R[1].X, __R[2].Y - 2 * C,
                                thep_tren_phai, __R[2].Y - 2 * C, thep_tren_phai + C, __R[2].Y  - 3 * C);

                    //THÉP BỤNG NẾU BƯỚC CỘT >= 1.6M

                    if (__BL >= 1600)
                    {
                        double thep_duoi_trai = __R[0].X + __BL / S2;
                        double thep_duoi_phai = __R[0].X + __BL * (S2 - 1) / S2;

                        AppendPolylineHtml("b1-beam-support-steel-below", thep_duoi_phai - C, __R[0].Y + 3 * C,
                                thep_duoi_phai, __R[0].Y + 2 * C, thep_duoi_trai, __R[0].Y + 2 * C,
                                thep_duoi_trai + C, __R[0].Y + 3 * C);

                        AppendDimensionX("b9-dimension", __R[0].X, __R[2].Y,thep_tren_trai, F, _thep_dai_text(CLP1, thep_dai_index));
                        AppendDimensionX("b9-dimension", thep_tren_trai, __R[2].Y, thep_tren_phai, F,_thep_dai_text(CLP2, thep_dai_index));
                        AppendDimensionX("b9-dimension", thep_tren_phai, __R[2].Y, __R[1].X, F, _thep_dai_text(CLP1, thep_dai_index));

                        gDraw2DDCLS._dimension_txt_offset = -gDraw2DDCLS._dimension_txt_offset;
                        AppendDimensionX("b9-dimension", thep_duoi_trai, __R[0].Y - 3 * C, __R[0].X, F);
                        AppendDimensionX("b9-dimension", thep_duoi_phai, __R[0].Y - 3 * C, thep_duoi_trai, F);
                        AppendDimensionX("b9-dimension", __R[1].X, __R[0].Y - 3 * C, thep_duoi_phai, F);
                        gDraw2DDCLS._dimension_txt_offset = -gDraw2DDCLS._dimension_txt_offset;
                    }
                       
                    double b = __R[0].X + (__BL + __BL * (S2 - 1)) / (S2 * 2);

                    AppendNoteLeader("b9-beam-note", thep_tren_index.ToString(), NST + "Ø" + STM, new pPos(__R[0].X + __BL / (S1 * 2), __R[2].Y - C * 2), 0);

                    //ghi chu thep duoi
                    
                    AppendNoteLeader("b9-beam-note", thep_duoi_index.ToString(), NSB + "Ø" + STM, 
                        new pPos((__R[0].X + __R[2].X) / 2, __R[0].Y + C * 2), 1);

                    beam_section_pt.X += 5 * F + BW;
                    mat_cat_index = (mat_cat_index + 1) % 2;

                    AppendNoteCutX("b9-beam-note", "1", b, __R[2].Y + F * 2, -F * 5);
                    AppendNoteCutX("b9-beam-note", "2", __R[0].X + __BL / (S2 * 2), __R[2].Y + F * 2, -F * 5);
                    AppendNoteCutX("b9-beam-note", "2", __R[1].X - __BL / (S2 * 2), __R[2].Y + F * 2, -F * 5);

                    beam_section_pt.X += 5 * F + BW;
                    mat_cat_index = (mat_cat_index + 1) % 2;
                    
                    if (p2.X - __R[1].X < 100)
                        AppendNoteLeader("b9-beam-note", thep_tren_index.ToString(), NST + "Ø" + STM,
                            new pPos(__R[1].X - __BL / (S1 * 2), __R[2].Y - C * 2), 0);
                                        
                    //TAO THEP DAI
                    double y1 = __R[2].Y - C / 2, y2 = __R[0].Y + C / 2;

                    for (double j = __R[0].X + CLP1; j <= thep_tren_trai; j += CLP1)
                        AppendPolylineHtml("b1-beam-clip", j, y1, j, y2);

                    for (double j = thep_tren_phai + CLP1; j <= __R[1].X; j += CLP1)
                        AppendPolylineHtml("b1-beam-clip", j, y1, j, y2);

                    for (double j = thep_tren_trai + CLP2; j <= thep_tren_phai; j += CLP2)
                        AppendPolylineHtml("b1-beam-clip", j, y1, j, y2);


                }
                else
                {
                    //THÉP LƯNG TRÁI

                    if (p1.X - R[0].X < 100)
                    {
                        AppendPolylineHtml("b1-beam-support-steel-above-left", __R[0].X + C * 4, __R[0].Y + 3 * C,
                                __R[0].X + C * 3, __R[0].Y + 2 * C, __R[0].X + C * 3, __R[0].Y + 2 * C,
                                __R[0].X + C * 3, __R[0].Y + BH - 2 * C, __R[1].X, __R[0].Y + BH - 2 * C);
                        mat_cat_index = 0;

                        AppendNoteCutX("b9-beam-note", "2", (__R[0].X + __R[1].X) / 2, __R[1].Y + F * 2, -F * 5);
                        
                        beam_section_pt.X += 5 * F + BW;
                        mat_cat_index = (mat_cat_index + 1) % 2;
                    }
                    else if (R[2].X - p2.X < 100)
                    {
                        AppendPolylineHtml("b1-beam-support-steel-above-left", __R[1].X - C * 4, __R[0].Y + 3 * C,
                                __R[1].X - C * 3, __R[0].Y + 2 * C, __R[1].X - C * 3, __R[0].Y + 2 * C,
                                __R[1].X - C * 3, __R[0].Y + BH - 2 * C, __R[0].X, __R[0].Y + BH - 2 * C);

                        AppendNoteCutX("b9-beam-note", "2", (__R[0].X + __R[1].X) / 2, __R[1].Y + F * 2, -F * 5);
                        
                        beam_section_pt.X += 5 * F + BW;
                        mat_cat_index = (mat_cat_index + 1) % 2;
                    }

                    double y1 = __R[1].Y - C / 2, y2 = __R[0].Y + C / 2;

                    for (double j = __R[0].X + CLP1; j <= __R[1].X - CLP1; j += CLP1)
                        AppendPolylineHtml("b1-beam-clip", j, y1, j, y2);

                    AppendDimension("b9-dimension",__R[0].X, __R[1].Y, __R[1].X, __R[1].Y, F, _thep_dai_text(CLP1, thep_dai_index));
                    AppendDimension("b9-dimension", __R[0].X, __R[1].Y, __R[1].X, __R[1].Y,  - F * 2);
                    AppendDimension("b9-dimension",__R[1].X, __R[1].Y, __R[0].X, __R[1].Y,  - F * 1.5);
                }
            }
        }

        public void AddLetters(pPos[] ls)
        {
            GridLetters = ls;

            foreach (pPos _p in GridLetters)
            {
                pPos p = new pPos(_p.X, R[0].Y - 2.8 * F);
                AppendCircleHtml("b9-beam-note", p, 1.2);
                AppendTextHtml("b9-beam-note-txt1", _p.Content, p);

                p.Y += F * 6;
                AppendCircleHtml("b9-beam-note", p, 1.2);
                AppendTextHtml("b9-beam-note-txt1", _p.Content, p);
            }
        }

        public double C = 50;
        public int NST = 2; //SỐ LƯỢNG THÉP CHỦ TRÊN
        public int NSM = 2; //SỐ LƯỢNG THÉP CHỦ DƯỚI
        public int NSB = 2; //SỐ LƯỢNG THÉP CHỦ DƯỚI
        public double S1 = 4;
        public double S2 = 8;
        public double CL1 = 200; //THÉP ĐAI GỐI
        public double CL2 = 200; //THÉP ĐAI BỤNG
        public double CLP1 = 100; //KHOẢNG CÁCH THÉP ĐAI GỐI
        public double CLP2 = 200; //KHOẢNG CÁCH THÉP ĐAI BỤNG
        public double STC = 6; //THÉP ĐAI
        public double STM = 16;
        public double STS = 12;
        public double F = 500;
        public double SLB = 100;
        public double BW = 200;
        public double BH = 400;
        public pPos[] R;
        public pPos[] GridLetters = new pPos[0];
    }


    public class CadLibStructCLS
    {
        static gDraw2DStruct region2d;
        static cReadData CssData;

        public static void DrawBeam()
        {
            ObjectIdCollection selIds = ACD.GetSelection();

            if (selIds.Count > 0)
            {
                CssData = new cReadData(selIds);
                //pPos[] bb = ACD.DB._getBound(selIds);
                //cReadData.html_basepoint = new pPos(bb[0].X, bb[1].Y);

                cReadData.super_key = "beam";
                cReadData.__sc = 1;
                cReadData._dpsc = 0.01;

                gDraw2DDCLS._dimension_architecture_tick = true;
                //gDraw2DDCLS._dimension_txt_offset = 50;
                gDraw2DDCLS._dimension_round = 50;

                region2d = new gDraw2DStruct(CssData);
                region2d.DrawBeam(ACD.DB._getAllVertices(selIds).SelfIntersect.OrderBy(__p => __p.X).ToArray());

                region2d.AddLetters(CssData.GridLetters);
                //ACD.WR("Segments {0} Contents {1}", its.GetSegment().Count, WriteHtmlCLS.dict_contents.Count);

                //Ghi file Html plan_html.html
                WriteHtmlCLS.WriteHtml("beam_html", "beamstyle.css", "BEAM", CssData, 400, 200);
            }
        }
    }
}

