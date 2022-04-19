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
using System.Windows.Controls.Primitives;
using EnRagedGUI.Domain;
using System.Diagnostics;
using Serilog;

namespace EnRagedGUI
{

    public partial class Dashboard : Page
    {


        public static Tunnel.Ringlogger Ringloggger;
        public volatile static bool ThreadsRunning;

        public Dashboard()
        {
            InitializeComponent();

            dropDownLocations.ItemsSource = ShowLocations.GetLocations();
            VersionTextBox.Text = "Version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            themeToggle.IsChecked = Default.DarkTheme;
            killSwitchToggle.IsChecked = Default.KillSwitch;

            ExternalIP.Content = Tools.GetExternalIPAddress();

            Ringloggger = new Tunnel.Ringlogger(LogFile, "GUI");

            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(2500);
            }).ContinueWith(t =>
            {
                //need to get the message queue from the snackbar, so need to be on the dispatcher
                MainSnackbar.MessageQueue.Enqueue("Welcome to EnRagedVPN!");
            }, TaskScheduler.FromCurrentSynchronizationContext());

            ConnectionStatusChanged += Dashboard_ConnectionStatusChanged;

            ExternalIpChanged();

            App.Globals.Snackbar = this.MainSnackbar;
        }

        public static event EventHandler<Models.ConnectionType> ConnectionStatusChanged;

        public void ExternalIpChanged()
        {
            ExternalIP.Content = "External IP: " + Tools.GetExternalIPAddress();
        }

        private async void Dashboard_ConnectionStatusChanged(object sender, Models.ConnectionType e)
        {
            var converter = new BrushConverter();
            Log.Information("Connection status changed to {status}", e.Connection);
            switch (e.Connection)
            {

                case Models.ConnectionStatus.Connecting:
                    ButtonProgressAssist.SetIndicatorForeground(ConnectionButton, (Brush)converter.ConvertFromString("orange"));
                    ButtonProgressAssist.SetIsIndeterminate(ConnectionButton, true);
                    break;

                case Models.ConnectionStatus.Connected:
                    Default.isConnected = true;
                    ConnectionButton.Background = (Brush)converter.ConvertFromString("#FF51AB52");
                    ButtonProgressAssist.SetIsIndeterminate(ConnectionButton, true);
                    ButtonProgressAssist.SetIndicatorForeground(ConnectionButton, (Brush)converter.ConvertFromString("green"));
                    MainSnackbar.MessageQueue.Enqueue("Connected To " + e.ConnectionName);
                    ExternalIpChanged();
                    break;

                case Models.ConnectionStatus.Disconnected:
                    ButtonProgressAssist.SetIsIndeterminate(ConnectionButton, false);
                    ConnectionButton.ClearValue(Button.BackgroundProperty);
                    try
                    {
                        await RemoveService();
                        Default.isConnected = false;
                        Default.Save();
                        ExternalIpChanged();
                        if (e.ConnectionNotification)
                        {
                            MainSnackbar.MessageQueue.Enqueue("Disconnected From " + ConnectionInfo.Name + "!");
                        }
                    }
                    catch { }
                    break;

            }
        }

        private async void Dashboard_Page_Loaded(object sender, RoutedEventArgs e)
        {
            ThreadsRunning = true;
            await Task.Run(async () => { await TailTransfer(); });
            await Task.Run(async () => { await TailLog(); });
        }


        public async void Connection_Button_Click(object sender, RoutedEventArgs e)
        {

            if (dropDownLocations.SelectedValue?.ToString() == null)
            {
                var messageDialog = new MessageDialogPrompt
                {
                    Message = { Text = "No Location Selected!" },
                };

                await DialogHost.Show(messageDialog, "RootDialog");


                return;
            }


            if (!Default.isConnected)
            {
                ConnectionStatusChanged?.Invoke(this, new Models.ConnectionType
                {
                    ConnectionId = dropDownLocations.SelectedValue?.ToString(),
                    ConnectionName = dropDownLocations.Text.ToString(),
                    Connection = Models.ConnectionStatus.Connecting
                });
            }

            await StartConnection(dropDownLocations.SelectedValue.ToString());
        }

        public async Task StartConnection(string locationId)
        {

            Log.Information("Starting Connection");
            Log.Information("Location: " + locationId);
            Log.Information("KillSwitch: " + Default.KillSwitch);
            Log.Information("isConnected" + Default.isConnected.ToString());
            if (Default.isConnected)
            {

                if (dropDownLocations.SelectedValue.ToString() != ConnectionInfo.Id)
                {

                    var view = new MessageDialogPrompt
                    {
                        DataContext = new(),
                        Message = { Text = "Are you sure you want to disconnect from " + ConnectionInfo.Name + " and connect to " + dropDownLocations.Text + "?" },
                    };

                    //show the dialog
                    var result = await DialogHost.Show(view, "RootDialog");
                    if (result.ToString() == "true")
                    {
                        await RemoveService();
                        Default.isConnected = false;
                        Default.Save();
                        await StartConnection(dropDownLocations.SelectedValue.ToString());
                    }

                    return;
                }

                ConnectionStatusChanged?.Invoke(this, new Models.ConnectionType
                {
                    Connection = Models.ConnectionStatus.Disconnected,
                    ConnectionNotification = false
                }); ;

                return;
            }

            try
            {

                await Account.GetNewToken();

                var config = GenerateNewConfigAsync(locationId);

                Log.Debug(await config.ConfigureAwait(true));

                if (string.IsNullOrEmpty(await config.ConfigureAwait(true)))
                {
                    throw new Exception("Location unavailable, try again later!");
                }

                await File.WriteAllBytesAsync(ConfigFile, Encoding.UTF8.GetBytes(await config.ConfigureAwait(true)));
                await Task.Run(() => Tunnel.Service.Add(ConfigFile, true));

                ConnectionInfo.Name = dropDownLocations.Text;
                ConnectionInfo.Id = dropDownLocations.SelectedValue.ToString();

                ConnectionStatusChanged?.Invoke(this, new Models.ConnectionType
                {
                    ConnectionId = ConnectionInfo.Id,
                    ConnectionName = ConnectionInfo.Name,
                    Connection = Models.ConnectionStatus.Connected
                });

            }
            catch (Exception ex)
            {
                var converter = new BrushConverter();
                ButtonProgressAssist.SetIsIndeterminate(ConnectionButton, false);
                MessageBox.Show(ex.Message);
                await RemoveService();
            }
            return;
        }

        private async Task TailLog()
        {
            var converter = new BrushConverter();
            var cursor = Tunnel.Ringlogger.CursorAll;

            while (ThreadsRunning)
            {
                var lines = Ringloggger.FollowFromCursor(ref cursor);

                foreach (var line in lines)
                {
                    Console.WriteLine(line);
                }

                lines.Where(x => x.Contains("Startup complete")).ToImmutableList().ForEach(x =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ConnectionStatusChanged?.Invoke(this, new Models.ConnectionType
                        {
                            ConnectionId = ConnectionInfo.Id,
                            ConnectionName = ConnectionInfo.Name,
                            Connection = Models.ConnectionStatus.Connected
                        });
                    });

                });

                lines.Where(x => x.Contains("retrying (try 3)")).ToImmutableList().ForEach(x =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ConnectionStatusChanged?.Invoke(this, new Models.ConnectionType
                        {
                            Connection = Models.ConnectionStatus.Disconnected
                        });
                    });
                });

                lines.Where(x => x.Contains("Shutting down")).ToImmutableList().ForEach(x =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ConnectionStatusChanged?.Invoke(this, new Models.ConnectionType
                        {
                            Connection = Models.ConnectionStatus.Disconnected,
                            ConnectionNotification = true
                        });
                    });
                });

                //Suspending the thread seems to bring cpu usage 
                try { Thread.Sleep(300); } catch { break; }
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

        private readonly PaletteHelper paletteHelper = new();

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

                        await new Dashboard().StartConnection(ConnectionInfo.Id);
                    });
                });

            }
        }

        private async void Btn_Update_Click(object sender, RoutedEventArgs e)
        {

            await CheckForUpdate(true);

            //var messageDialog = new MessageDialog
            //{
            //    Message = { Text = "Nothing to show" }
            //};

            //await DialogHost.Show(messageDialog, "RootDialog");

        }

    }
}
