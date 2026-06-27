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

namespace AcadScript
{
    public class HtmlRoundCLS
    {
        static List<pPos> ptsText = new List<pPos>();
        static List<pPos> ptsValue = new List<pPos>();

        static void _reaPlanHtmlData()
        {
            string[] ar = File.ReadAllLines(@"D:\html\plan_html.html");
            

            foreach (string s in ar)
                if (s.Trim().st_("<text"))
                {
                    int a = s.IndexOf("x=\"");
                    //ACD.WR("R1");

                    pPos pt = new pPos(0, 0);

                    if (a != -1)
                    {
                        int b = s.IndexOf("\" y=", a);
                        //ACD.WR("R2");

                        if (b != -1)
                        {
                            ACD.WR("R3 - X = {0}", s.Substring(a + 3, b - a - 3));
                            pt.X = s.Substring(a + 3, b - a - 3).ToNumber();
                            //ACD.WR("R3.1");
                            a = s.IndexOf("\" fill", b + 1);
                            //ACD.WR("R3.2");
                            if (a != -1)
                            {
                                ACD.WR("R3.3 >{0} - Y = {1}", a - b - 5, s.Substring(b + 5, a - b - 5));

                                pt.Y = s.Substring(b + 5, a - b - 5).ToNumber();
                                ACD.WR("R3.4 PT = {0}", pt);

                                a = s.IndexOf("dominant-baseline");
                                string _st = s.Substring(a).Replace("</text>", "");

                                a = _st.IndexOf(">");
                                ACD.WR("R5 _st = {0} a = {1}", _st, a);

                                if (a != -1)
                                {
                                    pt.Content = _st.Substring(a + 1);

                                    if ("0123456789".Any(_c => pt.Content.st_(_c.ToString())))
                                        ptsValue.Add(pt);
                                    else
                                        ptsText.Add(pt);
                                }
                            }
                        }
                    }
                }

            

            //List<double> lsVal = new List<double>();
            double[] numbers = new double[] { 100, 125, 135, 150, 175, 205, 225, 250, 275, 750 };
            int[] counters = new int[numbers.Length];

            foreach (pPos pt in ptsValue)
            {
                double v = pt.Content.ToNumber(); //.roundNumber(5);

                //if(!lsVal.Contains(v))
                //lsVal.Add(v);

                v = numbers.OrderBy(_v => Math.Abs(v - _v)).First();
                counters[Array.IndexOf(numbers, v)]++;

                pt.Content = v.ToString();
            }

            ACD.WR("Text {0}", ptsText.ToText());
            ACD.WR("Value {0}", ptsValue.ToText());

            //lsVal = lsVal.Distinct().OrderBy(v => v).ToList();
            ACD.WR("Value {0}", numbers.ToTextDouble(","));
            ACD.WR("Counter {0}", counters.ToTextInt(","));

            ACD.WR("Sum {0}", counters.Sum());
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                pPos pt = ACD.GetPoint();

                if (pt != null)
                {
                    _reaPlanHtmlData();
                    ObjectIdCollection txtIds = new ObjectIdCollection();
                
                    foreach (pPos p in ptsText)
                        txtIds.Add(ACD.DB.CreateText(p.Content, p, 2, 200, "LAYER=G_Text"));

                    foreach (pPos p in ptsValue)
                    {
                        ObjectId _txt = ACD.DB.CreateText(p.Content, p, 2, 200);
                        ACD.DB._setLayer(_txt, "G_Value");
                        txtIds.Add(_txt);
                    }

                    string blockname = ACD.DB.uniqueBlockName("txtBlock");

                    pPos[] bb = ACD.DB._getBound(txtIds);

                    ACD.DB.NewBlock(txtIds, blockname, true, false, bb[0]);

                    ACD.DB.Insert(blockname, pt);
                }

                ACD.Focus();
            }
        }
    }
}

