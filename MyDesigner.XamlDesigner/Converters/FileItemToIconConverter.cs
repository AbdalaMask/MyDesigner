using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MyDesigner.XamlDesigner.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using static MyDesigner.XamlDesigner.ViewModels.ProjectExplorerViewViewModel;

namespace MyDesigner.XamlDesigner.Converters
{
    /// <summary>
    /// محول لتحويل FileItem إلى أيقونة متجهية
    /// يعمل مع كل من ItemType والمسار القديم للتوافق الكامل
    /// </summary>
    public class FileItemToIconConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is FileItem fileItem)
                {
                    // أولاً: محاولة الحصول على الأيقونة بناءً على ItemType
                    IImage? icon = GetIconByItemType(fileItem.ItemType);

                    // ثانياً: إذا لم نجد، حاول بناءً على اسم الملف
                    if (icon == null && !string.IsNullOrEmpty(fileItem.Name))
                    {
                        icon = FileIconHelper.GetVectorIconForFile(fileItem.Name);
                    }

                    // ثالثاً: إذا لم نجد، حاول تحميل الصورة القديمة من المسار
                    if (icon == null && !string.IsNullOrEmpty(fileItem.Icon))
                    {
                        try
                        {
                            // محاولة تحميل الصورة القديمة
                            var uri = new Uri(fileItem.Icon, UriKind.RelativeOrAbsolute);
                            icon = new Bitmap(uri.ToString());
                        }
                        catch
                        {
                            // تجاهل الأخطاء
                        }
                    }

                    // إرجاع الأيقونة أو null
                    return icon;
                }
            }
            catch
            {
                // في حالة أي خطأ، أرجع null
            }

            return null;
        }

        private IImage? GetIconByItemType(FileItemType itemType)
        {
            return itemType switch
            {
                FileItemType.Project => IconResourceHelper.ProjectIcon,
                FileItemType.Dependencies => IconResourceHelper.DependenciesIcon,
                FileItemType.Analyzers => IconResourceHelper.AnalyzerIcon,
                FileItemType.Frameworks => IconResourceHelper.FrameworkIcon,
                FileItemType.Packages => IconResourceHelper.PackageIcon,
                FileItemType.Projects => IconResourceHelper.ProjectIcon,
                FileItemType.Folder => IconResourceHelper.FolderIcon,
                FileItemType.XamlFile => IconResourceHelper.XamlFileIcon,
                FileItemType.CSharpFile => IconResourceHelper.CSharpFileIcon,
                FileItemType.ConfigFile => IconResourceHelper.ConfigFileIcon,
                FileItemType.JsonFile => IconResourceHelper.JsonFileIcon,
                FileItemType.OtherFile => IconResourceHelper.ReferenceIcon,
                _ => null
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Helper classes that need to be implemented
    public static class FileIconHelper
    {
        public static IImage? GetVectorIconForFile(string fileName)
        {
            // Implement file icon logic based on file extension
            var extension = System.IO.Path.GetExtension(fileName).ToLower();

            return extension switch
            {
                ".xaml" => IconResourceHelper.XamlFileIcon,
                ".cs" => IconResourceHelper.CSharpFileIcon,
                ".json" => IconResourceHelper.JsonFileIcon,
                ".config" => IconResourceHelper.ConfigFileIcon,
                _ => IconResourceHelper.ReferenceIcon
            };
        }
    }

    public static class IconResourceHelper
    {
        // These would be loaded from resources or created as vector graphics
        public static IImage? ProjectIcon => null; // Load from resources
        public static IImage? DependenciesIcon => null; // Load from resources
        public static IImage? AnalyzerIcon => null; // Load from resources
        public static IImage? FrameworkIcon => null; // Load from resources
        public static IImage? PackageIcon => null; // Load from resources
        public static IImage? FolderIcon => null; // Load from resources
        public static IImage? XamlFileIcon => null; // Load from resources
        public static IImage? CSharpFileIcon => null; // Load from resources
        public static IImage? ConfigFileIcon => null; // Load from resources
        public static IImage? JsonFileIcon => null; // Load from resources
        public static IImage? ReferenceIcon => null; // Load from resources
    }
}
