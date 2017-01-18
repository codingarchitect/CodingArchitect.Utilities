using System;
using System.IO;
using System.Reflection;

namespace CodingArchitect.Utilities.AppDomain
{
    public class Launcher : MarshalByRefObject
    {
        public static string ExecuteProgram(string programDirectory, string programAssemblyFileName, string entryPointTypeName)
        {
            var appDomainSetup = new AppDomainSetup()
            {
                ApplicationBase = programDirectory,
                ShadowCopyFiles = "true"
            };
            System.AppDomain appDomain = System.AppDomain.CreateDomain("LINQPad ShadowCopy Domain", null, appDomainSetup);
            Launcher program = (Launcher)appDomain.CreateInstanceAndUnwrap(
                typeof(Launcher).Assembly.FullName,
                typeof(Launcher).FullName);

            var result = program.Execute(programDirectory, programAssemblyFileName, entryPointTypeName);
            System.AppDomain.Unload(appDomain);
            Console.WriteLine(result);
            return result;
        }

        /// <summary>
        /// This gets executed in the temporary appdomain.
        /// No error handling to simplify demo.
        /// </summary>
        public string Execute(string programDirectory, string programAssemblyFileName, string entryPointTypeName)
        {
            // load the bytes and run Main() using reflection
            // working with bytes is useful if the assembly doesn't come from disk
            Assembly assembly = Assembly.LoadFrom(Path.Combine(programDirectory, programAssemblyFileName));
            Type entryPointType = assembly.GetType(entryPointTypeName);
            MethodInfo main = entryPointType.GetMethod("Execute");
            var instance = Activator.CreateInstance(entryPointType);
            return (string)main.Invoke(instance, null);
        }
    }

}
