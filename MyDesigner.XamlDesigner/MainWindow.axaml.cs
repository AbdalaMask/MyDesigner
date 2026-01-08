using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using System;
using System.ComponentModel;
using System.Linq;
using MyDesigner.Designer.PropertyGrid;
using MyDesigner.Designer;
using Avalonia.Input;
using MyDesigner.XamlDesigner.Configuration;
using MyDesigner.XamlDesigner.ViewModels;


namespace MyDesigner.XamlDesigner;

public partial class MainWindow : Window
{
    public static MainWindow Instance { get; private set; }
    public MainWindowViewModel ViewModel { get; }

    public MainWindow()
    {
        Instance = this;
        InitializeShell();
        
        // Initialize ViewModel
        ViewModel = new MainWindowViewModel();
        DataContext = ViewModel;
        
        // Initialize designer metadata
        BasicMetadata.Register();
        
        InitializeComponent();
        
        // Setup keyboard shortcuts
        SetupKeyboardShortcuts();
        
        LoadSettings();
    }

    private void SetupKeyboardShortcuts()
    {
        // Handle global keyboard shortcuts
        this.KeyDown += MainWindow_KeyDown;
    }

    private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        // Try to execute keyboard shortcuts
        if (Commands.KeyGestureHelper.TryExecuteCommand(e, Shell.Instance))
        {
            e.Handled = true;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeShell()
    {
        // Setup control references like the original project
        SetupControlReferences();
        
        //// Create a new document by default
        //    Shell.Instance.New();
        
    }

    private void SetupControlReferences()
    {
        // This will be called after the dock layout is created
        // We need to find the PropertyGridView and connect it to Shell
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            // Try to find PropertyGridView in the dock layout
            // Since we're using ViewModels now, the connection is automatic through data binding
            System.Diagnostics.Debug.WriteLine("Setting up control references...");
            
            // The PropertyGrid connection is now handled through ViewModels
            // PropertyGridViewModel automatically binds to Shell.Instance.CurrentDocument.SelectionService.SelectedItems
        }, Avalonia.Threading.DispatcherPriority.Loaded);
    }

    private void LoadSettings()
    {
        try
        {
            var settings = Settings.Default;
            
            WindowState = settings.MainWindowState;
            
            if (settings.MainWindowRect.Width > 0 && settings.MainWindowRect.Height > 0)
            {
                Position = new Avalonia.PixelPoint((int)settings.MainWindowRect.X, (int)settings.MainWindowRect.Y);
                Width = settings.MainWindowRect.Width;
                Height = settings.MainWindowRect.Height;
            }
        }
        catch (Exception ex)
        {
            Shell.ReportException(ex);
        }
    }

    private void SaveSettings()
    {
        try
        {
            var settings = Settings.Default;
            
            settings.MainWindowState = WindowState;
            
            if (WindowState == WindowState.Normal)
            {
                settings.MainWindowRect = new Avalonia.Rect(Position.X, Position.Y, Width, Height);
            }

            settings.Save();
        }
        catch (Exception ex)
        {
            Shell.ReportException(ex);
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        try
        {
            Environment.Exit(0);
            if (!Shell.Instance.PrepareExit())
            {
                e.Cancel = true;
                return;
            }
            
            SaveSettings();
            Shell.Instance.SaveSettings();
            
            // Ensure all background processes are stopped
            CleanupResources();
        }
        catch (Exception ex)
        {
            Shell.ReportException(ex);
        }
        
        base.OnClosing(e);
    }

    private void CleanupResources()
    {
        try
        {
            // Close all documents and their associated resources
            Shell.Instance.CloseAll();
            
            // Stop any running timers or background operations
            StopAllBackgroundOperations();
            
            // Clear any static references
            Instance = null;
            
            // Force garbage collection to clean up any remaining resources
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            // Force application exit after a short delay
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                Environment.Exit(0);
            }, Avalonia.Threading.DispatcherPriority.Background);
        }
        catch (Exception ex)
        {
            Shell.ReportException(ex);
            // Force exit even if cleanup fails
            Environment.Exit(1);
        }
    }

    private void StopAllBackgroundOperations()
    {
        try
        {
            // Stop any dispatcher timers or background operations
            // This is a safety measure to ensure all background work is stopped
            
            // Clear any pending dispatcher operations
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { }, Avalonia.Threading.DispatcherPriority.Background);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    public async System.Threading.Tasks.Task<string> AskOpenFileName()
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open XAML File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("XAML Files")
                    {
                        Patterns = new[] { "*.xaml" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            return files?.FirstOrDefault()?.Path.LocalPath;
        }
        catch (Exception ex)
        {
            Shell.ReportException(ex);
            return null;
        }
    }

    public async System.Threading.Tasks.Task<string> AskSaveFileName(string initName)
    {
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save XAML File",
                SuggestedFileName = initName,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("XAML Files")
                    {
                        Patterns = new[] { "*.xaml" }
                    }
                }
            });

            return file?.Path.LocalPath;
        }
        catch (Exception ex)
        {
            Shell.ReportException(ex);
            return null;
        }
    }

    // Drag and drop support
    private void OnDragEnter(object sender, DragEventArgs e)
    {
        ProcessDrag(e);
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        ProcessDrag(e);
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            if (files != null)
            {
                ProcessPaths(files.Select(f => f.Path.LocalPath));
            }
        }
    }

    private void ProcessDrag(DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.None;

        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            if (files != null)
            {
                foreach (var file in files)
                {
                    var path = file.Path.LocalPath;
                    if (path.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase) ||
                        path.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase) ||
                        path.EndsWith(".xaml", StringComparison.InvariantCultureIgnoreCase))
                    {
                        e.DragEffects = DragDropEffects.Copy;
                        break;
                    }
                }
            }
        }
    }

    private void ProcessPaths(System.Collections.Generic.IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (path.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase) ||
                path.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase))
            {
                // Add assembly to toolbox
                Toolbox.Instance.AddAssembly(path);
            }
            else if (path.EndsWith(".xaml", StringComparison.InvariantCultureIgnoreCase))
            {
                Shell.Instance.Open(path);
            }
        }
    }

    // Event handlers for menu items
    private void RecentFiles_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (e.Source is MenuItem menuItem && menuItem.Header is string path)
            {
                ViewModel.OpenRecentFileCommand.Execute(path);
            }
        }
        catch (Exception ex)
        {
            Shell.ReportException(ex);
        }
    }
}