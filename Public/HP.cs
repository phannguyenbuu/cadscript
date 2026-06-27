using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Autodesk.AutoCAD.DatabaseServices;



namespace AcadScript
{
    public static class HPsExtension
    {
        public static ObjectIdCollection HatchByParams(this Database db,
            IEnumerable<pPos> _src, IEnumerable<string> prams, string extend_pram = null)
        {
            ObjectIdCollection all_res = new ObjectIdCollection();
            pPos[] src = _src.ToArray();

            bool show_area = prams._props("SHOW_AREA").ToBool();
            //bool isRect = false;
            pPos move_pt = new pPos(0, 0);

            if (!extend_pram._prop("MOVE").empty())
                move_pt = pPos.FromString(extend_pram.ReplaceEquation()._prop("MOVE"));

            for (int line = 0; line < prams.Count(); line++)
                if (prams.ElementAt(line).Trim().StartsWith("#"))
                {
                    ObjectIdCollection res = new ObjectIdCollection();
                    string pram = prams.ElementAt(line);

                    pram += src.ToInfo();
                    pram += "|D=" + DE.DEF_DEF_HEIGHT.ToString();

                    string[] pram_propnames = pram._allPropNames();
                    if (!extend_pram.empty())
                        foreach (string k in extend_pram._allPropNames())
                            if (k.Upper() != "MOVE" && !pram_propnames.Contains(k))
                                pram += "|" + k + "=" + extend_pram._prop(k);

                    pram = pram.ReplaceEquation();
                    //ACD.WR("CMD3 {0}", pram);
                    string key = pram._firstPropName();
                    string val = pram._firstProp();

                    bool isVerts = !pram._prop("Verts").empty();

                    PosCollection pls = new PosCollection();

                    if (!pram._prop("VERTS").empty())
                        pls = new PosCollection(pram._prop("VERTS"));

                    if (!pram._prop("MOVE").empty())
                        move_pt = pPos.FromString(pram._prop("MOVE"));

                    //ACD.WR("CMD4 {0}", pram);
                    PosCollection new_src = new PosCollection();

                    switch (key)
                    {
                        case "#MOVE":
                            move_pt = pPos.FromString(pram._firstProp());
                            break;
                        case "#HSTYLE":
                            res = _hSTYLEAction(db, src, pram);
                            break;
                        case "#LW":
                            if (!pram._prop("LSTYLE").empty())
                            {
                                foreach (pPos[] ls in pls)
                                    res.AddRange(db.DrawStylePolyline(pram._prop("LSTYLE"),
                                        ls.Move(move_pt), pram.Replace(key, "LAYER")));
                            }
                            else
                            {
                                new_src = isVerts ? pls : src.ToCollection();

                                res = db.DrawPolyline(new_src.Select(ls => ls.Move(move_pt).ModifyPts(pram)).ToCollectionSameClosed(), pram);
                                //!isVerts || pram._prop("LSTYLE").Upper() == "RECT" ? new bool[] { true }
                                //: ACD.FromString_Closed, pram.Replace(key, "LAYER"));
                            }
                            break;
                        case "#HATCH":
                            new_src = isVerts ? pls : src.ToCollection();
                            res = db.DrawHatchs(new_src.Select(ls
                                => ls.Move(move_pt).ModifyPts(pram)).ToCollectionSameClosed(), pram.Replace(key, "HPATTERN"));
                            break;

                        case "#INS":
                            if (pls.Count > 0)
                            {
                                foreach (pPos[] ls in pls)
                                    res.AddRange(db.InsertBlock(val, ls.Move(move_pt), pram));

                                if (val.StartsWith("H"))
                                    db._setLayer(res, DE.DEF_LAYER_HATCH);

                                if (!pram._prop("ROTATION").empty())
                                    db._setRotations(res, pram._prop("ROTATION").ToNumber());

                                if (res.Count > 0 && !pram._prop("STRETCH").empty())
                                    res = _stretchBXParam(db, res, pram);
                            }
                            break;

                        case "#CC":
                            if (isVerts)
                                foreach (pPos[] ls in pls)
                                    foreach (pPos pt in ls)
                                    {
                                        res.AddRange(db.DrawCircle(pt + move_pt, pram._prop("RADIUS").ToNumber(20), pram.Replace(key, "LAYER")));
                                    }
                            else
                            {
                                foreach (pPos pt in src.ModifyPts(pram))
                                    res.AddRange(db.DrawCircle(pt + move_pt, pram._prop("RADIUS").ToNumber(20), pram.Replace(key, "LAYER")));
                            }
                            break;

                        case "#CLONE":
                            foreach (pPos[] ls in pls)
                            {
                                db.GetEntities(ls, EN_SELECT.AC_ALL);
                                res.AddRange(db.CloneObjects(IR.SelectedIds));
                            }

                            if (!db.HasLayout(pram._firstProp()).IsNull)
                                db._setLayer(res, pram._firstProp());

                            db.MoveObject(res, move_pt);
                            break;
                        case "#STRETCH":
                            pPos mv = pPos.FromString(pram._firstProp());

                            if (!mv.IsNull && !(mv.X == 0 && mv.X == 0))
                            {
                                foreach (pPos[] ls in pls)
                                {
                                    db.GetEntities(ls, EN_SELECT.AC_ALL);
                                    db.Stretchs(IR.SelectedIds, ls, mv);
                                }
                            }
                            break;

                        case "#DEL":
                            db.GetEntities(src, EN_SELECT.AC_ALL);
                            ObjectIdCollection ids = new ObjectIdCollection();

                            foreach (pPos[] ls in pls)
                                ids.AddRange(db.FilterIds(IR.SelectedIds, pram._firstProp().filter(",;"))
                                .Cast<ObjectId>().Where(id => db._getBound(id).All(p => p.Inside(ls.Rect()))).ToCollection());

                            db.EraseObjects(ids);
                            break;
                        default:
                            if (pram.st_("#TEXT") || pram.st_("#NOTE"))
                            {
                                var dict = pram._propContent();
                                string[] ar = dict.Key.Replace(";;", "\r\n").filter("\r\n");
                                double sz = dict.Value;

                                foreach (string s1 in ar)
                                {
                                    pPos p = pPos.FromString(s1.filter("()").First()) + move_pt;
                                    res.Add(db.CreateText(s1._getInComma(), p, sz, 0, GP.DEF_LAYER_TEXT));

                                    //if (pram.st_("#NOTE"))
                                    //{
                                    //    res.Add(db.DrawEllipse(p, DE.INI_Value("NOTE_ELLIPSE_WIDTH").ToNumber(),
                                    //        DE.INI_Value("NOTE_ELLIPSE_HEIGHT").ToNumber(), "G-Text"));
                                    //}
                                }
                            }
                            break;
                    }

                    res = res.Cast<ObjectId>().Where(id => db.ValidId(id)).Select(id => id).ToCollection();
                    //ARRAX=0,300;0,H-800
                    if (res.Count > 0)
                        res = _afterHatchEffect(db, res, pram);

                    all_res.AddRange(res);

                    if (show_area)
                        db.CreateText((src.Area() / 1000000).roundNumber(0.01) + "m2", src.GetCentroidInside(), 500);
                }

            ACD.DB.GetEntities(null, EN_SELECT.AC_DXF_AND_HYPERLINK, "LWPOLYLINE", "$removal");
            ACD.DB.EraseObjects(IR.SelectedIds);

            return all_res;
        }

