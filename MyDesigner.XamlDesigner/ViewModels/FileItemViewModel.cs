using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MyDesigner.XamlDesigner.Models;

namespace MyDesigner.XamlDesigner.ViewModels;

public partial class FileItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string fullPath = string.Empty;

    [ObservableProperty]
    private string icon = string.Empty;

    [ObservableProperty]
    private FileItemType itemType;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isFontBold;

   
    public ObservableCollection<FileItemViewModel> Children { get; } = new();
}