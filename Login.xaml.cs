
using System;
using System.IO;
using System.Net;
using System.Windows;

namespace EnRagedGUI
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }
        public class User
        {
            public string id { get; set; }
            public string email { get; set; }
            public int userId { get; set; }
            public string token { get; set; }
            public bool active { get; set; }
            public int maxConnections { get; set; }
            public long tokenExipry { get; set; }
        }

        public class content
        {
            public string email { get; set; }
            public string password { get; set; }
        }
        public class Root
        {
            public bool error { get; set; }
            public int status { get; set; }
            public User user { get; set; }
        }
        private void GetToken()
        {
            var url = $"{Globals.API_IP}auth/login";
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json; charset=utf-8";
            httpWebRequest.Method = "POST";
            httpWebRequest.Accept = "application/json; charset=utf-8";

            using var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream());


            try
            {
                var loginContent = new content
                {
                    email = textBoxEmail.Text.Trim(),
                    password = passwordBox1.Password.Trim()
                };
                string jsonString = System.Text.Json.JsonSerializer.Serialize(loginContent);

                streamWriter.Write(jsonString);
                streamWriter.Flush();
                streamWriter.Close();

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using var streamReader = new StreamReader(httpResponse.GetResponseStream());
                var result = streamReader.ReadToEnd().ToString();
                System.Diagnostics.Debug.WriteLine(result);
                var userObject = System.Text.Json.JsonSerializer.Deserialize<Root>(result);
                //System.Diagnostics.Debug.WriteLine($"email: { userObject}");
                Properties.Settings.Default["token"] = userObject.user.token;
                Properties.Settings.Default.Save();

                if (!userObject.error)
                {
                    MainWindow window = new();
                    window.Show();

                    this.Close();
                }
                else
                {
                    MessageBox.Show("Your credentials are invalid!");
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
                System.Diagnostics.Debug.WriteLine(err.Message);
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            GetToken();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }


        private void Minimize_Window(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

    }
}
