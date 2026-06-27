using System.Drawing;
using System;
using System.Globalization;
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
//using Photoshop;

namespace AcadScript
{
    public class WriteHtmlCLS
    {
        public static Dictionary<string, List<string>> dict_contents;
        static cReadData CssData;
        static List<string> Contents;
        static int W, H;

        public static void AddCategory(string category)
        {
            if (dict_contents == null)
                dict_contents = new Dictionary<string, List<string>>();
            //ACD.WR("category:{0}", category);
            if (!dict_contents.ContainsKey(category))
                dict_contents.Add(category, new List<string>());
        }

        public static void AddItem(string category, string value)
        {
            AddCategory(category);
            //ACD.WR("K:[{0}]", category);
            dict_contents[category].Add(value);
        }

        
        static string _repLessTen(string s)
        {
            string snum = "0123456789";
            int index = -1;

            foreach (char c in snum)
            {
                index = s.IndexOf(c);

                if (index != -1)
                    break;
            }

            if(index != -1)
            {
                string val = s.Substring(index);
                
                if(val.ToNumber() < 10)
                    s = s.Replace(val, "0" + val);
                
            }

            return s;
        }


        public static string __comment(string s)
        {
            return "<!-- " + s + " -->\r\n";
        }

        //static string[] _groupTextLines(List<string> srcs, string type, string key)
        //{
        //    string[] ls = srcs.Where(__s => __s.ct_("<" + type)).ToArray();
        //    string[] _new_datas = ls.Select(__s => __s.Trim().filter("</>").First().Replace("' ","'| ")).ToArray();
        //    string[] clrs = _new_datas.Select(__s => __s._prop(key)).Distinct().ToArray();

        //    List<string> res = new List<string>();

        //    foreach(string clr in clrs)
        //    {
        //        res.Add(clr.et_() ? "\t<g>" : "\t<g " + key + "=" + clr + ">");

        //        for(int i = 0; i < ls.Length; i++)
        //            if(_new_datas[i]._prop(key) == clr)
        //            {
        //                res.Add("\t\t" + ls[i].Replace(" " + key + "=" + clr, ""));
        //            }

        //        res.Add("\t</g>");
        //    }

        //    return res.ToArray();
        //}

        static void _addDropShadowFX()
        {
            Contents.Add("\t\t<filter id = 'dropshadow' x = '-2' y = '-2' width = '200' height = '200'>");
            Contents.Add("\t\t\t<feGaussianBlur stdDeviation = '1'/>");
            Contents.Add("\t\t</filter>");
        }

        public static string DefaultHtmlCss(string css_file_name, string title)
        {
            string content = "<head>\r\n"
                   + "\t<link rel = 'stylesheet' href = 'styles/" + css_file_name + "'>\r\n"
                   + "\t<title>" + title + "</title>\r\n"
                   + "</head>\r\n\r\n";

            return content;
        }


        public static void WriteHtml(string html_file_name, string css_file_name, string title,
            cReadData _css, int _w, int _h, string table_data = null)
        {
            CssData = _css;
            
            W = _w;
            H = _h;

            html_file_name = @"D:\html\" + html_file_name + ".html";

            File.WriteAllText(html_file_name, DefaultHtmlCss(css_file_name, title)
                + "<style>\r\n"
                + CssData.HtmlCssList.OrderBy(s => s).ToTextStr("\r\n") 
                //+ CssData.StyleList.OrderBy(s => s._firstPropName())
                //    .Select(s => s._firstPropName() + "\r\n{" + s._firstProp() + "\r\n}\r\n").ToTextStr("\r\n")
                + "</style>\r\n\r\n");

            string[] html_keys = new string[] { "<polyline", "<polygon", "<circle", "<div", "<text" };

            var ls = CssData.html_contents.Distinct().OrderBy(s => DE.NumericArray(0, html_keys.Length - 1)
                .FirstOrDefault(n => s.ct_(html_keys[n])))
                .ThenBy(s => s.ct_(">") ? _repLessTen(s.Substring(s.IndexOf(">") + 1)) : "")
                .ToList();

            //ACD.WR("Width x Height {0} x {1}", cReadData.SuperKeyValue("x").ToNumber(), cReadData.SuperKeyValue("y").ToNumber());
            

            Contents = new List<string> { String.Format("\t<svg width='{0}' height='{1}' viewBox = '{2} {3} {0} {1}'>",
                W, H * 2, cReadData.SuperKeyValue("x").ToNumber(), cReadData.SuperKeyValue("y").ToNumber())};

            
            _addDropShadowFX();

            //ACD.WR("End {0}", dict_contents.Count);
            //return;

            var __ls = new List<string>();

            if (dict_contents != null)
            {
                dict_contents = dict_contents.OrderBy(_itm => _itm.Key)
                    .ToDictionary(_itm => _itm.Key, _itm => _itm.Value);

                foreach (var _itm in dict_contents)
                {
                    //ACD.WR("Item {0}-{1};", _itm.Key, _itm.Value.Count);
                    Contents.Add("\t\t<g class='" + _itm.Key + "'>");

                    foreach (var _s in _itm.Value)
                        Contents.Add("\t\t\t" + _s);

                    Contents.Add("\t\t</g>");
                }
            }

            Contents.Add("\t</svg>");
            Contents.Add("\n");
                        
            string str = "<div class='centered'>\r\n" + Contents.ToTextStr("\r\n") + "</div>\r\n";

            if (!table_data.et_())
            {
                str += table_data + "\r\n"

                    + "<script async src = 'https://unpkg.com/es-module-shims@1.3.6/dist/es-module-shims.js'></script>\r\n"
                    + "<script type = 'importmap'>\r\n"
                    + "\t{\r\n"
                    + "\t\t\"imports\": {\r\n"
                    + "\t\t\t\"three\": \"./colla/build/three.module.js\",\r\n"
                    + "\t\t\t\"three/addons/\": \"./colla/jsm/\"\r\n"
                    + "\t\t}\r\n"
                    + "\t}\r\n"

                    + "</script>\r\n"
                    + "<script type = \"module\" src = \"colla/build/collada.js\"></script>\r\n"
                    + "<script src = \"script/plan.js\"></script>";

                //string _script_plan = @"D:\html\script\plan.js";

                //if (File.Exists(_script_plan))
                //{
                //    str += "<script>\r\n";
                //    str += File.ReadAllText(_script_plan);
                //    str += "\r\n</script>\r\n";
                //}
            }

            File.AppendAllText(html_file_name, str);
        }
    }
}