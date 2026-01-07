using Avalonia;
using Avalonia.Controls;
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
using System.Xml;
using System.Xml.Linq;


namespace MyDesigner.XamlDesigner;

public partial class ProjectExplorerView : UserControl
{
    public ProjectExplorerView()
    {
        InitializeComponent();
        FilesTreeView = this.FindControl<TreeView>("FilesTreeView");
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
   
    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
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

                LoadFilesToSolution(Settings.Default.ProjectPath);
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


    #region New

 

public async void OpenFolder() // أصبحت async
{
    // الحصول على الـ Visual Root (غالباً الـ Window)
    var topLevel = TopLevel.GetTopLevel(this);

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

    if (files.Count > 0)
    {
        var csprojPath = files[0].Path.LocalPath;
        var projectFolder = Path.GetDirectoryName(csprojPath);

        if (!string.IsNullOrEmpty(projectFolder))
        {
            CloseAllDocuments();
            // في Avalonia نستخدم ItemsSource غالباً، لكن إذا كنت تستخدم Items مباشرة:
            ((IList<object>)FilesTreeView.Items).Clear();
            currentFileName = string.Empty;
            LoadFilesToSolution(projectFolder);
        }
    }
}


private string currentFileName;
    private string[] xamlCsFiles;

    /// <summary>
    /// تحميل جميع المشاريع من ملف .sln
    /// </summary>
    private void LoadAllProjectsFromSolution(string slnPath, string folderPath)
    {
        try
        {

            // قراءة محتوى ملف .sln
            var slnContent = File.ReadAllText(slnPath);
            var lines = slnContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var projectPaths = new List<string>();

            // البحث عن جميع المشاريع في .sln
            foreach (var line in lines)
            {
                if (line.StartsWith("Project("))
                {
                    var parts = line.Split(new[] { '"' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 5)
                    {
                        var csprojRelativePath = parts[5];
                        if (csprojRelativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                        {
                            var csprojFullPath = Path.Combine(folderPath, csprojRelativePath);
                            if (File.Exists(csprojFullPath))
                            {
                                projectPaths.Add(csprojFullPath);
                            }
                        }
                    }
                }
            }



            if (projectPaths.Count == 0)
            {
                LoadSingleProject(folderPath);
                return;
            }

            // إنشاء عقدة Solution
            var solutionName = Path.GetFileNameWithoutExtension(slnPath);
            var solutionItem = new TreeViewItem
            {
                Header = new FileItem
                {
                    Name = $"Solution '{solutionName}' ({projectPaths.Count} of {projectPaths.Count} projects)",
                    Icon = "/Assets/Visual_Studio_Icon_2022.png",
                    FullPath = slnPath,
                    ItemType = FileItemType.Solution
                },
                            HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
                IsExpanded = true
            };

            // تنظيم المشاريع حسب المجلدات
            var folderDict = new Dictionary<string, TreeViewItem>(StringComparer.OrdinalIgnoreCase);
            folderDict[""] = solutionItem; // المجلد الجذر هو Solution نفسه

            foreach (var projectPath in projectPaths)
            {
                var relativePath = projectPath.Substring(folderPath.Length + 1);
                var projectFolder = Path.GetDirectoryName(relativePath)?.Replace("\\", "/").Replace("../", "") ?? "";

                // إنشاء المجلدات إذا لم تكن موجودة
                if (!string.IsNullOrEmpty(projectFolder))
                {
                    EnsureSolutionFolderExists(folderDict, projectFolder, folderPath, solutionItem);
                }

                // تحميل المشروع
                var loader = new View.ProjectLoader();
                var projectDir = Path.GetDirectoryName(projectPath);

                if (loader.LoadProject(projectDir))
                {
                    var projectItem = CreateProjectTreeItem(loader, projectDir);

                    // إضافة المشروع إلى المجلد المناسب
                    var parentFolder = string.IsNullOrEmpty(projectFolder) ? solutionItem : folderDict[projectFolder];
                    parentFolder.Items.Add(projectItem);
                }
            }


            FilesTreeView.Items.Add(solutionItem);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في تحميل Solution: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            LoadSingleProject(folderPath);
        }
    }

    /// <summary>
    /// التأكد من وجود مجلد Solution في القاموس
    /// </summary>
    private void EnsureSolutionFolderExists(Dictionary<string, TreeViewItem> folderDict, string path, string basePath, TreeViewItem solutionItem)
    {
        if (string.IsNullOrEmpty(path) || folderDict.ContainsKey(path))
            return;

        var parts = path.Split('/');
        var currentPath = "";

        for (int i = 0; i < parts.Length; i++)
        {
            var parentPath = currentPath;
            currentPath = i == 0 ? parts[i] : $"{currentPath}/{parts[i]}";

            if (!folderDict.ContainsKey(currentPath))
            {
                var folderItem = new TreeViewItem
                {
                    Header = new FileItem
                    {
                        Name = parts[i],
                        Icon = "/Assets/folder_icon.png", // أيقونة المجلد
                        FullPath = Path.Combine(basePath, currentPath.Replace("/", "\\")),
                        ItemType = FileItemType.Folder
                    },
                                HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
                };

                folderDict[currentPath] = folderItem;

                // إضافة المجلد للمجلد الأب
                var parentFolder = string.IsNullOrEmpty(parentPath) ? solutionItem : folderDict[parentPath];
                parentFolder.Items.Add(folderItem);
            }
        }
    }

    /// <summary>
    /// تحميل مشروع واحد
    /// </summary>
    private void LoadSingleProject(string folderPath)
    {
        var loader = new View.ProjectLoader();
        if (!loader.LoadProject(folderPath))
        {
            //System.Windows.MessageBox.Show("فشل تحميل المشروع. تأكد من وجود ملف .csproj", "خطأ",
            //    MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Console.WriteLine($"تم تحميل المشروع: {loader.ProjectName}");
        Console.WriteLine($"نوع المشروع: {loader.ProjectType}");

        var projectItem = CreateProjectTreeItem(loader, folderPath);
        FilesTreeView.Items.Add(projectItem);
        projectItem.IsExpanded = true;
    }

    /// <summary>
    /// تحميل مشروع محدد إلى الشجرة
    /// </summary>
    private void LoadProjectToTree(string csprojPath)
    {
        try
        {
            var projectFolder = Path.GetDirectoryName(csprojPath);
            var loader = new View.ProjectLoader();

            if (!loader.LoadProject(projectFolder))
            {
                Console.WriteLine($"فشل تحميل المشروع: {csprojPath}");
                return;
            }

            Console.WriteLine($"تم تحميل المشروع: {loader.ProjectName}");

            var projectItem = CreateProjectTreeItem(loader, projectFolder);
            FilesTreeView.Items.Add(projectItem);
            projectItem.IsExpanded = false; // لا نوسع المشاريع الفرعية تلقائياً
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في تحميل المشروع {csprojPath}: {ex.Message}");
        }
    }

    /// <summary>
    /// إنشاء عنصر شجرة للمشروع
    /// </summary>
    private TreeViewItem CreateProjectTreeItem(View.ProjectLoader loader, string folderPath)
    {
        // التحقق من أن هذا هو المشروع الرئيسي
        bool isStartupProject = Settings.Default.ProjectPath == folderPath;

        // إضافة علامة للمشروع الرئيسي
        string projectDisplayName = isStartupProject
            ? $"▶ {loader.ProjectName} ({loader.ProjectType})"
            : $"{loader.ProjectName} ({loader.ProjectType})";

        var projectItem = new TreeViewItem
        {
            Header = new FileItem
            {
                Name = projectDisplayName,
                Icon = "/Assets/Visual_Studio_Icon_2022.png",
                FullPath = folderPath,
                ItemType = FileItemType.Project
            },
            HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate")
        };

        // تغيير لون الخط للمشروع الرئيسي
        if (isStartupProject)
        {
            projectItem.FontWeight = Avalonia.Media.FontWeight.Bold;
        }

        // إضافة Dependencies
        Console.WriteLine($"[CreateProjectTreeItem] Adding Dependencies for {loader.ProjectName}");
        AddDependenciesNode(projectItem, folderPath, loader.ProjectType);
        Console.WriteLine($"[CreateProjectTreeItem] Dependencies added. Project has {projectItem.Items.Count} items");

        // إضافة الملفات في المجلد الرئيسي
        if (loader.Structure.RootFiles != null)
        {
            foreach (var file in loader.Structure.RootFiles.OrderBy(f => f.Name))
            {
                AddFileToTree(projectItem, file, loader.ProjectType);
            }
        }

        // إضافة المجلدات
        if (loader.Structure.Folders != null)
        {
            foreach (var folder in loader.Structure.Folders.OrderBy(f => f.Name))
            {
                AddFolderToTree(projectItem, folder, loader.ProjectType);
            }
        }

        // تحميل المراجع لمشاريع WPF
        if (loader.ProjectType == "WPF")
        {
            LoadProjectReferences(folderPath);
        }

        return projectItem;
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

    /// <summary>
    ///     تحميل المشروع باستخدام ProjectLoader الجديد مع عرض Dependencies
    /// </summary>
    public void LoadFilesToSolution(string folderPath)
    {
        try
        {


            // مسح العناصر السابقة
            FilesTreeView.Items.Clear();

            // البحث عن ملف .sln
            var slnFiles = Directory.GetFiles(folderPath, "*.sln", SearchOption.TopDirectoryOnly);



            if (slnFiles.Length > 0)
            {

                // إذا وجد ملف .sln، نحمل جميع المشاريع منه
                LoadAllProjectsFromSolution(slnFiles[0], folderPath);
            }
            else
            {

                // إذا لم يوجد .sln، نحمل المشروع الواحد
                LoadSingleProject(folderPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في LoadFilesToTreeView3: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            //System.Windows.MessageBox.Show($"خطأ في تحميل المشروع:\n{ex.Message}", "خطأ",
            //    MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     إضافة عقدة Dependencies مثل Visual Studio
    /// </summary>
    private void AddDependenciesNode(TreeViewItem projectItem, string projectPath, string projectType)
    {
        Console.WriteLine($"[AddDependenciesNode] Starting for project: {projectPath}, type: {projectType}");

        var dependenciesItem = new TreeViewItem
        {
            Header = new FileItem
            {
                Name = "Dependencies",
                Icon = Helpers.FileIconHelper.GetDependenciesIcon(),
                FullPath = projectPath,
                ItemType = FileItemType.Dependencies
            },
                        HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
        };

        Console.WriteLine("[AddDependenciesNode] Dependencies item created");

        // إضافة Frameworks
        var frameworksItem = new TreeViewItem
        {
            Header = new FileItem
            {
                Name = "Frameworks",
                Icon = Helpers.FileIconHelper.GetFrameworkIcon(),
                FullPath = projectPath,
                ItemType = FileItemType.Frameworks
            },
                        HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
        };

        // تحديد Framework بناءً على نوع المشروع
        string frameworkName = projectType switch
        {
            "WPF" => "Microsoft.WindowsDesktop.App.WPF",
            "Avalonia" => "Avalonia",
            "Maui" => "Microsoft.Maui",
            _ => ".NET"
        };

        var frameworkSubItem = new TreeViewItem
        {
            Header = new FileItem
            {
                Name = frameworkName,
                Icon = Helpers.FileIconHelper.GetFrameworkIcon(),
                FullPath = projectPath,
                ItemType = FileItemType.Frameworks
            },
                        HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
        };
        frameworksItem.Items.Add(frameworkSubItem);
        dependenciesItem.Items.Add(frameworksItem);

        // إضافة Analyzers
        AddAnalyzersNode(dependenciesItem, projectPath);

        // إضافة Assemblies (التجميعات)
        AddAssembliesNode(dependenciesItem, projectPath);

        // إضافة Packages (NuGet)
        AddPackagesNode(dependenciesItem, projectPath);

        // إضافة Projects (مشاريع مرجعية)
        AddProjectReferencesNode(dependenciesItem, projectPath);

        Console.WriteLine($"[AddDependenciesNode] Dependencies has {dependenciesItem.Items.Count} sub-items");
        projectItem.Items.Add(dependenciesItem);
        Console.WriteLine($"[AddDependenciesNode] Dependencies added to project. Project now has {projectItem.Items.Count} items");
    }

    /// <summary>
    /// إضافة عقدة Analyzers
    /// </summary>
    private void AddAnalyzersNode(TreeViewItem dependenciesItem, string projectPath)
    {
        try
        {
            var csprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.TopDirectoryOnly);
            if (csprojFiles.Length == 0) return;

            var doc = new XmlDocument();
            doc.Load(csprojFiles[0]);

            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            var analyzers = doc.SelectNodes("//Analyzer", nsmgr);

            if (analyzers != null && analyzers.Count > 0)
            {
                var analyzersItem = new TreeViewItem
                {
                    Header = new FileItem
                    {
                        Name = "Analyzers",
                        Icon = "/Assets/analyzer_icon.png",
                        FullPath = projectPath,
                        ItemType = FileItemType.Analyzers
                    },
                                HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
                };

                foreach (XmlNode analyzer in analyzers)
                {
                    var includeAttr = analyzer.Attributes?["Include"];
                    if (includeAttr != null)
                    {
                        var analyzerName = Path.GetFileNameWithoutExtension(includeAttr.Value);
                        var analyzerItem = new TreeViewItem
                        {
                            Header = new FileItem
                            {
                                Name = analyzerName,
                                Icon = "/Assets/analyzer_icon.png",
                                FullPath = projectPath,
                                ItemType = FileItemType.Analyzers
                            },
                                        HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
                        };
                        analyzersItem.Items.Add(analyzerItem);
                    }
                }

                if (analyzersItem.Items.Count > 0)
                {
                    dependenciesItem.Items.Add(analyzersItem);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في إضافة Analyzers: {ex.Message}");
        }
    }

    /// <summary>
    ///     إضافة عقدة Assemblies (التجميعات)
    /// </summary>
    private void AddAssembliesNode(TreeViewItem dependenciesItem, string projectPath)
    {
        try
        {
            var csprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.TopDirectoryOnly);
            if (csprojFiles.Length == 0) return;

            var doc = new XmlDocument();
            doc.Load(csprojFiles[0]);

            // البحث عن Reference nodes (مراجع التجميعات)
            var referenceNodes = doc.GetElementsByTagName("Reference");
            Console.WriteLine($"عدد التجميعات المكتشفة: {referenceNodes.Count}");

            if (referenceNodes.Count == 0) return;

            var assembliesItem = new TreeViewItem
            {
                Header = new FileItem
                {
                    Name = "Assemblies",
                    Icon = Helpers.FileIconHelper.GetAssemblyIcon(),
                    FullPath = projectPath,
                    ItemType = FileItemType.Dependencies
                },
                            HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
            };

            foreach (XmlNode node in referenceNodes)
            {
                var includeAttr = node.Attributes?["Include"];
                if (includeAttr != null)
                {
                    var assemblyName = includeAttr.Value;

                    // إزالة Version, Culture, PublicKeyToken من الاسم
                    if (assemblyName.Contains(","))
                    {
                        assemblyName = assemblyName.Split(',')[0].Trim();
                    }

                    Console.WriteLine($"إضافة تجميع: {assemblyName}");

                    var assemblyItem = new TreeViewItem
                    {
                        Header = new FileItem
                        {
                            Name = assemblyName,
                            Icon = Helpers.FileIconHelper.GetAssemblyIcon(),
                            FullPath = projectPath,
                            ItemType = FileItemType.Dependencies
                        },
                                    HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
                    };
                    assembliesItem.Items.Add(assemblyItem);
                }
            }

            if (assembliesItem.Items.Count > 0)
            {
                Console.WriteLine($"تم إضافة {assembliesItem.Items.Count} تجميع");
                dependenciesItem.Items.Add(assembliesItem);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في تحميل Assemblies: {ex.Message}");
        }
    }

    /// <summary>
    ///     إضافة عقدة Packages (NuGet)
    /// </summary>
    private void AddPackagesNode(TreeViewItem dependenciesItem, string projectPath)
    {
        try
        {
            var csprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.TopDirectoryOnly);
            if (csprojFiles.Length == 0)
            {
                Console.WriteLine("لم يتم العثور على ملف .csproj");
                return;
            }

            Console.WriteLine($"تحميل ملف المشروع: {csprojFiles[0]}");

            // استخدام XDocument للتعامل مع SDK-style projects
            var xdoc = System.Xml.Linq.XDocument.Load(csprojFiles[0]);

            // البحث عن جميع عناصر PackageReference (بدون namespace)
            var packageElements = xdoc.Descendants()
                .Where(e => e.Name.LocalName == "PackageReference")
                .ToList();

            Console.WriteLine($"عدد حزم NuGet المكتشفة: {packageElements.Count}");

            if (packageElements.Count == 0) return;

            var packagesItem = new TreeViewItem
            {
                Header = new FileItem
                {
                    Name = "Packages",
                    Icon = Helpers.FileIconHelper.GetPackageIcon(),
                    FullPath = projectPath,
                    ItemType = FileItemType.Packages
                },
                            HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
            };

            foreach (var element in packageElements)
            {
                var includeAttr = element.Attribute("Include");
                var versionAttr = element.Attribute("Version");

                if (includeAttr != null)
                {
                    var packageName = includeAttr.Value;
                    var version = versionAttr?.Value ?? "";

                    // إذا لم يكن هناك Version في Attribute، ابحث في العناصر الفرعية
                    if (string.IsNullOrEmpty(version))
                    {
                        var versionElement = element.Elements()
                            .FirstOrDefault(e => e.Name.LocalName == "Version");
                        if (versionElement != null)
                        {
                            version = versionElement.Value;
                        }
                    }

                    var displayName = string.IsNullOrEmpty(version) ? packageName : $"{packageName} ({version})";
                    Console.WriteLine($"إضافة حزمة: {displayName}");

                    var packageItem = new TreeViewItem
                    {
                        Header = new FileItem
                        {
                            Name = displayName,
                            Icon = Helpers.FileIconHelper.GetPackageIcon(),
                            FullPath = projectPath,
                            ItemType = FileItemType.Packages
                        },
                                    HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
                    };
                    packagesItem.Items.Add(packageItem);
                }
            }

            if (packagesItem.Items.Count > 0)
            {
                Console.WriteLine($"تم إضافة {packagesItem.Items.Count} حزمة NuGet");
                dependenciesItem.Items.Add(packagesItem);
            }
            else
            {
                Console.WriteLine("لم يتم إضافة أي حزم NuGet");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في تحميل Packages: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    ///     إضافة عقدة Projects (مشاريع مرجعية)
    /// </summary>
    private void AddProjectReferencesNode(TreeViewItem dependenciesItem, string projectPath)
    {
        try
        {
            var csprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.TopDirectoryOnly);
            if (csprojFiles.Length == 0) return;

            Console.WriteLine($"تحميل المشاريع المرجعية من: {csprojFiles[0]}");

            // استخدام XDocument للتعامل مع SDK-style projects
            var xdoc = System.Xml.Linq.XDocument.Load(csprojFiles[0]);

            // البحث عن جميع عناصر ProjectReference (بدون namespace)
            var projectElements = xdoc.Descendants()
                .Where(e => e.Name.LocalName == "ProjectReference")
                .ToList();

            Console.WriteLine($"عدد المشاريع المرجعية المكتشفة: {projectElements.Count}");

            if (projectElements.Count == 0) return;

            var projectsItem = new TreeViewItem
            {
                Header = new FileItem
                {
                    Name = "Projects",
                    Icon = "/Assets/Visual_Studio_Icon_2022.png",
                    FullPath = projectPath,
                    ItemType = FileItemType.Projects
                },
                            HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
            };

            foreach (var element in projectElements)
            {
                var includeAttr = element.Attribute("Include");
                if (includeAttr != null)
                {
                    var projectName = Path.GetFileNameWithoutExtension(includeAttr.Value);
                    var referencedProjectPath = Path.Combine(projectPath, includeAttr.Value);

                    // تحويل المسار النسبي إلى مسار مطلق
                    referencedProjectPath = Path.GetFullPath(referencedProjectPath);
                    var referencedProjectDir = Path.GetDirectoryName(referencedProjectPath);

                    Console.WriteLine($"إضافة مشروع مرجعي: {projectName}");
                    Console.WriteLine($"مسار المشروع المرجعي: {referencedProjectDir}");

                    var projectItem = new TreeViewItem
                    {
                        Header = new FileItem
                        {
                            Name = projectName,
                            Icon = "/Assets/Visual_Studio_Icon_2022.png",
                            FullPath = referencedProjectPath,
                            ItemType = FileItemType.Projects
                        },
                                    HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
                    };

                    // تحليل وإضافة Dependencies للمشروع المرجعي
                    if (Directory.Exists(referencedProjectDir))
                    {
                        Console.WriteLine($"تحليل Dependencies للمشروع: {projectName}");
                        AddDependenciesForReferencedProject(projectItem, referencedProjectDir);
                    }
                    else
                    {
                        Console.WriteLine($"تحذير: مسار المشروع المرجعي غير موجود: {referencedProjectDir}");
                    }

                    projectsItem.Items.Add(projectItem);
                }
            }

            if (projectsItem.Items.Count > 0)
            {
                Console.WriteLine($"تم إضافة {projectsItem.Items.Count} مشروع مرجعي");
                dependenciesItem.Items.Add(projectsItem);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في تحميل Project References: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    ///     إضافة Dependencies للمشروع المرجعي
    /// </summary>
    private void AddDependenciesForReferencedProject(TreeViewItem projectItem, string projectPath)
    {
        try
        {
            Console.WriteLine($"[AddDependenciesForReferencedProject] بدء تحليل: {projectPath}");

            // تحديد نوع المشروع
            var projectType = DetectProjectType(projectPath);
            Console.WriteLine($"[AddDependenciesForReferencedProject] نوع المشروع: {projectType}");

            var dependenciesItem = new TreeViewItem
            {
                Header = new FileItem
                {
                    Name = "Dependencies",
                    Icon = Helpers.FileIconHelper.GetDependenciesIcon(),
                    FullPath = projectPath,
                    ItemType = FileItemType.Dependencies
                },
                            HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
            };

            // إضافة Frameworks
            var frameworksItem = new TreeViewItem
            {
                Header = new FileItem
                {
                    Name = "Frameworks",
                    Icon = Helpers.FileIconHelper.GetFrameworkIcon(),
                    FullPath = projectPath,
                    ItemType = FileItemType.Frameworks
                },
                            HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
            };

            string frameworkName = projectType switch
            {
                "WPF" => "Microsoft.WindowsDesktop.App.WPF",
                "Avalonia" => "Avalonia",
                "Maui" => "Microsoft.Maui",
                _ => ".NET"
            };

            var frameworkSubItem = new TreeViewItem
            {
                Header = new FileItem
                {
                    Name = frameworkName,
                    Icon = Helpers.FileIconHelper.GetFrameworkIcon(),
                    FullPath = projectPath,
                    ItemType = FileItemType.Frameworks
                },
                            HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
            };
            frameworksItem.Items.Add(frameworkSubItem);
            dependenciesItem.Items.Add(frameworksItem);

            // إضافة Analyzers
            AddAnalyzersNode(dependenciesItem, projectPath);

            // إضافة Assemblies
            AddAssembliesNode(dependenciesItem, projectPath);

            // إضافة Packages
            AddPackagesNode(dependenciesItem, projectPath);

            // إضافة Projects المرجعية (بشكل متداخل)
            AddProjectReferencesNode(dependenciesItem, projectPath);

            if (dependenciesItem.Items.Count > 0)
            {
                Console.WriteLine($"[AddDependenciesForReferencedProject] تم إضافة {dependenciesItem.Items.Count} عنصر Dependencies");
                projectItem.Items.Add(dependenciesItem);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في إضافة Dependencies للمشروع المرجعي: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    ///     تحديد نوع المشروع من ملف .csproj
    /// </summary>
    private string DetectProjectType(string projectPath)
    {
        try
        {
            var csprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.TopDirectoryOnly);
            if (csprojFiles.Length == 0) return "Unknown";

            var xdoc = System.Xml.Linq.XDocument.Load(csprojFiles[0]);

            // البحث عن PackageReference لتحديد النوع
            var packages = xdoc.Descendants()
                .Where(e => e.Name.LocalName == "PackageReference")
                .Select(e => e.Attribute("Include")?.Value)
                .Where(v => v != null)
                .ToList();

            if (packages.Any(p => p.Contains("Avalonia")))
                return "Avalonia";
            if (packages.Any(p => p.Contains("Maui")))
                return "Maui";

            // البحث عن UseWPF
            var useWpf = xdoc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "UseWPF");
            if (useWpf != null && useWpf.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                return "WPF";

            // البحث عن TargetFramework
            var targetFramework = xdoc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "TargetFramework");
            if (targetFramework != null && targetFramework.Value.Contains("windows"))
                return "WPF";

            return "Unknown";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في تحديد نوع المشروع: {ex.Message}");
            return "Unknown";
        }
    }

    /// <summary>
    ///     إضافة مجلد إلى الشجرة
    /// </summary>
    private void AddFolderToTree(TreeViewItem parentItem, View.ProjectFolder folder, string projectType)
    {
        var folderItem = new TreeViewItem
        {
            Header = new FileItem
            {
                Name = folder.Name,
                Icon = Helpers.FileIconHelper.GetFolderIcon(),
                FullPath = folder.FullPath,
                ItemType = FileItemType.Folder
            },
                        HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
        };

        // إضافة الملفات في المجلد
        if (folder.Files != null)
        {
            foreach (var file in folder.Files.OrderBy(f => f.Name))
            {
                AddFileToTree(folderItem, file, projectType);
            }
        }

        // إضافة المجلدات الفرعية
        if (folder.SubFolders != null)
        {
            foreach (var subFolder in folder.SubFolders.OrderBy(f => f.Name))
            {
                AddFolderToTree(folderItem, subFolder, projectType);
            }
        }

        parentItem.Items.Add(folderItem);
    }

    /// <summary>
    ///     إضافة ملف إلى الشجرة
    /// </summary>
    private void AddFileToTree(TreeViewItem parentItem, View.ProjectFile file, string projectType)
    {
        // استخدام FileIconHelper للحصول على الأيقونة المناسبة
        string icon = Helpers.FileIconHelper.GetIconForFile(file.Name);

        // تحديد نوع العنصر بناءً على نوع الملف
        FileItemType itemType = file.Type switch
        {
            View.ProjectFileType.CSharp => FileItemType.CSharpFile,
            View.ProjectFileType.Config => FileItemType.ConfigFile,
            View.ProjectFileType.Json => FileItemType.JsonFile,
            View.ProjectFileType.Xaml => FileItemType.XamlFile,
            _ => FileItemType.OtherFile
        };

        var fileItem = new TreeViewItem
        {
            Header = new FileItem
            {
                Name = file.Name,
                Icon = icon,
                FullPath = file.FullPath,
                ItemType = itemType
            },
                        HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
        };

        // إذا كان ملف XAML/AXAML وله ملف code-behind، أضفه كعنصر فرعي
        if (file.Type == View.ProjectFileType.Xaml && file.CodeBehindFile != null)
        {
            var csItem = new TreeViewItem
            {
                Header = new FileItem
                {
                    Name = file.CodeBehindFile.Name,
                    Icon = Helpers.FileIconHelper.GetIconForFile(file.CodeBehindFile.Name),
                    FullPath = file.CodeBehindFile.FullPath,
                    ItemType = FileItemType.CSharpFile
                },
                            HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
            };
            fileItem.Items.Add(csItem);
        }

        parentItem.Items.Add(fileItem);
    }

    /// <summary>
    ///     تحميل جميع المراجع والمكتبات من المشروع
    /// </summary>
    private void LoadProjectReferences(string projectPath)
    {
        try
        {
            var referencesLoader = new View.ProjectReferencesLoader();
            referencesLoader.LoadAllReferences(projectPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في تحميل المراجع: {ex.Message}");
        }
    }

    /// <summary>
    ///     الطريقة القديمة - محفوظة للتوافق
    /// </summary>
    [Obsolete("استخدم LoadFilesToTreeView3 الجديدة")]
    public void LoadFilesToTreeView3_Old(string folderPath)
    {
        // Clear previous items from the TreeView
        FilesTreeView.Items.Clear();

        // 1. Determine project name and type
        var projectName = Path.GetFileName(folderPath);
        var projectType = "Unknown";

        try
        {
            // 2. Determine the project type based on XAML and AXAML files
            // Exclude App.xaml, App.xaml.cs, and App.axaml files from the type determination process
            var xamlFilesType = Directory.GetFiles(folderPath, "*.xaml", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase) &&
                            !Path.GetFileName(f).Equals("App.xaml", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var axamlFilesType = Directory.GetFiles(folderPath, "*.axaml", SearchOption.AllDirectories)
                .Where(f => !Path.GetFileName(f).Equals("App.axaml", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (axamlFilesType.Any())
                projectType = "Avalonia";
            else if (xamlFilesType.Any())
                // If no .axaml files are found, check the content of .xaml files
                foreach (var xamlPath in xamlFilesType)
                {
                    var xamlContent = File.ReadAllText(xamlPath);
                    if (xamlContent.Contains("xmlns=\"http://schemas.microsoft.com/dotnet/2021/maui"))
                    {
                        projectType = "Maui";
                        break; // Type determined, no need to continue the loop
                    }

                    if (xamlContent.Contains("xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation"))
                    {
                        projectType = "WPF";
                        break; // Type determined, no need to continue the loop
                    }
                }


            // Search for a .csproj file in the specified path
            var csprojFiles = Directory.GetFiles(folderPath, "*.csproj", SearchOption.TopDirectoryOnly);
            if (csprojFiles.Length > 0)
            {
                var csprojFile = csprojFiles[0];
                // If a .csproj file is found, use its name as the project name
                projectName = Path.GetFileNameWithoutExtension(csprojFiles[0]);

                if (projectType == "WPF")
                {
                    var doc = new XmlDocument();
                    doc.Load(csprojFile);

                    var projectReferenceNodes = doc.GetElementsByTagName("ProjectReference");
                    foreach (XmlNode node in projectReferenceNodes)
                    {
                        var includeAttr = node.Attributes?["Include"];
                        if (includeAttr != null)
                        {
                            var referencedCsprojPath =
                                Path.GetFullPath(Path.Combine(Path.GetDirectoryName(csprojFile), includeAttr.Value));
                            var referencedProjectFolder = Path.GetDirectoryName(referencedCsprojPath);
                            var referencedProjectName = Path.GetFileNameWithoutExtension(referencedCsprojPath);

                            var binDirectory = Path.Combine(referencedProjectFolder, "bin");
                            if (Directory.Exists(binDirectory))
                            {
                                var dllFiles = Directory.GetFiles(binDirectory, referencedProjectName + ".dll",
                                    SearchOption.AllDirectories);
                                foreach (var dllPath in dllFiles)
                                {
                                    var fileName = Path.GetFileNameWithoutExtension(dllPath);
                                    var alreadyExists = Toolbox.Instance.AssemblyNodes.Any(node =>
                                        string.Equals(Path.GetFileName(node.Name), fileName,
                                            StringComparison.OrdinalIgnoreCase)
                                    );

                                    if (!alreadyExists)
                                        Toolbox.Instance.AddAssembly(dllPath);
                                    else
                                        Console.WriteLine($"تم تجاهل {dllPath} لأنه موجود بالفعل.");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"مجلد bin غير موجود في {referencedProjectFolder}");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Handle potential errors during file reading
            Console.WriteLine($"Error finding project type: {ex.Message}");
        }

        // Save project information to application settings
        Settings.Default.ProjectName = Path.GetFileNameWithoutExtension(projectName);
        Settings.Default.ProjectPath = folderPath;
        Settings.Default.ProjectType = projectType;
        Settings.Default.Save();

        // 3. Create the main project item with its type
        var projectItem = new TreeViewItem
        {
            Header = new FileItem
            {
                Name = $"{projectName} ({projectType})",
                Icon = "/Assets/Open.png", // Icon for the project
                FullPath = folderPath // Full path to the project
            },
                        HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
        };

        // Add the project item to the TreeView
        FilesTreeView.Items.Add(projectItem);

        // 4. Recursively search for all required files in all subfolders
        // Exclude App.xaml, App.axaml, AppShell.xaml, Colors.xaml, and Styles.xaml
        var xamlFiles = Directory.GetFiles(folderPath, "*.xaml", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase) &&
                        !Path.GetFileName(f).Equals("App.xaml", StringComparison.OrdinalIgnoreCase) &&
                        !Path.GetFileName(f).Equals("AppShell.xaml", StringComparison.OrdinalIgnoreCase) &&
                        !Path.GetFileName(f).Equals("Colors.xaml", StringComparison.OrdinalIgnoreCase) &&
                        !Path.GetFileName(f).Equals("Styles.xaml", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var axamlFiles = Directory.GetFiles(folderPath, "*.axaml", SearchOption.AllDirectories)
            .Where(f => !Path.GetFileName(f)
                .Equals("App.axaml", StringComparison.OrdinalIgnoreCase)) // Exclude App.axaml
            .ToArray();
        var CsFiles = "";
        if (Settings.Default.ProjectType == "WPF")
            CsFiles = "*.xaml.cs";
        else if (Settings.Default.ProjectType == "Avalonia")
            CsFiles = "*.axaml.cs";
        else if (Settings.Default.ProjectType == "Maui") CsFiles = "*.xaml.cs";
        // Exclude .xaml.cs files for App, AppShell, Colors, and Styles
        xamlCsFiles = Directory.GetFiles(folderPath, CsFiles, SearchOption.AllDirectories)
          .Where(f => !Path.GetFileName(f).Equals("App.xaml.cs", StringComparison.OrdinalIgnoreCase) &&
                      !Path.GetFileName(f).Equals("AppShell.xaml.cs", StringComparison.OrdinalIgnoreCase) &&
                      !Path.GetFileName(f).Equals("Colors.xaml.cs", StringComparison.OrdinalIgnoreCase) &&
                      !Path.GetFileName(f).Equals("Styles.xaml.cs", StringComparison.OrdinalIgnoreCase))
          .ToArray();

        // 5. Add XAML and CS files as sub-items of the project item
        foreach (var xaml in xamlFiles)
        {
            var xamlFileName = Path.GetFileNameWithoutExtension(xaml).Trim();

            // Search for the corresponding .xaml.cs file
            var csFile = xamlCsFiles.FirstOrDefault(cs =>
                string.Equals(Path.GetFileNameWithoutExtension(cs).Replace(".xaml", string.Empty), xamlFileName,
                    StringComparison.OrdinalIgnoreCase) &&
                Path.GetDirectoryName(cs) == Path.GetDirectoryName(xaml));

            // Log the paths for debugging
            Console.WriteLine($"XAML File: {xaml}");
            if (csFile != null) Console.WriteLine($"CS File: {csFile}");

            // Main item (XAML file)
            var xamlItem = new TreeViewItem
            {
                Header = new FileItem
                {
                    Name = Path.GetFileName(xaml),
                    Icon = "/Assets/xaml_icon.png",
                    FullPath = xaml // Set the full path
                },
                            HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
            };

            // Sub-item (.xaml.cs file)
            if (csFile != null)
            {
                var csItem = new TreeViewItem
                {
                    Header = new FileItem
                    {
                        Name = Path.GetFileName(csFile),
                        Icon = "/Assets/CS_16x.png",
                        FullPath = csFile // Set the full path
                    },
                                HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
                };

                xamlItem.Items.Add(csItem);
            }

            // Add the item to the main project item
            projectItem.Items.Add(xamlItem);
        }

        // 6. Add Avalonia XAML files as sub-items of the project item
        foreach (var axaml in axamlFiles)
        {
            var axamlFileName = Path.GetFileNameWithoutExtension(axaml).Trim();
            // Search for the corresponding .xaml.cs file
            var csFile = xamlCsFiles.FirstOrDefault(cs =>
                string.Equals(Path.GetFileNameWithoutExtension(cs).Replace(".axaml", string.Empty), axamlFileName,
                    StringComparison.OrdinalIgnoreCase) &&
                Path.GetDirectoryName(cs) == Path.GetDirectoryName(axaml));

            // Main item (AXAML file)
            var axamlItem = new TreeViewItem
            {
                Header = new FileItem
                {
                    Name = Path.GetFileName(axaml),
                    Icon = "/Assets/xaml_icon.png", // Ensure the icon exists or replace it as needed
                    FullPath = axaml
                },
                            HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
            };
            // Sub-item (.xaml.cs file)
            if (csFile != null)
            {
                var csItem = new TreeViewItem
                {
                    Header = new FileItem
                    {
                        Name = Path.GetFileName(csFile),
                        Icon = "/Assets/CS_16x.png",
                        FullPath = csFile // Set the full path
                    },
                                HeaderTemplate = (Avalonia.Controls.Templates.IDataTemplate)this.FindResource("FileTemplate"),
                };

                axamlItem.Items.Add(csItem);
            }

            // Add the item to the main project item
            projectItem.Items.Add(axamlItem);
        }

        // Expand the project item automatically after loading
        projectItem.IsExpanded = true;
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
                        currentFileName = filePath;
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

   

    #endregion






}