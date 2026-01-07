using Avalonia.Controls;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using MyDesigner.Designer.OutlineView;
using MyDesigner.Designer.PropertyGrid;
using MyDesigner.Designer.ThumbnailView;
using MyDesigner.XamlDesigner.ViewModels.Tools;
using MyDesigner.XamlDesigner.Views;
using System.ComponentModel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MyDesigner.XamlDesigner.ViewModels
{
    public class DockFactory : Factory
    {
        private readonly object _context;
        private IRootDock? _rootDock;
        private IDocumentDock? _documentDock;

        public DockFactory(object context)
        {
            _context = context;
        }

        public override IRootDock CreateLayout()
        {
            // Create tool ViewModels
            var ProjectExplorerTool = new Tools.ProjectExplorerDock { Id = "projectExplorer", Title = "ProjectExplorer" };
            var toolboxTool = new Tools.ToolboxDock { Id = "Toolbox", Title = "Toolbox" };
            var outlineTool = new Tools.OutlineDock { Id = "Outline", Title = "Outline" };
            var errorsTool = new Tools.ErrorsToolDock { Id = "Errors", Title = "Errors" };
            var propertiesTool = new Tools.PropertyGridDock { Id = "Properties", Title = "Properties" };
            var thumbnailTool = new Tools.ThumbnailDock { Id = "Thumbnail", Title = "Thumbnail" };
            var symbolTool = new Tools.SymbolsDock { Id = "Symbols", Title = "Symbols" };
            // Left dock (Toolbox/Outline/ProjectExplorer group)
            var leftDock = new ProportionalDock
            {
                Proportion = 0.2,
                Orientation = Orientation.Vertical,
                VisibleDockables = CreateList<IDockable>
                (
                    new ToolDock
                    {
                        Proportion = 0.6,
                        ActiveDockable = toolboxTool,
                        VisibleDockables = CreateList<IDockable>(ProjectExplorerTool, toolboxTool, outlineTool),
                        Alignment = Alignment.Left
                    }
                )
            };

            // Right dock (Properties/Thumbnail group)
            var rightDock = new ProportionalDock
            {
                Proportion = 0.25,
                Orientation = Orientation.Vertical,
                VisibleDockables = CreateList<IDockable>
                (
                    new ToolDock
                    {
                        Proportion = 0.7,
                        ActiveDockable = propertiesTool,
                        VisibleDockables = CreateList<IDockable>(propertiesTool, symbolTool, thumbnailTool),
                        Alignment = Alignment.Right
                    }
                )
            };

            // Document dock (Top part of center area)
            var documentDock = new Dock.Model.Mvvm.Controls.DocumentDock
            {
                Id = "Documents",
                Title = "Documents",
                IsCollapsable = false,
                VisibleDockables = CreateList<IDockable>(),
                CanCreateDocument = true,
                Proportion = 0.8 // Documents take 80% height of the center column
            };

            // Errors panel hosted in a ToolDock (Bottom part of center area)
            var errorsDockPanel = new ToolDock
            {
                ActiveDockable = errorsTool,
                VisibleDockables = CreateList<IDockable>(errorsTool),
                Alignment = Alignment.Bottom,
                Proportion = 0.2 // Errors take 20% height of the center column
            };

            // **Center Column Layout:** A vertical stack of DocumentDock on top and Errors below it
            var centerColumnLayout = new ProportionalDock
            {
                Orientation = Orientation.Vertical,
                VisibleDockables = CreateList<IDockable>
                (
                    documentDock,
                    new ProportionalDockSplitter(), // Splitter only within the center column
                    errorsDockPanel
                )
            };

            // Main layout: Horizontal arrangement (Left panel | Center Column | Right panel)
            var mainLayout = new ProportionalDock
            {
                Orientation = Orientation.Horizontal,
                VisibleDockables = CreateList<IDockable>
                (
                    leftDock,
                    new ProportionalDockSplitter(),
                    centerColumnLayout, // Use the new vertical center layout here
                    new ProportionalDockSplitter(),
                    rightDock
                )
            };

            // Home view wraps the main layout
            var homeView = new ProportionalDock
            {
                Id = "Home",
                Title = "Home",
                ActiveDockable = mainLayout,
                VisibleDockables = CreateList<IDockable>(mainLayout)
            };

            // Set up the final root dock
            var rootDock = CreateRootDock();
            rootDock.IsCollapsable = false;
            rootDock.ActiveDockable = homeView;
            rootDock.DefaultDockable = homeView;
            rootDock.VisibleDockables = CreateList<IDockable>(homeView);

            _documentDock = documentDock;
            _rootDock = rootDock;

            return rootDock;
        }



        public override void InitLayout(IDockable layout)
        {
            DockableLocator = new Dictionary<string, Func<IDockable?>>()
            {
                ["Root"] = () => _rootDock,
                ["Documents"] = () => _documentDock
            };

            HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
            {
                [nameof(IDockWindow)] = () => new HostWindow()
            };

            base.InitLayout(layout);
        }

        public void AddDocument(Document document)
        {
            if (_documentDock != null)
            {
                var documentViewModel = new DocumentDock(document);
                _documentDock.VisibleDockables.Add(documentViewModel);
                _documentDock.ActiveDockable = documentViewModel;
                
                // Update the factory to handle the new document
                if (ContextLocator != null)
                {
                    ContextLocator[documentViewModel.Id] = () => new Views.DocumentView { DataContext = documentViewModel };
                }
            }
        }

        public void RemoveDocument(Document document)
        {
            if (_documentDock != null)
            {
                var docToRemove = _documentDock.VisibleDockables
                    .OfType<DocumentDock>()
                    .FirstOrDefault(d => d.Document == document);
                if (docToRemove != null)
                {
                    _documentDock.VisibleDockables.Remove(docToRemove);
                    
                    // Remove from ContextLocator
                    if (ContextLocator != null && ContextLocator.ContainsKey(docToRemove.Id))
                    {
                        ContextLocator.Remove(docToRemove.Id);
                    }
                }
            }
        }
    }

    // Document dockable wrapper
    public class DocumentDock : Dock.Model.Mvvm.Controls.Document
    {
        public Document Document { get; }

        public DocumentDock(Document document)
        {
            Document = document;
            Id = $"Document_{document.Name}_{Guid.NewGuid():N}";
            Title = document.Name;
            
            // Set the Document as the context for the DocumentView
            Context = document;
        }
    }
}