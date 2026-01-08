using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MyDesigner.XamlDesigner;

public partial class DatabaseConnectionManager : UserControl
{
    private List<DatabaseConnection> _connections = new();
    private readonly string _connectionsFilePath = "database_connections.json";
    public DatabaseConnectionManager()
    {
        InitializeComponent();
        LoadConnections();
        UpdateConnectionsList();
    }
    /// <summary>
    /// Load saved connections
    /// </summary>
    private void LoadConnections()
    {
        try
        {
            if (File.Exists(_connectionsFilePath))
            {
                var json = File.ReadAllText(_connectionsFilePath);
                _connections = JsonSerializer.Deserialize<List<DatabaseConnection>>(json) ?? new();
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"خطأ في تحميل الاتصالات: {ex.Message}", false);
        }
    }

    /// <summary>
    /// Save connections
    /// </summary>
    private void SaveConnections()
    {
        try
        {
            var json = JsonSerializer.Serialize(_connections, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_connectionsFilePath, json);
        }
        catch (Exception ex)
        {
            ShowStatus($"خطأ في حفظ الاتصالات: {ex.Message}", false);
        }
    }

    /// <summary>
    /// Update connections list
    /// </summary>
    private void UpdateConnectionsList()
    {
        var connectionsList = this.FindControl<ListBox>("ConnectionsList");
        if (connectionsList != null)
        {
            connectionsList.Items.Clear();
            foreach (var conn in _connections)
            {
                connectionsList.Items.Add($"{conn.Name} ({conn.DatabaseType})");
            }
        }
    }

