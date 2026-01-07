using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DialogHostAvalonia;
using MyDesigner.XamlDesigner.Helpers;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MyDesigner.XamlDesigner;

public partial class PageSizeSettingsWindow : UserControl
{ 
   private bool _isUpdatingDimensions = false;
    private double _aspectRatio = 1.0;
    private readonly Dictionary<string, List<PageSizePreset>> _platformPresets;

    public PageSizeSettings? Result { get; private set; }

    public PageSizeSettingsWindow()
    {
        InitializeComponent();
        _platformPresets = InitializePresets();
        InitializeDefaults();
        UpdatePreview();
    }

    public PageSizeSettingsWindow(PageSizeSettings currentSettings) : this()
    {
        LoadSettings(currentSettings);
    }

    private Dictionary<string, List<PageSizePreset>> InitializePresets()
    {
        return new Dictionary<string, List<PageSizePreset>>
        {
            ["WPF"] = new List<PageSizePreset>
            {
                new("Desktop HD", 1920, 1080),
                new("Desktop FHD", 1366, 768),
                new("Desktop 4K", 3840, 2160),
                new("Tablet", 1024, 768),
                new("Small Window", 800, 600),
                new("Large Window", 1200, 900),
                new("Custom", 0, 0)
            },
            ["Avalonia"] = new List<PageSizePreset>
            {
                new("Cross-Platform Desktop", 1280, 720),
                new("Linux Desktop", 1920, 1080),
                new("macOS Window", 1440, 900),
                new("Windows Desktop", 1366, 768),
                new("Compact View", 800, 600),
                new("Wide Screen", 1600, 900),
                new("Custom", 0, 0)
            },
            ["Maui"] = new List<PageSizePreset>
            {
                new("iPhone 14", 390, 844),
                new("iPhone 14 Pro Max", 430, 932),
                new("Samsung Galaxy S23", 360, 780),
                new("iPad", 768, 1024),
                new("Android Tablet", 800, 1280),
                new("Desktop (Windows)", 1200, 800),
                new("Desktop (macOS)", 1440, 900),
                new("Custom", 0, 0)
            }
        };
    }

    private void InitializeDefaults()
    {
        var platformComboBox = this.FindControl<ComboBox>("PlatformComboBox");
        var portraitRadioButton = this.FindControl<RadioButton>("PortraitRadioButton");
        var maintainAspectRatioCheckBox = this.FindControl<CheckBox>("MaintainAspectRatioCheckBox");
        var widthTextBox = this.FindControl<TextBox>("WidthTextBox");
        var heightTextBox = this.FindControl<TextBox>("HeightTextBox");

        if (platformComboBox != null) platformComboBox.SelectedIndex = 0; // WPF by default
        if (portraitRadioButton != null) portraitRadioButton.IsChecked = true;
        if (maintainAspectRatioCheckBox != null) maintainAspectRatioCheckBox.IsChecked = true;

        // Set default dimensions
        if (widthTextBox != null) widthTextBox.Text = "800";
        if (heightTextBox != null) heightTextBox.Text = "600";

        UpdateAspectRatio();
    }

