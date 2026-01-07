using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;

namespace MyDesigner.XamlDesigner.Converters;

 /// <summary>
    /// Converter لعرض الكنترول بشكل مصغر
    /// </summary>
    public class ControlToThumbnailConverter : IValueConverter
    {
        // Cache للكنترولات المنشأة لتجنب إنشائها مرات متعددة
        private static readonly Dictionary<Type, Control> _cache = new Dictionary<Type, Control>();

        // قائمة بالكنترولات الآمنة التي يمكن إنشاؤها
        private static readonly HashSet<string> _safeControls = new HashSet<string>
        {
            "Button", "TextBox", "TextBlock", "Label", "CheckBox", "RadioButton",
            "ComboBox", "ListBox", "Slider", "ProgressBar", "Border", "Grid",
            "Canvas", "StackPanel", "WrapPanel", "DockPanel", "Image", "Rectangle",
            "Ellipse", "Line", "Path", "Polygon", "Polyline", "ScrollViewer",
            "TabControl", "TreeView", "DataGrid", "ListView", "Expander",
            "Separator", "ToolBar", "Menu", "ContextMenu", "PasswordBox",
            "Calendar", "DatePicker", "Frame", "Viewbox", "ScrollBar", "GridSplitter"
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Type controlType)
            {
                // التحقق من الـ Cache أولاً
                if (_cache.TryGetValue(controlType, out var cachedElement))
                {
                    return cachedElement;
                }

                Control result;

                // التحقق من أن الكنترول آمن للإنشاء
                if (!_safeControls.Contains(controlType.Name))
                {
                    // الكنترول غير موجود في القائمة الآمنة، نستخدم أيقونة افتراضية
                    result = CreateDefaultIcon(controlType);
                    _cache[controlType] = result;
                    return result;
                }

                try
                {
                    // محاولة إنشاء الكنترول الفعلي باستخدام Activator.CreateInstance
                    result = CreateRealControlThumbnail(controlType);
                    _cache[controlType] = result;
                    return result;
                }
                catch
                {
                    // أي خطأ، نعرض أيقونة افتراضية
                    result = CreateDefaultIcon(controlType);
                    _cache[controlType] = result;
                    return result;
                }
            }

            // إرجاع أيقونة افتراضية
            return CreateDefaultIcon(value as Type);
        }

        private Control CreateRealControlThumbnail(Type controlType)
        {
            // إنشاء instance حقيقي من الكنترول باستخدام Activator.CreateInstance
            var control = Activator.CreateInstance(controlType) as Control;

            if (control != null)
            {
                // تطبيق إعدادات أساسية
                control.IsHitTestVisible = false;
                control.Focusable = false;

                // إعدادات خاصة حسب نوع الكنترول
                if (control is Button button)
                {
                    button.Content = "Button";
                    button.MinWidth = 60;
                    button.MinHeight = 22;
                    button.Padding = new Thickness(8, 3, 8, 3);
                }
                else if (control is TextBox textBox)
                {
                    textBox.Text = "TextBox";
                    textBox.MinWidth = 60;
                    textBox.MinHeight = 20;
                    textBox.Padding = new Thickness(2);
                }
                else if (control is CheckBox checkBox)
                {
                    checkBox.Content = "CheckBox";
                    checkBox.MinWidth = 16;
                    checkBox.MinHeight = 16;
                }
                else if (control is RadioButton radioButton)
                {
                    radioButton.Content = "RadioButton";
                    radioButton.MinWidth = 16;
                    radioButton.MinHeight = 16;
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.MinWidth = 60;
                    comboBox.MinHeight = 22;
                    // TODO: Add items to ComboBox for Avalonia
                }
                else if (control is ListBox listBox)
                {
                    listBox.MinWidth = 60;
                    listBox.MinHeight = 60;
                    // TODO: Add items to ListBox for Avalonia
                }
                else if (control is Slider slider)
                {
                    slider.MinWidth = 60;
                    slider.MinHeight = 20;
                    slider.Value = 50;
                    slider.Minimum = 0;
                    slider.Maximum = 100;
                }
                else if (control is ProgressBar progressBar)
                {
                    progressBar.MinWidth = 60;
                    progressBar.MinHeight = 16;
                    progressBar.Value = 50;
                    progressBar.Minimum = 0;
                    progressBar.Maximum = 100;
                }
                else if (control is TextBlock textBlock)
                {
                    textBlock.Text = "TextBlock";
                    textBlock.MinWidth = 60;
                }
                else if (control is Label label)
                {
                    label.Content = "Label";
                    label.MinWidth = 60;
                }
                else if (control is Border border)
                {
                    border.MinWidth = 60;
                    border.MinHeight = 60;
                    border.BorderBrush = Brushes.Gray;
                    border.BorderThickness = new Thickness(1);
                }
                else if (control is Grid grid)
                {
                    grid.MinWidth = 60;
                    grid.MinHeight = 60;
                    grid.Background = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0));
                }
                else if (control is Canvas canvas)
                {
                    canvas.MinWidth = 60;
                    canvas.MinHeight = 60;
                    canvas.Background = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0));
                }
                else if (control is StackPanel stackPanel)
                {
                    stackPanel.MinWidth = 60;
                    stackPanel.MinHeight = 60;
                    stackPanel.Background = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0));
                }
                else if (control is Image image)
                {
                    image.MinWidth = 60;
                    image.MinHeight = 60;
                    image.Stretch = Stretch.None;
                }
                else
                {
                    // للكنترولات الأخرى، نطبق حجم افتراضي
                    control.MinWidth = 60;
                    control.MinHeight = 30;
                }

                // وضع الكنترول في Viewbox للتحجيم
                var viewbox = new Viewbox
                {
                    Width = 36,
                    Height = 36,
                    Stretch = Stretch.Uniform,
                    StretchDirection = StretchDirection.DownOnly,
                    Child = control
                };

                return viewbox;
            }

            // إذا فشل التحويل إلى Control، نرجع أيقونة افتراضية
            return CreateDefaultIcon(controlType);
        }

        private Control CreateDefaultIcon(Type controlType)
        {
            var typeName = controlType?.Name ?? "?";
            var initial = typeName.Length > 0 ? typeName.Substring(0, Math.Min(2, typeName.Length)) : "?";

            var border = new Border
            {
                Width = 36,
                Height = 36,
                Background = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                CornerRadius = new CornerRadius(4),
                Child = new TextBlock
                {
                    Text = initial,
                    FontSize = 14,
                    FontWeight = FontWeight.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
                }
            };
            return border;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }