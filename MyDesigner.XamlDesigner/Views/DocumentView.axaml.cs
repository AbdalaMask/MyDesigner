using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using CSharpEditor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MyDesigner.Designer.Controls;
using MyDesigner.XamlDesigner.Tools;
using MyDesigner.XamlDesigner.ViewModels;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyDesigner.XamlDesigner.Views
{
    public partial class DocumentView : UserControl
    {
        public DocumentViewModel ViewModel { get; private set; }
        private AvaloniaEdit.TextEditor? _textEditor;
        private ContentPresenter? _designSurface;
        CSharpEditor.Editor Editor;
        public DocumentView()
        {
            InitializeComponent();
           

         
            this.Loaded += DocumentView_Loaded;
        }

        public Document Document { get; private set; }

        private void DocumentView_Loaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is Document document)
            {
                Document = document;
                ViewModel = new DocumentViewModel(document);
                DataContext = ViewModel;
                Shell.Instance.Views[Document] = this;
                Document.Mode = DocumentMode.Design;
                SetupDocument();
            }
            else if (DataContext is ViewModels.DocumentDock documentDock)
            {
                Document = documentDock.Document;
                ViewModel = new DocumentViewModel(documentDock.Document);
                DataContext = ViewModel;
                Shell.Instance.Views[Document] = this;
                Document.Mode = DocumentMode.Design;
                SetupDocument();
            }
        }

        private async void SetupDocument()
        {
            if (Document == null) return;


            // Initial source code
            string sourceText = "";
            using (Stream stream = this.GetType().Assembly.GetManifestResourceStream("MyDesigner.XamlDesigner.HelloWorld.cs"))
            using (StreamReader reader = new StreamReader(stream))
            {
                sourceText = reader.ReadToEnd();
            }

            // Minimal set of references for a console application - double check these with your target framework version - sometimes they change.
            string systemRuntime = Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Runtime.dll");
            CSharpEditor.CachedMetadataReference[] minimalReferences = new CSharpEditor.CachedMetadataReference[]
            {
                    CSharpEditor.CachedMetadataReference.CreateFromFile(systemRuntime),                                           // System.Runtime.dll
                    CSharpEditor.CachedMetadataReference.CreateFromFile(typeof(object).Assembly.Location),                        // System.Private.CoreLib.dll
                    CSharpEditor.CachedMetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location)                 // System.Console.dll
            };

            Editor = await CSharpEditor.Editor.Create(sourceText, references: minimalReferences, compilationOptions: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

            Grid.SetRow(Editor, 1);
            this.FindControl<Grid>("MainGrid").Children.Add(Editor);


            this.FindControl<Button>("RunButton").Click += async (s, e) =>
            {
                Assembly assembly = (await Editor.Compile(Editor.SynchronousBreak, Editor.AsynchronousBreak)).Assembly;

                if (assembly != null)
                {
                    // Run on a separate thread, in order to enable breakpoints in synchronous functions.
                    // No need to use a separate thread if the entry point is async.
                    new Thread(() =>
                    {
                        assembly.EntryPoint.Invoke(null, new object[assembly.EntryPoint.GetParameters().Length]);
                    }).Start();
                }
            };

            Document.PropertyChanged += Document_PropertyChanged;
            
            // Find controls
            _textEditor = this.FindControl<AvaloniaEdit.TextEditor>("uxTextEditor");
            _designSurface = this.FindControl<ContentPresenter>("uxDesignSurface");
            var enumBar = this.FindControl<EnumBar>("uxEnumBar");

            if (_textEditor != null)
            {
                _textEditor.TextChanged += uxTextEditor_TextChanged;
                _textEditor.Text = Document.Text ?? string.Empty;
            }

            if (_designSurface != null)
            {
                _designSurface.Content = Document.DesignSurface;
            }

            if (enumBar != null)
            {
                // EnumBar will be bound to ViewModel.Mode via XAML binding
                // No need to set Value manually
            }
        }

        void uxTextEditor_TextChanged(object sender, EventArgs e)
        {
            if (Document != null && _textEditor != null)
            {
                Document.Text = _textEditor.Text;
            }
        }

        void Document_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_textEditor == null || Document == null) return;

            if (e.PropertyName == "Text" && Document.Text != _textEditor.Text)
                _textEditor.Text = Document.Text ?? string.Empty;
            if (e.PropertyName == "XamlElementLineInfo")
            {
                try
                {
                    // Use Dispatcher.UIThread.Post instead of Task.Delay to avoid blocking
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            if (Document.XamlElementLineInfo != null)
                            {
                                _textEditor.SelectionLength = 0;
                                _textEditor.SelectionStart = Document.XamlElementLineInfo.Position;
                                _textEditor.SelectionLength = Document.XamlElementLineInfo.Length;
                            }
                            else
                            {
                                _textEditor.SelectionStart = 0;
                                _textEditor.SelectionLength = 0;
                            }

                            _textEditor.Focus();
                        }
                        catch (Exception)
                        {
                            // Ignore selection errors
                        }
                    }, Avalonia.Threading.DispatcherPriority.Background);
                }
                catch (Exception)
                { }
            }
        }

        public void JumpToError(XamlError error)
        {
            if (Document == null || _textEditor == null) return;

            Document.Mode = DocumentMode.Xaml;
            try
            {
                _textEditor.ScrollTo(error.Line, error.Column);
                _textEditor.CaretOffset = _textEditor.Document.GetOffset(error.Line, error.Column);

                int n = 0;
                char chr;
                while ((chr = _textEditor.Document.GetCharAt(_textEditor.CaretOffset + n)) != ' ' && chr != '.' && chr != '<' && chr != '>' && chr != '"')
                { n++; }

                _textEditor.SelectionLength = n;
            }
            catch (ArgumentException)
            {
                // invalid line number
            }
        }
    }
}
