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
using AEC = Autodesk.Aec.Arch.DatabaseServices;

namespace AcadScript
{
    public class ObjectLayerCLS
    {
        void _drawClip(pPos[] R, double thep_tren_trai, double thep_tren_phai)
        {
            double C = 25, CLP1 = 100, CLP2 = 200;
            double y1 = R[0].Y + C / 2;
            double y2 = R[1].Y - C / 2;

            for (double i = 0; i < 5; i++)
            {
                double curx = R[0].X - (4 - i) * CLP1 + thep_tren_trai;
                ACD.DB.Draw2D(curx, y1, curx, y2, "B-Clip");

                curx = R[0].X + i * CLP1 + thep_tren_phai;
                ACD.DB.Draw2D(curx, y1, curx, y2, "B-Clip");

                curx = (R[0].X + R[1].X) / 2 + (i - 2) * CLP2;
                ACD.DB.Draw2D(curx, y1, curx, y2, "B-Clip");
            }
        }

        static void BlockElementToLayer(Database db, ObjectId blockId, string layer)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                BlockReference block = (BlockReference)tr.GetObject(blockId, OpenMode.ForWrite);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(block.BlockTableRecord, OpenMode.ForWrite);

                //ACD.DB.EraseObjects(btr.Cast<ObjectId>()
                    //.Where(id => ACD.DB._isDim(id) || ACD.DB._isText(id)).Select(id => id).ToCollection());

                foreach (ObjectId lwpId in btr.Cast<ObjectId>())
                    ACD.DB._setLayer(lwpId, layer);

                btr.UpgradeOpen();

