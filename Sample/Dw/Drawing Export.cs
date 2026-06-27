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
    

    

    public class DrawingExportCLS
    {
        static Dictionary<string, string> dicts;
        //static string prefix = "";
        static string[] current_xnotes = null;

        static string _convertXNote(IEnumerable<string> ls)
        {
            return ls.Select(st => st.Replace("=", "~")).ToTextStr("$");
        }

        static void _setValue(string content, object verts_value)//, string xnote)
        {
            dicts[content] += verts_value + ";;";
        }

        public static string[] GetAllXNotes(ObjectIdCollection ids)
        {
            return ids.ToList().SelectMany(id => ACD.DB.GetXNotes(id)).Distinct().ToArray();
        }

        

        static void _getArrayInfo(ObjectId id)
        {
            ACD.DB.BlockEntitiesAction(id, (subIds) =>
            {
                PosCollection pls = ACD.DB._getAllVertices(subIds).Move(basept.Invert)
                    .Select(pts => pts.Boundary()).ToCollectionSameClosed(false);

                ACD.DB.GetEntities(null, EN_SELECT.AC_DXF, "MTEXT", "TEXT");
                pPos[] txtPts = IR.SelectedIds.ToList().Select(txtId => ACD.DB._getPoint(txtId)).ToArray();
                txtPts = txtPts.Where(p => p.Content.st_(".")).ToArray();

                string st = "";

                for (int i = 0; i < pls.Count; i++)
                {
                    string note = txtPts.Where(p => p.InsideRect(pls[i][0], pls[i][1]))
                                        .Select(p => p.Content).ToTextStr(",");

                    st += "Zone" + i
                        + (!note.empty() ? note : "")
                        + "=" + pls[i].ToText() + "\r\n";
                }

                //ACD.DB.SetXNotes(id, st);
            });
        }
        
        static string html_content;
        static double[] getElipInfo(Database db, ObjectId objId)
        {
            double[] res = null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (objId.ObjectClass.DxfName == "ELLIPSE")
                {
                    Ellipse elp = (Ellipse)tr.GetObject(objId, OpenMode.ForRead);
                    double rot = elp.StartPoint.ToPos().AngleVector(elp.Center.ToPos(), elp.StartPoint.ToPos() + new pPos(1, 0));
                    res = new double[] { elp.MajorRadius, elp.MinorRadius, rot };
                    
                }
                
                tr.Commit();
            }
            return res;
        }

        static void initHtml(ObjectIdCollection ids)
        {
            pPos[] bb = ACD.DB._getBound(ids);

            html_content = "<head>\r\n"
                        + "\t<link rel = \"stylesheet\" href = \"styles/cadstyle.css\">\r\n"
                        + "</head>\r\n\r\n"
                        + String.Format("<svg width=\"{0}\" height=\"{1}\">\r\n", bb.Size().X, bb.Size().Y);
        }

        static bool _comprVals(string condition)
        {
            string[] ar = condition.filter("<>=");
            double val1 = ar.First().ToNumber();
            double val2 = ar.Last().ToNumber();

            //if (condition.ct_("<"))
                //ACD.WR("CPR: {0},{1},{2}", condition, val1,val2);

            bool res = false;

            if ((condition.ct_(">>") && val1 >= val2)
                || (condition.ct_("<") && val1 <= val2)
                || (condition.ct_(">") && val1 > val2)
                || (condition.ct_("<<") && val1 < val2)
                || (val1 == val2))
                res = true;

            
            return res;
        }

        static bool isVerts_(pPos[] pts, string xnote)
        {
            pPos[] bb = pts.Boundary();
            pPos sz = bb.Size();
            PosCollection segs = pts.GetSegment().OrderBy(sg => sg.Length(false)).ToCollectionSameClosed(false);

            string[] ar = xnote.filter(";");

            for (int i = 0; i < ar.Length; i++)
            {
                string res = ar[i];
                bool equal = !res.ct_(">") && !res.ct_("<");

                if (res.st_("n"))
                {
                    res = pts.Length + (equal ? "=" : "") + res.Substring(1);

                    //if (ar[i] == "n2")
                        //ACD.WR("N2: {0},{1},{2}", ar[i], res, !_comprVals(res));

                }
                else if (res.st_("seg_") && res.ct_("<"))
                    res = segs.First().Length(false) + res.Substring(4);
                else if (res.st_("seg_") && res.ct_(">"))
                    res = segs.Last().Length(false) + res.Substring(4);
                else if (res.st_("seg") && res.ct_("<"))
                    res = segs.Last().Length(false) + res.Substring(3);
                else if (res.st_("seg") && res.ct_(">"))
                    res = segs.First().Length(false) + res.Substring(3);

                

                else if (res.st_("s") && res.ct_(">"))
                    res = Math.Min(sz.X, sz.Y) +  res.Substring(1);
                
                

                else if (res.st_("r") && res.ct_(">"))
                    res = Math.Max(sz.X, sz.Y) +  res.Substring(1);
                else if (res.st_("s") && res.ct_("<"))
                    res = Math.Max(sz.X, sz.Y) +  res.Substring(1);
                else if (res.st_("r") && res.ct_("<"))
                    res = Math.Min(sz.X, sz.Y) +  res.Substring(1);
                else if (res.st_("w"))
                    res = sz.X + (equal ? "=" : "") + res.Substring(1);
                else if (res.st_("h"))
                    res = sz.X + (equal ? "=" : "") + res.Substring(1);

                if (!_comprVals(res))
                    return false;
            }

            return true;
        }

        static string _getPtsNodeName(pPos[] pts, string[] xnotes, bool closed)
        {
            //ACD.WR("R0_{0}", xnotes.ToTextStr("\r\n"));
            string[] datas = xnotes.Where(s => s.st_("node.filter.") || s.st_("$"))
                .Select(s => s.Replace("node.filter.", "$")).ToArray();
            string res = null;
            string defname = null;

            foreach (var xnote in datas)
            {
                string key = xnote._firstPropName();
                string val = xnote._firstProp();

                //if (val.ct_("n2"))
                //    ACD.WR("XN2: {0},{1},{2}", val, closed, !val.ct_("cl"));

                if (val.ct_("def"))
                    defname = key;
                else
                {
                    if ((val.ct_("cl") && (closed
                        || pts.First()._isVeryClosed(pts.Last())))
                        || (!closed && !val.ct_("cl")))
                    {
                        if (isVerts_(pts, val))
                        {
                            res = key;
                            break;
                        }
                    }
                }
            }

            return res.et_() ? defname : res;
        }

        static string _getContentFromColorIndex(string content, ObjectId id)
        {
            int color_index = ACD.DB._getColorIndex(id);

            string[] ar = content.filter("()");
            string res = content;

            if (ar.Length > color_index)
            {
                res = ar[color_index];
                ar = res.filter(" ");

                //ACD.WR("ARR {0}", ar.ToTextStr(","));
                res = "";

                for (int ii = 0; ii < ar.Length; ii++)
                    if(!ar[ii].st_("#"))
                    {
                        string s = ar[ii];

                        List<double> ls = new List<double>();
                        string[] _ar = (s.st_("h") ? s.Substring(1) : s).filter("&");

                        for (int i = 0; i < _ar.Length; i++)
                        {
                            string _s = _ar[i];

                            if (_s.ct_("x"))
                                ls.AddRange(DE.NumericArray(0,
                                    (int)(_s.filter("x").Last().ToNumber()) - 1)
                                    .Select(_i => _s.filter("x").First().ToNumber() * 100));
                            else
                                ls.Add(_s.ToNumber() * 100);

                            _ar[i] = ls.ToTextDouble("&");
                        }

                        ar[ii] = (ar[ii].st_("h") ? "h" : "") + _ar.ToTextStr("&");
                    }

                res = ar.ToTextStr(" ");
            }

            return res;
        }

        static pPos pt_start_point;

        public static void DataToClipboard(ObjectIdCollection ids, IEnumerable<string> _xnotes = null)
        {
            if (basept == null)
                basept = new pPos(0, 0);
            //ACD.WR("OKr1");
            pPos[] bb = ACD.DB._getBound(ids);
            //ACD.WR("OKr1.1");
            PosCollection openings = new PosCollection();
            //ACD.WR("OKr2 {0}", ids.Count);
            double w = 1000;// (ACD.DB._getBound(ids)[1].X - basept.X).roundNumber(100);
            double h = 1000; // (ACD.DB._getBound(ids)[1].Y - basept.Y).roundNumber(100);

            foreach (ObjectId id in ids)
                if (ACD.DB._isCircle(id))
                {
                    pt_start_point = ACD.DB._getPoint(id);
                }

            foreach (ObjectId id in ids)
                if (ACD.DB._isArray(id))
                {
                    _getArrayInfo(id);
                }
                else if (ACD.DB.ValidId(id) && !ACD.DB._isDim(id) && !ACD.DB._isHatch(id))
                {
                    double z_depth = ACD.AllSelectionXNotes._props("Z").ToNumber();

                    //current_xnotes = current_xnotes.filter("|").Distinct().ToTextStr("|");//._removeProp("H");

                    if (current_xnotes != null)
                        foreach (string sname in current_xnotes._allPropNames())
                        if(sname == "H" || current_xnotes._props(sname).Contains(",")|| current_xnotes._props(sname).Contains(";"))
                            current_xnotes = current_xnotes._removeProp(sname);

                    string content = "";
                    string[] xnotes = ACD.DB.GetXNotes(id);

                    string _prefix = ACD.DB._getLayer(id).filter("_-").First();

                    if (_xnotes != null)
                    {
                        xnotes = _xnotes.ToArray();
                        _prefix = _getContentFromColorIndex(xnotes._props("prefix"), id); 
                    }

                    if (ACD.DB._isPolyline(id) || ACD.DB._isLine(id) || ACD.DB._isArc(id) || ACD.DB._isDoor(id))
                        content = "#LW=" + _prefix;// + ACD.DB._getLayer(id).filter("_-").First();
                    else if (ACD.DB._isWall(id))
                        content = "#WALL=" + _prefix;// + ACD.DB._getLayer(id).filter("_-").First();
                    else if (ACD.DB._isDoor(id))
                        content = "#DR=" + _prefix;// + ACD.DB._getLayer(id).filter("_-").First();
                    else if (ACD.DB._isBlock(id))
                        content = "#INS=" + _prefix;// + ACD.DB._getIdName(id).filter("_-").First();
                    else if (ACD.DB._isText(id))
                        content = "#TXT=" + _prefix;// + ACD.DB._getTextHeight(id);
                    else if (ACD.DB._isCircle(id) || ACD.DB._isElip(id))
                    {
                        content = "#CIR=" + _prefix; //+ ACD.DB._getLayer(id).filter("_-").First();
                    }
                    else if (ACD.DB._isElip(id))
                        content = "#ELP=" + _prefix; //+ ACD.DB._getLayer(id).filter("_-").First();
                                        
                    pPos[] _checkverts = ACD.DB._getVertices(id, 0);

                    if (!content.et_())
                    {
                        if (dicts.Keys.ToList().IndexOf(content) == -1)
                            dicts.Add(content, "");

                        if (ACD.DB._isWall(id) && !ACD.DB._isWallArc(id))
                        {
                            //ACD.WR("IIR2");
                            ObjectId wallId = ACD.DB.GetWallShape(id, 0);
                            pPos[] verts = ACD.DB._getVertices(wallId, splitarc).Move(basept.Invert);
                            _checkverts = ACD.DB._getVertices(wallId, 0);

                            if (xnotes != null)
                            {
                                string xname = _getPtsNodeName(_checkverts, xnotes, true);

                                if (!xname.et_())
                                {
                                    content = "#WALL=" + xname;
                                    if (dicts.Keys.ToList().IndexOf(content) == -1)
                                        dicts.Add(content, "");
                                }
                            }

                            ACD.DB.EraseObject(wallId);
                            openings.AddRange(ACD.DB._getWallOpeningPos(id));

                            verts[0].Content += "H" + ACD.DB._getWallHeight(id).roundNumber();

                            if (current_xnotes != null)
                                verts[0].Content += "$" + _convertXNote(current_xnotes);

                            if (z_depth != 0)
                                foreach (pPos p in verts) p.Z = z_depth;

                            _setValue(content, verts.Select(p => p.Round(1)).ToText());

                            html_content += "<polyline class=\"style" + ACD.DB._getIdName(id) 
                                + "\" points=\"" + verts.Select(p => p.X + "," + (h - p.Y)).ToTextStr(" ");
                        }
                        else if (ACD.DB._isPolyline(id) || ACD.DB._isLine(id) ||ACD.DB._isMLine(id))
                        {
                            pPos[] verts = ACD.DB._getVertices(id, splitarc).Move(basept.Invert);
                            bool closed = ACD.DB._isPolylineClosed(id);

                            if (xnotes != null)
                            {
                                string xname = _getPtsNodeName(_checkverts, xnotes, closed);

                                //ACD.WR("Xname {0}", xname);

                                if (!xname.et_())
                                {
                                    content = "#LW=" + xname;
                                    if (dicts.Keys.ToList().IndexOf(content) == -1)
                                        dicts.Add(content, "");
                                }
                            }

                            if (current_xnotes != null)
                                verts[0].Content += "$" + _convertXNote(current_xnotes);

                            if (z_depth != 0)
                                foreach (pPos p in verts) p.Z = z_depth;

                            if (closed)
                                verts = verts.Add(verts[0]);

                            _setValue(content, verts.ToText(closed));

                            html_content += "<polyline points=\"" 
                                + verts.Select(p => p.X.roundNumber(0.01) + "," + p.Y.roundNumber(0.01)).ToTextStr(" ")
                                + "\"/>";
                        }
                        else if (ACD.DB._isArc(id))
                        {
                            //ACD.WR("_SPLITARC: {0}", splitarc);
                            pPos[] verts = ACD.DB._getVertices(id, splitarc).Move(basept.Invert);
                            //verts[0].Content = "LAYER~" + ACD.DB._getLayer(id);

                            if (xnotes != null)
                            {
                                string xname = _getPtsNodeName(_checkverts, xnotes, false);

                                if (!xname.et_())
                                {
                                    content = "#LW=" + xname;
                                    if (dicts.Keys.ToList().IndexOf(content) == -1)
                                        dicts.Add(content, "");
                                }
                            }

                            if (current_xnotes != null)
                                verts[0].Content += "$" + _convertXNote(current_xnotes);

                            if (z_depth != 0)
                                foreach (pPos p in verts) p.Z = z_depth;

                            _setValue(content, verts.ToText(false));

                            html_content += "<polyline id=\"arc\" class=\"style" + ACD.DB._getLayer(id)
                               + "\" points=\"" + verts.Select(p => p.X + "," + (h - p.Y)).ToTextStr(" ")
                                + "\"/>";

                        }
                        else if (ACD.DB._isDoor(id))
                        {
                            List<pPos> verts = ACD.DB._getVertices(id, splitarc).Move(basept.Invert).ToList();
                            verts.AddRange(ACD.DB._getBound(id).Rect().Move(basept.Invert));

                            if (xnotes != null)
                            {
                                string xname = _getPtsNodeName(_checkverts.ToArray(), xnotes, false);

                                if (!xname.et_())
                                {
                                    content = "#DR=" + xname;
                                    if (dicts.Keys.ToList().IndexOf(content) == -1)
                                        dicts.Add(content, "");
                                }
                            }

                            if (current_xnotes != null)
                                verts[0].Content += "$" + _convertXNote(current_xnotes);

                            _setValue(content, verts.ToText(false));

                            html_content += "<polyline id=\"door\" class=\"style" + ACD.DB._getIdName(id)
                                + "\" points=\"" + verts.Select(p => p.X + "," + (h - p.Y)).ToTextStr(" ")
                                + "\"/>";
                        }
                        
                        else if (ACD.DB._isBlock(id))
                        {
                            double rot = ACD.DB._getRotation(id).roundNumber();
                            pPos pt = ACD.DB._getPoint(id) - basept;
                            pt.Content = ACD.DB._getIdName(id);
                            pPos scale = ACD.DB._getScale(id);

                            if (rot != 0) pt.Content = "$R~" + rot + "$" + pt.Content;
                            if (!(scale.X == 1 && scale.Y == 1)) pt.Content = "$S~" + scale.X + "_" + scale.Y + "$" + pt.Content;

                            pt.Content = pt.Content.Replace("$$", "$");

                            if (xnotes != null)
                                pt.Content += "$" + _convertXNote(xnotes);

                            if (z_depth != 0)
                                pt.Z = z_depth;

                            _setValue(content, pt);

                            if (!ACD.DB.GetXNotes(id)._props("Sub").ToBool() || ACD.DB.GetXNotes(id)._props("Show").ToBool())
                            {
                                //prefix = ACD.DB._getIdName(id).filter("_-").Last();

                                List<string> _xts = ACD.DB.GetXNotes(id).ToList();
                                _xts.Add("Prefix="+ ACD.DB._getIdName(id));
                                ACD.DB.BlockEntitiesAction(id, _ids => { DataToClipboard(_ids, _xts); });
                            }
                        }
                        else if (ACD.DB._isCircle(id))
                        {
                            pPos pt = ACD.DB._getPoint(id) - basept;
                            pt.Content = "R~" + ACD.DB._getRadius(id) * drawing_scale;

                            if (current_xnotes != null)
                                pt.Content += "$" + _convertXNote(current_xnotes);

                            if (z_depth != 0)
                                pt.Z = z_depth;

                            _setValue(content, pt);
                                                        
                            html_content += "<div style=\"border-radius: 50%;position:absolute;"
                                + "left:" + pt.X + "px;"
                                + "top:" + (h - pt.Y) + "px;"
                                 + "width:" + ACD.DB._getRadius(id) + "px;"
                                 + "height:" + ACD.DB._getRadius(id) + "px;" 
                                 + "border: 0.5px solid black;\"></div>";
                        }
                        else if (ACD.DB._isElip(id))
                        {
                            pPos pt = ACD.DB._getPoint(id) - basept;
                            pt.Content = ACD.DB._getIdInfo(id).Replace("|", "$").Replace("=", "~");

                            double[] ls = getElipInfo(ACD.DB, id);

                            if (current_xnotes != null)
                                pt.Content += "$" + _convertXNote(current_xnotes);

                            if (z_depth != 0)
                                pt.Z = z_depth;

                            _setValue(content, ACD.DB._getPoint(id) - basept);

                            html_content += "<div style=\"border-radius: 50%;position:absolute;"
                                + "left:" + pt.X + "px;"
                                + "top:" + (h - pt.Y) + "px;"
                                 + "width:" + ls[0] + "px;"
                                 + "height:" + ls[1] + "px;"
                                 + "transform: rotate(" + ls[2] + "deg);"
                                 + "border: 0.5px solid black;\"></div>";
                        }
                        else if (ACD.DB._isText(id))
                        {
                            pPos pt = ACD.DB._getPoint(id) - basept;
                            pt.Content = "TXT~" + ACD.DB._getContent(id);

                            if (current_xnotes != null)
                                pt.Content += "$" + _convertXNote(current_xnotes);

                            if (z_depth != 0)
                                pt.Z = z_depth;

                            _setValue(content, ACD.DB._getPoint(id) - basept);

                            html_content += "<div style=\"position:absolute;"
                                + "left:" + pt.X + "px;"
                                + "top:" + (h - pt.Y) + "px;"
                                + "\">"
                                + ACD.DB._getContent(id)
                                + "</div>";
                        }//dicts[content] += ACD.DB._getContent(id) + "(" + (ACD.DB._getPoint(id) - basept) + ")|";

                        html_content += "\r\n";
                    }
                }
        }

        static int splitarc = 16;
        static pPos basept = null;

        static void _clearPosContent(pPos p, params string[] key_content_keeps)
        {
            if (!p.Content.empty())
            {
                string s = p.Content.Replace("~", "=");
                p.Content = null;

                foreach (string k in key_content_keeps)
                    if (!s._prop(k).empty())
                        p.Content = k + "~" + s._prop(k);
            }
        }

        static void EventToClipboard(object o = null, EventArgs e = null)
        {
            dicts = dicts.OrderBy(itm => itm.Key)
                        .ToDictionary(keXItem => keXItem.Key, valueItem => valueItem.Value);

            string res = "";
            
            if (!xname.empty() && xzone != null)
                res += "#XZONE=" + xname + "|Verts=" + xzone.ToText(false) + "\r\n";

            string contentPoints = "";
            string ext_st = "XYS=[";

            foreach (var itm in dicts)
            {
                string st = "";

                if (!itm.Key.st_("#ELEMENT"))
                {
                    if (itm.Key._firstPropName() != "#INS")
                    {
                        PosCollection pls = new PosCollection(itm.Value);
                        //string[] str = pls.AllPoints.Where(p => !p.Content.empty())
                        //    .SelectMany(p => p.Content.Replace("~", "=").Replace("$", "|").Upper().filter("|"))
                        //    .Distinct().OrderBy(s => s).ToArray();

                        //ACD.WR("Result {0}", str.ToTextStr("|"));


                        //foreach (pPos p in pls.AllPoints)
                        //    _clearPosContent(p, "R", "W", "H"); //giữ lại phần content của pPos CIR và ELIP
                        contentPoints += pls.AllPoints.Where(p => !p.Content.empty())
                            .Select(p => new pPos((p.X * drawing_scale).roundNumber(0.01), 
                                    (p.Y * drawing_scale).roundNumber(0.01),0,p.Content).ToString()).ToTextStr(";");


                        st = "";
                        // st = pls.Select(ls => ls.Select(p
                        //=> p * drawing_scale).ToArray()).ToCollectionSameClosed(false) + "\r\n";

                        foreach (pPos[] ls in pls)
                            if(ls.Length > 1)
                        {
                            st += "[";
                            var vls = pt_start_point == null || ls[0]._isVeryClosed(pt_start_point) ? ls : ls.Reverse();

                            foreach (pPos p in pt_start_point != null && !ls[0]._isVeryClosed(pt_start_point) ? ls : ls.Reverse())
                                st += "[" + (p.X * drawing_scale).roundNumber(0.01) + "," + (p.Y * drawing_scale).roundNumber(0.01) + "],";

                            st += "],\n";

                            for (int ax = 0; ax < 2; ax++)
                            {
                                int nex = (ax + 1) % 2;
                                if (vls.All(p => Math.Abs(p[ax] - vls[0][ax]) < 0.2))
                                    ext_st += "[" + string.Join(",", vls.Select(p => p[nex])) + "],";
                            }
                            

                        }

                        if (!st.et_())
                            st += "\n]";
                    }
                }
                else if(!itm.Value.et_() && !itm.Value._firstProp().et_())
                {
                    st = itm.Key + "|" + itm.Value + "\r\n";
                    st = st.Replace("|Verts=", "\r\nVerts=")
                        .Replace("|Faces=", "\r\nFaces=");
                }

                if (!st.et_())
                    res += "LINES =[\n" + st.Replace("||", "|").Replace(",;", ";") + "\n";
            }

            res += "\nTEXT='" + contentPoints + "'";
            System.Windows.Forms.Clipboard.SetText(res);
            ext_st += "]";

            string prefix__ = "NAME = '" + objName + "'\nMTL = 'MI_Concrete_Ground_Tiles_A_Dark'\nH=2\n";
            if (objName.st_("BP_"))
                prefix__ += "BP='" + objName + "'\n";

            File.WriteAllText(@"D:\lines.py", prefix__ + res + ext_st);
        }

        static string xname = "", objName = null;
        static pPos[] xzone = null;
        static double drawing_scale = 1;
        
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                //ACD.WR("OK1");
                Database db = ACD.DB;
                ObjectIdCollection ids = ACD.GetSelection();

                if (ids.Count > 0)
                {
                    var blockId = ids.ToList().FirstOrDefault(id => ACD.DB._isBlock(id));

                    if (!blockId.IsNull)
                    {
                        basept = ACD.DB._getPoint(blockId);
                        objName = basept.Content;
                    }

                    objName = "Sample";

                    if (objName.et_())
                        objName = ACD.ED.GetInputString("Enter name", "Spline_01");
                    //ACD.WR("OK2");
                    initHtml(ids);
                    
                    drawing_scale = ACD.AllSelectionXNotes._props("drawing.scale").ToNumber(drawing_scale);
                    

                    if(basept == null)
                        basept = ACD.GetPoint();

                    xname = ACD.AllSelectionXNotes._props("XNAME");

                    if (xzone != null)
                    {
                        ObjectId lwpId = ACD.DB.DrawPolyline(xzone.Rect(), true, "LAYER=Defpoints");
                        ACD.DB.SetXNotes(lwpId, "Type=Erased");
                        ACD.DB._setIdInfo(lwpId, "LTYPE=HIDDEN|LCOLOR=4|LWEIGHT=50");
                    }

                    int default_arc_segments = (int)ACD.AllSelectionXNotes._props("drawing.segments").ToNumber(16);
                    //ACD.WR("OK4");
                    if (basept == null) basept = new pPos(0, 0);
                    //ACD.WR("OK4.3");
                    dicts = new Dictionary<string, string>();
                    //ACD.WR("OK4.2");
                    DataToClipboard(ids);
                    //ACD.WR("OK5");
                    ObjectIdCollection hatchIds = ids.ToList()
                        .Where(id => ACD.DB._isHatch(id)).ToCollection();

                    EventToClipboard();

                    File.WriteAllText(@"D:\html\htmllog.html", html_content + "</svg>\r\n");
                }
            }

            ACD.Focus();
        }
    }
}

