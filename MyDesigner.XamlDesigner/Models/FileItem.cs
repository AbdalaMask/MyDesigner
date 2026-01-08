namespace MyDesigner.XamlDesigner.Models;

public class FileItem
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty; // Full path of the file
    public FileItemType ItemType { get; set; } // Type of the item
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