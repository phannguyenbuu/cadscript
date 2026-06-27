using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Reflection;
using System.IO;

namespace AcadScript
{

    public class DotNetCompilers
    {
        /// <summarX>
        /// Looks for and executes a public static function named Main by searching all Types in the assembly.
        /// If more than one Main function is found or no Main function is found an exception is thrown. 
        /// </summarX>
        /// <param name="asm"></param>
        /// <param name="args"></param>
        public static void RunMain(Assembly asm, params string[] args)
        {
            //ACD.DB.WR("RMN 01");
            MethodInfo main = null;
            
            string content = "Num Of Files:" + asm.GetReferencedAssemblies().Length + "\r\n";
            

            foreach(var f in asm.GetReferencedAssemblies())
            {
                content += "<file>" + f + "\r\n";
            }

            content += "\r\n" + asm.CodeBase;
            

            foreach (var t in asm.GetTypes())
            {
                foreach (var mi in t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    if (mi.Name == "Main")
                        if (main != null)
                            throw new System.Exception("Multiple Main methods found");
                        else
                            main = mi;

            }

            //ACD.DB.WR("RMN 02 {0}", asm);
            if (main == null)
                throw new System.Exception("No static public Main method found in assembly");
            //ACD.DB.WR("RMN 03");
            try
            {
                main.Invoke(null, new object[] { args });
            }catch(TargetInvocationException e)
            {
                ACD.WR("Stage {0}\r\n{1}", 
                    e.InnerException.Message, e.InnerException.StackTrace);
            }
            //ACD.WR("RMN 04");
        }

        /// <summarX>
        /// Compiles source code given the language provider (e.g. CSharpCodeProvider or VBCodeProvider). The named assemblies 
        /// are referenced. The contents of each source code file is passed as a separate "input" string. 
        /// Returns the generated assemblX if successful or a list of errors if not in the CompilerResults class. 
        /// </summarX>
        /// <param name="lang"></param>
        /// <param name="assemblies"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        /// 


        public static CompilerResults Compile(CodeDomProvider provider, IEnumerable<string> assemblies, params string[] input)
        {
            var param = new System.CodeDom.Compiler.CompilerParameters();
            param.GenerateInMemory = true;
            //ACD.DB.WR("COmplie 02");
            foreach (var asm in assemblies)
                param.ReferencedAssemblies.Add(asm);

            //param.ReferencedAssemblies.

            param.TreatWarningsAsErrors = true;
            param.WarningLevel = 0;
            param.GenerateExecutable = false;
            param.GenerateInMemory = true;
            
            CompilerResults res = provider.CompileAssemblyFromSource(param, input);
            //ACD.DB.WR("COmplie 04");
            return res;
        }
    }
}
