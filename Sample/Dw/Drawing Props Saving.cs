using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Forms;
//using SyncObject;

namespace AcadScript
{
    public class DrawingPropSavingCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                Clipboard.SetText("<AcadScript>\r\n" + ACD.DB.GetAllDrawingProp().ToTextStr("\r\n"));
            }

            ACD.Focus();
        }
    }
}

