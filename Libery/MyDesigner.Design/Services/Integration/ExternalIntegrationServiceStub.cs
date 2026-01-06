using System;
using System.Threading.Tasks;

namespace MyDesigner.Design.Services.Integration
{
    public class ExternalIntegrationServiceStub : IExternalIntegrationService
    {
        public event EventHandler<DocumentChangedEventArgs> DocumentChangedByHost;

        public Task<StartSessionResult> StartSessionAsync(string filePath, string[] assemblyPaths = null, string projectAssemblyName = null)
        {
            var result = new StartSessionResult();
            // Placeholder stub - actual XAML loading is done by the host application
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    result.Warnings.Add("File path is empty");
                }
                else if (!System.IO.File.Exists(filePath))
                {
                    result.Warnings.Add($"File not found: {filePath}");
                }

                if (assemblyPaths == null || assemblyPaths.Length == 0)
                {
                    result.Warnings.Add("No assembly paths provided");
                }

                result.SessionId = Guid.NewGuid().ToString();
            }
            catch (Exception ex)
            {
                result.SessionId = Guid.NewGuid().ToString();
                result.Warnings.Add($"Error in StartSessionAsync: {ex.Message}");
            }

            return Task.FromResult(result);
        }

        public Task OpenFileAsync(string sessionId, string filePath)
        {
            return Task.CompletedTask;
        }

        public Task SelectElementAsync(string sessionId, string elementId)
        {
            return Task.CompletedTask;
        }

        public Task ApplyPropertyEditAsync(string sessionId, string elementId, string propertyName, string value)
        {
            return Task.CompletedTask;
        }

        public Task<byte[]> GetPreviewAsync(string sessionId, PreviewOptions options)
        {
            return Task.FromResult(Array.Empty<byte>());
        }
    }
}