        static ObjectIdCollection _hSTYLEAction(Database db, IEnumerable<pPos> src, string pram)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            string[] ref_prams = HP.LoadHatchSample(pram.StartsWith("#HSTYLE") ? pram._firstProp() : pram._prop("HSTYLE"));

            if (ref_prams != null && ref_prams.Length > 0)
            {
                string comma = pram._allVariableAndValues();

                //if (!pram._prop("BX").empty())
                //    comma += "|BX=" + pram._prop("BX");

                if (!comma.empty())
                    foreach (string cmd in comma._extractListParam())
                    {
                        //ACD.WR("CMD {0}", cmd);
                        res = db.HatchByParams(src, ref_prams, cmd);
                    }
            }
            return res;
        }

        static ObjectIdCollection _stretchBXParam(Database db, ObjectIdCollection ids, string pram)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            //bool isRect = false;
            PosCollection pls = new PosCollection(pram._prop("STRETCH").filter("()").First());
            pPos v = pPos.FromString(pram._prop("STRETCH").filter("()").Last());

            if (pls.Count > 0 && (v.X.roundNumber() != 0 || v.X.roundNumber() != 0))
            {
                pPos[] r = pls.First.Rect();

                foreach (ObjectId blockId in ids)
                {
                    pPos blockpt = db._getPoint(blockId);

                    pram += "|X=" + blockpt.X.roundNumber();
                    pram += "|X=" + blockpt.X.roundNumber();
                    pram = pram.ReplaceEquation();

                    ObjectIdCollection tmp_ids = db.ExplodeEntity(blockId);
                    db.EraseObject(blockId);

                    db.DrawPolyline(r.Move(blockpt), true, "LAYER=" + DE.DEFPOINTS);
                    db.Stretchs(tmp_ids, r.Move(blockpt), v);

                    res.AddRange(tmp_ids);
                }
            }

