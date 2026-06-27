using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace AcadScript
{

    public class g3DTree: g3DBuild
    {
        PosCollection treeList;

        public g3DTree(ObjectIdCollection selIds,  cReadData _css): base(selIds, _css)
        {
            treeList = new PosCollection();
            
        }

        public void AddTrees(pPos[] pts, double dist)
        {
            List<pPos> newtrees = new List<pPos>();

            foreach (pPos __p in pts)
            {
                bool found = false;
                foreach (pPos __tp in treeList.AllPoints)
                    if (__p._isVeryClosed(__tp, dist * cReadData.__sc))
                    {
                        found = true;
                        break;
                    }

                if (!found)
                    newtrees.Add(__p);
            }

            if (newtrees.Count > 0)
                treeList.Add(newtrees.ToArray());
        }
    }
}
