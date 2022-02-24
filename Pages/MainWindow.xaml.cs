using EnRagedGUI.Properties;
using System;
using System.IO;
using System.Security.AccessControl;
using System.Threading;
using System.Windows;
using static EnRagedGUI.Globals;

namespace EnRagedGUI
{

    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            MakeConfigDirectory();
            InitializeComponent();
            StartUpEvents();
            try { File.Delete(LogFile); } catch { }
            log = new Tunnel.Ringlogger(LogFile, "GUI");
        }

        private void StartUpEvents()
        {
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
                var ds = new DirectorySecurity();
                ds.SetSecurityDescriptorSddlForm("O:BAG:BAD:PAI(A;OICI;FA;;;BA)(A;OICI;FA;;;SY)");
                FileSystemAclExtensions.CreateDirectory(ds, UserDirectory);
            }
            catch
            {
                MessageBox.Show("EnRagedVPN requires administrator privileges", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }

    }
}
