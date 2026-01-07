using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MyDesigner.XamlDesigner.Configuration;



namespace MyDesigner.XamlDesigner.Helpers;

/// <summary>
/// مدير إعدادات مقاسات الصفحة للمنصات المختلفة
/// </summary>
public static class PageSizeManager
{
    private static readonly Dictionary<DocumentType, PageSizeSettings> _defaultSettings = new()
    {
        [DocumentType.WPF] = new PageSizeSettings
        {
            Platform = "WPF",
            Width = 800,
            Height = 600,
            WpfSettings = new WpfPageSettings
            {
                IsResizable = true,
                StartMaximized = false
            }
        },
        [DocumentType.Avalonia] = new PageSizeSettings
        {
            Platform = "Avalonia",
            Width = 1280,
            Height = 720,
            AvaloniaSettings = new AvaloniaPageSettings
            {
                CanResize = true,
                Topmost = false
            }
        },
        [DocumentType.Maui] = new PageSizeSettings
        {
            Platform = "Maui",
            Width = 390,
            Height = 844,
            MauiSettings = new MauiPageSettings
            {
                MinWidth = 320,
                MinHeight = 568
            }
        }
    };

    /// <summary>
    /// الحصول على الإعدادات الافتراضية لمنصة معينة
    /// </summary>
    public static PageSizeSettings GetDefaultSettings(DocumentType documentType)
    {
        return _defaultSettings.ContainsKey(documentType) 
            ? CloneSettings(_defaultSettings[documentType])
            : CloneSettings(_defaultSettings[DocumentType.WPF]);
    }

    /// <summary>
    /// تطبيق إعدادات مقاس الصفحة على مستند
    /// </summary>
    public static void ApplyPageSizeToDocument(Document document, PageSizeSettings settings)
    {
        if (document?.DesignSurface == null || settings == null)
            return;

        try
        {
            // تطبيق الأبعاد الأساسية
            document.DesignSurface.Width = settings.Width;
            document.DesignSurface.Height = settings.Height;

            // تطبيق الإعدادات الخاصة بالمنصة
            ApplyPlatformSpecificSettings(document, settings);

            // تحديث خصائص المستند
            //document.PageWidth = settings.Width;
            //document.PageHeight = settings.Height;
            //document.Platform = Enum.Parse<DocumentType>(settings.Platform);

            // إشعار بالتغيير
            //document.OnPropertyChanged(nameof(document.PageWidth));
            //document.OnPropertyChanged(nameof(document.PageHeight));
            //document.OnPropertyChanged(nameof(document.Platform));
        }
        catch (Exception ex)
        {
            Shell.ReportException(ex);
        }
    }

    /// <summary>
    /// تطبيق الإعدادات الخاصة بكل منصة
    /// </summary>
    private static void ApplyPlatformSpecificSettings(Document document, PageSizeSettings settings)
    {
        switch (settings.Platform)
        {
            case "WPF":
                ApplyWpfSettings(document, settings.WpfSettings);
                break;
            case "Avalonia":
                ApplyAvaloniaSettings(document, settings.AvaloniaSettings);
                break;
            case "Maui":
                ApplyMauiSettings(document, settings.MauiSettings);
                break;
        }
    }

    /// <summary>
    /// تطبيق إعدادات WPF
    /// </summary>
    private static void ApplyWpfSettings(Document document, WpfPageSettings wpfSettings)
    {
        if (wpfSettings == null) return;

        // إضافة خصائص WPF محددة إلى XAML
        UpdateXamlForWpf(document, wpfSettings);
    }

    /// <summary>
    /// تطبيق إعدادات Avalonia
    /// </summary>
    private static void ApplyAvaloniaSettings(Document document, AvaloniaPageSettings avaloniaSettings)
    {
        if (avaloniaSettings == null) return;

        // تطبيق إعدادات Avalonia
        UpdateXamlForAvalonia(document, avaloniaSettings);
    }

    /// <summary>
    /// تطبيق إعدادات MAUI
    /// </summary>
    private static void ApplyMauiSettings(Document document, MauiPageSettings mauiSettings)
    {
        if (mauiSettings == null) return;

        // تطبيق إعدادات MAUI
        UpdateXamlForMaui(document, mauiSettings);
    }

