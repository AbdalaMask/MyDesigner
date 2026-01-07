using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

namespace MyDesigner.XamlDesigner.Services;

/// <summary>
/// خدمة إدارة المشاريع
/// </summary>
public class ProjectService
{
    private readonly List<Document> _openDocuments = new();
    private Document? _activeDocument;

    public event EventHandler<Document>? DocumentOpened;
    public event EventHandler<Document>? DocumentClosed;
    public event EventHandler<Document>? ActiveDocumentChanged;

    /// <summary>
    /// المستندات المفتوحة
    /// </summary>
    public IReadOnlyList<Document> OpenDocuments => _openDocuments.AsReadOnly();

    /// <summary>
    /// المستند النشط
    /// </summary>
    public Document? ActiveDocument
    {
        get => _activeDocument;
        set
        {
            if (_activeDocument != value)
            {
                _activeDocument = value;
                ActiveDocumentChanged?.Invoke(this, value!);
            }
        }
    }

    /// <summary>
    /// فتح مستند جديد
    /// </summary>
    public async Task<Document?> OpenDocumentAsync(string filePath)
    {
        try
        {
            // التحقق من أن الملف موجود
            if (!File.Exists(filePath))
                return null;

            // التحقق من أن المستند غير مفتوح بالفعل
            var existingDoc = _openDocuments.Find(d => 
                string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            
            if (existingDoc != null)
            {
                ActiveDocument = existingDoc;
                return existingDoc;
            }

            // قراءة محتوى الملف
            var content = await File.ReadAllTextAsync(filePath);
            
            // إنشاء مستند جديد
            var document = new Document
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                Content = content,
                IsModified = false
            };

            // إضافة المستند إلى القائمة
            _openDocuments.Add(document);
            ActiveDocument = document;

            // إثارة الحدث
            DocumentOpened?.Invoke(this, document);

            return document;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening document: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// إنشاء مستند جديد
    /// </summary>
    public Document CreateNewDocument(string fileName = "Untitled.axaml")
    {
        var document = new Document
        {
            FileName = fileName,
            Content = GetDefaultContent(fileName),
            IsModified = false,
            IsNew = true
        };

        _openDocuments.Add(document);
        ActiveDocument = document;

        DocumentOpened?.Invoke(this, document);
        return document;
    }

    /// <summary>
    /// حفظ مستند
    /// </summary>
    public async Task<bool> SaveDocumentAsync(Document document, string? filePath = null)
    {
        try
        {
            var targetPath = filePath ?? document.FilePath;
            
            if (string.IsNullOrEmpty(targetPath))
                return false;

            await File.WriteAllTextAsync(targetPath, document.Content);
            
            document.FilePath = targetPath;
            document.FileName = Path.GetFileName(targetPath);
            document.IsModified = false;
            document.IsNew = false;

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving document: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// إغلاق مستند
    /// </summary>
    public bool CloseDocument(Document document)
    {
        try
        {
            if (!_openDocuments.Contains(document))
                return false;

            // التحقق من التغييرات غير المحفوظة
            if (document.IsModified)
            {
                // يمكن إضافة حوار تأكيد هنا
                // في الوقت الحالي، سنغلق المستند مباشرة
            }

            _openDocuments.Remove(document);

            // تحديد مستند نشط جديد
            if (ActiveDocument == document)
            {
                ActiveDocument = _openDocuments.Count > 0 ? _openDocuments[^1] : null;
            }

            DocumentClosed?.Invoke(this, document);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error closing document: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// إغلاق جميع المستندات
    /// </summary>
    public async Task<bool> CloseAllDocumentsAsync()
    {
        try
        {
            var documentsToClose = new List<Document>(_openDocuments);
            
            foreach (var document in documentsToClose)
            {
                if (document.IsModified)
                {
                    // يمكن إضافة حوار تأكيد هنا
                    var saved = await SaveDocumentAsync(document);
                    if (!saved)
                    {
                        // المستخدم ألغى العملية
                        return false;
                    }
                }
                
                CloseDocument(document);
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error closing all documents: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// الحصول على المحتوى الافتراضي للملف
    /// </summary>
    private string GetDefaultContent(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        
        return extension switch
        {
            ".axaml" => GetDefaultAxamlContent(),
            ".cs" => GetDefaultCSharpContent(),
            ".json" => "{\n  \n}",
            ".xml" => "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<root>\n</root>",
            _ => string.Empty
        };
    }

    /// <summary>
    /// المحتوى الافتراضي لملف AXAML
    /// </summary>
    private string GetDefaultAxamlContent()
    {
        return @"<UserControl xmlns=""https://github.com/avaloniaui""
             xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
             xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
             xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
             mc:Ignorable=""d"" d:DesignWidth=""800"" d:DesignHeight=""450""
             x:Class=""MyNamespace.MyUserControl"">
    
    <Grid>
        <!-- Add your content here -->
    </Grid>
    
</UserControl>";
    }

    /// <summary>
    /// المحتوى الافتراضي لملف C#
    /// </summary>
    private string GetDefaultCSharpContent()
    {
        return @"using System;
using Avalonia.Controls;

namespace MyNamespace;

public partial class MyClass : UserControl
{
    public MyClass()
    {
        InitializeComponent();
    }
}";
    }

    /// <summary>
    /// البحث عن مستند بالمسار
    /// </summary>
    public Document? FindDocumentByPath(string filePath)
    {
        return _openDocuments.Find(d => 
            string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// الحصول على المستندات المعدلة
    /// </summary>
    public List<Document> GetModifiedDocuments()
    {
        return _openDocuments.FindAll(d => d.IsModified);
    }
}