using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;
using AcadScript;
//using Microsoft.CodeAnalysis.CSharp.Scripting;
//using Microsoft.CodeAnalysis.Scripting;

namespace AcadScript
{
    public class LibraryConfig
    {
        public static string FolderPath
        {
            get
            {
                var basePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                return Path.Combine(basePath, "Autodesk", "3dsMax", "Samples", "CSharpAcadScript");
            }
        }

        public static string FilePath
        {
            get
            {
                return Path.Combine(FolderPath, "config.xml");
            }
        }

        public static Config Default = new Config();

        public static string ApplicationFolder
        {
            get
            {
                //ACD.ED.WriteMessage("OK_CONFIG\r\n");
                //ACD.ED.WriteMessage("ASSEMBLY {0}", Assembly.GetEntryAssembly().Location);

                string dir = pString.INI_String("ACAD_ASSEMBLY_DIR");

                return dir;// Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            }
        }

        public static string ScriptPath
        {
            get
            {
                return (BinDir + @"\AcadScript").FindLatestFileVersion(".DLL");
            }
        }

        public static void WR(string message, params object[] contents)
        {
            var ED = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
            string st = String.Format(message, contents);

            ED.WriteMessage(st + "\r\n");
            string fname = @"D:\CADinfo.txt";

            if (System.IO.File.Exists(fname))
                System.IO.File.AppendAllText(fname, st);
            else
                System.IO.File.WriteAllText(fname, st);
        }

        public static string BinDir
        {
            get
            {
                return Path.Combine(ApplicationFolder, "bin", "assemblies");
            }
        }

        public string[] Assemblies
        {
            get
            {
                List<string> res = new List<string> { "System.dll",
                    "System.Core.dll",
                    "System.Data.dll",
                    "System.Data.DataSetExtensions.dll",
                    "System.Drawing.dll",
                    "System.Windows.Forms.dll",
                    "System.Xml.dll",
                    "System.Xml.Linq.dll",
                    "System.Xaml.dll",
                    "System.Numerics.dll", "System.Runtime.dll","System.Collections.dll"};

                List<string> dlls = pString.INI_String("ACAD_DLL_LIST").filter(",")
                    .Select(dll => Path.Combine(BinDir, dll.EndsWith(".dll") ? dll : (dll + ".dll"))).ToList();
                dlls.Add((BinDir + @"\AcadConsole").FindLatestFileVersion(".DLL"));

                foreach (string dll in dlls)
                {
                    //string file = Path.Combine(BinDir, dll.EndsWith(".dll") ? dll : (dll + ".dll"));
                    if (File.Exists(dll))
                        res.Add(dll);
                    else
                        WR("DLL not exist {0}", dll);
                }

                return res.ToArray();
            }
        }
    }

    public static class ASRun
    {
        public static CompilerResults CompResult;
        public static System.Windows.Forms.RichTextBox OutputText;

        public static string FindLatestFileVersion(this string file, string fileExt)
        {
            string fileDir = Path.GetDirectoryName(file + fileExt);
            string st = Path.GetFileName(file + fileExt);
            string fileKeX = Path.GetFileNameWithoutExtension(st);
            string res = null;
            string[] lsFile = Directory.GetFiles(fileDir, fileKeX + "*" + fileExt);

            if (lsFile.Length > 0)
            {
                lsFile = lsFile.OrderBy(f => (new FileInfo(f)).LastWriteTime.ToFileTime()).ToArray();
                res = lsFile.Last();
            }

            return res;
        }
        public static void Output(string s)
        {
            if(OutputText != null)
                OutputText.AppendText(s);

            File.AppendAllText(@"D:\acadlog.txt", s + "\n");
        }

        public static void OutputLine(string s = "")
        {
            Output(s);
            Output("\n");
        }

