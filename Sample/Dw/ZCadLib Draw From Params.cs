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


using System.Windows.Forms;
//using SyncObject;

namespace AcadScript
{
    public class DrawFromParamsCLS
    {
        static pPos basept = null;
        static PosCollection zones;


        static ObjectIdCollection _drawFromParams(ObjectId objId)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            pPos[] bb = ACD.DB._getBound(objId);

            pPos[] r = IZone.GetZoneFromPoint(bb[0]);

            if (r != null)
            {
                pPos[] new_r = IZone.GetZoneFromPoint(basept);
                pPos mv = new_r != null ? new_r[0] - r[0] : basept - r[0];

                if (ACD.DB._isBlock(objId))
                {
                    pPos blockpt = ACD.DB._getPoint(objId);
                    //string blockname = ;
                    ACD.DB.GetEntities(r, EN_SELECT.AC_DXF, "INSERT");

                    ObjectIdCollection blockIds = IR.SelectedIds.ToList().Where(id
                        => ACD.DB._getPoint(id).Content == blockpt.Content).ToCollection();

                    ACD.WR("Blockname {0} count {1}", blockpt.Content, blockIds.Count);

                    string[] xnotes = blockIds.ToList().SelectMany(id => ACD.DB.GetXNotes(id)).ToArray();
                    string blockname = null, layer = null;
                    pPos basepoint = new pPos(0, 0);
                    bool symmetry = false;
                    PosCollection verts = new PosCollection();

                    foreach (string key in xnotes.ToTextStr("|")._allPropNames())
                    {
                        if (key.Upper().EndsWith(".NAME"))
                            blockname = xnotes._props(key);
                        else if (key.Upper().EndsWith(".LAYER"))
                            layer = xnotes._props(key);
                        else if (key.Upper().EndsWith(".SYMMETRY"))
                            symmetry = xnotes._props(key).Upper() == "TRUE" || xnotes._props(key).ToBool();
                        else if (key.Upper().EndsWith(".BASEPOINT"))
                            basepoint = pPos.FromString(xnotes._props(key));
                        else if (key.Upper().EndsWith(".VERTS"))
                            verts = new PosCollection(xnotes._props(key));
                    }

                    if (!blockname.empty())
                    {
                        if (verts.Count == 0)
                        {
                            string verts_fname = Path.Combine(Path.GetDirectoryName(ACD.CurrentDWGPath), blockname + ".verts");

                            ACD.WR("Verts file {0}", verts_fname);

                            if (File.Exists(verts_fname))
                                verts = new PosCollection(File.ReadAllText(verts_fname));
                        }

                        if (verts.Count > 0 && !blockname.empty())
                        {
                            blockname = ACD.DB.uniqueBlockName(blockname);
                            ACD.DB.NewBlock(ACD.DB.DrawPolyline(verts), blockname, true, false, basepoint);

                            ObjectIdCollection newIds = new ObjectIdCollection();
                            ObjectId resId = ACD.DB.Insert(blockname, blockpt);
                            newIds.Add(resId);

                            if (symmetry)
                            {
                                ObjectId tId = ACD.DB.CloneObject(resId);
                                ACD.DB._setScale(tId, -1, 1);
                                newIds.Add(tId);
                            }

                            res.AddRange(newIds);

                            foreach (ObjectId id in blockIds)
                                if (id != objId)
                                {
                                    ObjectIdCollection _newIds = ACD.DB.CloneObjects(newIds);
                                    res.AddRange(_newIds);
                                    ACD.DB.MoveObject(_newIds, mv + ACD.DB._getPoint(id) - blockpt);
                                }

                            ACD.DB.MoveObject(newIds, mv);
                        }
                    }
                }
            }

            return res;
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                basept = ACD.GetPoint();
                if (basept != null)
                {
                    ACD.DB.GenerateAllRegions();
                    ObjectIdCollection selIds = ACD.GetSelection();

                    foreach (ObjectId objId in selIds)
                    {
                        _drawFromParams(objId);
                    }
                }

                ACD.Focus();
            }
        }
    }
}

