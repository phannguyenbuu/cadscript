//using System.Drawing;
using System;
//using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;

//using Autodesk.AutoCAD.ApplicationServices;
//using Autodesk.AutoCAD.EditorInput;
//using Autodesk.AutoCAD.DatabaseServices;
//using Autodesk.AutoCAD.Runtime;
//using Autodesk.AutoCAD.Geometry;
//using Autodesk.AutoCAD.Colors;
//using Autodesk.AutoCAD.Internal;


//using System.Windows.Forms;
//using SyncObject;
//using Photoshop;

namespace AcadScript
{
    public class WriteHtmlCLS
    {
        public static Dictionary<string, List<string>> dict_contents;
        static cReadData CssData;
        static List<string> Contents;
        public static Dictionary<string, List<string>> VrayPyContents, MXSContents;
        static int W, H;

        public static void AddVrayCategory(string category)
        {
            if (VrayPyContents == null)
                VrayPyContents = new Dictionary<string, List<string>>();
            //ACD.WR("category:{0}", category);
            if (!VrayPyContents.ContainsKey(category))
                VrayPyContents.Add(category, new List<string>());
        }

        public static void AddMXSCategory(string category)
        { 
            if (MXSContents == null)
                MXSContents = new Dictionary<string, List<string>>();

            if (!MXSContents.ContainsKey(category))
                MXSContents.Add(category, new List<string>());
        }

        public static void AddVrayItem(string category, string value)
        {
            AddVrayCategory(category);
            //ACD.WR("K:[{0}]", category);
            VrayPyContents[category].Add(value);
        }

        public static void AddMXSItem(string category, string value)
        {
            AddMXSCategory(category);
            MXSContents[category].Add(value);
        }

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
            dict_contents[category].Add(value);
        }

        public static void AddVrayExtrude(IEnumerable<pPos> pts, double amount, string mtl)
        {
            string key = mtl + "@" + amount;
            var _pts = pts.ToArray();

            if (dictMeshExtrudes.ContainsKey(key))
                dictMeshExtrudes[key].Add(_pts);
            else
                dictMeshExtrudes.Add(key, new PosCollection() { _pts });
        }

        public static void AddVraySweep(IEnumerable<pPos> pts,
            IEnumerable<pPos> section, bool closed_path,
            string mtl, string tex_displacement = null)
        {
            string key = mtl + "@" + section.ToText(true);
            var _pts = pts.ToArray();

            if (dictMeshSweeps.ContainsKey(key))
                dictMeshSweeps[key].Add(_pts);
            else
                dictMeshSweeps.Add(key, new PosCollection() { _pts });
        }

        static void _updateMXSAndVrayPts(PosCollection pls, string key, Action<string> __fnMXS, Action<string> __fnVray)
        {
            string __vray_txt = "";

            for (int i = 0; i < pls.Count; i++)
            {
                List<pPos> __pts = new List<pPos>() { pls[i][0] };

                for (int j = 1; j < pls[i].Length; j++)
                    if (!pls[i][j]._isVeryClosed(__pts.Last()))
                        __pts.Add(pls[i][j]);

                __vray_txt += "[" + __pts.Select(_p => "(" + _p.Round(2) + ",0)").ToTextStr(",") + "],";

                string __mxs_txt = String.Format("shp = drawPointListPolyline #({0}) closed:true name:\"{1}\"",
                     __pts.Select(_p => "[" + _p.Round(2) + ",0]").ToTextStr(","), key);

                __fnMXS(__mxs_txt);
            }

            if (__vray_txt.EndsWith(",")) 
                __vray_txt = __vray_txt.Substring(0, __vray_txt.Length - 1);
            
            __fnVray(__vray_txt);
        }

