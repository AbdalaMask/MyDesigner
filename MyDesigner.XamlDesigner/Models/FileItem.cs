namespace MyDesigner.XamlDesigner.Models;

public class FileItem
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty; // المسار الكامل للملف
    public FileItemType ItemType { get; set; } // نوع العنصر
}

public enum FileItemType
{
    Solution,
    Project,
    Dependencies,
    Analyzers,
    Frameworks,
    Packages,
    Projects,
    Folder,
    XamlFile,
    CSharpFile,
    ConfigFile,
    JsonFile,
    OtherFile
}