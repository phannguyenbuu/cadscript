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
using System.Text;
//using SyncObject;

namespace AcadScript
{
    public class TextMergeCLS
    {
        class TextItem
        {
            public ObjectId Id;
            public pPos Point;
            public pPos[] Bound;
            public pPos LocalPoint;
            public pPos[] LocalBound;
            public string Content;
            public double Height;
            public double Rotation;
        }

        static double _readTextHeight(Database db, ObjectId id)
        {
            double res = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (id.ObjectClass.DxfName == "TEXT")
                {
                    DBText txt = (DBText)tr.GetObject(id, OpenMode.ForRead);
                    if (txt != null) res = txt.Height;
                }
                else if (id.ObjectClass.DxfName == "MTEXT")
                {
                    MText txt = (MText)tr.GetObject(id, OpenMode.ForRead);
                    if (txt != null)
                    {
                        res = txt.TextHeight;
                        if (res <= 0) res = txt.Height;
                    }
                }

                tr.Commit();
            }

            return res;
        }

        static double _readTextRotation(Database db, ObjectId id)
        {
            double res = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (id.ObjectClass.DxfName == "TEXT")
                {
                    DBText txt = (DBText)tr.GetObject(id, OpenMode.ForRead);
                    if (txt != null) res = txt.Rotation;
                }
                else if (id.ObjectClass.DxfName == "MTEXT")
                {
                    MText txt = (MText)tr.GetObject(id, OpenMode.ForRead);
                    if (txt != null) res = txt.Rotation;
                }

                tr.Commit();
            }

            if (double.IsNaN(res) || double.IsInfinity(res))
            {
                res = 0;
            }

