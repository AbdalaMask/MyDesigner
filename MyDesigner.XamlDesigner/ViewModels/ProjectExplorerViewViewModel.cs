using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyDesigner.XamlDesigner.Configuration;
using MyDesigner.XamlDesigner.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace MyDesigner.XamlDesigner.ViewModels
{
    public partial class ProjectExplorerViewViewModel : ViewModelBase
    {

        // Test Commands
        public RelayCommand RunCommand { get; private set; }
        public RelayCommand NewFileCommand { get; private set; }

        public RelayCommand BuildCommand { get; private set; }

        public RelayCommand RebuildCommand { get; private set; }

        public RelayCommand StopCommand { get; private set; }


        public ProjectExplorerViewViewModel()
        {
            //   LoadAllProjectsFromSolution("D:\\repos\\Maui\\WpfToPlatforms.sln", "D:\\repos\\Maui");
        }

        // ������ ������� ��� TreeView
        public ObservableCollection<FileItemViewModel> SolutionItems { get; } = new();

        public async Task OpenFolderAsync(IStorageProvider storageProvider)
        {
            var options = new FilePickerOpenOptions
            {
                Title = "���� ��� �������",
                FileTypeFilter = new[] { new FilePickerFileType("C# Project") { Patterns = new[] { "*.csproj" } } }
            };

            var result = await storageProvider.OpenFilePickerAsync(options);
            if (result.Count > 0)
            {
                var csprojPath = result[0].Path.LocalPath;
                var projectFolder = Path.GetDirectoryName(csprojPath);

                SolutionItems.Clear(); // ���� ����� ��� UI ��������
                LoadProject(projectFolder);
            }
        }

        private void LoadProject(string folderPath)
        {
            // ���� ������ �����
            var projectNode = new FileItemViewModel
            {
                Name = "MyProject (Core)",
                Icon = "avares://MyDesigner.XamlDesigner/Assets/Visual_Studio_Icon_2022.png",
                ItemType = FileItemType.Solution,
                IsFontBold = true,
                IsExpanded = true
            };

            // ����� ����� �����
            projectNode.Children.Add(new FileItemViewModel { Name = "MainWindow.axaml", ItemType = FileItemType.XamlFile });

            SolutionItems.Add(projectNode);
        }



        #region New






        private string currentFileName;
        private string[] xamlCsFiles;

        /// <summary>
        /// تحميل جميع المشاريع من ملف .sln
        /// </summary>
        public void LoadAllProjectsFromSolution(string slnPath, string folderPath)
        {
            try
            {
                // ... داخل الدالة الخاصة بك
                Dispatcher.UIThread.Post(() =>
                {

                    // مسح العناصر السابقة
                    SolutionItems.Clear();
                    // تنبيه الواجهة بأن الخاصية تغيرت (إضافي للضمان)
                    OnPropertyChanged(nameof(SolutionItems));
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
                    var solutionItem = new FileItemViewModel
                    {

                        Name = $"Solution '{solutionName}' ({projectPaths.Count} of {projectPaths.Count} projects)",
                        Icon = "avares://MyDesigner.XamlDesigner/Assets/Visual_Studio_Icon_2022.png",
                        FullPath = slnPath,
                        ItemType = FileItemType.Solution,
                        IsFontBold = true,
                        IsExpanded = true

                    };

                    // تنظيم المشاريع حسب المجلدات
                    var folderDict = new Dictionary<string, FileItemViewModel>(StringComparer.OrdinalIgnoreCase);
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
                            parentFolder.Children.Add(projectItem);
                        }
                    }


                    SolutionItems.Add(solutionItem);


                    // تنبيه الواجهة بأن الخاصية تغيرت (إضافي للضمان)
                    OnPropertyChanged(nameof(SolutionItems));
                });

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
        private void EnsureSolutionFolderExists(Dictionary<string, FileItemViewModel> folderDict, string path, string basePath, FileItemViewModel solutionItem)
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
                    var folderItem = new FileItemViewModel
                    {

                        Name = parts[i],
                        Icon = "avares://MyDesigner.XamlDesigner/Assets/folder_icon.png", // أيقونة المجلد
                        FullPath = Path.Combine(basePath, currentPath.Replace("/", "\\")),
                        ItemType = FileItemType.Folder

                    };

                    folderDict[currentPath] = folderItem;

                    // إضافة المجلد للمجلد الأب
                    var parentFolder = string.IsNullOrEmpty(parentPath) ? solutionItem : folderDict[parentPath];
                    parentFolder.Children.Add(folderItem);
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
            SolutionItems.Add(projectItem);
            projectItem.IsExpanded = true;
        }

        /// <summary>
        /// تحميل مشروع محدد إلى الشجرة
        /// </summary>
        public void LoadProjectToTree(string csprojPath)
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
                SolutionItems.Add(projectItem);
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
        private FileItemViewModel CreateProjectTreeItem(View.ProjectLoader loader, string folderPath)
        {
            // التحقق من أن هذا هو المشروع الرئيسي
            bool isStartupProject = Settings.Default.ProjectPath == folderPath;

            // إضافة علامة للمشروع الرئيسي
            string projectDisplayName = isStartupProject
                ? $"▶ {loader.ProjectName} ({loader.ProjectType})"
                : $"{loader.ProjectName} ({loader.ProjectType})";

            var projectItem = new FileItemViewModel
            {

                Name = projectDisplayName,
                Icon = "avares://MyDesigner.XamlDesigner/Assets/Visual_Studio_Icon_2022.png",
                FullPath = folderPath,
                ItemType = FileItemType.Project


            };

            // تغيير لون الخط للمشروع الرئيسي
            if (isStartupProject)
            {
                // projectItem.FontWeight = Avalonia.Media.FontWeight.Bold;
            }

            // إضافة Dependencies
            Console.WriteLine($"[CreateProjectTreeItem] Adding Dependencies for {loader.ProjectName}");
            AddDependenciesNode(projectItem, folderPath, loader.ProjectType);
            Console.WriteLine($"[CreateProjectTreeItem] Dependencies added. Project has {projectItem.Children.Count} items");

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
        ///     تحميل المشروع باستخدام ProjectLoader الجديد مع عرض Dependencies
        /// </summary>
        public void LoadFilesToSolution(string folderPath)
        {
            try
            {


                // مسح العناصر السابقة
                SolutionItems.Clear();

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
        private void AddDependenciesNode(FileItemViewModel projectItem, string projectPath, string projectType)
        {
            Console.WriteLine($"[AddDependenciesNode] Starting for project: {projectPath}, type: {projectType}");

            var dependenciesItem = new FileItemViewModel
            {

                Name = "Dependencies",
                Icon = Helpers.FileIconHelper.GetDependenciesIcon(),
                FullPath = projectPath,
                ItemType = FileItemType.Dependencies


            };

            Console.WriteLine("[AddDependenciesNode] Dependencies item created");

            // إضافة Frameworks
            var frameworksItem = new FileItemViewModel
            {

                Name = "Frameworks",
                Icon = Helpers.FileIconHelper.GetFrameworkIcon(),
                FullPath = projectPath,
                ItemType = FileItemType.Frameworks

            };

            // تحديد Framework بناءً على نوع المشروع
            string frameworkName = projectType switch
            {
                "WPF" => "Microsoft.WindowsDesktop.App.WPF",
                "Avalonia" => "Avalonia",
                "Maui" => "Microsoft.Maui",
                _ => ".NET"
            };

            var frameworkSubItem = new FileItemViewModel
            {

                Name = frameworkName,
                Icon = Helpers.FileIconHelper.GetFrameworkIcon(),
                FullPath = projectPath,
                ItemType = FileItemType.Frameworks

            };
            frameworksItem.Children.Add(frameworkSubItem);
            dependenciesItem.Children.Add(frameworksItem);

            // إضافة Analyzers
            AddAnalyzersNode(dependenciesItem, projectPath);

            // إضافة Assemblies (التجميعات)
            AddAssembliesNode(dependenciesItem, projectPath);

            // إضافة Packages (NuGet)
            AddPackagesNode(dependenciesItem, projectPath);

            // إضافة Projects (مشاريع مرجعية)
            AddProjectReferencesNode(dependenciesItem, projectPath);

            Console.WriteLine($"[AddDependenciesNode] Dependencies has {dependenciesItem.Children.Count} sub-items");
            projectItem.Children.Add(dependenciesItem);
            Console.WriteLine($"[AddDependenciesNode] Dependencies added to project. Project now has {projectItem.Children.Count} items");
        }

        /// <summary>
        /// إضافة عقدة Analyzers
        /// </summary>
        private void AddAnalyzersNode(FileItemViewModel dependenciesItem, string projectPath)
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
                    var analyzersItem = new FileItemViewModel
                    {

                        Name = "Analyzers",
                        Icon = "avares://MyDesigner.XamlDesigner/Assets/analyzer_icon.png",
                        FullPath = projectPath,
                        ItemType = FileItemType.Analyzers

                    };

                    foreach (XmlNode analyzer in analyzers)
                    {
                        var includeAttr = analyzer.Attributes?["Include"];
                        if (includeAttr != null)
                        {
                            var analyzerName = Path.GetFileNameWithoutExtension(includeAttr.Value);
                            var analyzerItem = new FileItemViewModel
                            {

                                Name = analyzerName,
                                Icon = "avares://MyDesigner.XamlDesigner/Assets/analyzer_icon.png",
                                FullPath = projectPath,
                                ItemType = FileItemType.Analyzers

                            };
                            analyzersItem.Children.Add(analyzerItem);
                        }
                    }

                    if (analyzersItem.Children.Count > 0)
                    {
                        dependenciesItem.Children.Add(analyzersItem);
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
        private void AddAssembliesNode(FileItemViewModel dependenciesItem, string projectPath)
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

                var assembliesItem = new FileItemViewModel
                {

                    Name = "Assemblies",
                    Icon = Helpers.FileIconHelper.GetAssemblyIcon(),
                    FullPath = projectPath,
                    ItemType = FileItemType.Dependencies

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

                        var assemblyItem = new FileItemViewModel
                        {

                            Name = assemblyName,
                            Icon = Helpers.FileIconHelper.GetAssemblyIcon(),
                            FullPath = projectPath,
                            ItemType = FileItemType.Dependencies

                        };
                        assembliesItem.Children.Add(assemblyItem);
                    }
                }

                if (assembliesItem.Children.Count > 0)
                {
                    Console.WriteLine($"تم إضافة {assembliesItem.Children.Count} تجميع");
                    dependenciesItem.Children.Add(assembliesItem);
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
        private void AddPackagesNode(FileItemViewModel dependenciesItem, string projectPath)
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

                var packagesItem = new FileItemViewModel
                {

                    Name = "Packages",
                    Icon = Helpers.FileIconHelper.GetPackageIcon(),
                    FullPath = projectPath,
                    ItemType = FileItemType.Packages

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

                        var packageItem = new FileItemViewModel
                        {

                            Name = displayName,
                            Icon = Helpers.FileIconHelper.GetPackageIcon(),
                            FullPath = projectPath,
                            ItemType = FileItemType.Packages

                        };
                        packagesItem.Children.Add(packageItem);
                    }
                }

                if (packagesItem.Children.Count > 0)
                {
                    Console.WriteLine($"تم إضافة {packagesItem.Children.Count} حزمة NuGet");
                    dependenciesItem.Children.Add(packagesItem);
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
        private void AddProjectReferencesNode(FileItemViewModel dependenciesItem, string projectPath)
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

                var projectsItem = new FileItemViewModel
                {

                    Name = "Projects",
                    Icon = "avares://MyDesigner.XamlDesigner/Assets/Visual_Studio_Icon_2022.png",
                    FullPath = projectPath,
                    ItemType = FileItemType.Projects

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

                        var projectItem = new FileItemViewModel
                        {

                            Name = projectName,
                            Icon = "avares://MyDesigner.XamlDesigner/Assets/Visual_Studio_Icon_2022.png",
                            FullPath = referencedProjectPath,
                            ItemType = FileItemType.Projects

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

                        projectsItem.Children.Add(projectItem);
                    }
                }

                if (projectsItem.Children.Count > 0)
                {
                    Console.WriteLine($"تم إضافة {projectsItem.Children.Count} مشروع مرجعي");
                    dependenciesItem.Children.Add(projectsItem);
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
        private void AddDependenciesForReferencedProject(FileItemViewModel projectItem, string projectPath)
        {
            try
            {
                Console.WriteLine($"[AddDependenciesForReferencedProject] بدء تحليل: {projectPath}");

                // تحديد نوع المشروع
                var projectType = DetectProjectType(projectPath);
                Console.WriteLine($"[AddDependenciesForReferencedProject] نوع المشروع: {projectType}");

                var dependenciesItem = new FileItemViewModel
                {

                    Name = "Dependencies",
                    Icon = Helpers.FileIconHelper.GetDependenciesIcon(),
                    FullPath = projectPath,
                    ItemType = FileItemType.Dependencies

                };

                // إضافة Frameworks
                var frameworksItem = new FileItemViewModel
                {

                    Name = "Frameworks",
                    Icon = Helpers.FileIconHelper.GetFrameworkIcon(),
                    FullPath = projectPath,
                    ItemType = FileItemType.Frameworks

                };

                string frameworkName = projectType switch
                {
                    "WPF" => "Microsoft.WindowsDesktop.App.WPF",
                    "Avalonia" => "Avalonia",
                    "Maui" => "Microsoft.Maui",
                    _ => ".NET"
                };

                var frameworkSubItem = new FileItemViewModel
                {

                    Name = frameworkName,
                    Icon = Helpers.FileIconHelper.GetFrameworkIcon(),
                    FullPath = projectPath,
                    ItemType = FileItemType.Frameworks

                };
                frameworksItem.Children.Add(frameworkSubItem);
                dependenciesItem.Children.Add(frameworksItem);

                // إضافة Analyzers
                AddAnalyzersNode(dependenciesItem, projectPath);

                // إضافة Assemblies
                AddAssembliesNode(dependenciesItem, projectPath);

                // إضافة Packages
                AddPackagesNode(dependenciesItem, projectPath);

                // إضافة Projects المرجعية (بشكل متداخل)
                AddProjectReferencesNode(dependenciesItem, projectPath);

                if (dependenciesItem.Children.Count > 0)
                {
                    Console.WriteLine($"[AddDependenciesForReferencedProject] تم إضافة {dependenciesItem.Children.Count} عنصر Dependencies");
                    projectItem.Children.Add(dependenciesItem);
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
        private void AddFolderToTree(FileItemViewModel parentItem, View.ProjectFolder folder, string projectType)
        {
            var folderItem = new FileItemViewModel
            {

                Name = folder.Name,
                Icon = Helpers.FileIconHelper.GetFolderIcon(),
                FullPath = folder.FullPath,
                ItemType = FileItemType.Folder

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

            parentItem.Children.Add(folderItem);
        }

        /// <summary>
        ///     إضافة ملف إلى الشجرة
        /// </summary>
        private void AddFileToTree(FileItemViewModel parentItem, View.ProjectFile file, string projectType)
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

            var fileItem = new FileItemViewModel
            {

                Name = file.Name,
                Icon = icon,
                FullPath = file.FullPath,
                ItemType = itemType

            };

            // إذا كان ملف XAML/AXAML وله ملف code-behind، أضفه كعنصر فرعي
            if (file.Type == View.ProjectFileType.Xaml && file.CodeBehindFile != null)
            {
                var csItem = new FileItemViewModel
                {

                    Name = file.CodeBehindFile.Name,
                    Icon = Helpers.FileIconHelper.GetIconForFile(file.CodeBehindFile.Name),
                    FullPath = file.CodeBehindFile.FullPath,
                    ItemType = FileItemType.CSharpFile

                };
                fileItem.Children.Add(csItem);
            }

            parentItem.Children.Add(fileItem);
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





        #region Project Management Functions - دوال إدارة المشروع



        /// <summary>
        /// إنشاء ملف جديد
        /// </summary>
        public void NewFile()
        {
            try
            {
                // سيتم تنفيذها لاحقاً - إنشاء ملف جديد
                Console.WriteLine("إنشاء ملف جديد");
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// إنشاء مجلد جديد
        /// </summary>
        public void NewFolder()
        {
            try
            {
                // سيتم تنفيذها لاحقاً - إنشاء مجلد جديد
                Console.WriteLine("إنشاء مجلد جديد");
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// حذف العنصر المحدد
        /// </summary>
        public void DeleteSelectedItem()
        {
            try
            {
                // سيتم تنفيذها لاحقاً - حذف العنصر المحدد
                Console.WriteLine("حذف العنصر المحدد");
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// إعادة تسمية العنصر المحدد
        /// </summary>
        public void RenameSelectedItem()
        {
            try
            {
                // سيتم تنفيذها لاحقاً - إعادة تسمية العنصر المحدد
                Console.WriteLine("إعادة تسمية العنصر المحدد");
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// تحديث عرض المشروع
        /// </summary>
        public void RefreshView()
        {
            try
            {
                // تحديث الشجرة
                SolutionItems.Clear();
                Console.WriteLine("تم تحديث عرض المشروع");
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// بناء المشروع
        /// </summary>
        public void BuildProject()
        {
            try
            {
                // سيتم تنفيذها لاحقاً - بناء المشروع
                Console.WriteLine("بناء المشروع");
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// تشغيل المشروع
        /// </summary>
        public void RunProject()
        {
            try
            {
                // سيتم تنفيذها لاحقاً - تشغيل المشروع
                Console.WriteLine("تشغيل المشروع");
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        #endregion

        #endregion
        public string filePath = string.Empty;




    }




}