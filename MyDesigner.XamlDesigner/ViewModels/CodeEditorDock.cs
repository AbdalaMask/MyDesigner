using System;

namespace MyDesigner.XamlDesigner.ViewModels
{
    // Code Editor dockable wrapper
    public class CodeEditorDock : Dock.Model.Mvvm.Controls.Document
    {
        public Document Document { get; }

        public CodeEditorDock(Document document)
        {
            Document = document;
            Id = $"CodeEditor_{document.Name}_{Guid.NewGuid():N}";
            Title = $"{document.Name} (Code)";
            
            // Set the Document as the context for the CodeEditorView
            Context = document;
        }

        public override bool OnClose()
        {
            // إشعار Shell بإغلاق المستند
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CodeEditorDock.OnClose] Closing document: {Document.FilePath}");
                return Shell.Instance.Close(Document);
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
                return false;
            }
        }
    }
}