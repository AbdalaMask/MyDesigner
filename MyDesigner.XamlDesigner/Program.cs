using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;

namespace MyDesigner.XamlDesigner
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            // Simple CLI switch: --serve [port]
            bool serve = false;
            int port = 50023;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--serve") serve = true;
                else if (args[i] == "--port" && i + 1 < args.Length && int.TryParse(args[i + 1], out var p))
                {
                    port = p; i++;
                }
            }

            IntegrationServerHost? host = null;
            if (serve)
            {
                try
                {
                    // Create a callback that will open files in Shell once it's initialized
                    Func<string, string[], string, Task> onOpenFile = async (filePath, assemblyPaths, projectAssemblyName) =>
                    {
                        MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[IntegrationCallback] ========== START FILE OPEN ==========");
                        MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[IntegrationCallback] File open request: {filePath}");
                        MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[IntegrationCallback] Assembly paths ({assemblyPaths?.Length ?? 0}): {string.Join(",", assemblyPaths ?? new string[0])}");
                        MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[IntegrationCallback] Project assembly: {projectAssemblyName}");
                        MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[IntegrationCallback] Shell.Instance is {(Shell.Instance == null ? "NULL" : "initialized")}");
                        MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[IntegrationCallback] Documents in Shell: {Shell.Instance?.Documents?.Count ?? 0}");
                        
                        // Wait for Shell to be ready (set after Avalonia initializes)
                        int waitCount = 0;
                        for (int i = 0; i < 50 && Shell.Instance == null; i++) 
                        {
                            MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[IntegrationCallback] Waiting for Shell... ({i}/50)");
                            await Task.Delay(100);
                            waitCount = i;
                        }
                        
                        if (Shell.Instance == null)
                        {
                            MyDesigner.Design.Services.Integration.FileOpeningLogContext.Error($"[IntegrationCallback] ERROR: Shell.Instance is still NULL after {waitCount} waits!");
                            return;
                        }
                        
                        MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[IntegrationCallback] Shell ready after {waitCount} waits, calling Shell.Instance.OpenWithAssemblies()");
                        MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[IntegrationCallback] Before OpenWithAssemblies: CurrentDocument = {Shell.Instance.CurrentDocument?.FilePath}");
                        
                        // Marshal the file opening to the UI thread since Document creation involves Avalonia objects
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[IntegrationCallback] [UIThread] Executing OpenWithAssemblies on UI thread");
                            Shell.Instance.OpenWithAssemblies(filePath, assemblyPaths, projectAssemblyName);
                            MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[IntegrationCallback] [UIThread] OpenWithAssemblies completed");
                        });
                        
                        MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[IntegrationCallback] After OpenWithAssemblies: CurrentDocument = {Shell.Instance.CurrentDocument?.FilePath}");
                        MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[IntegrationCallback] Documents in Shell: {Shell.Instance.Documents.Count}");
                        foreach (var doc in Shell.Instance.Documents)
                        {
                            MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[IntegrationCallback]   - Document: {doc.FilePath ?? "(no path)"} (IsDirty={doc.IsDirty})");
                        }
                        MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[IntegrationCallback] ========== END FILE OPEN ==========");
                    };
                    
                    host = new IntegrationServerHost(port, onOpenFile, (file) =>
                    {
                        var list = new List<MyDesigner.Design.Services.Integration.DiagnosticInfo>();
                        try
                        {
                            if (Shell.Instance != null)
                            {
                                var doc = Shell.Instance.Documents.FirstOrDefault(d => d.FilePath == file);
                                if (doc != null)
                                {
                                    var xerr = doc.XamlErrorService;
                                    if (xerr != null && xerr.Errors != null)
                                    {
                                        foreach (var e in xerr.Errors)
                                        {
                                            list.Add(new MyDesigner.Design.Services.Integration.DiagnosticInfo { Level = "error", Message = e.Message, Line = e.Line, Column = e.Column });
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                        return list.AsEnumerable();
                    });
                    host.Start();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[IntegrationServerHost] Error starting: {ex}");
                }
            }

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnMainWindowClose);

            host?.Stop();
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }

    // small host that wires the stub service to the IntegrationServer
    internal class IntegrationServerHost
    {
        private readonly MyDesigner.Design.Services.Integration.IntegrationServer _server;

        public IntegrationServerHost(int port, Func<string, string[], string, Task>? onOpenFile = null, Func<string, IEnumerable<MyDesigner.Design.Services.Integration.DiagnosticInfo>>? getDiagnostics = null)
        {
            var stub = new MyDesigner.Design.Services.Integration.ExternalIntegrationServiceStub();
            _server = new MyDesigner.Design.Services.Integration.IntegrationServer(stub, port, onOpenFile, getDiagnostics);
        }

        public void Start() => _server.Start();
        public void Stop() => _server.Stop();
    }
}
