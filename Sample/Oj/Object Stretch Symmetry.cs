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

namespace AcadScript
{
    public class ObjectStretchSymmetryCLS
    {
        static pPos[] AnyParamsToPts(params object[] ls)
        {
            List<pPos> pts = new List<pPos>();

            pPos pt = new pPos(0, 0);
            pPos basept = new pPos(0, 0);
            int number_index = 0;

            foreach (var obj in ls)
                if (obj is string)
                {
                    string st = obj.ToString();
                    if (st.st_("$"))
                        basept = pPos.FromString(st.Substring(1).Replace(" ", ","));
                }

            //string s_number = "0123456789";

            foreach (var obj in ls)
                if (obj is pPos)
                {
                    pts.Add((pPos)obj);
                }
                else if (obj.GetType() == typeof(pPos[]))
                    pts.AddRange((pPos[])obj);
                else if (!(obj is string))
                {
                    double n = obj.ToNumber(double.NaN);

                    if (!double.IsNaN(n))
                    {
                        pt[number_index % 2] = n;

                        if (number_index % 2 == 1)
                            pts.Add(pt.Clone());

                        number_index++;
                    }
                }

            return pts.Move(basept).ToArray();
        }


        static void Strecth2D(ObjectIdCollection ids, params object[] objs)
        {
            string txt_override = "<>";
            double spacing = 0;

            foreach (var obj in objs)
                if (obj is string)
                {
                    string st = obj.ToString();

                    if (st.st_("S"))
                        spacing = st.Substring(1).ToNumber();
                    else if (st.st_("T"))
                        txt_override = st.Substring(1);
                }

            pPos[] pts = AnyParamsToPts(objs);


        }


        static void Resize2D(Database db, ObjectId id, params object[] prams)
        {

        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();

                if(selIds.Count > 0)
                {
                    
                }
                
            }
            ACD.Focus();
        }
    }
}