    private void LoadSettings(PageSizeSettings settings)
    {
        var platformComboBox = this.FindControl<ComboBox>("PlatformComboBox");
        var widthTextBox = this.FindControl<TextBox>("WidthTextBox");
        var heightTextBox = this.FindControl<TextBox>("HeightTextBox");
        var landscapeRadioButton = this.FindControl<RadioButton>("LandscapeRadioButton");
        var portraitRadioButton = this.FindControl<RadioButton>("PortraitRadioButton");

        // Set platform
        if (platformComboBox != null)
        {
            for (int i = 0; i < platformComboBox.Items.Count; i++)
            {
                if (platformComboBox.Items[i] is ComboBoxItem item &&
                    item.Content?.ToString()?.Contains(settings.Platform) == true)
                {
                    platformComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        // Set dimensions
        if (widthTextBox != null) widthTextBox.Text = settings.Width.ToString();
        if (heightTextBox != null) heightTextBox.Text = settings.Height.ToString();

        // Set orientation
        if (settings.Width > settings.Height)
        {
            if (landscapeRadioButton != null) landscapeRadioButton.IsChecked = true;
        }
        else
        {
            if (portraitRadioButton != null) portraitRadioButton.IsChecked = true;
        }

        UpdateAspectRatio();
        UpdatePreview();
    }

    private void PlatformComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var comboBox = sender as ComboBox;
        if (comboBox?.SelectedItem is ComboBoxItem selectedItem)
        {
            string platform = selectedItem.Content?.ToString()?.Split(' ')[0] ?? "WPF";
            UpdatePresetComboBox(platform);
            UpdatePreview();
        }
    }

    private void UpdatePresetComboBox(string platform)
    {
        var presetComboBox = this.FindControl<ComboBox>("PresetComboBox");
        if (presetComboBox == null) return;

        presetComboBox.Items.Clear();

        if (_platformPresets.ContainsKey(platform))
        {
            foreach (var preset in _platformPresets[platform])
            {
                var item = new ComboBoxItem
                {
                    Content = preset.Name,
                    Tag = preset
                };
                presetComboBox.Items.Add(item);
            }
        }

        presetComboBox.SelectedIndex = 0;
    }

    private void PresetComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // This will be handled by ApplyPresetButton_Click
    }

    private void ApplyPresetButton_Click(object? sender, RoutedEventArgs e)
    {
        var presetComboBox = this.FindControl<ComboBox>("PresetComboBox");
        var widthTextBox = this.FindControl<TextBox>("WidthTextBox");
        var heightTextBox = this.FindControl<TextBox>("HeightTextBox");

        if (presetComboBox?.SelectedItem is ComboBoxItem selectedItem &&
            selectedItem.Tag is PageSizePreset preset)
        {
            if (preset.Width > 0 && preset.Height > 0)
            {
                _isUpdatingDimensions = true;
                if (widthTextBox != null) widthTextBox.Text = preset.Width.ToString();
                if (heightTextBox != null) heightTextBox.Text = preset.Height.ToString();
                _isUpdatingDimensions = false;

                UpdateAspectRatio();
                UpdateOrientation();
                UpdatePreview();
            }
        }
    }

    private void DimensionTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isUpdatingDimensions) return;

        var textBox = sender as TextBox;
        if (textBox == null) return;

        // Validate numeric input
        if (!IsValidNumericInput(textBox.Text ?? ""))
        {
            // Remove invalid characters
            int caretIndex = textBox.CaretIndex;
            textBox.Text = Regex.Replace(textBox.Text ?? "", @"[^\d]", "");
            textBox.CaretIndex = Math.Min(caretIndex, textBox.Text?.Length ?? 0);
            return;
        }

        var maintainAspectRatioCheckBox = this.FindControl<CheckBox>("MaintainAspectRatioCheckBox");
        if (maintainAspectRatioCheckBox?.IsChecked == true)
        {
            MaintainAspectRatio(textBox);
        }

