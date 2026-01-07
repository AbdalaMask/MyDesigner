using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using DialogHostAvalonia;

namespace MyDesigner.XamlDesigner.Services;

public interface IDialogService
{
    Task ShowMessageAsync(string message, string title = "Information");
    Task ShowErrorAsync(string message, string title = "Error");
    Task ShowWarningAsync(string message, string title = "Warning");
    Task<bool> ShowConfirmationAsync(string message, string title = "Confirmation");
    Task<T> ShowDialogAsync<T>(UserControl content, string identifier = "MainDialogHost");
    Task<object> ShowDialogAsync(UserControl content, string identifier = "MainDialogHost");
}

public class DialogService : IDialogService
{
    public async Task ShowMessageAsync(string message, string title = "Information")
    {
        var dialog = new Views.MessageDialog
        {
            Title = title,
            Message = message,
            DialogType = MessageDialogType.Information
        };

        await DialogHost.Show(dialog, "MainDialogHost");
    }

    public async Task ShowErrorAsync(string message, string title = "Error")
    {
        var dialog = new Views.MessageDialog
        {
            Title = title,
            Message = message,
            DialogType = MessageDialogType.Error
        };

        await DialogHost.Show(dialog, "MainDialogHost");
    }

    public async Task ShowWarningAsync(string message, string title = "Warning")
    {
        var dialog = new Views.MessageDialog
        {
            Title = title,
            Message = message,
            DialogType = MessageDialogType.Warning
        };

        await DialogHost.Show(dialog, "MainDialogHost");
    }

    public async Task<bool> ShowConfirmationAsync(string message, string title = "Confirmation")
    {
        var dialog = new Views.ConfirmationDialog
        {
            Title = title,
            Message = message
        };

        var result = await DialogHost.Show(dialog, "MainDialogHost");
        return result is bool boolResult && boolResult;
    }

    public async Task<T> ShowDialogAsync<T>(UserControl content, string identifier = "MainDialogHost")
    {
        var result = await DialogHost.Show(content, identifier);
        return result is T typedResult ? typedResult : default(T);
    }

    public async Task<object> ShowDialogAsync(UserControl content, string identifier = "MainDialogHost")
    {
        return await DialogHost.Show(content, identifier);
    }
}

public enum MessageDialogType
{
    Information,
    Warning,
    Error,
    Success
}