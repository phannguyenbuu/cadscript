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
    public class TextReminderCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                string key = "<KEYWORD/>";
                string[] prams = ACD.LoadChapter(DE.INI_FILE, "TEXT_REMINDER");
                
                if (prams != null)
                {
                    foreach(string pram in prams)
                    {
                        if(pram._firstPropName().Upper() == key.Upper())
                        {
                            string[] ls = pram._firstProp().filter(";");

                            foreach(string st in ls)
                            {
                                ACD.WR(st);
                                pPos pt = ACD.GetPoint();
                                                                
                                if (!pt.IsNull)
                                {
                                    double h = 2.5;
                                    string s = "";

                                    if (key.Upper().Contains("_T"))
                                        h = 4;

                                    if (key.Upper().Contains("_B"))
                                        s = "\\B";

                                    if (key.Upper().Contains("_U"))
                                        s += "\\L";

                                    if (key.Upper().Contains("_C"))
                                        s = "#M" + s;


                                    if (!s.empty())
                                        s = s + (st.Contains("(") ? st._getBeforeComma() : st).ToUpper();
                                    else
                                        s = (st.Contains("(") ? st._getBeforeComma() : st).ToUpper();

                                    ObjectId txtId = ACD.DB.CreateText(s, pt, h, 0, "LAYER=A-Anno-Dims");

                                    if (key.Upper().Contains("_V"))
                                        ACD.DB.Rotate(new ObjectIdCollection { txtId }, 90, pt);

                                    if (key.Upper().Contains("_C"))
                                        ACD.DB.DrawCircle(pt, h / ACD.DB.CurrentAnnotativeScale(), "LAYER=A-Anno-Dims");
                                }
                            }
                        }
                    }
                }

                ACD.Focus();
            }
        }
    }
}


