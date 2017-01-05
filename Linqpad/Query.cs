using System.Collections.Generic;

namespace CodingArchitect.Utilities.Linqpad
{
    public class Query
    {
        public string Kind { get; set; }
        public List<string> Namespaces { get; set; }
        public List<string> GACReferences { get; set; }
        public List<string> RelativeReferences { get; set; }
        public List<string> OtherReferences { get; set; }
    }
}