                tr.Commit();
            }
        }

        static void _setMLStyle(Database db, ObjectId id, string mlStyleName)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                DBDictionary MlineStyleDic = (DBDictionary)tr.GetObject(db.MLStyleDictionaryId, OpenMode.ForWrite);

                

                if (!MlineStyleDic.Contains(mlStyleName))
                {
                    MlineStyleDic.UpgradeOpen();
                    MlineStyle mlineStyle = new MlineStyle();
                    MlineStyleDic.SetAt(mlStyleName, mlineStyle);

                    tr.AddNewlyCreatedDBObject(mlineStyle, true);

                    mlineStyle.EndAngle = Math.PI * 0.5;
                    mlineStyle.StartAngle = Math.PI * 0.5;
                    
                    mlineStyle.Name = mlStyleName;

                    string[] keywords = mlStyleName.filter("_").Select(s => s.Upper()).ToArray();

                    string val = keywords.FirstOrDefault(s => s.st_("clr") || s.st_("color"));

                    short colorindex = 0;
                    if (!val.et_())
                        colorindex = (short)val.Replace("clr", "").Replace("color", "").ToNumber();

                    val = keywords.FirstOrDefault(s => "0123456789".Any(ch => s.st_(ch.ToString())));
                    
                    double lwidth = 1;
                    if (!val.et_())
                        lwidth = val.ToNumber();

                    Autodesk.AutoCAD.Colors.Color Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByAci, colorindex);

                    MlineStyleElement element = new MlineStyleElement(lwidth / 2, Color, db.Celtype);
                    mlineStyle.Elements.Add(element, true);

                    element = new MlineStyleElement(-lwidth / 2, Color, db.Celtype);
                    mlineStyle.Elements.Add(element, false);

                    if(keywords.Contains("R"))
                    {
                        mlineStyle.StartRoundCap = true;
                        mlineStyle.EndRoundCap = true;
                    }

                    if (keywords.Contains("C"))
                    {
                        mlineStyle.StartSquareCap = true;
                        mlineStyle.EndSquareCap = true;
                    }
                }

                MlineJustification justify = MlineJustification.Top;
                bool closed = false;

                if (db._isMLine(id))
                {
                    Mline srcMl = (Mline)tr.GetObject(id, OpenMode.ForRead);
                    justify = srcMl.Justification;
                    closed = srcMl.IsClosed;
                }
                else
                    closed = db._isPolylineClosed(id);

                Mline ml = new Mline();

                ml.Style = MlineStyleDic.GetAt(mlStyleName);

                ml.Normal = Vector3d.ZAxis;
                ml.Justification = justify;

                pPos[] pts = db._getVertices(id);

                foreach (pPos p in pts)
                    ml.AppendSegment(p.ToPoint3());

                ml.IsClosed = closed;

                btr.AppendEntity(ml);
                tr.AddNewlyCreatedDBObject(ml, true);

                db.EraseObject(id);
                
                tr.Commit();
            }
        }

        public static void Main(string[] args)
        {
            string[] layer_keywors = pString.INI_String("ACAD_OBJECT_LAYERS").filter(";"); //new string[] { "0", "Defpoints", "A-FIN", "A-Hidden", "A-Hatch", "I-Furn" };

            using (ACD.Lock())
            {
                string key = "<LAYER/>";

                ObjectIdCollection selIds = new ObjectIdCollection();

                if (!key.ct_("mode"))
                    selIds = ACD.GetSelection();

                ACD.DB._setLayerColor("A-Hatch", 8);
                ACD.DB._setLayerColor("A-Hidden", 8);

                ObjectId gridId = selIds.ToList().FirstOrDefault(id => ACD.DB._isGrid(id));

                List<double[]> gridXys = new List<double[]>();

                if (!gridId.IsNull)
                    ACD.DB.BlockEntitiesAction(gridId, _ids => { 
                        gridXys = _ids.ToList().Where(_id 
                            => ACD.DB._isPolyline(_id)).Select(_id 
                            => ACD.DB._getVertices(_id)).ToCollectionSameClosed(false).ExtractPtsXY(50,100); });



                if (key.ct_("replace_f6_f8"))
                {
                    ObjectIdCollection dimIds = selIds.FilterIds("DIMENSION");

                    foreach (ObjectId id in dimIds)
                    {
                        string st = ACD.DB._getContent(id).Replace("\r\n", "-");

                        if (!st.et_() && st != "<>")
                        {
                            ACD.WR("Value {0} new {1}", st, st.Replace("6a", "8a"));
                            ACD.DB._setContent(id, st.Replace("6a", "8a"));
                        }
                    }
                }
                else if (key.ct_("split_beam") && gridXys.Count > 0)
                {
                    foreach (ObjectId id in selIds)
                        if (!ACD.DB._isGrid(id))
                        {
                            pPos[] bb = ACD.DB._getBound(id);

                            double h = 400;

                            string[] xnotes = ACD.DB.GetXNotes(id);

                            if (xnotes.Any(s => s.st_("#H")))
                                h = xnotes.FirstOrDefault(s => s.st_("#H")).Replace("#H", "").ToNumber();

                            pPos p1 = new pPos(bb[0].X, bb[0].Y);
                            pPos p2 = new pPos(bb[1].X, bb[0].Y);


                            List<double> lsX = gridXys[0].Where(v => p1.X <= v - 200 && p2.X >= v + 200).ToList();
                            lsX.Insert(0, p1.X);
                            lsX.Add(p2.X);

                            lsX = lsX.Select(v => v.roundNumber(50)).Distinct().ToList();

                            for (int i = 0; i < lsX.Count - 1; i++)
                            {
                                ACD.DB.Draw2D(new pPos(lsX[i], p1.Y).RectToPoint(new pPos(lsX[i + 1], p1.Y - h)), "c");
                            }

                            ACD.DB.CreateText(xnotes.FirstOrDefault(s => s.st_("#")), p1 + new pPos(0, 500), 4);
                            ACD.DB.CreateText("L=" + (p2.X - p1.X).roundNumber(10) + " - SL:1", p1 + new pPos(0, 300), 2);
                        }
                }
                else if (key.ct_("cross"))
                {
                    foreach (ObjectId id in selIds)
                    {
                        pPos[] bb = ACD.DB._getBound(id);
                        ACD.DB.Draw2D(bb[0], bb[1]);
                        ACD.DB.Draw2D(bb[0].X, bb[1].Y, bb[1].X, bb[0].Y);
                    }
                }
                else if (key.ct_("bottom_mleader"))
                {
                    foreach (ObjectId id in selIds)
                        ACD.DB._setMLeadeBottomUnderline(id);
                }
                else if (key.ct_("add_beam_clip"))
                {
                    double height = ACD.ED.GetInputString("Enter beam height", "400").ToNumber();
                    ObjectIdCollection dimIds = selIds._filterDXF("DIMENSION");

                    foreach (ObjectId id in dimIds)
                    {
                        string content = ACD.DB._getContent(id);
                        pPos p1 = ACD.DB._getPoint(id, 0), p2 = ACD.DB._getPoint(id, 1);

                        if (content.ct_("a100") || content.ct_("a200"))
                        {
                            double clp = content.ct_("a100") ? 100 : 200;
                            for (double i = 0; i < 5; i++)
                            {
                                double curx = (i - 2) * clp + (p1.X + p2.X) / 2;
                                ACD.DB.Draw2D(curx, p1.Y - 25, curx, p1.Y - height + 25, "B-Clip");
                            }
                        }
                    }
                }
                else if (key.ct_("fix_dupl_dim"))
                {
                    ObjectIdCollection dimIds = selIds._filterDXF("DIMENSION");

                    string[] all_dims = dimIds.ToList().Select(id
                       => new pPos[] { ACD.DB._getPoint(id, 0).Round(10),
                           ACD.DB._getPoint(id, 1).Round(10),
                           ACD.DB._getPoint(id, 2).Round(10) }.ToText(false))
                        .Distinct().ToArray();

                    ObjectIdCollection erIds = new ObjectIdCollection();

                    foreach (string dimst in all_dims)
                    {
                        ObjectIdCollection ids = dimIds.ToList().Where(id
                       => dimst == new pPos[] { ACD.DB._getPoint(id, 0).Round(10),
                           ACD.DB._getPoint(id, 1).Round(10),
                           ACD.DB._getPoint(id, 2).Round(10) }.ToText(false)).ToCollection();

                        if (ids.Count > 1)
                            for (int i = 0; i < ids.Count - 1; i++)
                                erIds.Add(ids[i]);
                    }

                    if (erIds.Count > 0)
                        ACD.DB.EraseObjects(erIds);

                    ACD.WR("Erase objects {0}", erIds.Count);
                }
                else if (key.ct_("fix_beam_section"))
                {
                    //ACD.DB.GetEntities(null, EN_SELECT.AC_DXF, "LWPOLYLINE", "DIMENSION", "MTEXT", "TEXT");
                    ObjectIdCollection txtIds = selIds._filterDXF("DIMENSION", "MTEXT", "TEXT");

                    ObjectIdCollection sIds = selIds._filterDXF("LWPOLYLINE").ToList()
                        .Where(id => (ACD.DB._getVertices(id).Length == 6
                        && ACD.DB._getBound(id).Size().X.roundNumber(10) == 300
                        && ACD.DB._getBound(id).Size().Y.roundNumber(10) == 400)

                        || (ACD.DB._getVertices(id).Length == 6
                        && ACD.DB._getBound(id).Size().Y.roundNumber(1) == 125)).ToCollection();

                    foreach (ObjectId id in sIds)
                        if (ACD.DB._getBound(id).Size().X.roundNumber(10) == 300)
                            ACD.DB.Draw2D(ACD.DB._getBound(id)[0].Rect(200, 400), "c");

                    foreach (ObjectId id in txtIds)
                    {
                        string s = ACD.DB._getContent(id);

                        if (s.ct_("(") && s.ct_(")"))
                            s = s.Replace("(" + s._getInComma("()") + ")", "");

                        if (s == "6" || s == "8" || s == "10" || s == "12" || s == "14") s = "4";
                        if (s == "5" || s == "7" || s == "9" || s == "11" || s == "13" || s == "15") s = "3";

                        ACD.DB._setContent(id, s);
                    }

                    ACD.DB.EraseObjects(sIds);
                }
                else if (key.ct_("sum_text"))
                {
                    double result = selIds.ToList().Where(id => ACD.DB._isText(id)).Sum(id => ACD.DB._getContent(id).ToNumber());
                    ACD.WR("Result {0}=", result);
                    System.Windows.Forms.Clipboard.SetText(result.ToString());
                }
                else if (key.st_("ml"))
                {
                    foreach (ObjectId mId in selIds)
                        if(ACD.DB._isMLine(mId) || ACD.DB._isPolyline(mId))
                        {
                            _setMLStyle(ACD.DB, mId, key);

                        }
                }
                else if (key.ct_("replace_D_number"))
                {
                    foreach (ObjectId txtId in selIds)
                    {
                        string[] contents = ACD.DB._getContent(txtId).filter("\r\n");
                        string val = contents.FirstOrDefault(s => s.st_("[1]"));
                        string line = "", newline = "";

                        if (!val.et_())
                        {
                            val = val.filter(" ").FirstOrDefault(s => s.st_("L"));
                            if (!val.et_())
                            {
                                int num = (int)(val.Substring(1).ToNumber() / 150);
                                line = contents.FirstOrDefault(s => s.st_("[D]"));

                                ACD.WR("Num {0}", num);

                                if (!val.et_())
                                {
                                    string val_s = line.filter("x ").FirstOrDefault(s => s.st_("N"));
                                    ACD.WR("Val_s {0}", val_s);

                                    if (!val_s.et_())
                                        newline = line.Replace(val_s, "N" + num);
                                }
                            }
                        }

                        if (!line.et_() && !newline.et_())
                            ACD.DB._setContent(txtId, ACD.DB._getContent(txtId).Replace(line, newline));
                    }
                }
                else if (key.ct_("mode_hidden_furniture"))
                {
                    ACD.DB._setLayerColor("I-Furniture", 8);
                    ACD.DB._setLayerLineType("I-Furniture", "HIDDEN");
                }
                else if (key.ct_("extent_u_steel"))
                {
                    ObjectIdCollection hisIds = new ObjectIdCollection();

                    foreach (ObjectId id in selIds)
                    {
                        List<pPos> pts = (new PosCollection(ACD.DB._getVertices(id).Select(p => p.Round(10).ToString()).Distinct().ToTextStr(";"))).First().ToList();

                        pPos v1 = new pPos(pts[0].Y - pts[1].Y, pts[1].X - pts[0].X).Normalize;
                        pPos v2 = new pPos(pts[pts.Count - 2].Y - pts[pts.Count - 1].Y,
                            pts[pts.Count - 1].X - pts[pts.Count - 2].X).Normalize;

                        pPos np = new pPos(0, 0);

                        if (pts[0].DistanceTo(pts[1]) > 200)
                        {
                            //ACD.WR("Pts_1 {0}; {1}", pts[0], pts[0] + v1 * 100);
                            np = pts[0] - v1 * 100;
                            if (np.X < pts[0].X || np.Y > pts[0].Y)
                                np += v1 * 200;

                            pts.Insert(0, np);
                            if (!hisIds.Contains(id)) hisIds.Add(id);
                        }

                        if (pts[pts.Count - 1].DistanceTo(pts[pts.Count - 2]) > 200)
                        {
                            np = pts.Last() - v2 * 100;
                            if (np.X < pts.Last().X || np.Y > pts.Last().Y)
                                np += v2 * 200;

                            pts.Add(np);
                            if (!hisIds.Contains(id)) hisIds.Add(id);
                        }

                        //ACD.WR("Pts_2 {0}", pts.ToText());

                        ACD.DB.Draw2D(pts.ToArray(), "LAYER=" + ACD.DB._getLayer(id), "LWIDTH=" + ACD.DB._getLineworkWidth(id));
                    }

                    ACD.DB.EraseObjects(hisIds);
                }
                if (key.ct_("mode_normal_furniture"))
                {
                    ACD.DB._setLayerColor("I-Furniture", 30);
                    ACD.DB._setLayerLineType("I-Furniture", "Continuous");
                }
                else if (key.st_("FENCELINE"))
                {
                    foreach (ObjectId id in selIds)
                        if (ACD.DB._isPolyline(id))
                        {
                            //ACD.DB._setLayer(id, "A-HATCH");
                            ACD.DB._setIdInfo(id, "LAYER=A-Hatch|LTYPE=FENCELINE1|LSCALE=0.25|LWIDTH=10");
                        }
                }
                else if (key.ct_("BOLD"))
                {
                    string w = ACD.ED.GetInputString("Linewidth (10)", "10");
                    foreach (ObjectId id in selIds)
                        if (ACD.DB._isPolyline(id))
                            ACD.DB._setLineworkWidth(id, w.ToNumber());
                }
                else if (key.st_("<ROUND"))
                {
                    double _val = ACD.ED.GetInputString("Round (10)", "100").ToNumber();
                    foreach (ObjectId id in selIds)
                        if (ACD.DB._isPolyline(id))
                            ACD.DB.FilletAll(id, _val);
                }
                else if (key.ct_("NROUND"))
                {
                    double _val = key.Replace("<","").Replace(">", "").filter("_").Last().ToNumber(10);

                    foreach (ObjectId id in selIds)
                        if (ACD.DB._isPolyline(id))
                        {
                            ACD.DB.FilletAll(id, _val * 20);
                            ACD.DB._setIdInfo(id, "LAYER=A-Hatch|LTYPE=FENCELINE1|LSCALE=0.25|LWIDTH=" + _val);
                            
                        }
                }


                //LAYER KEYWORDS
                else if (layer_keywors.Contains(key))
                {
                    //ACD.DB._setLayer(selIds, key);

                    foreach (ObjectId id in selIds)
                        if (ACD.DB._isBlock(id))
                        {
                            //BlockElementToLayer(ACD.DB, id, key);
                            ACD.DB.BlockEntitiesEdit(id, _ids => { foreach (ObjectId _id in _ids) ACD.DB._setLayer(_id, key); });
                            ACD.DB._setLayer(id, "0");
                        }
                        else
                            ACD.DB._setLayer(id, key);

                    ACD.ED.Regen();
                }
                else if (key.ct_("<ANNO>"))
                {
                    foreach (ObjectId _id in selIds)
                        if (ACD.DB._isText(_id) || ACD.DB._isDim(_id) || ACD.DB._isLeader(_id))
                            ACD.DB._setLayer(_id, "A-Anno-Dims");

                    foreach (ObjectId id in selIds)
                        if (ACD.DB._isBlock(id))
                            ACD.DB.BlockEntitiesEdit(id, _ids =>
                            {
                                foreach (ObjectId _id in _ids)
                                    if (ACD.DB._isText(_id) || ACD.DB._isDim(_id) || ACD.DB._isLeader(_id))
                                        ACD.DB._setLayer(_id, "A-Anno-Dims");
                            });
                }
                else if (key.ct_("WALL"))
                {
                    foreach (ObjectId id in selIds)
                        if (ACD.DB._isWall(id))
                            ACD.DB._setWallStyle(id, key);
                }
                else if (key.st_("D") || key.StartsWith("B") || key.st_("S"))
                {
                    foreach (ObjectId id in selIds)
                        if (ACD.DB._isDoor(id))
                            ACD.DB._setDoorStyle(id, key);
                }else if(key.st_("@"))
                {
                    string linetypename = key.Substring(1);
                    ACD.DB.CreateComplexLinetype("Name=" + linetypename + "|Value=" + linetypename, "------ ");

                    foreach (ObjectId id in selIds)
                    {
                        ACD.DB._setLayer(id, DE.DEFPOINTS);
                        ACD.DB._setLineworkLineType(id, linetypename);
                        ACD.DB._setLineworkLineScale(id, 5);
                    }
                }
            }

            ACD.Focus();
        }
    }
}

