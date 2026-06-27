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
    public class TableCellGoCLS
    {
        static pPos pt;
        
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                string content = ACD.DB.GetTableElement(pt);
                if (!content.empty() && !content._prop("IDS").empty())
                {
                    //ACD.WR("Content {0} IDS {1}", content, content._prop("IDS"));
                    ObjectIdCollection ids = content._prop("IDS").filter(",")
                        .Cast<string>().Select(st => ACD.DB.ToObjectId(st)).ToCollection();

                    ACD.WR("Index {0} Content {1} Ids {2} Dxf {3}",
                        pt, content, ids.Count, ids.First());

                    if (ids.Count > 0 && ACD.DB.ValidId(ids.First()))
                    {
                        ACD.ED.SetImpliedSelection(ids.ToArray());
                        ACD.DB.ZoomBounds(ACD.DB._getBound(ids.First()));
                    }
                }
                ACD.Focus();
            }
        }
    }
}

