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
using Squirrel;
using EnRagedGUI.Domain;

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
            public static readonly string UserDirectory = Path.Combine(Path.GetDirectoryName(Environment.CurrentDirectory), "Config");
            public static readonly string ConfigFile = Path.Combine(UserDirectory, "enragedvpn.conf");
            public static readonly string LogFile = Path.Combine(UserDirectory, "log.bin");
            public static Snackbar Snackbar;
            public static async Task CheckForUpdate(bool manual)
            {
                await Task.Run(async () =>
                {
                    try
                    {

                        using var mgr = new GithubUpdateManager(Default.UpdateUrl);

                        if (!mgr.IsInstalledApp)
                        {
                            if (manual)
                            {
                                Current.Dispatcher.Invoke((Action)async delegate
                                {
                                    var messageDialog = new MessageDialog
                                    {
                                        Message = { Text = "There's a configuration issue, reinstalling the application would fix this!" }
                                    };

                                    await DialogHost.Show(messageDialog, "RootDialog");
                                });
                            }
                            return;
                        }

                        var newVersion = await mgr.UpdateApp();

                        // optionally restart the app automatically, or ask the user if/when they want to restart
                        if (newVersion != null)
                        {
                            if (Default.isConnected)
                            {

                                var view = new MessageDialogPrompt
                                {
                                    DataContext = new(),
                                    Message = { Text = "Are you sure you want to update? You will be disconnected!" },
                                };

                                //show the dialog
                                var result = await DialogHost.Show(view, "RootDialog");
                                if (result.ToString() == "true")
                                {
                                    UpdateManager.RestartApp();
                                }
                                Console.WriteLine(result);

                                return;
                            }
                            UpdateManager.RestartApp();
                        }
                        else
                        {
                            if (manual)
                            {
                                Current.Dispatcher.Invoke((Action)async delegate
                                {
                                    var messageDialog = new MessageDialog
                                    {
                                        Message = { Text = "You are using the latest version!" }
                                    };

                                    await DialogHost.Show(messageDialog, "RootDialog");
                                });
                            }
                        }

                        return;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
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
            Current.Dispatcher.Invoke((Action)async delegate
            {
                var messageDialog = new MessageDialog
                {
                    Message = { Text = errorMessage }
                };

                await DialogHost.Show(messageDialog, "RootDialog");
            });
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Tunnel.Service.Remove(Globals.ConfigFile, true);
            try { File.Delete(Globals.LogFile); } catch { }
            try { File.Delete(Globals.ConfigFile); } catch { }
        }
    }
}