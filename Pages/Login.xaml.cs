using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static EnRagedGUI.JsonObjects.LoginJsonClass;

namespace EnRagedGUI
{
    /// <summary>
    /// Interaction logic for Page1.xaml
    /// </summary>
    public partial class Login : Page
    {
        public Login()
        {
            InitializeComponent();
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

                MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow.Content = new Dashboard(false);
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
