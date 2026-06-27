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
    public class TextRotationCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                //ACD.WR("If text contains X then rotation = 0 else if contains Y then rotation = 90");
                ObjectIdCollection selIds = ACD.GetSelection();

                foreach (ObjectId id in selIds)
                    if (ACD.DB._isText(id)||ACD.DB._isBlock(id))
                    {
                        double rot = ACD.DB._getRotation(id);
                        
                        if (rot % 90 < 45)
                            ACD.DB._setRotation(id, 0);
                        else
                            ACD.DB._setRotation(id, -90);
                    }
            }
            ACD.Focus();
        }
    }
}

