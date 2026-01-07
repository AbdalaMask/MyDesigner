using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DialogHostAvalonia;

namespace MyDesigner.XamlDesigner.Views;

public partial class ConfirmationDialog : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ConfirmationDialog, string>(nameof(Title), "Confirmation");

    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<ConfirmationDialog, string>(nameof(Message), "");

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public ConfirmationDialog()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void YesButton_Click(object sender, RoutedEventArgs e)
    {
        DialogHost.Close("MainDialogHost", true);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogHost.Close("MainDialogHost", false);
    }
}