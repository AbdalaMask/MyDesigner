using MyDesigner.XamlDesigner.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace MyDesigner.XamlDesigner.View;

/// <summary>
/// فئة لتحليل وتحميل بنية المشروع من ملف .csproj
/// </summary>
public class ProjectLoader
{
    public string ProjectPath { get; private set; }
    public string ProjectName { get; private set; }
    public string ProjectType { get; private set; }
    public ProjectStructure Structure { get; private set; }

    /// <summary>
    /// تحميل المشروع من المسار المحدد
    /// </summary>
    public bool LoadProject(string projectPath)
    {
        try
        {
            ProjectPath = projectPath;

            // البحث عن ملف .csproj أو .sln
            var csprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.TopDirectoryOnly);
            var slnFiles = Directory.GetFiles(projectPath, "*.sln", SearchOption.TopDirectoryOnly);
            
            // إذا لم يتم العثور على أي ملف مشروع
            if (csprojFiles.Length == 0 && slnFiles.Length == 0)
                return false;

            // تفضيل ملف .csproj إذا كان موجوداً
            string projectFile;
            string actualProjectPath;
            
            if (csprojFiles.Length > 0)
            {
                projectFile = csprojFiles[0];
                actualProjectPath = Path.GetDirectoryName(projectFile);
            }
            else
            {
                // إذا كان هناك فقط .sln، نبحث عن .csproj في المجلدات الفرعية
                var allCsprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories);
                if (allCsprojFiles.Length > 0)
                {
                    projectFile = allCsprojFiles[0];
                    actualProjectPath = Path.GetDirectoryName(projectFile);
                }
                else
                {
                    // إذا لم نجد .csproj، نستخدم اسم .sln
                    ProjectName = Path.GetFileNameWithoutExtension(slnFiles[0]);
                    projectFile = slnFiles[0];
                    
                    // في حالة .sln فقط، نحاول تحميل المشروع بشكل أساسي
                    return LoadFromSolutionFile(slnFiles[0], projectPath);
                }
            }

            ProjectName = Path.GetFileNameWithoutExtension(projectFile);
            ProjectPath = actualProjectPath;

            // تحليل ملف المشروع
            var doc = XDocument.Load(projectFile);

            // تحديد نوع المشروع
            ProjectType = DetermineProjectType(doc, actualProjectPath);

