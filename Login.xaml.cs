using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
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
        public class Root
        {
            public bool error { get; set; }
            public int status { get; set; }
            public User user { get; set; }
        }

        static bool IsValidEmail(string email)
        {
            var trimmedEmail = email.Trim();

            if (trimmedEmail.EndsWith("."))
            {
                return false;
            }
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == trimmedEmail;
            }
            catch
            {
                return false;
            }
        }

        private async Task GetTokenAsync()
        {
            var email = EmailTextBox.Text.Trim();
            var password = PasswordTextBox.Password.Trim();

            if (!IsValidEmail(email))
            {
                MessageBox.Show("Email not valid!");
                throw new Exception("Email not valid!");
            }
            using var client = new HttpClient();
            client.BaseAddress = new Uri(Globals.API_IP);
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            //login data
            var formContent = new FormUrlEncodedContent(new[]
            {
                    new KeyValuePair<string, string>("email", email),
                    new KeyValuePair<string, string>("password", password)
                });
            
            //send request
            HttpResponseMessage responseMessage = await client.PostAsync("/auth/login", formContent);

            var responseJson = await responseMessage.Content.ReadAsStringAsync();

            var userObject = System.Text.Json.JsonSerializer.Deserialize<Root>(responseJson);

            System.Diagnostics.Debug.WriteLine(userObject.status);
            if (userObject.status == 200)
            {
                Properties.Settings.Default.token = userObject.user.token;
                Properties.Settings.Default.Save();

                MainWindow window = new();
                window.Show();
                this.Close();
            }
            else if (userObject.status == 404)
            {
                MessageBox.Show("Wrong username or password!");
            }
        }

        private void Login_Button_Click(object sender, RoutedEventArgs e)
        {
            _ = GetTokenAsync();
        }

    }
}