        UpdateOrientation();
        UpdatePreview();
    }

    private bool IsValidNumericInput(string input)
    {
        return string.IsNullOrEmpty(input) || Regex.IsMatch(input, @"^\d+$");
    }

    private void MaintainAspectRatio(TextBox changedTextBox)
    {
        if (_aspectRatio <= 0) return;

        var widthTextBox = this.FindControl<TextBox>("WidthTextBox");
        var heightTextBox = this.FindControl<TextBox>("HeightTextBox");

        _isUpdatingDimensions = true;

        try
        {
            if (changedTextBox == widthTextBox && double.TryParse(widthTextBox?.Text, out double width))
            {
                double newHeight = width / _aspectRatio;
                if (heightTextBox != null) heightTextBox.Text = ((int)Math.Round(newHeight)).ToString();
            }
            else if (changedTextBox == heightTextBox && double.TryParse(heightTextBox?.Text, out double height))
            {
                double newWidth = height * _aspectRatio;
                if (widthTextBox != null) widthTextBox.Text = ((int)Math.Round(newWidth)).ToString();
            }
        }
        finally
        {
            _isUpdatingDimensions = false;
        }
    }

    private void UpdateAspectRatio()
    {
        var widthTextBox = this.FindControl<TextBox>("WidthTextBox");
        var heightTextBox = this.FindControl<TextBox>("HeightTextBox");

        if (double.TryParse(widthTextBox?.Text, out double width) &&
            double.TryParse(heightTextBox?.Text, out double height) &&
            height > 0)
        {
            _aspectRatio = width / height;
        }
    }

    private void UpdateOrientation()
    {
        var widthTextBox = this.FindControl<TextBox>("WidthTextBox");
        var heightTextBox = this.FindControl<TextBox>("HeightTextBox");
        var landscapeRadioButton = this.FindControl<RadioButton>("LandscapeRadioButton");
        var portraitRadioButton = this.FindControl<RadioButton>("PortraitRadioButton");

        if (double.TryParse(widthTextBox?.Text, out double width) &&
            double.TryParse(heightTextBox?.Text, out double height))
        {
            _isUpdatingDimensions = true;

            if (width > height)
            {
                if (landscapeRadioButton != null) landscapeRadioButton.IsChecked = true;
            }
            else
            {
                if (portraitRadioButton != null) portraitRadioButton.IsChecked = true;
            }

            _isUpdatingDimensions = false;
        }
    }

    private void UpdatePreview()
    {
        var previewCanvas = this.FindControl<Canvas>("PreviewCanvas");
        var previewRectangle = this.FindControl<Rectangle>("PreviewRectangle");
        var previewLabel = this.FindControl<TextBlock>("PreviewLabel");
        var widthTextBox = this.FindControl<TextBox>("WidthTextBox");
        var heightTextBox = this.FindControl<TextBox>("HeightTextBox");
        var platformComboBox = this.FindControl<ComboBox>("PlatformComboBox");

        if (previewCanvas == null || previewRectangle == null || previewLabel == null)
            return;

        if (double.TryParse(widthTextBox?.Text, out double width) &&
            double.TryParse(heightTextBox?.Text, out double height) &&
            width > 0 && height > 0)
        {
            // Calculate scale to fit preview area
            double canvasWidth = previewCanvas.Bounds.Width > 0 ? previewCanvas.Bounds.Width : 400;
            double canvasHeight = previewCanvas.Bounds.Height > 0 ? previewCanvas.Bounds.Height : 180;

            double scaleX = (canvasWidth - 40) / width;
            double scaleY = (canvasHeight - 40) / height;
            double scale = Math.Min(scaleX, scaleY);

            double previewWidth = width * scale;
            double previewHeight = height * scale;

            // Center the preview
            double left = (canvasWidth - previewWidth) / 2;
            double top = (canvasHeight - previewHeight) / 2;

            Canvas.SetLeft(previewRectangle, left);
            Canvas.SetTop(previewRectangle, top);
            previewRectangle.Width = previewWidth;
            previewRectangle.Height = previewHeight;

            // Update label
            string platform = (platformComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString()?.Split(' ')[0] ?? "Unknown";
            previewLabel.Text = $"{platform}: {width} × {height} px";
            Canvas.SetLeft(previewLabel, left);
            Canvas.SetTop(previewLabel, top + previewHeight + 5);
        }
    }

    private void ResetButton_Click(object? sender, RoutedEventArgs e)
    {
        InitializeDefaults();
        UpdatePreview();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogHost.Close("MainDialogHost", false);
    }

    private void ApplyButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ValidateInput())
        {
            Result = CreatePageSizeSettings();
            DialogHost.Close("MainDialogHost", false);
        }
    }

    private bool ValidateInput()
    {
        var widthTextBox = this.FindControl<TextBox>("WidthTextBox");
        var heightTextBox = this.FindControl<TextBox>("HeightTextBox");

        if (!double.TryParse(widthTextBox?.Text, out double width) || width <= 0)
        {
            // íãßä ÅÖÇÝÉ ÑÓÇáÉ ÎØÃ åäÇ
            widthTextBox?.Focus();
            return false;
        }

        if (!double.TryParse(heightTextBox?.Text, out double height) || height <= 0)
        {
            // íãßä ÅÖÇÝÉ ÑÓÇáÉ ÎØÃ åäÇ
            heightTextBox?.Focus();
            return false;
        }

        return true;
    }

    private PageSizeSettings CreatePageSizeSettings()
    {
        var widthTextBox = this.FindControl<TextBox>("WidthTextBox");
        var heightTextBox = this.FindControl<TextBox>("HeightTextBox");
        var platformComboBox = this.FindControl<ComboBox>("PlatformComboBox");

        double.TryParse(widthTextBox?.Text, out double width);
        double.TryParse(heightTextBox?.Text, out double height);
        string platform = (platformComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString()?.Split(' ')[0] ?? "WPF";

        var settings = new PageSizeSettings
        {
            Platform = platform,
            Width = (int)width,
            Height = (int)height
        };

        return settings;
    }
}
// Supporting classes
public class PageSizePreset
{
    public string Name { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public PageSizePreset(string name, int width, int height)
    {
        Name = name;
        Width = width;
        Height = height;
    }
}

public class PageSizeSettings
{
    public string Platform { get; set; } = "WPF";
    public int Width { get; set; }
    public int Height { get; set; }
}