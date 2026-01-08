using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Dock.Model.Core;
using MyDesigner.Designer.OutlineView;
using MyDesigner.Designer.PropertyGrid;
using MyDesigner.Designer.ThumbnailView;

using MyDesigner.XamlDesigner.ViewModels;
using MyDesigner.XamlDesigner.ViewModels.Tools;
using MyDesigner.XamlDesigner.Views;

using StaticViewLocator;
using System;
using System.Diagnostics.CodeAnalysis;

namespace MyDesigner.XamlDesigner
{
    [StaticViewLocator]
    public partial class ViewLocator : IDataTemplate
    {
        public Control? Build(object? data)
        {
            if (data is null)
            {
                return null;
            }

            switch (data)
            {
                case ProjectExplorerDock:
                  
                  return Core.PageRegistry.ProjectExplorer;
                case PropertyGridDock vm:
                    return new Views.Tools.PropertyGridToolView();
                case ToolboxDock vm:
                    return new FromToolboxView { DataContext = Toolbox.Instance };
                case ErrorsToolDock vm:
                    return new Views.Tools.ErrorListToolView();
                case OutlineDock vm:
                    return new Views.Tools.OutlineToolView();
                case ThumbnailDock vm:
                    return new Views.Tools.ThumbnailToolView();
                case DocumentDock vm:
                    return new DocumentView();
                default:
                    return new TextBlock { Text = $"View not found for {data.GetType().Name}" };
            }
        }

        public bool Match(object? data)
        {
            if (data is null)
            {
                return false;
            }

            var type = data.GetType();
            return data is IDockable || s_views.ContainsKey(type);
        }
    }
}