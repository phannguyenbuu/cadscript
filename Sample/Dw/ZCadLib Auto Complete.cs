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
    public class CadLibAutoCompleteCLS
    {
        static List<int[]> _getAllCellsPosition (PosCollection pls)
        {
            pPos[] cpoints = pls.Select(ls => ls.CenterPoint()).ToArray();

            List<double[]> xys = cpoints.ExtractPtsXY();

            List<int[]> res = new List<int[]>();

            for (int index = 0; index < cpoints.Length; index++)
            {
                int[] tmp = new int[2] { -1, -1 };

                for (int axis = 0; axis < 2; axis++)
                {
                    for (int i = 0; i < xys[axis].Length - 1; i++)
                        if (cpoints[index][axis] >= xys[axis][i]
                            && cpoints[index][axis] < xys[axis][i + 1])
                        {
                            tmp[axis] = i;
                            break;
                        }
                    if (tmp[axis] == -1)
                        tmp[axis] = xys[axis].Length - 1;
                }
                res.Add(tmp);
            }

            return res;
        }

        static string _buildFromArrayObj(ObjectId objId, string[] xnotes)
        {
            PosCollection zones = ACD.DB.ZoneFromIds(new ObjectIdCollection() { objId });
            //ACD.WR("Zones {0}", zones.ToString());
            string cmd = "";

            if (zones.Count > 0)
            {
                List<int[]> zoneIndexes = _getAllCellsPosition(zones);
                //ACD.WR("Zones {0}", zoneIndexes.Select(l => l.ToTextInt()).ToTextStr(";"));

                cmd = xnotes.Where(s => !s.StartsWith("Cell_")).ToTextStr("|");

                foreach (string xnote in xnotes)
                {
                    if(xnote.StartsWith("Cell_"))
                    {
                        int[] vals = xnote._firstPropName().Replace("Cell_", "")
                            .filter("_").Select(s => (int)s.ToNumber()).ToArray();

                        if(vals.Length >= 2)
                        {
                            int[] indexes = DE.NumericArray(0, zones.Count - 1)
                                .Where(i => vals[0] == zoneIndexes[i][0] 
                                && vals[1] == zoneIndexes[i][1]).Select(i => i).ToArray();

                            if(indexes.Length > 0)
                            {
                                //zones[indexes.First()][0].Content = xnote._firstProp();
                                ACD.WR("Command zone {0}", zones[indexes.First()][0]);
                                cmd = cmd._setprop("Command", xnote._firstProp())
                                    ._setprop("Verts", zones[indexes.First()].ToText());
                            }
                        }
                    }
                }
            }

            return cmd;
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                foreach(ObjectId objId in selIds)
                {
                    string[] xnotes = ACD.DB.GetXNotes(objId);

                    if (xnotes.Length > 0)
                    {
                        if (ACD.DB._isArray(objId))
                        {
                            string cmd = _buildFromArrayObj(objId, xnotes);
                            ACD.WR("CMD {0}", cmd);
                        }
                    }
                }
            }

            ACD.Focus();
        }
    }
}