        public static string UniqueClassName(string content)
        {
            string main_line = "public static void Main(string[] args)";
            string res = content;
            string[] _contents = res.filter("\r\n");

            int index = DE.NumericArray(0, _contents.Length - 1).FirstOrDefault(i 
                => _contents[i].Contains(main_line));

            if (index != 0)
            {
                index = DE.NumericArray(0, index - 1).Reverse().FirstOrDefault(i 
                    => _contents[i].Contains("class") && _contents[i].Contains("public"));

                if (index != 0)
                {
                    _contents[index] = _contents[index].TrimEnd() + "_1";
                    res = _contents.ToTextStr("\r\n");
                }
            }

            return res;
        }

        public static void CompileAndRun(string content)
        {
            //var parameters = new CompilerParameters(new[] { "mscorlib.dll", "System.Core.dll", "System.dll", "DS_Server.dll" }, "DS Server.exe", true);
            //ProviderOptions setting = new ProviderOptions();
            //setting.AllOptions
            //ASLib.LoadMethod("ACD", "Echo", "Hello World A! {0} + {1}", new object[] { "L", "A" });

            //Replace class name with extension _1
            content = UniqueClassName(content);

            System.IO.File.WriteAllText(@"D:\tmp.cs", content);


            CodeDomProvider provider = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", "v4.0" } });
            OutputLine("-= Executing C# script =-");

            try
            {
                var config = Config.Default;

                //OutputLine(fileins.ToText("\r\n"));

                //foreach (string f in fileins)
                //    if(File.Exists(f))
                //        content += "\r\n" + File.ReadAllText(f);

                //Config.WR("ASSM {0}", config.Assemblies.Length);

                List<string> dirs = config.Assemblies.ToList();
                //dirs.Add(@"D:\library.dll");

                CompResult = DotNetCompilers.Compile(provider, dirs, content);
                //CompResult.CompiledAssembly.CreateInstance("");
                File.WriteAllText(@"D:/html/acadscript.cs", content);

                if (CompResult.Errors.Count > 0)
                {
                    OutputLine("Compilation failed");
                    int count = 0;

                    foreach (var e in CompResult.Errors)
                    {
                        count++;
                        OutputLine(String.Format("{0}.Compilation error : {1}", count, e.ToString()));
                    }
                    return;
                }
                else
                {
                    OutputLine("Compilation succeeded");
                    DotNetCompilers.RunMain(CompResult.CompiledAssembly);
                }
            }
            catch (System.Exception ex)
            {
                OutputLine("Exception occurred " + ex.Message + " " + ex.StackTrace);
            }
            OutputLine("-= Completed executing script =-");
        }

        public static string OpenScript(string fileName,
            string additon_dir = @"D:\Dropbox\VS Projects\ACadScript\Public\")
        {
            List<string> files = new List<string> { fileName };
            
            files.AddRange(Directory.GetFiles(additon_dir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !Path.GetFileNameWithoutExtension(f).StartsWith("_")));

            //string app_dir = @"D:\Dropbox\VS Projects\ACadScript\";

            //string[] fileins = File.ReadAllLines(fileName)
            //    .Where(s => s.Trim().StartsWith("//fileIn "))
            //    .Select(s => app_dir + s.Trim()
            //    .Replace("//fileIn ", "").Replace(".", "\\") + ".cs").ToArray();

            //files.AddRange(fileins);
            
            OutputText.Clear();
            OutputLine(files.Select(f => Path.GetFileNameWithoutExtension(f)).ToTextStr("\r\n"));

            List<string> res = new List<string>();
            List<string> using_list = new List<string>();

            foreach (string f in files)
                if(File.Exists(f))
                {
                    string[] str = File.ReadAllLines(f);

                    for (int i = 0; i < str.Length; i++)
                        if (str[i].StartsWith("using"))
                            using_list.Add(str[i]);
                        else
                            res.Add(str[i]);
                }

            using_list = using_list.Distinct().ToList();
            using_list.AddRange(res);
            res = using_list;

            //ACD.WR("OK");

            File.WriteAllLines(@"D:\publiclog.cs", res.ToArray());
            return res.ToTextStr("\r\n");

            //FileName = fileName;
        }

    }

