using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MyDesigner.XamlDesigner.Plugins;

/// <summary>
/// Manages designer plugins
/// </summary>
public class PluginManager
{
    private readonly List<IDesignerPlugin> _loadedPlugins = new();
    private readonly IDesignerHost _host;

    public PluginManager(IDesignerHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    /// <summary>
    /// All loaded plugins
    /// </summary>
    public IReadOnlyList<IDesignerPlugin> LoadedPlugins => _loadedPlugins.AsReadOnly();

    /// <summary>
    /// Load plugins from directory
    /// </summary>
    public void LoadPluginsFromDirectory(string pluginDirectory)
    {
        if (!Directory.Exists(pluginDirectory))
        {
            _host.Log($"Plugin directory not found: {pluginDirectory}", LogLevel.Warning);
            return;
        }

        var pluginFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.AllDirectories);
        
        foreach (var pluginFile in pluginFiles)
        {
            try
            {
                LoadPlugin(pluginFile);
            }
            catch (Exception ex)
            {
                _host.Log($"Failed to load plugin from {pluginFile}: {ex.Message}", LogLevel.Error);
            }
        }
    }

    /// <summary>
    /// Load a single plugin
    /// </summary>
    public bool LoadPlugin(string pluginPath)
    {
        try
        {
            if (!File.Exists(pluginPath))
            {
                _host.Log($"Plugin file not found: {pluginPath}", LogLevel.Error);
                return false;
            }

            var assembly = Assembly.LoadFrom(pluginPath);
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IDesignerPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var pluginType in pluginTypes)
            {
                var plugin = Activator.CreateInstance(pluginType) as IDesignerPlugin;
                if (plugin != null)
                {
                    if (plugin.IsCompatible(GetDesignerVersion()))
                    {
                        plugin.Initialize(_host);
                        _loadedPlugins.Add(plugin);
                        _host.Log($"Loaded plugin: {plugin.Name} v{plugin.Version}", LogLevel.Info);
                    }
                    else
                    {
                        _host.Log($"Plugin {plugin.Name} is not compatible with current designer version", LogLevel.Warning);
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _host.Log($"Error loading plugin {pluginPath}: {ex.Message}", LogLevel.Error);
            return false;
        }
    }

    /// <summary>
    /// Unload all plugins
    /// </summary>
    public void UnloadAllPlugins()
    {
        foreach (var plugin in _loadedPlugins.ToList())
        {
            try
            {
                plugin.Cleanup();
                _host.Log($"Unloaded plugin: {plugin.Name}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _host.Log($"Error unloading plugin {plugin.Name}: {ex.Message}", LogLevel.Error);
            }
        }

        _loadedPlugins.Clear();
    }

    /// <summary>
    /// Get plugin by name
    /// </summary>
    public IDesignerPlugin GetPlugin(string name)
    {
        return _loadedPlugins.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get designer version
    /// </summary>
    private string GetDesignerVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0.0";
    }
}