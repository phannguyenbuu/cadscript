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

//using NCalc;

namespace AcadScript
{
    public class TextAllToCurrentFontCLS
    {
        static void _setAllBlockAtt(Database db, ObjectId objId)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference blockRef = (BlockReference)tr.GetObject(objId, OpenMode.ForWrite);
                foreach (ObjectId attId in blockRef.AttributeCollection)
                {
                    var attDef = tr.GetObject(attId, OpenMode.ForWrite) as AttributeReference;
                    if (attDef == null)
                        continue;
                    
                    attDef.TextStyleId = ACD.DB.Textstyle;
                    attDef.Dispose();
                }

                tr.Commit();
            }
        }
        
        public static void _setCurrentStyle(ObjectId id)
        {
            Database db = ACD.DB;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                TextStyleTableRecord ttr = (TextStyleTableRecord)db.Textstyle.GetObject(OpenMode.ForRead);
                if (id.ObjectClass.DxfName == "MTEXT")
                {
                    MText mtxt = (MText)tr.GetObject(id, OpenMode.ForWrite);
                    mtxt.TextStyleId = db.Textstyle;

                    if (db._getLayer(id).Upper() != DE.DEFPOINTS.Upper())
                        db._setLayer(id, DE.DEF_LAYER_TEXT);
                }
                else if (id.ObjectClass.DxfName == "TEXT")
                {
                    DBText mtxt = (DBText)tr.GetObject(id, OpenMode.ForWrite);
                    mtxt.TextStyleId = db.Textstyle;
                    if (db._getLayer(id).Upper() != DE.DEFPOINTS.Upper())
                        db._setLayer(id, DE.DEF_LAYER_TEXT);
                }
                else if (id.ObjectClass.DxfName == "DIMENSION")
                {
                    Dimension dim = (Dimension)tr.GetObject(id, OpenMode.ForWrite);
                    dim.DimensionStyle = db.Dimstyle;
                    db._setLayer(id, pString.INI_String("DIM_LAYER"));
                }
                else if (id.ObjectClass.DxfName == "ACAD_TABLE")
                {
                    Table tab = (Table)tr.GetObject(id, OpenMode.ForWrite);
                    tab.TableStyle = db.Tablestyle;
                    db._setLayer(id, DE.DEF_LAYER_TEXT);
                }
                else if (db._isBlock(id))
                {
                    if (db._isGObject(id))
                        db._setLayer(id, DE.DEF_LAYER_TEXT);

                    double sc = Math.Abs(db._getScale(id).X);
                    BlockReference blockRef = (BlockReference)tr.GetObject(id, OpenMode.ForWrite);
                    foreach (ObjectId attId in blockRef.AttributeCollection)
                    {
                        var attDef = tr.GetObject(attId, OpenMode.ForWrite) as AttributeReference;

                        if (attDef != null)
                        {
                            attDef.TextStyleId = db.Textstyle;
                            attDef.Height = (attDef.Tag.Upper().Contains("SCALE") ? 3 : 4) * sc;
                            attDef.Dispose();
                        }
                        //res = attDef.IsMTextAttribute ? attDef.MTextAttribute.Text : attDef.TextString;

                    }
                }
                else if (db._isHatch(id))
                {
                    db._setLayer(id, DE.DEF_LAYER_HATCH);
                }
                tr.Commit();
            }
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ACD.DB.GetEntities(null, EN_SELECT.AC_DXF, "INSERT", "MTEXT","TEXT");

                foreach (ObjectId id in IR.SelectedIds)
                    if (ACD.DB._isBlock(id))
                    {
                        try
                        {
                            _setAllBlockAtt(ACD.DB, id);
                        }
                        catch (System.Exception ex)
                        {
                            continue;
                        };
                    }else
                    {
                        _setCurrentStyle(id);
                    }
            }
           
            ACD.Focus();
        }
    }
}

