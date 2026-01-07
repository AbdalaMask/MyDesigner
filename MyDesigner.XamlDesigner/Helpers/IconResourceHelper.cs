using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using System;
using System.Globalization;

namespace MyDesigner.XamlDesigner.Helpers;

/// <summary>
/// مساعد للحصول على الأيقونات المتجهية من ملف الموارد
/// تحميل آمن وكسول للأيقونات لتجنب تجميد البرنامج
/// </summary>
public static class IconResourceHelper
{
    private static bool _initialized = false;
    private static readonly object _lock = new object();

    /// <summary>
    /// الحصول على أيقونة من الموارد بشكل آمن
    /// </summary>
    public static IImage? GetIcon(string iconKey)
    {
        try
        {
            // تحقق من وجود Application.Current
            if (Application.Current == null)
                return null;

            // ابحث في موارد التطبيق مباشرة
            if ((bool)Application.Current.FindResource(iconKey))
            {
                return Application.Current.FindResource(iconKey) as IImage;
            }

            //// ابحث في MergedDictionaries
            //foreach (var dict in Application.Current.Resources.MergedDictionaries)
            //{
            //    if (dict.Contains(iconKey))
            //    {
            //        return dict[iconKey] as IImage;
            //    }
            //}
        }
        catch
        {
            // في حالة فشل التحميل، نعيد null بصمت
        }
        return null;
    }

    // أيقونات الملفات
    public static IImage? CSharpFileIcon => GetIcon("CSharpFileIcon");
    public static IImage? XamlFileIcon => GetIcon("XamlFileIcon");
    public static IImage? JsonFileIcon => GetIcon("JsonFileIcon");
    public static IImage? XmlFileIcon => GetIcon("XmlFileIcon");
    public static IImage? TextFileIcon => GetIcon("TextFileIcon");
    public static IImage? ImageFileIcon => GetIcon("ImageFileIcon");
    public static IImage? ConfigFileIcon => GetIcon("ConfigFileIcon");

    // أيقونات المجلدات والمشاريع
    public static IImage? FolderIcon => GetIcon("FolderIcon");
    public static IImage? FolderOpenIcon => GetIcon("FolderOpenIcon");
    public static IImage? ProjectIcon => GetIcon("ProjectIcon");
    public static IImage? SolutionIcon => GetIcon("SolutionIcon");

    // أيقونات Dependencies
    public static IImage? DependenciesIcon => GetIcon("DependenciesIcon");
    public static IImage? PackageIcon => GetIcon("PackageIcon");
    public static IImage? AssemblyIcon => GetIcon("AssemblyIcon");
    public static IImage? FrameworkIcon => GetIcon("FrameworkIcon");
    public static IImage? AnalyzerIcon => GetIcon("AnalyzerIcon");
    public static IImage? ReferenceIcon => GetIcon("ReferenceIcon");

    // أيقونات العناصر البرمجية
    public static IImage? NamespaceIcon => GetIcon("NamespaceIcon");
    public static IImage? ClassIcon => GetIcon("ClassIcon");
    public static IImage? InterfaceIcon => GetIcon("InterfaceIcon");
    public static IImage? MethodIcon => GetIcon("MethodIcon");
    public static IImage? PropertyIcon => GetIcon("PropertyIcon");
    public static IImage? EventIcon => GetIcon("EventIcon");
    public static IImage? FieldIcon => GetIcon("FieldIcon");
    public static IImage? EnumIcon => GetIcon("EnumIcon");
    public static IImage? StructIcon => GetIcon("StructIcon");

    // أيقونات الحالة
    public static IImage? WarningIcon => GetIcon("WarningIcon");
    public static IImage? ErrorIcon => GetIcon("ErrorIcon");
    public static IImage? InfoIcon => GetIcon("InfoIcon");
    public static IImage? SuccessIcon => GetIcon("SuccessIcon");
    public static IImage? PropertiesIcon => GetIcon("PropertiesIcon");

    /// <summary>
    /// الحصول على أيقونة بناءً على نوع الملف
    /// </summary>
    public static IImage? GetIconForFileType(string extension)
    {
        return extension?.ToLowerInvariant() switch
        {
            ".cs" => CSharpFileIcon,
            ".xaml" or ".axaml" => XamlFileIcon,
            ".json" => JsonFileIcon,
            ".xml" or ".config" => XmlFileIcon,
            ".txt" or ".md" or ".readme" => TextFileIcon,
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".ico" or ".svg" => ImageFileIcon,
            ".csproj" => ProjectIcon,
            ".sln" => SolutionIcon,
            _ => ReferenceIcon
        };
    }

    /// <summary>
    /// الحصول على أيقونة بناءً على نوع العنصر في شجرة المشروع
    /// </summary>
    public static IImage? GetIconForNodeType(string nodeType)
    {
        return nodeType?.ToLowerInvariant() switch
        {
            "folder" => FolderIcon,
            "project" => ProjectIcon,
            "solution" => SolutionIcon,
            "dependencies" => DependenciesIcon,
            "packages" => PackageIcon,
            "assemblies" => AssemblyIcon,
            "frameworks" => FrameworkIcon,
            "analyzers" => AnalyzerIcon,
            "properties" => PropertiesIcon,
            "reference" => ReferenceIcon,
            _ => FolderIcon
        };
    }
}
public class StringToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string uriPath && !string.IsNullOrEmpty(uriPath))
        {
            try
            {
                return new Bitmap(AssetLoader.Open(new Uri(uriPath)));
            }
            catch { return null; }
        }
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}