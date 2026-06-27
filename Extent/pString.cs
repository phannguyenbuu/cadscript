using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace AcadScript
{

    public static class pString
    {
        public static bool isRectByVerts;

        public static string CADLIB = @"D:\Dropbox\CADLIb\";
        public static string CADLIB_ELEMENT = CADLIB + @"Elements\";
        public static string CADLIB_CONSTRUCT = CADLIB + @"Constructs\";
        public static string INI_FILE = CADLIB_CONSTRUCT + @"\INI_Setting.txt";

        //--------------------------------------------------------------------------------------------------------------------------------------
        public static double INI_Value(string key)
        {
            return INI_String(key).ToNumber();
        }

        public static string _gValue(this string st, string key)
        {
            string res = null;
            string[] ar = st.filter(" ");
            string v = ar.FirstOrDefault(s => s.st_(key + "=\""));

            if (!v.et_())
            {
                res = v._firstProp().Replace("\"", "");
            }

            return res;
        }

        public static string _sValue(this string st, string key, string value)
        {
            string res = "";

            if (!st._gValue(key).et_())
                res = st.Replace(key + "=\"" + st._gValue(key) + "\"", key + "=\"" + value + "\"");
            else
                res = st.Replace("/>", " " + key + "=\"" + value + "\"/>");

            return res;
        }

        public static string _eraseValue(this string st, string key)
        {
            return st.Replace(" " + key + "=\"" + st._gValue(key) + "\"", "");
        }

        public static bool st_(this string st, string key)
        {
            return st.Upper().StartsWith(key.Upper());
        }

        public static bool et_(this string st)
        {
            return st.empty();
        }

        public static bool ct_(this string st, string key)
        {
            return st.Upper().Contains(key.Upper());
        }

        public static bool Set_INI_String(string key, string value)
        {
            if (File.Exists(INI_FILE))
            {
                string[] res = File.ReadAllLines(INI_FILE);
                res = res._setprops(key, value);
                File.WriteAllLines(INI_FILE, res);
                return true;
            }
            else
                return false;
        }

        public static string INI_String(string key)
        {
            string res = null;

            if (File.Exists(INI_FILE))
                res = File.ReadAllLines(INI_FILE)._props(key);
            return res;
        }

        public static bool IsDVTitle(this string st)
        {
            return st.ToUpper() == "TITLE" || st.ToUpper() == "SHEET" || st.ToUpper() == "CHAPTER";
        }

        public static string[] INI_Params(string key)
        {
            List<string> res = new List<string>();

            if (File.Exists(DE.INI_FILE))
            {
                string[] str = File.ReadAllLines(DE.INI_FILE);

                string val = str._props(">" + key);
                if (val.empty())
                    val = str._props(key);

                if (!val.empty())
                {
                    //res = new List<string>();
                    val = val.Replace("~", "=");
                    val = val.Replace("/", "|");
                    val = val.Replace("\\", "|");

                    string[] ar = val.filter("#");

                    if (ar.Length > 1)
                        for (int i = 0; i < ar.Length; i++)
                            res.Add("#" + ar[i]);
                    else
                        res.Add(val);
                }
            }
            return res.ToArray();
        }

        public static string IntListToText(this IEnumerable<int[]> intlist)
        {
            string content = "";
            foreach (int[] ls in intlist)
            {
                for (int i = 0; i < ls.Length; i++)
                    content += ls[i] + (i < ls.Length - 1 ? "," : "");
                content += ";";
            }
            return content;
        }

        public static double _getPramTextHeight(this string st)
        {
            string[] keys2 = new string[] { "S", "M", "L", "XL" };
            string[] keys1 = new string[] { "TXT", "CONTENT" };
            double res = 10;

            for (int i1 = 0; i1 < keys1.Length; i1++)
            {
                for (int i2 = 0; i2 < keys2.Length; i2++)
                    if (st.Contains(keys1[i1] + keys2[i2]))
                    {
                        res = pString.INI_Value("TXT" + keys2[i2]);
                        break;
                    }
                if (res != 0)
                    break;
            }

            return res;
        }
        public static string RemoveUnicodeCharacters(this string st, string fallbackStr = "")
        {
            Encoding enc = Encoding.GetEncoding(Encoding.ASCII.CodePage,
              new EncoderReplacementFallback(fallbackStr),
              new DecoderReplacementFallback(fallbackStr));

            return enc.GetString(enc.GetBytes(st));
        }
        public static string UpperWords(this string value)
        {
            char[] array = value.ToCharArray();
            // Handle the first letter in the string.
            if (array.Length >= 1)
                array[0] = char.ToUpper(array[0]);

            // Scan through the letters, checking for spaces.
            // ... Uppercase the lowercase letters following spaces.
            string key = " ._([|;,";
            for (int i = 1; i < array.Length; i++)
                if (key.Contains(array[i - 1]))
                    array[i] = char.ToUpper(array[i]);
                else
                    array[i] = char.ToLower(array[i]);

            return new string(array);
        }

        public static string UniConvert(this string value)
        {
            string str = value;
            //ConvertFont convert = new ConvertFont();
            //FontIndex manguon = FontIndex.iNotKnown;
            //FontIndex madich = FontIndex.iUNI;
            //DateTime t = DateTime.Now;
            //convert.Convert(ref str, manguon, madich);

            //if (str.Contains("VƠMOÙNG"))
            //    str = str.Replace("VƠMOÙNG", "VỈ MÓNG");

            return str;
        }

        public static T[] Add<T>(this T[] target, params T[] items)
        {
            // Validate the parameters
            if (target == null)
                target = new T[] { };
            if (items == null)
                items = new T[] { };

            // Join the arrays
            T[] result = new T[target.Length + items.Length];
            target.CopyTo(result, 0);
            items.CopyTo(result, target.Length);
            return result;
        }

        public static void LeftShiftArray<T>(this T[] arr, int shift)
        {
            shift = shift % arr.Length;
            T[] buffer = new T[shift];
            Array.Copy(arr, buffer, shift);
            Array.Copy(arr, shift, arr, 0, arr.Length - shift);
            Array.Copy(buffer, 0, arr, arr.Length - shift, shift);
        }


        public static bool ToBool(this string st)
        {
            return st.Upper() == "YES" || st.Upper() == "ON" || st.Upper() == "Y" || st.Upper() == "TRUE";
        }

        public static string[] _removeProp(this IEnumerable<string> ls, string propname)
        {
            return ls.ToTextObj("|")._removeProp(propname).filter("|");
        }

        public static string[] _allPropNames(this IEnumerable<string> ls)
        {
            return ls.ToTextObj("|")._allPropNames();
        }

        public static int _getIntEndOfString(this string st)
        {
            string numchar = "0123456789";
            string key = st.filter("=").First();
            string res = "";

            for (int i = key.Length - 1; i >= 0; i--)
                if (numchar.Contains(key[i]))
                    res = key[i] + res;
                else
                    break;
            return (int)res.ToNumber();
        }

        public static double ToNumber(this object _content, double default_value = 0)
        {
            double d = default_value;

            if (_content != null)
            {
                string content = _content.ToString().Upper();
                string snum = @"-+0123456789.:/E";

                content = content.Replace("%%P", "");
                content = content.Replace("(", "");
                content = content.Replace(")", "");

                string st = "";
                for (int i = 0; i < content.Length; i++)
                    if (snum.Contains(content[i]))
                        st += content[i];

                //Console.WriteLine("Content {0} st {1}", content, st);

                try
                {
                    if (st.Contains("e"))
                        d = double.Parse(st, System.Globalization.CultureInfo.InvariantCulture);
                    else if (!st.Contains(":") && !st.Contains("/"))
                        d = Convert.ToDouble(st);
                    else
                    {
                        double first = Convert.ToDouble(st.filter(":/").First());
                        double end = Convert.ToDouble(st.filter(":/").Last());

                        //Console.WriteLine("Content {0} first {1} end {2}", content, first, end);

                        if (end != 0)
                            d = first / end;
                        else
                            d = default_value;
                    }
                }
                catch (System.Exception ex)
                {
                    //d = default_value;
                }
            }
            return d;
        }


        public static string _firstProp(this string st)
        {
            string key = st._firstPropName();
            return !key.empty() ? st._prop(key) : null;
        }

        public static string _firstPropName(this string st)
        {
            string[] ar = st.filter("=");
            return ar.Length > 0 ? st.filter("=").First() : null;
        }


        public static string[] _allVariables(this string st)
        {
            return st._allPropNames().Where(w => !w.StartsWith("#")
                && w != st._firstPropName() && !w.st_("TEXT")
                && !w.st_("CONTENT") && w.Length <= 4).ToArray();
        }

        public static string _allVariableAndValues(this string st, params string[] excludes)
        {
            string res = "";

            foreach (string v in st._allVariables())
                if (!excludes.Any(s => s.Upper() == v.Upper()) && !st._prop(v).empty())
                    res += "|" + v + "=" + st._prop(v);

            return res;
        }

        public static string[] _getPramsHeaders(this IEnumerable<string> str)
        {
            List<string> headers = new List<string>();

            foreach (string st in str)
                headers.AddRange(st.filter("|")
                    .Where(word => word.Contains("=")).Select(word => word.filter("=")[0].Upper())
                    .Where(w => !headers.Contains(w)).Select(w => w));

            return headers.ToArray();
        }


        public static string[] _setUserListProp(this string src, string prop, bool overwrite = false)
        {
            List<string> res = new List<string>();

            //Console.WriteLine("[VARS]{0}", src._allVariables().ToText(";"));

            foreach (string name in src._allVariables())
                prop = prop._setprop(name, src._prop(name));

            string[] names = prop._allPropNames();

            foreach (string name in names)
                src = src._removeProp(name);

            int total = 1;

            if (!prop.empty())
                while (total != 0 && res.Count != total)
                {
                    total = res.Count;
                    string[] ls = res.Count == 0 ? new string[] { src } : res.ToArray();
                    res = new List<string>();

                    foreach (string st in ls)
                    {
                        string[] param_list = prop._getParamArrayList();

                        if (param_list.Length > 0)
                            foreach (string pram in param_list)
                            {
                                string new_st = st;

                                foreach (string name in names)
                                    if (overwrite)
                                        new_st = new_st._setprop(name, pram._prop(name));
                                    else if (new_st._prop(name).empty())
                                        new_st += "|" + name + "=" + pram._prop(name);

                                res.Add(new_st);
                            }
                    }
                }

            if (res.Count == 0)
                res = new List<string> { src };
            return res.ToArray();
        }


        public static string _propchar(this string _src, char _key)
        {
            return _src._prop(_key.ToString());
        }

        public static string[] _allProps(this string _src, string sfilter = "|")
        {
            string[] res = new string[0];

            if (!_src.empty())
                res = _src.filter(sfilter).Where(s => s.filter("=").Length > 1)
                    .Select(s => s.filter("=").Last()).ToArray();

            return res;
        }

        public static string[] _allPropNames(this string _src, string sfilter = "|")
        {
            string[] res = new string[0];

            if (!_src.empty())
                res = _src.filter(sfilter).Where(s => s.filter("=").Length > 1)
                    .Select(s => s.filter("=").First()).ToArray();

            return res;
        }

        public static string _prop(this string _src, string _key)
        {
            string sfilter = "|";
            if (_src.empty()) return null;
            string res = null;

            string key = _key;
            string[] ar = _src.filter(sfilter);

            int index = Array.FindIndex(ar, st => st.filter("=").First().Upper() == key.Upper());

            if (index != -1)
            {
                res = ar[index].Substring(ar[index].IndexOf("=") + 1).Trim();

                if ((res.StartsWith("<") && res.EndsWith(">"))
                    || res.StartsWith("{") && res.EndsWith("}"))
                {
                    ar = res.Substring(1, res.Length - 2).filter(",");
                    index = Array.FindIndex(ar, s => s.StartsWith("!"));
                    res = index != -1 ? ar[index].Substring(1) : (ar.Length > 0 ? ar.First() : null);
                }
            }
            else
                res = null;

            return res;
        }

        public static string _props(this IEnumerable<string> _src, string _key)
        {
            string res = null;
            foreach (string st in _src)
            {
                res = st._prop(_key);
                if (res != null)
                    break;
            }
            return res;
        }

        public static string _removeProp(this string _src, string _key)
        {
            string res = "";
            string[] ar = _src.filter("|");
            for (int i = 0; i < ar.Length; i++)
                if (!ar[i].st_(_key.Upper() + "="))
                    res += ar[i] + "|";
            if (_src.StartsWith("|"))
                res = "|" + res;
            return res;
        }


        public static bool empty(this string st)
        {
            return st == null || st.Trim() == "";
        }


        public static string _setprop(this string _src, string _key, string _value)
        {
            string res = "";


            if (_src._prop(_key).empty())
                res = _src + (_src.EndsWith("|") ? "" : "|") + _key + "=" + _value;
            else
            {
                int index = _src.ToUpper().StartsWith(_key.ToUpper() + "=") ? 0
                    : _src.ToUpper().IndexOf("|" + _key.ToUpper() + "=") + 1;

                int break_index = _src.IndexOf("|", index + 1);

                //Console.WriteLine("[Key]{0}[First]{1}[Index]{2}",
                //    _key, _src.Substring(0, index + _key.Length + 1), index);
                //Console.WriteLine("Last:{0}", (break_index == -1 ? "" : _src.Substring(break_index)));

                res = _src.Substring(0, index + _key.Length + 1)
                    + _value + (break_index == -1 ? "" : _src.Substring(break_index));
            }
            return res;
        }

        public static string[] _setprops(this IEnumerable<string> _src, string _key, string _value)
        {
            List<string> src = _src.ToList();
            int index = src.FindIndex(s => s._prop(_key) != null);

            if (index != -1)
                src[index] = _key + "=" + _value;
            else
                src.Add(_key + "=" + _value);

            return src.ToArray();
        }


        public static double roundNumber(this double v, double round_number = 1)
        {
            return Math.Round(v / round_number) * round_number;
        }

        public static string[] filter(this string st, string key = ",;|\r\n")
        {
            if (st.empty())
                return new string[0];
            List<char> keys = new List<char>();
            for (int i = 0; i < key.Length; i++)
                keys.Add(key[i]);
            string[] res = st.Split(keys.ToArray()).Cast<string>()
                .Where(ch => !ch.empty()).Select(ch => ch.Trim()).ToArray();
            return res.Length == 0 ? new string[] { st } : res;
        }

        public static bool _startWithNumber(this string st)
        {
            if (st.empty())
                return false;
            string nums = "0123456789";
            return nums.Contains(st[0]);
        }

        public static bool IsUpper(this string st)
        {
            return st.ToUpper() == st;
        }

        public static string Upper(this string st)
        {
            string res = "";

            if (!st.empty())
                for (int i = 0; i < st.Length; i++)
                    if (st[i] != ' ') res += st[i].ToString().ToUpper();

            return res;
        }


        static string[] _getParamArrayList(this string src)
        {
            string[] props = src._allProps();
            List<string> res = new List<string>();

            if (props.Length > 0)
            {
                int total = src._allProps().Max(s => s.filter("&").Length);

                string[] names = src._allPropNames();
                List<string[]> param_list = names.Select(s => new string[total]).ToList();

                for (int i = 0; i < names.Length; i++)
                {
                    string[] ar = src._prop(names[i]).filter("&");

                    for (int j = 0; j < total; j++)
                        param_list[i][j] = j < ar.Length ? ar[j] : ar.Last();
                }

                if (param_list.Count > 0)
                    for (int j = 0; j < param_list.First().Length; j++)
                    {
                        string st = "";
                        for (int i = 0; i < names.Length; i++)
                            st += "|" + names[i] + "=" + param_list[i][j];
                        res.Add(st);
                    }
            }
            return res.ToArray();
        }

        public static string _getBeforeComma(this string st, string comma = "()")
        {
            string res = null;

            if (st.Contains(comma.First()) && st.Contains(comma.Last()))
            {
                int index1 = st.Length - st.Invert().IndexOf(comma.First()) - 1;
                int index2 = st.Length - st.Invert().IndexOf(comma.Last()) - 1;

                res = st.Substring(0, index1);
            }
            return res;
        }


        public static bool _isValidEquation(this string st)
        {
            string valids = "0123456789 -+*/^";
            return st.All(c => valids.Contains(c));
        }

        public static string Invert(this string text)
        {
            if (text == null)
                return null;

            // this was posted by petebob as well 
            char[] array = text.ToCharArray();
            Array.Reverse(array);
            return new String(array);
        }
        public static string _removeFirstProp(this string st)
        {
            int index = st.IndexOf("|");
            return st.Substring(index + 1);
        }

        public static string _getInComma(this string st, string comma = "()")
        {
            string res = null;

            if (st.Contains(comma.First()) && st.Contains(comma.Last()))
            {
                int index1 = st.Length - st.Invert().IndexOf(comma.First()) - 1;
                int index2 = st.Length - st.Invert().IndexOf(comma.Last()) - 1;

                res = st.Substring(index1 + 1, index2 - index1 - 1);
            }
            return res;
        }

        public static Dictionary<string, string[]> GetChapters(this IEnumerable<string> str)
        {
            Dictionary<string, string[]> res = new Dictionary<string, string[]>();
            List<string> ls = new List<string>();
            string chapter = "DATA";

            foreach (string st in str)
                if (!st.empty())
                {
                    if (st.st_("CHAPTER="))
                    {
                        if (ls.Count > 0)
                            res.Add(chapter, ls.ToArray());

                        chapter = st._firstProp();
                        ls = new List<string>();
                    }
                    else if (st.st_("[") && st.Upper().EndsWith("]"))
                    {
                        if (ls.Count > 0)
                            res.Add(chapter, ls.ToArray());

                        chapter = st._getInComma("[]");
                        ls = new List<string>();
                    }
                    else if (!st.StartsWith("---"))
                    {
                        ls.Add(st);
                    }
                }

            if (!chapter.empty())
                res.Add(chapter, ls.ToArray());

            return res;
        }

        public static string[] ReplaceChapter(this IEnumerable<string> str, string chapter_name, IEnumerable<string> chapter_contents)
        {
            List<string> res = new List<string>();
            var dict = str.GetChapters();

            int index = dict.Keys.ToList().FindIndex(s => s.Upper() == chapter_name.Upper());

            if (index != -1)
                dict[dict.Keys.ElementAt(index)] = chapter_contents.ToArray();
            else
                dict.Add(chapter_name, chapter_contents.ToArray());

            dict = dict.OrderBy(itm => itm.Key).ToDictionary(itm => itm.Key, itm => itm.Value);

            return dict.ToChapter();
        }

        public static string[] ToChapter(this Dictionary<string, string[]> dict)
        {
            List<string> res = new List<string>();
            foreach (string key in dict.Keys)
            {
                res.Add("----------------------------------------------------------------");
                res.Add("Chapter=" + key);
                res.Add("-----------------");
                res.AddRange(dict[key]);
            }

            return res.ToArray();
        }


        public static Dictionary<string, string> ToDict(this string st)
        {
            Dictionary<string, string> res = new Dictionary<string, string>();
            string[] str = st._allPropNames().OrderBy(s => -s.Length).ThenBy(s => s).ToArray();

            foreach (string s in str)
                if (!res.Keys.Contains(s))
                    res.Add(s, st._prop(s));

            return res;
        }


        public static string ToTextStr(this IEnumerable<string> contents, string seperator = ",")
        {
            string res = "";

            if (contents != null && contents.Count() > 0)
            {
                foreach (string st in contents)
                    res += st == null ? "" : st + seperator;
                //res = res.EndsWith(seperator) ? res.Substring(0, res.Length - 1) : res;
            }
            return res.EndsWith(seperator) ? res.Substring(0, res.Length - 1) : res; 
        }

        public static string ToTextDouble(this IEnumerable<double> objs, string seperator)
        {
            string res = "";
            foreach (double obj in objs)
                res += obj.ToString() + seperator;
            //if (res.Length > 0) res = res.Substring(0, res.Length - 1);
            return res.EndsWith(seperator) ? res.Substring(0, res.Length - 1) : res;
        }
        public static string ToTextInt(this IEnumerable<int> objs, string seperator = ",")
        {
            string res = "";
            foreach (int obj in objs)
                res += obj.ToString() + seperator;
            
            return res.EndsWith(seperator) ? res.Substring(0, res.Length - 1) : res;
        }

        public static string ReplaceFomular(this string st)
        {
            string ks = "|;,";
            string[] ar = st.filter(ks);
            string valid_keys = " 0123456789^+-*/().";

            Dictionary<string, string> dicts = new Dictionary<string, string>();

            foreach (string s in ar)
                if (!dicts.Keys.Contains(s) && s.Upper().All(ch => valid_keys.Contains(ch)))
                {
                    object o = null;

                    try
                    {
                        System.Data.DataTable dt = new System.Data.DataTable();
                        o = dt.Compute(s, "");
                    }
                    catch
                    {
                        o = s;
                    }

                    if (o != null)
                        dicts.Add(s, o.ToString());
                }

            dicts = dicts.OrderBy(itm => -itm.Key.Length)
                .ThenBy(itm => itm.Key.Upper()).ToDictionary(itm => itm.Key, itm => itm.Value);

            foreach (string k in dicts.Keys)
                st = st.Replace(k, dicts[k]);

            return st;
        }

        public static string Replace(this string st, Dictionary<string, string> values)
        {
            if (!st.empty())
                for (int i = 0; i < values.Count(); i++)
                    st = st.Replace(values.Keys.ElementAt(i), values[values.Keys.ElementAt(i)]);
            return st;
        }

        public static string ReplaceEquation(this string src, bool inorge_first_prop = false)
        {
            string[] keys = src._allPropNames();
            string first = src._firstProp();

            Dictionary<string, string> dicts = new Dictionary<string, string>();

            isRectByVerts = false;

            if (!src._prop("RECT").empty())
            {
                isRectByVerts = true;
                src = src.Replace("|RECT", "|VERTS");
            }

            string key_sperates = "`~!$&,<>;\"?\\=[]{}|";
            //Console.WriteLine("[STEP1]{0}\r\nPROP_VIEW:{1}", src, src._prop("VIEW"));
            string res = src._processWord(key_sperates + "()-+*/ ")
                ._processWord(key_sperates, ReplaceFomular)
                .Replace("`", "").Trim();

            if (inorge_first_prop)
                res = res.Replace(res._firstProp(), first);
            return res;
        }

        public static string _prevChar(this string st, int index)
        {
            while (index >= 0 && st[index] == ' ')
                index--;

            return index == -1 ? null : st[index].ToString();
        }

        public static string _nexChar(this string st, int index)
        {
            while (index < st.Length && st[index] == ' ')
                index++;

            return index >= st.Length ? null : st[index].ToString();
        }

        static string _processWord(this string st, string key_seperates, Func<string, string> fn = null)
        {
            string res = "", word = "";
            string[] propnames = st._allPropNames().OrderBy(s => -s.Length).ToArray();

            for (int i = 0; i < st.Length; i++)
                if (key_seperates.Contains(st[i]))
                {
                    if (!word.empty())
                    {
                        if (i > word.Length && st[i - word.Length - 1] == '`' && st[i] == '`')
                        {
                            res += word;
                        }
                        else
                            res += fn == null ?
                                (i > 0 && st._nexChar(i) != "=" && !st._prop(word).empty() ? st._prop(word) : word)
                                : fn(word);
                    }

                    word = "";
                    res += st[i];
                }
                else
                    word += st[i];

            if (!word.empty())
                res += !st._prop(word).empty() ? st._prop(word) : word;

            return res;
        }

        public static string[] _extractListParam(this string st)
        {
            List<string> res = new List<string>();

            string[] keys = st._allPropNames();
            int total = keys.Where(k => k.Upper() != "BY").Max(k => st._prop(k).filter(";,").Length);

            string current_step = "";
            //bool isRect = false;

            for (int i = 0; i < total; i++)
            {
                string content = current_step.empty() ? "" : ("|MOVE=" + current_step);

                foreach (string k in keys)
                {
                    string val = st._prop(k);
                    if (k.Upper() != "BY")
                    {
                        string[] ar = val.filter(";,");
                        string s = ar.Length > i ? ar[i] : ar.Last();

                        content += "|" + k + "=" + s;
                    }
                    else
                    {
                        if (!current_step.empty())
                            current_step = val.filter(",").First() + "+" + current_step.filter(",").First() + ","
                                + val.filter(",").Last() + "+" + current_step.filter(",").Last();
                        else
                            current_step = val;


                    }
                }

                res.Add(content);
                current_step = current_step.ReplaceEquation().Replace(";", "");
            }

            return res.ToArray();
        }
    }
}
