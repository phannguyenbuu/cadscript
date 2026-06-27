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
    public class XNoteCloneCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                //Tree = 15
                //TreeAlign = Center
                //TreeJoint = On
                //TreeOffset = 0

                ObjectIdCollection selIds = ACD.GetSelection();

                string content = System.Windows.Forms.Clipboard.GetText().Replace("\r\n","|");

                if(content._allProps().Length > 0)
                    foreach(ObjectId objId in selIds)
                    {
                        string[] xnotes = ACD.DB.GetXNotes(objId);

                        foreach (string s in content._allPropNames())
                            xnotes = xnotes._setprops(s , content._prop(s));

                        ACD.DB.SetXNotes(objId, xnotes.ToTextStr("\r\n"));
                    }

                ACD.Focus();
            }
        }
    }
}