        public static void PrintMeshSweep()
        {
            foreach (var itm in dictMeshSweeps)
            {
                string mtl = itm.Key.filter("@").First();
                pPos[] section = new PosCollection(itm.Key.filter("@").Last()).First();

                _updateMXSAndVrayPts(itm.Value, itm.Key,
                    (s) =>
                    {
                        AddMXSItem("Mesh", s);
                    },
                    (s) =>
                    {
                        AddVrayItem("Mesh", String.Format("vrs.AddMesh(p3d.SweepShape([{0}], [{1}]), position = [({2},{3})], mtl = '{4}')",
                            s, section.Select(_p => "(" + _p.Round(2) + ")").ToTextStr(","), cReadData.html_basepoint.Round(2), 0, mtl));
                    });
            }
        }


        public static void PrintMeshExtrude()
        {
            foreach (var itm in dictMeshExtrudes)
            {
                var basept = cReadData.html_basepoint;

                string mtl = itm.Key.filter("@").First();
                double amount = itm.Key.filter("@").Last().ToNumber();
               
                if (amount < 0)
                {
                    basept.Z += amount;
                    amount = -amount;
                }

                _updateMXSAndVrayPts(itm.Value, itm.Key,
                   (s) =>
                   {
                       AddMXSItem("Mesh", s);
                       AddMXSItem("Mesh", String.Format("addmodifier shp (extrude amount:{0})", amount));
                       AddMXSItem("Mesh", String.Format("append objs shp"));
                       AddMXSItem("Mesh", String.Format("\n"));
                   },
                   (s) =>
                   {
                       AddVrayItem("Mesh", String.Format("vrs.AddMesh(p3d.ExtrudeShape([{0}], {1}), position = [({2},{3})], mtl = '{4}')",
                        s, amount, basept.Round(2), 0, mtl));
                   });
            }
        }

        public static List<gVrayProxyElement> VrayProxies = new List<gVrayProxyElement>();
        public static Dictionary<string, PosCollection> dictMeshExtrudes = new Dictionary<string, PosCollection>();
        public static Dictionary<string, PosCollection> dictMeshSweeps = new Dictionary<string, PosCollection>();

