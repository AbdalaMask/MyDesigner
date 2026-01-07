using Avalonia.Controls;
using System.Collections.Generic;
using System.Windows.Input;

namespace MyDesigner.XamlDesigner.Plugins;

/// <summary>
/// Interface for designer plugins
/// </summary>
public interface IDesignerPlugin
{
    /// <summary>
    /// Plugin name
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Plugin version
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Plugin description
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Plugin author
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Initialize the plugin
    /// </summary>
    /// <param name="host">Designer host</param>
    void Initialize(IDesignerHost host);

    /// <summary>
    /// Cleanup plugin resources
    /// </summary>
    void Cleanup();

    /// <summary>
    /// Check if plugin is compatible with current version
    /// </summary>
    /// <param name="designerVersion">Designer version</param>
    /// <returns>True if compatible</returns>
    bool IsCompatible(string designerVersion);
}

/// <summary>
/// Designer host interface for plugins
/// </summary>
public interface IDesignerHost
{
    /// <summary>
    /// Current document
    /// </summary>
    Document CurrentDocument { get; }

    /// <summary>
    /// All open documents
    /// </summary>
    IEnumerable<Document> Documents { get; }

    /// <summary>
    /// Register a command
    /// </summary>
    void RegisterCommand(string name, ICommand command);

    /// <summary>
    /// Register a tool window
    /// </summary>
    void RegisterToolWindow(string name, Control window);

    /// <summary>
    /// Show message to user
    /// </summary>
    void ShowMessage(string message, string title = "Plugin Message");

    /// <summary>
    /// Log message
    /// </summary>
    void Log(string message, LogLevel level = LogLevel.Info);
}

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}