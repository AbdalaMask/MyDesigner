using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using DialogHostAvalonia;
using System.IO;

namespace MyDesigner.XamlDesigner;

public partial class NewProjectWindow : UserControl
{
    public string ProjectName { get; private set; }
    public string ProjectLocation { get; private set; }
    public string ProjectType { get; private set; }
    public string Template { get; private set; }
    public bool DialogResult { get; private set; }
    public NewProjectWindow()
    {
        InitializeComponent();
        UpdatePreview();

        // Set up event handlers for real-time preview updates
        var projectNameTextBox = this.FindControl<TextBox>("ProjectNameTextBox");
        var projectLocationTextBox = this.FindControl<TextBox>("ProjectLocationTextBox");

        if (projectNameTextBox != null)
            projectNameTextBox.TextChanged += (s, e) => UpdatePreview();

        if (projectLocationTextBox != null)
            projectLocationTextBox.TextChanged += (s, e) => UpdatePreview();
    }
    private void UpdatePreview()
    {
        var projectNameTextBox = this.FindControl<TextBox>("ProjectNameTextBox");
        var projectLocationTextBox = this.FindControl<TextBox>("ProjectLocationTextBox");
        var previewTextBlock = this.FindControl<TextBlock>("PreviewTextBlock");

        if (projectNameTextBox != null && projectLocationTextBox != null && previewTextBlock != null)
        {
            var projectName = projectNameTextBox.Text ?? "MyAvaloniaProject";
            var projectLocation = projectLocationTextBox.Text ?? "C:\\Projects";
            var fullPath = Path.Combine(projectLocation, projectName);

            previewTextBlock.Text = $"Project will be created at: {fullPath}\nÓíÊã ÅäÔÇÁ ÇáãÔÑæÚ Ýí: {fullPath}";
        }
    }

    private async void BrowseLocation_Click(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Project Location",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                var projectLocationTextBox = this.FindControl<TextBox>("ProjectLocationTextBox");
                if (projectLocationTextBox != null)
                {
                    projectLocationTextBox.Text = folders[0].Path.LocalPath;
                    UpdatePreview();
                }
            }
        }
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate inputs
        var projectNameTextBox = this.FindControl<TextBox>("ProjectNameTextBox");
        var projectLocationTextBox = this.FindControl<TextBox>("ProjectLocationTextBox");

        if (projectNameTextBox == null || string.IsNullOrWhiteSpace(projectNameTextBox.Text))
        {
            // TODO: Show validation message
            return;
        }

        if (projectLocationTextBox == null || string.IsNullOrWhiteSpace(projectLocationTextBox.Text))
        {
            // TODO: Show validation message
            return;
        }

        // Get selected values
        ProjectName = projectNameTextBox.Text.Trim();
        ProjectLocation = projectLocationTextBox.Text.Trim();

        // Get project type
        var avaloniaRadio = this.FindControl<RadioButton>("AvaloniaRadio");
        var wpfRadio = this.FindControl<RadioButton>("WpfRadio");
        var mauiRadio = this.FindControl<RadioButton>("MauiRadio");
        var unoRadio = this.FindControl<RadioButton>("UnoRadio");

        if (avaloniaRadio?.IsChecked == true)
            ProjectType = "Avalonia";
        else if (wpfRadio?.IsChecked == true)
            ProjectType = "WPF";
        else if (mauiRadio?.IsChecked == true)
            ProjectType = "MAUI";
        else if (unoRadio?.IsChecked == true)
            ProjectType = "Uno";
        else
            ProjectType = "Avalonia"; // Default

        // Get template
        var templateComboBox = this.FindControl<ComboBox>("TemplateComboBox");
        Template = templateComboBox?.SelectedIndex switch
        {
            0 => "Empty",
            1 => "MVVM",
            2 => "Library",
            _ => "Empty"
        };

        DialogResult = true;
        DialogHost.Close("MainDialogHost", false);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        DialogHost.Close("MainDialogHost", false);
    }
    
}