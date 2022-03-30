using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
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

        //Theme Code ========================>
        public bool IsDarkTheme { get; set; }
        private readonly PaletteHelper paletteHelper = new PaletteHelper();

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
            //var email = EmailTextBox.Text.Trim();
            //var password = PasswordTextBox.Password.Trim();
            //var email = "";
            //var password = "";
            var email = txtUsername.Text.Trim();
            var password = txtPassword.Password.Trim();

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

            System.Diagnostics.Debug.WriteLine(userObject.code);
            if (userObject.code == 200)
            {
                Properties.Settings.Default.token = userObject.user.token;
                Properties.Settings.Default.RefreshToken = userObject.user.refreshToken;
                Properties.Settings.Default.Email = email;
                Properties.Settings.Default.Save();

                MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow.Content = new Dashboard(false);
            }
            else if (userObject.code == 401)
            {
                MessageBox.Show("Wrong username or password!");
            }
            else
            {
                MessageBox.Show("Not sure what went wrong!");
            }
        }

        private void Login_Button_Click(object sender, RoutedEventArgs e)
        {
            _ = GetTokenAsync();
        }

        private void toggleTheme(object sender, RoutedEventArgs e)
        {

            //get the current theme used in the application
            ITheme theme = paletteHelper.GetTheme();

            //if condition true, then set IsDarkTheme to false and, SetBaseTheme to light
            if (IsDarkTheme = theme.GetBaseTheme() == BaseTheme.Dark)
            {
                IsDarkTheme = false;
                theme.SetBaseTheme(Theme.Light);
                LoginLogo.Source = new BitmapImage(new Uri(@"/Pages/Enraged_Black.png", UriKind.RelativeOrAbsolute));
            }

            //else set IsDarkTheme to true and SetBaseTheme to dark
            else
            {
                IsDarkTheme = true;
                theme.SetBaseTheme(Theme.Dark);
                LoginLogo.Source = new BitmapImage(new Uri(@"/Pages/Enraged_White.png", UriKind.RelativeOrAbsolute));
            }

            //to apply the changes use the SetTheme function
            paletteHelper.SetTheme(theme);
        }

        private void exitApp(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }


    }
}
