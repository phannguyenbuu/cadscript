using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
//using System.Reflection;
using System.Linq;
using System.IO;
using System.Text;
using System;
//using SyncObject;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Runtime;

namespace AcadScript
{
    public class InfoObjectIdsCLS
    {
        static Dictionary<string,string> dictCSS;
        //static List<string> wallStyles;
        //static string resultcss;

        static string increaseCssName(string cssname)
        {
            string[] ls = dictCSS.Where(itm => itm.Key.StartsWith(cssname)).Select(itm => itm.Key).ToArray();
            int max_index = 0;
            
            if(ls.Length > 0)
                max_index = ls.Max(s => (int)s.Substring(cssname.Length).Replace(".","").ToNumber());
            
            return cssname + "." + (max_index + 1);
        }

        static string vertsToCss(IEnumerable<pPos> pts)
        {
            string res = "";

            if (pts.Count() > 1)
            {
                pPos basept = pts.Boundary()[0].Round(1);
                res = "verts:$" + basept.X + "_" + basept.Y + "," 
                    + pts.Select(p => p.Round(1) - basept).ToText(false).Replace(";", ",") + ";";
            }
            else
                res = "verts:" + pts.First().Round(1) + ";";

            return res;
        }

        static void AddCssBlock(ObjectId blockId, string roomname)
        {
            string wstyle = ACD.DB._getIdName(blockId);
            string cssname = wstyle + "." + roomname;
                        
            cssname = increaseCssName(cssname);
            string info = vertsToCss(new pPos[] { ACD.DB._getPoint(blockId) });
            info += "rotation:" + ACD.DB._getRotation(blockId).roundNumber() + ";";
            info += "scale:" + ACD.DB._getScale(blockId) + ";";

            dictCSS.Add(cssname, info);

            ACD.DB.BlockEntitiesAction(blockId, _ids =>
            {
                foreach (ObjectId _id in _ids)
                    if(ACD.DB._isBlock(_id))
                        AddCssBlock(_id, roomname);
            });
        }

        static void AddCssWall(ObjectId wallId, string roomname)
        {
            string wstyle = ACD.DB._getIdName(wallId);
            string cssname = wstyle + "." + roomname;

            pPos[] pts = ACD.DB._getVertices(wallId);
            cssname += "." + (pts[0].AngleAxisVsVector(pts[1]) == 0? "x" : "y");
            cssname = increaseCssName(cssname);

            string info = "";// "height:" + ACD.DB._getWallHeight(wallId).roundNumber(10) + ";";
                
            info += vertsToCss(ACD.DB._getWallShape(wallId));



            List<string> openingItems = new List<string>();

            //ACD.WR("Openings {0}", ACD.DB._getWallOpenings(wallId).Count);

            foreach(pPos[] _pts in ACD.DB._getWallOpenings(wallId))
            {
                string _css = _pts[0].Content;
                string _cssname = _css.filter("{").First();

                _cssname = increaseCssName(_cssname);

                dictCSS.Add(_cssname, _css.filter("{}").Last() + vertsToCss(_pts));

                openingItems.Add(_cssname);
            }

            if (openingItems.Count > 0)
                info += "openings:" + openingItems.ToTextStr(",") + ";";

            dictCSS.Add(cssname, info);
        }

        static void _outputCss()
        {
            string content = "";
            dictCSS = dictCSS.OrderBy(itm => itm.Key).ToDictionary(itm => itm.Key, itm => itm.Value);
            foreach (var itm in dictCSS)
            {
                content += "\r\n" + itm.Key;
                content += "\r\n{\r\n";
                content += itm.Value.Replace(";", ";\r\n");
                content += "}\r\n";
            }

            File.WriteAllText(@"D:\logcss.txt", content);
        }

        static Dictionary<string, List<object>> dicts;

        static void _saveCSV(IEnumerable<string> ls)
        {
            //ACD.WR("Content: {0}", contents.ToTextStr("\r\n"));
            string res = "<head>\r\n"
                   + "\t<link rel = \"stylesheet\" href = \"styles/planstyle.css\">\r\n"
                   + "</head>\r\n\r\n";

            res += "<body>\r\n";
            res += "<div>\r\n";
            res += "<table>\r\n";
                        
            res += "\t<tr>\r\n";
            
            res += "\t\t<th>STT</th>\r\n";
            res += "\t\t<th>Phân khu</th>\r\n";
            res += "\t\t<th>Diện tích<br>(m2)</th>\r\n";
            res += "\t\t<th>Số lô<br>(lô)</th>\r\n";
            res += "\t\t<th>Biệt thự<br>(lô)</th>\r\n";
            res += "\t\t<th>Liên kế<br>(lô)</th>\r\n";
            
            res += "\t</tr>\r\n";

            int i = 0;

            foreach(string st in ls)
                if(!st.st_(">>"))
                {
                    res += "\t<tr>\r\n";
                    res += "\t\t<th>" + i + "</th>\r\n";

                    int cnt = 0;
                    string[] ar = st.filter(";");

                    foreach (string _s in st.filter(";"))
                    {
                        res += "\t\t<th>" +  _s.filter(" ").First() + "</th>\r\n";
                        cnt++;
                    }

                    res += "\t</tr>\r\n";
                    i++;
                }

            foreach (string st in ls)
                if (st.st_(">>"))
                {
                    res += "\t<tr>\r\n";
                    res += "\t\t<th>" + "</th>\r\n";

                    if (st.st_(">>TỔNG"))
                    {
                        res += "\t\t<th>" + st.filter(";")[0] + "</th>\r\n";
                        res += "\t\t<th>" + st.filter(";")[1] + "</th>\r\n";
                        res += "\t\t<th>" + st.filter(";")[2] + "</th>\r\n";
                    }
                    else
                    {
                        res += "\t\t<th>" + st.filter("=").First() + "</th>\r\n";
                        res += "\t\t<th>" + "</th>\r\n";
                        res += "\t\t<th>" + st.filter("=").Last() + "</th>\r\n";
                    }
                    res += "\t</tr>\r\n";
                }

            res += "</table>\r\n";
            res += "</div>\r\n";
            res += "</body>\r\n";
            
            File.WriteAllText(@"D:/html/table_data.html", res);
        }

