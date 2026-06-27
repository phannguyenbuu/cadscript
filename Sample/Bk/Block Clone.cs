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

using MessagingToolkit;
using System.Windows.Forms;
using SyncObject;

namespace AcadScript
{
    public class BlockCloneCLS
    {
        
        
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                ObjectIdCollection selIds = EDSelection.GetSelection();
                ObjectId objId = selIds[0];

                foreach (ObjectId id in selIds)
                    if (id != objId)
                    {
                        pPos[] pts = ACD.DB._getVertices(id, 0);

                        foreach (pPos pt in pts)
                        {
                            ObjectId newId = ACD.DB.CloneObjects(objId);
                            ACD.DB._setPoint(newId, pt);
                        }
                    }
            }
        }
    }
}

