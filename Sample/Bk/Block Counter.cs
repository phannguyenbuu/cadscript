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
    public class BlockCounterCLS
    {
        string[] _collectBlockIndexes(IEnumerable<string> str)
        {
            string[] infors = str.OrderBy(st => st._prop("KEX").Upper())
                .ThenBy(st => st._prop("TITLE").Upper())
                .ThenBy(st => st._prop("VALUE").Upper()).ToArray();

            //ACD.WRArray("INFOR", infors);

            int current_index = 0;
            string current_ids = infors[current_index]._prop("ID");

            for (int i = 1; i < infors.Length; i++)
                if (infors[i]._prop("TITLE").Upper() == infors[current_index]._prop("TITLE").Upper())
                {
                    current_ids += "," + infors[i]._prop("ID");
                    infors[i] = null;
                }
                else
                {
                    infors[current_index] += "|IDS=" + current_ids;
                    current_index = i;
                    current_ids = infors[current_index]._prop("ID");
                }

            if (infors[current_index]._prop("IDS").empty())
                infors[current_index] += "|IDS=" + current_ids;

            infors = infors.Cast<string>().Where(st => !st.empty()).Select(st => st).ToArray();
            return infors;
        }

        static void _drawPointer(pPos start, pPos target)
        {
            List<pPos> pts = new List<pPos> { start };
            pPos[] bb = new pPos[] { start, target }.Boundary();

            if (Math.Abs(target.X - start.X) > Math.Abs(target.X - start.X))
                pts.Add(new pPos(target.X, start.X));
            else
                pts.Add(new pPos(start.X, target.X));

            pts.Add(target);

            pPos p = target.Along(400, pts[pts.Count - 2]);
            pPos[] tmp = p.Parallel(target, 200);

            pts.Add(tmp.First());
            pts.Add(p);

            //ACD.DB.DrawPolyline(pts, false, "LAYER=DEFPOINTS");
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection()._filterDXF("INSERT", "AEC_WINDOW", "AEC_DOOR");
                foreach (ObjectId blockId in selIds)
                {
                    pPos pt = ACD.DB._getPoint(blockId);
                    ObjectId gridId = ACD.DB.GridFromPoint(pt);

                    if (!gridId.IsNull)
                    {
                        string blockname = ACD.DB._getIdName(blockId);

                        ACD.DB.GetEntities(ACD.DB._getBound(gridId),
                            EN_SELECT.AC_DXF_AND_NAME, "INSERT", "AEC_WINDOW", "AEC_DOOR", blockname);

                        pPos[] bb = ACD.DB._getBound(gridId);

                        if (IR.GridFromPoint_Inside)
                            pt.X -= bb[1].X - bb[0].X;

                        foreach (ObjectId id in IR.SelectedIds)
                            _drawPointer(pt, ACD.DB._getBound(id).CenterPoint());

                        ACD.DB.CreateText(String.Format("{0} has {1} items", blockname,
                            IR.SelectedIds.Count), pt + new pPos(500, 0), 200);

                        //IR.GetDWGFromKeXword(DE.CADLIB_ELEMENT, blockname, pt + new pPos(2000, 0));
                        ACD.WR("Blockcounter >> found {0}", blockname);
                    }
                    else
                        ACD.WR("Blockcounter >> no grid found!");
                }

                ACD.Focus();
            }
        }
    }
}

