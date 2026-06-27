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
    public class PolylineToWallCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();
                PosCollection pls = selIds.Cast<ObjectId>()
                    .Select(id => ACD.DB._getVertices(id))
                    .Where(ls => ls.Length() >= 250)
                    .OrderBy(ls => -ls.Length()).ToCollectionSameClosed();

                PosCollection wall_regions = pls.AlignWall("Wall");

                for (int i = 0; i < wall_regions.Count; i++)
                    ACD.DB.DrawWall(new pPos[] { wall_regions[i][0], wall_regions[i][1] }, wall_regions.AlignWall_Styles[i]);

                ACD.DB.EraseObjects(selIds);
            }
            ACD.Focus();
        }
    }
}

