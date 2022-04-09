using EnRagedGUI.Helper;
using System.Linq;
using MaterialDesignThemes.Wpf;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static EnRagedGUI.App.Globals;
using static EnRagedGUI.Helper.Wireguard;
using static EnRagedGUI.Properties.Settings;
using System.Collections.Immutable;


namespace EnRagedGUI
{

    public partial class Dashboard : Page
    {

        public Dashboard()
        {
            InitializeComponent();
            GetPublicIPAddress();

            dropDownLocations.ItemsSource = ShowLocations.GetLocations();
            VersionTextBox.Text = "Version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            themeToggle.IsChecked = Default.DarkTheme;
            killSwitchToggle.IsChecked = Default.KillSwitch;

            log = new Tunnel.Ringlogger(LogFile, "GUI");

            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(2500);
            }).ContinueWith(t =>
            {
                //note you can use the message queue from any thread, but just for the demo here we 
                //need to get the message queue from the snackbar, so need to be on the dispatcher
                MainSnackbar.MessageQueue.Enqueue("Welcome to EnRagedVPN!");
            }, TaskScheduler.FromCurrentSynchronizationContext());



            App.Globals.Snackbar = this.MainSnackbar;
        }

        private void Dashboard_Page_Loaded(object sender, RoutedEventArgs e)
        {
            ThreadsRunning = true;
            Task.Run(async () => { await TailTransfer(); });
            Task.Run(async () => { await TailLog(); });
        }

        public void GetPublicIPAddress()
        {

            Task.Factory.StartNew(() =>
            {
                void bindData()
                {
                    if (!string.IsNullOrEmpty(Tools.GetExternalIPAddress()))
                    {
                        ExternalIP.Content = "External IP: " + Tools.GetExternalIPAddress();
                        ExternalIP.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        ExternalIP.Content = "External IP: ";
                    }
                }
                this.Dispatcher.InvokeAsync(bindData);
            });
        }

        public async void Connection_Button_Click(object sender, RoutedEventArgs e)
        {
            if (dropDownLocations.SelectedValue?.ToString() == null)
            {
                MessageBox.Show("No Location Selected");
                return;
            }
            var converter = new BrushConverter();
            ButtonProgressAssist.SetIndicatorForeground(ConnectionButton, (Brush)converter.ConvertFromString("orange"));
            ButtonProgressAssist.SetIsIndeterminate(ConnectionButton, true);
            await StartConnection(dropDownLocations.SelectedValue.ToString());
            return;
        }

        public async Task StartConnection(string locationId)
        {

            if (Default.isConnected)
            {
                if (dropDownLocations.SelectedValue.ToString() != ConnectionState.Id)
                {
                    var result = MessageBox.Show("Are you sure you want to disconnect from " + ConnectionState.Name + " and connect to " + dropDownLocations.Text + "?", "Confirm", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        await RemoveService();
                        Default.isConnected = false;
                        Default.Save();
                        GetPublicIPAddress();
                        await StartConnection(dropDownLocations.SelectedValue.ToString());
                    }
                    return;
                }

                await RemoveService();
                Default.isConnected = false;
                Default.Save();
                ButtonProgressAssist.SetIsIndeterminate(ConnectionButton, false);
                var converter = new BrushConverter();
                ConnectionButton.ClearValue(Button.BackgroundProperty);
                GetPublicIPAddress();
                return;
            }

            try
            {

                await Account.GetNewToken();

                var config = GenerateNewConfigAsync(locationId);

                if (string.IsNullOrEmpty(await config.ConfigureAwait(true)))
                {
                    throw new Exception("Location unavailable, try again later!");
                }

                await File.WriteAllBytesAsync(ConfigFile, Encoding.UTF8.GetBytes(await config.ConfigureAwait(true)));
                await Task.Run(() => Tunnel.Service.Add(ConfigFile, true));

                Default.isConnected = true;
                Default.Save();

                ConnectionState.Name = dropDownLocations.Text;
                ConnectionState.Id = dropDownLocations.SelectedValue.ToString();

            }
            catch (Exception ex)
            {
                var converter = new BrushConverter();
                ButtonProgressAssist.SetIsIndeterminate(ConnectionButton, false);
                log.Write(ex.Message);
                MessageBox.Show(ex.Message);
                await RemoveService();
                GetPublicIPAddress();
            }
            return;
        }

        private async Task TailLog()
        {
            var converter = new BrushConverter();
            var cursor = Tunnel.Ringlogger.CursorAll;

            while (ThreadsRunning)
            {
                var lines = log.FollowFromCursor(ref cursor);

                lines.Where(x => x.Contains("Startup complete")).ToImmutableList().ForEach(x =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        GetPublicIPAddress();
                        ConnectionButton.Background = (Brush)converter.ConvertFromString("#FF51AB52");
                        ButtonProgressAssist.SetIsIndeterminate(ConnectionButton, true);
                        ButtonProgressAssist.SetIndicatorForeground(ConnectionButton, (Brush)converter.ConvertFromString("green"));
                        MainSnackbar.MessageQueue.Enqueue("Connected To " + ConnectionState.Name);
                    });
                });

                lines.Where(x => x.Contains("retrying (try 3)")).ToImmutableList().ForEach(x =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (Default.isConnected)
                        {
                            Default.isConnected = false;
                            Default.Save();
                            GetPublicIPAddress();
                            MainSnackbar.MessageQueue.Enqueue("Disconnected From " + ConnectionState.Name + "!");
                        }
                        ButtonProgressAssist.SetIsIndeterminate(ConnectionButton, false);
                        var converter = new BrushConverter();
                        ConnectionButton.ClearValue(Button.BackgroundProperty);
                    });
                });

                lines.Where(x => x.Contains("Shutting down")).ToImmutableList().ForEach(x =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (Default.isConnected)
                        {
                            Default.isConnected = false;
                            Default.Save();
                            GetPublicIPAddress();
                        }
                        MainSnackbar.MessageQueue.Enqueue("Disconnected From " + ConnectionState.Name + "!");
                        ButtonProgressAssist.SetIsIndeterminate(ConnectionButton, false);
                        var converter = new BrushConverter();
                        ConnectionButton.ClearValue(Button.BackgroundProperty);
                    });
                });

                //Suspending the thread seems to bring cpu usage 
                try { Thread.Sleep(300); } catch { }
            }
        }

        public async Task TailTransfer()
        {
            Tunnel.Driver.Adapter adapter = null;
            while (ThreadsRunning)
            {
                if (adapter == null)
                {
                    while (ThreadsRunning)
                    {
                        try
                        {
                            adapter = Tunnel.Service.GetAdapter(ConfigFile);
                            break;
                        }
                        catch
                        {
                            try
                            {
                                Thread.Sleep(1000);
                            }
                            catch { }
                        }
                    }
                }
                if (adapter == null)
                    continue;
                try
                {
                    ulong rx = 0;
                    ulong tx = 0;

                    var config = adapter.GetConfiguration();
                    foreach (var peer in config.Peers)
                    {
                        rx += peer.RxBytes;
                        tx += peer.TxBytes;
                    }

                    await Task.Factory.StartNew(() =>
                    {
                        var usage = Tools.FormatBytes(tx + rx);

                        void bindData()
                        {
                            if (!string.IsNullOrEmpty(usage))
                            {
                                Usage.Visibility = Visibility.Visible;
                                Usage.Content = "Usage: " + usage;
                            }
                        }
                        this.Dispatcher.InvokeAsync(bindData);
                    });

                    Thread.Sleep(1000);
                }
                catch { adapter = null; }
            }
        }


        //Theme Code ========================>
        public bool IsDarkTheme { get; set; }
        private readonly PaletteHelper paletteHelper = new PaletteHelper();

        private void ToggleTheme(object sender, RoutedEventArgs e)
        {

            //get the current theme used in the application
            ITheme theme = paletteHelper.GetTheme();

            //if condition true, then set IsDarkTheme to false and, SetBaseTheme to light
            if (Default.DarkTheme = theme.GetBaseTheme() == BaseTheme.Dark)
            {
                theme.SetBaseTheme(Theme.Light);
                Default.DarkTheme = false;
            }

            //else set IsDarkTheme to true and SetBaseTheme to dark
            else
            {
                theme.SetBaseTheme(Theme.Dark);
                Default.DarkTheme = true;
            }

            Default.Save();
            paletteHelper.SetTheme(theme);
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

            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow.MainWindowFrame.Content = new Login();
        }

        private async void KillSwitchToggle_Click(object sender, RoutedEventArgs e)
        {

            if ((bool)killSwitchToggle.IsChecked)
            {
                Default.KillSwitch = true;
                Default.Save();
            }
            else
            {
                Default.KillSwitch = false;
                Default.Save();
            }

            if (Default.isConnected)
            {
                await Task.Run(() =>
                {
                    this.Dispatcher.Invoke(async () =>
                    {
                        await RemoveService();
                        Default.isConnected = false;
                        Default.Save();

                        await new Dashboard().StartConnection(ConnectionState.Id);
                    });
                });

            }
        }




    }
}
