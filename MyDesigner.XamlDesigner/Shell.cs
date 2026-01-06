using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyDesigner.Design.Services.Integration;
using MyDesigner.Designer.PropertyGrid;
using MyDesigner.XamlDesigner.Commands;
using MyDesigner.XamlDesigner.Tools;

namespace MyDesigner.XamlDesigner
{
    public partial class Shell : ObservableObject
    {
        private readonly Services.RecentFilesService _recentFilesService;

        public Shell()
        {
            _recentFilesService = new Services.RecentFilesService();
            Documents = new ObservableCollection<Document>();
            Views = new Dictionary<object, Views.DocumentView>();

            // Initialize commands with proper CanExecute predicates
            NewCommand = new RelayCommand(New);
            OpenCommand = new RelayCommand(Open);
            SaveCommand = new RelayCommand(SaveCurrentDocument, () => CurrentDocument != null && CurrentDocument.IsDirty);
            SaveAsCommand = new RelayCommand(SaveCurrentDocumentAs, () => CurrentDocument != null);
            SaveAllCommand = new RelayCommand(() => SaveAll(), () => Documents.Any(d => d.IsDirty));
            CloseCommand = new RelayCommand(CloseCurrentDocument, () => CurrentDocument != null);
            CloseAllCommand = new RelayCommand(() => CloseAll(), () => Documents.Count > 0);
            ExitCommand = new RelayCommand(Exit);
            RunCommand = new RelayCommand(Run, () => CurrentDocument != null);
            RenderToBitmapCommand = new RelayCommand(RenderToBitmap, () => CurrentDocument != null);
            RefreshCommand = new RelayCommand(Refresh, CanRefresh);
            UndoCommand = new RelayCommand(Undo, CanUndo);
            RedoCommand = new RelayCommand(Redo, CanRedo);
            CutCommand = new RelayCommand(Cut, CanCutCopyDelete);
            CopyCommand = new RelayCommand(Copy, CanCutCopyDelete);
            PasteCommand = new RelayCommand(Paste, CanPaste);
            DeleteCommand = new RelayCommand(Delete, CanCutCopyDelete);
            SelectAllCommand = new RelayCommand(SelectAll, () => CurrentDocument != null);
            FindCommand = new RelayCommand(Find, () => CurrentDocument != null);

            LoadSettings();
        }

        public static Shell Instance { get; } = new Shell();
        public const string ApplicationTitle = "MyDesigner XAML Designer";

        public PropertyGridView? PropertyGridView { get; internal set; }
        public MyDesigner.Designer.PropertyGrid.IPropertyGrid? PropertyGrid { get; internal set; }
        
        public ObservableCollection<Document> Documents { get; private set; }
        public ObservableCollection<string> RecentFiles => _recentFilesService.RecentFiles;
        public Dictionary<object, Views.DocumentView> Views { get; private set; }

        [ObservableProperty]
        private Document currentDocument;

        public string Title => CurrentDocument != null ? 
            $"{CurrentDocument.Title} - {ApplicationTitle}" : ApplicationTitle;

        // Commands using CommunityToolkit.Mvvm
        public RelayCommand NewCommand { get; }
        public RelayCommand OpenCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand SaveAsCommand { get; }
        public RelayCommand SaveAllCommand { get; }
        public RelayCommand CloseCommand { get; }
        public RelayCommand CloseAllCommand { get; }
        public RelayCommand ExitCommand { get; }
        public RelayCommand RunCommand { get; }
        public RelayCommand RenderToBitmapCommand { get; }
        public RelayCommand RefreshCommand { get; }
        public RelayCommand UndoCommand { get; }
        public RelayCommand RedoCommand { get; }
        public RelayCommand CutCommand { get; }
        public RelayCommand CopyCommand { get; }
        public RelayCommand PasteCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand SelectAllCommand { get; }
        public RelayCommand FindCommand { get; }

        partial void OnCurrentDocumentChanged(Document? oldValue, Document value)
        {
            FileOpeningLogContext.Info($"[Shell.OnCurrentDocumentChanged] ========== START ==========");
            FileOpeningLogContext.Info($"[Shell.OnCurrentDocumentChanged] Old value: {oldValue?.Title} ({oldValue?.FilePath})");
            FileOpeningLogContext.Info($"[Shell.OnCurrentDocumentChanged] New value: {value?.Title} ({value?.FilePath})");
            OnPropertyChanged(nameof(Title));
            
            // Notify that all document-related properties have changed
            OnPropertyChanged(nameof(CurrentDocument));
            
            // Refresh command states
            RefreshCommandStates();
            FileOpeningLogContext.Info($"[Shell.OnCurrentDocumentChanged] ========== END ==========");
        }