        public static void AddVrayProxy(string proxyname, IEnumerable<pPos> pts, double scale = 1)
        {
            if (VrayProxies == null)
                VrayProxies = new List<gVrayProxyElement>();

            //var _pts = pts.Move(cReadData.html_basepoint.Invert * cReadData.__sc);

            var ar = VrayProxies.Where(itm => itm.ProxyName == proxyname).ToArray();

            if (ar.Length > 0)
            {
                ar[0].AddRange(pts);
                ar[0].Scale.AddRange(DE.NumericArray(0, pts.Count() - 1).Select(__ => scale));
            }
            else
                VrayProxies.Add(new gVrayProxyElement(proxyname, pts, scale));
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

        static void _runPython()
        {
            //System.Diagnostics.Process.Start(@"D:\html\run _vray_render.bat");
            Process.Start("C:\\WINDOWS\\system32\\cmd.exe", "/k C:\\Python312\\python \"D:\\html\\vray\\user_run.py\"");
        }

        public static void WriteMXS(string mxs_file_name)
        {
            string cmd = "FileIn @\"D:\\Dropbox\\VS Projects\\MaxScriptLib\\MaxScriptLib\\{0}.ms\"";
            mxs_file_name = @"D:\html\" + mxs_file_name + ".ms";

            List<string> __contents = new List<string>();

            __contents.Add(String.Format(cmd, "MergePolygonLib"));
            __contents.Add(String.Format(cmd, "PolyElementLib"));
            __contents.Add(String.Format(cmd, "BoundingBoxLib"));
            __contents.Add(String.Format(cmd, "GeoCalculate"));

            __contents.Add("objs = #()");

            PrintMeshExtrude();
            PrintMeshSweep();

            foreach (string k in MXSContents.Keys.OrderBy(_s => _s))
                __contents.AddRange(MXSContents[k]);

            __contents.Add(String.Format("new_obj = MergePolygon objs newname:\"{0}\"", cReadData.super_key));
            __contents.Add(String.Format("bb = BoundingObj new_obj"));
            __contents.Add(String.Format("new_obj.pivot = [0,0,0]"));
            __contents.Add(String.Format("for i = 1 to polyop.getNumFaces new_obj do"));
            __contents.Add(String.Format("    polyop.setFaceMatID new_obj i 1"));
            __contents.Add(String.Format("select new_obj"));

            

            File.WriteAllLines(mxs_file_name, __contents.ToArray());
        }

        public static void WriteVrayPython(string vray_python_file_name, bool with_run = false, 
            double sun_brightness = 0.05,
            double[] sun_pos = null,
            double[] cmr = null)

        {
            string srcfile = Path.GetDirectoryName(vray_python_file_name) + @"\_main_vray_scene.py";

            List<string> __src = File.ReadAllLines(srcfile).ToList();
            List<string> __contents = new List<string>();
            List<string> __end_contents = new List<string>();
            int __mode_content = 0;

            foreach (string __s in __src)
                if (__mode_content == 0)
                {
                    __contents.Add(__s);
                    if (__s.ct_("#ENVIROMENT"))
                        __mode_content = 1;
                }
                else if (__mode_content == 1 && __s.ct_("#CAMERA"))
                {
                    __mode_content = 2;
                    __end_contents.Add(__s);
                }
                else if (__mode_content == 2)
                {
                    if (__s.st_("target = "))
                        __end_contents.Add("target = [" + (cReadData.size.X / 2).roundNumber() + ", -" + (cReadData.size.Y / 2).roundNumber() + ",0]");
                    else if (__s.st_("radius = "))
                        __end_contents.Add("radius = " + ((cReadData.size.X / 2).roundNumber() + 10000));
                    else if (__s.st_("cmrs = "))
                    {


                        __end_contents.Add(__s.Replace(", 32", ", " + cReadData.number_of_round_cameras)
                            .Replace(", 20000", ", " + cReadData.camera_height));


                    }
                    else
                        __end_contents.Add(__s);
                }

            List<double[]> sun_list = new List<double[]>()
            {
                new double[] { -0.8,0.7,0.0,-0.2,-0.2,0.9,0.6,0.6,0.2,50000.0,50000.0,20000.0},
                new double[] { -0.8,-0.8,0.0,0.1,-0.2,0.9,-0.7,0.6,0.2,-50000.0,50000.0,20000.0},
                new double[] { 0.7,-0.8,0.0,0.1,0.1,0.9,-0.7,-0.7,0.2,-50000.0,-50000.0,20000.0},
                new double[] { 0.7,0.7,0.0,-0.2,0.1,0.9,0.6,-0.7,0.2,50000.0,-50000.0,20000.0}
            };

            if (sun_pos == null) 
                sun_pos = sun_list[0];

            __contents.Add(String.Format("vrs.addLight('{0}',{1}, tm = [{2}], target =[0, 0, 0])", 
                "Sun01", sun_brightness, sun_pos.ToTextDouble(",")));

            VrayProxies = VrayProxies.OrderBy(__itm => __itm.ProxyName).ToList();
            foreach (var itm in VrayProxies)
                itm.ToHtml();
            
            dictMeshExtrudes = dictMeshExtrudes.OrderBy(__itm => __itm.Key).ToDictionary(__itm => __itm.Key, __itm => __itm.Value);
            dictMeshSweeps = dictMeshSweeps.OrderBy(__itm => __itm.Key).ToDictionary(__itm => __itm.Key, __itm => __itm.Value);

            PrintMeshExtrude();
            PrintMeshSweep();

            foreach (string k in VrayPyContents.Keys.OrderBy(_s => _s))
                __contents.AddRange(VrayPyContents[k]);

            __contents.AddRange(__end_contents);

            File.WriteAllLines(vray_python_file_name, __contents.ToArray());

            if(with_run)
                _runPython();
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
            

            Contents = new List<string> { String.Format("\t<svg id='mySvg' width='{0}' height='{1}' viewBox = '{2} {3} {0} {1}'>",
                W, H, cReadData.SuperKeyValue("x").ToNumber(), cReadData.SuperKeyValue("y").ToNumber())};

            
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
            }

            File.AppendAllText(html_file_name, str);
        }
    }
}