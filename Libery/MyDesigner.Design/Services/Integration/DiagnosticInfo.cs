namespace MyDesigner.Design.Services.Integration
{
    public class DiagnosticInfo
    {
        public string Level { get; set; } = "error";
        public string Message { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }
}
