using Squirrel;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static EnRagedGUI.Properties.Settings;

using System.Reflection;
using System.Security.Principal;

namespace EnRagedGUI
{

    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {

            // Named Mutexes are available computer-wide. Use a unique name.
            using var mutex = new Mutex(false, "EnRagedVPN");
            // TimeSpan.Zero to test the mutex's signal state and
            // return immediately without blocking
            bool isAnotherInstanceOpen = !mutex.WaitOne(TimeSpan.Zero);
            if (isAnotherInstanceOpen)
            {
                MessageBox.Show("Only one instance of EnRagedVPN may be running at one point!");
                return;
            }


            if (!IsRunAsAdministrator())
            {
                // It is not possible to launch a ClickOnce app as administrator directly, so instead we launch the
                // app as administrator in a new process.
                String appStartPath = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

                var processInfo = new ProcessStartInfo(appStartPath + @"\EnRagedVPN.exe");

                // The following properties run the new process as administrator
                processInfo.UseShellExecute = true;
                processInfo.Verb = "runas";

                // Start the new process
                try
                {
                    Process.Start(processInfo);
                }
                catch (Exception)
                {
                    // The user did not allow the application to run as administrator
                    MessageBox.Show("Sorry, this application must be run as Administrator.");
                }
            }
            else
            {
                if (e.Args.Length == 3 && e.Args[0] == "/service")
                {
                    var t = new Thread(() =>
                    {
                        try
                        {
                            var currentProcess = Process.GetCurrentProcess();
                            var uiProcess = Process.GetProcessById(int.Parse(e.Args[2]));
                            if (uiProcess.MainModule.FileName != currentProcess.MainModule.FileName)
                                return;
                            uiProcess.WaitForExit();
                            Tunnel.Service.Remove(e.Args[1], false);
                        }
                        catch { }
                    });
                    t.Start();
                    Tunnel.Service.Run(e.Args[1]);
                    t.Interrupt();
                    return;
                }

                DispatcherUnhandledException += App_DispatcherUnhandledException;
            }
            mutex.ReleaseMutex();
            base.OnStartup(e);
        }

        private static bool IsRunAsAdministrator()
        {
            var wi = WindowsIdentity.GetCurrent();
            var wp = new WindowsPrincipal(wi);

            return wp.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string errorMessage = string.Format("An unhandled exception occurred: {0}", e.Exception.Message);
            MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}