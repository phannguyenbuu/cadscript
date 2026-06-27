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

//
using System.Windows.Forms;
//using SyncObject;
    
namespace AcadScript
{ 
    public class FindDuplCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ACD.DB.GetEntities(null, EN_SELECT.AC_DXF,"INSERT");

                Dictionary<ObjectId, string[]> dicts = IR.SelectedIds.ToList()
                    .ToDictionary(id => id, id => ACD.DB.GetXNotes(id));
                //ObjectIdCollection selIds = new ObjectIdCollection();

                //for(int i = 0; i < dicts.Count - 1; i++)
                //    if (!selIds.Contains(dicts.Keys.ElementAt(i)))
                //        for (int j = i + 1; j < dicts.Count; j++)
                //            if(!selIds.Contains(dicts.Keys.ElementAt(j))
                //                &&

                //                !dicts.Values.ElementAt(i)._props("Name").empty() 
                //                && dicts.Values.ElementAt(i)._props("Name").Upper() 
                //                == dicts.Values.ElementAt(i)._props("Name").Upper()

                //                && dicts.Values.ElementAt(i)._props("Index").Upper()
                //                == dicts.Values.ElementAt(i)._props("Index").Upper())
                //            {
                //                if (!selIds.Contains(dicts.Keys.ElementAt(i)))
                //                    selIds.Add(dicts.Keys.ElementAt(i));

                //                if (!selIds.Contains(dicts.Keys.ElementAt(j)))
                //                    selIds.Add(dicts.Keys.ElementAt(j));

                //                ACD.WR("ObjectId {0}", dicts.Values.ElementAt(i)._props("Name"));
                //                //break;
                //            }

                //selIds = selIds.ToList().Distinct().ToCollection();

                string[] keywords = new string[] { "AS-A","AS-B"};
                var selIds = dicts.Keys.Where(id
                    => !keywords.Any(k => !dicts[id]._props("Name").empty()
                    && dicts[id]._props("Name").Upper().Contains(k.ToUpper()))).ToCollection();
                ACD.WR("NameCount={0}", selIds.Count);

                if(selIds.Count > 0)
                    ACD.DB._setObjectVisible(selIds, false);
            }
            ACD.Focus();
        }
    }
}

