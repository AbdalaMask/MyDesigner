using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using System;


namespace MyDesigner.Common.Controls
{
    /// <summary>
    /// Simple class for auto-completion data - links CompletionItem and ICompletionData
    /// </summary>
    public class SimpleCompletionData : ICompletionData
    {
        private readonly AvalonEditCompletionItem _completionItem;

        public SimpleCompletionData(CompletionItem completionItem)
        {
            _completionItem = AvalonEditCompletionItem.FromCompletionItem(completionItem ?? throw new ArgumentNullException(nameof(completionItem)));
        }
        
        public SimpleCompletionData(AvalonEditCompletionItem completionItem)
        {
            _completionItem = completionItem ?? throw new ArgumentNullException(nameof(completionItem));
        }

        public string Text => _completionItem.Text;

        public object Content => _completionItem.Text;

        public object Description => _completionItem.Description ?? string.Empty;

        public IImage Image => null; // يمكن إضافة أيقونات لاحقاً

        public double Priority => _completionItem.Priority;

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            if (textArea?.Document == null || completionSegment == null)
                return;

            try
            {
                // التأكد من أن النص صحيح
                var replacementText = _completionItem.CompletionText ?? _completionItem.Text;
                if (string.IsNullOrEmpty(replacementText))
                    return;

                // استبدال النص في المنطقة المحددة
                textArea.Document.Replace(completionSegment.Offset, completionSegment.Length, replacementText);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في إكمال النص: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Simple completion item for AvalonEdit compatibility
    /// </summary>
    public class AvalonEditCompletionItem
    {
        public string Text { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CompletionText { get; set; } = string.Empty;
        public double Priority { get; set; } = 0.0;

        public AvalonEditCompletionItem() { }

        public AvalonEditCompletionItem(string text, string description = "", double priority = 0.0)
        {
            Text = text;
            Description = description;
            CompletionText = text;
            Priority = priority;
        }
        
        /// <summary>
        /// Convert from CompletionItem to AvalonEditCompletionItem
        /// </summary>
        public static AvalonEditCompletionItem FromCompletionItem(CompletionItem item)
        {
            return new AvalonEditCompletionItem
            {
                Text = item.Text,
                Description = item.Description,
                CompletionText = item.Text,
                Priority = 1.0
            };
        }
    }
}