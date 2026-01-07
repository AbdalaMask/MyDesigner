using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using MyDesigner.Designer.Controls;
using MyDesigner.Designer.Services;


namespace MyDesigner.XamlDesigner.Views
{
    public partial class FromToolboxView : UserControl
    {
        public Toolbox? ViewModel => DataContext as Toolbox;
        private AssemblyNode? _selectedAssembly;
        private ControlNode? _selectedControl;
        private DragListener? _dragListener;
        private CreateComponentTool? _currentTool;
        private PointerEventArgs? _lastPointerEvent;

        public FromToolboxView()
        {
            InitializeComponent();
            
            // Setup event handlers
            var treeView = this.FindControl<TreeView>("uxTreeView");
            if (treeView != null)
            {
                treeView.SelectionChanged += TreeView_SelectionChanged;
                treeView.DoubleTapped += TreeView_DoubleTapped;
                treeView.KeyDown += TreeView_KeyDown;
                treeView.PointerMoved += TreeView_PointerMoved;
                
                // Setup drag and drop
                _dragListener = new DragListener(treeView);
                _dragListener.Started += OnDragStarted;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("=== ERROR: TOOLBOX TREEVIEW NOT FOUND ===");
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void TreeView_PointerMoved(object? sender, PointerEventArgs e)
        {
            _lastPointerEvent = e;
        }

        private async void OnDragStarted(DragListener drag)
        {
            // Get the selected control node for drag operation
            if (_selectedControl != null && _currentTool != null && _lastPointerEvent != null)
            {
                try
                {
                    // Create data object for drag and drop
                    var dataObject = new DataObject();
                    dataObject.Set(typeof(CreateComponentTool).FullName!, _currentTool);
                    
                    // Start drag and drop operation using the last pointer event
                    await DragDrop.DoDragDrop(_lastPointerEvent, dataObject, DragDropEffects.Copy);
                    
                    System.Diagnostics.Debug.WriteLine($"Drag operation started for: {_selectedControl.Type.Name}");
                }
                catch (Exception ex)
                {
                    Shell.ReportException(ex);
                }
            }
        }

        private void TreeView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems?.Count > 0)
            {
                var selectedItem = e.AddedItems[0];
                
                if (selectedItem is AssemblyNode assembly)
                {
                    _selectedAssembly = assembly;
                    _selectedControl = null;
                    _currentTool = null;
                }
                else if (selectedItem is ControlNode control)
                {
                    _selectedControl = control;
                    // Prepare tool when selection changes (for keyboard focus)
                    PrepareTool(control, false);
                }
            }
        }

        private void TreeView_DoubleTapped(object? sender, TappedEventArgs e)
        {
            if (_selectedControl != null)
            {
                PrepareTool(_selectedControl, false);
            }
        }

        private void TreeView_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && _selectedAssembly != null && ViewModel != null)
            {
                ViewModel.Remove(_selectedAssembly);
                e.Handled = true;
            }
        }

        private void PrepareTool(ControlNode node, bool drag)
        {
            if (node?.Type != null && Shell.Instance.CurrentDocument?.DesignContext != null)
            {
                try
                {
                    var tool = new CreateComponentTool(node.Type);
                    Shell.Instance.CurrentDocument.DesignContext.Services.Tool.CurrentTool = tool;
                    
                    // Store the tool for drag operations
                    _currentTool = tool;
                    
                    System.Diagnostics.Debug.WriteLine($"Tool prepared: {node.Type.Name}, Drag: {drag}");
                }
                catch (Exception ex)
                {
                    Shell.ReportException(ex);
                }
            }
        }

        private async void BrowseForAssemblies_OnClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null && ViewModel != null)
                {
                    var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = "Select Assemblies",
                        AllowMultiple = true,
                        FileTypeFilter = new[]
                        {
                            new FilePickerFileType("Assemblies")
                            {
                                Patterns = new[] { "*.dll", "*.exe" }
                            },
                            new FilePickerFileType("All Files")
                            {
                                Patterns = new[] { "*.*" }
                            }
                        }
                    });

                    if (files?.Count > 0)
                    {
                        foreach (var file in files)
                        {
                            ViewModel.AddAssembly(file.Path.LocalPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        private void RemoveAssembly_OnClick(object? sender, RoutedEventArgs e)
        {
            if (_selectedAssembly != null && ViewModel != null)
            {
                ViewModel.Remove(_selectedAssembly);
            }
        }

        private void Refresh_OnClick(object? sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            
            try
            {
                // Refresh by reloading assemblies
                var assemblies = ViewModel.AssemblyNodes.Select(a => a.Assembly).ToList();
                ViewModel.AssemblyNodes.Clear();
                
                foreach (var assembly in assemblies)
                {
                    ViewModel.AddAssembly(assembly);
                }
                
                System.Diagnostics.Debug.WriteLine("Toolbox refreshed");
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        private void ActivateTool(ControlNode controlNode)
        {
            PrepareTool(controlNode, false);
        }

        private void ClearSearchButton_OnClick(object? sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void SearchTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}