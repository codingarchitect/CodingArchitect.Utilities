using System;
using System.Collections.Generic;

namespace CodingArchitect.Utilities.Linqpad
{
    public class Options
    {
        private Options(string @namespace)
        {
            if (string.IsNullOrWhiteSpace(@namespace)) throw new ArgumentNullException("namespace");

            this.CodeFiles = new List<CodeFile>();
            this.Namespace = @namespace;
        }

        public static Options CreateInMemoryDll(string @namespace)
        {
            var options = new Options(@namespace);

            options.OutputFile = null;
            options.IsInMemory = true;
            options.StartupObject = null;

            return options;
        }

        public static Options CreateOnDiskDll(string @namespace, string outputFile)
        {
            if (string.IsNullOrWhiteSpace(outputFile)) throw new ArgumentNullException("outputFile");

            var options = new Options(@namespace);

            options.OutputFile = outputFile;
            options.IsInMemory = false;
            options.StartupObject = null;

            return options;
        }

        public static Options CreateOnDiskExe(string @namespace, string outputFile, string startupObject)
        {
            if (string.IsNullOrWhiteSpace(outputFile)) throw new ArgumentNullException("outputFile");
            if (string.IsNullOrWhiteSpace(startupObject)) throw new ArgumentNullException("startupObject");

            var options = new Options(@namespace);

            options.OutputFile = outputFile;
            options.IsInMemory = false;
            options.StartupObject = startupObject;

            return options;
        }

        public Options AddCodeFile(CodeType type, string filePath)
        {
            this.CodeFiles.Add(new CodeFile { Type = type, FilePath = filePath });
            return this;
        }

        public string Namespace { get; private set; }
        public string OutputFile { get; private set; }
        public List<CodeFile> CodeFiles { get; private set; }
        public string StartupObject { get; private set; }
        public bool IsInMemory { get; private set; }
    }
}
