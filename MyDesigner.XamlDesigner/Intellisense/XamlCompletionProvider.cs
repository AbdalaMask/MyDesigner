using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using MyDesigner.XamlDesigner.Helpers;
using MyDesigner.XamlDom;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;


namespace MyDesigner.XamlDesigner.Intellisense
{
    /// <summary>
    /// مزود IntelliSense للـ XAML
    /// </summary>
    public class XamlCompletionProvider
    {
        private readonly XamlTypeFinder _typeFinder;
        private readonly Dictionary<string, Type> _xamlTypes;
        private readonly Dictionary<string, string[]> _commonProperties;
        private readonly HashSet<Assembly> _externalAssemblies = new();

        public XamlCompletionProvider(XamlTypeFinder typeFinder)
        {
            _typeFinder = typeFinder;
            _xamlTypes = new Dictionary<string, Type>();
            _commonProperties = new Dictionary<string, string[]>();
            
            InitializeXamlTypes();
            InitializeCommonProperties();
        }

        private void InitializeXamlTypes()
        {
            try
            {
                // Avalonia Controls الأساسية
                var avaloniaTypes = new[]
                {
                    typeof(Window), typeof(UserControl), typeof(ContentControl),
                    typeof(Grid), typeof(StackPanel), typeof(Canvas), typeof(DockPanel), typeof(WrapPanel),
                    typeof(Button), typeof(TextBox), typeof(TextBlock), typeof(Label),
                    typeof(ComboBox), typeof(ListBox),  typeof(TreeView),
                    typeof(CheckBox), typeof(RadioButton), typeof(Slider), typeof(ProgressBar),
                    typeof(Image), typeof(Border), typeof(Rectangle), typeof(Ellipse),
                    typeof(Menu), typeof(MenuItem),
                    typeof(TabControl), typeof(TabItem), typeof(Expander),
                    typeof(ScrollViewer), typeof(Viewbox)
                };

                foreach (var type in avaloniaTypes)
                {
                    _xamlTypes[type.Name] = type;
                }

                // إضافة أنواع إضافية من التجميعات المحملة
                LoadAdditionalTypes();
               
                // إضافة تجميعات HMI إذا كانت متوفرة
                try
                {
                    var hmiCore = Assembly.Load("HMIControl.Core");
                    AddAssembly(hmiCore);
                }
                catch { }

                try
                {
                    var hmiHmi = Assembly.Load("HMIControl.HMI");
                    AddAssembly(hmiHmi);
                }
                catch { }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تهيئة أنواع XAML: {ex.Message}");
            }
        }

