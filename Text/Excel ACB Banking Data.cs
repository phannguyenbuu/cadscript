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
using SyncObject;

namespace AcadScript
{
    public class ExcelACBBankingDataCLS
    {
        public static void Main(string[] args)
        {
            using (ACD.Lock())
            {
                //ACD.WR("Exist {0}", File.Exists(@"D:\Dropbox\_Documents\Docs\SMS.txt"));
                string[] str = File.ReadAllLines(@"D:\Dropbox\_Documents\Docs\SMS2.txt");

                //ACD.WR("Line {0}", str.Length);
                List<string> result = new List<string>();

                foreach (string s in str)
                    if(!s.empty() && s.Upper().StartsWith("ACB"))
                    {
                        string[] letters = s.filter(" ");
                        string res = "";

                        //foreach (string lt in letters)
                        //{
                        //    if (lt.Contains("/") && lt.Contains("2020") || lt.Contains("2021"))
                        //        res += lt.Replace(".", "") + "\t";
                        //}
                        int a = s.IndexOf(" luc ");
                        int b = -1;

                        if (a != -1)
                        {
                            b = s.IndexOf(".", a);

                            if (b != -1)
                                res += s.Substring(a, b - a + 1).Replace(" luc ", "").Replace(".", "").Replace(" ","\t") + "\t";

                            b = s.IndexOf("GD:");
                            if (b != -1)
                                res += s.Substring(b).Replace("GD:", "").ToUpper() + "\t";

                            b = s.IndexOf("(VND)");

                            if (b != -1)
                            {
                                string val = s.Substring(b, a - b + 1).Replace("(VND)", "").Upper();
                                if (val.StartsWith("+"))
                                    res += " \t" + val + "\t"; 
                                else
                                    res += val + "\t \t";
                            }

                            a = s.IndexOf("So du ");

                            if (b != -1)
                            {
                                b = s.IndexOf(".", a);
                                if (b != -1)
                                    res += s.Substring(a, b - a + 1).Replace("So du ", "").Replace(".", "");
                            }
                        }

                        result.Add(res);
                        ACD.WR(res);
                    }

                Clipboard.SetText(result.ToText("\r\n"));
                //ACD.Focus();
            }
        }
    }
}