            // بناء بنية المشروع
            Structure = BuildProjectStructure(doc, actualProjectPath);

      
            Settings.Default.ProjectType = ProjectType;
            Settings.Default.Save();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في تحميل المشروع: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// تحديد نوع المشروع من محتوى .csproj
    /// </summary>
    private string DetermineProjectType(XDocument doc, string projectPath)
    {
        var root = doc.Root;
        if (root == null) return "Unknown";

        // البحث عن TargetFramework
        var targetFramework = root.Descendants("TargetFramework").FirstOrDefault()?.Value ??
                            root.Descendants("TargetFrameworks").FirstOrDefault()?.Value ?? "";

        // فحص PackageReference
        var packageRefs = root.Descendants("PackageReference")
            .Select(x => x.Attribute("Include")?.Value ?? "")
            .ToList();

        if (packageRefs.Any(p => p.Contains("Avalonia")))
            return "Avalonia";

        if (packageRefs.Any(p => p.Contains("Microsoft.Maui")))
            return "Maui";
        // فحص نوع المشروع من TargetFramework
        if (targetFramework.Contains("net") && targetFramework.Contains("android"))
            return "Maui";

        if (targetFramework.Contains("net") && targetFramework.Contains("ios"))
            return "Maui";

        // فحص SDK
        var sdk = root.Attribute("Sdk")?.Value ?? "";
        if (sdk.Contains("Microsoft.NET.Sdk.Maui"))
            return "Maui";

      

        // فحص ملفات XAML في المشروع
        var xamlFiles = Directory.GetFiles(projectPath, "*.xaml", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var axamlFiles = Directory.GetFiles(projectPath, "*.axaml", SearchOption.AllDirectories).ToArray();

        if (axamlFiles.Any())
            return "Avalonia";

        // فحص محتوى ملفات XAML
        foreach (var xamlFile in xamlFiles.Take(3))
        {
            try
            {
                var content = File.ReadAllText(xamlFile);
                if (content.Contains("xmlns=\"http://schemas.microsoft.com/dotnet/2021/maui\""))
                    return "Maui";
            }
            catch { }
        }

        return "WPF";
    }

    /// <summary>
    /// بناء بنية المشروع من ملف .csproj
    /// </summary>
    private ProjectStructure BuildProjectStructure(XDocument doc, string projectPath)
    {
        var structure = new ProjectStructure
        {
            Name = ProjectName,
            Type = ProjectType,
            RootPath = projectPath,
            Folders = new List<ProjectFolder>()
        };

        // الحصول على جميع الملفات المضمنة في المشروع
        var root = doc.Root;
        if (root == null) return structure;

        // التحقق من نوع المشروع (SDK-style أم القديم)
        var isSdkStyle = root.Attribute("Sdk") != null;

        Console.WriteLine($"نوع المشروع: {(isSdkStyle ? "SDK-style" : "Legacy")}");

        // جمع الملفات من ItemGroup
        var itemGroups = root.Descendants("ItemGroup");
        var includedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var excludedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var itemGroup in itemGroups)
        {
            // جمع الملفات المستبعدة (Remove)
            var removedItems = itemGroup.Elements()
                .Where(e => e.Name.LocalName == "Compile" ||
                           e.Name.LocalName == "Page" ||
                           e.Name.LocalName == "None")
                .Where(e => e.Attribute("Remove") != null)
                .Select(e => e.Attribute("Remove")?.Value)
                .Where(v => !string.IsNullOrEmpty(v));

            foreach (var item in removedItems)
            {
                // دعم wildcards مثل "Visual Studio\**\*.cs"
                if (item.Contains("**"))
                {
                    var pattern = item.Replace("**\\", "").Replace("**", "");
                    excludedFiles.Add(pattern);
                }
                else
                {
                    excludedFiles.Add(item.Replace("\\", "/"));
                }
            }

            // جمع ملفات Page, Compile, None, Content المضمنة صراحةً
            var items = itemGroup.Elements()
                .Where(e => e.Name.LocalName == "Page" ||
                           e.Name.LocalName == "Compile" ||
                           e.Name.LocalName == "None" ||
                           e.Name.LocalName == "Content" ||
                           e.Name.LocalName == "ApplicationDefinition")
                .Where(e => e.Attribute("Include") != null)
                .Select(e => e.Attribute("Include")?.Value)
                .Where(v => !string.IsNullOrEmpty(v));

            foreach (var item in items)
            {
                // دعم wildcards مثل "View\**\*.cs"
                if (item.Contains("**"))
                {
                    var expandedFiles = ExpandWildcard(item, projectPath);
                    foreach (var file in expandedFiles)
                    {
                        includedFiles.Add(file);
                    }
                }
                else
                {
                    includedFiles.Add(item.Replace("\\", "/"));
                }
            }
        }

        // في مشاريع SDK-style، يتم تضمين جميع الملفات تلقائياً
        if (isSdkStyle)
        {
            Console.WriteLine("مشروع SDK-style: سيتم تضمين جميع الملفات تلقائياً...");
            var allFiles = GetAllProjectFiles(projectPath);

            // إضافة جميع الملفات ما عدا المستبعدة
            foreach (var file in allFiles)
            {
                var shouldExclude = false;

                // التحقق من الاستبعاد
                foreach (var excludePattern in excludedFiles)
                {
                    if (file.Contains(excludePattern.Replace("\\", "/")))
                    {
                        shouldExclude = true;
                        break;
                    }
                }

                if (!shouldExclude)
                {
                    includedFiles.Add(file);
                }
            }
        }
        else if (includedFiles.Count == 0)
        {
            // مشاريع قديمة بدون ملفات محددة
            Console.WriteLine("لم يتم العثور على ملفات في .csproj، سيتم البحث في المجلدات...");
            includedFiles = GetAllProjectFiles(projectPath);
        }

        Console.WriteLine($"عدد الملفات المضمنة: {includedFiles.Count}");
        Console.WriteLine($"عدد الملفات المستبعدة: {excludedFiles.Count}");

        // بناء الهيكل الشجري
        BuildFolderStructure(structure, includedFiles, projectPath);

        Console.WriteLine($"تم بناء الهيكل: {structure.RootFiles?.Count ?? 0} ملفات جذرية، {structure.Folders?.Count ?? 0} مجلدات");

        return structure;
    }

