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
    public class PlotSaveViewportCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();
                ObjectIdCollection regionIds = ACD.DB.FilterIds(selIds, "INSERT");

                if (regionIds.Count > 0)
                {
                    bool b = ACD.ED.GetInputString("Do you want to set current scale?(Y/N)","N") == "Y";

                    string sc = "";

                    foreach (ObjectId id in regionIds)
                    {
                        PosCollection regions = new PosCollection();
                        ObjectIdCollection objIds = ACD.DB.ExplodeEntity(id);

                        foreach (ObjectId objId in objIds)
                            regions.Add(ACD.DB._getBound(objId));

                        ACD.DB.EraseObjects(objIds);

                        if (regions.Count > 0)
                        {
                            sc = regions.First().BoundScale(GP.PAGESIZE.X, GP.PAGESIZE.Y);
                            ACD.WR("Scale {0}", sc);
                        }
                    }

                    if (!sc.empty() && b)
                        ACD.DB.SetAnnotationScale(sc);
                }

                ACD.Focus();
            }
        }
    }
}

