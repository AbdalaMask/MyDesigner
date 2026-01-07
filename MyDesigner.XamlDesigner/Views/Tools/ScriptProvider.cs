

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDesigner.Common.Controls
{
    /// <summary>
    /// This is a simple script provider that adds a few using statements to the C# scripts (.csx files)
    /// </summary>
    class ScriptProvider 
    {
        public string GetNamespace()
        {
            return "HMIControl.HMI";
        }

        public string GetUsing()
        {
            return "using System; " +
                   "using System.Collections.Generic; " +
                   "using System.Linq; " +
                   "using System.Text; " +
                   "using System.Windows; " +
                   "using System.Windows.Controls; " +
                   "using System.Windows.Data; " +
                   "using System.Windows.Documents; " +
                   "using System.Windows.Input; " +
                   "using System.Windows.Media; " +
                   "using System.Windows.Media.Imaging; " +
                   "using System.Windows.Navigation; " +
                   "using System.Windows.Shapes; " +
                   "using HMIControl.Base; " +
                   "using HMIControl.Collections; " +
                   "using HMIControl.Core; " +
                   "using HMIControl.HMI; " +
                   "using HMIControl.HMI.Ex1; " +
                   "using HMIControl.HMI.Ex2; " +
                   "using HMIControl.HMI.Model; " +
                   "using MyStudio.Common; " +
                   "using MyStudio.DriverComm; ";
        }

        public string GetVars() => null;
    }
}
