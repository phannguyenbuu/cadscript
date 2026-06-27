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
    public class DimensionCoteCLS
    {

        

        static string[] XNotes;
        static double scale = 0.5;

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                

                if (selIds.Count > 0 && ACD.DB._isBlock(selIds.First()))
                {
                    //ACD.Get2Points();

                    //if (ACD.FirstPoint != null && ACD.LastPoint != null)
                    {


                        XNotes = ACD.AllSelectionXNotes;

                        double hmin = XNotes._props("Terrain.HMin").ToNumber();
                        double hmax = XNotes._props("Terrain.HMax").ToNumber();
                        double hfinish = XNotes._props("Terrain.HFinish").ToNumber();
                        int haxis = (int)XNotes._props("Terrain.Axis").ToNumber();

                        string hpoints = XNotes._props("Terrain.Points");

                        if (!hpoints.empty())
                        {
                            double y1 = hpoints.filter(" ")[0].ToNumber();
                            double y2 = hpoints.filter(" ")[1].ToNumber();

                            double axis = XNotes._props("Terrain.Axis").ToNumber();

                            ObjectIdCollection infoIds = new ObjectIdCollection();
                            ObjectId mainId = selIds.First();
                            pPos[] bb = ACD.DB._getBound(mainId);
                            pPos basept = ACD.DB._getPoint(mainId);
                            ObjectIdCollection cloneIds = new ObjectIdCollection();

                            ACD.DB.BlockEntitiesAction(mainId, (subIds) =>
                            {
                            //PosCollection pls = ACD.DB._getAllVertices(subIds, 0);
                            //PosCollection segments = pls.GetSegment();
                            //pPos[] verts = pls.AllPoints;

                                string title = XNotes._props("Att.Title");
                                string[] cotes = DE.NumericArray(1, 10).Select(n => XNotes._props("Att.Cote" + n)).ToArray();

                                subIds = subIds.ToList().OrderBy(id => ACD.DB.GetBlockAtt(id, title).Replace("N", "").ToNumber()).ToCollection();
                                pPos pt = basept.Clone();

                                for (int i = 0; i < subIds.Count; i++)
                                    if (ACD.DB._isBlock(subIds[i]))
                                    {
                                        ObjectId id = ACD.DB.CloneObject(subIds[i]);

                                        cloneIds.Add(id);

                                        //string name = ACD.DB.GetBlockAtt(id, title); //"N" + (i >= 99 ? "" : "0") + (i >= 9 ? "" : "0") + (i + 1);
                                        //                                             //ACD.DB.SetBlockAtt(id, title,name);

                                        //pPos ptord = ACD.DB._getPoint(id) - basept;

                                        //    infoIds.AddRange(_showText(name, "X" + (ptord.X < 0 ? "" : "+") + ptord.X.roundNumber(0.01),
                                        //        "Y" + (ptord.Y < 0 ? "" : "+") + ptord.Y.roundNumber(0.01), pt));
                                        //    pt += new pPos(20, 0);

                                        //    if (pt.X > bb[1].X - 20)
                                        //    {
                                        //        pt.X = basept.X;
                                        //        pt.Y -= 20;
                                        //    }

                                        double ny = ACD.DB._getPoint(id)[haxis];

                                        double n = (ny - y1) / (y2 - y1);

                                        if (n < 0)
                                            n = 0;

                                        if (n > 1)
                                            n = 1;

                                        ACD.DB.SetBlockAtt(id, cotes[0], "H+" + (n * (hmax - hmin) + hfinish).roundNumber(0.01));
                                        ACD.DB.SetBlockAtt(id, cotes[1], "H+" + (n * (hmax - hmin)).roundNumber(0.01));
                                    }
                            });

                            //string blockname = ACD.DB.uniqueBlockName("infor_");
                            //ACD.DB.NewBlock(infoIds, blockname, true, false, basept);
                            //ObjectId infoId = ACD.DB.Insert(blockname, basept);
                            //ACD.DB._setScale(infoId, 0.5, 0.5);


                            string blockname = ACD.DB.uniqueBlockName(ACD.DB._getIdName(mainId));
                            ACD.DB.NewBlock(cloneIds, blockname, true, false, basept);
                            ACD.DB.Insert(blockname, basept);
                        }
                    }

                    
                }

                ACD.Focus();
            }
        }
    }
}

