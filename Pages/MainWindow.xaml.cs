using EnRagedGUI.Helper;
using EnRagedGUI.Properties;
using FluentScheduler;
using MaterialDesignThemes.Wpf;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static EnRagedGUI.App.Globals;
using static EnRagedGUI.Helper.Wireguard;
using static EnRagedGUI.Properties.Settings;

namespace EnRagedGUI
{

    public partial class MainWindow : Window
    {
        private readonly PaletteHelper paletteHelper = new PaletteHelper();
        public static Snackbar Snackbar;
        public MainWindow()
        {
            InitializeComponent();

            try { File.Delete(LogFile); } catch { }

            JobManager.Initialize();
            SetStartUpTheme();

            _ = StartUpEventsAsync();

        }

        private async Task StartUpEventsAsync()
        {
            await CheckForUpdate(false);

            Default.isConnecting = false;
            Default.isConnected = false;
            Default.LastLocationId = "";
            Default.Save();

            try
            {
                JobManager.AddJob(async () => await CheckForUpdate(false), s => s.ToRunEvery(12).Hours());
            }
            catch { }

            if (Default.token != "")
            {
                MainWindowFrame.Content = new Dashboard();

                //await Task.Delay(1000);
                //if (Default.FirstRun)
                //{
                //    Default.FirstRun = false;
                //    Default.Save();
                //    await Task.Delay(1000);
                //    await Task.Run(() =>
                //    {
                //        try
                //        {
                //            var dialog = new FirstRunDialog();
                //            dialog.ShowDialog();
                //        }
                //        catch (Exception ex)
                //        {
                //            log.Error(ex.Message);
                //        }
                //    });
                //}
                //else
                //{
                //    await Task.Delay(1000);
                //    await Task.Run(() =>
                //    {
                //        try
                //        {
                //            var dialog = new FirstRunDialog();
                //            dialog.ShowDialog();
                //        }
                //        catch (Exception ex)
                //        {
                //            log.Error(ex.Message);
                //        }
                //    });
                //}
            }
            else
            {
                MainWindowFrame.Content = new Login();
            }

        }

        public void SetStartUpTheme()
        {
            //get the current theme used in the application
            ITheme theme = paletteHelper.GetTheme();

            if (Default.DarkTheme == true)
            {
                theme.SetBaseTheme(Theme.Dark);
            }
            else
            {
                theme.SetBaseTheme(Theme.Light);

            }

            //to apply the changes use the SetTheme function
            paletteHelper.SetTheme(theme);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Default.Save();
            Environment.Exit(0);
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {

                this.DragMove();
            }
        }

        private void ExitApp(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void Btn_Logout_Click(object sender, RoutedEventArgs e)
        {
            Default.token = "";
            Default.RefreshToken = "";
            Default.Save();
            if (Default.isConnected)
            {
                await Task.Run(() =>
                {
                    Tunnel.Service.Remove(ConfigFile, true);
                    try { File.Delete(ConfigFile); } catch { }
                });
                Default.isConnected = false;
            }


            MainWindowFrame.Content = new Login();
            //Console.WriteLine(Application.Current);
        }
    }
}
