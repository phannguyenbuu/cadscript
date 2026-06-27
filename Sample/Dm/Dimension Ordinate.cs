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
    
    public class DimensionOrdinateCLS
    {

        //static ObjectIdCollection _showText(string title, string value1, string value2, pPos pt)
        //{
        //    ObjectIdCollection res = new ObjectIdCollection();
        //    res.Add(ACD.DB.DrawPolyline(new pPos(0,0).Rect(20, -6), true));
        //    res.Add(ACD.DB.DrawPolyline((new pPos(0, -6)).Rect(20, -6), true));
        //    res.Add(ACD.DB.DrawPolyline((new pPos(0, -12)).Rect(20, -6), true));
        //    res.Add(ACD.DB.CreateText("#M" + title, new pPos(10, -3), 4));
        //    res.Add(ACD.DB.CreateText("#M" + value1, new pPos(10, -9), 4));
        //    res.Add(ACD.DB.CreateText("#M" + value2, new pPos(10, -15), 4));

        //    ACD.DB.MoveObject(res, pt);

        //    return res;
        //}

        static string[] XNotes;
        static double scale = 0.5;

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                if (selIds.Count > 0 && ACD.DB._isBlock(selIds.First()))
                {
                    XNotes = ACD.AllSelectionXNotes;

                    ObjectIdCollection infoIds = new ObjectIdCollection();
                    ObjectId mainId = selIds.First();
                    pPos[] bb = ACD.DB._getBound(mainId);
                    pPos basept = ACD.DB._getPoint(mainId);
                    //ObjectIdCollection cloneIds = new ObjectIdCollection();

                    UserTableDataCLS tab = new UserTableDataCLS(ACD.DB, basept);

                    ACD.DB.BlockEntitiesAction(mainId, (subIds) =>
                    {
                        //PosCollection pls = ACD.DB._getAllVertices(subIds, 0);
                        //PosCollection segments = pls.GetSegment();
                        //pPos[] verts = pls.AllPoints;

                        string title = XNotes._props("Att.Title");
                        string[] cotes = DE.NumericArray(1, 10).Select(n => XNotes._props("Att.Cote" + n)).ToArray();

                        subIds = subIds.ToList().OrderBy(id => ACD.DB.GetBlockAtt(id,title).Replace("N","").ToNumber()).ToCollection();

                        pPos pt = basept.Clone();

                        for (int i = 0; i < subIds.Count; i++)
                            if (ACD.DB._isBlock(subIds[i]))
                            {
                                ObjectId id = subIds[i];

                                //cloneIds.Add(id);

                                //string name = ; //"N" + (i >= 99 ? "" : "0") + (i >= 9 ? "" : "0") + (i + 1);
                                //ACD.DB.SetBlockAtt(id, title,name);

                                pPos ptord = ACD.DB._getPoint(id) - basept;

                                tab.AddUserTableData(ACD.DB.GetBlockAtt(id, title), 
                                    new string[]{"X" + (ptord.X < 0 ? "" : "+") + ptord.X.roundNumber(0.01),
                                    "Y" + (ptord.Y < 0 ? "" : "+") + ptord.Y.roundNumber(0.01) }, pt, pt.X > bb[1].X - 20 ? -20 : 0);
                            }
                    });

                    tab.Insert(0.5);
                }

                ACD.Focus();
            }
        }
    }
}

