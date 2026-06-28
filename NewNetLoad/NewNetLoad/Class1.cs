using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Windows;

using System.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LoadModule
{
    class ApplicationProxy : MarshalByRefObject
    {
        public void DoSomething(string DllFile, string cmd)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            Assembly oldVersion = Assembly.Load(new AssemblyName()
            {
                CodeBase = DllFile
            });

            doc.SendStringToExecute(cmd, true, false, false);
            ed.WriteMessage("\nLoading assembly: {0} successfull\n", DllFile);
        }
    }

    public class Worker : MarshalByRefObject
    {
        public void PrintDomain(string DllFile, string cmd)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            Assembly oldVersion = Assembly.Load(new AssemblyName()
            {
                CodeBase = DllFile
            });

            doc.SendStringToExecute(cmd, true, false, false);
            ed.WriteMessage("\nLoading assembly: {0} successfull\n", DllFile);
            ed.WriteMessage("Object is executing in AppDomain \"{0}\"",
                AppDomain.CurrentDomain.FriendlyName);
        }
    }

    public class Commands
    {
        static string CreateUniqueDir(string logPath, string logDirName)
        {
            Directory.CreateDirectory(logPath);
            int fileNumber = 0;
            
            while (Directory.Exists(logPath + @"\" + logDirName + "_" + (fileNumber != 0? fileNumber.ToString():"")))
                fileNumber++;

            return (logPath + @"\" + logDirName + "_" + (fileNumber != 0 ? fileNumber.ToString() : ""));
        }

        static string FindLatestDLL(string logFile)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            string fileExt  = ".DLL";
            string fileDir  = Path.GetDirectoryName(logFile + fileExt);
            string st = Path.GetFileName(logFile + fileExt);
            string fileKey = st.Substring(0,st.IndexOf(".") - 1);

            List<string> lsFile = Directory.GetFiles(fileDir, fileKey + "*" + fileExt).ToList();

            if (lsFile.Count > 0)
            {
                lsFile = lsFile.OrderBy(f => (new FileInfo(f)).LastWriteTime.ToFileTime()).ToList();
                return lsFile[lsFile.Count - 1];
            }

            return null;
        }

        static string CopyDLLToTempFolder(string source)
        {
            string dir = Path.GetTempPath();
            string file = Path.GetDirectoryName(source) + @"\" + Path.GetFileNameWithoutExtension(Path.GetTempFileName());
            
            File.Copy(source, file);
            return file;
        }

        static string ReadHistory(string fdata, string newdir, out string cmd)
        {
            string[] arLine = File.ReadAllLines(fdata);
            string path = Path.GetDirectoryName(fdata) + @"\";
            string fname = null;
            cmd = null;

            foreach (string st in arLine
                .Where(st => st.StartsWith(">")))
            {
                string[] arCmd = st.Trim().Substring(1).Split('>');

                if (arCmd.Length >= 2)
                {
                    fname = arCmd[0];
                    cmd = "._" + arCmd[1].Trim() + " ";
                }
                break;
            }

            return fname;
        }

        [CommandMethod("NL")]
        static public void MyNetLoad()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            string cmd = null;

            string sConfigLink = @"D:\Dropbox\VS Projects\VSLink.txt";

            if(!File.Exists(sConfigLink))
                sConfigLink = @"D:\VS Share\VSLink.txt";

            string DLLFile = ReadHistory(sConfigLink,@"D:\Release",out cmd);
            
            if (DLLFile != null && cmd != null)
            {
                try
                {
                    string fname = CopyDLLToTempFolder(FindLatestDLL(DLLFile));

                    Assembly.LoadFrom(fname);

                    doc.SendStringToExecute(cmd, true, false, false);
                    ed.WriteMessage("\nLoading assembly: {0} successfull\n", fname);
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage("\nCannot load {0}: {1} : {2}", DLLFile, ex.StackTrace, ex.Message);
                }
            }
            else
            {
                ed.WriteMessage("\nFile {0} not exist!\n", DLLFile);
            }
        }
    }
}