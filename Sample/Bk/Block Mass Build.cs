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

//
using System.Windows.Forms;
//using SyncObject;

namespace AcadScript
{
    public class BlockMassBuildCLS
    {
        static List<double[]> _getAllXys(ObjectIdCollection ids, int round)
        {
            List<double> lsX = new List<double>();
            List<double> lsY = new List<double>();

            foreach (ObjectId id in ids)
            {
                if (ACD.DB._isBlock(id))
                {
                    pPos[] bb = ACD.DB.GetXCLIP(id);

                    if (bb != null)
                    {
                        lsX.Add(bb[0].X);
                        lsX.Add(bb[1].X);
                        lsY.Add(bb[0].Y);
                        lsY.Add(bb[1].Y);
                    } else
                    ACD.DB.BlockEntitiesAction(id, _ids =>
                    {
                        List<double[]> tmps = _getAllXys(_ids, round);
                        lsX.AddRange(tmps[0]);
                        lsY.AddRange(tmps[1]);
                    });
                }
                else if (!ACD.DB._isText(id))
                {
                    pPos[] bb = ACD.DB._getBound(id);

                    for (double x = bb[0].X; x <= bb[1].X; x += round)
                        lsX.Add(x);

                    for (double y = bb[0].Y; y <= bb[1].Y; y += round)
                        lsY.Add(y);
                }
            }

            return new List<double[]> { lsX.ToArray(), lsY.ToArray() };
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                ObjectIdCollection selIds = ACD.GetSelection();
                int round = (int)ACD.ED.GetInputString("Enter round", "100").ToNumber();

                if (selIds.Count > 0)
                {
                    var vals = _getAllXys(selIds, 100).Select(ls => ls.Distinct().ToArray()).ToList();

                    ACD.WR("X={0}\r\nY={1}", vals[0].ToTextDouble(","), vals[1].ToTextDouble(","));
                }

                ACD.Focus();
            }
        }
    }
}

