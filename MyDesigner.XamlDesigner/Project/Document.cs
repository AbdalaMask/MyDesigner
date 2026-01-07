using CommunityToolkit.Mvvm.ComponentModel;
using MyDesigner.Design;
using MyDesigner.Design.Interfaces;
using MyDesigner.Designer;
using MyDesigner.Designer.Services;
using MyDesigner.Designer.Xaml;
using MyDesigner.XamlDesigner.Helpers;
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
                OnPropertyChanged(nameof(InCodeMode));
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
        public bool InCodeMode
        {
            get => Mode == DocumentMode.Code;
            set
            {
                if (value && Mode != DocumentMode.Code)
                {
                    Mode = DocumentMode.Code;
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

        public double PageWidth { get; internal set; }
        public double PageHeight { get; internal set; }
        public DocumentType Platform { get; internal set; }

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
                if (File.Exists(FilePath))
                {
                    Text = File.ReadAllText(FilePath);
                    UpdateDesign();
                    IsDirty = false;
                }
            }
            catch (Exception ex)
            {
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
                       // loadSettings.DesignerAssemblies.Add(this.GetType().Assembly);

                        DesignSurface.LoadDesigner(xmlReader, loadSettings);
                    }
                    if (DesignContext.RootItem != null)
                    {
                        OutlineRoot = DesignContext.RootItem.CreateOutlineNode();
                        if (UndoService != null)
                        {
                            UndoService.UndoStackChanged += UndoService_UndoStackChanged;
                        }
                    }
                }
                
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
            catch (Exception ex)
            {
                Shell.ReportException(ex);
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