    /// <summary>
    /// تحديث XAML لـ WPF
    /// </summary>
    private static void UpdateXamlForWpf(Document document, WpfPageSettings settings)
    {
        var xamlContent = document.Text;
        
        // إضافة أو تحديث خصائص النافذة
        if (xamlContent.Contains("<Window"))
        {
            // تحديث ResizeMode
            var resizeMode = settings.IsResizable ? "CanResize" : "NoResize";
            xamlContent = UpdateXamlAttribute(xamlContent, "ResizeMode", resizeMode);

            // تحديث WindowState
            if (settings.StartMaximized)
            {
                xamlContent = UpdateXamlAttribute(xamlContent, "WindowState", "Maximized");
            }

            document.Text = xamlContent;
        }
    }

    /// <summary>
    /// تحديث XAML لـ Avalonia
    /// </summary>
    private static void UpdateXamlForAvalonia(Document document, AvaloniaPageSettings settings)
    {
        var xamlContent = document.Text;
        
        if (xamlContent.Contains("<Window"))
        {
            // تحديث CanResize
            xamlContent = UpdateXamlAttribute(xamlContent, "CanResize", settings.CanResize.ToString().ToLower());

            // تحديث Topmost
            if (settings.Topmost)
            {
                xamlContent = UpdateXamlAttribute(xamlContent, "Topmost", "True");
            }

            document.Text = xamlContent;
        }
    }

    /// <summary>
    /// تحديث XAML لـ MAUI
    /// </summary>
    private static void UpdateXamlForMaui(Document document, MauiPageSettings settings)
    {
        var xamlContent = document.Text;
        
        // إضافة إعدادات MAUI
        if (xamlContent.Contains("<ContentPage") || xamlContent.Contains("<Shell"))
        {
            // إضافة MinimumWidthRequest و MinimumHeightRequest
            xamlContent = UpdateXamlAttribute(xamlContent, "MinimumWidthRequest", settings.MinWidth.ToString());
            xamlContent = UpdateXamlAttribute(xamlContent, "MinimumHeightRequest", settings.MinHeight.ToString());

            document.Text = xamlContent;
        }
    }

    /// <summary>
    /// تحديث خاصية في XAML
    /// </summary>
    private static string UpdateXamlAttribute(string xamlContent, string attributeName, string attributeValue)
    {
        var pattern = $@"{attributeName}\s*=\s*""[^""]*""";
        var replacement = $@"{attributeName}=""{attributeValue}""";
        
        if (System.Text.RegularExpressions.Regex.IsMatch(xamlContent, pattern))
        {
            return System.Text.RegularExpressions.Regex.Replace(xamlContent, pattern, replacement);
        }
        else
        {
            // إضافة الخاصية إذا لم تكن موجودة
            var windowTagPattern = @"(<Window[^>]*?)>";
            var windowReplacement = $@"$1 {attributeName}=""{attributeValue}"">";
            
            return System.Text.RegularExpressions.Regex.Replace(xamlContent, windowTagPattern, windowReplacement);
        }
    }

    /// <summary>
    /// إنشاء نسخة من الإعدادات
    /// </summary>
    private static PageSizeSettings CloneSettings(PageSizeSettings original)
    {
        return new PageSizeSettings
        {
            Platform = original.Platform,
            Width = original.Width,
            Height = original.Height,
            WpfSettings = original.WpfSettings != null ? new WpfPageSettings
            {
                IsResizable = original.WpfSettings.IsResizable,
                StartMaximized = original.WpfSettings.StartMaximized
            } : null,
            AvaloniaSettings = original.AvaloniaSettings != null ? new AvaloniaPageSettings
            {
                CanResize = original.AvaloniaSettings.CanResize,
                Topmost = original.AvaloniaSettings.Topmost
            } : null,
            MauiSettings = original.MauiSettings != null ? new MauiPageSettings
            {
                MinWidth = original.MauiSettings.MinWidth,
                MinHeight = original.MauiSettings.MinHeight
            } : null
        };
    }

