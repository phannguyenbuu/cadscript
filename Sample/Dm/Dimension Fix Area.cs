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
    public class FixAreaCLS
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

                pPos basept = ACD.GetPoint();
                double new_area = ACD.ED.GetInputString("Enter New Area:", "0").ToNumber(0);
                

                //double new_value = 0;

                if(basept != null)
                    foreach (ObjectId pId in selIds)
                        if (ACD.DB._isPolylineClosed(pId))
                        {
                            pPos[] pts = ACD.DB._getVertices(pId);
                            pPos[] copy_pts = pts.Select(_p => _p).ToArray();

                            basept.NearestPoints(pts);

                            int index = pPos.nearestPoint_Index;
                            
                            PosCollection area_list = new PosCollection();

                            foreach(int dir in new int[] { -1, 1 })
                                for(int i = 1; i <= 1000; i++)
                                {
                                    double offset_val = dir * i * 0.001;

                                    copy_pts[index] = pts[index].Along(offset_val, basept);

                                    area_list.Add(copy_pts.Select(__p => __p).ToArray());
                                }

                            area_list = area_list.OrderBy(__itm => Math.Abs(new_area - __itm.Area())).ToCollectionSameClosed(true);

                            //pPos[] __line01 = p1.Parallel(p2, 0.1 * dicts.First().Key);
                            ACD.DB.DrawPolyline(area_list.First(), true);

                            ACD.WR("New value {0}:", Math.Abs(new_area - area_list.First().Area()));
                        }

                ACD.DB.EraseObjects(selIds);
            }
            ACD.Focus();
        }
    }
}

