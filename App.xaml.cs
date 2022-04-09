using Squirrel;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static EnRagedGUI.Properties.Settings;

using System.Reflection;
using System.Security.Principal;
using System.IO;
using EnRagedGUI.Helper;
using System.Linq;
using MaterialDesignThemes.Wpf;

namespace EnRagedGUI
{

    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {

            Directory.CreateDirectory(Globals.UserDirectory);

            if (!IsRunAsAdministrator())
            {
                // It is not possible to launch a ClickOnce app as administrator directly, so instead we launch the
                // app as administrator in a new process.
                String appStartPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

                var processInfo = new ProcessStartInfo(appStartPath + @"\EnRagedVPN.exe")
                {
                    // The following properties run the new process as administrator
                    UseShellExecute = true,
                    Verb = "runas"
                };

                // Start the new process
                try
                {
                    Process.Start(processInfo);
                    Application.Current.Shutdown();
                }
                catch (Exception)
                {
                    // The user did not allow the application to run as administrator
                    MessageBox.Show("EnRagedVPN requires administrator privileges", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                }
                return;
            }

            DispatcherUnhandledException += App_DispatcherUnhandledException;

            base.OnStartup(e);
        }

        internal class Globals
        {
            public const string API_IP = "https://api.enragedvpn.com/";
            public static readonly string UserDirectory = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "Config");
            public static readonly string ConfigFile = Path.Combine(UserDirectory, "enragedvpn.conf");
            public static readonly string LogFile = Path.Combine(UserDirectory, "log.bin");

            public static Tunnel.Ringlogger log;
            public static Thread logPrintingThread, transferUpdateThread;
            public volatile static bool ThreadsRunning;
            public static Snackbar Snackbar;

            public static async Task CheckForUpdate()
            {
                try
                {
                    using var mgr = new UpdateManager("https://github.com/EnRagedVPN/enragedvpn-client");
                    Console.WriteLine("Checking for updates.");
                    if (mgr.IsInstalledApp)
                    {
                        Console.WriteLine($"Current Version: v{mgr.CurrentlyInstalledVersion()}");
                        var updates = await mgr.CheckForUpdate();
                        if (updates.ReleasesToApply.Any())
                        {
                            Console.WriteLine("Updates found. Applying updates.");
                            var release = await mgr.UpdateApp();

                            if (!Default.isConnected)
                            {

                                var result = MessageBox.Show("A new update has been downloaded, do you want to restart to apply update?");
                                if (result == MessageBoxResult.Yes)
                                {
                                    Console.WriteLine("Updates applied. Restarting app.");
                                    UpdateManager.RestartApp();
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {   //log exception and move on
                    Console.WriteLine($@"{e.Message}, Error finding latest version");
                }
            }
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

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Tunnel.Service.Remove(Globals.ConfigFile, true);
            try { File.Delete(Globals.LogFile); } catch { }
            try { File.Delete(Globals.ConfigFile); } catch { }
        }
    }
}