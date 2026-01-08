using Avalonia.Controls;
using Avalonia.Media;
using MyDesigner.XamlDesigner.Models;
using System.IO;

namespace MyDesigner.XamlDesigner.Helpers;

/// <summary>
/// Helper for adding vector icons to TreeView elements in Avalonia
/// </summary>
public static class TreeViewIconHelper
{
    /// <summary>
    /// Create TreeViewItem with vector icon
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
    /// Create TreeViewItem for project
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
    /// Create TreeViewItem for folder
    /// </summary>
    public static TreeViewItem CreateFolderItem(string folderName, string folderPath, bool isExpanded = false)
    {
        var icon = isExpanded ? IconResourceHelper.FolderOpenIcon : IconResourceHelper.FolderIcon;
        return CreateTreeViewItem(folderName, icon, folderPath, FileItemType.Folder);
    }

    /// <summary>
    /// Create TreeViewItem for file
    /// </summary>
    public static TreeViewItem CreateFileItem(string fileName, string filePath)
    {
        var icon = FileIconHelper.GetVectorIconForFile(fileName);
        var itemType = GetFileItemType(fileName);
        return CreateTreeViewItem(fileName, icon, filePath, itemType);
    }

    /// <summary>
    /// Create TreeViewItem for Dependencies
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
    /// Create TreeViewItem for Packages
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
    /// Create TreeViewItem for Assemblies
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
    /// Create TreeViewItem for Frameworks
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
    /// Create TreeViewItem for Analyzers
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
    /// Create TreeViewItem for Projects (referenced projects)
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
    /// Create TreeViewItem for NuGet package
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
    /// Create TreeViewItem for Assembly
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
    /// Create TreeViewItem for Framework
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
    /// Create TreeViewItem with custom content
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
    /// Add icon to existing TreeViewItem
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
    /// Get FileItem from TreeViewItem
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
    /// Determine FileItemType based on file name
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
    /// Update TreeViewItem icon based on expansion state
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
    /// Create TreeViewItem for Solutions
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