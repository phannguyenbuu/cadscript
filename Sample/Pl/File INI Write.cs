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
    public class INIWriteCLS
    {
        static void WriteINI()
        {
            double n = pString.INI_String("TrackValue").ToNumber();
            
            //lblTrackValue.Text = (n < 10 ? n * 10 : (n - 9) * 100).ToString();
            string[] str = new string[0];
            if (File.Exists(DE.INI_FILE))
            {
                str = File.ReadAllLines(DE.INI_FILE);
            }

            if (!str._props("TrackValue").empty())
            {
                str[Array.FindIndex(str, s => s.Upper().Contains("TRACKVALUE"))] = "TrackValue=" + n;
            }
            else
                str = str.Add("TrackValue=" + n);

            File.WriteAllLines(DE.INI_FILE, str);
        }

        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                WriteINI();

                ACD.Focus();
            }
        }
    }
}

