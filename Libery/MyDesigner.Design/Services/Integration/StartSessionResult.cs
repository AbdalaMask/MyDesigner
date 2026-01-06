using System.Collections.Generic;

namespace MyDesigner.Design.Services.Integration
{
    public sealed class StartSessionResult
    {
        public string SessionId { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
