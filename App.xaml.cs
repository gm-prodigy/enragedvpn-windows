using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;


namespace EnRagedGUI
{

    public partial class EntryPoint
    {
        // All WPF applications should execute on a single-threaded apartment (STA) thread
        [STAThread]
        public static void Main(string[] args)
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

            EntryApplication app = new();
            app.DispatcherUnhandledException += App_DispatcherUnhandledException;
            app.Run();

            mutex.ReleaseMutex();


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
            MainWindow window = new();
            window.Show();

        }
    }
}