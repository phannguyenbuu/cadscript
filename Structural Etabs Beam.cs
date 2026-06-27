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
    public class StructuralEtabsBeamCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                bool control_mode = ACD.ControlHold;
                ObjectIdCollection selIds = ACD.GetSelection();

                string key = "123456789";

                foreach (ObjectId txtId in selIds)
                    if (ACD.DB._isText(txtId))
                    {
                        string st = ACD.DB._getContent(txtId);
                        st = st.filter("()").Last();
                        int index = -1;

                        for (int i = st.Length - 1; i >= 0; i--)
                        {
                            if (key.Contains(st[i]))
                            {
                                index = i - 1;
                                break;
                            }
                        }

                        if (index > -1)
                            st = st.Substring(0, index + 1) + "x" + st.Substring(index + 1);

                        ACD.DB._setContent(txtId, st);
                    }
            }

            ACD.Focus();
        }
    }
}

