using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace AcadScript
{
    
    public class WallExportCLS
    {
        static ObjectIdCollection resultIds = new ObjectIdCollection();
        static PosCollection wall_pls, door_pls, window_pls;

        static PosCollection _getWallRectChains(pPos p1, pPos p2, double w, PosCollection opnPls)
        {
            PosCollection pls = new PosCollection();
            PosCollection res = new PosCollection();

            pPos[] line1 = p1.Parallel(p2, w);

            if (opnPls.Count > 0)
            {
                pls.Add(new pPos[] { p1, opnPls[0][0] });

                for (int i = 0; i < opnPls.Count; i++)
                {
                    pls.Add(opnPls[i]);
                    pls.Add(new pPos[] { opnPls[i][1], i == opnPls.Count - 1 ? p2 : opnPls[i + 1][0] });
                }

                //pls.Add(new pPos[] { opnPls.Last()[1], p2 });

                res = pls.Select(_l => new pPos[] {_l[0].Project2D(p1, p2),
                    _l[1].Project2D(p1, p2), _l[1].Project2D(line1[0], line1[1]),
                    _l[0].Project2D(line1[0], line1[1])}).ToCollectionSameClosed(true);

                res.Closed = res.Select(_l => true).ToArray();
            }
            else
            {
                res = new PosCollection() { new pPos[] { p1, p2, line1[1], line1[0] } };
                res.Closed = new bool[] { true };
            }

            return res;
        }

        static PosCollection _getWallIShape(ObjectIdCollection subIds, pPos p1, pPos p2, double w, PosCollection opnPls)
        {
            PosCollection rects = new PosCollection();

            ObjectIdCollection hIds = subIds.FilterIds("HATCH");
            //ACD.WR("S{0} H {1}", subIds.Count, hIds.Count);

            if (hIds.Count > 0)
            {
                PosCollection h_pls = new PosCollection();

                foreach (ObjectId _hid in hIds)
                    h_pls.AddRange(ACD.DB._getHatch(_hid));

                h_pls.Closed = h_pls.Select(_l => true).ToArray();

                rects = _getWallRectChains(p1, p2, w, opnPls);

                //ACD.DB.DrawPolyline(rects);
                //ACD.DB.DrawPolyline(h_pls);

                if (!h_pls.AllPoints.Any(_p => rects.Any(_l => _p.Inside(_l))))
                {
                    rects = _getWallRectChains(p1, p2, -w, opnPls);

                    if (!h_pls.AllPoints.Any(_p => rects.Any(_l => _p.Inside(_l))))
                        rects = new PosCollection();
                }

            }

            ACD.DB.EraseObjects(subIds);

            return rects;
        }

        //static pPos _getWFromStyle(string style)
        //{
        //    double w = 0;
        //    double h = 3000;

        //    if (style.ct_("100"))
        //        w = 100;
        //    else if (style.ct_("150"))
        //        w = 150;
        //    else if (style.ct_("200"))
        //        w = 200;
        //    else if (style.ct_("250"))
        //        w = 250;
        //    else if (style.ct_("300"))
        //        w = 300;
        //    else if (style.ct_("Fence"))
        //    {
        //        w = 0.05;
        //        h = 1;
        //    }

        //    return new pPos(w, h);
        //}

        static string _getWallOpeningInfo(ObjectId entId)
        {
            Database db = ACD.DB;
            string content = "";

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (entId.ObjectClass.DxfName == "AEC_WALL")
                {
                    Autodesk.Aec.Arch.DatabaseServices.Wall wall
                        = (Autodesk.Aec.Arch.DatabaseServices.Wall)tr.GetObject(entId, OpenMode.ForRead);

                    if (wall != null)
                    {
                        string style = ACD.DB._getIdName(entId);

                        ObjectIdCollection srcIds = wall.GetOpeningsFor();
                        PosCollection opnPls = new PosCollection();

                        List<pPos> cts = new List<pPos>();

                        foreach (ObjectId id in srcIds)
                        {
                            pPos[] _l = db._getVertices(id).OrderBy(_p => _p.DistanceTo(wall.StartPoint.ToPos())).ToArray();

                            opnPls.Add(_l);
                            cts.Add(_l.CenterPoint());
                            cts.Last().Content = db._getIdName(id);
                        }

                        opnPls = opnPls.OrderBy(_l => _l.CenterPoint().DistanceTo(wall.StartPoint.ToPos())).ToCollectionSameClosed(false);

                        //pPos sz = wall.Width;

                        PosCollection rects = _getWallIShape(ACD.DB.ExplodeAEC(new ObjectIdCollection { entId }, false),
                            wall.StartPoint.ToPos(), wall.EndPoint.ToPos(), wall.Width, opnPls); // _getWFromStyle(style)

                        foreach (pPos[] r in rects)
                        {
                            pPos[] _cts = cts.Where(_p => _p.Inside(r)).ToArray();

                            if (_cts.Length > 0)
                            {
                                string _style = _cts[0].Content;

                                if (!_style.ct_("Opening"))
                                {
                                    //content += "\t" + __comment("Opening=" + _style);
                                    //content += "\t" + __transStrList(r.Select(_p => _p * cReadData.__sc),
                                    //    " style = \"fill:" + (_style.st_("d") ? "green" : "blue") + ";fill-opacity:0.5\"");
                                    

                                    door_pls.Add(r);

                                    
                                }
                            }
                            else
                            {
                                wall_pls.Add(r);
                                
                            }
                            //content += "\t" + __transStrList(r.Select(_p => _p * cReadData.__sc),
                            //        " style = \"fill:red;fill-opacity:0.5\"");
                        }
                    }
                }

                tr.Commit();
            }

            return content;
        }

        static void _checkIntersectJointInWallPoints()
        {
            var itm = wall_pls.PosIndexes(10);
            var verts = itm.Verts;
            var indexes = itm.Indexes;

            for(int i = 0; i< indexes.Count - 1; i ++)
                for (int j = i + 1; j < indexes.Count; j++)
                {
                    int[] __intpts = indexes[i].Intersect(indexes[j]).ToArray();

                    if (__intpts.Length == 1)
                    {
                        int k = __intpts.First();
                        pPos p = verts[k];

                        List<int> ls = new List<int>();

                        foreach (int __n in indexes[i])
                            if (__n != k)
                                ls.Add(__n);

                        foreach (int __n in indexes[j])
                            if (__n != k)
                                ls.Add(__n);

                        List<pPos> __pts = ls.Select(__n => verts[__n]).Where(__p => __p._isVeryClosed(p, 300)).ToList();
                        __pts.Add(p);
                        
                        wall_pls.Add(__pts.Rect());
                    }
                }
        }

        static string __parseWallObjs(ObjectId entId)
        {
            Database db = ACD.DB;
            string content = "";

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (entId.ObjectClass.DxfName == "AEC_WALL")
                {
                    Autodesk.Aec.Arch.DatabaseServices.Wall wall
                        = (Autodesk.Aec.Arch.DatabaseServices.Wall)tr.GetObject(entId, OpenMode.ForRead);

                    if (wall != null)
                    {
                        string style = ACD.DB._getIdName(entId);

                        ObjectIdCollection srcIds = wall.GetOpeningsFor();
                        PosCollection opnPls = new PosCollection();

                        List<pPos> cts = new List<pPos>();

                        foreach (ObjectId id in srcIds)
                        {
                            pPos[] _l = db._getVertices(id).OrderBy(_p => _p.DistanceTo(wall.StartPoint.ToPos())).ToArray();

                            opnPls.Add(_l);
                            cts.Add(_l.CenterPoint());
                            cts.Last().Content = db._getIdName(id);
                        }
                    }
                }

                tr.Commit();
            }

            return content;
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                ACD.WR("!!! For ceiling object, put circle for low ceil plane !!!");
                ACD.WR("!!! Char $ = height, # = cote, +- = level !!!");
                ObjectIdCollection selIds = ACD.GetSelection().FilterIds("AEC_WALL","AEC_DOOR","AEC_WINDOW");
                ObjectIdCollection blockIds = ACD.GetSelection().FilterIds("INSERT");

                if (selIds.Count > 0)
                {
                    wall_pls = new PosCollection();
                    door_pls = new PosCollection();
                    window_pls = new PosCollection();

                    pPos[] bb = ACD.DB._getBound(selIds);

                    foreach(ObjectId wId in selIds)
                    {
                        _getWallOpeningInfo(wId);
                    }

                    //_checkIntersectJointInWallPoints();

                    foreach (pPos[] r in wall_pls)
                    {
                        resultIds.Add(ACD.DB.DrawPolyline(r, true));
                        ACD.DB._setLayer(resultIds.Last(), "A-Wall");
                    }

                    foreach (pPos[] r in door_pls)
                    {
                        resultIds.Add(ACD.DB.DrawPolyline(r, true));
                        ACD.DB._setLayer(resultIds.Last(), "A-Door");
                    }

                    //string new_block_name = ACD.DB.uniqueBlockName("wall_");
                    //ACD.DB.NewBlock(resultIds, new_block_name, true, false, bb[0]);
                    //ObjectIdCollection ids = ACD.DB.InsertBlock(new_block_name, new pPos[] { bb[0] }, null);
                    //ACD.DB._setLayer(ids, "0");


                }
            }

            ACD.Focus();
        }
    }
}

