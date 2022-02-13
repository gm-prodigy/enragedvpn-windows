using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Security.AccessControl;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;
using static EnRagedGUI.Globals;
using System.Net.Http;
using System.Net.Http.Headers;

namespace EnRagedGUI
{

    public partial class MainWindow : Window
    {
        private static Tunnel.Ringlogger log;
        private static Thread logPrintingThread, transferUpdateThread;
        private volatile static bool ThreadsRunning;

        public static bool Connected;
        public static string ConnectionButtonColour = "White";
        public static readonly string UserDirectory = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "Config");
        public static readonly string ConfigFile = Path.Combine(UserDirectory, "enragedvpn.conf");
        public static readonly string LogFile = Path.Combine(UserDirectory, "log.bin");

        public MainWindow()
        {
            MakeConfigDirectory();
            InitializeComponent();
            try { File.Delete(LogFile); } catch { }
            log = new Tunnel.Ringlogger(LogFile, "GUI");
            logPrintingThread = new Thread(new ThreadStart(TailLog));
            transferUpdateThread = new Thread(new ThreadStart(TailTransfer));
            GetPublicIPAddress();
            LoadEvents();
        }


        private static void MakeConfigDirectory()
        {
            var ds = new DirectorySecurity();
            ds.SetSecurityDescriptorSddlForm("O:BAG:BAD:PAI(A;OICI;FA;;;BA)(A;OICI;FA;;;SY)");
            FileSystemAclExtensions.CreateDirectory(ds, UserDirectory);
        }

        void  GetPublicIPAddress()
        {
            Task.Factory.StartNew(() =>
            {
                var ipAddress = GetExternalIPAddress();

                void bindData()
                {
                    if (!string.IsNullOrEmpty(ipAddress))
                        ExternalIP.Content = "External IP: " + ipAddress;
                    else
                        ExternalIP.Content = "External IP: ";

                    ExternalIP.Visibility = Visibility.Visible;
                    log.Write(ipAddress);
                }
                this.Dispatcher.InvokeAsync(bindData);
            });
        }



        public void Connection_Button_Click(object sender, RoutedEventArgs e)
        {
            StartConnection();
        }

        public void LoadEvents()
        {

            dropDownLocations.ItemsSource = ShowLocations.GetLocations();
            dropDownLocations.DisplayMemberPath = "Name";
            dropDownLocations.SelectedValuePath = "Id";
            dropDownLocations.SelectedIndex = dropDownLocations.Items.Count - 1;
        }

        private async Task<string> GenerateNewConfigAsync()
        {
            //log.Write("Generating keys");
            //log.Write("Exchanging keys with EnRaged Services");
            //log.Write("Generating Configuration");


            using var client = new HttpClient();
            client.BaseAddress = new Uri(API_IP);
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Properties.Settings.Default.token);

            //send request
            HttpResponseMessage responseMessage = await client.GetAsync($"/server/nodes/generate/{dropDownLocations.SelectedValue}");

            var responseJson = await responseMessage.Content.ReadAsStringAsync();

            var ServerObject = System.Text.Json.JsonSerializer.Deserialize<Root>(responseJson);

