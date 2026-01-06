using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MyDesigner.Design.Services.Integration
{
    // Minimal JSON command format: { "type": "startSession", "filePath": "..." }
    public class IntegrationServer : IDisposable
    {
        private readonly IExternalIntegrationService _service;
        private readonly TcpListener _listener;
        private CancellationTokenSource _cts;
        private readonly Func<string, string[], string, Task>? _onOpenFile;  // Callback to open file in host with assemblyPaths and projectAssemblyName
        private readonly Func<string, IEnumerable<MyDesigner.Design.Services.Integration.DiagnosticInfo>>? _getDiagnostics;
        private readonly Action<string, string>? _onLog;  // Callback to send logs back to extension: (level, message)

        public IntegrationServer(IExternalIntegrationService service, int port = 50023, Func<string, string[], string, Task>? onOpenFile = null, Func<string, IEnumerable<MyDesigner.Design.Services.Integration.DiagnosticInfo>>? getDiagnostics = null, Action<string, string>? onLog = null)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _listener = new TcpListener(IPAddress.Loopback, port);
            _onOpenFile = onOpenFile;
            _getDiagnostics = getDiagnostics;
            _onLog = onLog;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _listener.Start();
            _ = AcceptLoop(_cts.Token);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener.Stop();
        }

        private async Task AcceptLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                    _ = HandleClient(client, ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception) { }
        }

        private async Task HandleClient(TcpClient client, CancellationToken ct)
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                try
                {
                    while (!ct.IsCancellationRequested && client.Connected)
                    {
                        var line = await reader.ReadLineAsync().ConfigureAwait(false);
                        if (line == null) break;
                        try
                        {
                            var doc = JsonDocument.Parse(line);
                            if (!doc.RootElement.TryGetProperty("type", out var typeEl)) continue;
                            var type = typeEl.GetString();
                            switch (type)
                        {
                            case "startSession":
                                if (doc.RootElement.TryGetProperty("filePath", out var fp))
                                {
                                    var filePath = fp.GetString();
                                    string[] assemblyPaths = null;
                                    string projectAssemblyName = null;
                                    
                                    // Parse optional assemblyPaths
                                    if (doc.RootElement.TryGetProperty("assemblyPaths", out var ap2) && ap2.ValueKind == JsonValueKind.Array)
                                    {
                                        var list = new List<string>();
                                        foreach (var item in ap2.EnumerateArray())
                                        {
                                            try
                                            {
                                                var p = item.GetString();
                                                if (!string.IsNullOrEmpty(p)) list.Add(p);
                                            }
                                            catch { }
                                        }
                                        assemblyPaths = list.ToArray();
                                    }
                                    
                                    // Parse optional projectAssemblyName
                                    if (doc.RootElement.TryGetProperty("projectAssemblyName", out var pan)) projectAssemblyName = pan.GetString();

                                    // Send log: received startSession
                                    var logMsg = JsonSerializer.Serialize(new { type = "log", level = "info", message = $"[DesignerHost] Received startSession for {filePath}" });
                                    await writer.WriteLineAsync(logMsg).ConfigureAwait(false);

                                    // Call the service
                                    var result = await _service.StartSessionAsync(filePath, assemblyPaths, projectAssemblyName).ConfigureAwait(false);

                                    // Try to open the file in the host window if callback is provided
                                    if (_onOpenFile != null && !string.IsNullOrEmpty(filePath))
                                    {
                                        try
                                        {
                                            var openLog = JsonSerializer.Serialize(new { type = "log", level = "info", message = $"[DesignerHost] Opening file via callback: {filePath}" });
                                            await writer.WriteLineAsync(openLog).ConfigureAwait(false);
                                            
                                            // Create a logging action that sends messages through the TCP connection
                                            Action<string, string> logToExtension = (level, msg) =>
                                            {
                                                try
                                                {
                                                    var logMsg = JsonSerializer.Serialize(new { type = "log", level = level, message = msg });
                                                    writer.WriteLine(logMsg);
                                                }
                                                catch { }
                                            };
                                            
                                            // Set the logger context before invoking the callback
                                            // The callback (and all code it calls) can use this to send logs back
                                            MyDesigner.Design.Services.Integration.FileOpeningLogContext.SetLogger(logToExtension);
                                            
                                            try
                                            {
                                                await _onOpenFile(filePath, assemblyPaths ?? Array.Empty<string>(), projectAssemblyName).ConfigureAwait(false);
                                            }
                                            finally
                                            {
                                                MyDesigner.Design.Services.Integration.FileOpeningLogContext.ClearLogger();
                                            }
                                            
                                            var openLog2 = JsonSerializer.Serialize(new { type = "log", level = "info", message = $"[DesignerHost] File opened successfully" });
                                            await writer.WriteLineAsync(openLog2).ConfigureAwait(false);
                                        }
                                        catch (Exception ex)
                                        {
                                            var errLog = JsonSerializer.Serialize(new { type = "log", level = "error", message = $"[DesignerHost] Error opening file: {ex.Message}" });
                                            await writer.WriteLineAsync(errLog).ConfigureAwait(false);
                                        }
                                    }
                                    else
                                    {
                                        var warnLog = JsonSerializer.Serialize(new { type = "log", level = "warn", message = $"[DesignerHost] No callback or no file path provided" });
                                        await writer.WriteLineAsync(warnLog).ConfigureAwait(false);
                                    }

                                    // Send sessionStarted response
                                        var resp = JsonSerializer.Serialize(new { type = "sessionStarted", sessionId = result?.SessionId, warnings = result?.Warnings });
                                        await writer.WriteLineAsync(resp).ConfigureAwait(false);

                                        // If diagnostics callback provided, send diagnostics for this file
                                        if (_getDiagnostics != null && !string.IsNullOrEmpty(filePath))
                                        {
                                            try
                                            {
                                                var diags = _getDiagnostics(filePath);
                                                if (diags != null)
                                                {
                                                    foreach (var d in diags)
                                                    {
                                                        var dmsg = JsonSerializer.Serialize(new { type = "diagnostic", level = d.Level, message = d.Message, line = d.Line, column = d.Column });
                                                        await writer.WriteLineAsync(dmsg).ConfigureAwait(false);
                                                    }
                                                }
                                            }
                                            catch { }
                                        }
                                }
                                break;
                            // additional commands can be parsed here
                        }
                    }
                    catch (JsonException) { }
                }
            }
            catch (Exception) { }
            }
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }
    }
}
