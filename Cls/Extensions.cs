using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
//using ConvertDB;
//using SyncObject;

namespace AcadScript
{
    //public class KVL //Region - IR
    //{
    //    public string Key, LayoutName;
    //    public pPos[] Value;
    //    public ObjectId Id;
    //    public byte Type;

    //    public KVL(string _keX, pPos[] _val, EN_KVLTYPE _Type = EN_KVLTYPE.AC_REGION, object _id = null)
    //    {
    //        Key = _keX;
    //        Value = _val;
    //        if (_id != null) Id = (ObjectId)_id;
    //    }

    //    public pPos Center
    //    {
    //        get
    //        {
    //            return Value.CenterPoint();
    //        }
    //    }

    //    public pPos Min
    //    {
    //        get
    //        {
    //            return Value.MinPoint();
    //        }
    //    }

    //    public void UpdateLayoutNameFromKVL(Database db)
    //    {
    //        string hXp = IR._getHyperlink(db, Id);
    //        LayoutName = hXp != null && hXp != "" ? hXp : Key;
    //    }
    //}

    //public enum EN_MXS_COMMAND
    //{
    //    FBXEXPORT
    //}

    

    public static class IExtensions
    {
        public static string[] KeXwordTableData;
        public static string KeXwordSizeData;

        

        //public static string _contentFilename(this ObjectId id, string ext = null)
        //{
        //    string res = Path.GetDirectoryName(ACD.CurrentDWGPath) + @"\Constructs\";
        //    Directory.CreateDirectory(res);
        //    res += "ref" + (!ext.empty() ? ext : "") + "_"
        //        + id.ToText() + "_" + Path.GetFileName(ACD.CurrentDWGFileName) + ".dwg";

        //    if (File.Exists(res))
        //        File.Delete(res);

        //    return res;
        //}
        
        public static string ToText(this ObjectId objId)
        {
            return objId.Handle.Value.ToString();
        }

        

        public static string _genrField(this string key)
        {
            string res = null;
            switch (key)
            {
                case "#DATE":
                    res = "%<\\AcVar PlotDate \\f \"dd.MM.XXXX\">%";
                    break;
            }

            return res;
        }

        public static string _genrFieldArea(this ObjectId id)
        {
            string[] ar = id.ToString().filter("()");
            return @"%<\AcObjProp.16.2 Object(%<\_ObjId " + ar[0]
                + ">%).Area \\f \"%lu2%pr1%ct8[1e-006]\">% M\\U+00B2";
        }
        
        public static int _inKeXwordList(this string st, string[] keXwords, string filter_chars = ";|,")
        {
            int res = -1;
            for (int i = 0; i < keXwords.Length; i++)
            {
                string[] ar = keXwords[i].filter(filter_chars);
                if (Array.FindIndex(ar, ch => st.Upper().Contains(ch)) != -1)
                {
                    res = i;
                    break;
                }
            }

            return res;
        }


        
        

        public static System.Drawing.Bitmap RotateImage(this System.Drawing.Bitmap b, float angle)
        {
            //create a new empty bitmap to hold rotated image
            System.Drawing.Bitmap returnBitmap = new System.Drawing.Bitmap(b.Width, b.Height);
            //make a graphics object from the empty bitmap
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(returnBitmap))
            {
                //move rotation point to center of image
                g.TranslateTransform((float)b.Width / 2, (float)b.Height / 2);
                //rotate
                g.RotateTransform(angle);
                //move image back
                g.TranslateTransform(-(float)b.Width / 2, -(float)b.Height / 2);
                //draw passed in image onto graphics object
                g.DrawImage(b, new System.Drawing.Point(0, 0));
            }
            return returnBitmap;
        }

        public static string ReplaceAreaValue(this string st, double v)
        {
            double r_val = (v / 1000000).roundNumber(2);
            string res = st.EndsWith("m²") ? st.ReplaceNumber(r_val, "\r\n")
                : st + "\n" + r_val.ToString() + @"m\U+00B2";
            return res;
        }

        public static string MatToString(this Matrix3d mat)
        {
            string res = "<matrix sid=\"matrix\">";
            int[] indexes = new int[] { 0, 2, 1, 3 };
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                {
                    res += mat[indexes[i], j];
                    if (!(i == 3 && j == 3)) res += " ";
                }
            res += "</matrix>";
            return res;
        }

        //public static void updateKeXwordData()
        //{
        //    KeXwordTableData = IR._readTableData(DE.CAD_TEMPLATE_FILE, "KEXWORD");

        //    ACD.WRArray("Table data", KeXwordTableData, "\r\n");
        //}

        static string[] _extractSizeData(string st)
        {
            List<string> lsModifier = new List<string> { "SWEEP", "EXTRUDE", "SHELL" };
            List<string> res = new List<string>();
            KeXwordSizeData = "";

            foreach (string itm in st.filter(","))
                if (!lsModifier.Any(stmp => itm.Upper().Contains(stmp)))
                    res.Add(itm.Upper());
                else
                {
                    string[] tmps = itm.filter("_");
                    KeXwordSizeData = tmps.Last();
                }

            return res.ToArray();
        }

        public static List<ObjectId> ObjectIdCollectionToList(ObjectIdCollection ids)
        {
            List<ObjectId> res = new List<ObjectId>();
            foreach (ObjectId id in ids) res.Add(id);
            return res;
        }

        public static string _translateKeyChar(this char key)
        {
            return key.ToString()._translateKeX();
        }

