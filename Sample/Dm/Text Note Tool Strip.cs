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
    public class NoteToolStripCLS
    {
        static double fnC(double x)
        {
            return Math.Sqrt(x * x + (1 - x) * (1 - x));
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                List<string> prams = new List<string> { "Title=Test" };
                List<double> ar = new List<double>();

                double max = Double.NegativeInfinity;
                double index = 0;

                for (double i = 0; i <= 1; i += 0.001)
                    if (i != 0)
                    {
                        double v = fnC(i);
                        if (max < v)
                        {
                            max = v;
                            index = i;
                        }

                        ar.Add(v);
                        prams.Add(i.ToString() + "=" + v.ToString());
                    }

                prams.Add("Max value=" + max.ToString() + "(" + index.ToString() + ")");

                //DV.ViewParamData(prams);
                ACD.Focus();
            }
        }
    }
}

