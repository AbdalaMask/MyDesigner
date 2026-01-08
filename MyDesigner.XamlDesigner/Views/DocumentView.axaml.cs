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

            // هذا المحرر مخصص فقط لملفات XAML
            // ملفات C# ستستخدم CodeEditorView منفصل
            if (!string.IsNullOrEmpty(Document.FilePath))
            {
                if (Document.FilePath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) ||
                         Document.FilePath.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase))
                {
                    // تفعيل وضع التصميم لملفات XAML
                    Document.Mode = DocumentMode.Design;
                }
            }

            await SetupXamlEditor();
        }

        private async Task SetupXamlEditor()
        {
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
            if (Document == null) return;

            // معالجة تغييرات النص لمحرر XAML فقط
            if (_textEditor != null && e.PropertyName == "Text" && Document.Text != _textEditor.Text)
            {
                _textEditor.Text = Document.Text ?? string.Empty;
            }

            if (e.PropertyName == "XamlElementLineInfo")
            {
                try
                {
                    // Use Dispatcher.UIThread.Post instead of Task.Delay to avoid blocking
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            if (Document.XamlElementLineInfo != null && _textEditor != null)
                            {
                                _textEditor.SelectionLength = 0;
                                _textEditor.SelectionStart = Document.XamlElementLineInfo.Position;
                                _textEditor.SelectionLength = Document.XamlElementLineInfo.Length;
                            }
                            else if (_textEditor != null)
                            {
                                _textEditor.SelectionStart = 0;
                                _textEditor.SelectionLength = 0;
                            }

                            _textEditor?.Focus();
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
