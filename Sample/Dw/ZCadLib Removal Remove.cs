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
    public class RemovalRemoveCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ACD.DB.GetEntities(ACD.bRect, EN_SELECT.AC_ALL);
                ObjectIdCollection selIds = IR.SelectedIds;
                ACD.DB.EraseObjects(selIds.ToList()
                        .Where(id => ACD.DB._getHyperlink(id).StartsWith("REMOVAL")).ToCollection());
                ACD.Focus();
            }
        }
    }
}