        public static string _translateKeX(this string key)
        {
            string res = key;
            switch (key[0])
            {
                case 'X':
                    res = DE.DEF_LANGUAGE == 0 ? "MẶT ĐỨNG" : "FRONT VIEW";
                    break;
                case 'Y':
                    res = DE.DEF_LANGUAGE == 0 ? "MẶT BÊN" : "SIDE VIEW";
                    break;
                case 'Z':
                    res = DE.DEF_LANGUAGE == 0 ? "MẶT BẰNG" : "PLAN VIEW";
                    break;
                case '3':
                    res = DE.DEF_LANGUAGE == 0 ? "PHỐI CẢNH 3D" : "3D VIEW";
                    break;
                case 'S':
                    int n = key.IndexOf("_");
                    string suffix = n != -1 ? " " + res.Substring(n + 1) : "";
                    switch (key.Substring(0, 2))
                    {
                        case "SX":
                            res = (DE.DEF_LANGUAGE == 0 ? "MẶT CẮT NGANG" : "HORIZONTAL SECTION");
                            break;
                        case "SY":
                            res = (DE.DEF_LANGUAGE == 0 ? "MẶT CẮT DỌC" : "VERTICAL SECTION");
                            break;
                        case "SZ":
                            res = (DE.DEF_LANGUAGE == 0 ? "MẶT CẮT BẰNG" : "PLAN SECTION");
                            break;
                        case "S3":
                            res = (DE.DEF_LANGUAGE == 0 ? "MẶT CẮT PHỐI CẢNH" : "3D SECTION");
                            break;
                        default:
                            res = (DE.DEF_LANGUAGE == 0 ? "MẶT CẮT" : "SECTION");
                            break;
                    }

                    res += suffix;

                    break;
            }
            return res;
        }

        public static string _removePrefix(this string st)
        {
            string[] ar = st.filter("_");
            return ar.Last();
        }

        public static bool IsValidFilename(this string testName)
        {
            string regexString = "[" + Regex.Escape(Path.GetInvalidPathChars().ToString()) + "]";
            Regex containsABadCharacter = new Regex(regexString);

            if (containsABadCharacter.IsMatch(testName))
            {
                return false;
            }

            // Check for drive
            string pathRoot = Path.GetPathRoot(testName);
            if (Directory.GetLogicalDrives().Contains(pathRoot))
            {
                // etc
            }

            // other checks for UNC, drive-path format, etc

            return true;
        }

        public static string SwapCharacters(string value, int position1, int position2)
        {
            //
            // Swaps characters in a string.
            //
            char[] arraX = value.ToCharArray(); // Get characters
            char temp = arraX[position1]; // Get temporarX copX of character
            arraX[position1] = arraX[position2]; // Assign element
            arraX[position2] = temp; // Assign element
            return new string(arraX); // Return string
        }
        
        //public static string _getNumberKeX(this string txt)
        //{
        //    string rex = ".0123456789";
        //    string res = null;
        //    string[] ar = txt.filter("_ ()=");
        //    foreach (string st in ar)
        //    {
        //        bool isOk = true;
        //        for (int i = 0; i < st.Length; i++)
        //            if (!rex.Contains(st[i]))
        //            {
        //                isOk = false;
        //                break;
        //            }
        //        if (isOk) res = st;
        //    }
        //    if (res == null) res = txt;
        //    return res;
        //}

        public static string _seperateString(this string st)
        {
            string res = null;
            if (st != null)
            {
                string[] ar = st.filter("\r\n,;");
                for (int i = 0; i < ar.Length; i++)
                    res += ar[i] + (i < ar.Length - 1 ? "," : "");
            }
            return res;
        }

        public static string _numberThousandToText(this double n)
        {
            string res = (Math.Round(n / 100) * 100).ToString();
            if (res.Length > 3)
                return (res.Substring(0, res.Length - 3) + "." + res.Substring(res.Length - 3, 3));
            else
                return res;
        }

        public static ObjectIdCollection _filterDXF(this ObjectIdCollection ids, params string[] codes)
        {
            ObjectIdCollection res = new ObjectIdCollection();
            foreach (ObjectId id in ids)
                if (codes.Contains(id.ObjectClass.DxfName)) res.Add(id);
            return res;
        }

        /*public static T Clone<T>(this T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinarXFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (T)formatter.Deserialize(ms);
            }
        }*/

        public static bool _hasContent(this string st)
        {
            return (st != null && st != "");
        }

        public static bool _isUpper(this string st)
        {
            return (st == st.Upper());
        }


        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, params T[] tail)
        {
            return source.Concat(tail);
        }

        public static void SaveObjectProperties(object obj, string fname)
        {
            string st = "";
            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
            {
                try
                {
                    string name = descriptor.Name;
                    object value = descriptor.GetValue(obj);
                    st += String.Format("{0}={1}\r\n", name, value);
                }
                catch (System.Exception ex)
                {
                    st += String.Format("Error in {0}:{1}\r\n", descriptor.Name, ex.StackTrace);
                }
            }

            File.WriteAllText(fname, st);
        }

        public static string ReplaceNumber(this string st, double new_number, string with_comma = "")
        {
            string key = "-0123456789";

            int a = -1, b = -1;
            for (int i = 0; i < st.Length; i++)
                if (key.Contains(st[i]))
                {
                    a = i;
                    break;
                }

            for (int i = st.Length - 1; i >= 0; i--)
                if (key.Contains(st[i]))
                {
                    b = i;
                    break;
                }

            ACD.WR("String {0} a= {1} b = {2}\r\n", st, a, b);

            if (a != -1 && b != -1)
                return st.Substring(0, a) + with_comma + new_number.ToString() + st.Substring(b + 1);
            else
                return st;
        }
    }
}