    /// <summary>
    /// الحصول على المقاسات الشائعة لمنصة معينة
    /// </summary>
    public static List<PageSizePreset> GetCommonSizesForPlatform(DocumentType documentType)
    {
        return documentType switch
        {
            DocumentType.WPF => new List<PageSizePreset>
            {
                new("Desktop HD", 1920, 1080),
                new("Desktop Standard", 1366, 768),
                new("Tablet", 1024, 768),
                new("Small Window", 800, 600),
                new("Large Window", 1200, 900)
            },
            DocumentType.Avalonia => new List<PageSizePreset>
            {
                new("Cross-Platform Desktop", 1280, 720),
                new("Linux Desktop", 1920, 1080),
                new("macOS Window", 1440, 900),
                new("Compact View", 800, 600)
            },
            DocumentType.Maui => new List<PageSizePreset>
            {
                new("iPhone 14", 390, 844),
                new("iPhone 14 Pro Max", 430, 932),
                new("Samsung Galaxy S23", 360, 780),
                new("iPad", 768, 1024),
                new("Android Tablet", 800, 1280)
            },
            _ => new List<PageSizePreset>()
        };
    }

    /// <summary>
    /// التحقق من صحة الإعدادات
    /// </summary>
    public static bool ValidateSettings(PageSizeSettings settings)
    {
        if (settings == null) return false;
        if (settings.Width <= 0 || settings.Height <= 0) return false;
        if (string.IsNullOrEmpty(settings.Platform)) return false;

        // التحقق من الإعدادات الخاصة بكل منصة
        switch (settings.Platform)
        {
            case "Maui":
                if (settings.MauiSettings != null)
                {
                    if (settings.MauiSettings.MinWidth < 0 || settings.MauiSettings.MinHeight < 0)
                        return false;
                }
                break;
        }

        return true;
    }

    /// <summary>
    /// حفظ الإعدادات في ملف التكوين
    /// </summary>
    public static void SaveSettings(PageSizeSettings settings)
    {
        try
        {
            // حفظ في إعدادات التطبيق
            Settings.Default.LastPageWidth = settings.Width;
            Settings.Default.LastPageHeight = settings.Height;
            Settings.Default.LastPlatform = settings.Platform;
            Settings.Default.Save();
        }
        catch (Exception ex)
        {
            Shell.ReportException(ex);
        }
    }

    /// <summary>
    /// تحميل الإعدادات المحفوظة
    /// </summary>
    public static PageSizeSettings LoadSavedSettings()
    {
        try
        {
            var platform = Settings.Default.LastPlatform;
            if (string.IsNullOrEmpty(platform))
                platform = Settings.Default.DefaultPlatform ?? "WPF";

            var documentType = Enum.Parse<DocumentType>(platform);
            var settings = GetDefaultSettings(documentType);

            if (Settings.Default.LastPageWidth > 0)
                settings.Width = Settings.Default.LastPageWidth;
            
            if (Settings.Default.LastPageHeight > 0)
                settings.Height = Settings.Default.LastPageHeight;

            return settings;
        }
        catch
        {
            return GetDefaultSettings(DocumentType.WPF);
        }
    }

    /// <summary>
    /// تطبيق إعدادات تلقائية ذكية بناءً على نوع المشروع
    /// </summary>
    public static void ApplyAutoSettings(Document document)
    {
        try
        {
            // تحديد نوع المنصة
            var platformType = DetectPlatformFromDocument(document);
            
            // الحصول على الإعدادات المناسبة
            var settings = GetOptimalSettingsForPlatform(platformType, document);
            
            // تطبيق الإعدادات
            ApplyPageSizeToDocument(document, settings);
            
            // حفظ الإعدادات للمرة القادمة
            SaveSettings(settings);
            
            Console.WriteLine($"تم تطبيق إعدادات تلقائية: {platformType} - {settings.Width}×{settings.Height}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في التطبيق التلقائي: {ex.Message}");
            // تطبيق إعدادات افتراضية آمنة
            ApplyFallbackSettings(document);
        }
    }

    /// <summary>
    /// اكتشاف نوع المنصة من المستند
    /// </summary>
    private static DocumentType DetectPlatformFromDocument(Document document)
    {
        try
        {
            // من إعدادات المشروع
            if (!string.IsNullOrEmpty(Settings.Default.ProjectType))
            {
                if (Enum.TryParse<DocumentType>(Settings.Default.ProjectType, out var projectType))
                    return projectType;
            }

            // من امتداد الملف
            if (!string.IsNullOrEmpty(document.FilePath))
            {
                var extension = Path.GetExtension(document.FilePath).ToLower();
                switch (extension)
                {
                    case ".axaml":
                        return DocumentType.Avalonia;
                    case ".xaml":
                        // فحص إضافي للتمييز بين WPF و MAUI و Uno
                        return DetectXamlPlatform(document);
                }
            }

            return DocumentType.WPF; // افتراضي
        }
        catch
        {
            return DocumentType.WPF;
        }
    }

