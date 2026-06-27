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
    public class FlipSmallDimTextCLS
    {
        static pPos[] _getDimPoints(Database db, ObjectId objId)
        {
            pPos[] res = null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (objId.ObjectClass.DxfName == "DIMENSION")
                {
                    Entity ent = (Entity)tr.GetObject(objId, OpenMode.ForWrite);

                    if (ent is RotatedDimension)
                    {
                        RotatedDimension dim = (RotatedDimension)ent;
                        if (dim != null)
                        {
                            Point3dCollection p3ts = new Point3dCollection();
                            dim.GetStretchPoints(p3ts);

                            res = new pPos[] { p3ts[2].ToPos(), p3ts[3].ToPos() };
                        }
                    }
                }
                tr.Commit();
            }
            return res;
        }


        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();
                Database db = ACD.DB;

                double d = ACD.ED.GetInputString("Number value:", "200").ToNumber(200);

                foreach (ObjectId dimId in selIds)
                    if (ACD.DB._isDim(dimId) && ACD.DB._getLength(dimId) <= DE.DEF_TRACK_VALUE)
                    {
                        ACD.DB.ResetDefaultDimText(dimId, false);
                        //double angle = ACD.DB._getRotation(dimId);
                        //int axis = angle == 0 ? 0 : 1;

                        pPos[] line = _getDimPoints(ACD.DB, dimId);
                        //for(int i = 0; i < line.Length; i++)
                        //    ACD.DB.CreateText(i.ToString(), line[i]);

                        pPos txt_pt = db._getDimTextPos(dimId);

                        //ACD.DB.DrawCircle(txt_pt, 20);
                        
                        pPos p = txt_pt.ProjectLine(line[0], line[1]);
                        //double d = txt_pt.DistanceTo(line[0], line[1]);
                        //ACD.DB.DrawCircle(p, 50);
                        db._setDimTextPos(dimId, txt_pt.AlongRatio(p, 2.0));
                    }
            }
            ACD.Focus();
        }
    }
}