            return res;
        }

        static double _getCurrentStyleHeight(Database db)
        {
            double res = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (!db.Textstyle.IsNull)
                {
                    TextStyleTableRecord style = (TextStyleTableRecord)tr.GetObject(db.Textstyle, OpenMode.ForRead);
                    if (style != null)
                    {
                        res = style.TextSize;
                    }
                }

                if (res <= 0)
                {
                    res = db.Textsize;
                }

                tr.Commit();
            }

            if (res <= 0 || double.IsNaN(res) || double.IsInfinity(res))
            {
                res = DE.DEF_TEXT_HEIGHT;
            }

            return res;
        }

        static double _median(IEnumerable<double> values)
        {
            double[] arr = values.Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).OrderBy(v => v).ToArray();
            if (arr.Length == 0) return 0;
            int mid = arr.Length / 2;
            if (arr.Length % 2 == 1) return arr[mid];
            return (arr[mid - 1] + arr[mid]) / 2.0;
        }

        static pPos _toLocalPoint(pPos worldPoint, double axisRotationRad, pPos origin)
        {
            if (origin == null)
            {
                origin = new pPos(0, 0);
            }

            if (worldPoint == null)
            {
                return origin;
            }

            double dx = worldPoint.X - origin.X;
            double dy = worldPoint.Y - origin.Y;

            double cos = Math.Cos(axisRotationRad);
            double sin = Math.Sin(axisRotationRad);

            return new pPos(dx * cos + dy * sin, -dx * sin + dy * cos, worldPoint.Z);
        }

        static pPos[] _toLocalBound(pPos[] worldBound, double axisRotationRad, pPos origin)
        {
            if (worldBound == null || worldBound.Length < 2)
            {
                return new pPos[] { origin, origin };
            }

            pPos b0 = worldBound[0];
            pPos b1 = worldBound[1];

            pPos[] corners = new pPos[]
            {
                new pPos(b0.X, b0.Y),
                new pPos(b0.X, b1.Y),
                new pPos(b1.X, b0.Y),
                new pPos(b1.X, b1.Y)
            };

            pPos[] localCorners = corners
                .Select(pt => _toLocalPoint(pt, axisRotationRad, origin))
                .ToArray();

            return new pPos[]
            {
                new pPos(localCorners.Min(pt => pt.X), localCorners.Min(pt => pt.Y)),
                new pPos(localCorners.Max(pt => pt.X), localCorners.Max(pt => pt.Y))
            };
        }

        static pPos[] _getWorkBound(TextItem itm)
        {
            if (itm.LocalBound != null && itm.LocalBound.Length >= 2)
            {
                return itm.LocalBound;
            }

            if (itm.Bound != null && itm.Bound.Length >= 2)
            {
                return itm.Bound;
            }

            pPos pt = itm.Point ?? new pPos(0, 0);
            return new pPos[] { pt, pt };
        }

        static pPos _getWorkPoint(TextItem itm)
        {
            if (itm.LocalPoint != null)
            {
                return itm.LocalPoint;
            }

            return itm.Point ?? new pPos(0, 0);
        }

        static List<TextItem[]> _splitLineByLargeXGap(TextItem[] line, double splitGap = 50.0)
        {
            List<TextItem[]> res = new List<TextItem[]>();
            if (line == null || line.Length == 0)
            {
                return res;
            }

            List<TextItem> current = new List<TextItem> { line[0] };
            for (int i = 1; i < line.Length; i++)
            {
                double dx = _getWorkPoint(line[i]).X - _getWorkPoint(line[i - 1]).X;
                if (dx > splitGap)
                {
                    if (current.Count > 0)
                    {
                        res.Add(current.ToArray());
                    }
                    current = new List<TextItem>();
                }

                current.Add(line[i]);
            }

            if (current.Count > 0)
            {
                res.Add(current.ToArray());
            }

            return res;
        }

        static string _normalizeSpaces(string text)
        {
            if (text.empty()) return "";

            string res = text;
            while (res.Contains("  "))
            {
                res = res.Replace("  ", " ");
            }

            string noSpaceBefore = ",.;:!?)]}";
            foreach (char c in noSpaceBefore)
            {
                res = res.Replace(" " + c, c.ToString());
            }

            return res.Trim();
        }

        static string _mergeSingleLineByX(TextItem[] textData)
        {
            StringBuilder sb = new StringBuilder();
            TextItem prev = null;
            List<double> positiveGaps = new List<double>();

            for (int i = 1; i < textData.Length; i++)
            {
                pPos[] prevBound = _getWorkBound(textData[i - 1]);
                pPos[] curBound = _getWorkBound(textData[i]);

                double prevRight = prevBound[1].X;
                double curLeft = curBound[0].X;
                double gap = curLeft - prevRight;

                if (gap > 0)
                {
                    positiveGaps.Add(gap);
                }
            }

            double medianGap = _median(positiveGaps);

            foreach (TextItem itm in textData)
            {
                string cur = itm.Content == null ? "" : itm.Content.Replace("\r", " ").Replace("\n", " ").Trim();
                if (cur.empty())
                {
                    continue;
                }

                bool appendSpace = false;
                if (prev != null && sb.Length > 0)
                {
                    pPos[] prevBound = _getWorkBound(prev);
                    pPos[] curBound = _getWorkBound(itm);

                    double prevRight = prevBound[1].X;
                    double curLeft = curBound[0].X;
                    double gap = curLeft - prevRight;

                    double prevWidth = Math.Max(0, prevBound[1].X - prevBound[0].X);
                    double curWidth = Math.Max(0, curBound[1].X - curBound[0].X);
                    double prevChar = prevWidth / Math.Max(1, prev.Content == null ? 1 : prev.Content.Trim().Length);
                    double curChar = curWidth / Math.Max(1, cur.Length);

                    double avgHeight = (Math.Max(0.1, prev.Height) + Math.Max(0.1, itm.Height)) / 2.0;
                    double thresholdByChar = Math.Max(prevChar, curChar) * 0.9;
                    double thresholdByHeight = avgHeight * 0.25;
                    double thresholdByMedian = medianGap > 0 ? medianGap * 1.8 : 0;
                    double threshold = Math.Max(thresholdByHeight, Math.Max(thresholdByChar, thresholdByMedian));

                    appendSpace = gap > threshold;

                    if (",.;:!?)]}".Contains(cur[0]))
                    {
                        appendSpace = false;
                    }
                }

                if (appendSpace && sb[sb.Length - 1] != ' ')
                {
                    sb.Append(" ");
                }

                sb.Append(cur);
                prev = itm;
            }

            return _normalizeSpaces(sb.ToString());
        }

        static List<TextItem[]> _splitLinesByY(TextItem[] textData, double yTolerance = 1.0)
        {
            List<List<TextItem>> lines = new List<List<TextItem>>();
            List<double> lineYCenters = new List<double>();

            TextItem[] sortedByY = textData
                .OrderByDescending(itm => itm.LocalPoint.Y)
                .ThenBy(itm => itm.LocalPoint.X)
                .ToArray();

            foreach (TextItem itm in sortedByY)
            {
                int matchedLine = -1;
                double bestDy = double.PositiveInfinity;

                for (int i = 0; i < lineYCenters.Count; i++)
                {
                    double dy = Math.Abs(itm.LocalPoint.Y - lineYCenters[i]);
                    if (dy <= yTolerance && dy < bestDy)
                    {
                        bestDy = dy;
                        matchedLine = i;
                    }
                }

                if (matchedLine == -1)
                {
                    lines.Add(new List<TextItem> { itm });
                    lineYCenters.Add(itm.LocalPoint.Y);
                }
                else
                {
                    lines[matchedLine].Add(itm);
                    int n = lines[matchedLine].Count;
                    lineYCenters[matchedLine] = ((lineYCenters[matchedLine] * (n - 1)) + itm.LocalPoint.Y) / n;
                }
            }

            return lines
                .Select(line => line.OrderBy(itm => itm.LocalPoint.X).ThenBy(itm => itm.LocalPoint.Y).ToArray())
                .OrderByDescending(line => line.Average(itm => itm.LocalPoint.Y))
                .ToList();
        }

        static ObjectId _createMergedDBText(Database db, pPos pt, string content, double height, double rotation, ObjectId sourceId)
        {
            ObjectId newId = ObjectId.Null;

            if (height <= 0 || double.IsNaN(height) || double.IsInfinity(height))
            {
                height = 2.5;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(modelSpaceId, OpenMode.ForWrite);

                DBText txt = new DBText();
                txt.Position = new Point3d(pt.X, pt.Y, 0);
                txt.Height = height;
                txt.TextString = content;
                txt.TextStyleId = db.Textstyle;
                txt.Rotation = (double.IsNaN(rotation) || double.IsInfinity(rotation)) ? 0 : rotation;

                if (!sourceId.IsNull)
                {
                    Entity srcEnt = (Entity)tr.GetObject(sourceId, OpenMode.ForRead);
                    if (srcEnt != null)
                    {
                        txt.Layer = srcEnt.Layer;
                    }

                    if (sourceId.ObjectClass.DxfName == "TEXT")
                    {
                        DBText srcTxt = (DBText)tr.GetObject(sourceId, OpenMode.ForRead);
                        if (srcTxt != null && (double.IsNaN(rotation) || double.IsInfinity(rotation)))
                        {
                            txt.Rotation = srcTxt.Rotation;
                        }
                    }
                    else if (sourceId.ObjectClass.DxfName == "MTEXT")
                    {
                        MText srcTxt = (MText)tr.GetObject(sourceId, OpenMode.ForRead);
                        if (srcTxt != null && (double.IsNaN(rotation) || double.IsInfinity(rotation)))
                        {
                            txt.Rotation = srcTxt.Rotation;
                        }
                    }
                }

                btr.AppendEntity(txt);
                tr.AddNewlyCreatedDBObject(txt, true);
                newId = txt.ObjectId;

                tr.Commit();
            }

            return newId;
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                ObjectIdCollection selIds = ACD.GetSelection();
                ObjectIdCollection textIds = db.FilterIds(selIds, "MTEXT", "TEXT");

                if (textIds.Count == 0)
                {
                    ACD.WR("Please select MTEXT/TEXT objects.");
                    ACD.Focus();
                    return;
                }

                TextItem[] textData = textIds.ToList()
                    .Select(id => new TextItem
                    {
                        Id = id,
                        Point = db._getPoint(id),
                        Bound = db._getBound(id),
                        Content = db._getContent(id),
                        Height = _readTextHeight(db, id),
                        Rotation = _readTextRotation(db, id)
                    }).ToArray();

                foreach (TextItem itm in textData)
                {
                    if (itm.Bound == null || itm.Bound.Length < 2)
                    {
                        itm.Bound = new pPos[] { itm.Point, itm.Point };
                    }
                }

                TextItem axisSample = textData.First();
                double axisRotation = axisSample.Rotation;
                if (double.IsNaN(axisRotation) || double.IsInfinity(axisRotation))
                {
                    axisRotation = 0;
                }

                pPos axisOrigin = axisSample.Point;
                if (axisOrigin == null)
                {
                    axisOrigin = new pPos(0, 0);
                }

                foreach (TextItem itm in textData)
                {
                    itm.LocalPoint = _toLocalPoint(itm.Point, axisRotation, axisOrigin);
                    itm.LocalBound = _toLocalBound(itm.Bound, axisRotation, axisOrigin);
                }

                List<TextItem[]> lines = _splitLinesByY(textData, 1.0);
                List<TextItem[]> mergeGroups = new List<TextItem[]>();
                foreach (TextItem[] line in lines)
                {
                    mergeGroups.AddRange(_splitLineByLargeXGap(line, 50.0));
                }

                List<string> mergedLines = mergeGroups
                    .Select(line => _mergeSingleLineByX(line))
                    .Where(line => !line.empty())
                    .ToList();

                if (mergedLines.Count == 0)
                {
                    ACD.WR("Selected texts have no content.");
                    ACD.Focus();
                    return;
                }

                List<ObjectId> newIds = new List<ObjectId>();
                double currentTextHeight = _getCurrentStyleHeight(db);

                foreach (TextItem[] line in mergeGroups)
                {
                    string lineContent = _mergeSingleLineByX(line);
                    if (lineContent.empty())
                    {
                        continue;
                    }

                    TextItem lineSample = line.OrderBy(itm => itm.LocalPoint.X).First();

                    pPos insertPoint = line
                        .OrderBy(itm => itm.LocalPoint.X)
                        .ThenByDescending(itm => itm.LocalPoint.Y)
                        .First().Point;

                    ObjectId newId = _createMergedDBText(db, insertPoint, lineContent,
                        currentTextHeight, lineSample.Rotation, lineSample.Id);
                    if (!newId.IsNull)
                    {
                        newIds.Add(newId);
                    }
                }

                db.EraseObjects(textIds);
                ACD.WR("Merged {0} text objects into {1} DBTEXT line(s).", textIds.Count, newIds.Count);
                ACD.Focus();
            }
        }
    }
}
