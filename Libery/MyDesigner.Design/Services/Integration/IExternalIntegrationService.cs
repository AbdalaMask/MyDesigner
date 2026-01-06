using System;
using System.Threading.Tasks;

namespace MyDesigner.Design.Services.Integration
{
    public class PreviewOptions { }

    public class DocumentChangedEventArgs : EventArgs { }

    public interface IExternalIntegrationService
    {
        Task<StartSessionResult> StartSessionAsync(string filePath, string[] assemblyPaths = null, string projectAssemblyName = null);
        Task OpenFileAsync(string sessionId, string filePath);
        Task SelectElementAsync(string sessionId, string elementId);
        Task ApplyPropertyEditAsync(string sessionId, string elementId, string propertyName, string value);
        Task<byte[]> GetPreviewAsync(string sessionId, PreviewOptions options);
        event EventHandler<DocumentChangedEventArgs> DocumentChangedByHost;
    }
}