    /// <summary>
    /// توسيع wildcards مثل "View\**\*.cs"
    /// </summary>
    private List<string> ExpandWildcard(string pattern, string projectPath)
    {
        var files = new List<string>();

        try
        {
            // استخراج المجلد والامتداد من النمط
            var parts = pattern.Split(new[] { "**" }, StringSplitOptions.None);
            var baseFolder = parts[0].Replace("\\", "").Replace("/", "");
            var filePattern = parts.Length > 1 ? parts[1].Replace("\\", "") : "*.*";

            var searchPath = string.IsNullOrEmpty(baseFolder)
                ? projectPath
                : Path.Combine(projectPath, baseFolder);

            if (Directory.Exists(searchPath))
            {
                var foundFiles = Directory.GetFiles(searchPath, filePattern, SearchOption.AllDirectories)
                    .Select(f => f.Substring(projectPath.Length + 1).Replace("\\", "/"));

                files.AddRange(foundFiles);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في توسيع wildcard {pattern}: {ex.Message}");
        }

        return files;
    }

    /// <summary>
    /// الحصول على جميع ملفات المشروع
    /// </summary>
    private HashSet<string> GetAllProjectFiles(string projectPath)
    {
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var extensions = new[] { ".xaml", ".axaml", ".cs", ".config", ".json", ".xml", ".resx", ".settings" };

        // استبعاد المجلدات
        var excludeFolders = new[] { "bin", "obj", ".vs", "packages", "node_modules", ".git" };

        // استبعاد ملفات generated فقط
        var excludeFiles = new[] { "App.g.i.cs", "App.g.cs" };

        Console.WriteLine($"البحث عن الملفات في: {projectPath}");

        try
        {
            // البحث عن جميع الملفات
            var allFiles = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories)
                .Where(f =>
                {
                    // استبعاد المجلدات المحددة
                    var relativePath = f.Substring(projectPath.Length + 1);
                    if (excludeFolders.Any(folder => relativePath.StartsWith(folder + "\\") || relativePath.Contains("\\" + folder + "\\")))
                        return false;

                    // استبعاد الملفات المحددة
                    var fileName = Path.GetFileName(f);
                    if (excludeFiles.Any(ef => fileName.Equals(ef, StringComparison.OrdinalIgnoreCase)))
                        return false;

                    // استبعاد ملفات generated
                    if (fileName.EndsWith(".g.cs") || fileName.EndsWith(".g.i.cs"))
                        return false;

                    // قبول الامتدادات المحددة فقط
                    var ext = Path.GetExtension(f).ToLower();
                    return extensions.Contains(ext);
                })
                .Select(f => f.Substring(projectPath.Length + 1).Replace("\\", "/"));

            foreach (var file in allFiles)
            {
                files.Add(file);
            }

            Console.WriteLine($"إجمالي الملفات المكتشفة: {files.Count}");

            // عرض بعض الملفات للتشخيص
            var sampleFiles = files.Take(10).ToList();
            if (sampleFiles.Any())
            {
                Console.WriteLine("أمثلة على الملفات المكتشفة:");
                foreach (var file in sampleFiles)
                {
                    Console.WriteLine($"  - {file}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في البحث عن الملفات: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }

        return files;
    }

    /// <summary>
    /// بناء بنية المجلدات
    /// </summary>
    private void BuildFolderStructure(ProjectStructure structure, HashSet<string> files, string projectPath)
    {
        var folderDict = new Dictionary<string, ProjectFolder>(StringComparer.OrdinalIgnoreCase);

        // إنشاء المجلد الجذر
        var rootFolder = new ProjectFolder
        {
            Name = "",
            FullPath = projectPath,
            Files = new List<ProjectFile>(),
            SubFolders = new List<ProjectFolder>()
        };
        folderDict[""] = rootFolder;

        Console.WriteLine($"بناء بنية المجلدات من {files.Count} ملف...");

        // تحديد ملفات code-behind التي يجب تخطيها (لأنها ستُضاف تحت ملفات XAML)
        var codeBehindFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // البحث عن جميع ملفات XAML/AXAML وتحديد ملفات code-behind المرتبطة بها
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);

            if (fileName.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) &&
                !fileName.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase))
            {
                // البحث عن ملف .xaml.cs المقابل
                var csFile = file + ".cs";
                if (files.Contains(csFile))
                {
                    codeBehindFiles.Add(csFile);
                }
            }
            else if (fileName.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase) &&
                     !fileName.EndsWith(".axaml.cs", StringComparison.OrdinalIgnoreCase))
            {
                // البحث عن ملف .axaml.cs المقابل
                var csFile = file + ".cs";
                if (files.Contains(csFile))
                {
                    codeBehindFiles.Add(csFile);
                }
            }
        }

