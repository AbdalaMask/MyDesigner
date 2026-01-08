using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Data.Core.Plugins;
using MyDesigner.Designer;
using MyDesigner.XamlDesigner.ViewModels;
using MyDesigner.XamlDesigner.Views;

namespace MyDesigner.XamlDesigner
{
    public partial class App : Application
    {
        public static ProjectExplorerViewViewModel ExplorerVM { get; } = new();
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Configure shutdown behavior
                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
                
                // Handle application exit event
                desktop.Exit += OnApplicationExit;
                
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel()
                };
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = new MainWindowViewModel()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnApplicationExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            try
            {
                // Perform final cleanup
                Shell.Instance?.SaveSettings();
                
                // Force exit if needed
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                // Log the exception but don't prevent shutdown
                System.Diagnostics.Debug.WriteLine($"Error during application exit: {ex.Message}");
                Environment.Exit(1);
            }
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}