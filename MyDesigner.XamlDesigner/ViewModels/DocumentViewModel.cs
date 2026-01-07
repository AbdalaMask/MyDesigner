using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using System;
using System.ComponentModel;
using MyDesigner.Designer;

namespace MyDesigner.XamlDesigner.ViewModels
{
    public class HomeViewModel : RootDock
    {
    }
    public partial class DocumentViewModel : ViewModelBase
    {
        private Document document;
        [ObservableProperty]
        private DocumentMode mode;
        [ObservableProperty]
        private string xamlText;

        [ObservableProperty]
        private bool inDesignMode;

        [ObservableProperty]
        private bool inCodeMode;

        [ObservableProperty]
        private bool inXamlMode;

        [ObservableProperty]
        private string title;

        [ObservableProperty]
        private bool isDirty;
        [ObservableProperty]
        private DesignSurface designSurface = new DesignSurface();

        public DocumentViewModel(Document document)
        {
            this.document = document ?? throw new ArgumentNullException(nameof(document));
            
            // Initialize properties from document
            XamlText = document.Text;
            InDesignMode = document.InDesignMode;
            InXamlMode = document.InXamlMode;
            inCodeMode= document.InCodeMode;
            Title = document.Title;
            IsDirty = document.IsDirty;
            designSurface=document.DesignSurface;
            
            // Initialize DocumentMode based on current mode
            if (document.InDesignMode)
                Mode = DocumentMode.Design;
            else if (document.InXamlMode)
                Mode = DocumentMode.Xaml;
            else
                Mode = DocumentMode.Code;
            
            // Sync with document's Mode property
            document.Mode = Mode;
                
            // Subscribe to document changes
            document.PropertyChanged += Document_PropertyChanged;
            
            // Initialize commands
            SwitchToDesignModeCommand = new RelayCommand(SwitchToDesignMode);
            SwitchToXamlModeCommand = new RelayCommand(SwitchToXamlMode);
        }

        public Document Document => document;

        public RelayCommand SwitchToDesignModeCommand { get; }
        public RelayCommand SwitchToXamlModeCommand { get; }

        private void Document_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Document.Text):
                    if (XamlText != document.Text)
                        XamlText = document.Text;
                    break;
                case nameof(Document.InDesignMode):
                    if (InDesignMode != document.InDesignMode)
                        InDesignMode = document.InDesignMode;
                    break;
                case nameof(Document.InXamlMode):
                    if (InXamlMode != document.InXamlMode)
                        InXamlMode = document.InXamlMode;
                    break;
                case nameof(Document.Mode):
                    if (Mode != document.Mode)
                        Mode = document.Mode;
                    break;
                case nameof(Document.Title):
                    if (Title != document.Title)
                        Title = document.Title;
                    break;
                case nameof(Document.IsDirty):
                    if (IsDirty != document.IsDirty)
                        IsDirty = document.IsDirty;
                    break;
            }
        }

        partial void OnModeChanged(DocumentMode value)
        {
            System.Diagnostics.Debug.WriteLine($"DocumentViewModel.OnModeChanged: {value}");
            
            // Update the underlying document's mode
            if (document.Mode != value)
            {
                document.Mode = value;
            }
            
            // Prevent infinite loop by checking current state
            switch (value)
            {
                case DocumentMode.Design:
                    if (!InDesignMode)
                    {
                        System.Diagnostics.Debug.WriteLine("Setting InDesignMode = true, InXamlMode = false");
                        InDesignMode = true;
                        InXamlMode = false;
                    }
                    break;
                case DocumentMode.Xaml:
                    if (!InXamlMode)
                    {
                        System.Diagnostics.Debug.WriteLine("Setting InDesignMode = false, InXamlMode = true");
                        InDesignMode = false;
                        InXamlMode = true;
                    }
                    break;
                case DocumentMode.Code:
                    if (InDesignMode || InXamlMode)
                    {
                        System.Diagnostics.Debug.WriteLine("Setting InDesignMode = false, InXamlMode = false");
                        InDesignMode = false;
                        InXamlMode = false;
                    }
                    break;
            }
        }

        partial void OnXamlTextChanged(string value)
        {
            if (document.Text != value)
            {
                document.Text = value;
            }
        }

        partial void OnInDesignModeChanged(bool value)
        {
            if (value && Mode != DocumentMode.Design)
            {
                Mode = DocumentMode.Design;
            }
        }

        partial void OnInXamlModeChanged(bool value)
        {
            if (value && Mode != DocumentMode.Xaml)
            {
                Mode = DocumentMode.Xaml;
            }
        }

        private void SwitchToDesignMode()
        {
            try
            {
                InDesignMode = true;
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        private void SwitchToXamlMode()
        {
            try
            {
                InXamlMode = true;
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }
    }
}