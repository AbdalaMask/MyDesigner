using Avalonia;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MyDesigner.Common.Controls;

/// <summary>
/// مزود IntelliSense بسيط وسريع
/// </summary>
public class SimpleIntelliSenseProvider
{
    private static readonly Dictionary<string, Type> _commonTypes = new();

    static SimpleIntelliSenseProvider()
    {
        // تحميل الأنواع الشائعة
        LoadCommonTypes();
    }

    private static void LoadCommonTypes()
    {
        var types = new[]
        {
            typeof(string), typeof(int), typeof(double), typeof(bool), typeof(object),
            typeof(List<>), typeof(Dictionary<,>), typeof(Array), typeof(Console),
            typeof(DateTime), typeof(TimeSpan), typeof(Guid), typeof(Exception),
            typeof(System.Text.StringBuilder), typeof(System.IO.File), typeof(System.IO.Directory),
            typeof(System.Linq.Enumerable), typeof(System.Threading.Tasks.Task)
        };

        foreach (var type in types)
        {
            _commonTypes[type.Name] = type;
        }

        // إضافة WPF types
        try
        {
            var wpfTypes = new[]
            {
                typeof(Window),
                typeof(Button),
                typeof(TextBox),
                typeof(Grid),
               
                typeof(Application)
            };

            foreach (var type in wpfTypes)
            {
                _commonTypes[type.Name] = type;
            }
        }
        catch { }
    }

    public List<CompletionItem> GetCompletions(string code, int position)
    {
        var completions = new List<CompletionItem>();

        try
        {
            // الحصول على السياق قبل المؤشر
            var textBeforeCursor = code.Substring(0, Math.Min(position, code.Length));
            
            // التحقق من وجود نقطة قبل المؤشر مباشرة
            if (textBeforeCursor.EndsWith("."))
            {
                // محاولة الحصول على النوع قبل النقطة
                var match = Regex.Match(textBeforeCursor, @"(\w+)\.$");
                if (match.Success)
                {
                    var identifier = match.Groups[1].Value;
                    completions.AddRange(GetMembersForIdentifier(identifier, code));
                }
            }
            else
            {
                // عرض الكلمات المفتاحية والأنواع الشائعة
                completions.AddRange(GetKeywords());
                completions.AddRange(GetCommonTypes());
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SimpleIntelliSense] خطأ: {ex.Message}");
        }

        return completions;
    }

    private List<CompletionItem> GetMembersForIdentifier(string identifier, string code)
    {
        var members = new List<CompletionItem>();

        try
        {
            // محاولة تحديد نوع المتغير
            Type? type = null;

            // البحث عن تعريف المتغير في الكود
            var varPattern = $@"(?:var|string|int|double|bool|object|List<\w+>|Dictionary<\w+,\s*\w+>)\s+{identifier}\s*=";
            var varMatch = Regex.Match(code, varPattern);
            
            if (varMatch.Success)
            {
                var typeStr = varMatch.Value.Split(' ')[0];
                
                // تحديد النوع
                type = typeStr switch
                {
                    "string" => typeof(string),
                    "int" => typeof(int),
                    "double" => typeof(double),
                    "bool" => typeof(bool),
                    "object" => typeof(object),
                    _ => _commonTypes.ContainsKey(typeStr) ? _commonTypes[typeStr] : null
                };
            }

            // إذا لم نجد النوع، جرب الأنواع الشائعة
            if (type == null && _commonTypes.ContainsKey(identifier))
            {
                type = _commonTypes[identifier];
            }

            // إذا وجدنا النوع، احصل على أعضائه
            if (type != null)
            {
                // الحصول على Methods
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Where(m => !m.IsSpecialName)
                    .Take(50);

                foreach (var method in methods)
                {
                    var parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    members.Add(new CompletionItem
                    {
                        Text = method.Name,
                        DisplayText = $"{method.Name}({parameters})",
                        Description = $"Method: {method.ReturnType.Name} {method.Name}",
                        Kind = CompletionKind.Method
                    });
                }

                // الحصول على Properties
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Take(50);

                foreach (var prop in properties)
                {
                    members.Add(new CompletionItem
                    {
                        Text = prop.Name,
                        DisplayText = prop.Name,
                        Description = $"Property: {prop.PropertyType.Name} {prop.Name}",
                        Kind = CompletionKind.Property
                    });
                }

                // الحصول على Fields
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Take(20);

                foreach (var field in fields)
                {
                    members.Add(new CompletionItem
                    {
                        Text = field.Name,
                        DisplayText = field.Name,
                        Description = $"Field: {field.FieldType.Name} {field.Name}",
                        Kind = CompletionKind.Field
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SimpleIntelliSense] خطأ في GetMembers: {ex.Message}");
        }

        return members.OrderBy(m => m.Text).ToList();
    }

    private List<CompletionItem> GetKeywords()
    {
        var keywords = new[]
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this",
            "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
            "using", "virtual", "void", "volatile", "while", "var", "async", "await", "record"
        };

        return keywords.Select(k => new CompletionItem
        {
            Text = k,
            DisplayText = k,
            Description = "Keyword",
            Kind = CompletionKind.Keyword
        }).ToList();
    }

    private List<CompletionItem> GetCommonTypes()
    {  
        return _commonTypes.Select(kvp => new CompletionItem
        {
            Text = kvp.Key,
            DisplayText = kvp.Key,
            Description = $"Type: {kvp.Value.FullName}",
            Kind = CompletionKind.Class
        }).ToList();
    }
}

public class CompletionItem
{
    public string Text { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CompletionKind Kind { get; set; }
}

public enum CompletionKind
{
    Keyword,
    Class,
    Method,
    Property,
    Field,
    Variable
}