            try
            {
                if(ServerObject.status == 200)
                {

                    var certificate = ServerObject.certificate.data;
                    return $@"[Interface]
PrivateKey = {certificate.Interface.PrivateKey}
Address = {certificate.Interface.Address}
DNS = {certificate.Interface.DNS}

[Peer]
PublicKey = {certificate.Peer.PublicKey}
PresharedKey = {certificate.Peer.PresharedKey}
EndPoint = {certificate.Peer.Endpoint}
AllowedIPs = 0.0.0.0/0, ::0/0";


                }


            }catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }


            return "";
        }

        public async void StartConnection()
        {

            if (Connected)
            {
                ConnectionButton.IsEnabled = false;
                await Task.Run(() =>
                {
                    Tunnel.Service.Remove(ConfigFile, true);
                    try { File.Delete(ConfigFile); } catch { }
                });
                //updateTransferTitle(0, 0);
                //connectButton.Text = "Connect";
                ConnectionButton.IsEnabled = true;
                Connected = false;
                var converter = new BrushConverter();
                ConnectionButtonIcon.Foreground = (Brush)converter.ConvertFromString("White");
                return;
            }

            ConnectionButton.IsEnabled = false;
            try
            {
                var config = GenerateNewConfigAsync();

                if (string.IsNullOrEmpty(await config.ConfigureAwait(true)))
                {
                    throw new Exception("Location unavailable, try again later!");
                }

                await File.WriteAllBytesAsync(ConfigFile, Encoding.UTF8.GetBytes(await config.ConfigureAwait(true)));
                await Task.Run(() => Tunnel.Service.Add(ConfigFile, true));
                Connected = true;
                //connectButton.Text = "Disconnect";
                var converter = new BrushConverter();
                ConnectionButtonIcon.Foreground = (Brush)converter.ConvertFromString("#FF51AB52");
            }
            catch (Exception ex)
            {
                log.Write(ex.Message);
                MessageBox.Show(ex.Message);
                try { File.Delete(ConfigFile); } catch { }
                GetPublicIPAddress();
            }
            ConnectionButton.IsEnabled = true;
            return;
        }

        public class Root
        {
            public bool error { get; set; }
            public int status { get; set; }
            public Certificate certificate { get; set; }
        }

        public class Server
        {
            public string id { get; set; }
            public string serverIp { get; set; }
            public string countryCode { get; set; }
            public string country { get; set; }
            public string city { get; set; }
            public string latitude { get; set; }
            public string longitude { get; set; }
            public string hostname { get; set; }
            public string name { get; set; }
            public string publicKey { get; set; }
            public bool active { get; set; }
            public DateTime createdAt { get; set; }
            public DateTime updatedAt { get; set; }
        }

        public class Certificate
        {
            public Server server { get; set; }
            public Data data { get; set; }
        }
        public class Peer
        {
            public string PublicKey { get; set; }
            public string PresharedKey { get; set; }
            public string Endpoint { get; set; }
            public string AllowedIPs { get; set; }
        }

        public class Data
        {
            public Interface Interface { get; set; }
            public Peer Peer { get; set; }
        }

        public class Interface
        {
            public string PrivateKey { get; set; }
            public string Address { get; set; }
            public string DNS { get; set; }
        }



        private async void Window_Exit(object sender, RoutedEventArgs e)
        {
            if (Connected)
            {
                ConnectionButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Run(() =>
                {
                    Tunnel.Service.Remove(ConfigFile, true);
                    try { File.Delete(ConfigFile); } catch { }
                });
            }
            Application.Current.Shutdown();
            return;
        }

        private void Minimize_Window(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void MenuItem_Logout(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.token = "";
            Properties.Settings.Default.Save();
            if (Connected)
            {
                ConnectionButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            Login login = new Login();
            login.Show();

            this.Close();
        }

        private void TailLog()
        {var converter = new BrushConverter();var cursor = Tunnel.Ringlogger.CursorAll;
            while (ThreadsRunning)
            {
                var lines = log.FollowFromCursor(ref cursor);
                foreach (var line in lines)
                    Dispatcher.Invoke(new Action<string>(ConsoleLogs.AppendText), new object[] { line + "\r\n" });

                foreach (var line in lines)
                    if (line.Contains("Startup complete")){GetPublicIPAddress();}

                foreach (var line in lines)
                    if (line.Contains("retrying (try 3)"))
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            Tunnel.Service.Remove(ConfigFile, true);
                            try { File.Delete(ConfigFile); } catch { }
                            ConnectionButtonIcon.Foreground = (Brush)converter.ConvertFromString("White");
                            Connected = false;
                            GetPublicIPAddress();
                        });
                    }

                foreach (var line in lines)
                    if (line.Contains("Shutting down"))
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            if (Connected){ConnectionButtonIcon.Foreground = (Brush)converter.ConvertFromString("White");Connected = false;GetPublicIPAddress();}
                        });}

                try{Thread.Sleep(300);}catch{break;}
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

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            ThreadsRunning = true;
            logPrintingThread.Start();
            transferUpdateThread.Start();
        }

        public static void TailTransfer()
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
                    //Invoke(new Action<ulong, ulong>(updateTransferTitle), new object[] { rx, tx });
                    //Dispatcher.Invoke(new Action<ulong, ulong>(transferUsage), new object[] { rx, tx });
                    Thread.Sleep(1000);
                }
                catch { adapter = null; }
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var addButton = sender as FrameworkElement;
            if (addButton != null)
            {
                addButton.ContextMenu.IsOpen = true;
            }
        }
    }
}
