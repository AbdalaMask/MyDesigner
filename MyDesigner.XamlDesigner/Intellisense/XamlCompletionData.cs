using Avalonia.Media.Imaging;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using Avalonia.Media;
using System;

namespace MyDesigner.XamlDesigner.Intellisense;

/// <summary>
/// Auto-completion data for XAML
/// </summary>
public class XamlCompletionData : ICompletionData
{
    private IImage? _image;

    public XamlCompletionData(string text, string description = "", CompletionKind kind = CompletionKind.Property)
    {
        Text = text;
        Description = description;
        Kind = kind;
        _image = GetImageForKind(kind);
    }

    public IImage? Image => _image;

    public string Text { get; }

    public object Content => Text;

    public object Description { get; }

    public CompletionKind Kind { get; }

    public double Priority => 1.0;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, Text);
    }

    private IImage? GetImageForKind(CompletionKind kind)
    {
        try
        {
            return kind switch
            {
                CompletionKind.Class => GetBitmapImage("avares://MyDesigner.XamlDesigner.Avalonia/Assets/Icons/Class.png"),
                CompletionKind.Property => GetBitmapImage("avares://MyDesigner.XamlDesigner.Avalonia/Assets/Icons/Property.png"),
                CompletionKind.Method => GetBitmapImage("avares://MyDesigner.XamlDesigner.Avalonia/Assets/Icons/Method.png"),
                CompletionKind.Keyword => GetBitmapImage("avares://MyDesigner.XamlDesigner.Avalonia/Assets/Icons/Enum.png"),
                CompletionKind.Field => GetBitmapImage("avares://MyDesigner.XamlDesigner.Avalonia/Assets/Icons/Field.png"),
                CompletionKind.Variable => GetBitmapImage("avares://MyDesigner.XamlDesigner.Avalonia/Assets/Icons/Literal.png"),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private IImage? GetBitmapImage(string uri)
    {
        try
        {
            return new Bitmap(uri);
        }
        catch
        {
            return null;
        }
    }
}

public enum CompletionKind
{
    Keyword,
    Class,
    Method,
    Property,
    Field,
    Variable
}