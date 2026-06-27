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
    public class PlotViewportExtractionCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    //pPos pt = ACD.GetPoint();

                    //if(pt != null)
                    foreach (ObjectId id in selIds)
                    {
                        string fname = null;
                        pPos[] pts = null;
                        if (ACD.DB._isViewport(id))
                        {
                            //ACD.WR(ACD.DB._getIdInfo( id));
                            pts = ACD.DB._getBound(id);//.Transform(ACD.DB.mViewportToModel(id)).Rect();
                            //ACD.WRArray("Region_", r);
                            ACD.DB.DrawPolyline(pts);

                            fname = Path.GetDirectoryName(ACD.CurrentDWGPath) + @"\exp\";
                            Directory.CreateDirectory(fname);
                            fname += "ref" + id.ToText() + ".dwg";
                        }
                        else if (ACD.DB._isPolylineClosed(id))
                        {
                            pts = ACD.DB._getVertices(id);
                            fname = ACD.ED.GetInputString("Input filename", "New lib");
                        }

                        if (fname != null && pts != null)
                            using (Database db = ACD.ReadDWG(DE.CAD_TEMPLATE_FILE))
                            {
                                ACD.DB.GetEntities(pts, EN_SELECT.AC_ALL);
                                ObjectIdCollection ids = ACD.DB.CloneObjects(IR.SelectedIds, db);

                                if (ids.Count > 0)
                                {
                                    db.MoveObject(ids, new pPos(0, 0) - pts.Boundary()[0]);
                                    db.SaveAs(fname, DwgVersion.Current);
                                    ACD.WR("Viewport save to file {0}", fname);
                                }
                                else
                                    ACD.WR("No ObjectId Clone");
                            }
                    }
                }
                ACD.Focus();
            }
        }
    }
}

