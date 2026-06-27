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
    public class StructInforCLS
    {
        public PosCollection foundationList, beamList, steelSupportList;
        public string[] foundationNamings, beamNamings;
        pPos baseMTextPT;
        ObjectIdCollection srcIds;

        string[] _orderAndNamingFoundation(PosCollection pls)
        {
            string[] namelist = pls.Select(ls => ls[0].Content._firstProp().Upper()).Distinct().ToArray();

            return pls.Select(ls => "M" + (Array.IndexOf(namelist, ls[0].Content._firstProp().Upper()) + 1)
                + "(" + ls[1].RectToPoint(ls[2]).Size()[0].roundNumber(50) 
                + "x" + ls[1].RectToPoint(ls[2]).Size()[1].roundNumber(50) + ")").ToArray();
        }

        string _beamKeyOrder(pPos[] ls)
        {
            string axis = ls[0].Rotation == 0 ? "X" : "Y";
            pPos pt = ls.CenterPoint().Round(200);
            double v = ls[0].Rotation == 0 ? pt.X : pt.Y;

            return axis 
                + ls[0].Content._firstProp().Upper()
                + "[" + ls.MaxSegment(true).Length(false).roundNumber(100) 
                + "," +  v + "]";
        }

        string[] _orderAndNamingBeam(PosCollection pls)
        {
            string[] namelistY = pls.Where(ls => ls[0].Rotation == 90).Select(ls 
                => _beamKeyOrder(ls)).Distinct().ToArray();
            string[] namelistX = pls.Where(ls => ls[0].Rotation != 90).Select(ls 
                => _beamKeyOrder(ls)).Distinct().ToArray();

            return pls.Select(ls => "D" + (ls[0].Rotation == 0 ? "X" : "Y")
                + (Array.IndexOf(ls[0].Rotation == 90 ? namelistY: namelistX, _beamKeyOrder(ls)) + 1)
                + "(" + ls[0].Content._firstProp().Upper().Replace("D","") + ")").ToArray();
        }

        public StructInforCLS(ObjectIdCollection ids)
        {
            srcIds = ids;

            ObjectIdCollection txtIds = ids.ToList().Where(id => ACD.DB._isText(id) 
                && ACD.DB._getContent(id).StartsWith(".")).ToCollection();
            baseMTextPT = txtIds.Count > 0 ? ACD.DB._getPoint(txtIds[0]) : ACD.DB._getBound(ids)[0];

            foundationList = new PosCollection();
            beamList = new PosCollection();
            steelSupportList = new PosCollection();
            //ACD.WR("OK1");
            foreach (ObjectId id in ids)
            {
                pPos[] bb = ACD.DB._getBound(id);
                if (ACD.DB._isBlock(id))
                {
                    string stname = ACD.DB._getIdName(id).Upper();
                    pPos pt = ACD.DB._getPoint(id);
                    
                    if (stname.Contains("FOUNDATION") || stname.Contains("MONG"))
                    {
                        pPos[] pts = new pPos[] { pt, bb[0], bb[1]};
                        pts[0].Content = id.Handle.Value + "=" + stname;
                        foundationList.Add(pts);
                    }
                }else if(ACD.DB._isWall(id))
                {
                    string stname = ACD.DB._getIdName(id);
                    ObjectId wallId =  ACD.DB.GetWallShape(id);

                    pPos[] pts = ACD.DB._getVertices(wallId);
                    pPos[] seg = pts.MaxSegment();

                    pts[0].Rotation = Math.Abs(seg[1].X - seg[0].X) > Math.Abs(seg[1].Y - seg[0].Y) ? 0 : 90;

                    pts[0].Content = id.Handle.Value + "=" + stname;
                    ACD.DB.EraseObject(wallId);
                    beamList.Add(pts);
                }else if(ACD.DB._isPolyline(id) && !ACD.DB._isPolylineClosed(id) && ACD.DB._getLineweight(id) > 5)
                {
                    pPos[] pts = ACD.DB._getVertices(id);

                    int[] indexes = DE.NumericArray(0, steelSupportList.Count - 1).Where(i
                         => steelSupportList[i].CenterPoint()._isVeryClosed(pts.CenterPoint(), 200)).ToArray();

                    if (indexes.Length == 0)
                    {
                        pts[0].Content = "1x%%C" + ACD.DB._getLineweight(id);
                        steelSupportList.Add(pts);
                    }
                    else
                        foreach (int index in indexes)
                        {
                            string[] ar = steelSupportList[index][0].Content.filter("x");
                            steelSupportList[index][0].Content = (ar[0].ToNumber() + 1) + "x" + ar[1];
                        }
                }
            }
            //ACD.WR("OK2");
            foundationList = foundationList.OrderBy(ls => ls[0].Content).ThenBy(ls
                => (ls[1].X - ls[0].X).roundNumber(50)).ThenBy(ls => ls[1].Y - ls[0].Y).ToCollectionSameClosed();
            foundationNamings = _orderAndNamingFoundation(foundationList);
            //ACD.WR("OK3");
            beamList = beamList.OrderBy(ls => ls[0].Content._firstProp())
                .ThenBy(ls => ls[0].Rotation)
                .ThenBy(ls => -ls.MaxSegment(true).Length(false)).ToCollectionSameClosed();
            beamNamings = _orderAndNamingBeam(beamList);
            //ACD.WR("OK4");
            steelSupportList = steelSupportList.OrderBy(ls => ls[0].Content).ToCollectionSameClosed();
        }

        

        void _saveOldObjectLegendsData()
        {
            ObjectIdCollection oldIds = srcIds.ToList().Where(id => !ACD.DB.GetXNotes(id).empty()
                && ACD.DB.GetXNotes(id)._firstPropName() == "StructLegend").ToCollection();

            //ACD.WR("Old {0}", oldIds.Count);
            Dictionary<ObjectId, string> dict = new Dictionary<ObjectId, string>();

            foreach (ObjectId id in oldIds)
            {
                string note = ACD.DB.GetXNotes(id);

                if (!note._firstProp().empty())
                {
                    //ACD.WR("Value {0}:{1}", note._firstProp(), ACD.DB.strToObjectId(note._firstProp()).Count);

                    //if(ACD.DB.strToObjectId(note._firstProp()).Count > 0)
                    //    ACD.WR("{0},{1}", 
                    //    ACD.DB.strToObjectId(note._firstProp()).First().ObjectClass.DxfName, 
                    //    ACD.DB._getIdName(ACD.DB.strToObjectId(note._firstProp()).First()));

                    foreach (ObjectId nId in ACD.DB.strToObjectId(note._firstProp()))
                    {
                        pPos pt = ACD.DB._getPoint(id).Round(50);
                        pt.Content = "";
                        string new_note =
                            //ACD.DB.GetXNotes(nId).empty() ?
                            //    "Position=" + ACD.DB._getPoint(id).ToString()
                            //    + "|LegendRotation=" + ACD.DB._getRotation(id).ToString()
                            ACD.DB.GetXNotes(nId)
                            ._setprop("Position", pt.ToString())
                            ._setprop("Rotation", ACD.DB._getRotation(id).ToString());

                        //ACD.WR("New note:{0} ------ {1}", ACD.DB._getIdName(nId), new_note);
                        if(ACD.DB.ValidId(nId) && !new_note.empty())
                            ACD.DB.SetNoteXData(nId, new_note);
                        dict.Add(nId, new_note);
                    }
                }
            }

            ACD.DB.EraseObjects(oldIds);
        }

        void _loadOldObjectLegendsData(ObjectId txtId, string id_handle)
        {
            ObjectIdCollection handleIds = ACD.DB.strToObjectId(id_handle);

            foreach(ObjectId id in handleIds)
            {
                string note = ACD.DB.GetXNotes(id);
                if (!note._prop("Position").empty())
                    ACD.DB._setPoint(txtId, pPos.FromString(note._prop("Position")));
                if (!note._prop("Rotation").empty())
                    ACD.DB._setRotation(txtId, note._prop("Rotation").ToNumber());
            }
        }

        public string[] DataList()
        {
            List<string> res = new List<string>();
            List<string> savelist = new List<string>();

            for (int i = 0; i < foundationNamings.Length; i++)
                if(!savelist.Contains(foundationNamings[i]))
                {
                    pPos p1 = foundationList[i][0], p2 = foundationList[i][1];

                    int[] indexes = DE.NumericArray(0, foundationNamings.Length - 1)
                        .Where(n => foundationNamings[n] == foundationNamings[i]).ToArray();

                    res.Add(String.Format("Name={0}|Width={1}|Length={2}|Count={3}|Ids={4}",
                        foundationNamings[i], Math.Abs(p1.X - p2.X).roundNumber(),
                        Math.Abs(p1.Y - p2.Y).roundNumber(),
                        indexes.Length, 
                        indexes.Select(n => foundationList[n][0].Content._firstPropName()).ToTextStr()));
                    savelist.Add(foundationNamings[i]);
                }

            savelist = new List<string>();

            for (int i = 0; i < beamNamings.Length; i++)
                if (!savelist.Contains(beamNamings[i]))
                {
                    pPos[] ls = beamList[i].MaxSegment();
                    res.Add(String.Format("Name={0}|Axis={1}|Length={2}|Count={3}|Pos={4}", 
                        beamNamings[i], beamList[i][0].Rotation == 0 ? "X":"Y", ls.Length(false).roundNumber(50),
                        beamNamings.Count(s => s == beamNamings[i]),
                        beamList[i][0].Rotation == 0 ? ls.CenterPoint().X.roundNumber(50) 
                        : ls.CenterPoint().Y.roundNumber(50)));
                    savelist.Add(beamNamings[i]);
                }

            foreach (pPos[] pts in steelSupportList)
            {
                res.Add(String.Format("Name={0}|Length={1}", pts[0].Content, pts.Length(false).roundNumber()));
            }

            return res.ToArray();
        }

        public void ShowLegend()
        {
            //ACD.WR("OK1");
            _saveOldObjectLegendsData();
            //ACD.WR("OK2");
            ObjectIdCollection ids = new ObjectIdCollection();

            for (int i = 0; i < foundationList.Count; i++)
            {
                string id_handle = foundationList[i][0].Content._firstPropName();
                string content = foundationNamings[i];
                //ACD.WR("Size M:{0}", content);
                ObjectId txtId = ACD.DB.CreateText(content, foundationList[i][1] - new pPos(0, 300), 1.5);
                _loadOldObjectLegendsData(txtId, id_handle);
                ACD.DB.SetNoteXData(txtId, "StructLegend=" + id_handle);
                ids.Add(txtId);
            }
            //ACD.WR("OK3");
            PosCollection segList = beamList.Select(ls => ls.MaxSegment()).ToCollectionSameClosed();
            //ACD.WR("OK4");
            for (int i = 0; i < beamList.Count; i++)
            {
                pPos[] pts = segList.SliceIntersect(segList[i][0], segList[i][1]).OrderBy(p 
                    => p.X.roundNumber(50)).ThenBy(p => p.Y).Select(p 
                    => p.ToString()).Distinct().Select(s => pPos.FromString(s)).ToArray();

                for (int j = 0; j < pts.Length - 1; j++)
                {
                    string id_handle = beamList[i][0].Content._firstPropName();
                    string content = "#C" + beamNamings[i];

                    double rot = beamList[i][0].Rotation;
                    pPos txt_pt = (pts[j] + pts[j + 1])/2 - 300 * new pPos(rot == 0 ? 0 : 1, rot == 0 ? 1: 0);
                    ObjectId txtId = ObjectId.Null;

                    if (j == (pts.Length - 1) / 2)
                    {
                        txtId = ACD.DB.CreateText(content, txt_pt, 2);
                        _loadOldObjectLegendsData(txtId, id_handle);
                        ACD.DB.SetNoteXData(txtId, "StructLegend=" + id_handle);
                    }
                    else
                    {
                        txtId = ACD.DB.CreateText(content._getInComma(), txt_pt, 1.5);
                        ACD.DB.SetNoteXData(txtId, "StructLegend=");
                    }

                    ACD.DB.Rotate(new ObjectIdCollection() { txtId }, rot, ACD.DB._getPoint(txtId));
                    ids.Add(txtId);
                }
            }
            //ACD.WR("OK5");
            foreach (pPos[] pts in steelSupportList)
            {
                ObjectIdCollection leaderIds = ACD.DB.CreateMLeader(pts[0].Content, pts.CenterPoint(),
                    new pPos(baseMTextPT.X, pts.CenterPoint().Y + 1000));
                foreach (ObjectId id in leaderIds)
                    ACD.DB.SetNoteXData(id, "StructLegend=");
                ids.AddRange(leaderIds);
            }
        }

        public override string ToString()
        {
            return String.Format("Foundation:{0}, Beam:{1}, Steel:{2}", 
                foundationList.Count, beamList.Count, steelSupportList.Count);
        }
    }

    public class BeamFrPlanCLS
    {
        static double H = 1000, SP = 5000;

        static void _drawOnPlans(ObjectIdCollection selIds)
        {
            //ObjectIdCollection selIds = ACD.GetSelection();

            if (selIds.Count > 0 && selIds.ToList().Any(id => ACD.DB._isBlock(id)
                     && ACD.DB._getIdName(id).Upper().StartsWith("GRID"))
                    && selIds.ToList().Any(id => ACD.DB._isWall(id)))
            {
                ObjectId gridId = selIds.ToList().First(id => ACD.DB._isBlock(id)
                    && ACD.DB._getIdName(id).Upper().StartsWith("GRID"));

                ObjectIdCollection wallIds = selIds.ToList().Where(id
                    => ACD.DB._isWall(id) && ACD.DB._getVertices(id).Length == 2).Select(id => id)
                    .OrderBy(id => ACD.DB._getVertices(id)[0].AngleAxisVsVector(ACD.DB._getVertices(id)[1]))
                    .ToCollection();

                ObjectIdCollection nameIds = selIds.ToList().Where(id
                    => ACD.DB._isText(id)
                    && ACD.DB._getContent(id).Upper().StartsWith("D")).Select(id => id).ToCollection();

                ObjectIdCollection sizeIds = selIds.ToList().Where(id
                    => ACD.DB._isText(id)
                    && !ACD.DB._getContent(id).Upper().StartsWith("D")).Select(id => id).ToCollection();

                string default_val = "20x40";

                if (sizeIds.Count > 0)
                    default_val = ACD.DB._getContent(sizeIds.First());

                string st = ACD.ED.GetInputString("Enter size of beam", default_val);

                foreach (ObjectId txtId in sizeIds)
                    ACD.DB._setContent(txtId, st);

                string[] ar = st.filter("x");

                if (ar.Length > 1)
                {
                    double w = ar[0].ToNumber(20) * 10;
                    double h = ar[1].ToNumber(40) * 10;


                    ObjectIdCollection gridIds = ACD.DB._getAllGridsByIds(selIds);

                    PosCollection pls = ACD.DB._getAllVertices(gridIds);

                    pPos base_pt = ACD.GetPoint();

                    if (base_pt != null)
                    {
                        pPos[] bb = pls.Boundary;

                        double ny = 0;

                        ACD.DB.SetAnnotationScale("1:25");

                        int current_axis = -1;
                        List<string> history_list = new List<string>();

                        foreach (ObjectId wallId in wallIds)
                        {
                            pPos pt = base_pt.Clone();
                            pPos[] pts = ACD.DB._getVertices(wallId).OrderBy(p => p.X).ThenBy(p => p.Y).ToArray();

                            ObjectIdCollection selnameIds = nameIds.ToList().Where(id
                               => ACD.DB._getPoint(id).DistanceTo(pts[0], pts[1]) <= 600).ToCollection();

                            if (selnameIds.Count > 0)
                            {
                                string stname = ACD.DB._getContent(selnameIds.First()).Upper();

                                if (!history_list.Contains(stname))
                                {
                                    int total = nameIds.ToList().Count(id => ACD.DB._getContent(id).Upper() == stname);

                                    int n = pts[0].AngleAxisVsVector(pts[1]);

                                    if (n != current_axis)
                                    {
                                        current_axis = n;
                                        //ACD.DB.DrawPolyline(new pPos[] { new pPos(pt.X, pt.Y + H / 2), 
                                        //new pPos(pt.X + 5000, pt.Y + H / 2)}, false, "LWIDTH=50|LCOLOR=4");
                                        //pt.Y += 4 * H;
                                    }

                                    pPos[] r = null;

                                    double l = n == 0 ? pts[1].X - pts[0].X : pts[1].Y - pts[0].Y;
                                    r = n == 0 ? (pt + new pPos(0, ny)).RectToPoint(pt + new pPos(pts[1].X - pts[0].X, ny - h))
                                        : (pt + new pPos(0, ny)).RectToPoint(pt + new pPos(pts[1].Y - pts[0].Y, ny - h));

                                    if (r.Boundary()[0].X < pt.X - 10)
                                    {
                                        double mv = r.Boundary().Size().X;
                                        pt.X += mv;
                                        r = r.MoveByAxis(mv, 0);
                                    }

                                    ACD.DB.DrawPolyline(r, true);

                                    foreach (pPos[] ls in pls)
                                        if (ls[0].AngleAxisVsVector(ls[1]) != n)
                                        {
                                            double v = n == 0 ? pt.X + ls[0].X - pts[0].X : pt.X + ls[0].Y - pts[0].Y;
                                            if (r.Boundary()[0].X <= v + 10 && v <= r.Boundary()[1].X + 10)
                                            {
                                                if (n == 0)
                                                    ACD.DB.DrawPolyline(new pPos[] { pt + new pPos(ls[0].X - pts[0].X, ny - H),
                                                            pt + new pPos(ls[0].X - pts[0].X, ny + H) }, false, "LAYER=A-Hidden");
                                                else
                                                    ACD.DB.DrawPolyline(new pPos[] { pt + new pPos(ls[0].Y - pts[0].Y, ny - h - H),
                                                            pt + new pPos(ls[0].Y - pts[0].Y, ny + H) }, false, "LAYER=A-Hidden");
                                            }
                                        }

                                    ACD.DB.CreateText(stname + "\r\nL=" + Math.Abs(l).roundNumber(50) + " SL:" + total,
                                        base_pt + new pPos(0, ny + H), 8, 0, selnameIds.First());
                                    history_list.Add(stname);

                                    ny += H * 2 + h + SP;
                                }
                            }
                        }
                    }
                }
            }
        }

        static void _cloneObjToNewZone(ObjectId id, pPos[] zone)
        {
            ObjectId newId = ACD.DB.CloneObject(id);
            pPos[] bb = zone.Boundary();
            pPos[] obj_bb = ACD.DB._getBound(id);

            ACD.DB.MoveObject(newId, new pPos(bb[0].X, bb[0].Y) - obj_bb[0]);

        }

        static void _drawBeamInNewZone(string content, pPos[] zone, int beam_index)
        {
            string sname = content._prop("Name");
            double width = sname._getInComma().Upper().filter("X").First().ToNumber() * 10;
            double height = sname._getInComma().Upper().filter("X").Last().ToNumber() * 10;
            double length = content._prop("Length").ToNumber();

            ACD.WR("Size: {0},{1},{2}", length, width, height);

            pPos[] bb = zone.Boundary();
            pPos sz = zone.Size();
            pPos p = bb[0] + new pPos((sz.X - length) / 2, bb[0].Y + sz.Y / 4 + beam_index * sz.Y / 2);

            ACD.DB.DrawPolyline(p.RectToPoint(new pPos(p.X + length, p.Y - height)));
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                int mode = (int)ACD.ED.GetInputString("Select mode: 0. Legends from plans, 1. Object details from data, 2. Schedule from data").ToNumber();

                if (mode == 0)
                {
                    pPos pt = ACD.GetPoint();

                    if (pt != null)
                    {
                        pPos[] zone = ACD.DB.GetDrawingZone(pt);

                        if (zone != null)
                        {
                            ACD.DB.GetEntities(zone, EN_SELECT.AC_ALL);

                            StructInforCLS action = new StructInforCLS(IR.SelectedIds);
                            //ACD.WR(action.ToString());
                            action.ShowLegend();
                            string[] data = action.DataList();

                            ObjectId infoTextId = ObjectId.Null;
                            ObjectIdCollection txtIds = IR.SelectedIds.ToList().Where(id
                                => ACD.DB._isText(id) && ACD.DB.GetXNotes(id) == "StructData=").ToCollection();
                            if (txtIds.Count > 0)
                                infoTextId = txtIds.First();

                            else
                            {
                                infoTextId = ACD.DB.CreateText("", zone[0], 1);
                                ACD.DB.SetNoteXData(infoTextId, "StructData=");
                            }

                            ACD.DB._setContent(infoTextId, data.ToTextStr("\r\n"));
                            ACD.DB._setLayer(infoTextId, "Defpoints");
                        }
                    }
                }
                else if(mode == 1)
                {
                    ACD.WR("Select content:");
                    ObjectIdCollection txtIds = ACD.GetSelection().ToList().Where(id 
                        => ACD.DB._isText(id)).ToCollection();
                    
                    if(txtIds.Count > 0)
                    {
                        ACD.WR("Select zone:");
                        ObjectIdCollection arIds = ACD.GetSelection();

                        if (arIds.Count > 0 && arIds.ToList().Any(id => ACD.DB._isArray(id)))
                        {
                            PosCollection zones = ACD.DB.SetDrawingZonesByIds(arIds.ToList().Where(id 
                                => ACD.DB._isArray(id)).ToCollection()).OrderBy(ls
                                => ls[0].X.roundNumber(50)).ThenBy(ls => ls[0].Y.roundNumber(50)).ToCollectionSameClosed();
                            int current_zone = 0;
                            string content = ACD.DB._getContent(txtIds.First());

                            int beam_view_index = 0;

                            foreach(string s in content.filter("\r\n"))
                            {
                                string name = s._prop("Name");
                                if(name.StartsWith("M")) //FOUNDATION
                                {
                                    ObjectIdCollection ids = ACD.DB.strToObjectId(s._prop("Ids"));
                                    if (ids.Count > 0)
                                        _cloneObjToNewZone(ids.First(), zones[current_zone]);

                                    current_zone++;
                                }
                                else if (name.StartsWith("D")) //BEAM
                                {
                                    ACD.WR("D1: {0}", s);
                                    _drawBeamInNewZone(s, zones[current_zone], beam_view_index);
                                    ACD.WR("D2");
                                    beam_view_index++;

                                    if(beam_view_index >= 1)
                                        current_zone++;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

