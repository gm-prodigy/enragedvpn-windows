using EnRagedGUI.Properties;
using EnRagedVPN_GUI;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace EnRagedGUI
{
    public class EntryPoint
    {
        // All WPF applications should execute on a single-threaded apartment (STA) thread
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length == 3 && args[0] == "/service")
            {
                var t = new Thread(() =>
                {
                    try
                    {
                        var currentProcess = Process.GetCurrentProcess();
                        var uiProcess = Process.GetProcessById(int.Parse(args[2]));
                        if (uiProcess.MainModule.FileName != currentProcess.MainModule.FileName)
                            return;
                        uiProcess.WaitForExit();
                        Tunnel.Service.Remove(args[1], false);
                    }
                    catch { }
                });
                t.Start();
                Tunnel.Service.Run(args[1]);
                t.Interrupt();
                return;
            }

            EntryApplication app = new EntryApplication();
            app.DispatcherUnhandledException += App_DispatcherUnhandledException;
            app.Run();
        }

        private static void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string errorMessage = string.Format("An unhandled exception occurred: {0}", e.Exception.Message);
            MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public class EntryApplication : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (Settings.Default.token != "")
            {
                MainWindow window = new();
                window.Show();
            }
            else
            {
                Login login = new();
                login.Show();
            }


        }
    }
}