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

//
using System.Windows.Forms;
//using SyncObject;

namespace AcadScript
{
    public class BlockExtractCLS
    {
        static string _combineStyle(string content)
        {
            return content._gValue("name")
                + ";" + content._gValue("width")
                + ";" + content._gValue("height");
        }

        static string[] _collectDoor(ObjectIdCollection ids, string blocktagname, pPos pt)
        {
            List<string> res = new List<string>();
            PosCollection door_infos = new PosCollection();
            ObjectIdCollection door_blocks = ids.ToList().Where(id => ACD.DB._isDoor(id)).ToCollection();

            foreach (ObjectId id in door_blocks)
            {
                List<pPos> ls = new List<pPos>();
                ls.Add(ACD.DB._getPoint(id));
                ls.AddRange(ACD.DB._getBound(id));
                ls[0].Content = "<div type=\"" + (id.ObjectClass.DxfName == "AEC_DOOR" ? "door": "window") 
                    + "\" name=\"" + ACD.DB._getIdName(id) + "\" id=\"" + id.Handle.Value 
                    + "\" width=\"" + ACD.DB._getDoorWidth(id) + "\" height=\"" + ACD.DB._getDoorHeight(id) + "\"/>";
                door_infos.Add(ls.ToArray());
            }

            ACD.WR("Door blocks {0}", door_blocks.Count);

            string[] typelist = door_infos.Select(ls => _combineStyle(ls[0].Content))
                .Distinct().OrderBy(s => s).ToArray();

            if (typelist.Length > 0)
            {
                string curkey = "";
                int index = 1;

                ObjectIdCollection doorTagIds = new ObjectIdCollection();

                foreach (string type in typelist)
                {
                    string key = type.Substring(0,2);

                    if (curkey != key)
                    {
                        index = 1;
                    }

                    string[] contents = door_infos.Where(ls =>
                        type == _combineStyle(ls[0].Content)).Select(ls => ls[0].Content).ToArray();

                    if (contents.Length > 0)
                    {
                        string stname = key + (index < 10 ? "0" : "") + index;

                        res.Add(contents[0].Replace("/>",
                            " no=\"" + stname + "\""
                            + " ids=\"" + contents.Select(ls => ls._gValue("id")).ToTextStr(";") 
                            + "\" count=\"" + contents.Length + "\"/>")._eraseValue("id"));

                        foreach(string s in contents)
                            foreach(ObjectId drId in ACD.DB.strToObjectId(s._gValue("id")))
                            {
                                pPos p = ACD.DB._getBound(drId).CenterPoint();

                                if (drId.ObjectClass.DxfName == "AEC_WINDOW")
                                {
                                    pPos[] ls = ACD.DB._getDoor(drId);
                                    p = ls[0].Parallel(ls[1], 500).CenterPoint();
                                }
                                
                                doorTagIds.Add(ACD.DB.Draw2D(new pPos(-750, -250).Rect(1500, 500).Move(p), 
                                    "c", "f 0 250", "f 2 250", "f 4 250", "f 6 250","A-Anno-Dims"));
                                doorTagIds.Add(ACD.DB.CreateText("#M" + stname, p));
                            }
                    }

                    curkey = key;
                    index++;
                }

                if(doorTagIds.Count > 0)
                    ACD.DB.NewBlock(doorTagIds, blocktagname, true, false, pt);
            }

            return res.ToArray();
        }

        static void _buildBlock(ObjectIdCollection ids, string blockname, pPos pt)
        {
            if (ids.Count > 0)
            {
                //string blockname = blocknameprefix + "_" + ids.First().Handle;
                //ACD.WR("Blockname {0} count {1}", blockname.Length, ids.Count);
                ACD.DB.NewBlock(ids, blockname, true, false, pt);
                //ACD.DB.Insert(blockname, pt);
            }
        }
        
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                ObjectIdCollection selIds = ACD.GetSelection();
                string html = "";
                string key = "$tmp";

                if (selIds.Count > 0)
                {
                    pPos pt = null;

                    ObjectId gridId = selIds.ToList().FirstOrDefault(id => ACD.DB._isGrid(id));
                    if (!gridId.IsNull)
                    {
                        pt = ACD.DB._getPoint(gridId);
                        key = "_" + gridId.Handle.ToString();
                    }

                    if(pt == null)
                        pt = ACD.GetPoint();

                    if (pt != null)
                    {
                        _buildBlock(selIds.ToList().Where(id => ACD.DB._isDim(id)).ToCollection(), "dim" + key, pt);
                        _buildBlock(selIds.ToList().Where(id => ACD.DB._isText(id)).ToCollection(),"txt" + key, pt);
                        _buildBlock(selIds.ToList().Where(id => ACD.DB._isLeader(id)).ToCollection(), "lead" + key, pt);

                        string[] door_indors = _collectDoor(selIds.ToList().Where(id => ACD.DB._isDoor(id)).ToCollection(), "tag" + key, pt);
                        html += door_indors.ToTextStr("\r\n");

                        ACD.WR("HTML {0}", html);
                    }

                    File.WriteAllText(@"D:\html\door_schedule.html", html);
                }
                ACD.Focus();
            }
        }
    }
}

