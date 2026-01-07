using Avalonia.Controls;
using Avalonia.Media;
using MyDesigner.XamlDesigner.Models;
using System.IO;

namespace MyDesigner.XamlDesigner.Helpers;

/// <summary>
/// مساعد لإضافة الأيقونات المتجهية إلى عناصر TreeView في Avalonia
/// </summary>
public static class TreeViewIconHelper
{
    /// <summary>
    /// إنشاء TreeViewItem مع أيقونة متجهية
    /// </summary>
    public static TreeViewItem CreateTreeViewItem(string name, IImage icon, string fullPath, FileItemType itemType)
    {
        var item = new TreeViewItem
        {
            Header = new FileItem
            {
                Name = name,
                Icon = icon?.ToString() ?? string.Empty,
                FullPath = fullPath,
                ItemType = itemType
            }
        };

        return item;
    }

    /// <summary>
    /// إنشاء TreeViewItem للمشروع
    /// </summary>
    public static TreeViewItem CreateProjectItem(string projectName, string projectPath, string projectType)
    {
        return CreateTreeViewItem(
            $"{projectName} ({projectType})",
            IconResourceHelper.ProjectIcon,
            projectPath,
            FileItemType.Project
        );
    }

    /// <summary>
    /// إنشاء TreeViewItem للمجلد
    /// </summary>
    public static TreeViewItem CreateFolderItem(string folderName, string folderPath, bool isExpanded = false)
    {
        var icon = isExpanded ? IconResourceHelper.FolderOpenIcon : IconResourceHelper.FolderIcon;
        return CreateTreeViewItem(folderName, icon, folderPath, FileItemType.Folder);
    }

    /// <summary>
    /// إنشاء TreeViewItem للملف
    /// </summary>
    public static TreeViewItem CreateFileItem(string fileName, string filePath)
    {
        var icon = FileIconHelper.GetVectorIconForFile(fileName);
        var itemType = GetFileItemType(fileName);
        return CreateTreeViewItem(fileName, icon, filePath, itemType);
    }

    /// <summary>
    /// إنشاء TreeViewItem لـ Dependencies
    /// </summary>
    public static TreeViewItem CreateDependenciesItem(string projectPath)
    {
        return CreateTreeViewItem(
            "Dependencies",
            IconResourceHelper.DependenciesIcon,
            projectPath,
            FileItemType.Dependencies
        );
    }

    /// <summary>
    /// إنشاء TreeViewItem لـ Packages
    /// </summary>
    public static TreeViewItem CreatePackagesItem(string projectPath)
    {
        return CreateTreeViewItem(
            "Packages",
            IconResourceHelper.PackageIcon,
            projectPath,
            FileItemType.Packages
        );
    }

    /// <summary>
    /// إنشاء TreeViewItem لـ Assemblies
    /// </summary>
    public static TreeViewItem CreateAssembliesItem(string projectPath)
    {
        return CreateTreeViewItem(
            "Assemblies",
            IconResourceHelper.AssemblyIcon,
            projectPath,
            FileItemType.Dependencies
        );
    }

    /// <summary>
    /// إنشاء TreeViewItem لـ Frameworks
    /// </summary>
    public static TreeViewItem CreateFrameworksItem(string projectPath)
    {
        return CreateTreeViewItem(
            "Frameworks",
            IconResourceHelper.FrameworkIcon,
            projectPath,
            FileItemType.Frameworks
        );
    }

    /// <summary>
    /// إنشاء TreeViewItem لـ Analyzers
    /// </summary>
    public static TreeViewItem CreateAnalyzersItem(string projectPath)
    {
        return CreateTreeViewItem(
            "Analyzers",
            IconResourceHelper.AnalyzerIcon,
            projectPath,
            FileItemType.Analyzers
        );
    }

    /// <summary>
    /// إنشاء TreeViewItem لـ Projects (مشاريع مرجعية)
    /// </summary>
    public static TreeViewItem CreateProjectsItem(string projectPath)
    {
        return CreateTreeViewItem(
            "Projects",
            IconResourceHelper.ProjectIcon,
            projectPath,
            FileItemType.Projects
        );
    }

