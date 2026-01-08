using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MyDesigner.XamlDesigner.Configuration;
using MyDesigner.XamlDesigner.Models;
using MyDesigner.XamlDesigner.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;


namespace MyDesigner.XamlDesigner;

public partial class ProjectExplorerView : UserControl
{
    private string currentFileName;
    private string[] xamlCsFiles;
    public ProjectExplorerView()
    {
        InitializeComponent();
        FilesTreeView = this.FindControl<TreeView>("FilesTreeView");
        this.DataContext = App.ExplorerVM;
        // تسجيل هذا الـ View في الـ Service
        Services.ProjectService.Instance.RegisterProjectExplorer(this);
    }
  

    private void FilesTreeView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
      
    }

    private async void AddNewFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProjectExplorerViewViewModel vm)
        {
            // نمرر الـ StorageProvider الخاص بالنافذة الحالية للـ ViewModel
            await vm.OpenFolderAsync(TopLevel.GetTopLevel(this).StorageProvider);
        }
    }

    private void AddExistingFile_Click(object? sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }

    private void Edit_Click(object? sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }
    public string filePath = string.Empty;
   
  

    public async void OpenFolder() // أصبحت async
    {
        try
        {
            // محاولة الحصول على TopLevel من عدة مصادر
            var topLevel = TopLevel.GetTopLevel(this) ??
                          (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow as TopLevel ??
                          MainWindow.Instance as TopLevel;

            if (topLevel == null)
            {
                Console.WriteLine("لا يمكن الحصول على TopLevel للحوار");
                return;
            }

            // إعداد الفلاتر
            var options = new FilePickerOpenOptions
            {
                Title = "اختر ملف المشروع - Select Project File",
                FileTypeFilter = new[] {
                new FilePickerFileType("C# Project Files") { Patterns = new[] { "*.csproj" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
            },
                AllowMultiple = false
            };

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);

            if (files.Count > 0 && files[0].Path != null)
            {
                var csprojPath = files[0].Path.LocalPath;

                if (!string.IsNullOrEmpty(csprojPath))
                {
                    var projectFolder = Path.GetDirectoryName(csprojPath);

                    if (!string.IsNullOrEmpty(projectFolder))
                    {
                        CloseAllDocuments();
                        // في Avalonia نستخدم ItemsSource غالباً، لكن إذا كنت تستخدم Items مباشرة:
                        ((IList<object>)FilesTreeView.Items).Clear();
                      //  currentFileName = string.Empty;
                       // LoadFilesToSolution(projectFolder);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Shell.ReportException(ex);
        }
    }
    /// <summary>
    ///     إغلاق جميع المستندات المفتوحة (XAML وصفحات الكود) ومسح الأخطاء
    /// </summary>
    public void CloseAllDocuments()
    {
        try
        {
            // مسح رسائل الأخطاء من المشروع السابق
            try
            {
                if (Shell.Instance?.CurrentDocument?.XamlErrorService?.Errors != null)
                {
                    Shell.Instance.CurrentDocument.XamlErrorService.Errors.Clear();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في مسح الأخطاء: {ex.Message}");
            }

            // إغلاق مستندات XAML
            try
            {
                Shell.Instance?.CloseAll();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في إغلاق مستندات XAML: {ex.Message}");
            }


        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في إغلاق جميع المستندات: {ex.Message}");
        }
    }
    private void FilesTreeView_SelectedItemChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FilesTreeView.SelectedItem is TreeViewItem selectedItem && selectedItem.Header is FileItem fileItem)
        {
            filePath = fileItem.FullPath;

            if (File.Exists(filePath))
            {
                // Read file content
                var fileContent = File.ReadAllText(filePath);

                // Determine file type and perform actions
                if (filePath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
                {
                    // Check if the file is a MAUI file and convert it
                    if (fileContent.Contains("xmlns=\"http://schemas.microsoft.com/dotnet/2021/maui\""))
                    {
                        // Convert MAUI file to WPF
                        //var wpfXaml = new MauiToWpfConverter().Convert(fileContent);

                        //// Display the output in the editor
                        //var tempPath = Path.GetTempFileName() + ".xaml";
                        //File.WriteAllText(tempPath, wpfXaml);
                        //currentFileName = tempPath;
                        // Shell.Instance.OpenXaml(tempPath, filePath);


                    }
                    else
                    {
                        // Handle standard WPF XAML files
                      //  currentFileName = filePath;
                        // Shell.Instance.OpenXaml(filePath);
                    }


                }
                else if (filePath.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase))
                {
                    //// Convert Avalonia file to WPF
                    //var wpfXaml = new AvaloniaToWpfConverter().Convert(fileContent);

                    //// Display the output in the editor
                    //var tempPath = Path.GetTempFileName() + ".xaml";
                    //File.WriteAllText(tempPath, wpfXaml);
                    //currentFileName = tempPath;
                    //Shell.Instance.Open(tempPath, filePath);


                }
                else if (filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                         filePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                         filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                         filePath.EndsWith(".config", StringComparison.OrdinalIgnoreCase) ||
                         filePath.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
                         filePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                         filePath.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
                         filePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {

                }
            }
        }
    }


    /// <summary>
    /// فتح مشروع موجود
    /// </summary>
    public async Task OpenProject()
    {
        try
        {
            // محاولة الحصول على TopLevel من عدة مصادر
            var topLevel = TopLevel.GetTopLevel(this) ??
                          (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow as TopLevel ??
                          MainWindow.Instance as TopLevel;

            if (topLevel == null)
            {
                Console.WriteLine("لا يمكن الحصول على TopLevel للحوار");
                return;
            }
            var viewModel = DataContext as ProjectExplorerViewViewModel;
            if (viewModel == null)
            {
                Console.WriteLine("[LoadFilesToSolution] خطأ: لا يمكن الحصول على ViewModel");
                return;
            }
            var options = new FilePickerOpenOptions
            {
                Title = "فتح مشروع - Open Project",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Project Files")
                    {
                        Patterns = new[] { "*.csproj", "*.vbproj", "*.fsproj", "*.sln" }
                    },
                    new FilePickerFileType("Solution Files")
                    {
                        Patterns = new[] { "*.sln" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            };

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);

            if (files.Count > 0 && files[0].Path != null)
            {
                var selectedFile = files[0].Path.LocalPath;

                if (!string.IsNullOrEmpty(selectedFile))
                {
                    if (selectedFile.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                    {
                        // تحميل ملف Solution
                        var folderPath = Path.GetDirectoryName(selectedFile);
                        if (!string.IsNullOrEmpty(folderPath))
                        {
                            viewModel.LoadAllProjectsFromSolution(selectedFile, folderPath);
                        }
                    }
                    else if (selectedFile.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                             selectedFile.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase) ||
                             selectedFile.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase))
                    {
                        // تحميل مشروع واحد
                        viewModel. LoadProjectToTree(selectedFile);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Shell.ReportException(ex);
        }
    }

    /// <summary>
    /// فتح مجلد مشروع
    /// </summary>
    public async Task OpenFolderDialog()
    {
        try
        {
            // محاولة الحصول على TopLevel من عدة مصادر
            var topLevel = TopLevel.GetTopLevel(this) ??
                          (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow as TopLevel ??
                          MainWindow.Instance as TopLevel;

            if (topLevel == null)
            {
                Console.WriteLine("لا يمكن الحصول على TopLevel للحوار");
                return;
            }
            var viewModel = DataContext as ProjectExplorerViewViewModel;
            if (viewModel == null)
            {
                Console.WriteLine("[LoadFilesToSolution] خطأ: لا يمكن الحصول على ViewModel");
                return;
            }
            var options = new FolderPickerOpenOptions
            {
                Title = "اختيار مجلد المشروع - Select Project Folder",
                AllowMultiple = false
            };

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(options);

            if (folders.Count > 0 && folders[0].Path != null)
            {
                var selectedFolder = folders[0].Path.LocalPath;
                if (!string.IsNullOrEmpty(selectedFolder))
                {
                    viewModel. LoadFilesToSolution(selectedFolder);
                }
            }
        }
        catch (Exception ex)
        {
            Shell.ReportException(ex);
        }
    }

    /// <summary>
    /// فتح ملف واحد
    /// </summary>
    public async Task OpenFile()
    {
        try
        {
            // محاولة الحصول على TopLevel من عدة مصادر
            var topLevel = TopLevel.GetTopLevel(this) ??
                          (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow as TopLevel ??
                          MainWindow.Instance as TopLevel;

            if (topLevel == null)
            {
                Console.WriteLine("لا يمكن الحصول على TopLevel للحوار");
                return;
            }

            var options = new FilePickerOpenOptions
            {
                Title = "فتح ملف - Open File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("XAML Files")
                    {
                        Patterns = new[] { "*.xaml", "*.axaml" }
                    },
                    new FilePickerFileType("Code Files")
                    {
                        Patterns = new[] { "*.cs", "*.vb", "*.fs" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            };

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);

            if (files.Count > 0 && files[0].Path != null)
            {
                var selectedFile = files[0].Path.LocalPath;

                if (!string.IsNullOrEmpty(selectedFile))
                {
                    // فتح الملف في المحرر
                    if (selectedFile.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) ||
                        selectedFile.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase))
                    {
                        Shell.Instance.Open(selectedFile);
                    }
                    else
                    {
                        // فتح ملفات أخرى
                        Shell.Instance.Open(selectedFile);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Shell.ReportException(ex);
        }
    }



    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        var viewModel = DataContext as ProjectExplorerViewViewModel;
        if (viewModel == null)
        {
            Console.WriteLine("[LoadFilesToSolution] خطأ: لا يمكن الحصول على ViewModel");
            return;
        }
        if (!string.IsNullOrEmpty(filePath))
        {
            if (File.Exists(filePath))
            {
                var xamlFileName = Path.GetFileNameWithoutExtension(filePath).Trim();
                if (Settings.Default.ProjectType == "WPF" || Settings.Default.ProjectType == "Maui")
                {
                    // Search for the corresponding .xaml.cs file
                    var csFile = xamlCsFiles.FirstOrDefault(cs => string.Equals(Path.GetFileNameWithoutExtension(cs).Replace(".xaml", string.Empty), xamlFileName, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(csFile))
                        File.Delete(csFile);
                }
                else
                {
                    // Search for the corresponding .axaml.cs file
                    var csFile = xamlCsFiles.FirstOrDefault(cs => string.Equals(Path.GetFileNameWithoutExtension(cs).Replace(".axaml", string.Empty), xamlFileName, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(csFile))
                        File.Delete(csFile);
                }
                File.Delete(filePath);

                viewModel.LoadFilesToSolution(Settings.Default.ProjectPath);
            }
        }
    }

    private void SetAsStartupProject_Click(object? sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }

    private void RunProject_Click(object? sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }

    private void StopProject_Click(object? sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }

    private void OpenProjectLocation_Click(object? sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }

    private void CloseTree_Click(object? sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }

    private void ExpandTree_Click(object? sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }
}