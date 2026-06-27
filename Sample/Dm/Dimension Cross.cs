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
using Autodesk.Aec.ArchDACH.DatabaseServices;


using System.Windows.Forms;

namespace AcadScript
{
    public class DimensionCrossCLS
    {
        static void _dimCrossLines(PosCollection pls)
        {
            pPos[] npts = pls.SelfIntersect;

            for (int i = 0; i < pls.Count; i++)
            {
                List<pPos> cross = new List<pPos>();

                for (int j = 0; j < pls[i].Length - 1; j++)
                    foreach (pPos p in npts)
                        if (p.IsBetween(pls[i][j], pls[i][j + 1]))
                            cross.Add(p);

                if (cross.Count < 2 && pls[i].Length == 2)
                    IDimChain.CreateAlignDimension(ACD.DB, pls[i][0], pls[i][1], 0, 0);
                else if (cross.Count == 1 && pls[i].Length == 2)
                {
                    IDimChain.CreateAlignDimension(ACD.DB, pls[i][0], cross[0], 0, 0);
                    IDimChain.CreateAlignDimension(ACD.DB, cross[0], pls[i][1], 0, 0);
                }
                else if (cross.Count >= 2)
                    for (int j = 0; j < cross.Count - 1; j++)
                        IDimChain.CreateAlignDimension(ACD.DB, cross[j], cross[j + 1], 0, 0);
                
                else
                    for (int j = 0; j < pls[i].Length - 1; j++)
                        IDimChain.CreateAlignDimension(ACD.DB, pls[i][j], pls[i][j + 1], 0, 0);
            }
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    PosCollection __pls = new PosCollection();
                    foreach (ObjectId _id in selIds)
                    {
                        pPos[] pts = ACD.DB._getVertices(_id, 16);
                        if (pts.Length > 0)
                            __pls.Add(pts);

                    }

                    _dimCrossLines(__pls);
                }

                ACD.Focus();
            }
        }
    }
}
