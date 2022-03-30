using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static EnRagedGUI.Properties.Settings;



namespace EnRagedGUI.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Page
    {

        public Settings()
        {
            InitializeComponent();
            SettingsFrameContent.Content = new SettingsContent.General();
        }

        private void SettingsBackButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow.Content = new Dashboard(false);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SettingsFrameContent.Content = new SettingsContent.General();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SettingsFrameContent.Content = new SettingsContent.Account();
        }

        private async void Logout_Button_Click(object sender, RoutedEventArgs e)
        {
            Default.token = "";
            Default.RefreshToken = "";
            Default.Save();
            if (Default.isConnected)
            {
                await Task.Run(() =>
                {
                    Tunnel.Service.Remove(Globals.ConfigFile, true);
                    try { File.Delete(Globals.ConfigFile); } catch { }
                });
                Default.isConnected = false;
            }
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow.Content = new Login();
        }

        private void Exit_Button_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
