using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace MyDesigner.XamlDesigner.Helpers;

/// <summary>
/// Helper class for loading references from project files
/// </summary>
public static class ProjectReferenceLoader
{
    /// <summary>
    /// Load all references from .csproj file recursively
    /// </summary>
    public static List<string> LoadProjectReferences(string xamlFilePath)
    {
        var references = new List<string>();
        var processedProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        try
        {
            // البحث عن ملف .csproj
            var projectFile = FindProjectFile(xamlFilePath);
            if (projectFile == null)
            {
                Console.WriteLine($"[ProjectReferenceLoader] لم يتم العثور على ملف .csproj لـ: {xamlFilePath}");
                return references;
            }
            
            Console.WriteLine($"[ProjectReferenceLoader] تم العثور على ملف المشروع: {Path.GetFileName(projectFile)}");
            Console.WriteLine($"[ProjectReferenceLoader] المسار الكامل: {projectFile}");
            
            // تحميل المراجع بشكل متداخل
            LoadProjectReferencesRecursive(projectFile, references, processedProjects);
            
            Console.WriteLine($"[ProjectReferenceLoader] ========================================");
            Console.WriteLine($"[ProjectReferenceLoader] إجمالي المراجع المحملة: {references.Count}");
            if (references.Count > 0)
            {
                Console.WriteLine($"[ProjectReferenceLoader] قائمة المراجع:");
                foreach (var r in references)
                {
                    Console.WriteLine($"[ProjectReferenceLoader]   - {Path.GetFileName(r)}");
                }
            }
            Console.WriteLine($"[ProjectReferenceLoader] ========================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProjectReferenceLoader] خطأ: {ex.Message}");
        }
        
        return references;
    }
    
    /// <summary>
    /// Load references recursively from project file (public for use from SolutionReferenceLoader)
    /// </summary>
    public static void LoadProjectReferencesRecursivePublic(string projectFile, List<string> references, HashSet<string> processedProjects)
    {
        LoadProjectReferencesRecursive(projectFile, references, processedProjects);
    }
    
    /// <summary>
    /// Load references recursively from project file
    /// </summary>
    private static void LoadProjectReferencesRecursive(string projectFile, List<string> references, HashSet<string> processedProjects)
    {
        // تجنب المعالجة المتكررة لنفس المشروع
        var normalizedPath = Path.GetFullPath(projectFile);
        if (processedProjects.Contains(normalizedPath))
        {
            return;
        }
        
        processedProjects.Add(normalizedPath);
        
        try
        {
            // قراءة ملف .csproj
            var doc = XDocument.Load(projectFile);
            var projectDir = Path.GetDirectoryName(projectFile);
            
            // البحث عن ProjectReference
            var projectReferences = doc.Descendants("ProjectReference")
                .Select(pr => pr.Attribute("Include")?.Value)
                .Where(path => !string.IsNullOrEmpty(path))
                .ToList();
            
            Console.WriteLine($"[ProjectReferenceLoader] {Path.GetFileName(projectFile)}: تم العثور على {projectReferences.Count} مرجع");
            
            // معالجة كل مرجع
            foreach (var projectRef in projectReferences)
            {
                // تحويل المسار النسبي إلى مسار مطلق
                var fullProjectPath = Path.GetFullPath(Path.Combine(projectDir, projectRef));
                
                if (!File.Exists(fullProjectPath))
                {
                    Console.WriteLine($"[ProjectReferenceLoader] ملف المشروع غير موجود: {fullProjectPath}");
                    continue;
                }
                
                // تحميل المراجع من المشروع الفرعي أولاً (recursive)
                LoadProjectReferencesRecursive(fullProjectPath, references, processedProjects);
                
                // ثم إضافة DLL الخاص بهذا المشروع
                var dllPath = ResolveProjectReferenceToDll(projectRef, projectDir);
                if (dllPath != null && File.Exists(dllPath) && !references.Contains(dllPath))
                {
                    references.Add(dllPath);
                    Console.WriteLine($"[ProjectReferenceLoader] ✓ تم إضافة: {Path.GetFileName(dllPath)}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProjectReferenceLoader] خطأ في معالجة {Path.GetFileName(projectFile)}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Search for .csproj file in current folder or parent folders
    /// </summary>
    private static string FindProjectFile(string xamlFilePath)
    {
        var dir = new DirectoryInfo(Path.GetDirectoryName(xamlFilePath));
        
        while (dir != null)
        {
            var csprojFiles = dir.GetFiles("*.csproj");
            if (csprojFiles.Length > 0)
            {
                return csprojFiles[0].FullName;
            }
            
            dir = dir.Parent;
        }
        
        return null;
    }
    
    /// <summary>
    /// Convert ProjectReference path to DLL path
    /// </summary>
    private static string ResolveProjectReferenceToDll(string projectRefPath, string projectDir)
    {
        try
        {
            // تحويل المسار النسبي إلى مسار مطلق
            var fullProjectPath = Path.GetFullPath(Path.Combine(projectDir, projectRefPath));
            
            if (!File.Exists(fullProjectPath))
            {
                Console.WriteLine($"[ProjectReferenceLoader] ملف المشروع غير موجود: {fullProjectPath}");
                return null;
            }
            
            // قراءة اسم المشروع من ملف .csproj
            var projectName = Path.GetFileNameWithoutExtension(fullProjectPath);
            var projectRefDir = Path.GetDirectoryName(fullProjectPath);
            
            // البحث عن DLL في مجلدات bin (updated for .NET 10)
            var possiblePaths = new[]
            {
                Path.Combine(projectRefDir, "bin", "Debug", "net10.0", $"{projectName}.dll"),
                Path.Combine(projectRefDir, "bin", "Debug", "net8.0-windows", $"{projectName}.dll"),
                Path.Combine(projectRefDir, "bin", "Debug", "net8.0", $"{projectName}.dll"),
                Path.Combine(projectRefDir, "bin", "Debug", "net7.0-windows", $"{projectName}.dll"),
                Path.Combine(projectRefDir, "bin", "Debug", "net7.0", $"{projectName}.dll"),
                Path.Combine(projectRefDir, "bin", "Debug", "net6.0-windows", $"{projectName}.dll"),
                Path.Combine(projectRefDir, "bin", "Debug", "net6.0", $"{projectName}.dll"),
                Path.Combine(projectRefDir, "bin", "Release", "net10.0", $"{projectName}.dll"),
                Path.Combine(projectRefDir, "bin", "Release", "net8.0-windows", $"{projectName}.dll"),
                Path.Combine(projectRefDir, "bin", "Release", "net8.0", $"{projectName}.dll"),
                Path.Combine(projectRefDir, "bin", "Release", "net7.0-windows", $"{projectName}.dll"),
                Path.Combine(projectRefDir, "bin", "Release", "net7.0", $"{projectName}.dll"),
                Path.Combine(projectRefDir, "bin", "Release", "net6.0-windows", $"{projectName}.dll"),
                Path.Combine(projectRefDir, "bin", "Release", "net6.0", $"{projectName}.dll")
            };
            
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }
            
            Console.WriteLine($"[ProjectReferenceLoader] لم يتم العثور على DLL لـ: {projectName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProjectReferenceLoader] خطأ في تحويل المسار: {ex.Message}");
        }
        
        return null;
    }
}