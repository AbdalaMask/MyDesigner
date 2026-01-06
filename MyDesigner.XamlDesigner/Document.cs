using CommunityToolkit.Mvvm.ComponentModel;
using MyDesigner.Design;
using MyDesigner.Design.Interfaces;
using MyDesigner.Design.Services.Integration;
using MyDesigner.Designer;
using MyDesigner.Designer.Services;
using MyDesigner.Designer.Xaml;
using MyDesigner.XamlDesigner.Tools;
using MyDesigner.XamlDesigner.ViewModels;
using MyDesigner.XamlDom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace MyDesigner.XamlDesigner
{
    public partial class Document : ViewModelBase
    {
        private string tempName;
        private DesignSurface designSurface = new DesignSurface();
        private string text;
        private DocumentMode mode = DocumentMode.Design;
        private string filePath;
        private bool isDirty;
        private XamlElementLineInfo xamlElementLineInfo;
        private string[] _assemblyPaths;
        private string _projectAssemblyName;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string xamlText;

     //   [ObservableProperty]
        private IOutlineNode outlineRoot;

     

        public Document(string tempName, string text)
        {
            this.tempName = tempName;
            Text = text;
            IsDirty = false;
            UpdateDesign();
        }

        public Document(string filePath) : this(Path.GetFileNameWithoutExtension(filePath), "")
        {
            FilePath = filePath;
            ReloadFile();
        }

        public Document(string filePath, string[] assemblyPaths, string projectAssemblyName) : this(Path.GetFileNameWithoutExtension(filePath), "")
        {
            FileOpeningLogContext.Info($"[Document.ctor] ========== NEW DOCUMENT WITH ASSEMBLIES ==========");
            FileOpeningLogContext.Info($"[Document.ctor] FilePath: {filePath}");
            FileOpeningLogContext.Info($"[Document.ctor] Assembly paths ({assemblyPaths?.Length ?? 0}): {string.Join(",", assemblyPaths ?? new string[0])}");
            FileOpeningLogContext.Info($"[Document.ctor] Project assembly: {projectAssemblyName}");
            
            _assemblyPaths = assemblyPaths;
            _projectAssemblyName = projectAssemblyName;
            FilePath = filePath;
            
            FileOpeningLogContext.Info($"[Document.ctor] Stored: _assemblyPaths has {_assemblyPaths?.Length ?? 0} items");
            FileOpeningLogContext.Info($"[Document.ctor] Calling ReloadFile()");
            ReloadFile();
            FileOpeningLogContext.Info($"[Document.ctor] ========== END DOCUMENT CONSTRUCTOR ==========");
        }

        public string Text
        {
            get => text;
            set
            {
                if (text != value)
                {
                    text = value;
                    XamlText = value; // Sync with XamlText property
                    IsDirty = true;
                    OnPropertyChanged(nameof(Text));
                }
            }
        }

        public DocumentMode Mode
        {
            get => mode;
            set
            {
                mode = value;
                if (InDesignMode)
                {
                    UpdateDesign();
                }
                else
                {
                    UpdateXaml();

                    // Handle selection line info for XAML mode
                    if (this.DesignContext.Services.Selection.PrimarySelection != null)
                    {
                        var sel = this.DesignContext.Services.Selection.PrimarySelection;
                        var ln = ((PositionXmlElement)((XamlDesignItem)sel).XamlObject.XmlElement).LineNumber;
                    }
                }
                OnPropertyChanged(nameof(Mode));
                OnPropertyChanged(nameof(InXamlMode));
                OnPropertyChanged(nameof(InDesignMode));
            }
        }

        public bool InXamlMode
        {
            get => Mode == DocumentMode.Xaml;
            set
            {
                if (value && Mode != DocumentMode.Xaml)
                {
                    Mode = DocumentMode.Xaml;
                }
            }
        }

        public bool InDesignMode
        {
            get => Mode == DocumentMode.Design;
            set
            {
                if (value && Mode != DocumentMode.Design)
                {
                    Mode = DocumentMode.Design;
                }
            }
        }

        public string FilePath
        {
            get => filePath;
            private set
            {
                filePath = value;
                OnPropertyChanged(nameof(FilePath));
                OnPropertyChanged(nameof(FileName));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Name));
            }
        }

        public bool IsDirty
        {
            get => isDirty;
            private set
            {
                isDirty = value;
                OnPropertyChanged(nameof(IsDirty));
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(Title));
            }
        }

        public XamlElementLineInfo XamlElementLineInfo
        {
            get => xamlElementLineInfo;
            private set
            {
                xamlElementLineInfo = value;
                OnPropertyChanged(nameof(XamlElementLineInfo));
            }
        }

        public string FileName => FilePath != null ? Path.GetFileName(FilePath) : null;



        public string Title => IsDirty ? Name + "*" : Name;

        public DesignSurface DesignSurface => designSurface;

        public DesignContext DesignContext => designSurface?.DesignContext;

        public UndoService UndoService => DesignContext?.Services.GetService<UndoService>();

        public ISelectionService SelectionService => InDesignMode ? DesignContext?.Services.Selection : null;

        public XamlErrorService XamlErrorService => DesignContext?.Services.GetService<XamlErrorService>();


       

        public IOutlineNode OutlineRoot
        {
            get
            {
                return outlineRoot;
            }
            private set
            {
                outlineRoot = value;
                OnPropertyChanged(nameof(OutlineRoot));
                
                // Notify Shell that CurrentDocument properties changed
                if (Shell.Instance.CurrentDocument == this)
                {
                    Shell.Instance.NotifyPropertyChanged(nameof(Shell.CurrentDocument));
                }
            }
        }

        partial void OnXamlTextChanged(string value)
        {
            if (text != value)
            {
                text = value;
                IsDirty = true;
            }
        }

        private void ReloadFile()
        {
            try
            {
                MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[Document.ReloadFile] ========== START ==========");
                MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[Document.ReloadFile] FilePath: {FilePath}");
                
                if (File.Exists(FilePath))
                {
                    MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[Document.ReloadFile] File exists, reading content...");
                    Text = File.ReadAllText(FilePath);
                    MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[Document.ReloadFile] File content loaded ({Text.Length} characters)");
                    MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[Document.ReloadFile] First 100 chars: {Text.Substring(0, Math.Min(100, Text.Length)).Replace('\n', ' ').Replace('\r', ' ')}");
                    MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[Document.ReloadFile] Calling UpdateDesign()...");
                    UpdateDesign();
                    MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[Document.ReloadFile] UpdateDesign() completed");
                    IsDirty = false;
                }
                else
                {
                    MyDesigner.Design.Services.Integration.FileOpeningLogContext.Error($"[Document.ReloadFile] File DOES NOT EXIST!");
                }
                MyDesigner.Design.Services.Integration.FileOpeningLogContext.Info($"[Document.ReloadFile] ========== END ==========");
            }
            catch (Exception ex)
            {
                MyDesigner.Design.Services.Integration.FileOpeningLogContext.Error($"[Document.ReloadFile] EXCEPTION: {ex.Message}");
                Shell.ReportException(ex);
            }
        }

        public void Save()
        {
            if (FilePath == null)
                throw new InvalidOperationException("Cannot save document without file path. Use SaveAs instead.");

            try
            {
                if (InDesignMode)
                {
                    UpdateXaml();
                }
                File.WriteAllText(FilePath, Text);
                IsDirty = false;
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
                throw;
            }
        }

        public void SaveAs(string filePath)
        {
            try
            {
                FilePath = filePath;
                Save();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
                throw;
            }
        }

        public void Refresh()
        {
            try
            {
                UpdateXaml();
                UpdateDesign();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        private void UpdateXaml()
        {
            try
            {
                if (DesignSurface?.DesignContext != null)
                {
                    var sb = new StringBuilder();
                    using (var xmlWriter = new XamlXmlWriter(sb))
                    {
                        DesignSurface.SaveDesigner(xmlWriter);
                        Dictionary<XamlElementLineInfo, XamlElementLineInfo> d;
                        Text = XamlFormatter.Format(sb.ToString(), out d);

                        if (DesignSurface.DesignContext.Services.Selection.PrimarySelection != null)
                        {
                            var item = DesignSurface.DesignContext.Services.Selection.PrimarySelection;
                            var line = ((PositionXmlElement)((XamlDesignItem)item).XamlObject.XmlElement).LineNumber;
                            var pos = (((XamlDesignItem)item).XamlObject.PositionXmlElement).LinePosition;
                            var newP = d.FirstOrDefault(x => x.Key.LineNumber == line && x.Key.LinePosition == pos);
                            XamlElementLineInfo = newP.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        private void UpdateDesign()
        {
            try
            {
                FileOpeningLogContext.Info($"[Document.UpdateDesign] ========== START ==========");
                FileOpeningLogContext.Info($"[Document.UpdateDesign] Text is null/empty: {string.IsNullOrEmpty(Text)}");
                
                if (!string.IsNullOrEmpty(Text))
                {
                    OutlineRoot = null;
                    
                    // Unsubscribe from previous UndoService events to prevent memory leaks
                    if (UndoService != null)
                    {
                        UndoService.UndoStackChanged -= UndoService_UndoStackChanged;
                    }
                    
                    using (var xmlReader = XmlReader.Create(new StringReader(Text)))
                    {
                        BasicMetadata.Register();

                        var loadSettings = new XamlLoadSettings();
                        
                        FileOpeningLogContext.Info($"[Document.UpdateDesign] _assemblyPaths is null: {_assemblyPaths == null}");
                        FileOpeningLogContext.Info($"[Document.UpdateDesign] _assemblyPaths length: {_assemblyPaths?.Length ?? 0}");
                        
                        // If assembly paths were provided (from server mode), load them
                        if (_assemblyPaths != null && _assemblyPaths.Length > 0)
                        {
                            FileOpeningLogContext.Info($"[Document.UpdateDesign] Loading {_assemblyPaths.Length} assemblies");
                            var typeFinder = XamlTypeFinder.CreateAvaloniaTypeFinder();
                            foreach (var asmPath in _assemblyPaths)
                            {
                                try
                                {
                                    FileOpeningLogContext.Info($"[Document.UpdateDesign] Loading assembly: {asmPath}");
                                    var asm = System.Reflection.Assembly.LoadFrom(asmPath);
                                    typeFinder.RegisterAssembly(asm);
                                    FileOpeningLogContext.Info($"[Document.UpdateDesign] Successfully loaded: {asm.GetName().Name}");
                                }
                                catch (Exception ex)
                                {
                                    FileOpeningLogContext.Error($"[Document.UpdateDesign] Failed to load {asmPath}: {ex.Message}");
                                    Shell.ReportException(ex);
                                }
                            }
                            loadSettings.TypeFinder = typeFinder;
                            loadSettings.CurrentProjectAssemblyName = _projectAssemblyName;
                            FileOpeningLogContext.Info($"[Document.UpdateDesign] TypeFinder and project name set");
                        }
                        else
                        {
                            FileOpeningLogContext.Info($"[Document.UpdateDesign] No assembly paths provided, using default TypeFinder");
                        }

                        FileOpeningLogContext.Info($"[Document.UpdateDesign] Calling DesignSurface.LoadDesigner()...");
                        DesignSurface.LoadDesigner(xmlReader, loadSettings);
                        FileOpeningLogContext.Info($"[Document.UpdateDesign] DesignSurface.LoadDesigner() completed");
                    }
                    if (DesignContext.RootItem != null)
                    {
                        FileOpeningLogContext.Info($"[Document.UpdateDesign] RootItem created, building outline");
                        OutlineRoot = DesignContext.RootItem.CreateOutlineNode();
                        if (UndoService != null)
                        {
                            UndoService.UndoStackChanged += UndoService_UndoStackChanged;
                        }
                    }
                    else
                    {
                        FileOpeningLogContext.Error($"[Document.UpdateDesign] RootItem is NULL after LoadDesigner");
                    }
                }
                FileOpeningLogContext.Info($"[Document.UpdateDesign] ========== END ==========");
            }
            catch (Exception ex)
            {
                FileOpeningLogContext.Error($"[Document.UpdateDesign] EXCEPTION: {ex.Message}");
                FileOpeningLogContext.Error($"[Document.UpdateDesign] Exception details: {ex}");
                Shell.ReportException(ex);
            }
        }

        private void NotifyDesignSurfaceChanged()
        {
            OnPropertyChanged(nameof(SelectionService));
            OnPropertyChanged(nameof(XamlErrorService));
            OnPropertyChanged(nameof(DesignSurface));
            OnPropertyChanged(nameof(OutlineRoot));
            
            // Notify Shell that this document's properties have changed
            if (Shell.Instance.CurrentDocument == this)
            {
                Shell.Instance.NotifyPropertyChanged(nameof(Shell.CurrentDocument));
            }
        }

        // Add cleanup method for proper disposal
        public void Cleanup()
        {
            try
            {
                // Unsubscribe from events to prevent memory leaks
                if (UndoService != null)
                {
                    UndoService.UndoStackChanged -= UndoService_UndoStackChanged;
                }
                
                // Dispose design surface if it implements IDisposable
                if (designSurface is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        private void UndoService_UndoStackChanged(object sender, EventArgs e)
        {
            IsDirty = true;
            if (InXamlMode)
            {
                UpdateXaml();
            }
        }
    }

    public enum DocumentMode
    {
        Xaml,
        Design,Code
    }

 
}