using EnRagedGUI.Properties;
using FluentScheduler;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static EnRagedGUI.Globals;

namespace EnRagedGUI
{

    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            MakeConfigDirectory();
            InitializeComponent();
            _ = StartUpEventsAsync();
            try { File.Delete(LogFile); } catch { }
            log = new Tunnel.Ringlogger(LogFile, "GUI");

            JobManager.Initialize();
        }

        private async Task StartUpEventsAsync()
        {
            CheckForUpdate();
            try
            {
                JobManager.AddJob(() => log.Write("1 minutes just passed."), s => s.ToRunEvery(1).Minutes());
                JobManager.AddJob(() => CheckForUpdate(), s => s.ToRunEvery(100).Minutes());
            }
            catch { }



            if (Settings.Default.token != "")
            {
                MainWindowFrame.Content = new Dashboard(false);
            }
            else
            {
                MainWindowFrame.Content = new Login();
            }

        }

        private static void MakeConfigDirectory()
        {
            try
            {
                //var ds = new DirectorySecurity();
                //ds.SetSecurityDescriptorSddlForm("O:BAG:BAD:PAI(A;OICI;FA;;;BA)(A;OICI;FA;;;SY)");
                //FileSystemAclExtensions.CreateDirectory(ds, UserDirectory);
                Directory.CreateDirectory(UserDirectory);
            }
            catch
            {
                MessageBox.Show("EnRagedVPN requires administrator privileges", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Settings.Default.Save();
            Environment.Exit(0);
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {

                this.DragMove();
            }
        }
    }
}
