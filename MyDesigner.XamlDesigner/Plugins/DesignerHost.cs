using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace MyDesigner.XamlDesigner.Plugins;

/// <summary>
/// Implementation of IDesignerHost for plugins
/// </summary>
public class DesignerHost : IDesignerHost
{
    private readonly Dictionary<string, ICommand> _commands = new();
    private readonly Dictionary<string, Control> _toolWindows = new();

    public Document CurrentDocument => Shell.Instance.CurrentDocument;

    public IEnumerable<Document> Documents => Shell.Instance.Documents;

    public void RegisterCommand(string name, ICommand command)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Command name cannot be null or empty", nameof(name));

        if (command == null)
            throw new ArgumentNullException(nameof(command));

        _commands[name] = command;
        Log($"Registered command: {name}", LogLevel.Debug);
    }

    public void RegisterToolWindow(string name, Control window)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Tool window name cannot be null or empty", nameof(name));

        if (window == null)
            throw new ArgumentNullException(nameof(window));

        _toolWindows[name] = window;
        Log($"Registered tool window: {name}", LogLevel.Debug);

        // TODO: Add window to main UI
    }

    public void ShowMessage(string message, string title = "Plugin Message")
    {
        // TODO: Implement message dialog for Avalonia
        Console.WriteLine($"[{title}] {message}");
    }

    public void Log(string message, LogLevel level = LogLevel.Info)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var levelStr = level.ToString().ToUpper();
        Console.WriteLine($"[{timestamp}] [{levelStr}] {message}");
    }

    /// <summary>
    /// Get registered command by name
    /// </summary>
    public ICommand GetCommand(string name)
    {
        return _commands.TryGetValue(name, out var command) ? command : null;
    }

    /// <summary>
    /// Get registered tool window by name
    /// </summary>
    public Control GetToolWindow(string name)
    {
        return _toolWindows.TryGetValue(name, out var window) ? window : null;
    }

    /// <summary>
    /// Get all registered commands
    /// </summary>
    public IReadOnlyDictionary<string, ICommand> GetAllCommands()
    {
        return _commands.AsReadOnly();
    }

    /// <summary>
    /// Get all registered tool windows
    /// </summary>
    public IReadOnlyDictionary<string, Control> GetAllToolWindows()
    {
        return _toolWindows.AsReadOnly();
    }
}