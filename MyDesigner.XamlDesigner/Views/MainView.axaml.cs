using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MyDesigner.XamlDesigner.Configuration;
using MyDesigner.XamlDesigner.ViewModels;
using System;

namespace MyDesigner.XamlDesigner.Views;

public partial class MainView : UserControl
{  
    // ÕﬁÊ· «·„Ì“«  «·„Õ”‰…
    private MyDesigner.XamlDom.XamlTypeFinder _typeFinder;
    private MyDesigner.XamlDesigner.Intellisense.XamlCompletionProvider _xamlCompletionProvider;
    private MyDesigner.Common.Controls.SimpleIntelliSenseProvider _simpleIntelliSenseProvider;
    public MainView()
    {
        InitializeComponent();
        InitializeIntelliSense();

        // Register ProjectExplorerView with ProjectService and PageRegistry
        var projectExplorer = new ProjectExplorerView();
      
        Core.PageRegistry.RegisterProjectExplorer(projectExplorer);

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
            //  ÂÌ∆… „“Êœ IntelliSense ··‹ XAML
            var xamlCompletionProvider = new MyDesigner.XamlDesigner.Intellisense.XamlCompletionProvider(_typeFinder ?? CreateDefaultTypeFinder());

            //  ÂÌ∆… „“Êœ IntelliSense «·»”Ìÿ ··‹ C#
            var simpleProvider = new MyDesigner.Common.Controls.SimpleIntelliSenseProvider();

            // Õ›Ÿ «·„—«Ã⁄ ··«” Œœ«„ ·«Õﬁ«
            _xamlCompletionProvider = xamlCompletionProvider;
            _simpleIntelliSenseProvider = simpleProvider;

            System.Diagnostics.Debug.WriteLine(" „  ÂÌ∆… IntelliSense »‰Ã«Õ");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Œÿ√ ›Ì  ÂÌ∆… IntelliSense: {ex.Message}");
        }
    }
    private MyDesigner.XamlDom.XamlTypeFinder CreateDefaultTypeFinder()
    {
        // ≈‰‘«¡ TypeFinder «› —«÷Ì
        try
        {
            return new MyDesigner.XamlDom.XamlTypeFinder();
        }
        catch
        {
            return null;
        }
    }
    // œ«·… „”«⁄œ… ··Õ’Ê· ⁄·Ï «·‰«›–… «·√’·
    private Window GetParentWindow()
    {
        // TopLevel ÂÊ «·ﬂ·«” «·√”«”Ì ··‰Ê«›–° GetTopLevel  »ÕÀ ›Ì «·‘Ã—… «·„—∆Ì…
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
            //  »œÌ· »Ì‰ Ê÷⁄ «· ﬂ»Ì— (Maximized) Ê«·Ê÷⁄ «·ÿ»Ì⁄Ì (Normal)
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
        // ������� ���� ������� ��� ������� �����
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