using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodingArchitect.Utilities.AppDomain
{
    public abstract class ConsoleCatcherBase
    {
        public abstract void DoExecute();

        public string Execute()
        {
            TextWriter originalConsoleOutput = Console.Out;
            StringWriter writer = new StringWriter();
            Console.SetOut(writer);
            DoExecute();
            Console.SetOut(originalConsoleOutput);
            return writer.ToString();
        }
    }
}
