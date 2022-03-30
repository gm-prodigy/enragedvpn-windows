using EnRagedGUI.Helper;
using EnRagedGUI.Pages;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static EnRagedGUI.Globals;
using static EnRagedGUI.Helper.Wireguard;
using static EnRagedGUI.Properties.Settings;

namespace EnRagedGUI
{


    public partial class Dashboard : Page
    {

        public Dashboard(bool openLogs)
        {
            InitializeComponent();
            logPrintingThread = new Thread(new ThreadStart(TailLog));
            transferUpdateThread = new Thread(new ThreadStart(TailTransfer));
            GetPublicIPAddress();
            LoadEvents();

            if (openLogs)
            {
                testc();
            }
        }

        private void Dashboard_Page_Loaded(object sender, RoutedEventArgs e)
        {
            ThreadsRunning = true;
            logPrintingThread.Start();
            transferUpdateThread.Start();
        }

        void GetPublicIPAddress()
        {

            Task.Factory.StartNew(() =>
            {
                var ipAddress = Tools.GetExternalIPAddress();

                void bindData()
                {
                    if (!string.IsNullOrEmpty(ipAddress))
                        ExternalIP.Content = "External IP: " + ipAddress;
                    else
                        ExternalIP.Content = "External IP: ";

                    ExternalIP.Visibility = Visibility.Visible;
                }
                this.Dispatcher.InvokeAsync(bindData);
            });
        }

        public void Connection_Button_Click(object sender, RoutedEventArgs e)
        {
            if (dropDownLocations.SelectedValue?.ToString() == null)
            {
                MessageBox.Show("No Location Selected");
                return;
            }
            StartConnection(dropDownLocations.SelectedValue.ToString());
        }

        public void LoadEvents()
        {
            if (Default.isConnected)
            {
                var converter = new BrushConverter();
                ConnectionButtonIcon.Foreground = (Brush)converter.ConvertFromString("#FF51AB52");
            }
            dropDownLocations.ItemsSource = ShowLocations.GetLocations();
            dropDownLocations.DisplayMemberPath = "Name";
            dropDownLocations.SelectedValuePath = "Id";
            dropDownLocations.SelectedIndex = dropDownLocations.Items.Count - 1;
        }



        public async void StartConnection(string locationId)
        {


            if (Default.isConnected)
            {
                await Task.Run(() =>
                {
                    Tunnel.Service.Remove(ConfigFile, true);
                    try { File.Delete(ConfigFile); } catch { }
                });
                //updateTransferTitle(0, 0);
                //connectButton.Text = "Connect";
                Default.isConnected = false;
                Default.Save();
                var converter = new BrushConverter();
                ConnectionButtonIcon.Foreground = (Brush)converter.ConvertFromString("White");
                GetPublicIPAddress();
                return;
            }

            try
            {
                await Account.GetNewToken();

                var config = Wireguard.GenerateNewConfigAsync(locationId);

                Console.WriteLine(config);

                if (string.IsNullOrEmpty(await config.ConfigureAwait(true)))
                {
                    throw new Exception("Location unavailable, try again later!");
                }

                await File.WriteAllBytesAsync(ConfigFile, Encoding.UTF8.GetBytes(await config.ConfigureAwait(true)));
                await Task.Run(() => Tunnel.Service.Add(ConfigFile, true));

                Default.isConnected = true;
                Default.Save();

                var converter = new BrushConverter();

                ConnectionButtonIcon.Foreground = (Brush)converter.ConvertFromString("#FF51AB52");

                ConnectionState.Name = dropDownLocations.Text;
                ConnectionState.Id = dropDownLocations.SelectedValue.ToString();

            }
            catch (Exception ex)
            {
                log.Write(ex.Message);
                MessageBox.Show(ex.Message);
                try { File.Delete(ConfigFile); } catch { }
                GetPublicIPAddress();
            }
            return;
        }





        private async void Window_Exit(object sender, CancelEventArgs e)
        {
            if (Default.isConnected)
            {
                ConnectionButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Run(() =>
                {
                    Tunnel.Service.Remove(ConfigFile, true);
                    try { File.Delete(ConfigFile); } catch { }
                });
            }

            return;
        }

        private void TailLog()
        {
            var converter = new BrushConverter(); var cursor = Tunnel.Ringlogger.CursorAll;
            while (ThreadsRunning)
            {
                var lines = log.FollowFromCursor(ref cursor);
                foreach (var line in lines)
                    Dispatcher.Invoke(new Action<string>(ConsoleLogs.AppendText), new object[] { line + "\r\n" });

                foreach (var line in lines)
                    if (line.Contains("Startup complete")) { GetPublicIPAddress(); }

                foreach (var line in lines)
                    if (line.Contains("retrying (try 3)"))
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            Tunnel.Service.Remove(ConfigFile, true);
                            try { File.Delete(ConfigFile); } catch { }
                            ConnectionButtonIcon.Foreground = (Brush)converter.ConvertFromString("White");
                            Default.isConnected = false;
                            Default.Save();
                            GetPublicIPAddress();
                        });
                    }

                foreach (var line in lines)
                    if (line.Contains("Shutting down"))
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            if (Default.isConnected) { ConnectionButtonIcon.Foreground = (Brush)converter.ConvertFromString("White"); Default.isConnected = false; Default.Save(); GetPublicIPAddress(); }
                        });
                    }

                try { Thread.Sleep(300); } catch { break; }
            }
        }

        private void MenuItem_Logs(object sender, RoutedEventArgs e)
        {
            if (ConsoleLogsGrids.Visibility == Visibility.Hidden)
            {
                ConsoleLogsGrids.Visibility = Visibility.Visible;
            }
            else
            {
                ConsoleLogsGrids.Visibility = Visibility.Hidden;
            }
        }

        public void testc()
        {
            if (ConsoleLogsGrids.Visibility == Visibility.Hidden)
            {
                ConsoleLogsGrids.Visibility = Visibility.Visible;
            }
            else
            {
                ConsoleLogsGrids.Visibility = Visibility.Hidden;
            }
        }

        public void TailTransfer()
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

                    Task.Factory.StartNew(() =>
                    {
                        var usage = Tools.FormatBytes(tx + rx);

                        void bindData()
                        {
                            if (!string.IsNullOrEmpty(usage))
                            {
                                Usage.Visibility = Visibility.Visible;
                                Usage.Content = "Usage: " + usage;
                            }
                            else
                            {
                                Usage.Visibility = Visibility.Hidden;
                                Usage.Content = "Usage: ";
                            }
                        }
                        this.Dispatcher.InvokeAsync(bindData);
                    });

                    Thread.Sleep(1000);
                }
                catch { adapter = null; }
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement addButton)
            {
                addButton.ContextMenu.IsOpen = true;
            }
        }

        private void Settingsaa(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow.Content = new Settings();
        }
    }
}
