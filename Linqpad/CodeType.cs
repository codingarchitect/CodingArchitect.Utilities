using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodingArchitect.Utilities.Linqpad
{

    public enum CodeType
    {
        LinqStatements, // Wrap inside class and Main() method
        LinqProgramTypes, // Code after 'Define other methods and classes here' directly inside namespace
        LinqProgram, // Wrap all code inside class
    }
}
