using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyDesigner.XamlDesigner.Models;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace MyDesigner.XamlDesigner.ViewModels
{
    public partial class ProjectExplorerViewViewModel : ViewModelBase
    {
      
        // Test Commands
        public RelayCommand RunCommand { get; private set; }
        public RelayCommand NewFileCommand { get; private set; }

        public RelayCommand BuildCommand { get; private set; }

        public RelayCommand RebuildCommand { get; private set; }

        public RelayCommand StopCommand { get; private set; }


        public ProjectExplorerViewViewModel()
        {
           
        }

        // «·„’œ— «·—∆Ì”Ì ··‹ TreeView
        public ObservableCollection<FileItemViewModel> SolutionItems { get; } = new();

        public async Task OpenFolderAsync(IStorageProvider storageProvider)
        {
            var options = new FilePickerOpenOptions
            {
                Title = "«Œ — „·› «·„‘—Ê⁄",
                FileTypeFilter = new[] { new FilePickerFileType("C# Project") { Patterns = new[] { "*.csproj" } } }
            };

            var result = await storageProvider.OpenFilePickerAsync(options);
            if (result.Count > 0)
            {
                var csprojPath = result[0].Path.LocalPath;
                var projectFolder = Path.GetDirectoryName(csprojPath);

                SolutionItems.Clear(); // ”Ì „  ÕœÌÀ «·‹ UI  ·ﬁ«∆Ì«
                LoadProject(projectFolder);
            }
        }

        private void LoadProject(string folderPath)
        {
            // „À«· ·≈÷«›… „‘—Ê⁄
            var projectNode = new FileItemViewModel
            {
                Name = "MyProject (Core)",
                Icon = "avares://MyDesigner.XamlDesigner/Assets/Visual_Studio_Icon_2022.png",
                ItemType = FileItemType.Project,
                IsFontBold = true,
                IsExpanded = true
            };

            // ≈÷«›… ⁄‰«’— ›—⁄Ì…
            projectNode.Children.Add(new FileItemViewModel { Name = "MainWindow.axaml", ItemType = FileItemType.XamlFile });

            SolutionItems.Add(projectNode);
        }

    }

    public partial class FileItemViewModel : ObservableObject
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public string Icon { get; set; }
        public FileItemType ItemType { get; set; }

        [ObservableProperty]
        private bool _isExpanded;

        [ObservableProperty]
        private bool _isFontBold;

        // Â–Â ÂÌ «·ﬁ«∆„… «· Ì ” Õ ÊÌ ⁄·Ï «·⁄‰«’— «·›—⁄Ì…
        public ObservableCollection<FileItemViewModel> Children { get; } = new();
    }


    public class BoolToFontWeightConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isBold && isBold)
            {
                return FontWeight.Bold;
            }

            return FontWeight.Normal;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}