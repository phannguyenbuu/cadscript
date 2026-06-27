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
    public class ObjectCloningCLS
    {
        //static void 
        static pPos[] _getXLine(ObjectId id)
        {
            List<pPos> res = new List<pPos>();
            using (Transaction tr = ACD.DB.TransactionManager.StartTransaction())
            {
                Xline xline = (Xline)tr.GetObject(id, OpenMode.ForRead);

                if (xline != null)
                //res = new pPos[] { xline.BasePoint.ToPos(), xline.SecondPoint.ToPos() } ;
                {
                    Point3dCollection p3s = new Point3dCollection();

                    xline.GetStretchPoints(p3s);

                    foreach (Point3d p in p3s)
                        res.Add(p.ToPos());

                    ACD.WR("P {0};{1}", xline.BasePoint.ToPos(), p3s.Count);
                }
                tr.Commit();
            }

            return res.ToArray();
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    PosCollection pls = selIds.ToList().Where(objId => objId.ObjectClass.DxfName == "XLINE")
                        .Select(objId => _getXLine(objId)).ToCollectionSameClosed(false);
                        
                    ObjectIdCollection nodeIds = selIds._filterDXF("INSERT");

                    List<pPos> points = new List<pPos>();

                    for (int i = 0; i < pls.Count - 1; i++)
                        for (int j = i + 1; j < pls.Count; j++)
                            points.Add(pls[i][0].Intersect(pls[i][1],pls[j][0], pls[j][1], false));

                    //ACD.WR("Points {0}", points.Count);

                    points = (new PosCollection(points.Where(p => p!= null).Select(p => p.ToString()).Distinct().ToTextStr(";"))).First().ToList();

                    foreach (ObjectId nodeId in nodeIds)
                    {
                        foreach (pPos p in points)
                        {
                            ObjectId newId = ACD.DB.CloneObject(nodeId);
                            ACD.DB._setPoint(newId, p);
                        }
                    }

                    ACD.DB.EraseObjects(selIds._filterDXF("XLINE"));
                }
            }    
            
            ACD.Focus();
        }
    }
}

