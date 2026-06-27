using Autodesk.AutoCAD.DatabaseServices;
//using SyncObject;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
//using System.Drawing;
using System.Linq;
//using System.Text;

//fileIn Cls.IReader
//fileIn Cls.Global
//fileIn Cls.MethodEntity
//fileIn Cls.IZone

//fileIn Cls.ACD
//fileIn Cls.IS
//fileIn Cls.IDraw

//fileIn Cls.IBlock
//fileIn Cls.IAnno
//fileIn Cls.IDimChain

namespace AcadScript
{
    public class ObjectBoundingCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0)
                {
                    string s = ACD.ED.GetInputString("Get bounding of all objects? (Y/N)", "Y");

                    PosCollection pls = new PosCollection();

                    if (s.ToUpper() == "Y")
                    {
                        pls.Add(ACD.DB._getBound(selIds).Rect());
                        
                    } else
                    {
                        foreach (ObjectId id in selIds)
                            pls.Add(ACD.DB._getBound(id).Rect());
                    }

                    pls.Closed = pls.Select(ls => true).ToArray();

                    ACD.DB.DrawPolyline(pls,"LAYER=DEFPOINTS");

                    string build_grid = ACD.ED.GetInputString("Divide grid cell size? (100)", "100");
                    double sz = build_grid.ToNumber();

                    if(sz >0 )
                    {
                        foreach (pPos[] ls in pls)
                        {
                            ObjectIdCollection ids = new ObjectIdCollection();
                            pPos[] bb = ls.Boundary();

                            for (double x = 0; x < ls.Size().X; x += sz)
                                ids.Add(ACD.DB.DrawPolyline(new pPos[] { new pPos(bb[0].X + x, bb[0].Y),
                                    new pPos(bb[0].X + x, bb[1].Y) }, false, "LAYER=0"));

                            for (double y = 0; y < ls.Size().Y; y += sz)
                                ids.Add(ACD.DB.DrawPolyline(new pPos[] { new pPos(bb[0].X, bb[0].Y +  y),
                                    new pPos(bb[1].X, bb[0].Y + y) }, false, "LAYER=0"));

                            //ACD.WR("Size {0} BB {1} {2} LS_SIZE {3} IDS {4}", sz, bb[0], bb[1], ls.Size(), ids.Count);
                            string new_grid = ACD.DB.uniqueBlockName("grid");
                            ids = ACD.DB.NewBlock(ids, new_grid, true, false, bb[0]);
                            ACD.DB.Insert(new_grid, bb[0], "LAYER=A-Hatch");
                            ACD.DB.CreateText("CELL:" + sz + " x " + sz, 
                                bb[0] + new pPos(sz / 2, sz / 2), 2.5,0, "LAYER=A-Anno-Dims");
                        }
                    }
                }

                ACD.Focus();
            }
        }
    }
}

