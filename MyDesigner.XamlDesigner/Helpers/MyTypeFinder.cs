using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MyDesigner.XamlDom;


namespace MyDesigner.XamlDesigner.Helpers;

public class MyTypeFinder : XamlTypeFinder
{
    private static readonly object lockObj = new();
    private static MyTypeFinder _instance;

    public static MyTypeFinder Instance
    {
        get
        {
            lock (lockObj)
            {
                if (_instance == null)
                {
                    _instance = new MyTypeFinder();
                    _instance.ImportFrom(CreateAvaloniaTypeFinder());
                }
            }

            return _instance;
        }
    }

    public override Assembly LoadAssembly(string name)
    {
        // البحث في التجميعات المسجلة
        foreach (var registeredAssembly in RegisteredAssemblies)
        {
            if (registeredAssembly.GetName().Name == name)
                return registeredAssembly;
        }

        // البحث في صندوق الأدوات
        foreach (var assemblyNode in Toolbox.Instance.AssemblyNodes)
        {
            if (assemblyNode.Name == name)
                return assemblyNode.Assembly;
        }

        // محاولة تحميل التجميع من النظام
        try
        {
            return Assembly.Load(name);
        }
        catch
        {
            // محاولة تحميل من المسار الحالي
            try
            {
                var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var assemblyPath = Path.Combine(currentDir, name + ".dll");
                if (File.Exists(assemblyPath))
                {
                    return Assembly.LoadFrom(assemblyPath);
                }
            }
            catch
            {
                // تجاهل الأخطاء
            }
        }

        return null;
    }

    public override XamlTypeFinder Clone()
    {
        return _instance;
    }

    /// <summary>
    /// إنشاء Type Finder خاص بـ Avalonia
    /// </summary>
    private static XamlTypeFinder CreateAvaloniaTypeFinder()
    {
        var typeFinder = new XamlTypeFinder();

        try
        {
            // تسجيل تجميعات Avalonia الأساسية
            var avaloniaAssemblies = new[]
            {
                "Avalonia.Base",
                "Avalonia.Controls",
                "Avalonia.Markup",
                "Avalonia.Markup.Xaml",
                "Avalonia.Styling",
                "Avalonia.Animation",
                "Avalonia.Input",
                "Avalonia.Interactivity",
                "Avalonia.Layout",
                "Avalonia.Media",
                "Avalonia.Metadata",
                "Avalonia.Platform",
                "Avalonia.Visuals",
                "Avalonia.Themes.Fluent",
                "Avalonia.Controls.DataGrid",
                "Avalonia.Controls.ColorPicker"
            };

            foreach (var assemblyName in avaloniaAssemblies)
            {
                try
                {
                    var assembly = Assembly.Load(assemblyName);
                    typeFinder.RegisterAssembly(assembly);
                    Console.WriteLine($"Registered Avalonia assembly: {assemblyName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load Avalonia assembly {assemblyName}: {ex.Message}");
                }
            }

            // تسجيل تجميعات .NET الأساسية
            var systemAssemblies = new[]
            {
                "System.Runtime",
                "System.Collections",
                "System.ComponentModel",
                "System.ObjectModel",
                "netstandard"
            };

            foreach (var assemblyName in systemAssemblies)
            {
                try
                {
                    var assembly = Assembly.Load(assemblyName);
                    typeFinder.RegisterAssembly(assembly);
                }
                catch
                {
                    // تجاهل الأخطاء للتجميعات الاختيارية
                }
            }

            // تسجيل التجميع الحالي
            typeFinder.RegisterAssembly(Assembly.GetExecutingAssembly());

            // تسجيل تجميعات المشروع
            var projectAssemblies = new[]
            {
                "MyDesigner.Design",
                "MyDesigner.Designer",
                "MyDesigner.XamlDom.Avalonia.Complete"
            };

            foreach (var assemblyName in projectAssemblies)
            {
                try
                {
                    var assembly = Assembly.Load(assemblyName);
                    typeFinder.RegisterAssembly(assembly);
                    Console.WriteLine($"Registered project assembly: {assemblyName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load project assembly {assemblyName}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating Avalonia type finder: {ex.Message}");
        }

        return typeFinder;
    }

    /// <summary>
    /// تحديث Type Finder بتجميعات جديدة
    /// </summary>
    public void RefreshAssemblies()
    {
        try
        {
            // إعادة تحميل التجميعات من صندوق الأدوات
            foreach (var assemblyNode in Toolbox.Instance.AssemblyNodes)
            {
                if (!RegisteredAssemblies.Contains(assemblyNode.Assembly))
                {
                    RegisterAssembly(assemblyNode.Assembly);
                    Console.WriteLine($"Refreshed assembly: {assemblyNode.Name}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing assemblies: {ex.Message}");
        }
    }

    /// <summary>
    /// البحث عن نوع بالاسم
    /// </summary>
    public Type FindTypeByName(string typeName, string namespaceName = null)
    {
        try
        {
            // البحث في جميع التجميعات المسجلة
            foreach (var assembly in RegisteredAssemblies)
            {
                try
                {
                    var fullTypeName = string.IsNullOrEmpty(namespaceName) 
                        ? typeName 
                        : $"{namespaceName}.{typeName}";

                    var type = assembly.GetType(fullTypeName);
                    if (type != null)
                        return type;

                    // البحث في جميع الأنواع إذا لم يتم العثور على النوع بالاسم الكامل
                    var types = assembly.GetTypes();
                    var matchingType = types.FirstOrDefault(t => t.Name == typeName);
                    if (matchingType != null)
                        return matchingType;
                }
                catch
                {
                    // تجاهل أخطاء التجميعات التي لا يمكن الوصول إليها
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error finding type {typeName}: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// الحصول على جميع الأنواع المتاحة لفئة معينة
    /// </summary>
    public List<Type> GetAvailableTypes(Type baseType)
    {
        var availableTypes = new List<Type>();

        try
        {
            foreach (var assembly in RegisteredAssemblies)
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.IsPublic && !t.IsAbstract && baseType.IsAssignableFrom(t))
                        .OrderBy(t => t.Name);

                    availableTypes.AddRange(types);
                }
                catch
                {
                    // تجاهل أخطاء التجميعات
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting available types: {ex.Message}");
        }

        return availableTypes;
    }
}