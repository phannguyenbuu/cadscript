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
    public static class IExtension
    {
        public static PosCollection scalePointCollection(this PosCollection pls, double scale)
        {
            return pls.Select(ls => ls.Select(p 
                => { pPos np = p * scale; np.Content = p.Content; return np; }).ToArray()).ToCollectionClosedList(pls.Closed);
        } 

        public static string gfComma(this string st)
        {
            string s = st._getBeforeComma();
            string res = st;
            if (!s.empty())
                res = s;
            return res;
        }
    }

    public class Scene3DCLS
    {
        static string file_send = @"D:\transfer.ska";
        static string script_file = @"D:\Dropbox\VS Projects\ScriptEditor\Sample\File\Build Scene.cs";
        
        static void SendClipboardMessage(string caption, string content)
        {
            string st = "[" + caption + "]"
                + "\r\nTime=" + DateTime.Now.ToString("h:mm:ss:tt")
                + "\r\n\r\nScript=" + script_file + "\r\n"
                + "\r\n" + content;


            
                //+ "\r\neF_dPos = " + this.spX.Text + "," + this.spY.Text + "," + this.spZ.Text
                //+ "\r\neF_spD = " + this.spDistance.Text
                //+ "\r\neF_ckMinus = " + this.ckMinus.Checked
                //+ "\r\neF_ckDist = " + this.ckDistance.Checked
                //+ "\r\neF_ckX = " + this.ckX.Checked
                //+ "\r\neF_ckY = " + this.ckY.Checked
                //+ "\r\neF_ckZ = " + this.ckZ.Checked
                //+ "\r\neF_ckSourcePoint = " + this.ckSourcePoint.Checked
                //+ "\r\nKeyboardMode = " + KeyboardMode;


            File.WriteAllText(file_send, st);
        }

        static PosCollection allPolylines, treePls;
        static List<string> allPrams;
        static List<pPos> txtPts;
        static double drawing_scale = 1;


        static void _getParamsFromColorIndex(ObjectId id, string[] xnotes)
        {
            int cindex = ACD.DB._getColorIndex(id);
            //ACD.WR("CIndex {0}", cindex);
            string[] color_indexes = new string[] {"", "Red","Yellow","Green", "Cyan", "Blue","Magenta","Blue","White" };

            if (cindex < color_indexes.Length)
            {
                string val = xnotes._props("Color." + color_indexes[cindex]);

                if (!val.empty())
                    allPrams.Add(val + ".verts=" +  ACD.DB._getVertices(id, 16).Select(p 
                        => p * drawing_scale).ToText(ACD.DB._isPolylineClosed(id))); ;
            }
        }

        static string _blockInfo(ObjectId id)
        {
            pPos p = (ACD.DB._getPoint(id) * drawing_scale).Round(1);
            pPos sc = ACD.DB._getScale(id);
            
            if (sc.Z == 0) sc.Z = 1;

            return p.X + "," + p.Y + ",0," 
                + ACD.DB._getRotation(id).roundNumber(0.01)
                + "," + sc.X.roundNumber(0.01) + "," + sc.Y.roundNumber(0.01) + "," + sc.Z.roundNumber(0.01);
        }

        static void ReadXNoteItem(ObjectIdCollection ids, string[] tree_settings = null)
        {
            //List<string> prams = new List<string>() ;
            ObjectIdCollection checkedIds = new ObjectIdCollection();
            //PosCollection allRegions = new PosCollection();
            string name = tree_settings != null ? tree_settings._props("Name") : null;

            foreach (ObjectId id in ids)
                if(!checkedIds.Contains(id))
                {
                    

                    string[] xnotes = ACD.DB.GetXNotes(id);
                    string blockname = "";

                    if (ACD.DB._isBlock(id))
                    {
                        if (xnotes._props("Sub").ToBool())
                        {
                            ACD.DB.BlockEntitiesAction(id, (subIds) =>
                            {
                                ReadXNoteItem(subIds, 
                                    xnotes.Any(s => s.Upper().Contains(".SPACING")) ? xnotes : null);
                            });
                        }
                        else
                        {
                            blockname = ACD.DB._getIdName(id);

                            ObjectIdCollection sameIds = ids.ToList().Where(d
                                => !checkedIds.Contains(d) && ACD.DB._getIdName(d) == blockname).ToCollection();

                            string props = ACD.DB.GetAllDynBlockProp(id) + "|" + ACD.DB.GetAllBlockAtt(id);

                            string key = (name.empty() ? "" : name + "_") + blockname + (!props._prop("state").empty() ? "."
                                + props._prop("state") : "");

                            string fileIn = ACD.AllSelectionXNotes._props(blockname);
                            if (!fileIn.empty()) key = "@" + fileIn;

                            allPrams.Add(key + "=" + sameIds.ToList().Select(d => _blockInfo(d)).ToTextStr(";"));
                                                        
                            foreach (string propname in props._allPropNames())
                                allPrams.Add(blockname + "." + propname + "=" + props._prop(propname));

                            _getParamsFromColorIndex(id, tree_settings);

                            checkedIds.AddRange(sameIds);
                        }
                    }
                    else if (ACD.DB._isPolyline(id))
                    {
                        blockname = id.Handle.Value.ToString();
                        PosCollection pls = ACD.DB._getVertices(id, 16).ToCollection()
                            .scalePointCollection(drawing_scale);
                                                
                        allPolylines.AddRange(pls);
                        allPolylines.Closed = allPolylines.Closed.Add(ACD.DB._isPolylineClosed(id));

                        if(!name.empty())
                        {
                            allPrams.Add("#" + name + "=" + pls);

                            allPrams.AddRange(xnotes.Where(s => !s._firstPropName().Contains(".")).Select(s => name + "." + s));
                        }

                        if (allPolylines.Closed.Last())
                            allPolylines[allPolylines.Count - 1] = allPolylines[allPolylines.Count - 1].Add(allPolylines.Last().First());

                        if (tree_settings == null && xnotes.Any(s => s.Upper().Contains(".SPACING")))
                            tree_settings = xnotes;

                        if (tree_settings != null && ACD.DB._getLineworkWidth(id) >= 0.01)
                        {
                            treePls.Add(_xnoteTrees(id, tree_settings));
                            treePls.Last()[0].Content = ACD.DB.GetXNotes(id).ToTextStr("\r\n");
                            //ACD.WR("TREE_PLS {0}, {1}", treePls.Count, treePls.AllPoints.Length);
                        }

                        if(tree_settings != null)
                            _getParamsFromColorIndex(id, tree_settings);   
                    }
                    else if (ACD.DB._isText(id))
                    {
                        if(ACD.DB._getContent(id).StartsWith("#"))
                            txtPts.Add(ACD.DB._getPoint(id) * drawing_scale);
                    }

                    foreach (string xnote in xnotes)
                        allPrams.Add(blockname + "." + xnote);

                    checkedIds.Add(id);
                }

            //return allPrams.ToArray();
        }
        static float _ptsAngle(pPos[] pts)
        {
            pPos[] line = pts.MaxSegment().OrderBy(p => p.X).ThenBy(p => p.Y).ToArray();
            return (float)(line[0] - line[1]).Angle();// / 180 * Math.PI;
        }

        static pPos _generateBasePt(pPos[] pts, double distance, out pPos[] bbs)
        {
            float angle = _ptsAngle(pts);
            bbs = pts.Rotate(-90 + angle, pts.CenterPoint()).Boundary();
            //graph.DrawPolygon(Pens.Red, _lCP(bbs.Rect()));

            pPos p1 = new pPos((bbs[0].X + bbs[1].X) / 2, bbs[1].Y).Rotate(-angle + 90, pts.CenterPoint());
            pPos p2 = new pPos((bbs[0].X + bbs[1].X) / 2, bbs[0].Y).Rotate(-angle + 90, pts.CenterPoint());

            p1 = p2.AlongRatio(p1, 1.1);
            p2 = p1.AlongRatio(p2, 0.9);

            //graph.DrawLine(Pens.Red, _cP(p1), _cP(p2));

            pPos[] itps = pts.Intersect(p1, p2).OrderBy(p => -p.Y).ToArray();

            return itps.Length > 0 ? itps.First().Along(distance, p2) : p1.Along(distance, p2);
        }

        static pPos[] _xnoteTrees(ObjectId id, string[] tree_setting)
        {
            List<pPos> res = new List<pPos>();

            string[] propnames = tree_setting.ToTextStr("|")._allPropNames();
            string[] objectnames = propnames.Where(s => s.Contains(".")).Select(s => s.filter(".").First()).Distinct().ToArray();

            using (var tr = ACD.DB.TransactionManager.StartTransaction())
            {
                Random random = new Random();
                // Any random integer   
                string name = tree_setting != null ? tree_setting._props("Name") : null;

                foreach (string blockname in objectnames)
                {
                    double tree_spacing = tree_setting._props(blockname + ".Spacing").ToNumber();

                    if (tree_spacing != 0)
                    {
                        double tree_offset = tree_setting._props(blockname + ".Offset").ToNumber();

                        pPos tree_random_scale = pPos.FromString(tree_setting._props(blockname + ".Random.Scale"));
                        pPos tree_random_rotation = pPos.FromString(tree_setting._props(blockname + ".Random.Rotation"));

                        if (tree_spacing > 0)
                        {
                            var path = (Polyline)tr.GetObject(id, OpenMode.ForRead);
                            double tree_start = tree_setting._props(blockname + ".Start").ToNumber();

                            if (tree_spacing > 0)
                            {
                                var currentLen = tree_start;
                                var startPt = path.StartPoint;

                                while (currentLen < path.Length)
                                {
                                    pPos pt = path.GetPointAtDist(currentLen).ToPos();
                                    pPos p1 = currentLen >= 1 ? path.GetPointAtDist(currentLen - 1).ToPos() : pt;
                                    pPos p2 = currentLen <= path.Length - 1 ? path.GetPointAtDist(currentLen + 1).ToPos() : pt;

                                    if (!(tree_random_rotation.X == 0 && tree_random_rotation.Y == 0))
                                        pt.Rotation = random.Next((int)tree_random_rotation[0], (int)tree_random_rotation[1]);
                                    else if (tree_setting._props(blockname + ".Follow").ToBool())
                                        pt.Rotation = (p2 - p1).Angle() / Math.PI * 180;

                                    pt.Content = (name.empty() ? "" : name + "_") + blockname + "," 
                                        + (tree_random_scale.X != 0 ? random.Next((int)tree_random_scale[0], (int)tree_random_scale[1]) / 100.0 : 1) + ","
                                        + (tree_random_scale.Y != 0 ? random.Next((int)tree_random_scale[0], (int)tree_random_scale[1]) / 100.0 : 1) + ","
                                        + (tree_random_scale.Z != 0 ? random.Next((int)tree_random_scale[0], (int)tree_random_scale[1]) / 100.0 : 1);

                                    res.Add(pt);

                                    currentLen += tree_spacing;
                                }
                            }

                            //ACD.DB.EraseObject(tmpLwpId);
                        }
                    }
                }

                tr.Commit();
            }

            return res.ToArray();
        }

        static void _genrAllRegions(PosCollection pls)
        {
            //string[] nameList = txtPts.Where(p => p.Content.StartsWith("#"))
            //    .Select(p => p.Content.Upper()).Distinct().ToArray();

            GraphConsole.Compute(pls);

            PosCollection regions = GraphConsole.ResultPts;
            ACD.WR("Sources {0} Regions {1}", pls.Count, regions.Count);

            double angle = 0;
            int count = 31;

            foreach (pPos[] pts in regions)
            {
                pPos[] txts = txtPts.Where(p => p.Inside(pts)).ToArray();

                if (txts.Length > 0)
                {
                    string key = txts.First().Content;
                                        
                    ObjectIdCollection hIds = ACD.DB.DrawHatch(pts.Select(p => p / drawing_scale),
                        "HATCH=ANSI" + count + "|HSCALE=10|HANGLE=" + angle);

                    count++;

                    if (count > 77)
                        count = 31;

                    angle += 30;

                    foreach (ObjectId hId in hIds)
                        ACD.DB.SetXNotes(hId, "skClone=on\r\nName=" + key);
                    
                    allPrams.Add(key + ".verts=" + pts.Select(p => p.Round(1)).ToText(false));
                    //ACD.WR("Key = {0} Hatch = {1}", key, allPrams.Last());
                }
                else
                {
                    var ang = _ptsAngle(pts);
                    string house_name = _getHouseNameByArea(pts);
                    //ACD.WR("House_name = {0}", house_name);

                    if (!ACD.DB.HasBlock(house_name).IsNull)
                    {
                        pPos[] bbs = null;
                        
                        ObjectId houseId = ACD.DB.Insert(house_name,
                            _generateBasePt(pts, ACD.AllSelectionXNotes._props(house_name + ".Offset").ToNumber()
                                * drawing_scale, out bbs) / drawing_scale);
                        ACD.DB._setRotation(houseId, 90 - ang);
                        ACD.DB.SetXNotes(houseId, "skClone=on");
                    }
                }
            }

            //for (int i = 0; i < nameList.Length; i++)
            //{
            //    string key = nameList[i];
            //    PosCollection newpls = regions.Where(pts 
            //        => pts[0].Content == key).ToCollectionSameClosed();
            //    allPrams.Add(key + ".verts=" 
            //        + newpls.Select(ls => ls.Select(p => p.Round(1)).ToArray()));
            //}
            
            //return res.ToArray();
        }

        static string _getHouseNameByArea(pPos[] pts)
        {
            string res = null;
            double area = pts.Area()/(drawing_scale * drawing_scale);

            string[] house_names = ACD.AllSelectionXNotes.Where(s => 
                s._firstPropName().Upper().Contains(".AREA.MIN")
                || s._firstPropName().Upper().Contains(".AREA.MAX"))
                .Select(s => s._firstPropName().filter(".").First()).Distinct().OrderBy(s => s.Upper()).ToArray();

            //ACD.WR("House_name = {0}/{1}", house_names.Length,area);

            foreach (string name in house_names)
            {
                double min = ACD.AllSelectionXNotes._props(name + ".Area.Min").ToNumber(Double.NegativeInfinity);
                double max = ACD.AllSelectionXNotes._props(name + ".Area.Max").ToNumber(Double.PositiveInfinity);

                //ACD.WR("Name {0}: {1} < {2} < {3} [{4},{5}]", name, min, area, max, ACD.AllSelectionXNotes._props(name + "Area.Min"), ACD.AllSelectionXNotes._props(name + "Area.Max"));

                if (min <= area && area <= max)
                {
                    res = name;
                    break;
                }
            }

            return res;
        }

        static void Init()
        {
            txtPts = new List<pPos>();
            allPrams = new List<string>();
            allPolylines = new PosCollection();
            //sallRegions = new PosCollection();
            treePls = new PosCollection();
        }

        static pPos[] house_area_limits;
        //static string[] ACD.AllSelectionXNotes;
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ACD.DB.GetEntities(null, EN_SELECT.AC_DXF, "INSERT", "HATCH");
                ACD.DB.EraseObjects(IR.SelectedIds.ToList()
                    .Where(id => ACD.DB.GetXNotes(id)._props("skClone").ToBool()).ToCollection());

                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    int mode = 1;// (int)ACD.ED.GetInputString("Enter mode (1.Show in render / 2.Show in edit mode / 3.Auto render", "1").ToNumber();

                    System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                    stopWatch.Start();

                    Init();
                    //ACD.AllSelectionXNotes = selIds.ToList().SelectMany(id => ACD.DB.GetXNotes(id)).ToArray();
                    drawing_scale = ACD.AllSelectionXNotes._props("Drawing.Scale").ToNumber(drawing_scale);
                    ACD.WR("Drawing.Scale {0}", drawing_scale);

                    ReadXNoteItem(selIds);

                    _genrAllRegions(allPolylines);

                    ACD.WR("Tree {0}", treePls.AllPoints.Length) ;

                    string[] treenames = treePls.AllPoints.Where(p => !p.Content.empty())
                        .Select(p => p.Content.filter().First()).Distinct().ToArray();

                    allPrams.AddRange(treenames.Select(treename =>
                        treename + "=" + treePls.AllPoints.Where(p
                            => !p.Content.empty() && p.Content.filter().First().Upper() == treename.Upper())
                            .Select(p =>
                            {
                                pPos pt = (p * drawing_scale).Round(1);
                                return p.Content.Replace(p.Content.filter().First(), pt.X + "," + pt.Y + ",0," + p.Rotation);
                            }).ToTextStr(";"))
                    );

                    //ACD.WR("E3 {0}", treenames.ToTextStr(","));

                    foreach (pPos p in treePls.AllPoints)
                        if (!p.Content.empty() && !ACD.DB.HasBlock(p.Content.filter().First()).IsNull)
                        {
                            ObjectId blockId = ACD.DB.Insert(p.Content.filter().First(), p);
                            
                            ACD.DB._setRotation(blockId, p.Rotation);

                            pPos sc = new pPos(1, 1, 1);
                            string[] ar = p.Content.filter();

                            if (ar.Length > 3)
                                sc = new pPos(ar[1].ToNumber(), ar[2].ToNumber(), ar[3].ToNumber());

                            ACD.DB._setScale(blockId, sc.X, sc.Y);
                            ACD.DB.SetXNotes(blockId, "skClone=on");
                        }
                    
                    TimeSpan ts = stopWatch.Elapsed;
                    ACD.WR("Step 2 {0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                    //ACD.WR("E4");
                                        
                    stopWatch.Stop();
                    ts = stopWatch.Elapsed;
                    
                    ACD.WR("Step 3 {0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

                    //string[] propnames = allPrams.Select(s => s._firstPropName()).Distinct().ToArray();
                    string[] res = new string[] { "Mode=" + mode };

                    foreach (string s in allPrams)
                    {
                        string key = s._firstPropName();
                        string val = s._firstProp();

                        if ((key.StartsWith("#") && key.Upper().EndsWith(".VERTS") || !key.Contains("."))
                            && (new PosCollection(val).Count) > 0 || key.StartsWith("@"))
                                res = res._setprops(key, res._props(key) + ";;" + val);
                    }

                    string cmd = res.ToTextStr("\r\n")
                        .Replace("=;;", "=").Replace(".verts", "").Replace(";;", "|");

                    string[] keys = new string[] { "FBX", "Source.3D" };

                    foreach(string k in keys)
                        if(!ACD.AllSelectionXNotes._props(k).empty())
                            cmd += "\r\n" + k + "=" + ACD.AllSelectionXNotes._props(k);

                    SendClipboardMessage("MaxCommand", cmd);
                }

                ACD.Focus();
            }
        }
    }
}