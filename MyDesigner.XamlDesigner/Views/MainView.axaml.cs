using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MyDesigner.Designer;
using MyDesigner.XamlDesigner.Configuration;
using MyDesigner.XamlDesigner.Core;
using MyDesigner.XamlDesigner.Models;
using MyDesigner.XamlDesigner.ViewModels;
using System;
using System.IO;

namespace MyDesigner.XamlDesigner.Views;

public partial class MainView : UserControl
{
    public string CurrentFileName { get; set; }
    public MainWindowViewModel ViewModel { get; }
    private MyDesigner.XamlDom.XamlTypeFinder _typeFinder;
    private MyDesigner.XamlDesigner.Intellisense.XamlCompletionProvider _xamlCompletionProvider;
    private MyDesigner.Common.Controls.SimpleIntelliSenseProvider _simpleIntelliSenseProvider;
    public MainView()
    {

        // Initialize ViewModel
        ViewModel = new MainWindowViewModel();
        DataContext = ViewModel;

        // Initialize designer metadata
        BasicMetadata.Register();
        InitializeComponent();
        InitializeIntelliSense();

        // Register ProjectExplorerView with ProjectService and PageRegistry


        Core.PageRegistry.RegisterProjectExplorer(new ProjectExplorerView());

        PageRegistry.ProjectExplorer.FileSelected += ProjectExplorer_FileSelected;
        PageRegistry.ProjectExplorer.FileOpenRequested += ProjectExplorer_FileOpenRequested;
    }

    private void ProjectExplorer_FileOpenRequested(object? sender, string e)
    {

    }

    private void ProjectExplorer_FileSelected(object? sender, string filePath)
    {
        if (PageRegistry.ProjectExplorer?.TreeView.SelectedItem is FileItemViewModel selectedItem)
        {
            filePath = selectedItem.FullPath;

            if (File.Exists(filePath))
            {
                // Read file content
                var fileContent = File.ReadAllText(filePath);

                // Determine file type and perform actions
                if (filePath.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase))
                {
                    // Check if the file is a MAUI file and convert it
                    if (fileContent.Contains("xmlns=\"http://schemas.microsoft.com/dotnet/2021/maui\""))
                    {


                    }
                    else
                    {
                        // Handle standard WPF XAML files
                        CurrentFileName = filePath;
                        Shell.Instance.Open(filePath);
                    }


                }

                else if (filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                         filePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                         filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                         filePath.EndsWith(".config", StringComparison.OrdinalIgnoreCase) ||
                         filePath.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
                         filePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                         filePath.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
                         filePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {

                }
            }
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Ensure we have the correct DataContext
        if (DataContext == null && MainWindow.Instance?.ViewModel != null)
        {
            DataContext = MainWindow.Instance.ViewModel;
        }
    }

    private void InitializeIntelliSense()
    {
        try
        {

            var xamlCompletionProvider = new MyDesigner.XamlDesigner.Intellisense.XamlCompletionProvider(_typeFinder ?? CreateDefaultTypeFinder());


            var simpleProvider = new MyDesigner.Common.Controls.SimpleIntelliSenseProvider();


            _xamlCompletionProvider = xamlCompletionProvider;
            _simpleIntelliSenseProvider = simpleProvider;


        }
        catch (Exception ex)
        {

        }
    }
    private MyDesigner.XamlDom.XamlTypeFinder CreateDefaultTypeFinder()
    {

        try
        {
            return new MyDesigner.XamlDom.XamlTypeFinder();
        }
        catch
        {
            return null;
        }
    }

    private Window GetParentWindow()
    {

        return TopLevel.GetTopLevel(this) as Window;
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        var window = GetParentWindow();
        if (window != null)
        {
            window.WindowState = WindowState.Minimized;
        }
    }

    private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
    {
        var window = GetParentWindow();
        if (window != null)
        {

            if (window.WindowState == WindowState.Maximized)
            {
                window.WindowState = WindowState.Normal;
            }
            else
            {
                window.WindowState = WindowState.Maximized;
            }
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        var window = GetParentWindow();

        window?.Close();
    }

    private void TitleBar_PointerPressed(object sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        // Only start a window drag for left-button presses
        if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
        {
            var window = GetParentWindow();
            try
            {
                window?.BeginMoveDrag(e);
            }
            catch
            {
                // Some platforms or states may not allow BeginMoveDrag; ignore failures
            }
        }
    }


    private void DelFiles_Click(object sender, RoutedEventArgs e)
    {
        if (Settings.Default.RecentFiles != null)
        {
            Settings.Default.RecentFiles.Clear();
            Settings.Default.RecentFiles = null;
        }

        Shell.Instance.RecentFiles.Clear();
        DelFiles.ClearValue(MenuItem.ItemsSourceProperty);
        //SaveSettings();
    }
}