        private void LoadAdditionalTypes()
        {
            try
            {
                if (_typeFinder != null)
                {
                    // استخدام MyTypeFinder للحصول على أنواع إضافية من التجميعات المسجلة
                    foreach (var assembly in _typeFinder.RegisteredAssemblies.Take(10))
                    {
                        try
                        {
                            var types = assembly.GetExportedTypes()
                                .Where(t => t.IsPublic && !t.IsAbstract && !t.IsInterface)
                                .Take(50);
                            
                            foreach (var type in types)
                            {
                                if (!_xamlTypes.ContainsKey(type.Name))
                                {
                                    _xamlTypes[type.Name] = type;
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // تجاهل الأخطاء في تحميل أنواع التجميع
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تحميل الأنواع الإضافية: {ex.Message}");
            }
        }

        private void InitializeCommonProperties()
        {
            _commonProperties["StyledElement"] = new[]
            {
                "Name", "Width", "Height", "MinWidth", "MinHeight", "MaxWidth", "MaxHeight",
                "Margin", "HorizontalAlignment", "VerticalAlignment",
                "IsVisible", "IsEnabled", "Opacity", "Background", "Foreground",
                "FontFamily", "FontSize", "FontWeight", "FontStyle"
            };

            _commonProperties["Control"] = new[]
            {
                "Template", "BorderBrush", "BorderThickness",
                "Focusable", "IsTabStop", "TabIndex", "Padding"
            };

            _commonProperties["Panel"] = new[]
            {
                "Children", "Background"
            };

            _commonProperties["Grid"] = new[]
            {
                "RowDefinitions", "ColumnDefinitions", "ShowGridLines"
            };

            _commonProperties["StackPanel"] = new[]
            {
                "Orientation"
            };

            _commonProperties["Button"] = new[]
            {
                "Content", "Click", "Command", "CommandParameter", "IsDefault", "IsCancel"
            };

            _commonProperties["TextBox"] = new[]
            {
                "Text", "TextChanged", "AcceptsReturn", "AcceptsTab", "IsReadOnly",
                "MaxLength", "TextWrapping"
            };
        }

        public IList<ICompletionData> GetCompletionData(string text, int offset)
        {
            var completions = new List<ICompletionData>();

            try
            {
                var context = AnalyzeContext(text, offset);
                
                switch (context.Type)
                {
                    case XamlContextType.ElementName:
                        completions.AddRange(GetElementCompletions(context));
                        break;
                    case XamlContextType.AttributeName:
                        completions.AddRange(GetAttributeCompletions(context));
                        break;
                    case XamlContextType.AttributeValue:
                        completions.AddRange(GetAttributeValueCompletions(context));
                        break;
                    case XamlContextType.MarkupExtension:
                        completions.AddRange(GetMarkupExtensionCompletions(context));
                        break;
                    default:
                        completions.AddRange(GetElementCompletions(context));
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في الحصول على اقتراحات XAML: {ex.Message}");
            }

            return completions.OrderBy(c => c.Text).ToList();
        }

        private XamlContext AnalyzeContext(string xaml, int position)
        {
            var context = new XamlContext();
            
            if (position > xaml.Length) position = xaml.Length;
            
            var textBeforePosition = xaml.Substring(0, position);

            // تحليل السياق
            if (IsInElementName(textBeforePosition))
            {
                context.Type = XamlContextType.ElementName;
                context.Prefix = ExtractElementPrefix(textBeforePosition);
            }
            else if (IsInAttributeName(textBeforePosition))
            {
                context.Type = XamlContextType.AttributeName;
                context.ElementName = ExtractCurrentElementName(textBeforePosition);
                context.Prefix = ExtractAttributePrefix(textBeforePosition);
            }
            else if (IsInAttributeValue(textBeforePosition))
            {
                context.Type = XamlContextType.AttributeValue;
                context.ElementName = ExtractCurrentElementName(textBeforePosition);
                context.AttributeName = ExtractCurrentAttributeName(textBeforePosition);
            }
            else if (IsInMarkupExtension(textBeforePosition))
            {
                context.Type = XamlContextType.MarkupExtension;
                context.MarkupExtensionName = ExtractMarkupExtensionName(textBeforePosition);
            }

            return context;
        }

        private bool IsInElementName(string text)
        {
            var match = Regex.Match(text, @"<\s*([a-zA-Z_][\w\.:]*)?$");
            return match.Success;
        }

        private bool IsInAttributeName(string text)
        {
            var match = Regex.Match(text, @"<\s*[a-zA-Z_][\w\.:]*[^>]*\s+([a-zA-Z_][\w\.:]*)?$");
            return match.Success;
        }

        private bool IsInAttributeValue(string text)
        {
            var match = Regex.Match(text, @"[a-zA-Z_][\w\.:]*\s*=\s*[""'][^""']*$");
            return match.Success;
        }

        private bool IsInMarkupExtension(string text)
        {
            var match = Regex.Match(text, @"\{\s*([a-zA-Z_][\w\.:]*)?[^}]*$");
            return match.Success;
        }

        private string ExtractElementPrefix(string text)
        {
            var match = Regex.Match(text, @"<\s*([a-zA-Z_][\w\.:]*)?$");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private string ExtractAttributePrefix(string text)
        {
            var match = Regex.Match(text, @"\s+([a-zA-Z_][\w\.:]*)?$");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private string ExtractCurrentElementName(string text)
        {
            var match = Regex.Match(text, @"<\s*([a-zA-Z_][\w\.:]*)", RegexOptions.RightToLeft);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private string ExtractCurrentAttributeName(string text)
        {
            var match = Regex.Match(text, @"([a-zA-Z_][\w\.:]*)\s*=\s*[""'][^""']*$", RegexOptions.RightToLeft);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private string ExtractMarkupExtensionName(string text)
        {
            var match = Regex.Match(text, @"\{\s*([a-zA-Z_][\w\.:]*)", RegexOptions.RightToLeft);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private List<ICompletionData> GetElementCompletions(XamlContext context)
        {
            var completions = new List<ICompletionData>();
            var prefix = context.Prefix ?? string.Empty;

            foreach (var kvp in _xamlTypes)
            {
                if (string.IsNullOrEmpty(prefix) || kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    completions.Add(new XamlCompletionData(kvp.Key, $"Element: {kvp.Value.FullName}", CompletionKind.Class));
                }
            }

            return completions;
        }

        private List<ICompletionData> GetAttributeCompletions(XamlContext context)
        {
            var completions = new List<ICompletionData>();
            var prefix = context.Prefix ?? string.Empty;

            string elementName = context.ElementName;
            if (!string.IsNullOrEmpty(elementName) && elementName.Contains(":"))
            {
                elementName = elementName.Substring(elementName.IndexOf(":") + 1);
            }

            if (!string.IsNullOrEmpty(elementName) && _xamlTypes.ContainsKey(elementName))
            {
                var elementType = _xamlTypes[elementName];
                
                // الحصول على خصائص العنصر
                var properties = elementType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite)
                    .Take(50);

                foreach (var prop in properties)
                {
                    if (string.IsNullOrEmpty(prefix) || prop.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        completions.Add(new XamlCompletionData(prop.Name, $"Property: {prop.PropertyType.Name} {prop.Name}", CompletionKind.Property));
                    }
                }

                // إضافة الأحداث
                var events = elementType.GetEvents(BindingFlags.Public | BindingFlags.Instance)
                    .Take(30);

                foreach (var evt in events)
                {
                    if (string.IsNullOrEmpty(prefix) || evt.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        completions.Add(new XamlCompletionData(evt.Name, $"Event: {evt.Name}", CompletionKind.Method));
                    }
                }

                // إضافة الخصائص الشائعة
                AddCommonProperties(completions, elementType, prefix);
            }

            return completions;
        }

        private void AddCommonProperties(List<ICompletionData> completions, Type elementType, string prefix)
        {
            var typeHierarchy = new[] { elementType.Name, elementType.BaseType?.Name, "StyledElement", "Control" };
            
            foreach (var typeName in typeHierarchy)
            {
                if (!string.IsNullOrEmpty(typeName) && _commonProperties.ContainsKey(typeName))
                {
                    foreach (var prop in _commonProperties[typeName])
                    {
                        if ((string.IsNullOrEmpty(prefix) || prop.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) &&
                            !completions.Any(c => c.Text == prop))
                        {
                            completions.Add(new XamlCompletionData(prop, $"Common Property: {prop}", CompletionKind.Property));
                        }
                    }
                }
            }
        }

        private List<ICompletionData> GetAttributeValueCompletions(XamlContext context)
        {
            var completions = new List<ICompletionData>();

            if (!string.IsNullOrEmpty(context.AttributeName))
            {
                switch (context.AttributeName.ToLower())
                {
                    case "horizontalalignment":
                        completions.AddRange(GetEnumCompletions(typeof(HorizontalAlignment)));
                        break;
                    case "verticalalignment":
                        completions.AddRange(GetEnumCompletions(typeof(VerticalAlignment)));
                        break;
                    case "isvisible":
                        completions.Add(new XamlCompletionData("True", "Boolean: True", CompletionKind.Property));
                        completions.Add(new XamlCompletionData("False", "Boolean: False", CompletionKind.Property));
                        break;
                    case "orientation":
                        completions.AddRange(GetEnumCompletions(typeof(Orientation)));
                        break;
                    case "stretch":
                        completions.AddRange(GetEnumCompletions(typeof(Stretch)));
                        break;
                    case "fontweight":
                        var fontWeights = new[] { "Normal", "Bold", "Light", "SemiBold", "ExtraBold", "Black" };
                        foreach (var weight in fontWeights)
                        {
                            completions.Add(new XamlCompletionData(weight, $"FontWeight: {weight}", CompletionKind.Property));
                        }
                        break;
                    case "fontstyle":
                        var fontStyles = new[] { "Normal", "Italic", "Oblique" };
                        foreach (var style in fontStyles)
                        {
                            completions.Add(new XamlCompletionData(style, $"FontStyle: {style}", CompletionKind.Property));
                        }
                        break;
                    default:
                        completions.AddRange(GetCommonValueCompletions(context.AttributeName));
                        break;
                }
            }

            return completions;
        }

        private List<ICompletionData> GetEnumCompletions(Type enumType)
        {
            var completions = new List<ICompletionData>();
            
            if (enumType.IsEnum)
            {
                foreach (var value in Enum.GetNames(enumType))
                {
                    completions.Add(new XamlCompletionData(value, $"{enumType.Name}: {value}", CompletionKind.Property));
                }
            }

            return completions;
        }

        private List<ICompletionData> GetCommonValueCompletions(string attributeName)
        {
            var completions = new List<ICompletionData>();

            switch (attributeName.ToLower())
            {
                case "background":
                case "foreground":
                case "borderbrush":
                    completions.AddRange(GetColorCompletions());
                    break;
                case "width":
                case "height":
                case "minwidth":
                case "minheight":
                case "maxwidth":
                case "maxheight":
                    var sizes = new[] { "Auto", "100", "200", "*" };
                    foreach (var size in sizes)
                    {
                        completions.Add(new XamlCompletionData(size, $"Size: {size}", CompletionKind.Property));
                    }
                    break;
                case "margin":
                case "padding":
                    var thicknesses = new[] { "0", "5", "10", "0,5", "5,10", "10,5,10,5" };
                    foreach (var thickness in thicknesses)
                    {
                        completions.Add(new XamlCompletionData(thickness, $"Thickness: {thickness}", CompletionKind.Property));
                    }
                    break;
            }

            return completions;
        }

        private List<ICompletionData> GetColorCompletions()
        {
            var colors = new[]
            {
                "Transparent", "White", "Black", "Red", "Green", "Blue", "Yellow",
                "Orange", "Purple", "Pink", "Gray", "LightGray", "DarkGray",
                "Brown", "Cyan", "Magenta", "Lime", "Navy", "Maroon", "Olive"
            };

            return colors.Select(color => new XamlCompletionData(color, $"Color: {color}", CompletionKind.Property))
                        .Cast<ICompletionData>()
                        .ToList();
        }

        private List<ICompletionData> GetMarkupExtensionCompletions(XamlContext context)
        {
            var completions = new List<ICompletionData>();
            var prefix = context.MarkupExtensionName ?? string.Empty;

            var markupExtensions = new[]
            {
                "Binding", "StaticResource", "DynamicResource", "TemplateBinding",
                "RelativeSource", "x:Static", "x:Type", "x:Null"
            };

            foreach (var ext in markupExtensions)
            {
                if (string.IsNullOrEmpty(prefix) || ext.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    completions.Add(new XamlCompletionData(ext, $"Markup Extension: {ext}", CompletionKind.Method));
                }
            }

            return completions;
        }

        /// <summary>
        /// أضف تجميع خارجي (مثل مكتبات HMI أو أي مكتبة تحوي كنترولات)
        /// </summary>
        public void AddAssembly(Assembly assembly)
        {
            if (assembly == null || _externalAssemblies.Contains(assembly))
                return;
            _externalAssemblies.Add(assembly);
            try
            {
                var types = assembly.GetExportedTypes()
                    .Where(t => t.IsPublic && !t.IsAbstract && !t.IsInterface)
                    .Take(200);
                foreach (var type in types)
                {
                    if (!_xamlTypes.ContainsKey(type.Name))
                    {
                        _xamlTypes[type.Name] = type;
                    }
                }
            }
            catch { /* تجاهل أي خطأ في التحميل */ }
        }
    }

    public class XamlContext
    {
        public XamlContextType Type { get; set; }
        public string ElementName { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public string MarkupExtensionName { get; set; } = string.Empty;
    }

    public enum XamlContextType
    {
        ElementName,
        AttributeName,
        AttributeValue,
        MarkupExtension,
        Content
    }
}