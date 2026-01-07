using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MyDesigner.XamlDesigner.Helpers;

/// <summary>
/// مساعد لربط Events بالـ Code-Behind في Avalonia
/// </summary>
public class EventBindingHelper
{
    /// <summary>
    /// إنشاء Event Handler في ملف code-behind
    /// </summary>
    public static (bool success, string filePath, int lineNumber) CreateEventHandler(
        string axamlFilePath,
        string controlName,
        string eventName,
        string handlerName = null)
    {
        try
        {
            // تحديد اسم Handler إذا لم يتم تحديده
            if (string.IsNullOrEmpty(handlerName))
            {
                handlerName = string.IsNullOrEmpty(controlName)
                    ? $"{eventName}_Handler"
                    : $"{controlName}_{eventName}";
            }

            // البحث عن ملف code-behind
            var csFilePath = axamlFilePath + ".cs";
            if (!File.Exists(csFilePath))
            {
                Console.WriteLine($"ملف code-behind غير موجود: {csFilePath}");
                return (false, null, 0);
            }

            // قراءة محتوى الملف
            var content = File.ReadAllText(csFilePath);

            // التحقق من وجود Handler مسبقاً
            if (content.Contains($"void {handlerName}("))
            {
                Console.WriteLine($"Event Handler موجود مسبقاً: {handlerName}");

                // البحث عن رقم السطر
                var lines = content.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains($"void {handlerName}("))
                    {
                        return (true, csFilePath, i + 1);
                    }
                }

                return (true, csFilePath, 1);
            }

            // استخراج معلومات الـ class
            var classInfo = ExtractClassInfo(content);
            if (classInfo == null)
            {
                Console.WriteLine("فشل في استخراج معلومات الـ class");
                return (false, null, 0);
            }

            // إنشاء Event Handler
            var eventHandler = GenerateEventHandler(handlerName, eventName);

            // إدراج Handler في نهاية الـ class
            var insertPosition = FindInsertPosition(content, classInfo.Value.closingBracePosition);
            var newContent = content.Insert(insertPosition, eventHandler);

            // حفظ الملف
            File.WriteAllText(csFilePath, newContent);

            // حساب رقم السطر الجديد
            var lineNumber = content.Substring(0, insertPosition).Count(c => c == '\n') + 2;

