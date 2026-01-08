using Avalonia.Controls;
using MyDesigner.XamlDesigner.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace MyDesigner.XamlDesigner.View;

/// <summary>
/// Class for loading external references and libraries from project
/// </summary>
public class ProjectReferencesLoader
{
    private string _projectPath;
    private string _csprojPath;
    private HashSet<string> _loadedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // يحتوي على أسماء المكتبات المحملة في هذه الجلسة

    /// <summary>
    /// Load all references from project
    /// </summary>
    public void LoadAllReferences(string projectPath)
    {
        try
        {
            _projectPath = projectPath;
            _loadedAssemblies.Clear();

            // البحث عن ملف .csproj
            var csprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.TopDirectoryOnly);
            if (csprojFiles.Length == 0)
            {
                Console.WriteLine("لم يتم العثور على ملف .csproj");
                return;
            }

            _csprojPath = csprojFiles[0];
            var doc = XDocument.Load(_csprojPath);

            Console.WriteLine("========================================");
            Console.WriteLine($"تحميل مراجع المشروع: {Path.GetFileNameWithoutExtension(_csprojPath)}");
            Console.WriteLine("========================================");

            // 1. تحميل DLL من مجلد bin المشروع المفتوح نفسه (أولاً لضمان تحميل Controls الخاصة به)
            LoadProjectOutput();

            // 2. فحص ملفات XAML واستخراج namespaces المستخدمة
            ScanXamlFilesForNamespaces();

            // 3. تحميل مراجع المشاريع (ProjectReference)
            LoadProjectReferences(doc);

            // 4. تحميل مراجع الحزم (PackageReference)
            LoadPackageReferences(doc);

            // 5. تحميل المراجع المباشرة (Reference)
            LoadDirectReferences(doc);

            Console.WriteLine("========================================");
            Console.WriteLine($"✓ تم تحميل {_loadedAssemblies.Count} مكتبة بنجاح");
            Console.WriteLine("========================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في تحميل المراجع: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Scan XAML files and extract used namespaces
    /// </summary>
    private void ScanXamlFilesForNamespaces()
    {
        try
        {
            Console.WriteLine("\n[1.5] فحص ملفات XAML للبحث عن namespaces مخصصة:");

            // البحث عن جميع ملفات XAML في المشروع
            var xamlFiles = Directory.GetFiles(_projectPath, "*.xaml", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                .ToList();

            Console.WriteLine($"   وجد {xamlFiles.Count} ملف XAML");

            var customNamespaces = new HashSet<string>();
            var projectName = Path.GetFileNameWithoutExtension(_csprojPath);

            foreach (var xamlFile in xamlFiles)
            {
                try
                {
                    var content = File.ReadAllText(xamlFile);

                    // البحث عن xmlns:prefix="clr-namespace:..."
                    var namespacePattern = @"xmlns:(\w+)\s*=\s*""clr-namespace:([^""]+)""";
                    var matches = System.Text.RegularExpressions.Regex.Matches(content, namespacePattern);

                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        var prefix = match.Groups[1].Value;
                        var clrNamespace = match.Groups[2].Value;

                        // تجاهل namespaces النظام
                        if (!clrNamespace.StartsWith("System.") &&
                            !clrNamespace.StartsWith("Microsoft."))
                        {
                            customNamespaces.Add(clrNamespace);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠ خطأ في قراءة {Path.GetFileName(xamlFile)}: {ex.Message}");
                }
            }

            if (customNamespaces.Count > 0)
            {
                Console.WriteLine($"   ✓ وجد {customNamespaces.Count} namespace مخصص:");
                foreach (var ns in customNamespaces)
                {
                    Console.WriteLine($"      - {ns}");
                }

                // تحميل Controls من هذه الـ namespaces
                LoadControlsFromNamespaces(customNamespaces, projectName);
            }
            else
            {
                Console.WriteLine($"   ℹ لم يتم العثور على namespaces مخصصة");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ خطأ في فحص ملفات XAML: {ex.Message}");
        }
    }

    /// <summary>
    /// Load Controls from specified namespaces
    /// </summary>
    private void LoadControlsFromNamespaces(HashSet<string> namespaces, string projectName)
    {
        try
        {
            Console.WriteLine($"\n   محاولة تحميل Controls من {namespaces.Count} namespace:");

            // البحث عن Assembly المشروع
            var binFolder = Path.Combine(_projectPath, "bin");
            if (!Directory.Exists(binFolder))
            {
                Console.WriteLine($"   ⚠ مجلد bin غير موجود: {binFolder}");
                Console.WriteLine($"   ℹ قم ببناء المشروع أولاً (Build → Build Solution)");
                return;
            }

            // البحث عن DLL/EXE في جميع المجلدات الفرعية
            var outputFiles = new List<string>();

            try
            {
                var dllFiles = Directory.GetFiles(binFolder, "*.dll", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("\\ref\\") && !f.Contains("\\resources\\"))
                    .ToList();
                var exeFiles = Directory.GetFiles(binFolder, "*.exe", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("\\ref\\") && !f.Contains("\\resources\\"))
                    .ToList();

                outputFiles.AddRange(dllFiles);
                outputFiles.AddRange(exeFiles);

                Console.WriteLine($"   📁 وجد {dllFiles.Count} DLL و {exeFiles.Count} EXE في bin");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠ خطأ في البحث عن الملفات: {ex.Message}");
                return;
            }

            if (outputFiles.Count == 0)
            {
                Console.WriteLine($"   ⚠ لم يتم العثور على مخرجات في bin");
                Console.WriteLine($"   ℹ قم ببناء المشروع أولاً");
                return;
            }

            // محاولة تحميل كل ملف والبحث عن Controls
            var totalControlsLoaded = 0;

            foreach (var outputFile in outputFiles.OrderByDescending(f => File.GetLastWriteTime(f)))
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(outputFile);

                    // تخطي المكتبات المحملة مسبقاً
                    if (_loadedAssemblies.Contains(fileName))
                        continue;

                    // تحميل Assembly
                    var assembly = System.Reflection.Assembly.LoadFrom(outputFile);
                    var assemblyName = assembly.GetName().Name;

                    // التحقق من عدم التكرار في Toolbox
                    var alreadyExists = Toolbox.Instance.AssemblyNodes.Any(node =>
                        string.Equals(node.Assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase)
                    );

                    if (alreadyExists)
                        continue;

                    // البحث عن Controls في الـ namespaces المحددة
                    var types = assembly.GetExportedTypes();
                    var controlTypes = new List<Type>();

                    foreach (var type in types)
                    {
                        // التحقق من أن النوع في أحد الـ namespaces المطلوبة
                        if (!string.IsNullOrEmpty(type.Namespace) && namespaces.Contains(type.Namespace))
                        {
                            if (!type.IsAbstract &&
                                !type.IsGenericTypeDefinition &&
                                type.IsSubclassOf(typeof(Control)) &&
                                type.GetConstructor(
                                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                                    null, Type.EmptyTypes, null) != null)
                            {
                                controlTypes.Add(type);
                            }
                        }
                    }

                    if (controlTypes.Count > 0)
                    {
                        // تسجيل Assembly
                        MyTypeFinder.Instance.RegisterAssembly(assembly);

                        // إضافة إلى Toolbox
                        var node = new AssemblyNode
                        {
                            Assembly = assembly,
                            Path = outputFile
                        };

                        foreach (var type in controlTypes)
                        {
                            node.Controls.Add(new ControlNode { Type = type });
                        }

                        node.Controls.Sort((c1, c2) => c1.Name.CompareTo(c2.Name));
                        Toolbox.Instance.AssemblyNodes.Add(node);

                        Console.WriteLine($"   ✓ تم تحميل {controlTypes.Count} Control من {assemblyName}:");
                        foreach (var ctrl in controlTypes)
                        {
                            Console.WriteLine($"      - {ctrl.Name} ({ctrl.Namespace})");
                        }

                        _loadedAssemblies.Add(assemblyName);
                        totalControlsLoaded += controlTypes.Count;
                    }
                }
                catch (Exception ex)
                {
                    // تجاهل الأخطاء في تحميل ملفات معينة
                    Console.WriteLine($"   ⚠ تخطي {Path.GetFileName(outputFile)}: {ex.Message}");
                }
            }

            if (totalControlsLoaded == 0)
            {
                Console.WriteLine($"   ⚠ لم يتم العثور على Controls في الـ namespaces المحددة");
                Console.WriteLine($"   ℹ تأكد من:");
                Console.WriteLine($"      1. بناء المشروع (Build Solution)");
                Console.WriteLine($"      2. Controls ترث من UIElement");
                Console.WriteLine($"      3. Controls لها Constructor عام بدون معاملات");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ خطأ في تحميل Controls: {ex.Message}");
            Console.WriteLine($"   Stack: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Load project references (ProjectReference)
    /// </summary>
    private void LoadProjectReferences(XDocument doc)
    {
        var projectReferences = doc.Descendants("ProjectReference")
            .Select(x => x.Attribute("Include")?.Value)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        if (projectReferences.Count == 0)
        {
            Console.WriteLine("\n[2] لا توجد مراجع مشاريع (ProjectReference)");
            return;
        }

        Console.WriteLine($"\n[2] تحميل {projectReferences.Count} مرجع مشروع (ProjectReference):");

        foreach (var reference in projectReferences)
        {
            try
            {
                var referencedCsprojPath = Path.GetFullPath(
                    Path.Combine(Path.GetDirectoryName(_csprojPath), reference));

                if (!File.Exists(referencedCsprojPath))
                {
                    Console.WriteLine($"   ⚠ ملف المشروع غير موجود: {reference}");
                    continue;
                }

                var referencedProjectFolder = Path.GetDirectoryName(referencedCsprojPath);
                var referencedProjectName = Path.GetFileNameWithoutExtension(referencedCsprojPath);

                // التحقق من عدم تحميل المشروع مسبقاً
                if (_loadedAssemblies.Contains(referencedProjectName))
                {
                    Console.WriteLine($"   ℹ تم تجاهل (محمل مسبقاً): {referencedProjectName}");
                    continue;
                }

                // البحث في مجلد bin
                var binDirectory = Path.Combine(referencedProjectFolder, "bin");
                if (Directory.Exists(binDirectory))
                {
                    // البحث عن DLL في جميع المجلدات الفرعية (تجنب ref و resources)
                    var dllFiles = Directory.GetFiles(binDirectory, $"{referencedProjectName}.dll",
                        SearchOption.AllDirectories)
                        .Where(f => !f.Contains("\\ref\\") && !f.Contains("\\resources\\"))
                        .ToList();

                    // اختيار أحدث DLL
                    var latestDll = dllFiles
                        .OrderByDescending(f => File.GetLastWriteTime(f))
                        .FirstOrDefault();

                    if (latestDll != null)
                    {
                        LoadAssembly(latestDll);
                    }
                    else
                    {
                        Console.WriteLine($"   ⚠ لم يتم العثور على DLL: {referencedProjectName}");
                    }
                }
                else
                {
                    Console.WriteLine($"   ⚠ مجلد bin غير موجود: {binDirectory}");
                }

                // تحميل مراجع المشروع المرجعي بشكل تكراري
                LoadReferencedProjectDependencies(referencedCsprojPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ خطأ في تحميل مرجع المشروع {reference}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Load referenced project dependencies
    /// </summary>
    private void LoadReferencedProjectDependencies(string csprojPath)
    {
        try
        {
            var doc = XDocument.Load(csprojPath);
            var projectFolder = Path.GetDirectoryName(csprojPath);

            // تحميل PackageReference من المشروع المرجعي
            var packages = doc.Descendants("PackageReference")
                .Select(x => new
                {
                    Name = x.Attribute("Include")?.Value,
                    Version = x.Attribute("Version")?.Value ?? x.Element("Version")?.Value
                })
                .Where(x => !string.IsNullOrEmpty(x.Name));

            foreach (var package in packages)
            {
                LoadPackageFromNuGet(package.Name, package.Version, projectFolder);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في تحميل تبعيات المشروع: {ex.Message}");
        }
    }

    /// <summary>
    /// Load package references (PackageReference)
    /// </summary>
    private void LoadPackageReferences(XDocument doc)
    {
        var packageReferences = doc.Descendants("PackageReference")
            .Select(x => new
            {
                Name = x.Attribute("Include")?.Value,
                Version = x.Attribute("Version")?.Value ?? x.Element("Version")?.Value
            })
            .Where(x => !string.IsNullOrEmpty(x.Name))
            .ToList();

        if (packageReferences.Count == 0)
        {
            Console.WriteLine("\n[3] لا توجد مراجع حزم (PackageReference)");
            return;
        }

        Console.WriteLine($"\n[3] تحميل {packageReferences.Count} مرجع حزمة (PackageReference):");

        foreach (var package in packageReferences)
        {
            try
            {
                Console.WriteLine($"   📦 معالجة: {package.Name} ({package.Version ?? "latest"})");
                LoadPackageFromNuGet(package.Name, package.Version, _projectPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ خطأ في تحميل الحزمة {package.Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Load package from NuGet
    /// </summary>
    private void LoadPackageFromNuGet(string packageName, string version, string projectPath)
    {
        // تجاهل مكتبات النظام
        if (IsSystemAssembly(packageName))
        {
            Console.WriteLine($"تم تجاهل حزمة النظام: {packageName}");
            return;
        }

        // البحث في مجلد packages المحلي
        var packagesFolder = FindPackagesFolder(projectPath);
        if (packagesFolder != null)
        {
            var packageFolder = Directory.GetDirectories(packagesFolder, $"{packageName}*", SearchOption.TopDirectoryOnly)
                .OrderByDescending(d => d)
                .FirstOrDefault();

            if (packageFolder != null)
            {
                LoadDllsFromPackage(packageFolder);
                return;
            }
        }

        // البحث في مجلد NuGet العام
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var nugetCache = Path.Combine(userProfile, ".nuget", "packages", packageName.ToLower());

        if (Directory.Exists(nugetCache))
        {
            var versionFolder = string.IsNullOrEmpty(version)
                ? Directory.GetDirectories(nugetCache).OrderByDescending(d => d).FirstOrDefault()
                : Path.Combine(nugetCache, version);

            if (versionFolder != null && Directory.Exists(versionFolder))
            {
                LoadDllsFromPackage(versionFolder);
            }
        }
    }

    /// <summary>
    /// Search for packages folder
    /// </summary>
    private string FindPackagesFolder(string startPath)
    {
        var currentPath = startPath;
        while (!string.IsNullOrEmpty(currentPath))
        {
            var packagesPath = Path.Combine(currentPath, "packages");
            if (Directory.Exists(packagesPath))
                return packagesPath;

            var parentPath = Directory.GetParent(currentPath)?.FullName;
            if (parentPath == currentPath)
                break;
            currentPath = parentPath;
        }
        return null;
    }

    /// <summary>
    /// Load DLLs from package
    /// </summary>
    private void LoadDllsFromPackage(string packageFolder)
    {
        // البحث في مجلد lib
        var libFolder = Path.Combine(packageFolder, "lib");
        if (!Directory.Exists(libFolder))
            return;

        // البحث عن أفضل framework متوافق
        var frameworks = new[] {"net10.0-windows", "net8.0-windows", "net7.0-windows", "net6.0-windows",
                                "net5.0-windows", "netcoreapp3.1", "net48", "net472",
                                "net471", "net47", "net462", "net461", "net46", "net45" };

        string targetFolder = null;
        foreach (var framework in frameworks)
        {
            var fwFolder = Path.Combine(libFolder, framework);
            if (Directory.Exists(fwFolder))
            {
                targetFolder = fwFolder;
                break;
            }
        }

        // إذا لم يتم العثور على framework محدد، استخدم أي مجلد متاح
        if (targetFolder == null)
        {
            targetFolder = Directory.GetDirectories(libFolder)
                .OrderByDescending(d => d)
                .FirstOrDefault();
        }

        if (targetFolder != null && Directory.Exists(targetFolder))
        {
            // تحميل DLL files فقط من المجلد الرئيسي (بدون المجلدات الفرعية)
            // وتجاهل ملفات resources و ref
            var dllFiles = Directory.GetFiles(targetFolder, "*.dll", SearchOption.TopDirectoryOnly)
                .Where(f => !f.Contains("\\ref\\") && 
                           !f.Contains("\\resources\\") &&
                           !f.Contains("\\runtimes\\") &&
                           !Path.GetFileName(f).StartsWith("System.") &&
                           !Path.GetFileName(f).StartsWith("Microsoft."))
                .ToList();
            
            foreach (var dll in dllFiles)
            {
                LoadAssembly(dll);
            }
        }
    }

    /// <summary>
    /// Load direct references (Reference)
    /// </summary>
    private void LoadDirectReferences(XDocument doc)
    {
        var references = doc.Descendants("Reference")
            .Where(x => x.Attribute("Include") != null)
            .ToList();

        if (references.Count == 0)
        {
            Console.WriteLine("\n[4] لا توجد مراجع مباشرة (Reference)");
            return;
        }

        Console.WriteLine($"\n[4] تحميل {references.Count} مرجع مباشر (Reference):");

        foreach (var reference in references)
        {
            try
            {
                var includeName = reference.Attribute("Include")?.Value;
                var hintPath = reference.Element("HintPath")?.Value;
                
                if (!string.IsNullOrEmpty(hintPath))
                {
                    var fullPath = Path.GetFullPath(
                        Path.Combine(Path.GetDirectoryName(_csprojPath), hintPath));

                    if (File.Exists(fullPath))
                    {
                        Console.WriteLine($"   📚 معالجة: {includeName}");
                        LoadAssembly(fullPath);
                    }
                    else
                    {
                        Console.WriteLine($"   ⚠ ملف غير موجود: {includeName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ خطأ في تحميل المرجع المباشر: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Load project output itself
    /// </summary>
    private void LoadProjectOutput()
    {
        try
        {
            var projectName = Path.GetFileNameWithoutExtension(_csprojPath);
            Console.WriteLine($"\n[1] تحميل مخرجات المشروع: {projectName}");

            var binFolder = Path.Combine(_projectPath, "bin");
            bool loadedFromBin = false;

            if (Directory.Exists(binFolder))
            {
                // البحث عن DLL أو EXE
                var outputFiles = new List<string>();
                var dllFiles = Directory.GetFiles(binFolder, $"{projectName}.dll", SearchOption.AllDirectories);
                var exeFiles = Directory.GetFiles(binFolder, $"{projectName}.exe", SearchOption.AllDirectories);

                outputFiles.AddRange(dllFiles);
                outputFiles.AddRange(exeFiles);

                Console.WriteLine($"   وجد {dllFiles.Length} DLL و {exeFiles.Length} EXE");

                if (outputFiles.Count > 0)
                {
                    // اختيار أحدث ملف
                    var latestOutput = outputFiles
                        .OrderByDescending(f => File.GetLastWriteTime(f))
                        .FirstOrDefault();

                    if (latestOutput != null)
                    {
                        Console.WriteLine($"   📁 الملف: {Path.GetFileName(latestOutput)}");
                        Console.WriteLine($"   📅 آخر تعديل: {File.GetLastWriteTime(latestOutput)}");

                        LoadAssembly(latestOutput);
                        loadedFromBin = true;
                    }
                }
            }

            // إذا لم يتم التحميل من bin، حاول تحميل من Assembly الحالي
            if (!loadedFromBin)
            {
                Console.WriteLine($"   ⚠ لم يتم العثور على مخرجات مبنية");
                Console.WriteLine($"   ℹ محاولة تحميل Controls من Assembly الحالي...");
                LoadCurrentAssemblyControls(projectName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ خطأ في تحميل مخرجات المشروع: {ex.Message}");
        }
    }

    /// <summary>
    /// Load Controls from current Assembly (for unbuilt projects)
    /// </summary>
    private void LoadCurrentAssemblyControls(string projectName)
    {
        try
        {
            // الحصول على جميع Assemblies المحملة
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            // البحث عن Assembly المشروع المفتوح
            var targetAssembly = loadedAssemblies.FirstOrDefault(a =>
                !a.IsDynamic &&
                string.Equals(a.GetName().Name, projectName, StringComparison.OrdinalIgnoreCase));

            if (targetAssembly == null)
            {
                // محاولة تحميل من المسار
                var possiblePaths = new[]
                {
                    Path.Combine(_projectPath, "bin", "Debug", $"{projectName}.dll"),
                    Path.Combine(_projectPath, "bin", "Release", $"{projectName}.dll"),
                    Path.Combine(_projectPath, "bin", "Debug", $"{projectName}.exe"),
                    Path.Combine(_projectPath, "bin", "Release", $"{projectName}.exe")
                };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        Console.WriteLine($"   📁 وجد ملف: {path}");
                        LoadAssembly(path);
                        return;
                    }
                }

                Console.WriteLine($"   ⚠ لم يتم العثور على Assembly: {projectName}");
                Console.WriteLine($"   ℹ قم ببناء المشروع أولاً (Build Solution)");
                return;
            }

            var assemblyName = targetAssembly.GetName().Name;
            Console.WriteLine($"   ✓ وجد Assembly محمل: {assemblyName}");

            // التحقق من عدم وجودها في Toolbox
            var alreadyExists = Toolbox.Instance.AssemblyNodes.Any(node =>
                string.Equals(node.Assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase)
            );

            if (alreadyExists)
            {
                Console.WriteLine($"   ℹ تم تجاهل (موجود في Toolbox): {assemblyName}");
                return;
            }

            // البحث عن Controls في Assembly
            var types = targetAssembly.GetExportedTypes();
            var controlTypes = new List<Type>();

            foreach (var type in types)
            {
                if (!type.IsAbstract &&
                    !type.IsGenericTypeDefinition &&
                    type.IsSubclassOf(typeof(Control)) &&
                    type.GetConstructor(
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                        null, Type.EmptyTypes, null) != null)
                {
                    controlTypes.Add(type);
                }
            }

            if (controlTypes.Count == 0)
            {
                Console.WriteLine($"   ℹ لا توجد Controls في المشروع");
                return;
            }

            // تسجيل Assembly في TypeFinder
            MyTypeFinder.Instance.RegisterAssembly(targetAssembly);

            // إضافة إلى Toolbox
            var node = new AssemblyNode
            {
                Assembly = targetAssembly,
                Path = targetAssembly.Location
            };

            foreach (var type in controlTypes)
            {
                node.Controls.Add(new ControlNode { Type = type });
            }

            node.Controls.Sort((c1, c2) => c1.Name.CompareTo(c2.Name));
            Toolbox.Instance.AssemblyNodes.Add(node);

            Console.WriteLine($"   ✓ تم تحميل {node.Controls.Count} Control من المشروع:");
            foreach (var ctrl in node.Controls.Take(5))
            {
                Console.WriteLine($"      - {ctrl.Name}");
            }
            if (node.Controls.Count > 5)
            {
                Console.WriteLine($"      ... و {node.Controls.Count - 5} آخرين");
            }

            _loadedAssemblies.Add(assemblyName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ خطأ في تحميل Controls: {ex.Message}");
        }
    }



    /// <summary>
    /// Load Assembly to Toolbox
    /// </summary>
    private void LoadAssembly(string dllPath)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(dllPath);

            // تجاهل مكتبات النظام
            if (IsSystemAssembly(fileName))
            {
                Console.WriteLine($"   ℹ تم تجاهل (مكتبة نظام): {fileName}");
                return;
            }

            // تجاهل ملفات ref و resources
            if (dllPath.Contains("\\ref\\") || dllPath.Contains("\\resources\\") || dllPath.Contains("\\runtimes\\"))
            {
                Console.WriteLine($"   ℹ تم تجاهل (ملف مرجعي): {fileName}");
                return;
            }

            if (!File.Exists(dllPath))
            {
                Console.WriteLine($"   ⚠ تحذير: الملف غير موجود: {dllPath}");
                return;
            }

            // التحقق من عدم وجودها في Toolbox باستخدام اسم Assembly
            var alreadyExists = Toolbox.Instance.AssemblyNodes.Any(node =>
                string.Equals(node.Assembly.GetName().Name, fileName, StringComparison.OrdinalIgnoreCase)
            );

            if (alreadyExists)
            {
                Console.WriteLine($"   ℹ تم تجاهل (موجود في Toolbox): {fileName}");
                return;
            }

            // التحقق من عدم تحميل المكتبة مسبقاً في هذه الجلسة
            if (_loadedAssemblies.Contains(fileName))
            {
                Console.WriteLine($"   ℹ تم تجاهل (محمل في هذه الجلسة): {fileName}");
                return;
            }

            // التحقق من وجود Controls في المكتبة قبل إضافتها
            if (!HasUIControls(dllPath))
            {
                Console.WriteLine($"   ℹ تم تجاهل (لا يحتوي على Controls): {fileName}");
                _loadedAssemblies.Add(fileName);
                return;
            }

            // إضافة المكتبة إلى Toolbox
            Toolbox.Instance.AddAssembly(dllPath);
            _loadedAssemblies.Add(fileName);
            Console.WriteLine($"   ✓ تم تحميل: {fileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ خطأ في تحميل {Path.GetFileName(dllPath)}: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if library has UI Controls
    /// </summary>
    private bool HasUIControls(string dllPath)
    {
        try
        {
            var assembly = System.Reflection.Assembly.LoadFrom(dllPath);
            var types = assembly.GetExportedTypes();

            // البحث عن أي نوع يرث من UIElement
            foreach (var type in types)
            {
                if (!type.IsAbstract &&
                    !type.IsGenericTypeDefinition &&
                    type.IsSubclassOf(typeof(Control)) &&
                    type.GetConstructor(
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                        null, Type.EmptyTypes, null) != null)
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطأ في فحص Controls في {Path.GetFileName(dllPath)}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if library is a system library
    /// </summary>
    private bool IsSystemAssembly(string assemblyName)
    {
        var systemPrefixes = new[]
        {
            "System.", "Microsoft.", "mscorlib", "netstandard",
            "WindowsBase", "PresentationCore", "PresentationFramework",
            "Newtonsoft.Json", "NuGet.", "NETStandard.Library",
            "AvalonEdit", "ICSharpCode.", "Mono.Cecil", "IKVM.",
            "Dirkster.", "AvalonDock", "WPFToolkit", "DynamicDataDisplay",
            "Windows.", "UIAutomation", "Accessibility", "ReachFramework",
            "System", "Microsoft", "api-ms-", "clr", "sni", "sos",
            "runtime.", "hostfxr", "hostpolicy", "coreclr", "clrjit",
            "dbgshim", "mscordaccore", "mscordbi", "mscorrc"
        };

        var exactMatches = new[]
        {
            "mscorlib", "netstandard", "WindowsBase", "PresentationCore",
            "PresentationFramework", "System", "Microsoft", "System.Runtime",
            "System.Core", "System.Xml", "System.Data", "System.Drawing",
            "System.Windows.Forms", "System.Configuration", "System.Net.Http"
        };

        // تحقق من المطابقة الكاملة
        if (exactMatches.Contains(assemblyName, StringComparer.OrdinalIgnoreCase))
            return true;

        // تحقق من البادئات
        return systemPrefixes.Any(prefix =>
            assemblyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Clear all loaded assemblies
    /// </summary>
    public void ClearLoadedAssemblies()
    {
        _loadedAssemblies.Clear();
    }
}