    /// <summary>
    /// Change database type
    /// </summary>
    private void DatabaseType_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var comboBox = sender as ComboBox;
        if (comboBox?.SelectedItem is ComboBoxItem item)
        {
            var dbType = item.Content?.ToString();
            UpdateFieldsForDatabaseType(dbType);
        }
    }

    /// <summary>
    /// Update fields based on database type
    /// </summary>
    private void UpdateFieldsForDatabaseType(string? dbType)
    {
        var serverLabel = this.FindControl<TextBlock>("ServerLabel");
        var serverTextBox = this.FindControl<TextBox>("ServerTextBox");
        var portLabel = this.FindControl<TextBlock>("PortLabel");
        var portTextBox = this.FindControl<TextBox>("PortTextBox");
        var databaseLabel = this.FindControl<TextBlock>("DatabaseLabel");

        if (serverLabel == null || serverTextBox == null || portLabel == null ||
            portTextBox == null || databaseLabel == null)
            return;

        switch (dbType)
        {
            case "SQL Server":
                portTextBox.Text = "1433";
                serverLabel.IsVisible = true;
                serverTextBox.IsVisible = true;
                portLabel.IsVisible = true;
                portTextBox.IsVisible = true;
                databaseLabel.Text = "اسم قاعدة البيانات:";
                break;

            case "MySQL":
                portTextBox.Text = "3306";
                serverLabel.IsVisible = true;
                serverTextBox.IsVisible = true;
                portLabel.IsVisible = true;
                portTextBox.IsVisible = true;
                databaseLabel.Text = "اسم قاعدة البيانات:";
                break;

            case "PostgreSQL":
                portTextBox.Text = "5432";
                serverLabel.IsVisible = true;
                serverTextBox.IsVisible = true;
                portLabel.IsVisible = true;
                portTextBox.IsVisible = true;
                databaseLabel.Text = "اسم قاعدة البيانات:";
                break;

            case "SQLite":
                serverLabel.IsVisible = false;
                serverTextBox.IsVisible = false;
                portLabel.IsVisible = false;
                portTextBox.IsVisible = false;
                databaseLabel.Text = "مسار ملف قاعدة البيانات:";
                break;

            case "MongoDB":
                portTextBox.Text = "27017";
                serverLabel.IsVisible = true;
                serverTextBox.IsVisible = true;
                portLabel.IsVisible = true;
                portTextBox.IsVisible = true;
                databaseLabel.Text = "اسم قاعدة البيانات:";
                break;

            case "Oracle":
                portTextBox.Text = "1521";
                serverLabel.IsVisible = true;
                serverTextBox.IsVisible = true;
                portLabel.IsVisible = true;
                portTextBox.IsVisible = true;
                databaseLabel.Text = "اسم قاعدة البيانات:";
                break;

            case "Access":
                serverLabel.IsVisible = false;
                serverTextBox.IsVisible = false;
                portLabel.IsVisible = false;
                portTextBox.IsVisible = false;
                databaseLabel.Text = "مسار ملف Access:";
                break;
        }
    }

    /// <summary>
    /// Build connection string
    /// </summary>
    private void BuildConnectionString_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var databaseTypeComboBox = this.FindControl<ComboBox>("DatabaseTypeComboBox");
            var dbType = (databaseTypeComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var connectionString = BuildConnectionString(dbType);

            var connectionStringTextBox = this.FindControl<TextBox>("ConnectionStringTextBox");
            if (connectionStringTextBox != null)
            {
                connectionStringTextBox.Text = connectionString;
            }

            ShowStatus("تم بناء نص الاتصال بنجاح", true);
        }
        catch (Exception ex)
        {
            ShowStatus($"خطأ في بناء نص الاتصال: {ex.Message}", false);
        }
    }

    /// <summary>
    /// Build connection string based on type
    /// </summary>
    private string BuildConnectionString(string? dbType)
    {
        var serverTextBox = this.FindControl<TextBox>("ServerTextBox");
        var portTextBox = this.FindControl<TextBox>("PortTextBox");
        var databaseNameTextBox = this.FindControl<TextBox>("DatabaseNameTextBox");
        var usernameTextBox = this.FindControl<TextBox>("UsernameTextBox");
        var passwordBox = this.FindControl<TextBox>("PasswordBox");

        var server = serverTextBox?.Text ?? "";
        var port = portTextBox?.Text ?? "";
        var database = databaseNameTextBox?.Text ?? "";
        var username = usernameTextBox?.Text ?? "";
        var password = passwordBox?.Text ?? "";

        return dbType switch
        {
            "SQL Server" => $"Server={server},{port};Database={database};User Id={username};Password={password};",
            "MySQL" => $"Server={server};Port={port};Database={database};Uid={username};Pwd={password};",
            "PostgreSQL" => $"Host={server};Port={port};Database={database};Username={username};Password={password};",
            "SQLite" => $"Data Source={database};",
            "MongoDB" => $"mongodb://{username}:{password}@{server}:{port}/{database}",
            "Oracle" => $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={server})(PORT={port}))(CONNECT_DATA=(SERVICE_NAME={database})));User Id={username};Password={password};",
            "Access" => $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={database};",
            _ => ""
        };
    }

    /// <summary>
    /// Test connection
    /// </summary>
    private void TestConnection_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var databaseTypeComboBox = this.FindControl<ComboBox>("DatabaseTypeComboBox");
            var connectionStringTextBox = this.FindControl<TextBox>("ConnectionStringTextBox");

            var dbType = (databaseTypeComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var connectionString = string.IsNullOrWhiteSpace(connectionStringTextBox?.Text)
                ? BuildConnectionString(dbType)
                : connectionStringTextBox?.Text ?? "";

            bool success = TestDatabaseConnection(dbType, connectionString);

            if (success)
            {
                ShowStatus("✓ نجح الاتصال بقاعدة البيانات!", true);
            }
            else
            {
                ShowStatus("✗ فشل الاتصال بقاعدة البيانات", false);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"✗ خطأ في الاتصال: {ex.Message}", false);
        }
    }

    /// <summary>
    /// Test database connection
    /// </summary>
    private bool TestDatabaseConnection(string? dbType, string connectionString)
    {
        try
        {
            switch (dbType)
            {
                case "SQL Server":
                    // يمكن إضافة دعم SQL Server هنا
                    ShowStatus("نوع قاعدة البيانات غير مدعوم حالياً للاختبار", false);
                    return false;

                // يمكن إضافة دعم لأنواع أخرى هنا
                default:
                    ShowStatus("نوع قاعدة البيانات غير مدعوم حالياً للاختبار", false);
                    return false;
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// حفظ الاتصال
    /// </summary>
    private void SaveConnection_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var connectionNameTextBox = this.FindControl<TextBox>("ConnectionNameTextBox");
            var databaseTypeComboBox = this.FindControl<ComboBox>("DatabaseTypeComboBox");
            var serverTextBox = this.FindControl<TextBox>("ServerTextBox");
            var portTextBox = this.FindControl<TextBox>("PortTextBox");
            var databaseNameTextBox = this.FindControl<TextBox>("DatabaseNameTextBox");
            var usernameTextBox = this.FindControl<TextBox>("UsernameTextBox");
            var passwordBox = this.FindControl<TextBox>("PasswordBox");
            var connectionStringTextBox = this.FindControl<TextBox>("ConnectionStringTextBox");

            var dbType = (databaseTypeComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var connectionString = string.IsNullOrWhiteSpace(connectionStringTextBox?.Text)
                ? BuildConnectionString(dbType)
                : connectionStringTextBox?.Text ?? "";

            var connection = new DatabaseConnection
            {
                Name = connectionNameTextBox?.Text ?? "",
                DatabaseType = dbType ?? "SQL Server",
                Server = serverTextBox?.Text ?? "",
                Port = portTextBox?.Text ?? "",
                DatabaseName = databaseNameTextBox?.Text ?? "",
                Username = usernameTextBox?.Text ?? "",
                Password = passwordBox?.Text ?? "", // ملاحظة: يجب تشفير كلمة المرور في الإنتاج
                ConnectionString = connectionString
            };

            // تحديث أو إضافة
            var existing = _connections.FirstOrDefault(c => c.Name == connection.Name);
            if (existing != null)
            {
                _connections.Remove(existing);
            }

            _connections.Add(connection);
            SaveConnections();
            UpdateConnectionsList();

            ShowStatus("✓ تم حفظ الاتصال بنجاح", true);
        }
        catch (Exception ex)
        {
            ShowStatus($"✗ خطأ في حفظ الاتصال: {ex.Message}", false);
        }
    }

    /// <summary>
    /// اتصال جديد
    /// </summary>
    private void NewConnection_Click(object? sender, RoutedEventArgs e)
    {
        var connectionNameTextBox = this.FindControl<TextBox>("ConnectionNameTextBox");
        var serverTextBox = this.FindControl<TextBox>("ServerTextBox");
        var portTextBox = this.FindControl<TextBox>("PortTextBox");
        var databaseNameTextBox = this.FindControl<TextBox>("DatabaseNameTextBox");
        var usernameTextBox = this.FindControl<TextBox>("UsernameTextBox");
        var passwordBox = this.FindControl<TextBox>("PasswordBox");
        var connectionStringTextBox = this.FindControl<TextBox>("ConnectionStringTextBox");
        var databaseTypeComboBox = this.FindControl<ComboBox>("DatabaseTypeComboBox");

        if (connectionNameTextBox != null) connectionNameTextBox.Text = "اتصال جديد";
        if (serverTextBox != null) serverTextBox.Text = "localhost";
        if (portTextBox != null) portTextBox.Text = "1433";
        if (databaseNameTextBox != null) databaseNameTextBox.Text = "";
        if (usernameTextBox != null) usernameTextBox.Text = "";
        if (passwordBox != null) passwordBox.Text = "";
        if (connectionStringTextBox != null) connectionStringTextBox.Text = "";
        if (databaseTypeComboBox != null) databaseTypeComboBox.SelectedIndex = 0;
    }

    /// <summary>
    /// حذف الاتصال
    /// </summary>
    private async void DeleteConnection_Click(object? sender, RoutedEventArgs e)
    {
        var connectionsList = this.FindControl<ListBox>("ConnectionsList");
        if (connectionsList?.SelectedIndex >= 0)
        {
            // يمكن إضافة حوار تأكيد هنا
            _connections.RemoveAt(connectionsList.SelectedIndex);
            SaveConnections();
            UpdateConnectionsList();
            ShowStatus("تم حذف الاتصال", true);
        }
    }

    /// <summary>
    /// تحديد اتصال من القائمة
    /// </summary>
    private void ConnectionsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var connectionsList = sender as ListBox;
        if (connectionsList?.SelectedIndex >= 0 && connectionsList.SelectedIndex < _connections.Count)
        {
            var conn = _connections[connectionsList.SelectedIndex];

            var connectionNameTextBox = this.FindControl<TextBox>("ConnectionNameTextBox");
            var serverTextBox = this.FindControl<TextBox>("ServerTextBox");
            var portTextBox = this.FindControl<TextBox>("PortTextBox");
            var databaseNameTextBox = this.FindControl<TextBox>("DatabaseNameTextBox");
            var usernameTextBox = this.FindControl<TextBox>("UsernameTextBox");
            var passwordBox = this.FindControl<TextBox>("PasswordBox");
            var connectionStringTextBox = this.FindControl<TextBox>("ConnectionStringTextBox");
            var databaseTypeComboBox = this.FindControl<ComboBox>("DatabaseTypeComboBox");

            if (connectionNameTextBox != null) connectionNameTextBox.Text = conn.Name;
            if (serverTextBox != null) serverTextBox.Text = conn.Server;
            if (portTextBox != null) portTextBox.Text = conn.Port;
            if (databaseNameTextBox != null) databaseNameTextBox.Text = conn.DatabaseName;
            if (usernameTextBox != null) usernameTextBox.Text = conn.Username;
            if (passwordBox != null) passwordBox.Text = conn.Password;
            if (connectionStringTextBox != null) connectionStringTextBox.Text = conn.ConnectionString;

            // تحديد نوع قاعدة البيانات
            if (databaseTypeComboBox != null)
            {
                for (int i = 0; i < databaseTypeComboBox.Items.Count; i++)
                {
                    if ((databaseTypeComboBox.Items[i] as ComboBoxItem)?.Content?.ToString() == conn.DatabaseType)
                    {
                        databaseTypeComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// عرض حالة العملية
    /// </summary>
    private void ShowStatus(string message, bool success)
    {
        var statusBorder = this.FindControl<Border>("StatusBorder");
        var statusTextBlock = this.FindControl<TextBlock>("StatusTextBlock");

        if (statusBorder != null && statusTextBlock != null)
        {
            statusBorder.IsVisible = true;
            statusTextBlock.Text = message;
            statusBorder.BorderBrush = new SolidColorBrush(
                success ? Color.FromRgb(16, 124, 16) : Color.FromRgb(232, 17, 35)
            );
        }
    }

    /// <summary>
    /// إغلاق النافذة
    /// </summary>
    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        
    }
}
/// <summary>
/// كلاس لتخزين معلومات الاتصال
/// </summary>
public class DatabaseConnection
{
    public string Name { get; set; } = "";
    public string DatabaseType { get; set; } = "";
    public string Server { get; set; } = "";
    public string Port { get; set; } = "";
    public string DatabaseName { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = ""; // يجب تشفيرها في الإنتاج
    public string ConnectionString { get; set; } = "";
}