            Console.WriteLine($"✓ تم إنشاء Event Handler: {handlerName} في السطر {lineNumber}");
            return (true, csFilePath, lineNumber);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في إنشاء Event Handler: {ex.Message}");
            return (false, null, 0);
        }
    }

    /// <summary>
    /// استخراج معلومات الـ class من الكود
    /// </summary>
    private static (int closingBracePosition, string className)? ExtractClassInfo(string content)
    {
        // البحث عن class definition
        var classMatch = Regex.Match(content, @"class\s+(\w+)\s*(?::\s*\w+)?");
        if (!classMatch.Success)
            return null;

        var className = classMatch.Groups[1].Value;
        var classStartIndex = classMatch.Index;

        // البحث عن opening brace للـ class
        var openBraceIndex = content.IndexOf('{', classStartIndex);
        if (openBraceIndex == -1)
            return null;

        // البحث عن closing brace المطابق باستخدام عداد
        int braceCount = 1;
        int closingBraceIndex = openBraceIndex + 1;

        while (closingBraceIndex < content.Length && braceCount > 0)
        {
            if (content[closingBraceIndex] == '{')
                braceCount++;
            else if (content[closingBraceIndex] == '}')
                braceCount--;

            if (braceCount == 0)
                break;

            closingBraceIndex++;
        }

        if (braceCount != 0)
            return null;

        return (closingBraceIndex, className);
    }

    /// <summary>
    /// إيجاد موضع الإدراج المناسب
    /// </summary>
    private static int FindInsertPosition(string content, int closingBracePosition)
    {
        // البحث عن آخر method قبل closing brace
        var beforeBrace = content.Substring(0, closingBracePosition);

        // البحث عن آخر } لـ method (وليس للـ class)
        var lastMethodEnd = beforeBrace.LastIndexOf('}');

        // التأكد من أن هذه ليست closing brace للـ class نفسها
        if (lastMethodEnd != -1 && lastMethodEnd < closingBracePosition - 10)
        {
            // التحقق من وجود سطر جديد بعد }
            var afterBrace = lastMethodEnd + 1;
            while (afterBrace < content.Length && (content[afterBrace] == '\r' || content[afterBrace] == '\n'))
            {
                afterBrace++;
            }
            return afterBrace;
        }

        // إذا لم يتم العثور على methods، أدرج قبل closing brace للـ class مباشرة
        var lineStart = closingBracePosition;
        while (lineStart > 0 && content[lineStart - 1] != '\n')
        {
            lineStart--;
        }
        return lineStart;
    }

    /// <summary>
    /// توليد كود Event Handler لـ Avalonia
    /// </summary>
    private static string GenerateEventHandler(string handlerName, string eventName)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Event handler for {eventName}");
        sb.AppendLine("    /// </summary>");

        // تحديد signature بناءً على نوع Event في Avalonia
        var signature = GetAvaloniaEventSignature(eventName);
        sb.AppendLine($"    private void {handlerName}({signature})");
        sb.AppendLine("    {");
        sb.AppendLine($"        // TODO: Implement {eventName} logic");
        sb.AppendLine("    }");

        return sb.ToString();
    }

    /// <summary>
    /// الحصول على signature المناسب للـ Event في Avalonia
    /// </summary>
    private static string GetAvaloniaEventSignature(string eventName)
    {
        // Events شائعة في Avalonia
        var avaloniaEvents = new Dictionary<string, string>
        {
            { "Click", "object sender, Avalonia.Interactivity.RoutedEventArgs e" },
            { "Loaded", "object sender, Avalonia.Interactivity.RoutedEventArgs e" },
            { "Unloaded", "object sender, Avalonia.Interactivity.RoutedEventArgs e" },
            { "PointerEntered", "object sender, Avalonia.Input.PointerEventArgs e" },
            { "PointerExited", "object sender, Avalonia.Input.PointerEventArgs e" },
            { "PointerPressed", "object sender, Avalonia.Input.PointerPressedEventArgs e" },
            { "PointerReleased", "object sender, Avalonia.Input.PointerReleasedEventArgs e" },
            { "PointerMoved", "object sender, Avalonia.Input.PointerEventArgs e" },
            { "KeyDown", "object sender, Avalonia.Input.KeyEventArgs e" },
            { "KeyUp", "object sender, Avalonia.Input.KeyEventArgs e" },
            { "TextChanged", "object sender, Avalonia.Controls.TextChangedEventArgs e" },
            { "SelectionChanged", "object sender, Avalonia.Controls.SelectionChangedEventArgs e" },
            { "Checked", "object sender, Avalonia.Interactivity.RoutedEventArgs e" },
            { "Unchecked", "object sender, Avalonia.Interactivity.RoutedEventArgs e" },
            { "ValueChanged", "object sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e" },
            { "GotFocus", "object sender, Avalonia.Input.GotFocusEventArgs e" },
            { "LostFocus", "object sender, Avalonia.Interactivity.RoutedEventArgs e" },
            { "Tapped", "object sender, Avalonia.Input.TappedEventArgs e" },
            { "DoubleTapped", "object sender, Avalonia.Input.TappedEventArgs e" },
            { "PropertyChanged", "object sender, Avalonia.AvaloniaPropertyChangedEventArgs e" }
        };

        return avaloniaEvents.TryGetValue(eventName, out var signature)
            ? signature
            : "object sender, EventArgs e";
    }

    /// <summary>
    /// تحديث AXAML لإضافة Event Handler
    /// </summary>
    public static bool UpdateAxamlWithEvent(string axamlContent, string controlName, string eventName, string handlerName)
    {
        try
        {
            // البحث عن Control في AXAML
            var pattern = $@"<(\w+)[^>]*x:Name\s*=\s*""{controlName}""[^>]*>";
            var match = Regex.Match(axamlContent, pattern);

            if (!match.Success)
            {
                Console.WriteLine($"لم يتم العثور على Control: {controlName}");
                return false;
            }

            // التحقق من وجود Event مسبقاً
            if (match.Value.Contains($"{eventName}="))
            {
                Console.WriteLine($"Event موجود مسبقاً في AXAML: {eventName}");
                return true;
            }

            // إضافة Event إلى Control
            var updatedTag = match.Value.TrimEnd('>') + $" {eventName}=\"{handlerName}\">";
            var updatedAxaml = axamlContent.Replace(match.Value, updatedTag);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في تحديث AXAML: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// الحصول على قائمة Events المتاحة لـ Control في Avalonia
    /// </summary>
    public static List<EventInfo> GetAvailableAvaloniaEvents(Type controlType)
    {
        var events = new List<EventInfo>();

        try
        {
            var allEvents = controlType.GetEvents(BindingFlags.Public | BindingFlags.Instance);

            // تصفية Events الشائعة في Avalonia
            var commonAvaloniaEventNames = new[]
            {
                "Click", "PointerPressed", "PointerReleased", "PointerEntered", "PointerExited", "PointerMoved",
                "KeyDown", "KeyUp", "Loaded", "Unloaded", "AttachedToVisualTree", "DetachedFromVisualTree",
                "TextChanged", "SelectionChanged", "Checked", "Unchecked",
                "ValueChanged", "GotFocus", "LostFocus", "Tapped", "DoubleTapped",
                "PropertyChanged", "DataContextChanged"
            };

            events = allEvents
                .Where(e => commonAvaloniaEventNames.Contains(e.Name))
                .OrderBy(e => e.Name)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في الحصول على Events: {ex.Message}");
        }

        return events;
    }

    /// <summary>
    /// تحويل WPF Event إلى Avalonia Event المقابل
    /// </summary>
    public static string ConvertWpfEventToAvalonia(string wpfEventName)
    {
        var eventMapping = new Dictionary<string, string>
        {
            { "MouseEnter", "PointerEntered" },
            { "MouseLeave", "PointerExited" },
            { "MouseDown", "PointerPressed" },
            { "MouseUp", "PointerReleased" },
            { "MouseMove", "PointerMoved" },
            { "PreviewMouseDown", "PointerPressed" }, // Avalonia doesn't have Preview events
            { "PreviewMouseUp", "PointerReleased" },
            { "PreviewKeyDown", "KeyDown" },
            { "PreviewKeyUp", "KeyUp" }
        };

        return eventMapping.TryGetValue(wpfEventName, out var avaloniaEvent) ? avaloniaEvent : wpfEventName;
    }
}