    public class Config
    {
        public static string FolderPath
        {
            get
            {
                var basePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                return Path.Combine(basePath, "Autodesk", "3dsMax", "Samples", "CSharpAcadScript");
            }
        }

        public static string FilePath 
        {
            get 
            {   
                return Path.Combine(FolderPath, "config.xml");
            }
        }

        public static Config Default = new Config();

        //public static Config Load()
        //{
        //    var xs = new System.Xml.Serialization.XmlSerializer(typeof(Config));

        //    try
        //    {
        //        if (!File.Exists(FilePath))
        //        {
        //            var r = new Config();
        //            Directory.CreateDirectory(FolderPath);
        //            var stream = File.OpenWrite(FilePath);
        //            xs.Serialize(stream, r);
        //            return r;
        //        }
        //        else
        //        {
        //            var stream = File.OpenRead(FilePath);
        //            var r = xs.Deserialize(stream) as Config;
        //            return r;
        //        }
        //    }
        //    catch (System.Exception)
        //    {
        //        return new Config();
        //    }
        //}

        public static string ApplicationFolder
        {
            get
            {
                //ACD.ED.WriteMessage("OK_CONFIG\r\n");
                //ACD.ED.WriteMessage("ASSEMBLY {0}", Assembly.GetEntryAssembly().Location);

                string dir = pString.INI_String("ACAD_ASSEMBLY_DIR");

                return dir;// Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            }
        }
        
        //public static string ScriptPath
        //{
        //    get
        //    {
        //        return (BinDir + @"\AcadScript").FindLatestFileVersion(".DLL");
        //    }
        //}

        public static void WR(string message, params object[] contents)
        {
            var ED = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
            string st = String.Format(message, contents);

            ED.WriteMessage(st + "\r\n");
            string fname = @"D:\CADinfo.txt";

            if (System.IO.File.Exists(fname))
                System.IO.File.AppendAllText(fname, st);
            else
                System.IO.File.WriteAllText(fname, st);
        }

        public static string BinDir
        {
            get
            {
                return Path.Combine(ApplicationFolder, "bin", "assemblies");
            }
        }

        public string[] Assemblies
        {
            get
            {
                List<string> res = new List<string> { "System.dll",
                    "System.Core.dll",
                    "System.Data.dll",
                    "System.Data.DataSetExtensions.dll",
                    "System.Drawing.dll",
                    "System.Windows.Forms.dll",
                    "System.Xml.dll",
                    "System.Xml.Linq.dll",
                    "System.Xaml.dll",
                    "System.Numerics.dll", "System.Runtime.dll","System.Collections.dll","System.Web.dll"};

                List<string> dlls = pString.INI_String("ACAD_DLL_LIST").filter(",")
                    .Select(dll => Path.Combine(BinDir, dll.EndsWith(".dll") ? dll : (dll + ".dll"))).ToList();
                dlls.Add((BinDir + @"\AcadScript").FindLatestFileVersion(".DLL"));

                foreach (string dll in dlls)
                {
                    //string file = Path.Combine(BinDir, dll.EndsWith(".dll") ? dll : (dll + ".dll"));
                    if (File.Exists(dll))
                    { 
                        res.Add(dll);
                    //WR("DLL:{0}", dll);
                    }
                    else
                        WR("not exist {0}", dll);
                }

                return res.ToArray();
            }
        }

        static object getProperty(AppDomain ad, string clsname, string method)
        {
            Assembly[] assemblies = ad.GetAssemblies();
            object res = null;

            foreach (Assembly assm in assemblies)
            {
                var tp = assm.GetType(clsname);
                if (tp != null)
                {
                    res = tp.InvokeMember(method,
                                          BindingFlags.Public |
                                          BindingFlags.Static |
                                          BindingFlags.GetProperty,
                                          null,
                                          null,
                                          null);
                }
            }

            return res;
        }

        static dynamic runMethod(AppDomain ad, string clsname, string method, object[] args, int index = -1)
        {
            Assembly[] assemblies = ad.GetAssemblies();
            dynamic res = null;

            foreach (Assembly assm in assemblies)
            {
                Type tp = assm.GetType(clsname);
                

                if (tp != null)
                {
                    //ACD.WR("Method [{0}] getmethod: {1}", method, tp.GetMethod(method));
                    if (index == -1)
                        res = tp.InvokeMember(method,
                                              BindingFlags.Public |
                                              BindingFlags.Static |
                                              BindingFlags.InvokeMethod,
                                              null,
                                              null,
                                              args);
                    else
                    {
                        MethodInfo clist = tp.GetMethods().Where(c => c.Name == method).ElementAt(index);

                        ACD.WR("Method [{0}] params: {1}", method, 
                            clist.GetParameters().Select(p => p.ParameterType.Name).ToTextStr(","));

                        clist.Invoke(null, args);
                    }
                }
            }

            return res;
        }

        static dynamic newPos(AppDomain ad, string f, object[] args)
        {
            //args = new object[] { null };
            return ad.CreateInstanceFromAndUnwrap(f, typeof(pPos).FullName, true,
                            BindingFlags.Default, null, args, System.Globalization.CultureInfo.CurrentCulture, new object[] { });
        }

        //static dynamic newPosList(AppDomain ad, string f, object[] args)
        //{
        //    //args = new object[] { null };
        //    return ad.CreateInstanceFromAndUnwrap(f, typeof(List<pPos>).FullName, true,
        //                    BindingFlags.Default, null, args, System.Globalization.CultureInfo.CurrentCulture, new object[] { });
        //}

        //public static object LoadMethod(object obj, string method_name, params object[] theArguments)
        //{
        //    object res = null;

        //    MethodInfo[] methodInfos = obj.GetType().GetMethods();
        //    List<string> typenames = new List<string>();

        //    for (int i = 0; i < theArguments.Length; i++)
        //        typenames.Add(theArguments[i].GetType().Name.ToUpper());
            
        //        //ACD.WR("Method [{0}] params: {1}", method_name, typenames.ToTextStr(","));

        //        foreach (MethodInfo c in methodInfos)
        //            if (c.Name.ToUpper() == method_name.ToUpper())
        //            {
        //                ParameterInfo[] pi = c.GetParameters();
        //                bool b = true;
        //                string s = "";

        //                for (int i = 0; i < pi.Length; i++)
        //                {
        //                    s += (pi[i].ParameterType.Name + " " + pi[i].Name);

        //                    if (i + 1 < pi.Length)
        //                        s += (", ");

        //                    if (i >= typenames.Count || (typenames[i] != null
        //                        && pi[i].ParameterType.Name.ToUpper() != typenames[i]))
        //                    {
        //                        b = false;
        //                        break;
        //                    }
        //                }

        //                s = "[" + b + "]" + s;
        //                //ACD.WR("Params: {0}", s);

        //                if (b)
        //                {
        //                    //ACD.WR("R1");
        //                    res = c.Invoke(obj, theArguments);
        //                    //ACD.WR("R2");
        //                }
        //            }

        //    return res;
        //}


        public static void LoadAcadScriptAssembly()
        {
            using (ACD.Lock())
            {
                AppDomain currentDomain = AppDomain.CurrentDomain;
                AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

                string f = (BinDir + @"\AcadScript").FindLatestFileVersion(".DLL");
                AppDomainSetup domainSetup = new AppDomainSetup { PrivateBinPath = f };

                AppDomain ad = AppDomain.CreateDomain("New domain", null, domainSetup);

                //var args = new object[] { "3000,100,0" };

                //dynamic p1 = newPos(ad, f, new object[] { 3000,100,0,null }); //ad.CreateInstanceFromAndUnwrap(f, typeof(pPos).FullName, true, 
                                                                           //BindingFlags.Default, null, args, System.Globalization.CultureInfo.CurrentCulture,new object[] { } );

                //args = new object[] {null};
                //dynamic p2 = newPos(ad, f, new object[] { 0,0,0,null });// ad.CreateInstanceFromAndUnwrap(f, typeof(pPos).FullName, true,
                                                                  //BindingFlags.Default, null, args, System.Globalization.CultureInfo.CurrentCulture, new object[] { });

                //Type arrayType = p1.GetType().MakeArrayType();


                //Array ls = Array.CreateInstance(p1.GetType(), 3);
                ////for (int i = my1DArray.GetLowerBound(0); i <= my1DArray.GetUpperBound(0); i++)
                //ls.SetValue(p1, 0);
                //ls.SetValue(p2, 1);
                //ls.SetValue(p1 + p2, 2);

                //Type t = p1.GetType();
                ////ACD.WR("Type {0}", t.Name);

                //try
                //{
                //    pPos[] r = p1.RectToPoint(p2);

                //    //ACD.WR("Item {0},[{1},{2},{3},{4}]", r.Length, r[0],r[1],r[2],r[3]);

                //runMethod(ad, "AcadScript.ACD", "WR", new object[] { "{0}+{1}={2}", p1, p2, p1 + p2 });

                //var db = (Autodesk.AutoCAD.DatabaseServices.Database)getProperty(ad, "AcadScript.ACD", "DB");

                //}
                //catch (TargetInvocationException e)
                //{
                //    ACD.WR("Error:{0}", e.InnerException);
                //}
               
                if (ad != null)
                {
                    AppDomain.Unload(ad);
                    ad = null;
                }
            }
        }


        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                Assembly assembly = System.Reflection.Assembly.Load(args.Name);
                if (assembly != null)
                    return assembly;
            }
            catch
            { // ignore load error
            }

