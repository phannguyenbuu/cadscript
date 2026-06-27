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
        static void _insertBlock(string key, pPos pt, pPos pt2)
        {
            ObjectId objId = ObjectId.Null;

            if (ACD.DB.HasBlock(key).IsNull)
                objId = ACD.DB.Insert(DE.CAD_TEMPLATE_FILE, key, new pPos[] { pt }).ToList().FirstOrDefault();
            else
                objId = ACD.DB.Insert(key, new pPos[] { pt }.ToList().FirstOrDefault());

            if(!objId.IsNull)
            {
               ACD.DB._setRotation(objId, (pt - pt2).Angle());
            }
        }

        static void _drawLayout(pPos[] region)
        {
            
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                string key = "<KEYWORD/>";
                string[] prams = ACD.LoadChapter(DE.INI_FILE, "TEXT_REMINDER");
                bool mode = ACD.ControlHold;

                if (prams != null)
                {
                    foreach(string pram in prams)
                    {
                        string keyword = pram._firstPropName();

                        if (keyword.st_(key))
                        {
                            string[] ls = pram._firstProp().filter(";");

                            if (key.ct_("(n)"))
                                ls = DE.NumericArray(0, 100).Select(n => ls.First()).ToArray();
                            
                            foreach (string st in ls)
                            {
                                ACD.WR(st);
                                pPos pt = ACD.GetPoint();
                                                                
                                if (!pt.IsNull)
                                {
                                    pt = pt.Round(50);
                                    double h = 2.5;
                                    string s = "";

                                    if (key.Upper().Contains("(t)"))
                                        h = 4;

                                    if (key.Upper().Contains("(b)"))
                                        s = "\\B";

                                    if (key.Upper().Contains("(u)"))
                                        s += "\\L";

                                    if (key.Upper().Contains("(c)"))
                                        s = "#M" + s;


                                    if (!s.et_())
                                        s = s + (st.Contains("(") ? st._getBeforeComma() : st).ToUpper();
                                    else
                                        s = (st.Contains("(") ? st._getBeforeComma() : st).ToUpper();

                                    

                                    if (key.st_("SYMMETRY") || key.st_("SECTION"))
                                    {
                                        pPos pt2 = ACD.GetPoint();

                                        if (pt2 != null)
                                        {
                                            if (Math.Abs(pt.X - pt2.X) <= 500) 
                                                pt2.X = pt.X;
                                            else if (Math.Abs(pt.Y - pt2.Y) <= 500) 
                                                pt2.Y = pt.Y;

                                            pt2 = pt2.Round(50);

                                            ObjectId lwpId = ACD.DB.Draw2D(pt, pt2,"A-HIDDEN", "LTYPE=CENTER2", "LSCALE=0.25");
                                            
                                            if (key.st_("SECTION"))
                                            {
                                                ACD.DB.CreateText(s.filter("-").Last(), pt, h, 0);
                                                _insertBlock("G.Section.Icon", pt.Along(200, pt2), pt2);
                                                
                                                pt = pt2;
                                                h = 4;
                                            }else
                                            {
                                                _insertBlock("G.Symmetry.Icon", pt.Along(200, pt2), pt2);
                                                h = 2;
                                            }
                                        }
                                    }

                                    if (h != 0)
                                    {
                                        ObjectId txtId = ACD.DB.CreateText(s, pt, h, 0, "LAYER=A-Anno-Dims");

                                        if (key.ct_("(v)"))
                                            ACD.DB.Rotate(new ObjectIdCollection { txtId }, 90, pt);
                                        if (key.ct_("(45)"))
                                            ACD.DB.Rotate(new ObjectIdCollection { txtId }, 45, pt);
                                        if (key.ct_("(135)"))
                                            ACD.DB.Rotate(new ObjectIdCollection { txtId }, 135, pt);
                                        if (key.ct_("(225)"))
                                            ACD.DB.Rotate(new ObjectIdCollection { txtId }, 225, pt);
                                        if (key.ct_("(315)"))
                                            ACD.DB.Rotate(new ObjectIdCollection { txtId }, 315, pt);

                                        if (key.ct_("(c)"))
                                            ACD.DB.DrawCircle(pt, h / ACD.DB.CurrentAnnotativeScale(), "LAYER=A-Anno-Dims");
                                    }
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


