using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CodingArchitect.Utilities.Linqpad
{
    public static class Compiler
    {
        public static Assembly CompileFiles(Options options)
        {
            var outputPath = options.IsInMemory ? "" : options.OutputFile;

            if (!options.CodeFiles.Any())
                throw new InvalidOperationException("Should add at least one file to compile.");

            foreach (var codeFile in options.CodeFiles)
            {
                codeFile.RawContent = File.ReadAllLines(codeFile.FilePath);
                codeFile.Query = GetQuery(new FileInfo(codeFile.FilePath).DirectoryName, codeFile.RawContent);
                GetCode(codeFile, options.Namespace);
            }

            return BuildAssembly(options.CodeFiles, options, outputPath);
        }

        private static string[] GetNugetReferences()
        {
            var type = Type.GetType("LINQPad.ExecutionModel.Server, LINQPad");
            var currentServerPropertyInfo = type.GetProperty("CurrentServer");
            var currentServer = currentServerPropertyInfo.GetValue(null);
            var additionalRefsPropertyInfo = type.GetProperty("AdditionalRefs");
            var additionalRefs = additionalRefsPropertyInfo.GetValue(currentServer);
            return Array.FindAll<string>(additionalRefs as string[], ar => ar.Contains("NuGet"));
        }

        public static Query GetQuery(string folder, IEnumerable<string> content)
        {
            var xml = string.Join("\r\n", content.TakeWhile(l => l.Trim().StartsWith("<")));

            var queryElement = XDocument.Parse(xml).Element("Query");
            if (queryElement == null) throw new InvalidOperationException("Missing <Query> header definition");

            var otherReferences = queryElement.Elements("Reference").Where(e => e.Attribute("Relative") == null)
                    .Select(n => n.Value.Replace("<RuntimeDirectory>", RuntimeEnvironment.GetRuntimeDirectory()))
                    .Select(n => n.Replace("<ProgramFilesX86>", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)))
                    .ToList();
            otherReferences.AddRange(GetNugetReferences());
            var query = new Query
            {
                Kind = queryElement.Attribute("Kind").Value,
                Namespaces = queryElement.Elements("Namespace").Select(n => n.Value).ToList(),
                GACReferences = queryElement.Elements("GACReference").Select(n => n.Value).ToList(),
                RelativeReferences = queryElement.Elements("Reference").Where(e => e.Attribute("Relative") != null)
                    .Select(n => n.Attribute("Relative").Value)
                    .Select(x => new FileInfo(Path.Combine(folder, x)).FullName)
                    .ToList(),
                OtherReferences = otherReferences
            };

            return query;
        }

        public static void CopyReferencesToAppDirectory(string queryDirectory, string queryFilePath, string appDirectory)
        {
            var query = GetQuery(queryDirectory, File.ReadLines(queryFilePath));
            var otherReferences = query.OtherReferences.Where(p => p.Contains("LINQPad"));
            foreach (var otherReference in otherReferences)
            {
                var fileInfo = new FileInfo(otherReference);
                var copyLocalPath = Path.Combine(appDirectory, fileInfo.Name);
                if (!File.Exists(copyLocalPath)) File.Copy(otherReference, copyLocalPath);
            }
            foreach (var relativeReference in query.RelativeReferences)
            {
                var fileInfo = new FileInfo(relativeReference);
                var copyLocalPath = Path.Combine(appDirectory, fileInfo.Name);
                if (!File.Exists(copyLocalPath)) File.Copy(relativeReference, copyLocalPath);
            }
        }

        public static IEnumerable<string> GetCode(IEnumerable<IEnumerable<string>> contents, Query query, string ns)
        {
            return contents.Select(c => GetCode(c, query, ns));
        }

        private static void GetCode(CodeFile codeFile, string @namespace)
        {
            IEnumerable<string> result = null;

            if (codeFile.Query.Kind != "Program" && codeFile.Query.Kind != "Statements")
                throw new InvalidOperationException("Only queries of type C# program and C# statements are supported");

            var filteredContent = codeFile.RawContent.SkipWhile(l => l.Trim().StartsWith("<"));

            switch (codeFile.Type)
            {
                case CodeType.LinqStatements:
                    result = WrapInClass(WrapInMain(filteredContent));
                    break;
                case CodeType.LinqProgramTypes:
                    result = FilterTypes(filteredContent);
                    break;
                case CodeType.LinqProgram:
                    result = WrapInClass(filteredContent);
                    break;
                default:
                    throw new InvalidOperationException("Only queries of type C# program and C# statements are supported");
            }

            codeFile.Content = GetCode(result, codeFile.Query, @namespace);
        }

        private static IEnumerable<string> WrapInClass(IEnumerable<string> inputCode)
        {
            var s = new[] { "public class Program {" };
            var e = new[] { "}" };

            return s.Concat(inputCode).Concat(e);
        }

        private static IEnumerable<string> WrapInMain(IEnumerable<string> inputCode)
        {
            var s = new[] { "public static void Main() {" };
            var e = new[] { "}" };

            return s.Concat(inputCode).Concat(e);
        }

        private static IEnumerable<string> FilterTypes(IEnumerable<string> inputCode)
        {
            return inputCode.SkipWhile(l => l.Trim() != "// Define other methods and classes here");
        }

        public static string GetCode(IEnumerable<string> content, Query query, string ns)
        {
            var code = string.Join(Environment.NewLine, content);
            var codeBuilder = new StringBuilder();

            codeBuilder.AppendLine("using " + string.Join(";\r\nusing ", query.Namespaces.Union(StandardNamespaces)) + ";");
            codeBuilder.AppendLine(string.Format("namespace {0} {{", ns));
            codeBuilder.AppendLine(code);
            codeBuilder.AppendLine("}");

            return codeBuilder.ToString();
        }

        public static Assembly BuildAssembly(IEnumerable<CodeFile> codeFiles, Options options, string outputPath)
        {
            var providerOptions = new Dictionary<string, string> { { "CompilerVersion", "v4.0" } };
            var provider = new CSharpCodeProvider(providerOptions);
            var assemblies = new[]
            {
            codeFiles.SelectMany(c => c.Query.GACReferences.Select(s => Assembly.Load(s).Location)),
            codeFiles.SelectMany(c => c.Query.RelativeReferences.Select(s => Assembly.LoadFrom(s).Location)),
            codeFiles.SelectMany(c => c.Query.OtherReferences),
            Assemblies
        };

            var compilerparams = new CompilerParameters
            {
                GenerateExecutable = !string.IsNullOrWhiteSpace(options.StartupObject),
                OutputAssembly = options.IsInMemory ? null : outputPath,
                GenerateInMemory = true,
                IncludeDebugInformation = true,
                MainClass = options.StartupObject
            };

            compilerparams.ReferencedAssemblies.AddRange(assemblies.SelectMany(a => a).ToArray());

            var results = provider.CompileAssemblyFromSource(compilerparams, codeFiles.Select(f => f.Content).ToArray());
            if (results.Errors.HasErrors)
            {
                var errors = new StringBuilder("Compiler Errors:\r\n");
                foreach (CompilerError error in results.Errors)
                {
                    errors.AppendFormat("File {0}, Line {1},{2}\t: {3}\r\n",
                                        error.FileName, error.Line, error.Column, error.ErrorText);
                }

                throw new Exception("Errors compiling:\r\n" + errors + "\r\n\r\n" +
                    string.Join(Environment.NewLine, codeFiles.Select(f => f.FilePath + ":\r\n" + AddLineNumber(f.Content))));
            }

            return results.CompiledAssembly;
        }

        private static string AddLineNumber(string code)
        {
            var lines = code.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            return string.Join(Environment.NewLine, lines.Select((x, i) => (i + 1).ToString().PadLeft(4) + ": " + x));
        }

        private static List<string> StandardNamespaces
        {
            get
            {
                return new List<string>
            {
                "System",
                "System.IO",
                "System.Text",
                "System.Text.RegularExpressions",
                "System.Diagnostics",
                "System.Threading",
                "System.Reflection",
                "System.Collections",
                "System.Collections.Generic",
                "System.Linq",
                "System.Linq.Expressions",
                "System.Data",
                "System.Data.SqlClient",
                "System.Data.Linq",
                "System.Data.Linq.SqlClient",
                "System.Xml",
                "System.Xml.Linq",
                "System.Xml.XPath"
            };
            }
        }

        private static List<string> Assemblies
        {
            get
            {
                return new List<string>
            {
                "System.dll",
                "System.Core.dll",
                "System.Data.dll",
                "System.Xml.dll",
                "System.Xml.Linq.dll",
                "System.Data.Linq.dll",
                "System.Drawing.dll",
                "System.Data.DataSetExtensions.dll"
            };
            }
        }
    }
}