    /// <summary>
    /// تحديد نوع منصة XAML من المحتوى
    /// </summary>
    private static DocumentType DetectXamlPlatform(Document document)
    {
        try
        {
            var content = document.Text?.ToLower() ?? "";
            var fileName = document.FileName?.ToLower() ?? document.FilePath?.ToLower() ?? "";

            // فحص اسم الملف أولاً
            if (fileName.Contains("maui") || fileName.Contains("mobile"))
                return DocumentType.Maui;
            if (fileName.Contains("uno"))
                return DocumentType.Uno;
            if (fileName.Contains("avalonia"))
                return DocumentType.Avalonia;

            // فحص المحتوى
            if (content.Contains("microsoft.maui") || content.Contains("contentpage") || 
                content.Contains("shell") && content.Contains("flyoutitem"))
                return DocumentType.Maui;
            
            if (content.Contains("uno.ui") || content.Contains("uno.toolkit"))
                return DocumentType.Uno;
            
            if (content.Contains("avalonia") || content.Contains("avaloniaui"))
                return DocumentType.Avalonia;

            return DocumentType.WPF; // افتراضي
        }
        catch
        {
            return DocumentType.WPF;
        }
    }

    /// <summary>
    /// الحصول على إعدادات مثلى للمنصة
    /// </summary>
    private static PageSizeSettings GetOptimalSettingsForPlatform(DocumentType platform, Document document)
    {
        var settings = GetDefaultSettings(platform);

        // تخصيص إضافي بناءً على السياق
        switch (platform)
        {
            case DocumentType.WPF:
                // للتطبيقات المكتبية، استخدم مقاس شائع
                settings.Width = 1366;
                settings.Height = 768;
                break;

            case DocumentType.Avalonia:
                // للتطبيقات متعددة المنصات، استخدم مقاس متوسط
                settings.Width = 1280;
                settings.Height = 720;
                break;

            case DocumentType.Maui:
                // للتطبيقات المحمولة، ابدأ بمقاس هاتف شائع
                settings.Width = 390;
                settings.Height = 844;
                break;

            case DocumentType.Uno:
                // مشابه لـ MAUI
                settings.Width = 390;
                settings.Height = 844;
                break;
        }

        return settings;
    }

    /// <summary>
    /// تطبيق إعدادات احتياطية آمنة
    /// </summary>
    private static void ApplyFallbackSettings(Document document)
    {
        try
        {
            var fallbackSettings = new PageSizeSettings
            {
                Platform = "WPF",
                Width = 800,
                Height = 600,
                WpfSettings = new WpfPageSettings
                {
                    IsResizable = true,
                    StartMaximized = false
                }
            };

            ApplyPageSizeToDocument(document, fallbackSettings);
            Console.WriteLine("تم تطبيق إعدادات احتياطية آمنة");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في تطبيق الإعدادات الاحتياطية: {ex.Message}");
        }
    }
}

/// <summary>
/// إعدادات مقاس الصفحة
/// </summary>
public class PageSizeSettings
{
    public string Platform { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public WpfPageSettings WpfSettings { get; set; }
    public AvaloniaPageSettings AvaloniaSettings { get; set; }
    public MauiPageSettings MauiSettings { get; set; }
}

/// <summary>
/// إعدادات صفحة WPF
/// </summary>
public class WpfPageSettings
{
    public bool IsResizable { get; set; }
    public bool StartMaximized { get; set; }
}

/// <summary>
/// إعدادات صفحة Avalonia
/// </summary>
public class AvaloniaPageSettings
{
    public bool CanResize { get; set; }
    public bool Topmost { get; set; }
}

/// <summary>
/// إعدادات صفحة MAUI
/// </summary>
public class MauiPageSettings
{
    public double MinWidth { get; set; }
    public double MinHeight { get; set; }
}

/// <summary>
/// قالب مقاس صفحة
/// </summary>
public class PageSizePreset
{
    public string Name { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public PageSizePreset(string name, double width, double height)
    {
        Name = name;
        Width = width;
        Height = height;
    }
}

/// <summary>
/// أنواع المستندات
/// </summary>
public enum DocumentType
{
    WPF,
    Avalonia,
    Maui,
    Uno
}