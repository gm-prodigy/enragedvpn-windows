using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Security.AccessControl;
using EnRagedGUI.EnRagedGUI;
using System.Net;
using EnRagedGUI;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;
using static EnRagedGUI.Globals;
using static EnRagedGUI.Events;


namespace EnRagedVPN_GUI
{

    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            MakeConfigDirectory();
            InitializeComponent();
            LoadEvents();
            transferUpdateThread = new Thread(new ThreadStart(TailTransfer));
            logPrintingThread = new Thread(new ThreadStart(TailLog));
            try { File.Delete(LogFile); } catch { }
            log = new Tunnel.Ringlogger(LogFile, "GUI");

        }

        private static void MakeConfigDirectory()
        {
            var ds = new DirectorySecurity();
            ds.SetSecurityDescriptorSddlForm("O:BAG:BAD:PAI(A;OICI;FA;;;BA)(A;OICI;FA;;;SY)");
            FileSystemAclExtensions.CreateDirectory(ds, UserDirectory);
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
                ConnectionButtonColour = "White";
                ConnectionButtonIcon.Foreground = (Brush)converter.ConvertFromString(ConnectionButtonColour);
                return;
            }

            ConnectionButton.IsEnabled = false;
            try
            {
                var config = GenerateNewConfig();

                await File.WriteAllBytesAsync(ConfigFile, Encoding.UTF8.GetBytes(config));
                await Task.Run(() => Tunnel.Service.Add(ConfigFile, true));
                Connected = true;
                //connectButton.Text = "Disconnect";
                ConnectionButtonColour = "Red";
                var converter = new BrushConverter();
                ConnectionButtonIcon.Foreground = (Brush)converter.ConvertFromString(ConnectionButtonColour);

            }
            catch (Exception ex)
            {
                log.Write(ex.Message);
                MessageBox.Show(ex.Message);
                try { File.Delete(ConfigFile); } catch { }
            }
            ConnectionButton.IsEnabled = true;
            return;
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

        private string GenerateNewConfig()
        {
            //log.Write("Generating keys");
            //log.Write("Exchanging keys with EnRaged Services");
            //log.Write("Generating Configuration");

            //log.Write("Create Request");
            // create request
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"{API_IP}server/nodes/generate/{dropDownLocations.SelectedValue}");
            IWebProxy theProxy = request.Proxy;
            if (theProxy != null)
            {
                theProxy.Credentials = CredentialCache.DefaultCredentials;
            }
            CookieContainer cookies = new CookieContainer();
            request.UseDefaultCredentials = true;
            request.Timeout = 10000;

            // write the "Authorization" header
            request.Headers.Add("Authorization: Bearer " + EnRagedGUI.Properties.Settings.Default.token);
            request.Method = "GET";

            // get the response
            using HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            using StreamReader reader = new(response.GetResponseStream());
            var ServerObject = System.Text.Json.JsonSerializer.Deserialize<Root>(reader.ReadToEnd());


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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ThreadsRunning = true;
            logPrintingThread.Start();
            transferUpdateThread.Start();
            //updateServerList();
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

        private void Menu_Context(object sender, RoutedEventArgs e)
        {
            ContextMenu cm = this.FindResource("cmButton") as ContextMenu;
            cm.PlacementTarget = sender as Button;
            cm.IsOpen = true;
        }

        private void MenuItem_Logout(object sender, RoutedEventArgs e)
        {
            EnRagedGUI.Properties.Settings.Default.token = "";
            EnRagedGUI.Properties.Settings.Default.Save();
            if (Connected)
            {
                ConnectionButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            Login login = new Login();
            login.Show();

            this.Close();
        }

        private void MenuItem_Logger(object sender, RoutedEventArgs e)
        {
            Logger logs = new Logger();
            logs.Show();
        }

    }
}
