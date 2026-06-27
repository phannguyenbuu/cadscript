using System;
using System.Reflection;
using System.Linq;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Internal;

namespace AcadScript
{
    
    public class Testy
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection ids = ACD.GetSelection();

                ACD.DB._setHatchElevation(ids[0], 1000);
                    //.Transforms(ids, new pPos(0, 0, -ACD.DB._getElevation(ids[0])), 0, 1);
                ACD.Focus();
            }
        }

    }
}
