namespace Bunnypro.CommonMark.Specification
{
    public class Specification
    {
        public string[] Section { get; set; }
        public int SpecificationNumber { get; set; }
        public string Markdown { get; set; }
        public string Html { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
    }
}
