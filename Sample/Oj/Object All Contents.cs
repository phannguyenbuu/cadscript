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
    public class TextAllContentCLS
    {
        static List<string> _readContent(ObjectIdCollection ids)
        {
            List<string> res = new List<string>();

            foreach (ObjectId id in ids)
                if (ACD.DB._isText(id) || ACD.DB._isDim(id) || ACD.DB._isLeader(id))
                    res.Add(_validValue(id));
                else if (ACD.DB._isBlock(id))
                {
                    res.Add(ACD.DB.GetAllBlockAtt(id));
                    ACD.DB.BlockEntitiesAction(id, _ids => { res.AddRange(_readContent(_ids)); });
                }

            return res;
        }

        static int _countContent(ObjectIdCollection ids, string content)
        {
            int res = 0;

            foreach (ObjectId id in ids)
                if (ACD.DB._isText(id) || ACD.DB._isDim(id) || ACD.DB._isLeader(id))
                {
                    if (_validValue(id) == content) res++;

                }
                else if (ACD.DB._isBlock(id))
                {
                    foreach (string s in ACD.DB.GetAllBlockAtt(id).filter())
                        if (s._firstProp() == content)
                            res++;

                    ACD.DB.BlockEntitiesAction(id, _ids => { res += _countContent(_ids, content); });
                }

            return res;
        }

        static double _lengthContent(ObjectIdCollection ids, string content)
        {
            double res = 0;

            foreach (ObjectId id in ids)
                if (ACD.DB._isDim(id))
                {
                    if (_validValue(id) == content)
                    {
                        res = ACD.DB._getDimPoints(id).Length(false);
                        break;
                    }
                }
                else if (ACD.DB._isBlock(id))
                {
                    ACD.DB.BlockEntitiesAction(id, _ids => { res = _lengthContent(_ids, content); });
                }

            

            return res;
        }

        static double _maxDimAx(ObjectIdCollection ids, int ax)
        {
            double res = 0;

            foreach (ObjectId id in ids)
                if (ACD.DB._isDim(id))
                {
                    pPos[] pts = ACD.DB._getDimPoints(id);

                    if ((ax == 0 && Math.Abs(pts[0].Y - pts[1].Y) < 1) ||
                        (ax == 1 && Math.Abs(pts[0].X - pts[1].X) < 1))
                    {
                        double d = ACD.DB._getDimPoints(id).Length(false);
                        if (d > res)
                            res = d;
                    }
                }
                else if (ACD.DB._isBlock(id))
                {
                    ACD.DB.BlockEntitiesAction(id, _ids => { 
                        double d = _maxDimAx(_ids, ax);

                        if (d > res)
                            res = d;
                    });
                }

            return res;
        }


        static string _validValue(ObjectId id)
        {
            string s = ACD.DB._getContent(id);
            return s._getInComma().et_() ? s : s._getInComma();
        }

        static string _listBeamSteels(string[] contents)
        {
            string res = String.Format("[0] N{0}\r\n", quatity);

            string main_stl = "16", clip_stl = "6";
            
            if (contents.Any(s => s.ct_("∅20")))
                main_stl = "20";

            if (contents.Any(s => s.ct_("∅8")))
                clip_stl = "8";

            res += String.Format("[1] L{0} x H{1} x N{2} f{3}\r\n", 
                width.roundNumber(0.1) - 50, height.roundNumber(0.1) - 50, 2, main_stl);

            res += String.Format("[2] L{0} x N{1} f{2}\r\n", width.roundNumber(0.1) - 50, 2, main_stl);
           
            

            if (contents.Any(s => s.ct_("1" + cF)))
            {
                res += String.Format("[3] L{0} x H{1} x N{2} f{3}\r\n", 
                    (width * 0.5).roundNumber(0.1), height.roundNumber(0.1), 1, main_stl);

                

                res += String.Format("[4] L{0} x H{1} x N{2} f{3}\r\n",
                    (width * 0.75).roundNumber(0.1), height.roundNumber(0.1), 1, main_stl);

                res += String.Format("[D] W{0} x H{1} x N{2} f{3}\r\n",
                    150, height.roundNumber(0.1) - 50, (int)(width / 150), clip_stl);
            }
            else if(width >= 3000 )
            {
                res += String.Format("[3] L{0} x H{1} x N{2} f{3}\r\n",
                    (width * 0.5).roundNumber(0.1) * 2, height.roundNumber(0.1), 1, main_stl);

                res += String.Format("[4] L{0} x H{1} x N{2} f{3}\r\n",
                    (width * 0.75).roundNumber(0.1) * 2, height.roundNumber(0.1), 1, main_stl);

                res += String.Format("[D] W{0} x H{1} x N{2} f{3}\r\n",
                    150, height.roundNumber(0.1) - 50, (int)(width / 150), clip_stl);
            }
            else
            {
                res += String.Format("[D] W{0} x H{1} x N{2} f{3}\r\n",
                    150, height.roundNumber(0.1) - 50, (int)(width / 100), clip_stl);
            }

            return res;
        }

        static double width, height;
        static int quatity;
        static string cF = "ɸ";
        static Dictionary<string, double> dicts;

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;

                ObjectIdCollection selIds = ACD.GetSelection();


                if (selIds.Count > 0)
                {
                    dicts = new Dictionary<string, double>();

                    string[] flist = new string[] { "6", "8", "10", "12", "14", "16", "18", "20", "22" };

                    //foreach (ObjectId txtId in selIds)
                    //    ACD.WR("Txt {0} contain C {1}", ACD.DB._getContent(txtId), 
                    //        ACD.DB._getContent(txtId).Contains(cF));

                    width = _maxDimAx(selIds, 0);
                    height = _maxDimAx(selIds, 1);

                    List<string> contents = _readContent(selIds).Distinct().OrderBy(s => s).ToList();

                    string st = contents.FirstOrDefault(s => s.ct_("SL:"));

                    if (!st.et_())
                        quatity = (int)st.filter("-").FirstOrDefault(s => s.ct_("SL:")).Replace("SL:", "").First().ToNumber();

                    List<string> struct_contents = contents.Where(s => s.Contains(cF))
                        .OrderBy(s => s.filter(cF).Last().Upper()).ThenBy(s => s.Upper()).ToList();

                    double len = _maxDimAx(selIds, 0).roundNumber();

                    if (len != 0)
                        foreach (ObjectId id in selIds)
                            if (ACD.DB._isText(id))
                            {
                                string _st = ACD.DB._getContent(id);

                                if (_st.st_("SL:") && _st.ct_("-"))
                                    ACD.DB._setContent(id, st.Replace(_st.filter("-").Last(), len.ToString()));
                            }

                    string content = "<size>\r\n" + width.roundNumber() + "x" + height.roundNumber()

                        + "\r\n<struct>\r\n" + contents.Where(s => !s.et_() && s.ct_(cF))
                        .Select(s => s + "   [" + _countContent(selIds, s) + "] " 
                        + _lengthContent(selIds, s).roundNumber()).ToTextStr("\r\n")

                        + "\r\n\r\n<normal>\r\n" + contents.Where(s => !s.et_() && !s.ct_(cF))
                        .Select(s => s + "   [" + _countContent(selIds, s) + "] " 
                        + _lengthContent(selIds, s).roundNumber() ).ToTextStr("\r\n");


                    if (contents.Any(s => s.ct_(cF)))
                        content += "\r\n<quatity>\r\n" + _listBeamSteels(contents.ToArray());

                    MessageBox.Show(content);
                    Clipboard.SetText(content);
                }

                ACD.Focus();
            }
        }
    }
}

