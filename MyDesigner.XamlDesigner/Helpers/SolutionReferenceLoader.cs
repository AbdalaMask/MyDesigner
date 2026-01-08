using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MyDesigner.XamlDesigner.Helpers;

/// <summary>
/// Helper class for loading all references from Solution file
/// </summary>
public static class SolutionReferenceLoader
{
    /// <summary>
    /// Load all references from .sln file
    /// </summary>
    public static List<string> LoadSolutionReferences(string solutionPath)
    {
        var references = new List<string>();
        var processedProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        try
        {
            if (!File.Exists(solutionPath))
            {
                Console.WriteLine($"[SolutionReferenceLoader] ملف Solution غير موجود: {solutionPath}");
                return references;
            }
            
            Console.WriteLine($"[SolutionReferenceLoader] قراءة Solution: {Path.GetFileName(solutionPath)}");
            
            // قراءة ملف Solution
            var solutionContent = File.ReadAllText(solutionPath);
            var solutionDir = Path.GetDirectoryName(solutionPath);
            
            // استخراج مسارات المشاريع من ملف Solution
            // نمط: Project("{...}") = "ProjectName", "Path\To\Project.csproj", "{...}"
            var projectPattern = @"Project\(""\{[^}]+\}""\)\s*=\s*""[^""]+"",\s*""([^""]+\.csproj)""";
            var matches = Regex.Matches(solutionContent, projectPattern);
            
            Console.WriteLine($"[SolutionReferenceLoader] تم العثور على {matches.Count} مشروع في Solution");
            
            foreach (Match match in matches)
            {
                var projectRelativePath = match.Groups[1].Value;
                var projectFullPath = Path.GetFullPath(Path.Combine(solutionDir, projectRelativePath));
                
                if (File.Exists(projectFullPath))
                {
                    Console.WriteLine($"[SolutionReferenceLoader] معالجة مشروع: {Path.GetFileName(projectFullPath)}");
                    
                    // تحميل المراجع من هذا المشروع
                    ProjectReferenceLoader.LoadProjectReferencesRecursivePublic(
                        projectFullPath, 
                        references, 
                        processedProjects
                    );
                }
                else
                {
                    Console.WriteLine($"[SolutionReferenceLoader] مشروع غير موجود: {projectFullPath}");
                }
            }
            
            Console.WriteLine($"[SolutionReferenceLoader] ========================================");
            Console.WriteLine($"[SolutionReferenceLoader] إجمالي المراجع: {references.Count}");
            Console.WriteLine($"[SolutionReferenceLoader] ========================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SolutionReferenceLoader] خطأ: {ex.Message}");
        }
        
        return references;
    }
    
    /// <summary>
    /// Search for .sln file in specific folder
    /// </summary>
    public static string FindSolutionFile(string directory)
    {
        try
        {
            var dir = new DirectoryInfo(directory);
            
            while (dir != null)
            {
                var slnFiles = dir.GetFiles("*.sln");
                if (slnFiles.Length > 0)
                {
                    return slnFiles[0].FullName;
                }
                
                dir = dir.Parent;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SolutionReferenceLoader] خطأ في البحث عن Solution: {ex.Message}");
        }
        
        return null;
    }
}