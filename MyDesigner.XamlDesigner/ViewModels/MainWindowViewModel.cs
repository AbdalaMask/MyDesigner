using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Core;
using MyDesigner.XamlDesigner.Services;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace MyDesigner.XamlDesigner.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public static Services.DialogService dialogService = new Services.DialogService();

        [ObservableProperty]
        public string title;

        [ObservableProperty]
        private ObservableCollection<Document> documents;

        [ObservableProperty]
        private ObservableCollection<string> recentFiles;

        [ObservableProperty]
        private Document currentDocument;

        [ObservableProperty]
        private IDock? layout;

        private DockFactory? _dockFactory;
        [ObservableProperty]
        private bool isServerMode;

        public MainWindowViewModel()
        {
            // Initialize from Shell
            Documents = Shell.Instance.Documents;
            RecentFiles = Shell.Instance.RecentFiles;
            Title = Shell.Instance.Title;

            // Create dock factory and layout
            _dockFactory = new DockFactory(this);
            Layout = _dockFactory.CreateLayout();
            _dockFactory.InitLayout(Layout);

            // Expose server mode to view bindings
            IsServerMode = Program.IsServerMode;

            // Navigate to Home view like DockMvvmSample
            if (Layout is { } root)
            {
                root.Navigate.Execute("Home");
            }

            // Subscribe to Shell property changes
            Shell.Instance.PropertyChanged += Shell_PropertyChanged;

            // Subscribe to Documents collection changes
            Documents.CollectionChanged += Documents_CollectionChanged;

            // Add existing documents to dock
            foreach (var document in Documents)
            {
                _dockFactory.AddDocument(document);
            }

            // Initialize commands
            InitializeCommands();
        }

        private void Shell_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Shell.Title):
                    Title = Shell.Instance.Title;
                    break;
                case nameof(Shell.CurrentDocument):
                    CurrentDocument = Shell.Instance.CurrentDocument;
                    break;
            }
        }

        private void InitializeCommands()
        {
            // File Commands
            NewCommand = new RelayCommand(Shell.Instance.New);
            OpenCommand = new RelayCommand(Shell.Instance.Open);
            SaveCommand = new RelayCommand(Shell.Instance.SaveCurrentDocument, () => Shell.Instance.CurrentDocument != null);
            SaveAsCommand = new RelayCommand(Shell.Instance.SaveCurrentDocumentAs, () => Shell.Instance.CurrentDocument != null);
            SaveAllCommand = new RelayCommand(() => Shell.Instance.SaveAll(), () => Shell.Instance.CurrentDocument != null);
            CloseCommand = new RelayCommand(Shell.Instance.CloseCurrentDocument, () => Shell.Instance.CurrentDocument != null);
            CloseAllCommand = new RelayCommand(() => Shell.Instance.CloseAll(), () => Shell.Instance.CurrentDocument != null);
            ExitCommand = new RelayCommand(Shell.Instance.Exit);

            // Edit Commands
            UndoCommand = new RelayCommand(Shell.Instance.Undo, () => Shell.Instance.CurrentDocument != null);
            RedoCommand = new RelayCommand(Shell.Instance.Redo, () => Shell.Instance.CurrentDocument != null);
            CutCommand = new RelayCommand(Shell.Instance.Cut, () => Shell.Instance.CurrentDocument != null);
            CopyCommand = new RelayCommand(Shell.Instance.Copy, () => Shell.Instance.CurrentDocument != null);
            PasteCommand = new RelayCommand(Shell.Instance.Paste, () => Shell.Instance.CurrentDocument != null);
            DeleteCommand = new RelayCommand(Shell.Instance.Delete, () => Shell.Instance.CurrentDocument != null);
            SelectAllCommand = new RelayCommand(Shell.Instance.SelectAll, () => Shell.Instance.CurrentDocument != null);
            FindCommand = new RelayCommand(Shell.Instance.Find, () => Shell.Instance.CurrentDocument != null);
            RefreshCommand = new RelayCommand(Shell.Instance.Refresh, () => Shell.Instance.CanRefresh());

            // Test Commands
            RunCommand = new RelayCommand(Shell.Instance.Run, () => Shell.Instance.CurrentDocument != null);
            
        }

        // File Commands
        public RelayCommand NewCommand { get; private set; }
        public RelayCommand OpenCommand { get; private set; }
        public RelayCommand SaveCommand { get; private set; }
        public RelayCommand SaveAsCommand { get; private set; }
        public RelayCommand SaveAllCommand { get; private set; }
        public RelayCommand CloseCommand { get; private set; }
        public RelayCommand CloseAllCommand { get; private set; }
        public RelayCommand ExitCommand { get; private set; }

        // Edit Commands
        public RelayCommand UndoCommand { get; private set; }
        public RelayCommand RedoCommand { get; private set; }
        public RelayCommand CutCommand { get; private set; }
        public RelayCommand CopyCommand { get; private set; }
        public RelayCommand PasteCommand { get; private set; }
        public RelayCommand DeleteCommand { get; private set; }
        public RelayCommand SelectAllCommand { get; private set; }
        public RelayCommand FindCommand { get; private set; }
        public RelayCommand RefreshCommand { get; private set; }

        // Test Commands
        public RelayCommand RunCommand { get; private set; }
     

        public RelayCommand BuildCommand { get; private set; }

        public RelayCommand RebuildCommand { get; private set; }

        public RelayCommand StopCommand { get; private set; }


        public RelayCommand MinimizeCommand { get; private set; }

        public RelayCommand MaximizeCommand { get; private set; }

        public RelayCommand GenerateProjectCommand { get; private set; }
        
        // Convert To
        public RelayCommand SaveAsAvaloniaCommand { get; private set; }
        
        public RelayCommand SaveAsMauiCommand { get; private set; }
        public RelayCommand SaveAsUnoCommand { get; private set; }
        

        [RelayCommand]
        private void OpenRecentFile(string filePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath))
                {
                    Shell.Instance.Open(filePath);
                }
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        [RelayCommand]
        private static async void NewProject()
        {
            try
            {

                var newProjectDialog = new NewProjectWindow();
                var result = await dialogService.ShowDialogAsync(newProjectDialog);

                //if (result == true)
                //{

                //    await dialogService.ShowMessageAsync("�� ����� ����� ���� �����!", "��� - Success");
                //}
            }
            catch (Exception ex)
            {
                var dialogService = new Services.DialogService();
                await dialogService.ShowErrorAsync($"��� �� ����� ����� ����: {ex.Message}", "��� - Error");
            }
        }
        [RelayCommand]
        private async void OpenProject()
        {
            try
            {
                // ��� ����� ������ ��� ����� �������
                var dialog = new OpenProjectDialog();
                var result = await dialogService.ShowDialogAsync<OpenProjectDialog>(dialog);

                if (result != null)
                {
                    var choice = result.SelectedOption;

                    HandleOpenChoice(choice);
                }
            }
            catch (Exception ex)
            {
                var dialogService = new Services.DialogService();
                await dialogService.ShowErrorAsync($"��� �� ��� �������: {ex.Message}", "��� - Error");
            }
        }
        partial void OnCurrentDocumentChanged(Document value)
        {
            // Update Shell's current document
            if (Shell.Instance.CurrentDocument != value)
            {
                Shell.Instance.CurrentDocument = value;
            }

            // Refresh command states
            RefreshCommandStates();
        }

        private void Documents_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_dockFactory == null)
            {
                System.Diagnostics.Debug.WriteLine("DockFactory is null in Documents_CollectionChanged");
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (Document document in e.NewItems)
                {
                    System.Diagnostics.Debug.WriteLine($"Adding document to dock: {document.Name}");
                    _dockFactory.AddDocument(document);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (Document document in e.OldItems)
                {
                    System.Diagnostics.Debug.WriteLine($"Removing document from dock: {document.Name}");
                    _dockFactory.RemoveDocument(document);
                }
            }

            // Refresh command states after document changes
            RefreshCommandStates();
        }

        private void RefreshCommandStates()
        {
            // Trigger CanExecuteChanged for all commands
            if (SaveCommand is RelayCommand saveCmd) saveCmd.NotifyCanExecuteChanged();
            if (SaveAsCommand is RelayCommand saveAsCmd) saveAsCmd.NotifyCanExecuteChanged();
            if (SaveAllCommand is RelayCommand saveAllCmd) saveAllCmd.NotifyCanExecuteChanged();
            if (CloseCommand is RelayCommand closeCmd) closeCmd.NotifyCanExecuteChanged();
            if (CloseAllCommand is RelayCommand closeAllCmd) closeAllCmd.NotifyCanExecuteChanged();

            if (UndoCommand is RelayCommand undoCmd) undoCmd.NotifyCanExecuteChanged();
            if (RedoCommand is RelayCommand redoCmd) redoCmd.NotifyCanExecuteChanged();
            if (CutCommand is RelayCommand cutCmd) cutCmd.NotifyCanExecuteChanged();
            if (CopyCommand is RelayCommand copyCmd) copyCmd.NotifyCanExecuteChanged();
            if (PasteCommand is RelayCommand pasteCmd) pasteCmd.NotifyCanExecuteChanged();
            if (DeleteCommand is RelayCommand deleteCmd) deleteCmd.NotifyCanExecuteChanged();
            if (SelectAllCommand is RelayCommand selectAllCmd) selectAllCmd.NotifyCanExecuteChanged();
            if (FindCommand is RelayCommand findCmd) findCmd.NotifyCanExecuteChanged();
            if (RefreshCommand is RelayCommand refreshCmd) refreshCmd.NotifyCanExecuteChanged();

            if (RunCommand is RelayCommand runCmd) runCmd.NotifyCanExecuteChanged();
            if (NewProjectCommand is RelayCommand renderCmd) renderCmd.NotifyCanExecuteChanged();
        }

        #region Page Actions - دوال الصفحات

        /// <summary>
        /// إنشاء ملف جديد في المشروع
        /// </summary>
        [RelayCommand]
        private void CreateNewFile()
        {
            Core.PageActions.CreateNewFile();
        }

        /// <summary>
        /// إنشاء مجلد جديد في المشروع
        /// </summary>
        [RelayCommand]
        private void CreateNewFolder()
        {
            Core.PageActions.CreateNewFolder();
        }

        /// <summary>
        /// حذف العنصر المحدد
        /// </summary>
        [RelayCommand]
        private void DeleteSelectedItem()
        {
            Core.PageActions.DeleteSelectedItem();
        }

        /// <summary>
        /// إعادة تسمية العنصر المحدد
        /// </summary>
        [RelayCommand]
        private void RenameSelectedItem()
        {
            Core.PageActions.RenameSelectedItem();
        }

        /// <summary>
        /// بناء المشروع
        /// </summary>
        [RelayCommand]
        private void BuildProjectFromMain()
        {
            Core.PageActions.BuildProject();
        }

        /// <summary>
        /// تشغيل المشروع
        /// </summary>
        [RelayCommand]
        private void RunProjectFromMain()
        {
            Core.PageActions.RunProject();
        }

        /// <summary>
        /// فتح مشروع موجود
        /// </summary>
        [RelayCommand]
        private async Task OpenProjectFromMain()
        {
            await Core.PageActions.OpenProject();
        }

        /// <summary>
        /// فتح مجلد مشروع
        /// </summary>
        [RelayCommand]
        private async Task OpenFolderFromMain()
        {
            await Core.PageActions.OpenFolderDialog();
        }

        /// <summary>
        /// فتح ملف واحد
        /// </summary>
        [RelayCommand]
        private async Task OpenFileFromMain()
        {
            await Core.PageActions.OpenFile();
        }

        /// <summary>
        /// معالج الخيارات المختلفة للفتح
        /// </summary>
        public async Task HandleOpenChoice(string choice)
        {
            try
            {
                switch (choice)
                {
                    case "project":
                        await Core.PageActions.OpenProject();
                        break;
                    case "folder":
                        await Core.PageActions.OpenFolderDialog();
                        break;
                    case "file":
                        await Core.PageActions.OpenFile();
                        break;
                    default:
                        Console.WriteLine($"خيار غير معروف: {choice}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// تحديث عرض المشروع
        /// </summary>
        [RelayCommand]
        private void RefreshProjectView()
        {
            Core.PageActions.RefreshProjectView();
        }

        /// <summary>
        /// تحميل ملفات المشروع
        /// </summary>
        [RelayCommand]
        private void LoadProjectFiles(string projectPath)
        {
            Core.PageActions.LoadFilesToSolution(projectPath);
        }

        /// <summary>
        /// إغلاق جميع المستندات
        /// </summary>
        [RelayCommand]
        private void CloseAllDocuments()
        {
            Core.PageActions.CloseAllDocuments();
        }

        /// <summary>
        /// تحديث شبكة الخصائص
        /// </summary>
        [RelayCommand]
        private void RefreshPropertyGrid()
        {
            Core.PageActions.RefreshPropertyGrid();
        }

        /// <summary>
        /// مسح شبكة الخصائص
        /// </summary>
        [RelayCommand]
        private void ClearPropertyGrid()
        {
            Core.PageActions.ClearPropertyGrid();
        }

        /// <summary>
        /// تحديث قائمة الأخطاء
        /// </summary>
        [RelayCommand]
        private void RefreshErrorList()
        {
            Core.PageActions.RefreshErrorList();
        }

        /// <summary>
        /// مسح قائمة الأخطاء
        /// </summary>
        [RelayCommand]
        private void ClearErrorList()
        {
            Core.PageActions.ClearErrorList();
        }

        /// <summary>
        /// تحديث صندوق الأدوات
        /// </summary>
        [RelayCommand]
        private void RefreshToolbox()
        {
            Core.PageActions.RefreshToolbox();
        }

        /// <summary>
        /// إضافة تجميعة إلى صندوق الأدوات
        /// </summary>
        [RelayCommand]
        private void AddAssemblyToToolbox(string assemblyPath)
        {
            Core.PageActions.AddAssemblyToToolbox(assemblyPath);
        }

        /// <summary>
        /// تحديث المخطط التفصيلي
        /// </summary>
        [RelayCommand]
        private void RefreshOutline()
        {
            Core.PageActions.RefreshOutline();
        }

        /// <summary>
        /// توسيع جميع عقد المخطط
        /// </summary>
        [RelayCommand]
        private void ExpandAllOutlineNodes()
        {
            Core.PageActions.ExpandAllOutlineNodes();
        }

        /// <summary>
        /// طي جميع عقد المخطط
        /// </summary>
        [RelayCommand]
        private void CollapseAllOutlineNodes()
        {
            Core.PageActions.CollapseAllOutlineNodes();
        }

        /// <summary>
        /// تحديث المستند الحالي
        /// </summary>
        [RelayCommand]
        private void RefreshCurrentDocument()
        {
            Core.PageActions.RefreshCurrentDocument();
        }

        #endregion



    }
}