                // *** Try to load by filename - split out the filename of the full assembly name
                // *** and append the base path of the original assembly (ie. look in the same dir)
                // *** NOTE: this doesn't account for special search paths but then that never
                //           worked before either.
                string[] Parts = args.Name.Split(',');
                string File = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + Parts[0].Trim() + ".dll";

            return System.Reflection.Assembly.LoadFrom(File);
        }

    }

    //public interface IExampleProxy
    //{
    //    void Proxy(string st);
    //}

    //public class Example : MarshalByRefObject, IExampleProxy
    //{
    //    public double X = 0, Y = 0, Z = 0;
    //    public string Content;
    //    public double Rotation;

    //    public void Proxy(string st)
    //    {

    //    }

    //    public double this[int index]
    //    {
    //        get
    //        {
    //            double res = 0;
    //            switch (index)
    //            {
    //                case 0: res = this.X; break;
    //                case 1: res = this.Y; break;
    //                case 2: res = this.Z; break;
    //            }
    //            return res;
    //        }
    //        set
    //        {
    //            switch (index)
    //            {
    //                case 0: this.X = value; break;
    //                case 1: this.Y = value; break;
    //                case 2: this.Z = value; break;
    //            }
    //        }
    //    }

    //    public string HelloWorld(string name)
    //    {
    //        //ACD.WR("OKL");
    //        return $"Hello '{ name }'";
    //    }

    //    public Example(string st)
    //    {
    //        if (!st.empty())
    //        {
    //            Content = st._getInComma("[]");

    //            string[] ar = st.filter("<[]");

    //            if (st.Contains("<"))
    //            {
    //                Rotation = ar[1].ToNumber();
    //                st = ar.First();
    //            }

    //            ar = ar.First().filter("[],");
    //            this.X = this.Y = double.PositiveInfinity;
    //            this.Z = 0;

    //            if (ar.Length >= 2)
    //            {
    //                this[0] = ar[0].ToNumber();
    //                this[1] = ar[1].ToNumber();

    //                if (ar.Length >= 3)
    //                    this[2] = ar[2].ToNumber();
    //            }

    //        }
    //    }
    //}

}
