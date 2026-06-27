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
    public class BlockReplaceCLS
    {
        
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = ACD.GetSelection();
                ObjectIdCollection blockIds = selIds.ToList().Where(id => ACD.DB._isBlock(id)).ToCollection();

                foreach (ObjectId id in selIds)
                    if(!ACD.DB._isBlock(id)){
                        foreach (ObjectId bId in blockIds)
                        {
                            ObjectId cloneId = ACD.DB.CloneObject(bId);
                            ACD.DB._setPoint(cloneId, ACD.DB._getBound(id).CenterPoint());
                        }
                        ACD.DB.EraseObject(id);
                    }

                ACD.Focus();
            }
        }
    }
}

