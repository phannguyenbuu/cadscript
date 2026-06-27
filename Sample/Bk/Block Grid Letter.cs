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
    public class BlockGridLetterCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                //bool control_mod = Control.ModifierKeys == Keys.Control;
                //string[] blocknames = ACD.DB.ListBlock();
                //string[] libnames = new string[0];
                ObjectIdCollection selIds = ACD.GetSelection();

                double size = ACD.ED.GetInputString("Size", "400").ToNumber(400);

                if(selIds.Count > 0)
                foreach(ObjectId blockId in selIds)
                {
                    PosCollection pls = ACD.DB.GetIdPts(blockId);
                    List<double[]> xys = DE.NumericArray(0, pls.Count - 1)
                        .Where(n => !pls.Closed[n] && pls[n].Length() >= 1000)
                        .Select(n => pls[n]).ToCollectionSameClosed()
                        .SelfIntersect.ExtractPtsXY(1,50);

                    pPos[] bb = pls.Boundary;

                    for(int axis = 0; axis < 2; axis++)
                    {
                        int nex = (axis + 1) % 2;
                        
                        for (int i = 0; i < xys[axis].Length; i++)
                        {
                            pPos p1 = new pPos(0, 0);
                            p1[axis] = xys[axis][i];
                            p1[nex] = bb[0][nex];

                            pPos p2 = p1.Clone();
                            p2[nex] = bb[1][nex];

                            string letter = "#M" + (axis == 0 ? (i + 1).ToString() : ((char)(xys[axis].Length - i + 64)).ToString());

                            ACD.DB.CreateText(letter, p1, size / 2);
                            ACD.DB.CreateText(letter, p2, size / 2);
                            ACD.DB.DrawCircle(p1, size / 2, GP.DEF_LAYER_TEXT);
                            ACD.DB.DrawCircle(p2, size / 2, GP.DEF_LAYER_TEXT);
                        }
                    }
                }

                ACD.Focus();
            }
        }
    }
}