        Console.WriteLine($"تم تحديد {codeBehindFiles.Count} ملف code-behind");

        var processedFiles = 0;
        var skippedFiles = 0;

        foreach (var file in files)
        {
            var fullPath = Path.Combine(projectPath, file.Replace("/", "\\"));

            // التحقق من وجود الملف
            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"تحذير: الملف غير موجود: {fullPath}");
                skippedFiles++;
                continue;
            }

            var fileName = Path.GetFileName(file);
            var directory = Path.GetDirectoryName(file)?.Replace("\\", "/") ?? "";

            // تخطي ملفات معينة
            if (ShouldSkipFile(fileName))
            {
                skippedFiles++;
                continue;
            }

            // تخطي ملفات code-behind (ستُضاف تحت ملفات XAML)
            if (codeBehindFiles.Contains(file))
            {
                skippedFiles++;
                continue;
            }

            // إنشاء المجلدات إذا لم تكن موجودة
            EnsureFolderExists(folderDict, directory, projectPath);

            // إضافة الملف
            var folder = folderDict[directory];
            var projectFile = new ProjectFile
            {
                Name = fileName,
                FullPath = fullPath,
                RelativePath = file,
                Type = GetFileType(fileName)
            };

            // إذا كان ملف XAML/AXAML، ابحث عن ملف code-behind وأضفه كملف فرعي
            if (projectFile.Type == ProjectFileType.Xaml)
            {
                var csFile = file + ".cs";
                if (files.Contains(csFile))
                {
                    var csFullPath = Path.Combine(projectPath, csFile.Replace("/", "\\"));
                    if (File.Exists(csFullPath))
                    {
                        projectFile.CodeBehindFile = new ProjectFile
                        {
                            Name = Path.GetFileName(csFile),
                            FullPath = csFullPath,
                            RelativePath = csFile,
                            Type = ProjectFileType.CSharp
                        };
                    }
                }
            }

            folder.Files.Add(projectFile);
            processedFiles++;
        }

        Console.WriteLine($"تمت معالجة {processedFiles} ملف، تم تخطي {skippedFiles} ملف");

        // بناء الهيكل النهائي
        structure.Folders = rootFolder.SubFolders;
        structure.RootFiles = rootFolder.Files;

        // عرض إحصائيات
        Console.WriteLine($"الملفات الجذرية: {structure.RootFiles?.Count ?? 0}");
        Console.WriteLine($"المجلدات: {structure.Folders?.Count ?? 0}");
    }

    /// <summary>
    /// التأكد من وجود المجلد في القاموس
    /// </summary>
    private void EnsureFolderExists(Dictionary<string, ProjectFolder> folderDict, string path, string projectPath)
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
                var folder = new ProjectFolder
                {
                    Name = parts[i],
                    FullPath = Path.Combine(projectPath, currentPath.Replace("/", "\\")),
                    Files = new List<ProjectFile>(),
                    SubFolders = new List<ProjectFolder>()
                };

                folderDict[currentPath] = folder;

                // إضافة المجلد للمجلد الأب
                if (folderDict.ContainsKey(parentPath))
                {
                    folderDict[parentPath].SubFolders.Add(folder);
                }
            }
        }
    }

    /// <summary>
    /// تحديد نوع الملف
    /// </summary>
    private ProjectFileType GetFileType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLower();

        if (ext == ".xaml" || ext == ".axaml")
            return ProjectFileType.Xaml;
        if (ext == ".cs")
            return ProjectFileType.CSharp;
        if (ext == ".xml" || ext == ".config" || ext == ".resx" || ext == ".settings")
            return ProjectFileType.Config;
        if (ext == ".json")
            return ProjectFileType.Json;

        return ProjectFileType.Other;
    }

    /// <summary>
    /// تحديد الملفات التي يجب تخطيها
    /// </summary>
    private bool ShouldSkipFile(string fileName)
    {
        // تخطي ملفات generated
        if (fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase))
            return true;

        // تخطي ملفات .csproj و .sln
        if (fileName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    /// <summary>
    /// تحميل المشروع من ملف .sln
    /// </summary>
    private bool LoadFromSolutionFile(string slnPath, string projectPath)
    {
        try
        {
            // قراءة محتوى ملف .sln للبحث عن مشاريع .csproj
            var slnContent = File.ReadAllText(slnPath);
            var lines = slnContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // البحث عن سطور Project
            foreach (var line in lines)
            {
                if (line.StartsWith("Project("))
                {
                    // تحليل السطر للحصول على مسار .csproj
                    // مثال: Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "ProjectName", "ProjectName\ProjectName.csproj", "{GUID}"
                    var parts = line.Split(new[] { '"' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 5)
                    {
                        var csprojRelativePath = parts[3];
                        if (csprojRelativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                        {
                            var csprojFullPath = Path.Combine(projectPath, csprojRelativePath);
                            if (File.Exists(csprojFullPath))
                            {
                                // تحميل المشروع من ملف .csproj
                                ProjectName = Path.GetFileNameWithoutExtension(csprojFullPath);
                                var doc = XDocument.Load(csprojFullPath);
                                ProjectType = DetermineProjectType(doc, Path.GetDirectoryName(csprojFullPath));
                                Structure = BuildProjectStructure(doc, Path.GetDirectoryName(csprojFullPath));

                                // حفظ الإعدادات
                                Settings.Default.ProjectName = ProjectName;
                                Settings.Default.ProjectPath = projectPath;
                                Settings.Default.ProjectType = ProjectType;
                                Settings.Default.Save();

                                return true;
                            }
                        }
                    }
                }
            }

            // إذا لم نجد أي مشروع، نحاول البحث في المجلدات الفرعية
            var allCsprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories);
            if (allCsprojFiles.Length > 0)
            {
                var csprojFile = allCsprojFiles[0];
                ProjectName = Path.GetFileNameWithoutExtension(csprojFile);
                var doc = XDocument.Load(csprojFile);
                ProjectType = DetermineProjectType(doc, Path.GetDirectoryName(csprojFile));
                Structure = BuildProjectStructure(doc, Path.GetDirectoryName(csprojFile));

                // حفظ الإعدادات
                Settings.Default.ProjectName = ProjectName;
                Settings.Default.ProjectPath = projectPath;
                Settings.Default.ProjectType = ProjectType;
                Settings.Default.Save();

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في تحميل ملف .sln: {ex.Message}");
            return false;
        }
    }
}

/// <summary>
/// بنية المشروع
/// </summary>
public class ProjectStructure
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string RootPath { get; set; }
    public List<ProjectFolder> Folders { get; set; }
    public List<ProjectFile> RootFiles { get; set; }
}

/// <summary>
/// مجلد في المشروع
/// </summary>
public class ProjectFolder
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    public List<ProjectFile> Files { get; set; }
    public List<ProjectFolder> SubFolders { get; set; }
}

/// <summary>
/// ملف في المشروع
/// </summary>
public class ProjectFile
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    public string RelativePath { get; set; }
    public ProjectFileType Type { get; set; }
    public ProjectFile CodeBehindFile { get; set; } // ملف code-behind المرتبط (للملفات XAML/AXAML)
}

/// <summary>
/// أنواع الملفات
/// </summary>
public enum ProjectFileType
{
    Xaml,
    CSharp,
    Config,
    Json,
    Other
}
