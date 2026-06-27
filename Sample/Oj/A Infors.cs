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
    public class InfoAllObjectIdsCLS
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

        //static double _getNumberContent(ObjectId id)
        //{
        //    //double v = ACD.DB._getContent(id).Where(ch => "0123456789".Contains(ch)).ToString().ToNumber();
        //    //ACD.WR("V={0}", new string(ACD.DB._getContent(id).Where(ch => "0123456789".Contains(ch)).ToArray()));
        //    return new string(ACD.DB._getContent(id).Where(ch => "0123456789".Contains(ch)).ToArray()).ToNumber();
        //}

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


        static void BlockAllEditAction(Database db, Action<Database, ObjectId> act)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                RXClass rxc = RXClass.GetClass(typeof(BlockReference));
                DBDictionary layouts = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);

                foreach (var entry in layouts)
                {
                    Layout lay = (Layout)tr.GetObject(entry.Value, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(lay.BlockTableRecordId, OpenMode.ForRead);
                        
                    foreach (ObjectId id in btr)
                    {
                        if (id.ObjectClass == rxc)
                        {
                            if (!id.IsNull)
                            {
                                act(db, id);
                                //ACD.WR("IdName {0}", ACD.DB._getIdName(id));

                                if(!ACD.DB._getIdName(id).et_())
                                    ACD.DB.BlockEntitiesEdit(id, _ids =>
                                    {
                                        foreach (ObjectId _id in _ids)
                                            //if (!_id.IsNull)
                                                act(db, _id);
                                    });
                            }
                        }
                        else
                            act(db, id);
                    }
                }

                tr.Commit();
            }
        }

        static void _infoSingleId(Database db, ObjectId id)
        {
            string key = null;

            if (db._isText(id))
                _getMTextStyle(db, id, true);
            else if(db._isDim(id))
            {
                key = db._getIdInfo(id)._firstProp();
                _getDimTextStyle(db, id, true);
            }
            else if (db._isBlock(id))
            {
                key = db._getIdName(id);
                _getBlockAttMStyle(db, id, true);
            }

            if (!key.et_())
            {
                if(!dicts.ContainsKey(key))
                    dicts.Add(key, new List<object> {  });

                dicts[key].Add(id);
            }

            key = db._getLayer(id);

            if (!dicts.ContainsKey(key))
                dicts.Add(key, new List<object> { });

            dicts[key].Add(id);
        }

        static void _getMTextStyle(Database db, ObjectId objId, bool _setdefault = false)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                string key = null;

                if (objId.ObjectClass.DxfName == "MTEXT")
                {
                    MText mtxt = (MText)tr.GetObject(objId, OpenMode.ForWrite);
                    TextStyleTableRecord mstyle = (TextStyleTableRecord)tr.GetObject(mtxt.TextStyleId, OpenMode.ForWrite);

                    key = mstyle.Name;

                    if (_setdefault)
                        mtxt.TextStyleId = db.Textstyle;

                    
                    mtxt.UpgradeOpen();
                }
                else
                {
                    DBText mtxt = (DBText)tr.GetObject(objId, OpenMode.ForWrite);
                    TextStyleTableRecord mstyle = (TextStyleTableRecord)tr.GetObject(mtxt.TextStyleId, OpenMode.ForWrite);

                    key = mstyle.Name;

                    if (_setdefault)
                        mtxt.TextStyleId = db.Textstyle;

                    
                    mtxt.UpgradeOpen();
                }

                if (!key.et_())
                {
                    if (!dicts.ContainsKey(key))
                        dicts.Add(key, new List<object> { });

                    dicts[key].Add(objId);
                    dicts[key].Add("TEXT");
                }

                tr.Commit();
            }
        }

        static void _getDimTextStyle(Database db, ObjectId objId, bool _setdefault = false)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Dimension dim = (Dimension)tr.GetObject(objId, OpenMode.ForWrite);
                TextStyleTableRecord mstyle = (TextStyleTableRecord)tr.GetObject(dim.TextStyleId, OpenMode.ForWrite);

                string key = mstyle.Name;

                if (_setdefault)
                    dim.TextStyleId = db.Textstyle;


                if (!dicts.ContainsKey(key))
                    dicts.Add(key, new List<object> { });

                dicts[key].Add(objId);
                dicts[key].Add("DIM");

                dim.UpgradeOpen();

                tr.Commit();
            }
        }

        static void _getBlockAttMStyle(Database db, ObjectId objId, bool _setdefault = false)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference blockRef = (BlockReference)tr.GetObject(objId, OpenMode.ForWrite);

                foreach (ObjectId attId in blockRef.AttributeCollection)
                {
                    var attDef = tr.GetObject(attId, OpenMode.ForWrite) as AttributeReference;
                
                    if (attDef != null)
                    {
                        TextStyleTableRecord mstyle = (TextStyleTableRecord)tr.GetObject(attDef.TextStyleId, OpenMode.ForWrite);
                        string key = mstyle.Name;

                        if (_setdefault)
                            attDef.TextStyleId = db.Textstyle;

                        if (!dicts.ContainsKey(key))
                            dicts.Add(key, new List<object> { });

                        dicts[key].Add(objId);
                        dicts[key].Add("ATT");

                        attDef.DowngradeOpen();
                    }
                }

                blockRef.UpgradeOpen();
                
                tr.Commit();
            }
        }

        public static void Main(string[] args)
        {
            //ACD.WR("ok0");
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();
                List<double> ls = new List<double>();

                double[] indexes = new double[] { 100, 120, 135, 150, 175, 185, 200, 220, 250, 275, 300, 
                    320, 335, 350, 375, 400, 425, 450, 475, 500, 525, 
                    560, 580, 620, 650, 675, 720, 750, 840, 900, 950, 1000, 1100, 1900, 2000 };

                foreach(ObjectId _id in selIds)
                {
                    if(ACD.DB._isText(_id))
                    {
                        double v = ACD.DB._getContent(_id).ToNumber();

                        if (v > 100)
                        {
                            for (int i = 0; i < indexes.Length - 1; i++)
                                if (v > indexes[i] && v < indexes[i + 1])
                                {
                                    if (Math.Abs(indexes[i] - v) > Math.Abs(indexes[i + 1] - v))
                                        v = indexes[i + 1];
                                    else
                                        v = indexes[i];

                                    break;
                                }

                            if (!ls.Contains(v)) ls.Add(v);
                            ACD.DB._setContent(_id, v.ToString());
                        }

                        
                    }
                }

                ls = ls.OrderBy(v => v).ToList();
                  
                ACD.WR(ls.ToTextDouble(","));
                System.Windows.Forms.Clipboard.SetText(ls.ToTextDouble(","));
            }

            ACD.Focus();
        }
    }
}

