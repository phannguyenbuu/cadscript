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
    public class BlockRemoveLineCLS
    {
        static string layname;
        static void _init(ObjectId blockId)
        {
            layname = ACD.DB._getLayer(blockId);
            pls = ACD.DB.GetIdPts(blockId);
            T = pls.Select(ls => true).ToArray();
        }

        static void RemoveByPicks(ObjectId blockId)
        {
            List<pPos> picks = new List<pPos>();

            while (true)
            {
                pPos pt = ACD.GetPoint();
                if (pt != null)
                    picks.Add(pt);
                else
                    break;
            }

            if (picks.Count > 0)
            {
                _init(blockId);

                foreach (pPos p in picks)
                    for (int i = 0; i < pls.Count; i++)
                        if (T[i] && pls[i].Length > 0)
                        {
                            PosCollection segs = pls[i].GetSegment(pls.Closed[i]);
                            T[i] = !segs.Any(seg => p.IsBetween(seg[0], seg[1], 4));
                        }
            }
        }

        static void RemoveByLine(ObjectId blockId)
        {
            PosCollection line_picks = new PosCollection();
            ObjectIdCollection lines = new ObjectIdCollection();

            while (true)
            {
                ACD.Get2Points();

                if (ACD.MaxPoint != null && ACD.MinPoint != null)
                {
                    lines.Add(ACD.DB.DrawPolyline(new pPos[] { ACD.FirstPoint, ACD.LastPoint }, false));
                    line_picks.Add(new pPos[] { ACD.FirstPoint, ACD.LastPoint });
                }
                else
                    break;
            }

            ACD.DB.EraseObjects(lines);
            ACD.WR("Lines {0}", line_picks.Count);

            if (line_picks.Count > 0)
            {
                _init(blockId);

                foreach (pPos[] ls in line_picks)
                    for (int i = 0; i < pls.Count; i++)
                        if (T[i] && pls[i].Length > 0)
                        {
                            PosCollection segs = pls[i].GetSegment(pls.Closed[i]);
                            T[i] = !segs.Any(seg => ls[0].Intersect(ls[1], seg[0], seg[1]) != null);
                        }
            }
        }

        static PosCollection pls;
        static bool[] T;

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                bool bmode = ACD.ControlHold;
                Database db = ACD.DB;
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    if (!bmode)
                        RemoveByLine(selIds.First());
                    else
                        RemoveByPicks(selIds.First());

                    ObjectIdCollection blockIds = new ObjectIdCollection();
                    for (int i = 0; i < pls.Count; i++)
                        if (T[i])
                        {
                            blockIds.Add(ACD.DB.DrawPolyline(pls[i], pls.Closed[i], "LAYER=" + layname));
                        }

                    string blockname = ACD.DB.uniqueBlockName("NewBlock");
                    pPos[] bb = ACD.DB._getBound(blockIds);
                    pPos basept = bb.CenterPoint();

                    ACD.DB.NewBlock(blockIds, blockname, true, false, basept);
                    ACD.DB.Insert(blockname, basept + new pPos(5000, 0));
                }
                ACD.Focus();
            }
        }
    }
}

