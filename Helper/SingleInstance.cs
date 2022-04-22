using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace EnRagedGUI.Helper
{
    public sealed class SingleInstance
    {
        private static Mutex mutex;
        public static bool AlreadyRunning()
        {
            string uniqueName = null;
            var applicationName = Path.GetFileName(Assembly.GetEntryAssembly().GetName().Name);
            uniqueName ??= string.Format("{0}_{1}_{2}",
                Environment.MachineName,
                Environment.UserName,
                applicationName);

            mutex = new Mutex(true, uniqueName, out bool isOneTimeLaunch);
            return isOneTimeLaunch;
        }
    }
}
