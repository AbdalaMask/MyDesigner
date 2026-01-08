using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DialogHostAvalonia;

namespace MyDesigner.XamlDesigner;

public partial class OpenProjectDialog : UserControl
{
    public string? SelectedOption { get; private set; }
    public OpenProjectDialog()
    {
        InitializeComponent();
    }

    private void BtnOpenProject_Click(object? sender, RoutedEventArgs e)
    {
        SelectedOption = "project";
        DialogHost.Close("MainDialogHost", this);
    }

    private void BtnOpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        SelectedOption = "folder";
        DialogHost.Close("MainDialogHost", this);
    }

    private void BtnOpenFile_Click(object? sender, RoutedEventArgs e)
    {
        SelectedOption = "file";
        DialogHost.Close("MainDialogHost", this);
    }

  
    private void BtnCancel_Click(object? sender, RoutedEventArgs e)
    {
        SelectedOption = null;
        DialogHost.Close("MainDialogHost", false);
    }
}