            return res;
        }

        static ObjectIdCollection _afterHatchEffect(Database db, ObjectIdCollection ids, string st)
        {
            ObjectIdCollection res = new ObjectIdCollection();

            //ACD.WR("{1} HAS_DELETE {0}", !st._prop("DELETE").empty(), st);
            if (!st._prop("DELETE").empty())
            {
                PosCollection regions = new PosCollection(st._prop("DELETE"));
                ObjectIdCollection delIds = new ObjectIdCollection();
                foreach (pPos[] reg in regions)
                {
                    pPos[] r = reg.Rect();
                    //db.DrawPolyline(r, true, "LAYER=DEFPOINTS");
                    delIds.AddRange(ids.Cast<ObjectId>().Where(id => (db._isVertice(id)
                        && db._getVertices(id).All(p => p.Inside(r)))
                        || (db._isPoint(id) && db._getPoint(id).Inside(r))).ToCollection());
                }

                //ACD.WR("DEL {0}", delIds.Count);
                db.EraseObjects(delIds);

                foreach (ObjectId id in delIds)
                    ids.Remove(id);
            }

            res.AddRange(ids);
            if (!st._prop("ROTATION").empty() && st._prop("ROTATION").filter("()").Length > 1)
            {

                double ang = st._prop("ROTATION").filter("()").Last().ToNumber();
                pPos p = pPos.FromString(st._prop("ROTATION").filter("()").First());

                //ACD.WR("Rotate center {0}, {1}", st._prop("ROTATION"), p);


                db.Rotate(res, ang, p);
            }

            if (!st._prop("HPATTERN").empty())
            {
                res.AddRange(db.DrawHatchFromIds(res, st));
            }

            if (!st._prop("HSTYLE").empty())
            {
                string[] sub_prams = HP.LoadHatchSample(st._prop("HSTYLE"));

                ObjectIdCollection new_ids = new ObjectIdCollection();

                foreach (ObjectId id in res)
                    if (db._isPolylineClosed(id))
                        new_ids.AddRange(_hSTYLEAction(db, db._getVertices(id), st));
                //new_ids.AddRange(db.HatchByParams(db._getVertices(id), sub_prams, st._prop("HSTYLE")._getInComma()));

                if (!st._prop("ERASE").empty())
                {
                    db.EraseObjects(res);
                    res = new ObjectIdCollection();
                }

                res.AddRange(new_ids);
            }

            if (!st._prop("ARRAX").empty())
            {
                PosCollection pls = new PosCollection(st._prop("ARRAX"));

                if (pls.Count > 0 && pls.First.Length > 0)
                {
                    pPos incr = pls.First[0];
                    pPos lim_ar = pls.First[1];

                    //ACD.WR("ARRAX {0};{1} IDS {2}", incr, lim_ar, res.Count);

                    int n_x = incr.X == 0 ? 1 : (int)Math.Ceiling(lim_ar.X / incr.X);
                    int n_X = incr.X == 0 ? 1 : (int)Math.Ceiling(lim_ar.X / incr.X);

                    for (int i = 0; i < n_x; i++)
                        for (int j = 0; j < n_X; j++)
                            if (i != 0 || j != 0)
                            {
                                //ACD.WR("ARRAX_ITM {0},{1}", x, X);
                                ObjectIdCollection new_res = ACD.DB.CloneObjects(ids);
                                db.MoveObject(new_res, new pPos(i * incr.X, j * incr.X));
                                res.AddRange(new_res);
                            }
                }
            }

            if (!st._prop("MIRROR").empty())
            {
                PosCollection pls = new PosCollection(st._prop("MIRROR"));

                foreach (pPos[] ls in pls)
                    if (ls.Length > 1)
                    {
                        ObjectIdCollection cloneIds = db.CloneObjects(res);
                        db.MirrorObjects(cloneIds, ls.First(), ls.Last());
                        res.AddRange(cloneIds);
                    }
            }

            return res;
        }
    }


    public static class HP
    {
        public static Dictionary<string, string[]> chapters;
        public static string[] cadlib_blocknames;
        public static bool isload;

        static string[] _fileInCodes
        {
            get
            {
                return Directory.GetFiles(DE.CADLIB_CONSTRUCT + "Code", "*.cs");
            }
        }

        public static void InitHatch()
        {
            ACD.WR("AE00");
            chapters = File.ReadAllLines(DE.CADLIB_CONSTRUCT + @"\hatch.txt").GetChapters();
            //ACD.WR("AE01");
            ACD.cbCategory.Items.Clear();
            ACD.cbCategory.Items.Add("#Arrow");
            ACD.cbCategory.Items.Add("#Break");
            ACD.cbCategory.Items.Add("#Cross");
            ACD.cbCategory.Items.Add("#Block");
            //ACD.WR("AE02");
            List<string> itms = HP.chapters.Keys.Select(s => s.filter(" ").First()).ToList();
            itms.AddRange(_fileInCodes.Select(f => Path.GetFileNameWithoutExtension(f)
                .filter("[]").First()).Distinct().ToArray());
            itms = itms.Distinct().OrderBy(s => s).ToList();

            //ACD.WR("AE03");

            ACD.cbCategory.Items.AddRange(itms.ToArray());
            //ACD.WR("AE04");
            cadlib_blocknames = IR.ListBlockInFile(DE.CAD_TEMPLATE_FILE)
                .Where(s => !s.StartsWith("*") && !s.StartsWith("A$") && !s.StartsWith("_"))
                .OrderBy(s => s.Upper()).ToArray();

            if (!isload)
            {
                isload = true;

                ACD.cbCategory.SelectedIndexChanged += (o, e) =>
                {
                    //ACD.WR("OK1");
                    if (ACD.cbCategory.SelectedItem == null) return;
                    string key = ACD.cbCategory.SelectedItem.ToString();

                    if (key.StartsWith("#"))
                    {
                        if (key == "#Arrow")
                        {
                            ACD.Get2Points();

                            if (ACD.FirstPoint != null && ACD.LastPoint != null)
                            {
                                double n = ACD.ED.GetInputString("Input size {0}", "100").ToNumber(100);
                                ACD.DB.HatchKey(new pPos[] { ACD.FirstPoint, ACD.LastPoint }.ToCollection(), key, "Size=" + n);
                            }
                        }
                        else if (key == "#Break")
                        {
                            ACD.Get2Points();

                            if (ACD.FirstPoint != null && ACD.LastPoint != null)
                            {
                                //double n = ACD.ED.GetInputString("Input size {0}", "100").ToNumber(100);
                                ACD.DB.HatchKey(new pPos[] { ACD.FirstPoint, ACD.LastPoint }.ToCollection(), key);
                            }
                        }
                        else if (key == "#Cross")
                        {
                            ObjectIdCollection selIds = ACD.GetSelection();
                            PosCollection pls = null;

                            if (selIds.Count > 0)
                                pls = selIds.ToList().Select(id => ACD.DB._getVertices(id)).ToCollectionSameClosed();
                            else
                            {
                                ACD.Get2Points();
                                if (ACD.FirstPoint != null && ACD.LastPoint != null)
                                    pls = new pPos[] { ACD.FirstPoint, ACD.LastPoint }.ToCollection();
                            }

                            //ACD.WR("Pls {0}", pls.Count);

                            //if (pls != null)
                                foreach (pPos[] ls in pls)
                                {
                                    pPos[] bb = ls.Boundary();
                                    ACD.DB.Draw2D(bb[0], bb[1]);
                                    ACD.DB.Draw2D(new pPos(bb[0].X, bb[1].Y), new pPos(bb[1].X, bb[0].X));
                                }

                        }
                        else if (key == "#Block")
                        {
                            ACD.cbHatch.Items.Clear();
                            ACD.cbHatch.Items.AddRange(cadlib_blocknames);
                        }
                    }
                    else
                    {
                        ACD.cbHatch.Items.Clear();

                        string[] files = _fileInCodes;
                        string[] txts = files.Where(f => Path.GetFileNameWithoutExtension(f).st_(key.Upper())).ToArray();

                        //ACD.WR("txt {0}", txts.Length);

                        ACD.cbHatch.Items.AddRange((txts.Length > 0 ? txts.Select(f => Path.GetFileName(f))
                            : HP.chapters.Keys.Where(s => s.StartsWith(key))).OrderBy(s => s).ToArray());
                    }
                };

                ACD.cbHatch.SelectedIndexChanged += (o, e) =>
                {
                    if (ACD.cbCategory.SelectedItem == null || ACD.cbHatch.SelectedItem == null) return;
                    using (ACD.Lock())
                    {
                        string category = ACD.cbCategory.SelectedItem.ToString();
                        string key = ACD.cbHatch.SelectedItem.ToString();

                        ObjectIdCollection selIds = ACD.GetSelection();

                        if (category == "#Block")
                        {
                            pPos pt = ACD.GetPoint();

                            if (pt != null)
                            {
                                if (ACD.DB.HasBlock(key).IsNull)
                                    ACD.DB.Insert(DE.CAD_TEMPLATE_FILE, key, new pPos[] { pt });
                                else
                                    ACD.DB.Insert(key, new pPos[] { pt });
                            }
                        }
                        //else if (key.EndsWith(".cs"))
                        //{
                        //    string content = File.ReadAllText(key);
                        //    string[] param_keys = _findParamKey(content);
                        //    ACD.WR("Params: {0}", param_keys.ToTextStr());
                        //
                        //    pPos insert_point = new pPos(0, 0);
                        //
                        //    if (content.Contains("[BASEPOINT]"))
                        //        insert_point = ACD.GetPoint();
                        //
                        //    if (insert_point != null)
                        //    {
                        //        //ACD.WR("P {0},{1}", insert_point.X, insert_point.Y);
                        //        foreach (string pk in param_keys)
                        //        //if(!pk.Contains("[BASEPOINT]"))
                        //        {
                        //            content = content.Replace(pk, ACD.ED.GetInputString("Input value for " + pk, "0"));
                        //        }
                        //
                        //        content = content.Replace("[BASEPOINT]", insert_point.Round(50).ToString());
                        //
                        //        File.WriteAllText(@"D:\cadlog.txt", content);
                        //
                        //        ASRun.CompileAndRun(content);
                        //    }
                        //}
                        else
                        {
                            string[] prams = ACD.LoadChapter(DE.CADLIB_CONSTRUCT + @"\hatch.txt", key);

                            if (prams != null)
                            {
                                int splitarc = (int)pString.INI_String("SPLITARC").ToNumber();

                                if (selIds.Count > 0)
                                {
                                    PosCollection pls = new PosCollection();

                                    foreach (ObjectId objId in selIds)
                                        if (ACD.DB._isBlock(objId))
                                        {
                                            PosCollection _pls = new PosCollection();

                                            _pls.Name += ACD.DB._getIdName(objId) + ";";
                                            ACD.DB.BlockEntitiesAction(objId, _ids =>
                                            {
                                                _pls.AddRange(_ids.ToList().Where(id => ACD.DB._isPolyline(id))
                                                    .Select(id
                                                    => ACD.DB._getVertices(id, splitarc, true, true)).ToCollectionSameClosed());

                                                foreach (ObjectId _id in _ids)
                                                    _pls.Closed = _pls.Closed.Add(ACD.DB._isPolylineClosed(_id));
                                            });

                                            ACD.DB.HatchKey(_pls, key);
                                        }
                                        else if (ACD.DB._isPolyline(objId) || ACD.DB._isHatch(objId))
                                        {
                                            pls.Name += "LWP;";
                                            pls.Add(ACD.DB._getVertices(objId));
                                            pls.Closed = pls.Closed.Add(ACD.DB._isHatch(objId) || ACD.DB._isPolylineClosed(objId));
                                        }

                                    //GraphConsole.Compute(pls);

                                    //string layer = ACD.DB._getLayer(selIds.First());
                                    //double w = ACD.DB._getLineworkWidth(selIds.First());

                                    //ACD.WR("PLS {0}", GraphConsole.ResultPts.Count);

                                    //if (key.st_("TILE"))
                                    //{
                                    //    string[] blocknames = ACD.DB.ListBlock().Where(s => s.Upper().Contains("BASEPOINT")).ToArray();
                                    //    if (blocknames.Length > 0)
                                    //    {
                                    //        ACD.WR("Select basepoint... {0}", blocknames.First());
                                    //        pPos pt = ACD.GetPoint();
                                    //        ACD.DB.Insert(blocknames.First(), pt);
                                    //    }
                                    //}

                                    ACD.DB.HatchKey(pls, key);

                                    //string src = prams._props("SOURCE");
                                    //if (!src.empty())
                                    //{
                                    //    if (src.Upper() == "DELETE" || src.Upper() == "ERASE")
                                    //        ACD.DB.EraseObjects(selIds);
                                    //    else
                                    //        ACD.DB._setLayer(selIds, src);
                                    //}
                                }
                            }
                        }

                        ACD.Focus();
                    }
                };
            }
        }

        //static string[] _findParamKey(string content)
        //{
        //    List<string> res = new List<string>();
        //    string not_key = ";,?";
        //    string[] lines = content.filter("\r\n");

        //    foreach (string line in lines)
        //        if (line.Contains("[") && line.Contains("]"))
        //        {
        //            for (int a = 0; a < line.Length; a++)
        //                if (line.Substring(a).StartsWith("["))
        //                {
        //                    int b = line.IndexOf("]", a);

        //                    if (b != -1)
        //                    {
        //                        string k = line.Substring(a, b - a + 1);
        //                        if (!not_key.Any(c => k.Contains(c)))
        //                            res.Add(k);
        //                    }
        //                }
        //        }

        //    return res.ToArray();// .Where(s => _validParam(s)).Distinct().OrderBy(s => s).ToArray();
        //}

        //static bool _validParam(string s)
        //{
        //    string s_number = "0123456789";
        //    bool res = false;
        //    if(s != "[]" && !s_number.Contains(s[1]) && s != "[BASEPOINT]")
        //        res = true;
        //    return res;
        //}

        public static ObjectIdCollection HatchKey(this Database db, PosCollection pls, string key, string info = null)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            ACD.WR("Chapters {0} key {1}", chapters.Count, key);

            using (ACD.Lock())
            {
                if (key.Upper().Contains("ARROW"))
                {
                    for (int i = 0; i < pls.Count; i++)
                        if (pls[i].Length > 1)
                            res.Add(ACD.DB.DrawArrow(pls[i][0], pls[i][1], info._prop("Size").ToNumber(100)));
                }
                else if (key.Upper().Contains("BREAK"))
                {
                    for (int i = 0; i < pls.Count; i++)
                        if (pls[i].Length > 1)
                            res.Add(ACD.DB.DrawBreakLine(pls[i][0], pls[i][1], pls[i][0].DistanceTo(pls[i][1]) / 10));
                }
                else if (key.Upper().Contains("CROSS"))
                {
                    for (int i = 0; i < pls.Count; i++)
                        if (pls[i].Length > 1)
                        {
                            pPos[] bb = pls[i].Boundary();
                            res.Add(ACD.DB.Draw2D(bb[0].X, bb[1].Y, "LAYER=A-Hidden"));
                            res.Add(ACD.DB.Draw2D(bb[1].X, bb[0].Y, "LAYER=A-Hidden"));
                        }
                }
                else
                {
                    pls = pls.OrderBy(ls => ls.Boundary()[0].X).ToCollectionSameClosed();
                    string[] prams = null;

                    chapters = File.ReadAllLines(DE.CADLIB_CONSTRUCT + @"\hatch.txt").GetChapters();
                    ACD.WR("Chapters {0}", chapters.Count);
                    
                    if (chapters.Count > 0)
                    {
                        int index = chapters.Keys.ToList().FindIndex(k => k.Upper() == key.Upper());
                        ACD.WR("Dictionary Index {0}", index);

                        if (index != -1)
                        {
                            prams = chapters.Values.ElementAt(index);
                            ACD.WR("H04 {0}:{1}", index, prams.ToTextStr(";"));

                            if (prams != null)
                            {
                                bool found = false;

                                foreach (string pram in prams)
                                    if (pram.st_("#SCRIPT="))
                                    {
                                        found = true;
                                        string filename = ACD.FindFullname(pram._firstProp());

                                        if (File.Exists(filename))
                                        {
                                            string content = File.ReadAllText(filename);
                                            content = content.Replace("<REGION/>", pls.ToString());
                                            //content = content.Replace("<RECT/>", pls.Boundary.ToText());

                                            foreach (string s in prams)
                                                if (!s.empty() && s.Contains("="))
                                                    content = content.Replace("<" + s._firstPropName() + "/>", s._firstProp());

                                            //ACD.WR("<Content>{0}", content);
                                            File.WriteAllText(@"D:\tmp.cs", content);
                                            
                                            ASRun.CompileAndRun(content);
                                        }
                                        break;
                                    }

                                    if (!found)
                                    for (int j = 0; j < pls.Count; j++)
                                        res.AddRange(db.HatchParams(pls[j], prams));
                            }
                        }
                    }
                }
            }

            return res;
        }

         public static ObjectIdCollection HatchParams(this Database db,
            IEnumerable<pPos> _src, IEnumerable<string> prams, string extend_pram = null)
        {
            ObjectIdCollection all_res = new ObjectIdCollection();
            pPos[] src = _src.ToArray();

            bool show_area = prams._props("SHOW_AREA").ToBool();
            ACD.WR("Hatch Params {0}", show_area);
            pPos move_pt = new pPos(0, 0);

            if (!extend_pram._prop("MOVE").empty())
                move_pt = pPos.FromString(extend_pram.ReplaceEquation()._prop("MOVE"));

            for (int line = 0; line < prams.Count(); line++)
                if (prams.ElementAt(line).Trim().StartsWith("#"))
                {
                    ObjectIdCollection res = new ObjectIdCollection();
                    string pram = prams.ElementAt(line);

                    pram += src.ToInfo();
                    pram += "|D=" + DE.DEF_DEF_HEIGHT;

                    string[] pram_propnames = pram._allPropNames();
                    if (!extend_pram.empty())
                        foreach (string k in extend_pram._allPropNames())
                            if (k.Upper() != "MOVE" && !pram_propnames.Contains(k))
                                pram += "|" + k + "=" + extend_pram._prop(k);

                    pram = pram.ReplaceEquation();
                    ACD.WR("Hatch Params  {0}", pram);

                    string key = pram._firstPropName();
                    string val = pram._firstProp();

                    bool isVerts = !pram._prop("Verts").empty();

                    PosCollection pls = new PosCollection();

                    if (!pram._prop("VERTS").empty())
                        pls = new PosCollection(pram._prop("VERTS"));

                    if (!pram._prop("MOVE").empty())
                        move_pt = pPos.FromString(pram._prop("MOVE"));

                    ACD.WR("Hatch Params {0}", pram);
                    PosCollection new_src = new PosCollection();

                    switch (key)
                    {
                        case "#SCRIPT":
                            //ACD.WR("[File]{0}", val);
                            if (File.Exists(val))
                            {
                                string content = File.ReadAllText(val);
                                content = content.Replace("<REGION/>", src.ToText());
                                content = content.Replace("<RECT/>", src.Boundary().ToText());

                                //ACD.WR("OK1");

                                foreach (string s in prams)
                                    if(!s.empty() && s.Contains("="))
                                        content = content.Replace("<" + s._firstPropName() + "/>", s._firstProp());

                                //ACD.WR("<Content>{0}", content);
                                ASRun.CompileAndRun(content);
                            }
                            break;
                        case "#MOVE":
                            move_pt = pPos.FromString(pram._firstProp());
                            break;
                        case "#HSTYLE":
                            res = _hStyleAction(db, src, pram);
                            break;
                        case "#LW":
                            if (!pram._prop("LSTYLE").empty())
                            {
                                foreach (pPos[] ls in pls)
                                    res.AddRange(db.DrawStylePolyline(pram._prop("LSTYLE"),
                                        ls.Move(move_pt), pram.Replace(key, "LAYER")));
                            }
                            else
                            {
                                new_src = isVerts ? pls : src.ToCollection();
                                new_src = new_src.Select(ls => ls.Move(move_pt).ModifyPts(pram)).ToCollectionSameClosed();

                                for(int i = 0; i < new_src.Count; i++)
                                    res.Add(db.DrawPolyline(new_src[i],new_src.Closed[i], pram));
                                //!isVerts || pram._prop("LSTYLE").Upper() == "RECT" ? new bool[] { true }
                                //: ACD.FromString_Closed, pram.Replace(key, "LAYER"));
                            }
                            break;
                        case "#HATCH":
                            new_src = isVerts ? pls : src.ToCollection();
                            new_src = new_src.Select(ls
                                => ls.Move(move_pt).ModifyPts(pram)).ToCollectionSameClosed();

                            foreach(pPos[] ls in new_src)
                                res.AddRange(db.DrawHatch(ls, pram.Replace(key, "HPATTERN")));
                            break;

                        case "#INS":
                            if (pls.Count > 0)
                            {
                                foreach (pPos[] ls in pls)
                                    res.AddRange(db.InsertBlock(val, ls.Move(move_pt), pram));

                                if (val.StartsWith("H"))
                                    db._setLayer(res, DE.DEF_LAYER_HATCH);

                                if (!pram._prop("ROTATION").empty())
                                    db._setRotations(res, pram._prop("ROTATION").ToNumber());

                                if (res.Count > 0 && !pram._prop("STRETCH").empty())
                                    res = _stretchByParam(db, res, pram);
                            }
                            break;

                        case "#CC":
                            if (isVerts)
                                foreach (pPos[] ls in pls)
                                    foreach (pPos pt in ls)
                                    {
                                        res.AddRange(db.DrawCircle(pt + move_pt, pram._prop("RADIUS").ToNumber(20), pram.Replace(key, "LAYER")));
                                    }
                            else
                            {
                                foreach (pPos pt in src.ModifyPts(pram))
                                    res.AddRange(db.DrawCircle(pt + move_pt, pram._prop("RADIUS").ToNumber(20), pram.Replace(key, "LAYER")));
                            }
                            break;

                        case "#CLONE":
                            foreach (pPos[] ls in pls)
                            {
                                db.GetEntities(ls, EN_SELECT.AC_ALL);
                                res.AddRange(db.CloneObjects(IR.SelectedIds));
                            }

                            if (!db.HasLayout(pram._firstProp()).IsNull)
                                db._setLayer(res, pram._firstProp());

                            db.MoveObject(res, move_pt);
                            break;
                        case "#STRETCH":
                            pPos mv = pPos.FromString(pram._firstProp());

                            if (!mv.IsNull && !(mv.X == 0 && mv.Y == 0))
                            {
                                foreach (pPos[] ls in pls)
                                {
                                    db.GetEntities(ls, EN_SELECT.AC_ALL);
                                    db.Stretchs(IR.SelectedIds, ls, mv);
                                }
                            }
                            break;

                        case "#DEL":
                            db.GetEntities(src, EN_SELECT.AC_ALL);
                            ObjectIdCollection ids = new ObjectIdCollection();

                            foreach (pPos[] ls in pls)
                                ids.AddRange(db.FilterIds(IR.SelectedIds, pram._firstProp().filter(",;"))
                                .Cast<ObjectId>().Where(id => db._getBound(id).All(p => p.Inside(ls.Rect()))).ToCollection());

                            db.EraseObjects(ids);
                            break;
                        default:
                            if (pram.st_("#TEXT") || pram.st_("#NOTE"))
                            {
                                var dict = pram._propContent();
                                string[] ar = dict.Key.Replace(";;", "\r\n").filter("\r\n");
                                double sz = dict.Value;

                                foreach (string s1 in ar)
                                {
                                    pPos p = pPos.FromString(s1.filter("()").First()) + move_pt;
                                    //res.Add(db.CreateText(s1._getInComma(), p, sz, 0, GP.DEF_LAYER_TEXT));

                                    //if (pram.st_("#NOTE"))
                                    //{
                                    //    res.Add(db.DrawEllipse(p, pDES.INI_Value("NOTE_ELLIPSE_WIDTH").ToNumber(),
                                    //        pDES.INI_Value("NOTE_ELLIPSE_HEIGHT").ToNumber(), "G-Text"));
                                    //}
                                }
                            }
                            break;
                    }

                    res = res.Cast<ObjectId>().Where(id => db.ValidId(id)).Select(id => id).ToCollection();
                    //ARRAY=0,300;0,H-800
                    if (res.Count > 0)
                        res = _afterHatchEffect(db, res, pram);

                    all_res.AddRange(res);

                    //if (show_area)
                        //db.CreateText((src.Area() / 1000000).roundNumber(0.01) + "m2", src.GetCentroidInside(), 500);
                }

            //db.GetEntities(null, EN_SELECT.AC_DXF_AND_HYPERLINK, "LWPOLYLINE", "$removal");
            //db.EraseObjects(IR.SelectedIds);

            return all_res;
        }

        public static string[] LoadHatchSample(string key)
        {
            string[] res = null;
            var chapters = File.ReadAllLines(DE.CADLIB_CONSTRUCT + @"\hatch.txt").GetChapters();

            if (chapters.Count > 0)
            {
                string chars = ";,|()[]{}";
                Dictionary<string, string> dict = new Dictionary<string, string>();
                foreach (char ch in chars)
                    dict.Add(ch.ToString(), "");

                key = key.Replace(dict);
                int index = chapters.Keys.ToList().FindIndex(k => k.Upper() == key.Upper());

                if (index != -1)
                    res = chapters.Values.ElementAt(index);
            }

            return res;
        }

        static ObjectIdCollection _hStyleAction(Database db, IEnumerable<pPos> src, string pram)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            string[] ref_prams = LoadHatchSample(pram.StartsWith("#HSTYLE") ? pram._firstProp() : pram._prop("HSTYLE"));

            if (ref_prams != null && ref_prams.Length > 0)
            {
                string comma = pram._allVariableAndValues();

                //if (!pram._prop("BY").empty())
                //    comma += "|BY=" + pram._prop("BY");

                if (!comma.empty())
                    foreach (string cmd in comma._extractListParam())
                    {
                        //ACD.WR("CMD {0}", cmd);
                        res = HatchParams(db, src, ref_prams, cmd);
                    }
            }
            return res;
        }

        static ObjectIdCollection _stretchByParam(Database db, ObjectIdCollection ids, string pram)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            //bool isRect = false;
            PosCollection pls = new PosCollection(pram._prop("STRETCH").filter("()").First());
            pPos v = pPos.FromString(pram._prop("STRETCH").filter("()").Last());

            if (pls.Count > 0 && (v.X.roundNumber() != 0 || v.Y.roundNumber() != 0))
            {
                pPos[] r = pls.First.Rect();

                foreach (ObjectId blockId in ids)
                {
                    pPos blockpt = db._getPoint(blockId);

                    pram += "|X=" + blockpt.X.roundNumber();
                    pram += "|Y=" + blockpt.Y.roundNumber();
                    pram = pram.ReplaceEquation();

                    ObjectIdCollection tmp_ids = db.ExplodeEntity(blockId);
                    db.EraseObject(blockId);

                    db.DrawPolyline(r.Move(blockpt), true, "LAYER=" + DE.DEFPOINTS);
                    db.Stretchs(tmp_ids, r.Move(blockpt), v);

                    res.AddRange(tmp_ids);
                }
            }

            return res;
        }

        static ObjectIdCollection _afterHatchEffect(Database db, ObjectIdCollection ids, string st)
        {
            ObjectIdCollection res = new ObjectIdCollection();

            //ACD.WR("{1} HAS_DELETE {0}", !st._prop("DELETE").empty(), st);
            if (!st._prop("DELETE").empty())
            {
                PosCollection regions = new PosCollection(st._prop("DELETE"));
                ObjectIdCollection delIds = new ObjectIdCollection();
                foreach (pPos[] reg in regions)
                {
                    pPos[] r = reg.Rect();
                    //db.DrawPolyline(r, true, "LAYER=DEFPOINTS");
                    delIds.AddRange(ids.Cast<ObjectId>().Where(id => (db._isVertice(id)
                        && db._getVertices(id).All(p => p.Inside(r)))
                        || (db._isPoint(id) && db._getPoint(id).Inside(r))).ToCollection());
                }

                //ACD.WR("DEL {0}", delIds.Count);
                db.EraseObjects(delIds);

                foreach (ObjectId id in delIds)
                    ids.Remove(id);
            }

            res.AddRange(ids);
            if (!st._prop("ROTATION").empty() && st._prop("ROTATION").filter("()").Length > 1)
            {

                double ang = st._prop("ROTATION").filter("()").Last().ToNumber();
                pPos p = pPos.FromString(st._prop("ROTATION").filter("()").First());

                //ACD.WR("Rotate center {0}, {1}", st._prop("ROTATION"), p);


                db.Rotate(res, ang, p);
            }

            if (!st._prop("HPATTERN").empty())
            {
                res.AddRange(db.DrawHatchFromIds(res, st));
            }

            if (!st._prop("HSTYLE").empty())
            {
                string[] sub_prams = LoadHatchSample(st._prop("HSTYLE"));

                ObjectIdCollection new_ids = new ObjectIdCollection();

                foreach (ObjectId id in res)
                    if (db._isPolylineClosed(id))
                        new_ids.AddRange(_hStyleAction(db, db._getVertices(id), st));
                //new_ids.AddRange(db.HatchByParams(db._getVertices(id), sub_prams, st._prop("HSTYLE")._getInComma()));

                if (!st._prop("ERASE").empty())
                {
                    db.EraseObjects(res);
                    res = new ObjectIdCollection();
                }

                res.AddRange(new_ids);
            }

            if (!st._prop("ARRAY").empty())
            {
                PosCollection pls = new PosCollection(st._prop("ARRAY"));

                if (pls.Count > 0 && pls.First.Length > 0)
                {
                    pPos incr = pls.First[0];
                    pPos lim_ar = pls.First[1];

                    //ACD.WR("ARRAY {0};{1} IDS {2}", incr, lim_ar, res.Count);

                    int n_x = incr.X == 0 ? 1 : (int)Math.Ceiling(lim_ar.X / incr.X);
                    int n_y = incr.Y == 0 ? 1 : (int)Math.Ceiling(lim_ar.Y / incr.Y);

                    for (int i = 0; i < n_x; i++)
                        for (int j = 0; j < n_y; j++)
                            if (i != 0 || j != 0)
                            {
                                //ACD.WR("ARRAY_ITM {0},{1}", x, y);
                                ObjectIdCollection new_res = db.CloneObjects(ids);
                                db.MoveObject(new_res, new pPos(i * incr.X, j * incr.Y));
                                res.AddRange(new_res);
                            }
                }
            }

            if (!st._prop("MIRROR").empty())
            {
                PosCollection pls = new PosCollection(st._prop("MIRROR"));

                foreach (pPos[] ls in pls)
                    if (ls.Length > 1)
                    {
                        ObjectIdCollection cloneIds = db.CloneObjects(res);
                        db.MirrorObjects(cloneIds, ls.First(), ls.Last());
                        res.AddRange(cloneIds);
                    }
            }

            return res;
        }
    }
}

