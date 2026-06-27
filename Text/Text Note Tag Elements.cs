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
    public class TagElementsCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                string[] prams = null;// DV.DataViewValues();

                string current = null, current_title = null;

                string[] pramsIndex = prams.GetPramsIndex();

                for (int i = 0; i < prams.Length; i++)
                {
                    string key = prams[i].filter("=").First();
                    string val = prams[i].filter("=").Last();

                    if (key.IsDVTitle())
                    {
                        current_title = val;
                    }
                    else if (!key.StartsWith("[Ref"))
                    {
                        val = val.Replace("(", "|");
                        val = val.Replace(")", "=");

                        string s = val._prop("POS");

                        if (!s.empty())
                        {
                            pPos[] pts = new PosCollection(s).First;
                            IBlock.AttName = new string[] { "NO" };
                            IBlock.AttValue = new string[] { key };

                            foreach (pPos pt in pts)
                            {
                                pPos sz = new pPos(1000, 400);
                                db.DrawPolyline((pt - sz / 2).RectToPoint(pt + sz / 2),
                                    true, "LAYER=" + DE.DEF_LAYER_TEXT + "|ROUND=" + 150);
                                db.CreateText("#M" + pramsIndex[i], pt, 250, 0, "LAYER=" + DE.DEF_LAYER_TEXT);
                            }
                        }
                    }
                    current = prams[i];
                }
                ACD.Focus();
            }
        }
    }
}

