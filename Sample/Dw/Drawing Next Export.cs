using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace AcadScript
{
    

    class nodeElementCLS
    {

        public string[] object_list, layout_list;
//        obj=#type $lamp $lampy
//obj=#d1200 #o-200 #z-100 //distance offset z
//obj=#hiddencross t1
//obj=blue t1
//obj=red t1


        public nodeElementCLS(IEnumerable<string> values)
        {
            object_list = values.Where(s => DrawingNextExportCLS.color_index_list.Contains(s.filter(" ")[0])).ToArray();
            layout_list = values.Where(s => !object_list.Contains(s) && s.Contains("#")).ToArray();
        }

        public double _readValue(string src, string key)
        {
            string val = src.filter(" ").FirstOrDefault(s => s.StartsWith(key));

            if (!val.empty())
                val = val.Substring(key.Length);

            return val.empty() ? -1 : val.ToNumber();
        }
    }

    public class DrawingNextExportCLS
    {
        static string html_content, html_divs, obj_content;
        public static string[] color_index_list = new string[] { "def", "red", "yellow", "green", "cyan", "blue", "margenta", "white", "wall" };
        
        static nodeElementCLS NodeItem;
        static void initHtml()
        {
            html_content += "<style>\r\n"
                + "polyline {\r\n"
                + "fill:none;stroke:black;stroke-width:0.5\r\n"
                + "}\r\n";
        }

        public static string _getIdCss(ObjectId objId)
        {
            pPos basept = ACD.DB._getPoint(objId);
            string[] xnotes = ACD.DB.GetXNotes(objId);
            string res = "";

            Dictionary<string, List<string>> dicts = new Dictionary<string, List<string>>();

            foreach (string xnote in xnotes)
                if (dicts.ContainsKey(xnote._firstPropName()))
                    dicts[xnote._firstPropName()].Add(xnote._firstProp());
                else
                    dicts.Add(xnote._firstPropName(), new List<string>() { xnote._firstProp() });

            foreach (string k in dicts.Keys)
            {
                res += "\r\n" + k + "\r\n{";
                foreach (string s in dicts[k])
                {
                    int n = s.IndexOf("//");
                    string new_s = n == -1 ? s : s.Substring(0, n);

                    foreach (string ch in s.filter(" "))
                        if (color_index_list.Any(c => c == ch || c + "bold" == ch || c + "thin" == ch))
                        {
                            new_s = ch + ":" + s.Replace(ch, "").Trim().Replace("  ", " ");
                            break;
                        }

                    res += "\r\n" + new_s + ";";
                }
                res += "\r\n}\r\n";
            }

            var itm = dicts.FirstOrDefault(i => i.Key == "obj");

            if (!itm.Equals(new KeyValuePair<string, List<string>>()))
            {
                NodeItem = new nodeElementCLS(itm.Value);
            }

            return res;
        }

        public static string _getBlockData(ObjectId objId)
        {
            string res = "";
            pPos basept = ACD.DB._getPoint(objId);

            ACD.DB.BlockEntitiesAction(objId, (ids) =>
            {
                pPos[] bb = ACD.DB._getBound(ids);

                foreach (ObjectId id in ids)
                {
                    pPos[] verts = null;
                    bool closed = false;
                    string color_index = null, content = "";
                    int cindex = ACD.DB._getColorIndex(id);

                    ACD.WR("CIndex {0}", cindex);

                    if (cindex != -1 && cindex < color_index_list.Length)
                        color_index = color_index_list[cindex];
                    else if (cindex == 256)
                        color_index = "red";
                    else
                        color_index = "none";

                    if (ACD.DB._isWall(id) && !ACD.DB._isWallArc(id))
                    {
                        //ACD.WR("IIR2");
                        ObjectId wallId = ACD.DB.GetWallShape(id, 0);
                        verts = ACD.DB._getVertices(wallId, splitarc).Move(basept.Invert);
                        //color_index = "wall";
                    }
                    else if (ACD.DB._isPolyline(id) || ACD.DB._isLine(id) || ACD.DB._isArc(id))
                    {
                        verts = ACD.DB._getVertices(id, splitarc).Move(basept.Invert);
                        closed = ACD.DB._isPolylineClosed(id);

                        if (ACD.DB._getLineworkWidth(id) >= 10)
                            color_index += "bold";

                        content = "<polyline color=\"" + color_index + "\" points=\"" 
                            + verts.Select(p => p.X.roundNumber(0.01) + "," + p.Y.roundNumber(0.01)).ToTextStr(" ")
                            + "\"/>";

                        if(verts.Length > 2)
                            foreach(string s in NodeItem.object_list)
                            {
                                if(s.filter(" ")[0] == color_index)
                                {
                                    int index = (int)NodeItem._readValue(s,"#t");
                                    if(index != -1)
                                    {
                                        string line_layout = NodeItem.layout_list[index];
                                        string obj_name = s.filter(" ").FirstOrDefault(c => c.StartsWith("$"));
                                        double dist = NodeItem._readValue(s, "#d");
                                        double offset = NodeItem._readValue(s, "#o");
                                        double z = NodeItem._readValue(s, "#z");

                                    }
                                }
                            }

                    }
                    else if (ACD.DB._isDoor(id))
                    {
                        verts = ACD.DB._getVertices(id, splitarc).Move(basept.Invert);
                        //verts.AddRange(ACD.DB._getBound(id).Rect());
                        verts = verts.Add(ACD.DB._getDoorDirection(id));

                    }
                        
                    else if (ACD.DB._isBlock(id))
                    {
                        double rot = ACD.DB._getRotation(id).roundNumber();
                        pPos pt = ACD.DB._getPoint(id) - basept;
                        pt.Content = ACD.DB._getIdName(id);
                        pPos scale = ACD.DB._getScale(id);

                        if (rot != 0) pt.Content = "$R~" + rot + "$" + pt.Content;
                        if (!(scale.X == 1 && scale.Y == 1)) pt.Content = "$S~" + scale.X + "_" + scale.Y + "$" + pt.Content;

                        
                        

                    }
                    else if (ACD.DB._isCircle(id))
                    {
                        pPos pt = ACD.DB._getPoint(id) - basept;
                        pt.Z = ACD.DB._getRadius(id);

                        //color_index = color_index_list[ACD.DB._getColorIndex(id)];
                        content = String.Format("<circle color=\"" + color_index + "\" cx = \"{0}\" cy = \"{1}\" r = \"{2}\"/>", pt.X, pt.Y, ACD.DB._getRadius(id));
                        
                        verts = new pPos[] { pt };

                    }
                    else if (ACD.DB._isElip(id))
                    {
                       
                    }
                    else if (ACD.DB._isText(id))
                    {
                        pPos pt = ACD.DB._getPoint(id) - basept;
                        pt.Content = "TXT~" + ACD.DB._getContent(id);

                        
                    }

                    if(!content.empty())
                    {
                        res += content + "\r\n";
                    }
                }
            });

            

            return res;
        }
        

        static int splitarc = 16;
        static pPos basept = new pPos(0, 0);

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                ObjectIdCollection ids = ACD.GetSelection();
                html_divs = "";

                if (ids.Count > 0)
                {
                    initHtml();

                    foreach (ObjectId id in ids)
                        html_content += _getIdCss(id);

                    


                    html_content += "\r\n</style>\r\n";
                    html_content += "\r\n<svg>\r\n";

                    foreach (ObjectId id in ids)
                        if(ACD.DB._isBlock(id))
                        {
                            html_content += _getBlockData(id);
                        }

                    html_content += "\r\n</svg>\r\n";
                    html_content += "\r\n<div>\r\n" + html_divs;
                    html_content += "\r\n</div>\r\n";

                    File.WriteAllText(@"D:\html\htmllog.txt", html_content);
                }
            }

            ACD.Focus();
        }
    }
}

