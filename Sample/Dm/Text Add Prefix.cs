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
    public class TextAddPrefixCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Database db = ACD.DB;
                ObjectIdCollection selIds = ACD.GetSelection();
                string prefix = ACD.ED.GetInputString("Letter prefix");

                foreach (ObjectId txtId in selIds)
                    if (db._isText(txtId))
                    {
                        ACD.DB._setContent(txtId, prefix + ACD.DB._getContent(txtId));
                    }

                ACD.Focus();
            }
        }
    }
}

