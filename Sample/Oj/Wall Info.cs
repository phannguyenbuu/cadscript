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

namespace AcadScript
{
    public class WallInfoCLS
    {
        static ObjectIdCollection _showWallText(ObjectIdCollection selIds, double offset)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            PosCollection pls = new PosCollection();
            List<string> lsStyles = new List<string>();
            List<double> lsW = new List<double>();

            foreach (ObjectId wallId in selIds)
                if (ACD.DB._isWall(wallId))
                {
                    //ObjectId lwpId = ;
                    pls.Add(ACD.DB._getVertices(wallId));
                    //ACD.DB.EraseObjects(lwpId);

                    lsStyles.Add(ACD.DB._getIdName(wallId));
                    lsW.Add(ACD.DB._getWallWidth(wallId));
                }

            //ACD.WR("Wall_IDS {0}", pls.Count);

            for (int i = 0; i < pls.Count; i++)
            {
                List<pPos> tmps = new List<pPos>();
                for (int j = 0; j < pls.Count; j++)
                    if (i != j)
                        tmps.AddRange(pls[i].IntersectPts(pls[j], true));

                List<pPos> interps = new List<pPos>();

                foreach (pPos p in tmps)
                    if (interps.All(p1 => p1.X.roundNumber(100) != p.X.roundNumber(100)
                     || p1.Y.roundNumber(100) != p.Y.roundNumber(100)))
                        interps.Add(p);

                interps = interps.OrderBy(p => p.X.roundNumber(10)).ThenBy(p => p.Y.roundNumber(10)).ToList();

                ACD.WR("Wall {0} has {1}", i, interps.Count);

                for (int j = 1; j < interps.Count; j++)
                {
                    if (!interps[j]._isVeryClosed(interps[j - 1], lsW[i] * 2))
                    {
                        double rot = (interps[j] - interps[j - 1]).Angle();
                        pPos ct = interps[j].Parallel(interps[j - 1], lsW[i] * 2).CenterPoint();

                        //ACD.WR("OK1");
                        res.Add(ACD.DB.CreateText("#M" + lsStyles[i], ct, 2, 0, "ANNO=TRUE"));
                        //ACD.WR("OK2");
                        ACD.DB.Rotate(new ObjectIdCollection { res.Last() }, rot, ct);
                    }
                }
            }

            return res;
        }

        static ObjectIdCollection _showWallShape(ObjectIdCollection selIds, double extent)
        {
            Dictionary<string, double> dicts = new Dictionary<string, double>();
            ObjectIdCollection wallIds = new ObjectIdCollection();
            PosCollection pls = new PosCollection();
            PosCollection opening = new PosCollection();

            PosCollection wallExtents = new PosCollection();

            foreach (ObjectId wallId in selIds)
                if (ACD.DB._isWall(wallId))
                {
                    string style = ACD.DB._getIdInfo(wallId);

                    int index = dicts.Keys.ToList().FindIndex(s => s == style);
                    if (index == -1)
                        dicts.Add(style, ACD.DB._getLength(wallId));
                    else
                        dicts[style] += ACD.DB._getLength(wallId);

                    ObjectId lwpId = ACD.DB.GetWallShape(wallId, extent);

                    if (ACD.DB.ValidId(lwpId))
                    {
                        wallIds.Add(lwpId);

                        ObjectId lwpId2 = ACD.DB.GetWallShape(wallId, 200);

                        if (ACD.DB.ValidId(lwpId2))
                        {
                            wallExtents.Add(ACD.DB._getVertices(lwpId2));
                            ACD.DB.EraseObject(lwpId2);
                        }

                        pPos[] wallpts = ACD.DB._getVertices(lwpId).Offset(10);

                        double w = ACD.DB._getWallWidth(wallId);
                        foreach (pPos[] pts in ACD.DB._getWallOpeningPos(wallId))
                        {
                            pPos[] ws = pts[0].Parallel(pts[1], w);
                            if (ws.Any(p => !p.Inside(wallpts)))
                                ws = pts[0].Parallel(pts[1], -w);

                            List<pPos> r = pts.ToList();
                            r.AddRange(ws.Reverse());
                            opening.Add(r.ToArray());
                        }
                    }
                }

            wallExtents.Closed = wallExtents.Select(ls => true).ToArray();
            GraphConsole.Compute(wallExtents);
            //wallIds.AddRange(GraphConsole.ResultPts.Select(ls => ACD.DB.DrawPolyline(ls, true, "LWIDTH=20")).ToCollection());

            ACD.DB._setLayer(wallIds, "A-Wall");

            foreach (pPos[] ls in GraphConsole.ResultPts)
                if (!wallExtents.Any(ws => ls.All(p
                    => p.DistanceToPts(ws) < 1 || p.Inside(ws))) && ls.Area() > 1000000)
                    wallIds.AddRange(ACD.DB.DrawHatch(ls, "LAYER=A-Hatch|HPATTERN=ANSI31|HSCALE=100"));

            foreach (var itm in dicts)
                ACD.WR("Wall styles {0} is {1} m", itm.Key, (itm.Value / 1000).roundNumber(0.001));

            foreach (pPos[] ls in opening)
                wallIds.Add(ACD.DB.DrawPolyline(ls, true, "LAYER=A-Door"));

            return wallIds;
        }




        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                {
                    string input = ACD.ED.GetInputString("Include polylines? (Y/N)");

                    Database db = ACD.DB;

                    double movex = ACD.ED.GetInputString("Move distance? (Y/N)", "25000").ToNumber(25000);
                    pPos mv = new pPos(movex, 0);

                    //pPos ct = ACD.DB._getBound(selIds).CenterPoint();
                    //pPos[] org_region = ACD.DB.GetDrawingZone(ct);

                    //int zone_index = (int)org_region[0].Content.Replace("ZONE","").ToNumber();
                    //ACD.WR("Index:{0}", zone_index);

                    //ACD.WR("Wall 01");
                    //if (zone_index != -1)
                    //{
                    //    pPos[] region = ACD.DB.GetNextDrawingZoneIndex(zone_index);

                    //    if (region != null)
                    //        mv = region[0] - org_region[0];
                    //}
                    //ACD.WR("Wall 02");
                    double extent = ACD.ED.GetInputString("Input extent", "0").ToNumber();

                    ObjectIdCollection ids = new ObjectIdCollection();
                    //ACD.WR("Wall 03");
                    if (input.Upper() == "Y")
                        ids = ACD.DB.CloneObjects(selIds.ToList().Where(id
                            => ACD.DB._isPolyline(id)).Select(id => id).ToCollection());
                    //ACD.WR("Wall 04");
                    ids.AddRange(_showWallText(selIds, extent));
                    ids.AddRange(_showWallShape(selIds, extent));
                    ACD.DB.MoveObject(ids, mv);
                    //ACD.WR("Wall 05");
                }
                ACD.Focus();
            }
        }
    }
}

