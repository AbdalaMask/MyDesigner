using System;
using System.Collections.Generic;
using MyDesigner.XamlDesigner.Views;
using MyDesigner.XamlDesigner.Views.Tools;

namespace MyDesigner.XamlDesigner.Core
{
    /// <summary>
    /// سجل بسيط للصفحات للوصول المباشر إلى دوالها
    /// Simple page registry for direct access to page functions
    /// </summary>
    public static class PageRegistry
    {
        // مراجع مباشرة للصفحات
        public static ProjectExplorerView? ProjectExplorer { get; set; }
        public static MainView? MainView { get; set; }
        public static PropertyGridToolView? PropertyGrid { get; set; }
        public static ErrorListToolView? ErrorList { get; set; }
        public static FromToolboxView? Toolbox { get; set; }
        public static OutlineToolView? Outline { get; set; }

        // أحداث للإشعار بالتغييرات
        public static event Action<string>? PageRegistered;
        public static event Action<string>? PageUnregistered;

        /// <summary>
        /// تسجيل صفحة ProjectExplorer
        /// </summary>
        public static void RegisterProjectExplorer(ProjectExplorerView view)
        {
            ProjectExplorer = view;
            PageRegistered?.Invoke("ProjectExplorer");
        }

        /// <summary>
        /// تسجيل صفحة MainView
        /// </summary>
        public static void RegisterMainView(MainView view)
        {
            MainView = view;
            PageRegistered?.Invoke("MainView");
        }

        /// <summary>
        /// تسجيل صفحة PropertyGrid
        /// </summary>
        public static void RegisterPropertyGrid(PropertyGridToolView view)
        {
            PropertyGrid = view;
            PageRegistered?.Invoke("PropertyGrid");
        }

        /// <summary>
        /// تسجيل صفحة ErrorList
        /// </summary>
        public static void RegisterErrorList(ErrorListToolView view)
        {
            ErrorList = view;
            PageRegistered?.Invoke("ErrorList");
        }

        /// <summary>
        /// تسجيل صفحة Toolbox
        /// </summary>
        public static void RegisterToolbox(FromToolboxView view)
        {
            Toolbox = view;
            PageRegistered?.Invoke("Toolbox");
        }

        /// <summary>
        /// تسجيل صفحة Outline
        /// </summary>
        public static void RegisterOutline(OutlineToolView view)
        {
            Outline = view;
            PageRegistered?.Invoke("Outline");
        }

        /// <summary>
        /// إلغاء تسجيل جميع الصفحات
        /// </summary>
        public static void UnregisterAll()
        {
            ProjectExplorer = null;
            MainView = null;
            PropertyGrid = null;
            ErrorList = null;
            Toolbox = null;
            Outline = null;
            
            PageUnregistered?.Invoke("All");
        }

        /// <summary>
        /// التحقق من توفر صفحة معينة
        /// </summary>
        public static bool IsPageAvailable(string pageName)
        {
            return pageName.ToLower() switch
            {
                "projectexplorer" => ProjectExplorer != null,
                "mainview" => MainView != null,
                "propertygrid" => PropertyGrid != null,
                "errorlist" => ErrorList != null,
                "toolbox" => Toolbox != null,
                "outline" => Outline != null,
                _ => false
            };
        }
    }
}