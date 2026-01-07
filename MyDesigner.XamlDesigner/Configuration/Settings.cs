using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;

namespace MyDesigner.XamlDesigner.Configuration
{
    public class Settings
    {
        private static Settings _default;
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MyDesigner.XamlDesigner",
            "settings.json");

        public static Settings Default
        {
            get
            {
                if (_default == null)
                {
                    _default = Load();
                }
                return _default;
            }
        }

        public Rect MainWindowRect { get; set; } = new Rect(0, 0, 1200, 800);
        public WindowState MainWindowState { get; set; } = WindowState.Normal;
        public string DockLayout { get; set; } = "";
        public List<string> RecentFiles { get; set; } = new List<string>();
        public List<string> AssemblyList { get; set; } = new List<string>();
        public string ProjectName { get;  set; }
        public string ProjectType { get;  set; }
        public string ProjectPath { get;  set; }
        public double LastPageWidth { get; internal set; }
        public double LastPageHeight { get; internal set; }
        public string LastPlatform { get; internal set; }
        public string? DefaultPlatform { get; internal set; }

        private static Settings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
                }
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
            return new Settings();
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }
    }
}