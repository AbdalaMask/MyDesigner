using System;
using System.Threading.Tasks;

namespace MyDesigner.XamlDesigner.Core
{
    /// <summary>
    /// دوال مباشرة للوصول إلى دوال الصفحات المختلفة
    /// Direct functions to access different page functions
    /// </summary>
    public static class PageActions
    {
        #region ProjectExplorer Actions 

        /// <summary>
        /// فتح مجلد مشروع
        /// </summary>
        public static void OpenProjectFolder()
        {
            try
            {
                PageRegistry.ProjectExplorer?.OpenFolder();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// تحميل ملفات إلى الحل
        /// </summary>
        public static void LoadFilesToSolution(string projectPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(projectPath))
                {
                  //  PageRegistry.ProjectExplorer?.LoadFilesToSolution(projectPath);
                }
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// فتح مشروع موجود
        /// </summary>
        public static async Task OpenProject()
        {
            try
            {
                if (PageRegistry.ProjectExplorer != null)
                {
                    await PageRegistry.ProjectExplorer.OpenProject();
                }
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// فتح مجلد مشروع
        /// </summary>
        public static async Task OpenFolderDialog()
        {
            try
            {
                if (PageRegistry.ProjectExplorer != null)
                {
                    await PageRegistry.ProjectExplorer.OpenFolderDialog();
                }
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// فتح ملف واحد
        /// </summary>
        public static async Task OpenFile()
        {
            try
            {
                if (PageRegistry.ProjectExplorer != null)
                {
                   // await PageRegistry.ProjectExplorer.OpenFile();
                }
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// إنشاء ملف جديد
        /// </summary>
        public static void CreateNewFile()
        {
            try
            {
              //  PageRegistry.ProjectExplorer?.NewFile();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// إنشاء مجلد جديد
        /// </summary>
        public static void CreateNewFolder()
        {
            try
            {
              //  PageRegistry.ProjectExplorer?.NewFolder();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// حذف العنصر المحدد
        /// </summary>
        public static void DeleteSelectedItem()
        {
            try
            {
              //  PageRegistry.ProjectExplorer?.DeleteSelectedItem();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// إعادة تسمية العنصر المحدد
        /// </summary>
        public static void RenameSelectedItem()
        {
            try
            {
              //  PageRegistry.ProjectExplorer?.RenameSelectedItem();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// تحديث عرض المشروع
        /// </summary>
        public static void RefreshProjectView()
        {
            try
            {
              //  PageRegistry.ProjectExplorer?.RefreshView();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// إغلاق جميع المستندات
        /// </summary>
        public static void CloseAllDocuments()
        {
            try
            {
                PageRegistry.ProjectExplorer?.CloseAllDocuments();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// بناء المشروع
        /// </summary>
        public static void BuildProject()
        {
            try
            {
              //  PageRegistry.ProjectExplorer?.BuildProject();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// تشغيل المشروع
        /// </summary>
        public static void RunProject()
        {
            try
            {
               // PageRegistry.ProjectExplorer?.RunProject();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        #endregion

        #region PropertyGrid Actions 

        /// <summary>
        /// RefreshPropertyGrid
        /// </summary>
        public static void RefreshPropertyGrid()
        {
            try
            {
              //  PageRegistry.PropertyGrid?.RefreshProperties();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// مسح شبكة الخصائص
        /// </summary>
        public static void ClearPropertyGrid()
        {
            try
            {
              //  PageRegistry.PropertyGrid?.ClearProperties();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        #endregion

        #region ErrorList Actions - دوال قائمة الأخطاء

        /// <summary>
        /// تحديث قائمة الأخطاء
        /// </summary>
        public static void RefreshErrorList()
        {
            try
            {
              //  PageRegistry.ErrorList?.RefreshErrors();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// مسح قائمة الأخطاء
        /// </summary>
        public static void ClearErrorList()
        {
            try
            {
               // PageRegistry.ErrorList?.ClearErrors();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// الانتقال إلى الخطأ المحدد
        /// </summary>
        public static void GoToError(int errorIndex)
        {
            try
            {
               // PageRegistry.ErrorList?.GoToError(errorIndex);
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        #endregion

        #region Toolbox Actions - دوال صندوق الأدوات

        /// <summary>
        /// تحديث صندوق الأدوات
        /// </summary>
        public static void RefreshToolbox()
        {
            try
            {
               // PageRegistry.Toolbox?.RefreshToolbox();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// إضافة تجميعة إلى صندوق الأدوات
        /// </summary>
        public static void AddAssemblyToToolbox(string assemblyPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(assemblyPath))
                {
                   // PageRegistry.Toolbox?.AddAssembly(assemblyPath);
                }
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        #endregion

        #region Outline Actions - دوال المخطط التفصيلي

        /// <summary>
        /// تحديث المخطط التفصيلي
        /// </summary>
        public static void RefreshOutline()
        {
            try
            {
              //  PageRegistry.Outline?.RefreshOutline();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// توسيع جميع العقد في المخطط
        /// </summary>
        public static void ExpandAllOutlineNodes()
        {
            try
            {
               // PageRegistry.Outline?.ExpandAll();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// طي جميع العقد في المخطط
        /// </summary>
        public static void CollapseAllOutlineNodes()
        {
            try
            {
               // PageRegistry.Outline?.CollapseAll();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        #endregion

        #region MainView Actions - دوال العرض الرئيسي

        /// <summary>
        /// تحديث المستند الحالي
        /// </summary>
        public static void RefreshCurrentDocument()
        {
            try
            {
              //  PageRegistry.MainView?.RefreshDocument();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// تطبيق التغييرات على المستند
        /// </summary>
        public static void ApplyDocumentChanges()
        {
            try
            {
               // PageRegistry.MainView?.ApplyChanges();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// التراجع في المستند
        /// </summary>
        public static void UndoDocumentChange()
        {
            try
            {
              //  PageRegistry.MainView?.Undo();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        /// <summary>
        /// الإعادة في المستند
        /// </summary>
        public static void RedoDocumentChange()
        {
            try
            {
               // PageRegistry.MainView?.Redo();
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        #endregion

        #region Utility Actions - دوال مساعدة

        /// <summary>
        /// التحقق من توفر صفحة معينة
        /// </summary>
        public static bool IsPageAvailable(string pageName)
        {
            return PageRegistry.IsPageAvailable(pageName);
        }

        /// <summary>
        /// تنفيذ دالة آمنة على صفحة معينة
        /// </summary>
        public static void SafeExecute(Action action, string actionName = "Unknown")
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Shell.ReportException(new Exception($"خطأ في تنفيذ {actionName}: {ex.Message}", ex));
            }
        }

        /// <summary>
        /// تنفيذ دالة غير متزامنة آمنة
        /// </summary>
        public static async Task SafeExecuteAsync(Func<Task> action, string actionName = "Unknown")
        {
            try
            {
                if (action != null)
                {
                    await action();
                }
            }
            catch (Exception ex)
            {
                Shell.ReportException(new Exception($"خطأ في تنفيذ {actionName}: {ex.Message}", ex));
            }
        }

        /// <summary>
        /// معالج الخيارات المختلفة للفتح
        /// </summary>
        public static async Task HandleOpenChoice(string choice)
        {
            try
            {
                switch (choice)
                {
                    case "project":
                        await OpenProject();
                        break;
                    case "folder":
                        await OpenFolderDialog();
                        break;
                    case "file":
                        await OpenFile();
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

        #endregion
    }
}