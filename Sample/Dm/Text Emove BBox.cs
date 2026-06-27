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
    public class TextEmoveBBoxCLS
    {
        class PolyCandidate
        {
            public ObjectId Id;
            public pPos[] Vertices;
            public pPos[] Bound;
        }

        static bool _isClosed4VertexPolyline(Database db, ObjectId plId)
        {
            if (!db._isPolyline(plId) || !db._isPolylineClosed(plId))
            {
                return false;
            }

            pPos[] pts = db._getVertices(plId, 0, false, false);
            if (pts == null || pts.Length < 4)
            {
                return false;
            }

            List<pPos> corners = pts.ToList().NormalizeList().ToList();
            if (corners.Count > 1 && corners.First().DistanceTo(corners.Last()) <= 1)
            {
                corners.RemoveAt(corners.Count - 1);
            }

            return corners.Count == 4;
        }

        static bool _ratioInRange(double ratio, double minRatio, double maxRatio)
        {
            return (ratio >= minRatio && ratio <= maxRatio)
                || (ratio > 1e-9 && (1.0 / ratio) >= minRatio && (1.0 / ratio) <= maxRatio);
        }

        static bool _isBBoxRatioMatch(pPos[] textBb, pPos[] boxBb,
            double minWidthRatio = 0.6, double maxWidthRatio = 1.2,
            double minHeightRatio = 0.6, double maxHeightRatio = 1.2)
        {
            pPos textSize = textBb.Size();
            pPos boxSize = boxBb.Size();

            if (textSize.X <= 1e-6 || textSize.Y <= 1e-6 || boxSize.X <= 1e-6 || boxSize.Y <= 1e-6)
            {
                return false;
            }

            double rw = boxSize.X / textSize.X;
            double rh = boxSize.Y / textSize.Y;

            return _ratioInRange(rw, minWidthRatio, maxWidthRatio)
                && _ratioInRange(rh, minHeightRatio, maxHeightRatio);
        }

        static bool _isApproxTextBBox(pPos[] textBb, pPos[] boxBb,
            double minWidthRatio = 0.35, double maxWidthRatio = 6.0,
            double minHeightRatio = 0.4, double maxHeightRatio = 3.0,
            double maxCenterShiftFactor = 1.2)
        {
            if (!_isBBoxRatioMatch(textBb, boxBb,
                minWidthRatio, maxWidthRatio, minHeightRatio, maxHeightRatio))
            {
                return false;
            }

            pPos textCenter = textBb.CenterPoint();
            pPos boxCenter = boxBb.CenterPoint();
            pPos boxSize = boxBb.Size();

            double allowDx = Math.Max(1.0, boxSize.X * maxCenterShiftFactor);
            double allowDy = Math.Max(1.0, boxSize.Y * maxCenterShiftFactor);

            return Math.Abs(textCenter.X - boxCenter.X) <= allowDx
                && Math.Abs(textCenter.Y - boxCenter.Y) <= allowDy;
        }

        static bool _isTinyBlockByName(Database db, ObjectId id,
            string prefix = "BLOCK_", double maxWidth = 20, double maxHeight = 20)
        {
            if (!db._isBlock(id))
            {
                return false;
            }

            string name = db._getIdName(id, true);
            if (name.empty() || !name.StartsWith(prefix.Upper()))
            {
                return false;
            }

            pPos[] bb = db._getBound(id);
            if (bb == null || bb.Length < 2)
            {
                return false;
            }

            pPos sz = bb.Size();
            return sz.X > 0 && sz.Y > 0 && sz.X < maxWidth && sz.Y < maxHeight;
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count == 0)
                {
                    ACD.Focus();
                    return;
                }

                ObjectIdCollection textIds = selIds.FilterIds("MTEXT", "TEXT", "ATTRIB", "ATTDEF");
                ObjectIdCollection polyIds = selIds.ToList().Where(id => db._isPolyline(id)).ToCollection();
                ObjectIdCollection tinyBlockIds = selIds.ToList()
                    .Where(id => _isTinyBlockByName(db, id, "BLOCK_", 20, 20))
                    .ToCollection();

                bool canProcessTextBBox = textIds.Count > 0 && polyIds.Count > 0;
                if (!canProcessTextBBox && tinyBlockIds.Count == 0)
                {
                    ACD.WR("Need TEXT/MTEXT + closed 4-vertex polylines, or tiny Block_* in selection.");
                    ACD.Focus();
                    return;
                }

                ObjectIdCollection quadIds = new ObjectIdCollection();
                List<PolyCandidate> quadCandidates = new List<PolyCandidate>();

                if (canProcessTextBBox)
                {
                    quadIds = polyIds.ToList()
                        .Where(id => _isClosed4VertexPolyline(db, id))
                        .ToCollection();

                    quadCandidates = quadIds.ToList()
                        .Select(id => new PolyCandidate
                        {
                            Id = id,
                            Vertices = db._getVertices(id, 0, false, false),
                            Bound = db._getBound(id)
                        })
                        .Where(itm => itm.Bound != null && itm.Bound.Length >= 2)
                        .ToList();
                }

                ACD.WR("Text Emove BBox - texts:{0}, polylines:{1}, closed4:{2}, tinyBlock:{3}",
                    textIds.Count, polyIds.Count, quadCandidates.Count, tinyBlockIds.Count);

                HashSet<ObjectId> eraseSet = new HashSet<ObjectId>();

                if (canProcessTextBBox)
                {
                    foreach (ObjectId txtId in textIds)
                    {
                        pPos[] textBb = db._getBound(txtId);
                        if (textBb == null || textBb.Length < 2)
                        {
                            continue;
                        }

                        pPos txtCenter = textBb.CenterPoint();

                        foreach (PolyCandidate box in quadCandidates)
                        {
                            if (eraseSet.Contains(box.Id))
                            {
                                continue;
                            }

                            pPos[] boxBb = box.Bound;

                            bool bboxOverlap = txtCenter.InsideRect(boxBb[0], boxBb[1])
                                || textBb.IntersectBounding(boxBb, true);

                            if (!bboxOverlap && box.Vertices != null && box.Vertices.Length >= 3)
                            {
                                bboxOverlap = txtCenter.Inside(box.Vertices);
                            }

                            if (!bboxOverlap)
                            {
                                continue;
                            }

                            if (_isApproxTextBBox(textBb, boxBb))
                            {
                                eraseSet.Add(box.Id);
                            }
                        }
                    }
                }

                foreach (ObjectId bId in tinyBlockIds)
                {
                    eraseSet.Add(bId);
                }

                if (eraseSet.Count > 0)
                {
                    db.EraseObjects(eraseSet.ToList().ToCollection());
                }

                ACD.WR("Removed {0} object(s): {1} text-bbox polyline(s), {2} tiny Block_*.",
                    eraseSet.Count, eraseSet.Count - tinyBlockIds.Count, tinyBlockIds.Count);
                ACD.Focus();
            }
        }
    }
}