        static double _rN(double v, double r)
        {
            return Math.Floor(v / r) * r;
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                pPos[] bb = ACD.DB._getBound(selIds);
                pPos ct = bb.CenterPoint();

                int roundX = 50;
                int roundY = 10;
                //Dictionary<string, string> dicts = new Dictionary<string, string>();

                List<pPos> pts = new List<pPos>();

                foreach (ObjectId id in selIds)
                {
                    ACD.WR("DXF: {0}[{1}] XNote: {2} Color: {3} IsArray: {4}",
                        id.ObjectClass.DxfName, id.Handle.Value, ACD.DB.GetXNotes(id), 
                        ACD.DB._getColorIndex(id), ACD.DB._isArray(id));

                    if(ACD.DB._isBlock(id))
                    {
                        //ACD.WR("Blockname {0} Att {1}", ACD.DB._getIdName(id), ACD.DB.GetAllBlockAtt(id));
                        pPos pt = ACD.DB._getPoint(id);

                        bool found = false;

                        foreach (pPos _p in pts)
                            if (pt._isVeryClosed(_p, 40))
                            {
                                _p.Content += "|" + ACD.DB.GetAllBlockAtt(id);
                                found = true;
                                break;
                            }

                        if (!found)
                        {
                            pt.Content = ACD.DB.GetAllBlockAtt(id);
                            pts.Add(pt);
                        }
                        //ACD.DB._setPoint(id, new pPos(_rN(pt.X, roundX), _rN(pt.Y, roundY)));
                    }
                }

                foreach (ObjectId id in selIds)
                {
                    //ACD.WR("DXF: {0}[{1}] XNote: {2} Color: {3} IsArray: {4}",
                    //    id.ObjectClass.DxfName, id.Handle.Value, ACD.DB.GetXNotes(id),
                    //    ACD.DB._getColorIndex(id), ACD.DB._isArray(id));

                    if (ACD.DB._isText(id))
                    {
                        //ACD.WR("Blockname {0} Att {1}", ACD.DB._getIdName(id), ACD.DB.GetAllBlockAtt(id));
                        pPos pt = ACD.DB._getPoint(id);
                        string k = _rN(pt.X - ct.X, roundX) + "_" + _rN(pt.Y - ct.Y, roundY);

                        foreach (pPos _p in pts)
                            if (pt._isVeryClosed(_p, 40))
                            {
                                //_p.Content += "|" + ACD.DB.GetAllBlockAtt(id);
                         
                                int __cnt = 1;

                                string[] ar = ACD.DB._getContent(id).filter("\r\n");
                                //ACD.WR("T1");
                                if (ar.Length > 0)
                                    foreach (string __s in ar)
                                    {
                                        _p.Content += "|T" + __cnt + "=" + __s;
                                        __cnt++;
                                    }

                                break;
                            }

                        //ACD.DB._setPoint(id, new pPos(_rN(pt.X, roundX), _rN(pt.Y, roundY)));
                    }else if (ACD.DB._isHatch(id))
                    {
                        ACD.WR("Hatch {0}", ACD.DB._getIdInfo(id));
                    }
                }
                
                int total_1 = 0, total_2 = 0;
                double total_s = 0;

                string order_s = "AS5T";
                List<string> ls = new List<string>();

                foreach (pPos _p in pts)
                {
                    string[] pnames = _p.Content._allPropNames();

                    if (pnames.Length > 0)
                    {
                        pnames = pnames.OrderBy(__s => order_s.IndexOf(__s[0])).ToArray();
                        string val = "";

                        foreach (string _k in pnames)
                        {
                            string __st = _p.Content._prop(_k);
                            val += __st + ";";

                            if (__st.ct_("LÔ LIÊN KẾ"))
                                total_2 += (int)__st.filter(" ").First().ToNumber();
                            else if (__st.ct_("LÔ BIỆT THỰ"))
                                total_1 += (int)__st.filter(" ").First().ToNumber();

                            if (_k == "S")
                                total_s += __st.ToNumber();
                        }

                        ls.Add(val);
                    }
                }

                ls = ls.OrderBy(__s => __s._firstProp()).ToList();

                ls.Add(">>LÔ BIỆT THỰ=" + total_1.ToString("N0"));
                ls.Add(">>LÔ LIÊN KẾ=" + total_2.ToString("N0"));
                ls.Add(">>TỔNG;" + total_s.ToString("N0") + ";" + (total_1 + total_2).ToString("N0"));

                ACD.WR(ls.ToTextStr("\r\n"));
                System.Windows.Forms.Clipboard.SetText(ls.ToTextStr("\r\n"));

                _saveCSV(ls);
            }

            ACD.Focus();
        }
    }
}

