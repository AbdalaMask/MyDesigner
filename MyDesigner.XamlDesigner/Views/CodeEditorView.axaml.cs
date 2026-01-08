using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CSharpEditor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MyDesigner.XamlDesigner.ViewModels;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;


namespace MyDesigner.XamlDesigner.Views
{
    public partial class CodeEditorView : UserControl
    {
        public DocumentViewModel ViewModel { get; private set; }
        private CSharpEditor.Editor Editor;

        public CodeEditorView()
        {
            InitializeComponent();
            this.Loaded += CodeEditorView_Loaded;
        }

        public Document Document { get; private set; }

        private async void CodeEditorView_Loaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is Document document)
            {
                Document = document;
                ViewModel = new DocumentViewModel(document);
                DataContext = ViewModel;
                await SetupCodeEditor();
            }
            else if (DataContext is ViewModels.CodeEditorDock codeEditorDock)
            {
                Document = codeEditorDock.Document;
                ViewModel = new DocumentViewModel(codeEditorDock.Document);
                DataContext = ViewModel;
                await SetupCodeEditor();
            }
        }

        private async Task SetupCodeEditor()
        {
            if (Document == null) return;

            // قراءة محتوى ملف C#
            string sourceText = "";
            if (!string.IsNullOrEmpty(Document.FilePath) && File.Exists(Document.FilePath))
            {
                sourceText = await File.ReadAllTextAsync(Document.FilePath);
                Document.Text = sourceText;
            }
            else
            {
                // استخدام الملف الافتراضي إذا لم يكن هناك ملف
                using (Stream stream = this.GetType().Assembly.GetManifestResourceStream("MyDesigner.XamlDesigner.HelloWorld.cs"))
                using (StreamReader reader = new StreamReader(stream))
                {
                    sourceText = reader.ReadToEnd();
                    Document.Text = sourceText;
                }
            }

            // إعداد المراجع الأساسية
            string systemRuntime = Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Runtime.dll");
            CSharpEditor.CachedMetadataReference[] minimalReferences = new CSharpEditor.CachedMetadataReference[]
            {
                CSharpEditor.CachedMetadataReference.CreateFromFile(systemRuntime),
                CSharpEditor.CachedMetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                CSharpEditor.CachedMetadataReference.CreateFromFile(typeof(Button).Assembly.Location),
                CSharpEditor.CachedMetadataReference.CreateFromFile(typeof(ColorPicker).Assembly.Location),
                CSharpEditor.CachedMetadataReference.CreateFromFile(typeof(AvaloniaObject).Assembly.Location),
                CSharpEditor.CachedMetadataReference.CreateFromFile(typeof(Binding).Assembly.Location),
                CSharpEditor.CachedMetadataReference.CreateFromFile(typeof(AvaloniaXamlLoader).Assembly.Location),

            };

            try
            {
                Editor = await CSharpEditor.Editor.Create(sourceText, references: minimalReferences, 
                    compilationOptions: new CSharpCompilationOptions(OutputKind.WindowsApplication));

                Grid.SetRow(Editor, 1);
                this.FindControl<Grid>("MainGrid").Children.Add(Editor);

                // إعداد أحداث الأزرار
                SetupButtons();

                // ربط تغييرات النص مع المستند
                Editor.TextChanged += (s, e) =>
                {
                    if (Document != null)
                    {
                        Document.Text = Editor.Text;
                    }
                };

                // الاستماع لتغييرات المستند
                Document.PropertyChanged += Document_PropertyChanged;
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        private void SetupButtons()
        {
            // زر التشغيل
            var runButton = this.FindControl<Button>("RunButton");
            if (runButton != null)
            {
                runButton.Click += async (s, e) =>
                {
                    try
                    {
                        if (Editor != null)
                        {
                            Assembly assembly = (await Editor.Compile(Editor.SynchronousBreak, Editor.AsynchronousBreak)).Assembly;

                            if (assembly != null)
                            {
                                new Thread(() =>
                                {
                                    try
                                    {
                                        assembly.EntryPoint.Invoke(null, new object[assembly.EntryPoint.GetParameters().Length]);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Runtime Error: {ex.Message}");
                                    }
                                }).Start();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Shell.ReportException(ex);
                    }
                };
            }

            // زر الحفظ
            var saveButton = this.FindControl<Button>("SaveButton");
            if (saveButton != null)
            {
                saveButton.Click += (s, e) =>
                {
                    try
                    {
                        if (Document != null)
                        {
                            Document.Save();
                        }
                    }
                    catch (Exception ex)
                    {
                        Shell.ReportException(ex);
                    }
                };
            }
        }

        private void Document_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Document == null) return;

            // معالجة تغييرات النص لمحرر C#
            if (Editor != null && e.PropertyName == "Text" && Document.Text != Editor.Text)
            {
              //  Editor.Text = Document.Text ?? string.Empty;
            }
        }
    }
}