        public void RefreshCommandStates()
        {
            // Notify all commands to refresh their CanExecute state
            SaveCommand.NotifyCanExecuteChanged();
            SaveAsCommand.NotifyCanExecuteChanged();
            SaveAllCommand.NotifyCanExecuteChanged();
            CloseCommand.NotifyCanExecuteChanged();
            CloseAllCommand.NotifyCanExecuteChanged();
            RunCommand.NotifyCanExecuteChanged();
            RenderToBitmapCommand.NotifyCanExecuteChanged();
            RefreshCommand.NotifyCanExecuteChanged();
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
            CutCommand.NotifyCanExecuteChanged();
            CopyCommand.NotifyCanExecuteChanged();
            PasteCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
            SelectAllCommand.NotifyCanExecuteChanged();
            FindCommand.NotifyCanExecuteChanged();
        }

        // Public method to allow Document to notify property changes
        public void NotifyPropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }

        void LoadSettings()
        {
            try
            {
                var settings = Configuration.Settings.Default;
                
                // Load recent files
                _recentFilesService.LoadFromList(settings.RecentFiles);

                // Load assembly list for toolbox
                foreach (var assembly in settings.AssemblyList)
                {
                    try
                    {
                        Toolbox.Instance.AddAssembly(assembly);
                    }
                    catch (Exception ex)
                    {
                        ReportException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                ReportException(ex);
            }
        }

        public void SaveSettings()
        {
            try
            {
                var settings = Configuration.Settings.Default;
                
                // Save recent files
                settings.RecentFiles.Clear();
                settings.RecentFiles.AddRange(_recentFilesService.ToList());

                settings.Save();
            }
            catch (Exception ex)
            {
                ReportException(ex);
            }
        }

        public static void ReportException(Exception x)
        {
            Services.ErrorReportingService.ReportException(x);
        }

        public void JumpToError(XamlError error)
        {
            if (CurrentDocument != null && Views.ContainsKey(CurrentDocument))
            {
                Views[CurrentDocument].JumpToError(error);
            }
        }

        public bool CanRefresh()
        {
            return CurrentDocument != null;
        }

        public bool CanUndo()
        {
            return CurrentDocument?.DesignSurface != null && 
                   DesignSurfaceCommands.CanExecuteOnDesignSurface(CurrentDocument.DesignSurface, "undo");
        }

        public bool CanRedo()
        {
            return CurrentDocument?.DesignSurface != null && 
                   DesignSurfaceCommands.CanExecuteOnDesignSurface(CurrentDocument.DesignSurface, "redo");
        }

        public bool CanCutCopyDelete()
        {
            return CurrentDocument?.DesignSurface != null && 
                   DesignSurfaceCommands.CanExecuteOnDesignSurface(CurrentDocument.DesignSurface, "cut");
        }

        public bool CanPaste()
        {
            return CurrentDocument?.DesignSurface != null && 
                   DesignSurfaceCommands.CanExecuteOnDesignSurface(CurrentDocument.DesignSurface, "paste");
        }

        public void Refresh()
        {
            CurrentDocument?.Refresh();
        }

        #region Command Implementations

        public void Undo()
        {
            try
            {
                if (CurrentDocument?.DesignSurface != null)
                {
                    DesignSurfaceCommands.RouteToDesignSurface(CurrentDocument.DesignSurface, "undo");
                }
            }
            catch (Exception ex)
            {
                ReportException(ex);
            }
        }

        public void Redo()
        {
            try
            {
                if (CurrentDocument?.DesignSurface != null)
                {
                    DesignSurfaceCommands.RouteToDesignSurface(CurrentDocument.DesignSurface, "redo");
                }
            }
            catch (Exception ex)
            {
                ReportException(ex);
            }
        }

        public void Cut()
        {
            try
            {
                if (CurrentDocument?.DesignSurface != null)
                {
                    DesignSurfaceCommands.RouteToDesignSurface(CurrentDocument.DesignSurface, "cut");
                }
            }
            catch (Exception ex)
            {
                ReportException(ex);
            }
        }

        public void Copy()
        {
            try
            {
                if (CurrentDocument?.DesignSurface != null)
                {
                    DesignSurfaceCommands.RouteToDesignSurface(CurrentDocument.DesignSurface, "copy");
                }
            }
            catch (Exception ex)
            {
                ReportException(ex);
            }
        }

        public void Paste()
        {
            try
            {
                if (CurrentDocument?.DesignSurface != null)
                {
                    DesignSurfaceCommands.RouteToDesignSurface(CurrentDocument.DesignSurface, "paste");
                }
            }
            catch (Exception ex)
            {
                ReportException(ex);
            }
        }

        public void Delete()
        {
            try
            {
                if (CurrentDocument?.DesignSurface != null)
                {
                    DesignSurfaceCommands.RouteToDesignSurface(CurrentDocument.DesignSurface, "delete");
                }
            }
            catch (Exception ex)
            {
                ReportException(ex);
            }
        }

        public void SelectAll()
        {
            try
            {
                if (CurrentDocument?.DesignSurface != null)
                {
                    DesignSurfaceCommands.RouteToDesignSurface(CurrentDocument.DesignSurface, "selectall");
                }
            }
            catch (Exception ex)
            {
                ReportException(ex);
            }
        }

        public void Find()
        {
            try
            {
                // TODO: Implement find functionality
                System.Diagnostics.Debug.WriteLine("Find command executed");
            }
            catch (Exception ex)
            {
                ReportException(ex);
            }
        }

        #endregion

        #region File Operations

        bool IsSomethingDirty => Documents.Any(doc => doc.IsDirty);

        static int nonameIndex = 1;

        public void New()
        {
            try
            {
                // Try to find template in multiple locations
                var templatePaths = new[]
                {
                    "NewFileTemplate.xaml",
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NewFileTemplate.xaml"),
                    Path.Combine(Directory.GetCurrentDirectory(), "NewFileTemplate.xaml")
                };

                string templateContent = null;
                
                foreach (var templatePath in templatePaths)
                {
                    if (File.Exists(templatePath))
                    {
                        templateContent = File.ReadAllText(templatePath);
                        break;
                    }
                }

                // Fallback template if file not found
                if (templateContent == null)
                {
                    templateContent = @"<Window
    xmlns=""https://github.com/avaloniaui""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    Width=""640""
    Height=""480"">
    <Canvas />
</Window>";
                }
                
                var doc = new Document($"New{nonameIndex++}", templateContent);
                Documents.Add(doc);
                CurrentDocument = doc;
            }
            catch (Exception ex)
            {
                ReportException(ex);
            }
        }

        public async void Open()
        {
            var path = await MainWindow.Instance?.AskOpenFileName();
            if (path != null)
            {
                Open(path);
            }
        }

        public void Open(string path)
        {
            OpenWithAssemblies(path, null, null);
        }

        public void OpenWithAssemblies(string path, string[] assemblyPaths = null, string projectAssemblyName = null)
        {
            try
            {
                FileOpeningLogContext.Info($"[Shell.OpenWithAssemblies] ========== START OPEN WITH ASSEMBLIES ==========");
                FileOpeningLogContext.Info($"[Shell.OpenWithAssemblies] Called with path: {path}");
                path = Path.GetFullPath(path);
                FileOpeningLogContext.Info($"[Shell.OpenWithAssemblies] Full path: {path}");
                FileOpeningLogContext.Info($"[Shell.OpenWithAssemblies] Assembly paths ({assemblyPaths?.Length ?? 0}): {string.Join(",", assemblyPaths ?? new string[0])}");
                FileOpeningLogContext.Info($"[Shell.OpenWithAssemblies] Project assembly: {projectAssemblyName}");

                _recentFilesService.AddFile(path);

                // Check if document already open
                FileOpeningLogContext.Info($"[Shell.OpenWithAssemblies] Checking {Documents.Count} existing documents...");
                foreach (var doc in Documents)
                {
                    FileOpeningLogContext.Info($"[Shell.OpenWithAssemblies]   Comparing: {doc.FilePath} == {path} ? {doc.FilePath == path}");
                    if (doc.FilePath == path)
                    {
                        FileOpeningLogContext.Info($"[Shell.OpenWithAssemblies] Document already open, setting as current");
                        CurrentDocument = doc;
                        FileOpeningLogContext.Info($"[Shell.OpenWithAssemblies] CurrentDocument set to: {CurrentDocument?.FilePath}");
                        FileOpeningLogContext.Info($"[Shell.OpenWithAssemblies] ========== END OPEN WITH ASSEMBLIES (reused) ==========");
                        return;
                    }
                }

                FileOpeningLogContext.Info($"[Shell.OpenWithAssemblies] Creating new document for: {path}");
                var newDoc = new Document(path, assemblyPaths, projectAssemblyName);
                FileOpeningLogContext.Info($"[Shell.OpenWithAssemblies] Document created, FilePath={newDoc.FilePath}");
                
                Documents.Add(newDoc);
                FileOpeningLogContext.Info($"[Shell.OpenWithAssemblies] Document added to collection (count={Documents.Count})");
                
                FileOpeningLogContext.Info($"[Shell.OpenWithAssemblies] Setting CurrentDocument to: {newDoc.FilePath}");
                CurrentDocument = newDoc;
                FileOpeningLogContext.Info($"[Shell.OpenWithAssemblies] CurrentDocument is now: {CurrentDocument?.FilePath}");
                FileOpeningLogContext.Info($"[Shell.OpenWithAssemblies] CurrentDocument.Title: {CurrentDocument?.Title}");
                FileOpeningLogContext.Info($"[Shell.OpenWithAssemblies] ========== END OPEN WITH ASSEMBLIES (new) ==========");
            }
            catch (Exception ex)
            {
                FileOpeningLogContext.Error($"[Shell.OpenWithAssemblies] EXCEPTION: {ex}");
                ReportException(ex);
            }
        }

        public bool Save(Document doc)
        {
            try
            {
                if (doc.IsDirty)
                {
                    if (doc.FilePath == null)
                    {
                        // For synchronous Save method, we can't await async SaveAs
                        // We'll need to handle this differently or make Save async too
                        return false; // For now, return false if no file path
                    }
                    doc.Save();
                }
                return true;
            }
            catch (Exception ex)
            {
                ReportException(ex);
                return false;
            }
        }

        public async System.Threading.Tasks.Task<bool> SaveAs(Document doc)
        {
            try
            {
                var initName = doc.FileName ?? doc.Name + ".xaml";
                var path = await MainWindow.Instance?.AskSaveFileName(initName);
                if (path != null)
                {
                    doc.SaveAs(path);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                ReportException(ex);
                return false;
            }
        }

        public bool SaveAll()
        {
            return Documents.All(Save);
        }

        public bool Close(Document doc)
        {
            try
            {
                if (doc.IsDirty)
                {
                    // In a real application, show a dialog asking to save
                    // For now, just save automatically
                    if (!Save(doc)) return false;
                }
                
                // Cleanup document resources
                doc.Cleanup();
                
                Documents.Remove(doc);
                Views.Remove(doc);
                return true;
            }
            catch (Exception ex)
            {
                ReportException(ex);
                return false;
            }
        }

        public bool CloseAll()
        {
            var documentsToClose = Documents.ToArray();
            foreach (var doc in documentsToClose)
            {
                if (!Close(doc))
                    return false;
            }
            return true;
        }

        public bool PrepareExit()
        {
            if (IsSomethingDirty)
            {
                // In a real application, show a dialog asking to save all
                // For now, just save all automatically
                return SaveAll();
            }
            return true;
        }

        public void Exit()
        {
            MainWindow.Instance?.Close();
        }

        public void SaveCurrentDocument()
        {
            if (CurrentDocument != null)
                Save(CurrentDocument);
        }

        public async void SaveCurrentDocumentAs()
        {
            if (CurrentDocument != null)
                await SaveAs(CurrentDocument);
        }

        public void CloseCurrentDocument()
        {
            if (CurrentDocument != null)
                Close(CurrentDocument);
        }

        public void Run()
        {
            try
            {
                if (CurrentDocument?.DesignSurface?.DesignContext != null)
                {
                    var sb = new StringBuilder();
                    using (var xmlWriter = XmlWriter.Create(new StringWriter(sb)))
                    {
                        CurrentDocument.DesignSurface.SaveDesigner(xmlWriter);
                    }

                    var xamlText = sb.ToString();
                    
                    // Create and show preview window
                    ShowPreviewWindow(xamlText);
                }
            }
            catch (Exception ex)
            {
                ReportException(ex);
            }
        }

        private void ShowPreviewWindow(string xamlText)
        {
            try
            {
                // Create a new window to show the preview
                var previewWindow = new Avalonia.Controls.Window
                {
                    Title = "Preview - " + (CurrentDocument?.Name ?? "Untitled"),
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner
                };

                // Try to parse and load the XAML
                using (var stringReader = new StringReader(xamlText))
                using (var xmlReader = XmlReader.Create(stringReader))
                {
                    try
                    {
                        // Load the XAML content
                        // Load the result in a new window
                       

                        var ctl = new Avalonia.Markup.Xaml.RuntimeXamlLoaderDocument(xamlText);
                        var content = Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Load(ctl);
                        if (content is Avalonia.Controls.Window window)
                        {
                            // If it's a window, show it directly
                            window.Show();
                        }
                        else if (content is Avalonia.Controls.Control control)
                        {
                            // If it's a control, put it in the preview window
                            previewWindow.Content = control;
                            previewWindow.Show();
                        }
                        else
                        {
                            // Fallback: show XAML text
                            var textBlock = new Avalonia.Controls.TextBlock
                            {
                                Text = xamlText,
                                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                                Margin = new Avalonia.Thickness(10)
                            };
                            var scrollViewer = new Avalonia.Controls.ScrollViewer
                            {
                                Content = textBlock
                            };
                            previewWindow.Content = scrollViewer;
                            previewWindow.Show();
                        }
                    }
                    catch (Exception parseEx)
                    {
                        // Show error in preview window
                        var errorText = new Avalonia.Controls.TextBlock
                        {
                            Text = $"XAML Parse Error:\n{parseEx.Message}\n\nXAML Content:\n{xamlText}",
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            Margin = new Avalonia.Thickness(10),
                            Foreground = Avalonia.Media.Brushes.Red
                        };
                        var scrollViewer = new Avalonia.Controls.ScrollViewer
                        {
                            Content = errorText
                        };
                        previewWindow.Content = scrollViewer;
                        previewWindow.Show();
                    }
                }
            }
            catch (Exception ex)
            {
                ReportException(ex);
            }
        }

        public async void RenderToBitmap()
        {
            try
            {
                if (CurrentDocument?.DesignSurface?.DesignContext != null)
                {
                    // Get the file path to save
                    var filePath = await MainWindow.Instance?.AskSaveFileName("preview.png");
                    if (string.IsNullOrEmpty(filePath))
                        return;

                    var sb = new StringBuilder();
                    using (var xmlWriter = XmlWriter.Create(new StringWriter(sb)))
                    {
                        CurrentDocument.DesignSurface.SaveDesigner(xmlWriter);
                    }

                    var xamlText = sb.ToString();
                    
                    // Render to bitmap
                    await RenderXamlToBitmap(xamlText, filePath);
                }
            }
            catch (Exception ex)
            {
                ReportException(ex);
            }
        }

        private async System.Threading.Tasks.Task RenderXamlToBitmap(string xamlText, string filePath)
        {
            try
            {
                const int desiredWidth = 300;
                const int desiredHeight = 300;

                using (var stringReader = new StringReader(xamlText))
                using (var xmlReader = XmlReader.Create(stringReader))
                {
                    try
                    {
                        var ctl = new Avalonia.Markup.Xaml.RuntimeXamlLoaderDocument(xamlText);
                        var content = Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Load(ctl);

                        if (content is Avalonia.Controls.Control control)
                        {
                            // Create a temporary window to render the control
                            var tempWindow = new Avalonia.Controls.Window
                            {
                                Width = desiredWidth,
                                Height = desiredHeight,
                                WindowState = Avalonia.Controls.WindowState.Minimized,
                                ShowInTaskbar = false,
                                Content = control
                            };

                            // Show and hide the window to ensure it's rendered
                            tempWindow.Show();
                            
                            // Wait for layout to complete
                            await System.Threading.Tasks.Task.Delay(100);
                            
                            // Render to bitmap using Avalonia's RenderTargetBitmap
                            var pixelSize = new Avalonia.PixelSize(desiredWidth, desiredHeight);
                            var renderBitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(pixelSize, new Avalonia.Vector(96, 96));
                            
                            renderBitmap.Render(control);
                            
                            // Save the bitmap
                            renderBitmap.Save(filePath);
                            
                            tempWindow.Close();
                            
                            System.Diagnostics.Debug.WriteLine($"Bitmap saved to: {filePath}");
                        }
                    }
                    catch (Exception parseEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to render XAML to bitmap: {parseEx.Message}");
                        ReportException(parseEx);
                    }
                }
            }
            catch (Exception ex)
            {
                ReportException(ex);
            }
        }

        #endregion
    }
}