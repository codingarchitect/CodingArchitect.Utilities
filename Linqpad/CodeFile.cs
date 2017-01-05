namespace CodingArchitect.Utilities.Linqpad
{
    public class CodeFile
    {
        public CodeType Type { get; set; }
        public string FilePath { get; set; }
        public string[] RawContent { get; set; }
        public Query Query { get; set; }
        public string Content { get; set; }
    }
}
