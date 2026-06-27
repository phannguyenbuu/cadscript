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
    public class ListSteelCLS
    {
        static ObjectIdCollection _drawCell(double x, double y, double w, double h, object value = null)
        {
            ObjectIdCollection res = new ObjectIdCollection();

            res.Add(ACD.DB.Draw2D(0, 0, w, 0, w, -h, 0, -h, "c"));

            if (value != null && !value.ToString().et_())
                res.Add(ACD.DB.CreateText("#M" + value, new pPos(w / 2, -h / 2), h / 250 >= 1.2 ? h / 250 : 1.2));

            ACD.DB.MoveObject(res, new pPos(x, y));

            return res;
        }

        static string _getVal(string[] ar, string c, string ch)
        {
            string val = ar.FirstOrDefault(_s => _s.st_("[" + c + "]"));

            if (!val.et_())
            {
                val = val.Replace("[" + c + "]", "").filter("x ").FirstOrDefault(_s => _s.st_(ch));

                if (!val.et_())
                    val = val.Replace(ch, "");
            }

            return val;
        }

        static ObjectIdCollection _drawSteelShape(string c, double x, double y, string w, string h, bool ushape = false)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            double a = 800, b = 100, m = -b / 2;
            pPos pt = new pPos(-m, -b / 2);

            if (_isD(c))
            {
                res.Add(ACD.DB.Draw2D(a / 4, 0, a * 0.75, 0, a * 0.75, -b, a / 4, -b, "c"));
                res.Add(ACD.DB.CreateText("#M" + w, new pPos(a / 2, -b / 2), 1));

                ACD.WR("c = {0}x{1}", w, h);

                pt.X += a / 4;

                ObjectId txtId = ACD.DB.CreateText("#C" + h, pt, 1);
                ACD.DB.Transform(txtId, new pPos(0, 0), -90, 1, pt);
                res.Add(txtId);
            }
            else if (ushape)
            {
                res.Add(ACD.DB.Draw2D(0, -b, 0, 0, a, 0, a, -b));
                res.Add(ACD.DB.CreateText("#M" + w, new pPos(a / 2, -b / 2), 1));

                ObjectId txtId = ACD.DB.CreateText("#C" + h, pt, 1);
                ACD.DB.Transform(txtId, new pPos(0, 0), -90, 1, pt);
                res.Add(txtId);
            }
            else
            {
                res.Add(ACD.DB.Draw2D(0, m * 2, a, m * 2));
                res.Add(ACD.DB.CreateText("#C" + w, new pPos(a / 2, m), 1));
            }

            ACD.DB.MoveObject(res, new pPos(x, y));
            return res;
        }

        static bool _isD(string s)
        {
            return s.Replace("'", "") == "D";
        }

        static string[] ls_char
        {
            get
            {
                List<string> res = new List<string>();

                foreach (char c in "123456789D")
                {
                    string t = c.ToString();
                    res.Add(t);

                    for (int i = 0; i < 3; i++)
                    {
                        t += "'";
                        res.Add(t);
                    }
                }

                return res.ToArray();
            }
        }

        static ObjectIdCollection _drawSteelTable(string content, double y)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            string[] ar = content.filter("\r\n");



            string stname = ar.FirstOrDefault(s => s.st_("#"));

            if (!stname.et_())
                stname = stname.filter("#[").First().Trim();



            int quatity = (int)_getVal(ar, "0", "N").ToNumber(1);
            int count = 0;


            foreach (string c in ls_char)
            {

                int total = (int)_getVal(ar, c, "N").ToNumber();

                if (total != 0)
                {
                    res.AddRange(_drawCell(1250, -count * cell, 250, cell, c));


                    res.AddRange(_drawCell(1500, -count * cell, 1000, cell));


                    string f = _getVal(ar, c, "f");

                    if (f.et_())
                        f = _isD(c) ? "16" : "6";

                    res.AddRange(_drawCell(2500, -count * cell, 250, cell, f));

                    res.AddRange(_drawCell(2750, -count * cell, 250, cell, total));

                    res.AddRange(_drawCell(3000, -count * cell, 250, cell, quatity));

                    int n = (int)(quatity * _getVal(ar, c, "N").ToNumber());

                    res.AddRange(_drawCell(3250, -count * cell, 250, cell, n));

                    double l = _isD(c) ? (_getVal(ar, c, "W").ToNumber() + _getVal(ar, c, "H").ToNumber()) * 2
                        : _getVal(ar, c, "L").ToNumber() + _getVal(ar, c, "H").ToNumber() * 2;

                    res.AddRange(_drawCell(3500, -count * cell, 250, cell, (l / 1000).roundNumber(0.001)));

                    if (_isD(c))
                        res.AddRange(_drawSteelShape(c, 1600, -count * cell - 20,
                            _getVal(ar, c, "W"), _getVal(ar, c, "H")));
                    else if (_getVal(ar, c, "H").ToNumber() != 0)
                        res.AddRange(_drawSteelShape(c, 1600, -count * cell - 20,
                            _getVal(ar, c, "L"), _getVal(ar, c, "H"), true));
                    else
                        res.AddRange(_drawSteelShape(c, 1600, -count * cell - 20,
                            _getVal(ar, c, "L"), "0"));

                    res.AddRange(_drawCell(3750, -count * cell, 250, cell, (n * l / 1000).roundNumber(0.001)));

                    res.AddRange(_drawCell(4000, -count * cell, 250, cell, (dict_kg[f] * n * l / 1000).roundNumber(0.001)));

                    dicts_total[f] += n * l / 1000;
                    dicts_total_kg[f] += dict_kg[f] * n * l / 1000;

                    count++;
                }
            }
            res.AddRange(_drawCell(0, 0, 250, count * cell, stt.ToString()));
            res.AddRange(_drawCell(250, 0, 1000, count * cell, stname));

            ACD.DB.MoveObject(res, new pPos(0, y));

            return res;
        }

        static int stt = 0, cell = 150;
        static Dictionary<string, double> dict_kg = new Dictionary<string, double>{};
        static Dictionary<string, double> dicts_total, dicts_total_kg;

        static string _prefixNo(string s)
        {
            string number = "0123456789";
            foreach (char c in number)
                s = s.Replace(c.ToString(), "");

            return s;
        }

        static string _getNo(string s)
        {
            string number = "0123456789";
            string res = "";
            
            foreach (char c in number)
                if (s.ct_(c.ToString()))
                    res += c;
            
            return s;
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection().FilterIds("MTEXT","TEXT");

                if (selIds.Count > 0)
                {
                    dicts_total = new Dictionary<string, double>();
                    dicts_total_kg = new Dictionary<string, double>();

                    string[] flist = new string[] { "6", "8", "10", "12", "14", "16", "18", "20", "22" };

                    foreach (string s in flist)
                    {
                        dicts_total.Add(s, 0);
                        dicts_total_kg.Add(s, 0);
                    }

                    dict_kg.Add("6",0.22);
                    dict_kg.Add("8",0.39);
                    dict_kg.Add("10",0.62);
                    dict_kg.Add("12",0.89);
                    dict_kg.Add("14",1.21);
                    dict_kg.Add("16",1.58);
                    dict_kg.Add("18",2.00);
                    dict_kg.Add("20",2.47);
                    dict_kg.Add("22",2.98);
                    dict_kg.Add("25",3.85);
                    dict_kg.Add("28",4.83);
                    dict_kg.Add("32",6.31);

                    ObjectIdCollection res = new ObjectIdCollection();

                    res.AddRange(_drawCell(0, 0, 250, 250, "STT"));
                    res.AddRange(_drawCell(250, 0, 1000, 250, "TÊN CẤU KIỆN"));
                    res.AddRange(_drawCell(1250, 0, 250, 250, "SỐ HIỆU"));
                    res.AddRange(_drawCell(1500, 0, 1000, 250, "HÌNH DẠNG-KÍCH THƯỚC"));
                    res.AddRange(_drawCell(2500, 0, 250, 250, "ĐƯỜNG KÍNH\r\n(mm)"));
                    res.AddRange(_drawCell(2750, 0, 250, 250, "SỐ THANH"));
                    res.AddRange(_drawCell(3000, 0, 250, 250, "SỐ CẤU KIỆN"));
                    res.AddRange(_drawCell(3250, 0, 250, 250, "TỔNG SỐ\r\nTHANH"));
                    res.AddRange(_drawCell(3500, 0, 250, 250, "CHIỀU DÀI\r\n1 THANH\r\n(m)"));
                    res.AddRange(_drawCell(3750, 0, 250, 250, "TỔNG\r\nCHIỀU DÀI\r\n(m)"));
                    res.AddRange(_drawCell(4000, 0, 250, 250, "KHỐI LƯỢNG\r\n(kg)"));

                    double ny = -cell-100;
                    stt = 1;
                    string content;

                    pPos[] allpts = selIds.ToList().Select(id => ACD.DB._getPoint(id))
                        .Where(p => p.Content.filter("\r\n").Any(s => s.st_("#")))
                        .OrderBy(p => _prefixNo(p.Content.filter("\r\n").FirstOrDefault(s => s.st_("#"))))
                        .ThenBy(p => _getNo(p.Content.filter("\r\n").FirstOrDefault(s => s.st_("#"))).ToNumber()).ToArray();

                    foreach (pPos p in allpts)
                    {
                        content = p.Content;
                        ObjectIdCollection newIds = _drawSteelTable(content, ny);

                        res.AddRange(newIds);
                        ny -= ACD.DB._getBound(newIds).Size().Y;
                        stt++;
                    }

                    content = "";
                    foreach (string s in flist)
                        if(dicts_total[s] != 0)
                            content += "Thép f" + s + " tổng chiều dài " + dicts_total[s] + " (m) tổng khối lượng " + dicts_total_kg[s] + " (kg)\r\n";

                    MessageBox.Show(content);
                    Clipboard.SetText(content);

                    //ACD.DB.NewBlock(res, ACD.DB.uniqueBlockName("steel_schedule"), true, false, ACD.DB._getBound(selIds)[0]);
                    ACD.DB.MoveObject(res, ACD.DB._getBound(selIds[0])[0]);
                }

            }
            ACD.Focus();
        }
    }
}