    /// <summary>
    /// إنشاء TreeViewItem لحزمة NuGet
    /// </summary>
    public static TreeViewItem CreatePackageItem(string packageName, string version, string projectPath)
    {
        var displayName = string.IsNullOrEmpty(version) ? packageName : $"{packageName} ({version})";
        return CreateTreeViewItem(
            displayName,
            IconResourceHelper.PackageIcon,
            projectPath,
            FileItemType.Packages
        );
    }

    /// <summary>
    /// إنشاء TreeViewItem لتجميع (Assembly)
    /// </summary>
    public static TreeViewItem CreateAssemblyItem(string assemblyName, string projectPath)
    {
        return CreateTreeViewItem(
            assemblyName,
            IconResourceHelper.AssemblyIcon,
            projectPath,
            FileItemType.Dependencies
        );
    }

    /// <summary>
    /// إنشاء TreeViewItem لـ Framework
    /// </summary>
    public static TreeViewItem CreateFrameworkItem(string frameworkName, string projectPath)
    {
        return CreateTreeViewItem(
            frameworkName,
            IconResourceHelper.FrameworkIcon,
            projectPath,
            FileItemType.Frameworks
        );
    }

    /// <summary>
    /// إنشاء TreeViewItem مع محتوى مخصص
    /// </summary>
    public static TreeViewItem CreateCustomTreeViewItem(object header, string fullPath, FileItemType itemType)
    {
        var item = new TreeViewItem
        {
            Header = header,
            Tag = new FileItem
            {
                Name = header?.ToString() ?? "",
                FullPath = fullPath,
                ItemType = itemType
            }
        };

        return item;
    }

    /// <summary>
    /// إضافة أيقونة إلى TreeViewItem موجود
    /// </summary>
    public static void AddIconToTreeViewItem(TreeViewItem item, IImage icon)
    {
        if (item.Header is FileItem fileItem)
        {
            fileItem.Icon = icon?.ToString() ?? "";
        }
        else
        {
            // إنشاء FileItem جديد إذا لم يكن موجوداً
            var headerText = item.Header?.ToString() ?? "";
            item.Header = new FileItem
            {
                Name = headerText,
                Icon = icon?.ToString() ?? "",
                FullPath = item.Tag?.ToString() ?? "",
                ItemType = FileItemType.OtherFile
            };
        }
    }

    /// <summary>
    /// الحصول على FileItem من TreeViewItem
    /// </summary>
    public static FileItem GetFileItem(TreeViewItem item)
    {
        if (item.Header is FileItem fileItem)
            return fileItem;

        if (item.Tag is FileItem tagFileItem)
            return tagFileItem;

        // إنشاء FileItem افتراضي
        return new FileItem
        {
            Name = item.Header?.ToString() ?? "",
            FullPath = item.Tag?.ToString() ?? "",
            ItemType = FileItemType.OtherFile
        };
    }

    /// <summary>
    /// تحديد نوع FileItemType بناءً على اسم الملف
    /// </summary>
    private static FileItemType GetFileItemType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".cs" => FileItemType.CSharpFile,
            ".axaml" => FileItemType.XamlFile,
            ".xaml" => FileItemType.XamlFile,
            ".json" => FileItemType.JsonFile,
            ".xml" or ".config" => FileItemType.ConfigFile,
            ".csproj" or ".vbproj" or ".fsproj" => FileItemType.Project,
            ".sln" => FileItemType.Solution,
            _ => FileItemType.OtherFile
        };
    }

    /// <summary>
    /// تحديث أيقونة TreeViewItem بناءً على حالة التوسع
    /// </summary>
    public static void UpdateFolderIcon(TreeViewItem item, bool isExpanded)
    {
        if (GetFileItem(item).ItemType == FileItemType.Folder)
        {
            var icon = isExpanded ? IconResourceHelper.FolderOpenIcon : IconResourceHelper.FolderIcon;
            AddIconToTreeViewItem(item, icon);
        }
    }

    /// <summary>
    /// إنشاء TreeViewItem للحلول (Solutions)
    /// </summary>
    public static TreeViewItem CreateSolutionItem(string solutionName, string solutionPath)
    {
        return CreateTreeViewItem(
            solutionName,
            IconResourceHelper.SolutionIcon,
            solutionPath,
            FileItemType.Solution
        );
    }
}