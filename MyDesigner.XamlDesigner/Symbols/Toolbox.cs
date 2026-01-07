using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace MyDesigner.XamlDesigner
{
    public class Toolbox
    {
        public Toolbox()
        {
            AssemblyNodes = new ObservableCollection<AssemblyNode>();

            // Add default Avalonia controls - only add the main Avalonia.Controls assembly
            AddAssembly(typeof(Button).Assembly.Location);
          
            AddAssembly(typeof(ColorPicker).Assembly.Location);
         
          //  AddAssembly(typeof(Airseal).Assembly.Location);
         
        

        }

        public static Toolbox Instance { get; } = new Toolbox();
        
        public ObservableCollection<AssemblyNode> AssemblyNodes { get; private set; }

        public void AddAssembly(string path)
        {
            try
            {
                var assembly = Services.AssemblyService.LoadAssembly(path);
                if (assembly != null)
                {
                    AddAssembly(assembly);
                }
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        public void AddAssembly(Assembly assembly)
        {
            try
            {
                // Check if assembly is already added
                if (AssemblyNodes.Any(n => n.Assembly.FullName == assembly.FullName))
                    return;

                var node = new AssemblyNode
                {
                    Assembly = assembly,
                    Path = assembly.Location,
                    Controls = new List<ControlNode>()
                };

                foreach (var type in assembly.GetExportedTypes())
                {
                    if (IsControl(type))
                    {
                        node.Controls.Add(new ControlNode { Type = type });
                    }
                }

                // Sort controls by name
                node.Controls.Sort((c1, c2) => c1.Name.CompareTo(c2.Name));

                if (node.Controls.Any())
                {
                    AssemblyNodes.Add(node);
                }
            }
            catch (Exception ex)
            {
                Shell.ReportException(ex);
            }
        }

        public void Remove(AssemblyNode node)
        {
            AssemblyNodes.Remove(node);
        }

        private static bool IsControl(Type type)
        {
            return !type.IsAbstract && 
                   !type.IsGenericTypeDefinition && 
                   (typeof(Control).IsAssignableFrom(type) || 
                    typeof(TemplatedControl).IsAssignableFrom(type)) &&
                   type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) != null;
        }
    }

    public class AssemblyNode
    {
        public Assembly Assembly { get; set; }
        public List<ControlNode> Controls { get; set; } = new List<ControlNode>();
        public string Path { get; set; }

        public string Name => Assembly?.GetName().Name ?? "Unknown";
    }

    public class ControlNode
    {
        public Type Type { get; set; }
        public string Name => Type?.Name ?? "Unknown";
    }
}