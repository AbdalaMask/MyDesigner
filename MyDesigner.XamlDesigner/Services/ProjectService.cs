using System;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using System.IO;
using MyDesigner.XamlDesigner.Views;

namespace MyDesigner.XamlDesigner.Services
{
    public class ProjectService
    {
        private static ProjectService? _instance;
        public static ProjectService Instance => _instance ??= new ProjectService();

        // Event للإشعار بتحميل مشروع جديد
        public event Action<string>? ProjectLoaded;
        public event Action<string>? ProjectOpened;
        public event Action? ProjectClosed;
        
        // Reference إلى ProjectExplorerView الحالي
        private ProjectExplorerView? _projectExplorerView;

        public void RegisterProjectExplorer(ProjectExplorerView projectExplorer)
        {
            _projectExplorerView = projectExplorer;
        }

        public async Task<bool> OpenProjectAsync(IStorageProvider storageProvider)
        {
            try
            {
                if (_projectExplorerView == null)
                {
                    throw new InvalidOperationException("ProjectExplorerView is not registered");
                }

                // استخدام دالة OpenFolder الموجودة في ProjectExplorerView
                _projectExplorerView.OpenFolder();
                
                // إشعار بفتح المشروع
                var currentPath = Configuration.Settings.Default.ProjectPath;
                if (!string.IsNullOrEmpty(currentPath))
                {
                    ProjectOpened?.Invoke(currentPath);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في فتح المشروع: {ex.Message}");
                return false;
            }
        }

        public void LoadProject(string projectPath)
        {
            try
            {
                if (_projectExplorerView == null)
                {
                    throw new InvalidOperationException("ProjectExplorerView is not registered");
                }

                if (!Directory.Exists(projectPath))
                {
                    throw new DirectoryNotFoundException($"مسار المشروع غير موجود: {projectPath}");
                }

                // استخدام دالة LoadFilesToSolution الموجودة
              //  _projectExplorerView.LoadFilesToSolution(projectPath);
                
                // إشعار المكونات الأخرى بتحميل المشروع
                ProjectLoaded?.Invoke(projectPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في تحميل المشروع: {ex.Message}");
                throw; // إعادة رمي الاستثناء للتعامل معه في الطبقة العليا
            }
        }

        public void RefreshProject()
        {
            try
            {
                if (_projectExplorerView == null) return;

                // إعادة تحميل المشروع الحالي
                var currentPath = Configuration.Settings.Default.ProjectPath;
                if (!string.IsNullOrEmpty(currentPath))
                {
                    LoadProject(currentPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في تحديث المشروع: {ex.Message}");
                throw;
            }
        }

        public void CloseProject()
        {
            try
            {
                if (_projectExplorerView == null) return;

                // إغلاق جميع المستندات
                _projectExplorerView.CloseAllDocuments();
                
                // مسح إعدادات المشروع
                Configuration.Settings.Default.ProjectPath = string.Empty;
                Configuration.Settings.Default.ProjectName = string.Empty;
                Configuration.Settings.Default.Save();
                
                // إشعار بإغلاق المشروع
                ProjectClosed?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في إغلاق المشروع: {ex.Message}");
            }
        }

        public bool IsProjectLoaded => !string.IsNullOrEmpty(Configuration.Settings.Default.ProjectPath);
        
        public string? CurrentProjectPath => Configuration.Settings.Default.ProjectPath;
        
        public string? CurrentProjectName => Configuration.Settings.Default.ProjectName;

        public bool CanExecuteProjectCommands => IsProjectLoaded && _projectExplorerView != null;
    }
}