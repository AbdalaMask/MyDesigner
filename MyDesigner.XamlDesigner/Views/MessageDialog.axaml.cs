using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using DialogHostAvalonia;
using MyDesigner.XamlDesigner.Services;

namespace MyDesigner.XamlDesigner.Views;

public partial class MessageDialog : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MessageDialog, string>(nameof(Title), "Message");

    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<MessageDialog, string>(nameof(Message), "");

    public static readonly StyledProperty<MessageDialogType> DialogTypeProperty =
        AvaloniaProperty.Register<MessageDialog, MessageDialogType>(nameof(DialogType), MessageDialogType.Information);

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

    public MessageDialogType DialogType
    {
        get => GetValue(DialogTypeProperty);
        set => SetValue(DialogTypeProperty, value);
    }

    public MessageDialog()
    {
        InitializeComponent();
        DataContext = this;
        UpdateIcon();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == DialogTypeProperty)
        {
            UpdateIcon();
        }
    }

    private void UpdateIcon()
    {
        //Path iconPath = this.FindControl<Path>("IconPath");
        //if (iconPath == null) return;

        //var iconKey = DialogType switch
        //{
        //    MessageDialogType.Information => "InfoIcon",
        //    MessageDialogType.Warning => "WarningIcon",
        //    MessageDialogType.Error => "ErrorIcon",
        //    MessageDialogType.Success => "SuccessIcon",
        //    _ => "InfoIcon"
        //};

        //if (Resources.TryGetResource(iconKey, null, out var resource) && resource is StreamGeometry geometry)
        //{
        //    iconPath.Data = geometry;
        //}
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogHost.Close("MainDialogHost", true);
    }
}